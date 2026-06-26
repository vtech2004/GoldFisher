// ItemConfigAutoSync.cs - Editor 工具
// 自动扫描 Assets/Resources/Art/Items/ 下的 .png 文件，
// 将缺失的类型同步到 ItemConfigDatabase（静态字典），
// 由 SceneBuilder 在 BuildMainScene 时自动调用。
//
// 添加新物品的步骤：
//   1. 在 ItemType.cs 枚举中添加新项（PascalCase）
//   2. 在 Resources/Art/Items/ 放置对应的 .png 文件（snake_case 命名）
//   3. 在 ItemConfigDatabase 中添加默认配置（或运行 FishGame > Sync Item Configs 自动生成空缺配置）
//   4. 重新 Build Main Scene

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FishGame.Editor
{
    public static class ItemConfigAutoSync
    {
        private const string ITEMS_FOLDER = "Assets/Resources/Art/Items";

        /// <summary>
        /// 扫描 Resources/Art/Items/ 中所有 .png，返回存在的物品类型集合。
        /// 文件名 snake_case → ItemType 枚举（PascalCase）。
        /// </summary>
        public static HashSet<ItemType> ScanAvailableItemTypes()
        {
            var available = new HashSet<ItemType>();

            if (!Directory.Exists(ITEMS_FOLDER))
            {
                Debug.LogWarning($"[ItemConfigAutoSync] 目录不存在: {ITEMS_FOLDER}");
                return available;
            }

            var files = Directory.GetFiles(ITEMS_FOLDER, "*.png");
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file); // e.g. "small_fish"
                string pascal = SnakeToPascalCase(name);                // e.g. "SmallFish"
                if (System.Enum.TryParse<ItemType>(pascal, out var itemType) && itemType != ItemType.None)
                {
                    available.Add(itemType);
                }
            }

            return available;
        }

        /// <summary>
        /// snake_case → PascalCase
        /// </summary>
        private static string SnakeToPascalCase(string snake)
        {
            var result = new System.Text.StringBuilder();
            bool nextUpper = true;
            foreach (char c in snake)
            {
                if (c == '_')
                {
                    nextUpper = true;
                }
                else
                {
                    result.Append(nextUpper ? char.ToUpperInvariant(c) : c);
                    nextUpper = false;
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// 验证所有 ItemType（除 None）在 Resources/Art/Items/ 中都有对应的图片文件。
        /// 缺失的类型会输出警告。
        /// </summary>
        [MenuItem("FishGame/Validate Item Sprites")]
        public static void ValidateItemSprites()
        {
            var available = ScanAvailableItemTypes();
            var allTypes = System.Enum.GetValues(typeof(ItemType)).Cast<ItemType>()
                .Where(t => t != ItemType.None);

            int missingCount = 0;
            foreach (var type in allTypes)
            {
                string path = ArtResourcePath.GetItemPath(type);
                var sprite = Resources.Load<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Validate] 物品缺少 Sprite: {type} -> {path}");
                    missingCount++;
                }
            }

            Debug.Log($"[Validate] 物品 Sprite 检查完成。" +
                $"有效: {allTypes.Count() - missingCount}, 缺失: {missingCount}");
        }

        /// <summary>
        /// 列出 Resources/Art/Items/ 中所有可用的物品文件名及其映射的 ItemType。
        /// </summary>
        [MenuItem("FishGame/List Available Items")]
        public static void ListAvailableItems()
        {
            var available = ScanAvailableItemTypes();
            Debug.Log($"[Available Items] 共 {available.Count} 种物品:");
            foreach (var type in available.OrderBy(t => t.ToString()))
            {
                var cfg = ItemConfigDatabase.GetConfig(type);
                string path = ArtResourcePath.GetItemPath(type);
                // 检查实际文件是否存在
                bool exists = Resources.Load<Sprite>(path) != null;
                Debug.Log($"  {type,-18} → {path,-30} | {cfg.itemName,-6} ¥{cfg.baseValue,-4} 重{cfg.weight:F1} [{cfg.rarity}] {(exists ? "✓" : "✗")}");
            }

            // 也列出 ItemType 中定义了但没有对应文件的
            var allTypes = System.Enum.GetValues(typeof(ItemType)).Cast<ItemType>()
                .Where(t => t != ItemType.None && !available.Contains(t));
            foreach (var type in allTypes)
            {
                Debug.LogWarning($"  {type,-18} → 在 Items 中未找到对应图片文件");
            }
        }
    }
}
