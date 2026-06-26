using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using FishServer.Config;
using FishServer.Handlers;
using FishServer.Managers;
using FishServer.Messages;
using FishServer.Storage;

namespace FishServer
{
    /// <summary>
    /// 游戏服务器核心
    /// 负责TcpListener监听、接受客户端连接、管理在线会话、广播功能
    /// </summary>
    public class GameServer
    {
        private readonly ServerConfig _config;
        private TcpListener? _listener;
        private readonly ConcurrentDictionary<string, ClientSession> _sessions = new ConcurrentDictionary<string, ClientSession>();
        private CancellationTokenSource? _cts;

        // 核心组件
        public MessageDispatcher Dispatcher { get; }
        public PlayerManager PlayerManager { get; }
        public LeaderboardManager LeaderboardManager { get; }
        public FileStorage Storage { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        public GameServer(ServerConfig config)
        {
            _config = config;
            Storage = new FileStorage(config);
            PlayerManager = new PlayerManager(config, Storage);
            LeaderboardManager = new LeaderboardManager(PlayerManager);
            Dispatcher = new MessageDispatcher();
            RegisterHandlers();
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterHandlers()
        {
            Dispatcher.Register(MessageType.LoginRequest, new LoginHandler(PlayerManager));
            Dispatcher.Register(MessageType.LevelResultRequest, new LevelResultHandler(PlayerManager, LeaderboardManager));
            Dispatcher.Register(MessageType.PlayerDataRequest, new PlayerDataHandler(PlayerManager, LeaderboardManager));
            Dispatcher.Register(MessageType.LeaderboardRequest, new LeaderboardHandler(LeaderboardManager));
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            IPAddress ipAddress = ParseIPAddress(_config.IP);
            _listener = new TcpListener(ipAddress, _config.Port);
            _listener.Start();
            IsRunning = true;

            Console.WriteLine($"[GameServer] 服务器已启动，监听 {ipAddress}:{_config.Port}");
            Console.WriteLine($"[GameServer] 数据目录: {Path.GetFullPath(_config.DataPath)}");
            Console.WriteLine($"[GameServer] 玩家数据文件: {Path.GetFullPath(_config.GetPlayersFilePath())}");

            // 启动自动保存任务
            _ = Storage.StartAutoSaveAsync(PlayerManager.PlayersInternal, _cts.Token);

            // 接受连接循环
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);

                    // 检查最大连接数
                    if (_config.MaxConnections > 0 && _sessions.Count >= _config.MaxConnections)
                    {
                        Console.WriteLine($"[GameServer] 已达最大连接数 {_config.MaxConnections}，拒绝新连接");
                        client.Close();
                        continue;
                    }

                    var session = new ClientSession(client, this, Dispatcher, _config, _cts.Token);
                    _sessions[session.SessionId] = session;

                    // 启动会话处理任务（不等待）
                    _ = Task.Run(() => session.RunAsync());
                }
            }
            catch (OperationCanceledException)
            {
                // 正常关闭
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameServer] 监听异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public async Task StopAsync()
        {
            Console.WriteLine("[GameServer] 正在停止服务器...");
            IsRunning = false;

            _cts?.Cancel();
            _listener?.Stop();

            // 断开所有会话
            Console.WriteLine($"[GameServer] 正在断开 {_sessions.Count} 个客户端连接...");
            foreach (var session in _sessions.Values)
            {
                session.Disconnect();
            }
            _sessions.Clear();

            // 保存数据
            PlayerManager.Save();

            Console.WriteLine("[GameServer] 服务器已停止。");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 移除会话
        /// </summary>
        public void RemoveSession(ClientSession session)
        {
            _sessions.TryRemove(session.SessionId, out _);
        }

        /// <summary>
        /// 广播消息给所有在线会话
        /// </summary>
        public async Task BroadcastAsync(BaseMessage message)
        {
            var tasks = _sessions.Values.Select(s => s.SendAsync(message)).ToList();
            await Task.WhenAll(tasks);
            Console.WriteLine($"[GameServer] 已广播消息给 {_sessions.Count} 个客户端: type={message.type}");
        }

        /// <summary>
        /// 获取当前在线会话数
        /// </summary>
        public int OnlineCount => _sessions.Count;

        /// <summary>
        /// 解析IP地址，支持 "0.0.0.0" 表示所有网卡
        /// </summary>
        private IPAddress ParseIPAddress(string ip)
        {
            if (string.IsNullOrEmpty(ip) || ip == "0.0.0.0")
            {
                return IPAddress.Any;
            }
            return IPAddress.Parse(ip);
        }
    }
}
