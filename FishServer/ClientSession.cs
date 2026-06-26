using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FishServer.Config;
using FishServer.Messages;

namespace FishServer
{
    /// <summary>
    /// 客户端会话
    /// 处理单个客户端连接的异步消息收发
    /// </summary>
    public class ClientSession
    {
        private readonly TcpClient _client;
        private readonly GameServer _server;
        private readonly MessageDispatcher _dispatcher;
        private readonly ServerConfig _config;
        private readonly CancellationToken _cancellationToken;
        private NetworkStream? _stream;

        /// <summary>
        /// 会话唯一ID
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        /// 远程端点信息
        /// </summary>
        public string RemoteEndPoint { get; }

        /// <summary>
        /// 是否已认证（登录后设置）
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// 关联的玩家ID（登录后设置）
        /// </summary>
        public string? PlayerId { get; set; }

        public ClientSession(TcpClient client, GameServer server, MessageDispatcher dispatcher, ServerConfig config, CancellationToken cancellationToken)
        {
            _client = client;
            _server = server;
            _dispatcher = dispatcher;
            _config = config;
            _cancellationToken = cancellationToken;
            SessionId = Guid.NewGuid().ToString("N");
            RemoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        }

        /// <summary>
        /// 启动会话处理（异步接收消息循环）
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine($"[ClientSession] 客户端连接: {RemoteEndPoint} (会话: {SessionId})");

            try
            {
                _stream = _client.GetStream();
                var buffer = new byte[_config.ReceiveBufferSize];
                var messageBuffer = new StringBuilder();

                while (!_cancellationToken.IsCancellationRequested && _client.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, _cancellationToken);
                    if (bytesRead == 0)
                    {
                        // 客户端正常关闭连接
                        break;
                    }

                    // 将收到的字节转为字符串追加到消息缓冲
                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuffer.Append(chunk);

                    // 按换行符分割消息（每条消息一行）
                    // 同时支持不带换行的完整JSON消息
                    string allText = messageBuffer.ToString();
                    while (TryExtractMessage(ref allText, out string messageJson))
                    {
                        messageBuffer.Clear();
                        messageBuffer.Append(allText);
                        await ProcessMessageAsync(messageJson);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 服务器关闭，正常退出
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientSession] 会话 {SessionId} 异常: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        /// <summary>
        /// 尝试从缓冲中提取一条完整消息
        /// 消息格式：每条消息以换行符分隔，或单条完整JSON
        /// </summary>
        private bool TryExtractMessage(ref string buffer, out string message)
        {
            message = string.Empty;
            int newlineIdx = buffer.IndexOf('\n');
            if (newlineIdx >= 0)
            {
                message = buffer.Substring(0, newlineIdx).TrimEnd('\r');
                buffer = buffer.Substring(newlineIdx + 1);
                return !string.IsNullOrWhiteSpace(message);
            }

            // 没有换行符，尝试解析为完整JSON对象
            // 如果是完整的JSON（以{开始，以}结束且括号匹配），则处理
            string trimmed = buffer.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}") && IsBalancedJson(trimmed))
            {
                message = trimmed;
                buffer = string.Empty;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 简单检查JSON括号是否平衡
        /// </summary>
        private bool IsBalancedJson(string json)
        {
            int depth = 0;
            bool inString = false;
            bool escape = false;
            foreach (char c in json)
            {
                if (escape) { escape = false; continue; }
                if (c == '\\') { escape = true; continue; }
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;
                if (c == '{') depth++;
                else if (c == '}') depth--;
            }
            return depth == 0 && !inString;
        }

        /// <summary>
        /// 处理单条消息
        /// </summary>
        private async Task ProcessMessageAsync(string messageJson)
        {
            try
            {
                var message = JsonSerializer.Deserialize<BaseMessage>(messageJson);
                if (message == null) return;

                Console.WriteLine($"[ClientSession] 收到消息 [{SessionId}]: type={message.type}");

                // 登录后记录玩家ID
                if (message.type == MessageType.LoginRequest)
                {
                    // 在Handler处理后设置PlayerId
                }

                var response = _dispatcher.Dispatch(message);

                // 如果是登录响应且成功，提取playerId
                if (response.type == MessageType.LoginResponse)
                {
                    if (response.data.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        if (response.data.TryGetProperty("playerId", out var idProp))
                        {
                            PlayerId = idProp.GetString();
                            IsAuthenticated = true;
                        }
                    }
                }

                await SendAsync(response);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[ClientSession] 消息JSON解析失败 [{SessionId}]: {ex.Message}");
                var errorResponse = BaseMessage.Create(MessageType.ErrorResponse, new ErrorResponse
                {
                    message = "消息格式错误，应为JSON",
                    requestType = "",
                });
                await SendAsync(errorResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientSession] 处理消息异常 [{SessionId}]: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送消息到客户端
        /// </summary>
        public async Task SendAsync(BaseMessage message)
        {
            if (_stream == null || !_client.Connected) return;

            try
            {
                string json = JsonSerializer.Serialize(message);
                // 消息以换行符结尾，便于客户端分割
                byte[] data = Encoding.UTF8.GetBytes(json + "\n");
                await _stream.WriteAsync(data, _cancellationToken);
                Console.WriteLine($"[ClientSession] 发送消息 [{SessionId}]: type={message.type}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientSession] 发送消息失败 [{SessionId}]: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            Console.WriteLine($"[ClientSession] 客户端断开: {RemoteEndPoint} (会话: {SessionId})");
            _server.RemoveSession(this);
            try
            {
                _stream?.Close();
                _client.Close();
            }
            catch { }
        }
    }
}
