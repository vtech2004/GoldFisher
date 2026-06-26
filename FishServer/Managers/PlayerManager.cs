using System.Collections.Concurrent;
using System.Threading;
using FishServer.Config;
using FishServer.Models;
using FishServer.Storage;

namespace FishServer.Managers
{
    /// <summary>
    /// 玩家管理器
    /// 负责玩家数据的存储、登录、查询、关卡结果更新等
    /// 使用内存字典 + 文件持久化
    /// </summary>
    public class PlayerManager
    {
        private readonly ConcurrentDictionary<string, PlayerData> _players;
        private readonly FileStorage _storage;
        private readonly ServerConfig _config;
        private int _idCounter = 0;

        public PlayerManager(ServerConfig config, FileStorage storage)
        {
            _config = config;
            _storage = storage;
            _players = _storage.Load();

            // 初始化ID计数器（基于已存在玩家ID的最大值）
            int maxId = 0;
            foreach (var pid in _players.Keys)
            {
                if (int.TryParse(pid, out int n) && n > maxId)
                {
                    maxId = n;
                }
            }
            _idCounter = maxId;
            Console.WriteLine($"[PlayerManager] 初始化完成，当前玩家数: {_players.Count}, ID计数器: {_idCounter}");
        }

        /// <summary>
        /// 所有玩家数据（只读视图，供排行榜使用）
        /// </summary>
        public IReadOnlyCollection<PlayerData> AllPlayers => _players.Values.ToList().AsReadOnly();

        /// <summary>
        /// 内部玩家数据字典引用（供FileStorage自动保存使用）
        /// </summary>
        internal ConcurrentDictionary<string, PlayerData> PlayersInternal => _players;

        /// <summary>
        /// 玩家总数
        /// </summary>
        public int Count => _players.Count;

        /// <summary>
        /// 登录或创建玩家
        /// 如果用户名已存在则返回已有玩家，否则创建新玩家
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>玩家数据</returns>
        public PlayerData LoginOrCreate(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("用户名不能为空", nameof(username));
            }

            username = username.Trim();

            // 先按用户名查找已存在玩家
            var existing = _players.Values.FirstOrDefault(p => p.Username == username);
            if (existing != null)
            {
                existing.UpdateLastLogin();
                Console.WriteLine($"[PlayerManager] 玩家登录: {username} (ID: {existing.PlayerId})");
                return existing;
            }

            // 创建新玩家
            int newId = Interlocked.Increment(ref _idCounter);
            var player = new PlayerData
            {
                PlayerId = newId.ToString(),
                Username = username,
                TotalScore = 0,
                UnlockedLevels = 1, // 默认解锁第1关
                LevelScores = new Dictionary<int, long>(),
                LastLogin = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow,
            };

            _players[player.PlayerId] = player;
            Console.WriteLine($"[PlayerManager] 创建新玩家: {username} (ID: {player.PlayerId})");
            return player;
        }

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>玩家数据，不存在则返回null</returns>
        public PlayerData? GetPlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;
            _players.TryGetValue(playerId, out var player);
            return player;
        }

        /// <summary>
        /// 上报关卡结果
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="levelId">关卡ID</param>
        /// <param name="score">本次得分</param>
        /// <param name="success">是否通关</param>
        /// <returns>处理结果（包含是否刷新记录、是否解锁新关卡、新总分等）</returns>
        public LevelResult ProcessLevelResult(string playerId, int levelId, long score, bool success)
        {
            var result = new LevelResult();
            var player = GetPlayer(playerId);
            if (player == null)
            {
                result.Success = false;
                result.Message = "玩家不存在";
                return result;
            }

            if (levelId <= 0)
            {
                result.Success = false;
                result.Message = "关卡ID无效";
                return result;
            }

            // 检查关卡是否已解锁
            if (levelId > player.UnlockedLevels)
            {
                result.Success = false;
                result.Message = $"关卡 {levelId} 尚未解锁，当前已解锁到第 {player.UnlockedLevels} 关";
                return result;
            }

            // 只有通关成功才记录分数
            if (success)
            {
                bool newRecord = player.AddLevelScore(levelId, score);
                result.NewRecord = newRecord;

                // 解锁下一关
                int nextLevel = player.UnlockedLevels + 1;
                if (player.UnlockLevel(nextLevel))
                {
                    result.UnlockedNewLevel = true;
                    result.Reward = $"解锁第 {nextLevel} 关！";
                }
            }
            else
            {
                result.Message = "关卡未通关，分数未记录";
            }

            result.Success = true;
            result.TotalScore = player.TotalScore;
            result.UnlockedLevels = player.UnlockedLevels;
            if (string.IsNullOrEmpty(result.Message))
            {
                result.Message = result.NewRecord ? "刷新最高分！" : "关卡分数已记录";
            }

            Console.WriteLine($"[PlayerManager] 玩家 {player.Username} 上报关卡 {levelId}，得分 {score}，通关 {success}，总分 {player.TotalScore}");
            return result;
        }

        /// <summary>
        /// 保存所有数据到文件
        /// </summary>
        public void Save()
        {
            _storage.Save(_players);
        }
    }

    /// <summary>
    /// 关卡结果处理返回值
    /// </summary>
    public class LevelResult
    {
        public bool Success { get; set; }
        public bool NewRecord { get; set; }
        public bool UnlockedNewLevel { get; set; }
        public long TotalScore { get; set; }
        public int UnlockedLevels { get; set; }
        public string Reward { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
