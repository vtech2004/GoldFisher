using System.Collections.Concurrent;
using FishServer.Messages;
using FishServer.Models;

namespace FishServer.Managers
{
    /// <summary>
    /// 排行榜管理器
    /// 基于所有玩家总分排序，提供Top N排行榜和玩家排名查询
    /// </summary>
    public class LeaderboardManager
    {
        private readonly PlayerManager _playerManager;
        private readonly object _cacheLock = new object();
        private List<PlayerData> _cachedRanking = new List<PlayerData>();
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(5);

        public LeaderboardManager(PlayerManager playerManager)
        {
            _playerManager = playerManager;
        }

        /// <summary>
        /// 获取Top N排行榜
        /// </summary>
        /// <param name="topN">条目数量，<=0 表示返回全部</param>
        /// <returns>排行榜条目列表（已排序，含排名）</returns>
        public List<LeaderboardEntry> GetLeaderboard(int topN = 100)
        {
            var ranking = GetCachedRanking();
            var entries = new List<LeaderboardEntry>();

            int count = topN <= 0 ? ranking.Count : Math.Min(topN, ranking.Count);
            for (int i = 0; i < count; i++)
            {
                var p = ranking[i];
                entries.Add(new LeaderboardEntry
                {
                    rank = i + 1,
                    playerId = p.PlayerId,
                    username = p.Username,
                    totalScore = p.TotalScore,
                });
            }
            return entries;
        }

        /// <summary>
        /// 获取玩家排名
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>排名（从1开始），未上榜或不存在返回0</returns>
        public int GetPlayerRank(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return 0;
            var ranking = GetCachedRanking();
            for (int i = 0; i < ranking.Count; i++)
            {
                if (ranking[i].PlayerId == playerId)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// 强制刷新缓存（在玩家分数更新后调用）
        /// </summary>
        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _lastUpdateTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// 获取缓存的排名列表（按总分降序）
        /// </summary>
        private List<PlayerData> GetCachedRanking()
        {
            lock (_cacheLock)
            {
                if (DateTime.UtcNow - _lastUpdateTime > _cacheTtl || _cachedRanking.Count == 0)
                {
                    _cachedRanking = _playerManager.AllPlayers
                        .OrderByDescending(p => p.TotalScore)
                        .ThenBy(p => p.LastLogin) // 同分按登录时间早的靠前
                        .ToList();
                    _lastUpdateTime = DateTime.UtcNow;
                }
                return _cachedRanking;
            }
        }
    }
}
