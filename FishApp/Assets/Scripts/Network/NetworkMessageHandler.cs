// 网络消息处理器
// 负责注册和管理消息处理回调，将收到的消息分发到对应的处理器
// 使用观察者模式实现消息的分发

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 网络消息处理器，单例模式
    /// 负责注册消息处理回调并分发收到的消息
    /// </summary>
    public class NetworkMessageHandler : MonoBehaviour
    {
        private static NetworkMessageHandler _instance;
        public static NetworkMessageHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkMessageHandler>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(typeof(NetworkMessageHandler).Name);
                        _instance = go.AddComponent<NetworkMessageHandler>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 消息处理回调委托
        /// 参数为消息的JSON数据字符串
        /// </summary>
        /// <param name="json">消息JSON数据</param>
        public delegate void MessageHandler(string json);

        // 消息处理器字典：type -> 处理回调列表
        private Dictionary<string, List<MessageHandler>> _handlers = new Dictionary<string, List<MessageHandler>>();

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

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="handler">处理回调</param>
        public void RegisterHandler(string type, MessageHandler handler)
        {
            if (string.IsNullOrEmpty(type) || handler == null)
            {
                Debug.LogWarning("[NetworkMessageHandler] RegisterHandler: type或handler为空");
                return;
            }

            if (!_handlers.ContainsKey(type))
            {
                _handlers[type] = new List<MessageHandler>();
            }

            if (!_handlers[type].Contains(handler))
            {
                _handlers[type].Add(handler);
            }
        }

        /// <summary>
        /// 使用Action<string>注册消息处理器（接口兼容）
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="handler">处理回调</param>
        public void RegisterHandler(string type, Action<string> handler)
        {
            RegisterHandler(type, new MessageHandler(handler));
        }

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="handler">处理回调</param>
        public void UnregisterHandler(string type, MessageHandler handler)
        {
            if (string.IsNullOrEmpty(type) || handler == null)
            {
                return;
            }

            if (_handlers.ContainsKey(type))
            {
                _handlers[type].Remove(handler);
                if (_handlers[type].Count == 0)
                {
                    _handlers.Remove(type);
                }
            }
        }

        /// <summary>
        /// 注销某个类型的所有处理器
        /// </summary>
        /// <param name="type">消息类型</param>
        public void UnregisterAllHandlers(string type)
        {
            if (_handlers.ContainsKey(type))
            {
                _handlers.Remove(type);
            }
        }

        /// <summary>
        /// 注销所有消息处理器
        /// </summary>
        public void UnregisterAll()
        {
            _handlers.Clear();
        }

        /// <summary>
        /// 分发消息到对应的处理器
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="json">消息JSON数据</param>
        public void DispatchMessage(string type, string json)
        {
            if (string.IsNullOrEmpty(type))
            {
                Debug.LogWarning("[NetworkMessageHandler] DispatchMessage: type为空");
                return;
            }

            if (_handlers.TryGetValue(type, out List<MessageHandler> handlerList))
            {
                // 复制一份列表，防止在回调中修改字典导致迭代异常
                var handlersCopy = new List<MessageHandler>(handlerList);
                foreach (var handler in handlersCopy)
                {
                    try
                    {
                        handler?.Invoke(json);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[NetworkMessageHandler] 处理消息 {type} 时发生异常: {e}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkMessageHandler] 没有注册类型为 {type} 的处理器");
            }
        }

        /// <summary>
        /// 分发消息（从完整JSON字符串中解析type和data）
        /// 消息格式：{ "type": "...", "data": {...} }
        /// 处理器收到的json参数为内层data字段的JSON字符串（便于直接反序列化）
        /// 注意：服务器使用System.Text.Json，data为JSON对象（非字符串），
        /// 这里通过提取子对象来兼容Unity的JsonUtility。
        /// </summary>
        /// <param name="fullJson">完整的消息JSON字符串</param>
        public void DispatchMessage(string fullJson)
        {
            if (string.IsNullOrEmpty(fullJson))
            {
                return;
            }

            try
            {
                // 先用只含type的类解析出消息类型
                string msgType = ExtractJsonStringField(fullJson, "type");
                if (string.IsNullOrEmpty(msgType))
                {
                    Debug.LogWarning("[NetworkMessageHandler] 无法解析消息类型");
                    return;
                }

                // 提取data字段的JSON子串
                string dataJson = ExtractJsonObjectField(fullJson, "data");
                // 如果提取不到data，传递完整JSON
                if (string.IsNullOrEmpty(dataJson))
                {
                    dataJson = fullJson;
                }
                DispatchMessage(msgType, dataJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkMessageHandler] 解析消息JSON失败: {e}");
            }
        }

        /// <summary>
        /// 从JSON字符串中提取指定字符串字段的值。
        /// 仅适用于简单字符串字段（如 "type":"LoginResponse"）。
        /// </summary>
        private static string ExtractJsonStringField(string json, string fieldName)
        {
            // 查找 "fieldName":"value" 模式
            string pattern = "\"" + fieldName + "\"";
            int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            // 找到冒号后的值
            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx < 0) return null;

            // 跳过空白找到引号
            int quoteStart = json.IndexOf('"', colonIdx + 1);
            if (quoteStart < 0) return null;

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) return null;

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        /// <summary>
        /// 从JSON字符串中提取指定对象字段的JSON子串。
        /// 支持 { } 和 [ ] 类型的值，正确处理嵌套和字符串内的括号。
        /// </summary>
        private static string ExtractJsonObjectField(string json, string fieldName)
        {
            string pattern = "\"" + fieldName + "\"";
            int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            // 找到冒号
            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx < 0) return null;

            // 跳过空白找到值起始
            int i = colonIdx + 1;
            while (i < json.Length && (json[i] == ' ' || json[i] == '\t' || json[i] == '\n' || json[i] == '\r'))
                i++;

            if (i >= json.Length) return null;

            char startChar = json[i];
            if (startChar != '{' && startChar != '[')
            {
                // 可能是字符串类型的data，尝试提取字符串
                if (startChar == '"')
                {
                    int end = i + 1;
                    while (end < json.Length)
                    {
                        if (json[end] == '\\') { end += 2; continue; }
                        if (json[end] == '"') break;
                        end++;
                    }
                    if (end < json.Length) return json.Substring(i + 1, end - i - 1);
                }
                return null;
            }

            char closeChar = (startChar == '{') ? '}' : ']';
            int depth = 0;
            bool inString = false;
            bool escape = false;
            int start = i;

            for (; i < json.Length; i++)
            {
                char c = json[i];
                if (escape) { escape = false; continue; }
                if (c == '\\') { escape = true; continue; }
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;
                if (c == startChar) depth++;
                else if (c == closeChar)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return json.Substring(start, i - start + 1);
                    }
                }
            }
            return null;
        }
    }
}
