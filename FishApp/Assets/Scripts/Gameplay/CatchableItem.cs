// CatchableItem.cs
// 可捕获物品基类：挂载在水中物品Prefab上，记录物品属性并提供被抓/被释放回调。
// 子类可重写OnCaught/OnReleased实现特殊行为（如水母减速、炸弹释放重物）。

using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 可捕获物品基类。挂载到物品Prefab上，描述物品的价值、重量、类型等属性。
    /// </summary>
    public class CatchableItem : MonoBehaviour
    {
        [Header("物品基本配置")]
        [Tooltip("物品类型")]
        [SerializeField] private ItemType itemType = ItemType.SmallFish;

        [Tooltip("物品名称（用于UI显示）")]
        [SerializeField] private string itemName = "Fish";

        [Tooltip("基础价值（金币）")]
        [SerializeField] private int baseValue = 10;

        [Tooltip("重量因子（影响钩爪收回速度，越大越慢），建议0.1~5.0")]
        [SerializeField] private float weight = 1.0f;

        [Tooltip("稀有度")]
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;

        [Tooltip("物品Sprite资源路径（可选，用于动态加载）")]
        [SerializeField] private string spritePath = "";

        // Sprite 通常由 ItemSpawner 通过 SetSprite 在运行时注入，无需 Inspector 拖拽
        private Sprite itemSprite;

        [Header("状态")]
        [Tooltip("是否已被抓取")]
        [SerializeField] private bool isCaught = false;

        /// <summary>物品类型</summary>
        public ItemType ItemType => itemType;

        /// <summary>物品名称</summary>
        public string ItemName => itemName;

        /// <summary>基础价值</summary>
        public int BaseValue => baseValue;

        /// <summary>重量因子（越大收回越慢）</summary>
        public float Weight => weight;

        /// <summary>稀有度</summary>
        public ItemRarity Rarity => rarity;

        /// <summary>Sprite资源路径</summary>
        public string SpritePath => spritePath;

        /// <summary>物品Sprite</summary>
        public Sprite ItemSprite => itemSprite;

        /// <summary>是否已被抓取</summary>
        public bool IsCaught => isCaught;

        /// <summary>
        /// 被钩爪抓住时调用。子类可重写以实现特殊效果（如水母电击减速、炸弹抓取提示）。
        /// </summary>
        public virtual void OnCaught()
        {
            if (isCaught) return;
            isCaught = true;
            // 抓取后禁用自身碰撞体，防止重复抓取
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            // 可在此处播放被抓音效/动画
        }

        /// <summary>
        /// 被释放时调用（玩家使用炸弹丢弃重物）。子类可重写。
        /// </summary>
        public virtual void OnReleased()
        {
            isCaught = false;
            // 释放后可重新启用碰撞体（如需返回水中）
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }

        /// <summary>
        /// 根据物品类型快速配置默认属性。可在生成时调用。
        /// </summary>
        public void ApplyDefaultConfig(ItemType type)
        {
            itemType = type;
            var cfg = ItemConfigDatabase.GetConfig(type);
            itemName = cfg.itemName;
            baseValue = cfg.baseValue;
            weight = cfg.weight;
            rarity = cfg.rarity;
        }

        /// <summary>
        /// 运行时设置物品的价值和重量（由ItemSpawner在生成时调用）。
        /// </summary>
        /// <param name="value">基础价值</param>
        /// <param name="w">重量因子</param>
        public void SetRuntimeStats(int value, float w)
        {
            baseValue = value;
            weight = w;
        }

        /// <summary>
        /// 运行时设置物品 Sprite（由 ItemSpawner 在生成时调用，从 Resources 加载）。
        /// </summary>
        /// <param name="sprite">要应用的 Sprite</param>
        public void SetSprite(Sprite sprite)
        {
            itemSprite = sprite;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sprite != null)
                sr.sprite = sprite;
        }

        private void Reset()
        {
            // Inspector重置时自动配置默认值
            ApplyDefaultConfig(itemType);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Inspector 中改变 itemType 时自动填充 spritePath（从 Resources/Art/Items 名称推导）
            if (!string.IsNullOrEmpty(spritePath)) return;
            spritePath = ArtResourcePath.GetItemPath(itemType);
        }
#endif
    }

    /// <summary>
    /// 物品默认配置数据结构。
    /// </summary>
    [System.Serializable]
    public struct ItemConfig
    {
        public string itemName;
        public int baseValue;
        public float weight;
        public ItemRarity rarity;
    }

    /// <summary>
    /// 物品默认配置数据库（静态）。
    /// 提供各物品类型的默认价值、重量、稀有度。
    /// </summary>
    public static class ItemConfigDatabase
    {
        private static readonly System.Collections.Generic.Dictionary<ItemType, ItemConfig> configs =
            new System.Collections.Generic.Dictionary<ItemType, ItemConfig>
        {
            { ItemType.SmallFish,    new ItemConfig{ itemName="小鱼",    baseValue=20,   weight=0.5f,  rarity=ItemRarity.Common } },
            { ItemType.MediumFish,   new ItemConfig{ itemName="中鱼",    baseValue=60,   weight=1.2f,  rarity=ItemRarity.Uncommon } },
            { ItemType.BigFish,      new ItemConfig{ itemName="大鱼",    baseValue=150,  weight=2.5f,  rarity=ItemRarity.Rare } },
            { ItemType.Shark,        new ItemConfig{ itemName="鲨鱼",    baseValue=400,  weight=4.5f,  rarity=ItemRarity.Epic } },
            { ItemType.Crab,         new ItemConfig{ itemName="螃蟹",    baseValue=80,   weight=1.0f,  rarity=ItemRarity.Uncommon } },
            { ItemType.Jellyfish,    new ItemConfig{ itemName="水母",    baseValue=100,  weight=1.5f,  rarity=ItemRarity.Uncommon } },
            { ItemType.Pearl,        new ItemConfig{ itemName="珍珠",    baseValue=600,  weight=0.2f,  rarity=ItemRarity.Epic } },
            { ItemType.TreasureChest,new ItemConfig{ itemName="宝箱",    baseValue=500,  weight=3.5f,  rarity=ItemRarity.Rare } },
            { ItemType.GoldNugget,   new ItemConfig{ itemName="金块",    baseValue=300,  weight=3.0f,  rarity=ItemRarity.Rare } },
            { ItemType.Diamond,      new ItemConfig{ itemName="钻石",    baseValue=800,  weight=0.3f,  rarity=ItemRarity.Legendary } },
            { ItemType.Bomb,         new ItemConfig{ itemName="炸弹",    baseValue=0,    weight=1.0f,  rarity=ItemRarity.Special } },
            { ItemType.Dynamite,     new ItemConfig{ itemName="炸药",    baseValue=0,    weight=1.0f,  rarity=ItemRarity.Special } },
            { ItemType.Trash,        new ItemConfig{ itemName="垃圾",    baseValue=5,    weight=2.5f,  rarity=ItemRarity.Common } },
            { ItemType.Boot,         new ItemConfig{ itemName="旧靴子",  baseValue=2,    weight=1.5f,  rarity=ItemRarity.Common } },
            { ItemType.TinCan,       new ItemConfig{ itemName="易拉罐",  baseValue=3,    weight=0.8f,  rarity=ItemRarity.Common } },
            { ItemType.Seaweed,      new ItemConfig{ itemName="海草",    baseValue=1,    weight=0.6f,  rarity=ItemRarity.Common } },
            { ItemType.MysteryBox,   new ItemConfig{ itemName="神秘宝箱",baseValue=200,  weight=2.0f,  rarity=ItemRarity.Legendary } },
            { ItemType.None,         new ItemConfig{ itemName="无",      baseValue=0,    weight=0f,    rarity=ItemRarity.Common } },
        };

        public static ItemConfig GetConfig(ItemType type)
        {
            if (configs.TryGetValue(type, out var c)) return c;
            return new ItemConfig { itemName="未知", baseValue=0, weight=1f, rarity=ItemRarity.Common };
        }
    }
}
