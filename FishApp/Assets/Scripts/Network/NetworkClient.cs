// 网络客户端
// 负责与服务器建立TCP连接，异步收发JSON消息
// 消息格式：{ "type": "...", "data": {...} }，与FishServer协议一致
// 提供登录、上报关卡结果、获取玩家数据、查询排行榜等接口
// 支持自动重连和连接状态管理

using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 网络连接状态
    /// </summary>
    public enum NetworkConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    /// <summary>
    /// 网络客户端单例，管理与服务器的TCP连接
    /// 实现 INetworkClient 接口供 GameManager 调用
    /// </summary>
    public class NetworkClient : MonoBehaviour, INetworkClient
    {
        private static NetworkClient _instance;
        public static NetworkClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkClient>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(typeof(NetworkClient).Name);
                        _instance = go.AddComponent<NetworkClient>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("服务器配置")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private int serverPort = 8888;

        [Header("重连配置")]
        [SerializeField] private bool autoReconnect = true;
        [SerializeField] private float reconnectInterval = 3f;
        [SerializeField] private int maxReconnectAttempts = 5;

        [Header("心跳配置")]
        [SerializeField] private bool enableHeartbeat = true;
        [SerializeField] private float heartbeatInterval = 30f;

        // 消息分隔符，用于TCP流式消息的边界识别（与服务器一致）
        private const string MessageDelimiter = "\n";

        // 连接状态
        public NetworkConnectionState ConnectionState { get; private set; } = NetworkConnectionState.Disconnected;

        // 当前玩家信息
        public string CurrentPlayerId { get; private set; }
        public string CurrentUsername { get; private set; }
        public bool IsLoggedIn { get; private set; }

        // 事件
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnMessageReceived; // 完整JSON消息
        public event Action<string> OnError;

        // 内部网络对象
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private Thread _receiveThread;
        private Thread _sendThread;

        // 线程安全队列：主线程执行的回调
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _mainThreadActions = new System.Collections.Concurrent.ConcurrentQueue<Action>();

        // 发送队列
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _sendQueue = new System.Collections.Concurrent.ConcurrentQueue<string>();

        // 运行标志
        private bool _isRunning = false;
        private int _reconnectAttempts = 0;
        private bool _intentionalDisconnect = false;

        // 心跳计时
        private float _lastHeartbeatTime = 0f;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // 执行主线程回调
            while (_mainThreadActions.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkClient] 主线程回调执行异常: {e}");
                }
            }

            // 心跳
            if (enableHeartbeat && ConnectionState == NetworkConnectionState.Connected)
            {
                _lastHeartbeatTime += Time.deltaTime;
                if (_lastHeartbeatTime >= heartbeatInterval)
                {
                    _lastHeartbeatTime = 0f;
                    SendHeartbeat();
                }
            }
        }

        /// <summary>
        /// 连接到服务器（使用配置的IP和端口）
        /// </summary>
        public void Connect()
        {
            Connect(serverIP, serverPort);
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <param name="ip">服务器IP</param>
        /// <param name="port">服务器端口</param>
        public void Connect(string ip, int port)
        {
            if (ConnectionState == NetworkConnectionState.Connected || ConnectionState == NetworkConnectionState.Connecting)
            {
                Debug.Log("[NetworkClient] 已连接或正在连接中");
                return;
            }

            _intentionalDisconnect = false;
            serverIP = ip;
            serverPort = port;
            ConnectionState = NetworkConnectionState.Connecting;

            Debug.Log($"[NetworkClient] 正在连接服务器 {ip}:{port}...");

            // 在后台线程执行连接
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(ip, port);
                    _networkStream = _tcpClient.GetStream();
                    _isRunning = true;

                    ConnectionState = NetworkConnectionState.Connected;
                    _reconnectAttempts = 0;

                    // 启动接收线程
                    _receiveThread = new Thread(ReceiveLoop)
                    {
                        IsBackground = true,
                        Name = "NetworkClient_Receive"
                    };
                    _receiveThread.Start();

                    // 启动发送线程
                    _sendThread = new Thread(SendLoop)
                    {
                        IsBackground = true,
                        Name = "NetworkClient_Send"
                    };
                    _sendThread.Start();

                    // 主线程回调
                    _mainThreadActions.Enqueue(() =>
                    {
                        Debug.Log("[NetworkClient] 连接服务器成功");
                        OnConnected?.Invoke();
                    });
                }
                catch (Exception e)
                {
                    ConnectionState = NetworkConnectionState.Disconnected;
                    _mainThreadActions.Enqueue(() =>
                    {
                        Debug.LogError($"[NetworkClient] 连接服务器失败: {e.Message}");
                        OnError?.Invoke($"连接失败: {e.Message}");
                        OnDisconnected?.Invoke();

                        // 尝试重连
                        if (autoReconnect && !_intentionalDisconnect)
                        {
                            StartCoroutine(TryReconnect());
                        }
                    });
                }
            });
        }

        /// <summary>
        /// 断开与服务器的连接
        /// </summary>
        public void Disconnect()
        {
            _intentionalDisconnect = true;
            _isRunning = false;

            CloseConnection();

            ConnectionState = NetworkConnectionState.Disconnected;
            OnDisconnected?.Invoke();
            Debug.Log("[NetworkClient] 已断开服务器连接");
        }

        /// <summary>
        /// 发送消息（格式：{ "type": "...", "data": {...} }）
        /// data为JSON对象（非字符串），与服务器System.Text.Json格式一致
        /// </summary>
        /// <typeparam name="T">消息数据类型</typeparam>
        /// <param name="type">消息类型</param>
        /// <param name="data">消息数据对象</param>
        public void Send<T>(string type, T data)
        {
            if (ConnectionState != NetworkConnectionState.Connected)
            {
                Debug.LogWarning("[NetworkClient] 未连接服务器，无法发送消息");
                OnError?.Invoke("未连接服务器");
                return;
            }

            try
            {
                // 手动拼接JSON，确保data为JSON对象（而非字符串）
                // 服务器使用System.Text.Json，data字段为JsonElement（对象）
                string dataJson = data != null ? JsonUtility.ToJson(data) : "{}";
                string json = $"{{\"type\":\"{type}\",\"data\":{dataJson}}}";
                json += MessageDelimiter;
                _sendQueue.Enqueue(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] 发送消息失败: {e}");
                OnError?.Invoke($"发送失败: {e.Message}");
            }
        }

        /// <summary>
        /// 发送原始JSON字符串消息
        /// </summary>
        /// <param name="json">消息JSON</param>
        public void SendRaw(string json)
        {
            if (ConnectionState != NetworkConnectionState.Connected)
            {
                Debug.LogWarning("[NetworkClient] 未连接服务器，无法发送消息");
                return;
            }

            _sendQueue.Enqueue(json + MessageDelimiter);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="username">用户名</param>
        public void Login(string username)
        {
            if (ConnectionState != NetworkConnectionState.Connected)
            {
                Debug.LogWarning("[NetworkClient] 未连接服务器，无法登录");
                OnError?.Invoke("未连接服务器，无法登录");
                return;
            }

            LoginRequest request = new LoginRequest(username);
            CurrentUsername = username;
            Send(MessageType.LoginRequest, request);
            Debug.Log($"[NetworkClient] 发送登录请求: {username}");
        }

        /// <summary>
        /// 上报关卡结果
        /// </summary>
        /// <param name="levelId">关卡ID</param>
        /// <param name="score">得分</param>
        /// <param name="success">是否过关</param>
        public void ReportLevelResult(int levelId, int score, bool success)
        {
            if (ConnectionState != NetworkConnectionState.Connected)
            {
                Debug.LogWarning("[NetworkClient] 未连接服务器，无法上报关卡结果");
                OnError?.Invoke("未连接服务器");
                return;
            }

            LevelResultRequest request = new LevelResultRequest(CurrentPlayerId, levelId, score, success);
            Send(MessageType.LevelResultRequest, request);
            Debug.Log($"[NetworkClient] 上报关卡结果: level={levelId}, score={score}, success={success}");
        }

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        public void GetPlayerData()
        {
            if (ConnectionState != NetworkConnectionState.Connected)
            {
                Debug.LogWarning("[NetworkClient] 未连接服务器，无法获取玩家数据");
                OnError?.Invoke("未连接服务器");
                return;
            }

            PlayerDataRequest request = new PlayerDataRequest(CurrentPlayerId);
            Send(MessageType.PlayerDataRequest, request);
            Debug.Log("[NetworkClient] 请求玩家数据");
        }

        /// <summary>
        /// 查询排行榜
        /// </summary>
        /// <param name="topN">请求条目数量，默认100</param>
        public void GetLeaderboard(int topN = 100)
        {
            if (ConnectionState != NetworkConnectionState.Connected)
            {
                Debug.LogWarning("[NetworkClient] 未连接服务器，无法查询排行榜");
                OnError?.Invoke("未连接服务器");
                return;
            }

            LeaderboardRequest request = new LeaderboardRequest(topN);
            Send(MessageType.LeaderboardRequest, request);
            Debug.Log($"[NetworkClient] 请求排行榜 topN={topN}");
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        private void SendHeartbeat()
        {
            HeartbeatMessage heartbeat = new HeartbeatMessage
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            Send(MessageType.Heartbeat, heartbeat);
        }

        /// <summary>
        /// 尝试自动重连
        /// </summary>
        private IEnumerator TryReconnect()
        {
            if (!autoReconnect || _intentionalDisconnect)
            {
                yield break;
            }

            while (_reconnectAttempts < maxReconnectAttempts && !_intentionalDisconnect)
            {
                _reconnectAttempts++;
                Debug.Log($"[NetworkClient] 尝试重连 ({_reconnectAttempts}/{maxReconnectAttempts})...");

                yield return new WaitForSeconds(reconnectInterval);

                if (_intentionalDisconnect)
                {
                    yield break;
                }

                Connect(serverIP, serverPort);

                // 等待连接结果
                yield return new WaitForSeconds(1f);

                if (ConnectionState == NetworkConnectionState.Connected)
                {
                    yield break;
                }
            }

            if (_reconnectAttempts >= maxReconnectAttempts)
            {
                Debug.LogError("[NetworkClient] 达到最大重连次数，停止重连");
                OnError?.Invoke("达到最大重连次数，连接失败");
            }
        }

        /// <summary>
        /// 接收消息循环（在后台线程执行）
        /// </summary>
        private void ReceiveLoop()
        {
            byte[] buffer = new byte[8192];
            StringBuilder messageBuilder = new StringBuilder();

            while (_isRunning && _networkStream != null && _tcpClient.Connected)
            {
                try
                {
                    int bytesRead = _networkStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // 服务器关闭连接
                        break;
                    }

                    string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(received);

                    // 按分隔符拆分消息
                    string allMessages = messageBuilder.ToString();
                    int delimiterIndex;
                    while ((delimiterIndex = allMessages.IndexOf(MessageDelimiter)) != -1)
                    {
                        string singleMessage = allMessages.Substring(0, delimiterIndex);
                        allMessages = allMessages.Substring(delimiterIndex + MessageDelimiter.Length);

                        if (!string.IsNullOrEmpty(singleMessage))
                        {
                            string msgCopy = singleMessage;
                            _mainThreadActions.Enqueue(() =>
                            {
                                OnMessageReceived?.Invoke(msgCopy);
                                // 分发到消息处理器
                                NetworkMessageHandler.Instance?.DispatchMessage(msgCopy);
                            });
                        }
                    }

                    messageBuilder.Clear();
                    messageBuilder.Append(allMessages);
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"[NetworkClient] 接收消息异常: {e.Message}");
                    }
                    break;
                }
            }

            // 连接断开
            if (_isRunning)
            {
                _isRunning = false;
                ConnectionState = NetworkConnectionState.Disconnected;

                _mainThreadActions.Enqueue(() =>
                {
                    OnDisconnected?.Invoke();

                    if (autoReconnect && !_intentionalDisconnect)
                    {
                        StartCoroutine(TryReconnect());
                    }
                });
            }
        }

        /// <summary>
        /// 发送消息循环（在后台线程执行）
        /// </summary>
        private void SendLoop()
        {
            while (_isRunning && _networkStream != null && _tcpClient.Connected)
            {
                try
                {
                    if (_sendQueue.TryDequeue(out string message))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        _networkStream.Write(data, 0, data.Length);
                        _networkStream.Flush();
                    }
                    else
                    {
                        // 没有消息时休眠，避免CPU空转
                        Thread.Sleep(10);
                    }
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"[NetworkClient] 发送消息异常: {e.Message}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 关闭连接和资源
        /// </summary>
        private void CloseConnection()
        {
            _isRunning = false;

            try
            {
                _networkStream?.Close();
            }
            catch { }
            _networkStream = null;

            try
            {
                _tcpClient?.Close();
            }
            catch { }
            _tcpClient = null;

            // 清空发送队列
            while (_sendQueue.TryDequeue(out _)) { }
        }

        /// <summary>
        /// 设置当前登录玩家信息（由消息处理回调调用）
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="username">用户名</param>
        public void SetLoginInfo(string playerId, string username)
        {
            CurrentPlayerId = playerId;
            CurrentUsername = username;
            IsLoggedIn = !string.IsNullOrEmpty(playerId);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}
