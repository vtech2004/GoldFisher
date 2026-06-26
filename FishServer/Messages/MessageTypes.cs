using System.Text.Json;
using System.Text.Json.Serialization;

namespace FishServer.Messages
{
    /// <summary>
    /// 消息类型常量
    /// 客户端与服务器通过 type 字段区分消息类型
    /// </summary>
    public static class MessageType
    {
        // ===== 请求消息 =====
        /// <summary>登录请求</summary>
        public const string LoginRequest = "LoginRequest";

        /// <summary>关卡结果上报请求</summary>
        public const string LevelResultRequest = "LevelResultRequest";

        /// <summary>玩家数据查询请求</summary>
        public const string PlayerDataRequest = "PlayerDataRequest";

        /// <summary>排行榜查询请求</summary>
        public const string LeaderboardRequest = "LeaderboardRequest";

        // ===== 响应消息 =====
        /// <summary>登录响应</summary>
        public const string LoginResponse = "LoginResponse";

        /// <summary>关卡结果响应</summary>
        public const string LevelResultResponse = "LevelResultResponse";

        /// <summary>玩家数据响应</summary>
        public const string PlayerDataResponse = "PlayerDataResponse";

        /// <summary>排行榜响应</summary>
        public const string LeaderboardResponse = "LeaderboardResponse";

        /// <summary>通用错误响应</summary>
        public const string ErrorResponse = "ErrorResponse";
    }

    /// <summary>
    /// 基础消息包装类
    /// 所有网络传输的消息都使用此格式：{ "type": "...", "data": {...} }
    /// </summary>
    [Serializable]
    public class BaseMessage
    {
        /// <summary>消息类型</summary>
        public string type { get; set; } = string.Empty;

        /// <summary>消息数据（具体消息对象的JSON）</summary>
        public JsonElement data { get; set; }

        /// <summary>
        /// 创建一个带类型的消息包装
        /// </summary>
        public static BaseMessage Create(string msgType, object? dataObj)
        {
            var msg = new BaseMessage { type = msgType };
            if (dataObj != null)
            {
                msg.data = JsonSerializer.SerializeToElement(dataObj);
            }
            return msg;
        }
    }

    // ===================== 请求消息 =====================

    /// <summary>
    /// 登录请求
    /// </summary>
    [Serializable]
    public class LoginRequest
    {
        /// <summary>用户名</summary>
        public string username { get; set; } = string.Empty;
    }

    /// <summary>
    /// 关卡结果上报请求
    /// </summary>
    [Serializable]
    public class LevelResultRequest
    {
        /// <summary>玩家ID</summary>
        public string playerId { get; set; } = string.Empty;

        /// <summary>关卡ID</summary>
        public int levelId { get; set; }

        /// <summary>本次得分</summary>
        public long score { get; set; }

        /// <summary>是否通关成功</summary>
        public bool success { get; set; }
    }

    /// <summary>
    /// 玩家数据查询请求
    /// </summary>
    [Serializable]
    public class PlayerDataRequest
    {
        /// <summary>玩家ID</summary>
        public string playerId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 排行榜查询请求
    /// </summary>
    [Serializable]
    public class LeaderboardRequest
    {
        /// <summary>请求的排行榜条目数量，0或负数表示默认（如Top 100）</summary>
        public int topN { get; set; } = 100;
    }

    // ===================== 响应消息 =====================

    /// <summary>
    /// 登录响应
    /// </summary>
    [Serializable]
    public class LoginResponse
    {
        /// <summary>是否成功</summary>
        public bool success { get; set; }

        /// <summary>玩家ID（成功时返回）</summary>
        public string playerId { get; set; } = string.Empty;

        /// <summary>提示消息</summary>
        public string message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 关卡结果响应
    /// </summary>
    [Serializable]
    public class LevelResultResponse
    {
        /// <summary>是否成功处理</summary>
        public bool success { get; set; }

        /// <summary>奖励信息（如解锁新关卡等）</summary>
        public string reward { get; set; } = string.Empty;

        /// <summary>提示消息</summary>
        public string message { get; set; } = string.Empty;

        /// <summary>是否刷新了最高分</summary>
        public bool newRecord { get; set; }

        /// <summary>当前总分</summary>
        public long totalScore { get; set; }

        /// <summary>已解锁关卡数</summary>
        public int unlockedLevels { get; set; }
    }

    /// <summary>
    /// 玩家数据响应
    /// </summary>
    [Serializable]
    public class PlayerDataResponse
    {
        /// <summary>玩家ID</summary>
        public string playerId { get; set; } = string.Empty;

        /// <summary>用户名</summary>
        public string username { get; set; } = string.Empty;

        /// <summary>总分</summary>
        public long totalScore { get; set; }

        /// <summary>已解锁关卡数</summary>
        public int unlockedLevels { get; set; }

        /// <summary>排名（0表示未上榜）</summary>
        public int rank { get; set; }

        /// <summary>各关卡最高分</summary>
        public Dictionary<int, long> levelScores { get; set; } = new Dictionary<int, long>();
    }

    /// <summary>
    /// 排行榜条目
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        /// <summary>排名</summary>
        public int rank { get; set; }

        /// <summary>玩家ID</summary>
        public string playerId { get; set; } = string.Empty;

        /// <summary>用户名</summary>
        public string username { get; set; } = string.Empty;

        /// <summary>总分</summary>
        public long totalScore { get; set; }
    }

    /// <summary>
    /// 排行榜响应
    /// </summary>
    [Serializable]
    public class LeaderboardResponse
    {
        /// <summary>排行榜条目列表</summary>
        public List<LeaderboardEntry> entries { get; set; } = new List<LeaderboardEntry>();
    }

    /// <summary>
    /// 通用错误响应
    /// </summary>
    [Serializable]
    public class ErrorResponse
    {
        /// <summary>错误消息</summary>
        public string message { get; set; } = string.Empty;

        /// <summary>原始请求类型</summary>
        public string requestType { get; set; } = string.Empty;
    }
}
