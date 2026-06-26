// LevelDataCreator.cs - Editor工具
// 一键创建默认关卡数据并填入LevelConfig
// 菜单: FishGame > Create Default Levels

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FishGame.Editor
{
    public static class LevelDataCreator
    {
        private const string CONFIG_PATH = "Assets/Config/LevelConfig.asset";
        private const string LEVELS_FOLDER = "Assets/Config/Levels";

        [MenuItem("FishGame/Create Default Levels")]
        public static void CreateDefaultLevels()
        {
            if (!AssetDatabase.IsValidFolder(LEVELS_FOLDER))
                AssetDatabase.CreateFolder("Assets/Config", "Levels");

            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>(CONFIG_PATH);
            if (levelConfig == null)
            {
                Debug.LogError("[LevelDataCreator] 未找到 LevelConfig.asset，请先运行 FishGame > Build Main Scene");
                return;
            }

            var levelDefs = new[]
            {
                new LevelDef
                {
                    id = 1, name = "第1关", target = 300, time = 60f, bonus = 50,
                    items = new[]
                    {
                        new EntryDef(ItemType.SmallFish,  8, 15, 30,  0.4f, 0.8f),
                        new EntryDef(ItemType.MediumFish, 4, 50, 80,  1.0f, 1.5f),
                        new EntryDef(ItemType.Crab,       2, 60, 90,  0.8f, 1.2f),
                        new EntryDef(ItemType.TinCan,     2,  2,  5,  0.5f, 1.0f),
                    }
                },
                new LevelDef
                {
                    id = 2, name = "第2关", target = 600, time = 60f, bonus = 100,
                    items = new[]
                    {
                        new EntryDef(ItemType.SmallFish,  6, 15, 30,  0.4f, 0.8f),
                        new EntryDef(ItemType.MediumFish, 5, 50, 90,  1.0f, 1.8f),
                        new EntryDef(ItemType.BigFish,    2, 120, 180, 2.0f, 3.0f),
                        new EntryDef(ItemType.GoldNugget, 1, 250, 350, 2.5f, 3.5f),
                        new EntryDef(ItemType.Seaweed,    2,  1,  3,  0.4f, 0.8f),
                    }
                },
                new LevelDef
                {
                    id = 3, name = "第3关", target = 1000, time = 70f, bonus = 200,
                    items = new[]
                    {
                        new EntryDef(ItemType.MediumFish,    4, 55, 90,  1.0f, 1.8f),
                        new EntryDef(ItemType.BigFish,       3, 130, 180, 2.0f, 3.0f),
                        new EntryDef(ItemType.Jellyfish,     2, 80, 120, 1.2f, 2.0f),
                        new EntryDef(ItemType.TreasureChest, 1, 400, 600, 3.0f, 4.0f),
                        new EntryDef(ItemType.GoldNugget,    2, 260, 340, 2.5f, 3.5f),
                        new EntryDef(ItemType.Boot,          2,  1,  3,  1.2f, 1.8f),
                    }
                },
                new LevelDef
                {
                    id = 4, name = "第4关", target = 1500, time = 75f, bonus = 300,
                    items = new[]
                    {
                        new EntryDef(ItemType.MediumFish,    3, 55,  90,  1.0f, 1.8f),
                        new EntryDef(ItemType.BigFish,       4, 130, 190, 2.0f, 3.2f),
                        new EntryDef(ItemType.Shark,         1, 350, 450, 4.0f, 5.0f),
                        new EntryDef(ItemType.Pearl,         2, 500, 700, 0.1f, 0.3f),
                        new EntryDef(ItemType.Diamond,       1, 700, 900, 0.2f, 0.4f),
                        new EntryDef(ItemType.Trash,         2,  3,   8,  2.0f, 3.0f),
                    }
                },
                new LevelDef
                {
                    id = 5, name = "第5关", target = 2500, time = 80f, bonus = 500,
                    items = new[]
                    {
                        new EntryDef(ItemType.BigFish,       3, 140, 200, 2.2f, 3.5f),
                        new EntryDef(ItemType.Shark,         2, 370, 450, 4.0f, 5.0f),
                        new EntryDef(ItemType.Pearl,         2, 550, 700, 0.1f, 0.3f),
                        new EntryDef(ItemType.Diamond,       2, 750, 900, 0.2f, 0.4f),
                        new EntryDef(ItemType.MysteryBox,    2, 150, 300, 1.8f, 2.5f),
                        new EntryDef(ItemType.TreasureChest, 2, 450, 600, 3.0f, 4.5f),
                        new EntryDef(ItemType.Bomb,          1,   0,   0, 0.8f, 1.2f),
                    }
                },
            };

            var createdLevels = new List<LevelData>();
            foreach (var def in levelDefs)
            {
                string path = $"{LEVELS_FOLDER}/Level{def.id:D2}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (existing != null)
                {
                    createdLevels.Add(existing);
                    continue;
                }

                var ld = ScriptableObject.CreateInstance<LevelData>();
                ld.levelId    = def.id;
                ld.levelName  = def.name;
                ld.targetMoney = def.target;
                ld.timeLimit  = def.time;
                ld.clearBonus = def.bonus;

                ld.itemEntries = new List<LevelItemEntry>();
                foreach (var e in def.items)
                {
                    ld.itemEntries.Add(new LevelItemEntry
                    {
                        itemType    = e.type,
                        count       = e.count,
                        valueRange  = new Vector2Int(e.vMin, e.vMax),
                        weightRange = new Vector2(e.wMin, e.wMax),
                    });
                }

                AssetDatabase.CreateAsset(ld, path);
                createdLevels.Add(ld);
            }

            // 写入 LevelConfig
            var so = new SerializedObject(levelConfig);
            var levelsProp = so.FindProperty("levels");
            levelsProp.ClearArray();
            for (int i = 0; i < createdLevels.Count; i++)
            {
                levelsProp.InsertArrayElementAtIndex(i);
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = createdLevels[i];
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LevelDataCreator] 已创建 {createdLevels.Count} 个关卡数据并写入 LevelConfig。");
        }

        // 内部数据结构，只用于此工具
        private struct LevelDef
        {
            public int id;
            public string name;
            public int target;
            public float time;
            public int bonus;
            public EntryDef[] items;
        }

        private struct EntryDef
        {
            public ItemType type;
            public int count, vMin, vMax;
            public float wMin, wMax;
            public EntryDef(ItemType t, int c, int v0, int v1, float w0, float w1)
            { type=t; count=c; vMin=v0; vMax=v1; wMin=w0; wMax=w1; }
        }
    }
}
