// LevelData.cs
// 关卡数据配置类（ScriptableObject）：单关卡的配置数据。
// 包含关卡ID、名称、目标金额、时间限制、物品生成配置列表。

using UnityEngine;
using System.Collections.Generic;

namespace FishGame
{
    /// <summary>
    /// 关卡内单个物品的生成配置。
    /// </summary>
    [System.Serializable]
    public class LevelItemEntry
    {
        [Tooltip("物品类型")]
        public ItemType itemType = ItemType.SmallFish;

        [Tooltip("生成数量")]
        [Min(0)] public int count = 5;

        [Tooltip("价值范围（随机）")]
        public Vector2Int valueRange = new Vector2Int(10, 50);

        [Tooltip("重量范围（随机，影响收回速度）")]
        public Vector2 weightRange = new Vector2(0.5f, 1.5f);
    }

    /// <summary>
    /// 关卡数据ScriptableObject。
    /// 创建菜单：Assets -> Create -> FishGame -> LevelData
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "FishGame/LevelData", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("关卡基本信息")]
        [Tooltip("关卡ID")]
        public int levelId = 1;

        [Tooltip("关卡名称")]
        public string levelName = "第1关";

        [Tooltip("目标金额（达到此金额过关）")]
        public int targetMoney = 500;

        [Tooltip("时间限制（秒）")]
        public float timeLimit = 60f;

        [Header("物品配置")]
        [Tooltip("本关卡物品生成配置列表")]
        public List<LevelItemEntry> itemEntries = new List<LevelItemEntry>();

        [Header("奖励")]
        [Tooltip("过关奖励金币")]
        public int clearBonus = 100;

        /// <summary>验证关卡数据合法性</summary>
        public bool Validate()
        {
            return levelId > 0 && targetMoney > 0 && timeLimit > 0;
        }
    }
}
