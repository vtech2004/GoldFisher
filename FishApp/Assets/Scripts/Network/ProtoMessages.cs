// 网络消息定义文件
// 定义所有客户端与服务器之间传输的消息结构，使用JSON序列化
// 消息格式：{ "type": "...", "data": {...} }，与FishServer.Messages.MessageTypes保持一致

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 消息类型常量定义，客户端与服务器需保持一致
    /// 对应 FishServer.Messages.MessageType
    /// </summary>
    public static class MessageType
    {
        // ===== 请求消息 =====
        public const string LoginRequest = "LoginRequest";
        public const string LevelResultRequest = "LevelResultRequest";
        public const string PlayerDataRequest = "PlayerDataRequest";
        public const string LeaderboardRequest = "LeaderboardRequest";

        // ===== 响应消息 =====
        public const string LoginResponse = "LoginResponse";
        public const string LevelResultResponse = "LevelResultResponse";
        public const string PlayerDataResponse = "PlayerDataResponse";
        public const string LeaderboardResponse = "LeaderboardResponse";
        public const string ErrorResponse = "ErrorResponse";

        // ===== 其他 =====
        public const string Heartbeat = "Heartbeat";
        public const string Disconnect = "Disconnect";
    }

    /// <summary>
    /// 消息包装类（外层）
    /// 格式：{ "type": "...", "data": "..." }
    /// data 字段为内层消息对象的JSON字符串
    /// 注意：Unity的JsonUtility不支持JsonElement，因此data用string存储内层JSON
    /// </summary>
    [Serializable]
    public class MessageEnvelope
    {
        public string type;
        public string data; // 内层消息的JSON字符串

        /// <summary>
        /// 创建消息包装
        /// </summary>
        public static MessageEnvelope Create(string msgType, object dataObj)
        {
            var msg = new MessageEnvelope { type = msgType };
            if (dataObj != null)
            {
                msg.data = JsonUtility.ToJson(dataObj);
            }
            else
            {
                msg.data = "{}";
            }
            return msg;
        }
    }

    /// <summary>
    /// 消息基类（已弃用，保留向后兼容）
    /// </summary>
    [Serializable]
    public class BaseMessage
    {
        public string type;
    }

    // ============ 登录相关消息 ============

    /// <summary>
    /// 登录请求
    /// 对应服务器 LoginRequest
    /// </summary>
    [Serializable]
    public class LoginRequest
    {
        public string username;

        public LoginRequest() { }
        public LoginRequest(string username) { this.username = username; }
    }

    /// <summary>
    /// 登录响应
    /// 对应服务器 LoginResponse
    /// </summary>
    [Serializable]
    public class LoginResponse
    {
        public bool success;
        public string playerId;
        public string message;
    }

    // ============ 关卡结果相关消息 ============

    /// <summary>
    /// 关卡结果上报请求
    /// 对应服务器 LevelResultRequest
    /// </summary>
    [Serializable]
    public class LevelResultRequest
    {
        public string playerId;
        public int levelId;
        public long score;
        public bool success;

        public LevelResultRequest() { }
        public LevelResultRequest(string playerId, int levelId, long score, bool success)
        {
            this.playerId = playerId;
            this.levelId = levelId;
            this.score = score;
            this.success = success;
        }
    }

    /// <summary>
    /// 关卡结果上报响应
    /// 对应服务器 LevelResultResponse
    /// </summary>
    [Serializable]
    public class LevelResultResponse
    {
        public bool success;
        public string reward;
        public string message;
        public bool newRecord;
        public long totalScore;
        public int unlockedLevels;
    }

    // ============ 玩家数据相关消息 ============

    /// <summary>
    /// 获取玩家数据请求
    /// 对应服务器 PlayerDataRequest
    /// </summary>
    [Serializable]
    public class PlayerDataRequest
    {
        public string playerId;

        public PlayerDataRequest() { }
        public PlayerDataRequest(string playerId) { this.playerId = playerId; }
    }

    /// <summary>
    /// 玩家数据响应
    /// 对应服务器 PlayerDataResponse
    /// 注意：levelScores 是 Dictionary，JsonUtility 不直接支持，
    /// 收到时需手动解析，此处仅声明字段（可能为空）
    /// </summary>
    [Serializable]
    public class PlayerDataResponse
    {
        public string playerId;
        public string username;
        public long totalScore;
        public int unlockedLevels;
        public int rank;
        // levelScores: JsonUtility 不支持 Dictionary 序列化，此处忽略
        // 如需读取关卡分数，使用 PlayerDataResponseParser 辅助类
    }

    // ============ 排行榜相关消息 ============

    /// <summary>
    /// 排行榜查询请求
    /// 对应服务器 LeaderboardRequest
    /// </summary>
    [Serializable]
    public class LeaderboardRequest
    {
        public int topN = 100;

        public LeaderboardRequest() { }
        public LeaderboardRequest(int topN) { this.topN = topN; }
    }

    /// <summary>
    /// 排行榜条目
    /// 对应服务器 LeaderboardEntry
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string playerId;
        public string username;
        public long totalScore;
    }

    /// <summary>
    /// 排行榜响应
    /// 对应服务器 LeaderboardResponse
    /// 注意：entries 是列表，JsonUtility 不直接支持顶层列表，
    /// 需要使用包装类 LeaderboardResponseWrapper
    /// </summary>
    [Serializable]
    public class LeaderboardResponse
    {
        // JsonUtility 不支持 List 作为顶层字段直接反序列化为数组
        // 服务器发送格式为 { "entries": [...] }，需用包装类
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    // ============ 错误消息 ============

    /// <summary>
    /// 通用错误响应
    /// 对应服务器 ErrorResponse
    /// </summary>
    [Serializable]
    public class ErrorResponse
    {
        public string message;
        public string requestType;
    }

    // ============ 心跳消息 ============

    /// <summary>
    /// 心跳消息，用于保持连接活跃
    /// </summary>
    [Serializable]
    public class HeartbeatMessage
    {
        public long timestamp;
    }

    // ============ 辅助包装类 ============

    /// <summary>
    /// LeaderboardResponse的包装类
    /// 服务器发送 { "entries": [...] }，此包装类用于JsonUtility反序列化
    /// </summary>
    [Serializable]
    public class LeaderboardResponseWrapper
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    /// <summary>
    /// 消息解析辅助类
    /// 提供从消息包装中提取内层data并反序列化的方法
    /// </summary>
    public static class MessageParser
    {
        /// <summary>
        /// 从完整JSON字符串中解析消息类型
        /// </summary>
        /// <param name="fullJson">完整消息JSON</param>
        /// <param name="envelope">解析出的消息包装</param>
        /// <returns>是否解析成功</returns>
        public static bool TryParseEnvelope(string fullJson, out MessageEnvelope envelope)
        {
            envelope = null;
            if (string.IsNullOrEmpty(fullJson))
            {
                return false;
            }

            try
            {
                envelope = JsonUtility.FromJson<MessageEnvelope>(fullJson);
                return envelope != null && !string.IsNullOrEmpty(envelope.type);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 从消息包装中解析内层数据
        /// </summary>
        /// <typeparam name="T">内层数据类型</typeparam>
        /// <param name="envelope">消息包装</param>
        /// <returns>解析后的数据对象</returns>
        public static T ParseData<T>(MessageEnvelope envelope)
        {
            if (envelope == null || string.IsNullOrEmpty(envelope.data))
            {
                return default;
            }

            try
            {
                return JsonUtility.FromJson<T>(envelope.data);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[MessageParser] 解析内层数据失败: {e}");
                return default;
            }
        }

        /// <summary>
        /// 从完整JSON字符串中解析消息类型和数据
        /// </summary>
        /// <typeparam name="T">内层数据类型</typeparam>
        /// <param name="fullJson">完整消息JSON</param>
        /// <param name="msgType">输出消息类型</param>
        /// <param name="data">输出内层数据</param>
        /// <returns>是否解析成功</returns>
        public static bool TryParse<T>(string fullJson, out string msgType, out T data)
        {
            msgType = null;
            data = default;

            if (!TryParseEnvelope(fullJson, out MessageEnvelope envelope))
            {
                return false;
            }

            msgType = envelope.type;
            data = ParseData<T>(envelope);
            return true;
        }
    }
}
