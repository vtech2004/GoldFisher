// ItemSpawner.cs
// 物品生成器：根据关卡配置在水中区域随机生成物品，避免重叠，提供清场。

using UnityEngine;
using System.Collections.Generic;

namespace FishGame
{
    /// <summary>
    /// 物品生成器。挂在水中区域GameObject上。
    /// 根据LevelData在指定矩形区域内随机生成物品。
    /// </summary>
    public class ItemSpawner : MonoBehaviour
    {
        [Header("生成区域")]
        [Tooltip("生成区域中心（世界坐标）")]
        [SerializeField] private Vector2 spawnCenter = new Vector2(0f, -3f);

        [Tooltip("生成区域大小（宽,高）")]
        [SerializeField] private Vector2 spawnSize = new Vector2(16f, 8f);

        [Tooltip("物品基础 Prefab（含 SpriteRenderer + CatchableItem + Collider2D），Sprite 由代码动态加载")]
        [SerializeField] private GameObject baseItemPrefab;

        [Header("生成参数")]
        [Tooltip("避免重叠的最小间距")]
        [SerializeField] private float minSpawnDistance = 1.2f;

        [Tooltip("单次生成最大尝试次数（避免死循环）")]
        [SerializeField] private int maxAttempts = 30;

        [Header("运行时")]
        [SerializeField] private List<GameObject> spawnedItems = new List<GameObject>();

        /// <summary>已生成的物品列表</summary>
        public IReadOnlyList<GameObject> SpawnedItems => spawnedItems;

        /// <summary>
        /// 根据关卡数据生成所有物品。
        /// </summary>
        public void SpawnLevel(LevelData levelData)
        {
            ClearItems();
            if (levelData == null || levelData.itemEntries == null) return;

            foreach (var entry in levelData.itemEntries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    TrySpawnItem(entry);
                }
            }
        }

        /// <summary>
        /// 尝试生成单个物品（按类型与配置范围）。
        /// </summary>
        private void TrySpawnItem(LevelItemEntry entry)
        {
            if (baseItemPrefab == null)
            {
                Debug.LogWarning("[ItemSpawner] 未配置物品基础 Prefab");
                return;
            }

            Vector2 pos = FindValidPosition();
            if (pos == Vector2.zero && spawnedItems.Count > 0)
            {
                return;
            }

            GameObject go = Instantiate(baseItemPrefab, pos, Quaternion.identity, transform);

            // 确保 Rigidbody2D 存在（触发器必须）
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var item = go.GetComponent<CatchableItem>();
            if (item != null)
            {
                item.ApplyDefaultConfig(entry.itemType);
                string spritePath = ArtResourcePath.GetItemPath(entry.itemType);
                item.SetSprite(ArtResourcePath.Load(spritePath));
                OverrideItemStats(item, entry);
            }

            // 鱼类和宝物添加随机游动
            if (ShouldMove(entry.itemType))
            {
                var movement = go.AddComponent<FishMovement>();
                var bounds = new Rect(
                    spawnCenter.x - spawnSize.x / 2f,
                    spawnCenter.y - spawnSize.y / 2f,
                    spawnSize.x,
                    spawnSize.y
                );
                movement.Setup(GetMoveSpeed(entry.itemType), bounds);
                Debug.Log($"[ItemSpawner] Added FishMovement to {entry.itemType}, speed={movement.MoveSpeed:F2}");
            }

            spawnedItems.Add(go);
        }

        private static bool ShouldMove(ItemType type)
        {
            switch (type)
            {
                case ItemType.SmallFish:
                case ItemType.MediumFish:
                case ItemType.BigFish:
                case ItemType.Shark:
                case ItemType.Crab:
                case ItemType.Jellyfish:
                    return true;
                default:
                    return false;
            }
        }

        private static float GetMoveSpeed(ItemType type)
        {
            switch (type)
            {
                case ItemType.SmallFish:  return Random.Range(2.0f, 3.0f);
                case ItemType.MediumFish: return Random.Range(1.5f, 2.5f);
                case ItemType.BigFish:    return Random.Range(1.0f, 1.8f);
                case ItemType.Shark:      return Random.Range(1.8f, 2.8f);
                case ItemType.Crab:       return Random.Range(0.6f, 1.2f);
                case ItemType.Jellyfish:  return Random.Range(0.5f, 1.0f);
                default:                  return 1.0f;
            }
        }

        private void OverrideItemStats(CatchableItem item, LevelItemEntry entry)
        {
            // 通过反射或公开方法无法直接设置私有字段，这里提供一个配置后随机的方式：
            // 使用ItemConfigDatabase的默认值 + 范围抖动
            var cfg = ItemConfigDatabase.GetConfig(entry.itemType);
            int value = cfg.baseValue;
            float weight = cfg.weight;
            if (entry.valueRange.y > entry.valueRange.x)
            {
                value = Mathf.RoundToInt(Random.Range(entry.valueRange.x, entry.valueRange.y));
            }
            if (entry.weightRange.y > entry.weightRange.x)
            {
                weight = Random.Range(entry.weightRange.x, entry.weightRange.y);
            }
            // 通过公开属性只能读取，需借助公开方法设置；此处提供一个内部设置方式
            item.SetRuntimeStats(value, weight);
        }

        /// <summary>
        /// 在生成区域内寻找一个不与已生成物品重叠的位置。
        /// </summary>
        private Vector2 FindValidPosition()
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector2 pos = new Vector2(
                    spawnCenter.x + Random.Range(-spawnSize.x / 2f, spawnSize.x / 2f),
                    spawnCenter.y + Random.Range(-spawnSize.y / 2f, spawnSize.y / 2f)
                );

                bool valid = true;
                foreach (var existing in spawnedItems)
                {
                    if (existing == null) continue;
                    if (Vector2.Distance(pos, existing.transform.position) < minSpawnDistance)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid) return pos;
            }
            return Vector2.zero;
        }

        /// <summary>清场：销毁所有已生成的物品</summary>
        public void ClearItems()
        {
            foreach (var go in spawnedItems)
            {
                if (go != null) Destroy(go);
            }
            spawnedItems.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(spawnCenter, spawnSize);
        }
    }
}
