// ItemType.cs
// 可捕获物品类型与稀有度枚举定义。
// 黄金矿工换皮钓鱼游戏：定义水中可被钩爪抓取的所有物品类型。

namespace FishGame
{
    /// <summary>
    /// 可捕获物品类型枚举。
    /// 涵盖各种鱼、宝物、垃圾与道具。
    /// </summary>
    public enum ItemType
    {
        // —— 鱼类 ——
        SmallFish,      // 小鱼：轻、价值低
        MediumFish,     // 中型鱼：中等
        BigFish,        // 大鱼：重、价值较高
        Shark,          // 鲨鱼：很重、价值高
        Crab,           // 螃蟹：中等重量、中等价值
        Jellyfish,      // 水母：中等、价值中等，可能带有减速效果

        // —— 宝物 ——
        Pearl,          // 珍珠：很小但价值极高
        TreasureChest,  // 宝箱：重、价值很高
        GoldNugget,     // 金块：重、价值高
        Diamond,        // 钻石：极小、价值极高

        // —— 道具 ——
        Bomb,           // 炸弹/TNT道具：抓到后可用于释放已抓到的重物
        Dynamite,       // 炸药：同炸弹类道具

        // —— 垃圾 ——
        Trash,          // 垃圾：重但价值极低
        Boot,           // 旧靴子：中等重量、无价值
        TinCan,         // 易拉罐：轻、价值极低
        Seaweed,        // 海草：轻、价值低

        // —— 特殊 ——
        MysteryBox,     // 神秘宝箱：随机价值
        None            // 空/无效
    }

    /// <summary>
    /// 物品稀有度枚举。
    /// 用于影响生成概率与视觉表现。
    /// </summary>
    public enum ItemRarity
    {
        Common,         // 普通：小鱼、垃圾
        Uncommon,       // 较少见：中型鱼、螃蟹
        Rare,           // 稀有：大鱼、宝箱
        Epic,           // 史诗：鲨鱼、珍珠
        Legendary,      // 传说：钻石、神秘宝箱
        Special         // 特殊：炸弹等道具
    }
}
