using System.Collections.Concurrent;
using System.Text.Json;
using FishServer.Handlers;
using FishServer.Messages;

namespace FishServer
{
    /// <summary>
    /// 消息分发器
    /// 注册消息类型到处理器映射，收到消息后根据 type 字段分发到对应处理器
    /// </summary>
    public class MessageDispatcher
    {
        private readonly ConcurrentDictionary<string, IMessageHandler> _handlers = new ConcurrentDictionary<string, IMessageHandler>();

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="handler">处理器实例</param>
        public void Register(string messageType, IMessageHandler handler)
        {
            _handlers[messageType] = handler;
            Console.WriteLine($"[MessageDispatcher] 已注册消息处理器: {messageType}");
        }

        /// <summary>
        /// 分发并处理消息
        /// </summary>
        /// <param name="message">收到的消息包装</param>
        /// <returns>响应消息包装（处理失败时返回ErrorResponse）</returns>
        public BaseMessage Dispatch(BaseMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.type))
            {
                return BaseMessage.Create(MessageType.ErrorResponse, new ErrorResponse
                {
                    message = "消息类型为空",
                    requestType = "",
                });
            }

            if (_handlers.TryGetValue(message.type, out var handler))
            {
                try
                {
                    return handler.Handle(message.data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MessageDispatcher] 处理消息 {message.type} 异常: {ex.Message}");
                    return BaseMessage.Create(MessageType.ErrorResponse, new ErrorResponse
                    {
                        message = $"处理消息异常: {ex.Message}",
                        requestType = message.type,
                    });
                }
            }
            else
            {
                Console.WriteLine($"[MessageDispatcher] 未知消息类型: {message.type}");
                return BaseMessage.Create(MessageType.ErrorResponse, new ErrorResponse
                {
                    message = $"未知消息类型: {message.type}",
                    requestType = message.type,
                });
            }
        }
    }
}
