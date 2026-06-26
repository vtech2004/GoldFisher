using System.Text.Json;
using FishServer.Managers;
using FishServer.Messages;
using FishServer.Models;

namespace FishServer.Handlers
{
    /// <summary>
    /// 消息处理器接口
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="data">消息data字段的JSON元素</param>
        /// <returns>响应消息包装（包含响应类型和响应数据）</returns>
        BaseMessage Handle(JsonElement data);
    }

    /// <summary>
    /// 登录处理器
    /// </summary>
    public class LoginHandler : IMessageHandler
    {
        private readonly PlayerManager _playerManager;

        public LoginHandler(PlayerManager playerManager)
        {
            _playerManager = playerManager;
        }

        public BaseMessage Handle(JsonElement data)
        {
            try
            {
                var req = data.Deserialize<LoginRequest>();
                if (req == null || string.IsNullOrWhiteSpace(req.username))
                {
                    return BaseMessage.Create(MessageType.LoginResponse, new LoginResponse
                    {
                        success = false,
                        message = "用户名不能为空"
                    });
                }

                var player = _playerManager.LoginOrCreate(req.username);
                return BaseMessage.Create(MessageType.LoginResponse, new LoginResponse
                {
                    success = true,
                    playerId = player.PlayerId,
                    message = $"欢迎，{player.Username}！"
                });
            }
            catch (Exception ex)
            {
                return BaseMessage.Create(MessageType.LoginResponse, new LoginResponse
                {
                    success = false,
                    message = $"登录失败: {ex.Message}"
                });
            }
        }
    }

    /// <summary>
    /// 关卡结果处理器
    /// </summary>
    public class LevelResultHandler : IMessageHandler
    {
        private readonly PlayerManager _playerManager;
        private readonly LeaderboardManager _leaderboardManager;

        public LevelResultHandler(PlayerManager playerManager, LeaderboardManager leaderboardManager)
        {
            _playerManager = playerManager;
            _leaderboardManager = leaderboardManager;
        }

        public BaseMessage Handle(JsonElement data)
        {
            try
            {
                var req = data.Deserialize<LevelResultRequest>();
                if (req == null || string.IsNullOrEmpty(req.playerId))
                {
                    return BaseMessage.Create(MessageType.LevelResultResponse, new LevelResultResponse
                    {
                        success = false,
                        message = "请求参数无效"
                    });
                }

                var result = _playerManager.ProcessLevelResult(req.playerId, req.levelId, req.score, req.success);

                // 分数可能变化，刷新排行榜缓存
                if (result.Success && result.NewRecord)
                {
                    _leaderboardManager.InvalidateCache();
                }

                return BaseMessage.Create(MessageType.LevelResultResponse, new LevelResultResponse
                {
                    success = result.Success,
                    reward = result.Reward,
                    message = result.Message,
                    newRecord = result.NewRecord,
                    totalScore = result.TotalScore,
                    unlockedLevels = result.UnlockedLevels,
                });
            }
            catch (Exception ex)
            {
                return BaseMessage.Create(MessageType.LevelResultResponse, new LevelResultResponse
                {
                    success = false,
                    message = $"处理关卡结果失败: {ex.Message}"
                });
            }
        }
    }

    /// <summary>
    /// 玩家数据查询处理器
    /// </summary>
    public class PlayerDataHandler : IMessageHandler
    {
        private readonly PlayerManager _playerManager;
        private readonly LeaderboardManager _leaderboardManager;

        public PlayerDataHandler(PlayerManager playerManager, LeaderboardManager leaderboardManager)
        {
            _playerManager = playerManager;
            _leaderboardManager = leaderboardManager;
        }

        public BaseMessage Handle(JsonElement data)
        {
            try
            {
                var req = data.Deserialize<PlayerDataRequest>();
                if (req == null || string.IsNullOrEmpty(req.playerId))
                {
                    return BaseMessage.Create(MessageType.ErrorResponse, new ErrorResponse
                    {
                        message = "请求参数无效：playerId为空",
                        requestType = MessageType.PlayerDataRequest,
                    });
                }

                var player = _playerManager.GetPlayer(req.playerId);
                if (player == null)
                {
                    return BaseMessage.Create(MessageType.ErrorResponse, new ErrorResponse
                    {
                        message = "玩家不存在",
                        requestType = MessageType.PlayerDataRequest,
                    });
                }

                int rank = _leaderboardManager.GetPlayerRank(player.PlayerId);
                return BaseMessage.Create(MessageType.PlayerDataResponse, new PlayerDataResponse
                {
                    playerId = player.PlayerId,
                    username = player.Username,
                    totalScore = player.TotalScore,
                    unlockedLevels = player.UnlockedLevels,
                    rank = rank,
                    levelScores = player.LevelScores,
                });
            }
            catch (Exception ex)
            {
                return BaseMessage.Create(MessageType.ErrorResponse, new ErrorResponse
                {
                    message = $"查询玩家数据失败: {ex.Message}",
                    requestType = MessageType.PlayerDataRequest,
                });
            }
        }
    }

    /// <summary>
    /// 排行榜查询处理器
    /// </summary>
    public class LeaderboardHandler : IMessageHandler
    {
        private readonly LeaderboardManager _leaderboardManager;

        public LeaderboardHandler(LeaderboardManager leaderboardManager)
        {
            _leaderboardManager = leaderboardManager;
        }

        public BaseMessage Handle(JsonElement data)
        {
            try
            {
                int topN = 100;
                // data 可能为空对象，尝试读取 topN
                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("topN", out var topNProp))
                {
                    if (topNProp.TryGetInt32(out int n))
                    {
                        topN = n;
                    }
                }

                var entries = _leaderboardManager.GetLeaderboard(topN);
                return BaseMessage.Create(MessageType.LeaderboardResponse, new LeaderboardResponse
                {
                    entries = entries,
                });
            }
            catch (Exception ex)
            {
                return BaseMessage.Create(MessageType.ErrorResponse, new ErrorResponse
                {
                    message = $"查询排行榜失败: {ex.Message}",
                    requestType = MessageType.LeaderboardRequest,
                });
            }
        }
    }
}
