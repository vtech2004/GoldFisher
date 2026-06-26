// LevelConfig.cs
// 所有关卡的配置容器（ScriptableObject）。
// 作为整体关卡进度资源，由GameManager读取以推进关卡。

using UnityEngine;
using System.Collections.Generic;

namespace FishGame
{
    /// <summary>
    /// 全部关卡配置容器ScriptableObject。
    /// 创建菜单：Assets -> Create -> FishGame -> LevelConfig
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "FishGame/LevelConfig", order = 2)]
    public class LevelConfig : ScriptableObject
    {
        [Tooltip("所有关卡数据列表（按levelId顺序）")]
        public List<LevelData> levels = new List<LevelData>();

        /// <summary>关卡总数</summary>
        public int LevelCount => levels != null ? levels.Count : 0;

        /// <summary>
        /// 根据关卡ID获取关卡数据。
        /// </summary>
        public LevelData GetLevel(int levelId)
        {
            if (levels == null) return null;
            foreach (var lv in levels)
            {
                if (lv != null && lv.levelId == levelId) return lv;
            }
            return null;
        }

        /// <summary>
        /// 根据索引获取关卡数据（0-based）。
        /// </summary>
        public LevelData GetLevelByIndex(int index)
        {
            if (levels == null || index < 0 || index >= levels.Count) return null;
            return levels[index];
        }

        /// <summary>是否存在指定ID的关卡</summary>
        public bool HasLevel(int levelId)
        {
            return GetLevel(levelId) != null;
        }
    }
}
