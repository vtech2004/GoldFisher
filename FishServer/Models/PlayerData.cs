using System.Text.Json.Serialization;

namespace FishServer.Models
{
    /// <summary>
    /// 玩家数据模型
    /// 存储玩家的所有游戏数据，包括总分、解锁关卡、各关卡最高分等
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        /// <summary>
        /// 玩家唯一ID
        /// </summary>
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>
        /// 玩家用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 玩家总分（所有关卡最高分之和）
        /// </summary>
        public long TotalScore { get; set; } = 0;

        /// <summary>
        /// 已解锁的关卡数量（从1开始，1表示已解锁第1关）
        /// </summary>
        public int UnlockedLevels { get; set; } = 1;

        /// <summary>
        /// 各关卡最高分字典：关卡ID -> 最高分
        /// </summary>
        public Dictionary<int, long> LevelScores { get; set; } = new Dictionary<int, long>();

        /// <summary>
        /// 最后登录时间（UTC）
        /// </summary>
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建时间（UTC）
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 添加/更新关卡分数
        /// 如果新分数高于历史最高分，则更新并累加总分
        /// </summary>
        /// <param name="levelId">关卡ID</param>
        /// <param name="score">本次得分</param>
        /// <returns>是否刷新了最高分</returns>
        public bool AddLevelScore(int levelId, long score)
        {
            if (levelId <= 0) return false;
            if (score < 0) score = 0;

            bool isNewHigh = false;
            if (LevelScores.TryGetValue(levelId, out long prevHigh))
            {
                if (score > prevHigh)
                {
                    // 刷新最高分：总分加上差值
                    TotalScore += (score - prevHigh);
                    LevelScores[levelId] = score;
                    isNewHigh = true;
                }
            }
            else
            {
                // 首次通关该关卡
                LevelScores[levelId] = score;
                TotalScore += score;
                isNewHigh = true;
            }
            return isNewHigh;
        }

        /// <summary>
        /// 判断是否可以解锁指定关卡
        /// 玩家已解锁关卡数为 UnlockedLevels，下一关为 UnlockedLevels + 1
        /// </summary>
        /// <param name="levelId">要解锁的关卡ID</param>
        /// <returns>是否可解锁</returns>
        public bool CanUnlockLevel(int levelId)
        {
            // 只能解锁"下一关"
            return levelId == UnlockedLevels + 1;
        }

        /// <summary>
        /// 解锁下一关
        /// </summary>
        /// <param name="levelId">要解锁的关卡ID</param>
        /// <returns>是否成功解锁</returns>
        public bool UnlockLevel(int levelId)
        {
            if (!CanUnlockLevel(levelId)) return false;
            UnlockedLevels = levelId;
            return true;
        }

        /// <summary>
        /// 更新最后登录时间
        /// </summary>
        public void UpdateLastLogin()
        {
            LastLogin = DateTime.UtcNow;
        }
    }
}
