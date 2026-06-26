// ChineseFontCreator.cs - Editor工具
// 自动从系统扫描中文字体，生成 TMP Font Asset
// 菜单: FishGame > Create Chinese Font Asset

using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using UnityEngine.TextCore.LowLevel;

namespace FishGame.Editor
{
    public static class ChineseFontCreator
    {
        private const string FONT_ASSET_PATH = "Assets/Fonts/ChineseFont SDF.asset";
        private const string FONT_MAT_PATH = "Assets/Fonts/ChineseFont SDF.mat";

        /// <summary>
        /// 尝试加载已有的中文字体资产，不存在则自动创建
        /// </summary>
        public static TMP_FontAsset GetOrCreateChineseFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
            if (existing != null)
                return existing;

            return CreateChineseFontAsset();
        }

        [MenuItem("FishGame/Create Chinese Font Asset")]
        public static TMP_FontAsset CreateChineseFontAsset()
        {
            Font sourceFont = FindSystemChineseFont();
            if (sourceFont == null)
            {
                Debug.LogError("[ChineseFont] 系统中未找到可用的中文字体！\n"
                    + "请手动将一个中文字体 .ttf 文件放入 Assets/Fonts/ 目录，"
                    + "然后在 TMP Font Asset Creator 中生成 SDF 字体资产。");
                return null;
            }

            // 使用较大图集和动态填充模式，适合 CJK 字符
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 4096,
                atlasHeight: 4096,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true
            );

            if (fontAsset == null)
            {
                Debug.LogError("[ChineseFont] TMP 字体资产创建失败！");
                Object.DestroyImmediate(sourceFont);
                return null;
            }

            // 预添加所有游戏中会用到的中文字符 + 常用 ASCII
            string chars = GetAllNeededCharacters();
            fontAsset.TryAddCharacters(chars);

            // 确保目标目录存在
            if (!AssetDatabase.IsValidFolder("Assets/Fonts"))
                AssetDatabase.CreateFolder("Assets", "Fonts");

            // 保存字体资产
            AssetDatabase.CreateAsset(fontAsset, FONT_ASSET_PATH);

            // 保存材质
            if (fontAsset.material != null)
            {
                fontAsset.material.shader = Shader.Find("TextMeshPro/Distance Field");
                AssetDatabase.CreateAsset(fontAsset.material, FONT_MAT_PATH);
            }

            // 注册到 TMP Settings 的 fallback 字体链
            RegisterAsFallbackFont(fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var result = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
            Debug.Log($"[ChineseFont] 中文字体资产创建完成: {FONT_ASSET_PATH}");
            return result;
        }

        /// <summary>
        /// 将字体注册到 TMP Settings 的 fallback 字体链中
        /// </summary>
        private static void RegisterAsFallbackFont(TMP_FontAsset fontAsset)
        {
            string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            var settingsAsset = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);
            if (settingsAsset == null)
            {
                Debug.LogWarning("[ChineseFont] 未找到 TMP Settings.asset，跳过 fallback 注册。");
                return;
            }

            var so = new SerializedObject(settingsAsset);
            var fallbackList = so.FindProperty("m_fallbackFontAssets");

            // 检查是否已存在
            for (int i = 0; i < fallbackList.arraySize; i++)
            {
                if (fallbackList.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset)
                {
                    so.ApplyModifiedProperties();
                    return; // 已注册，跳过
                }
            }

            // 添加到列表
            int idx = fallbackList.arraySize;
            fallbackList.InsertArrayElementAtIndex(idx);
            fallbackList.GetArrayElementAtIndex(idx).objectReferenceValue = fontAsset;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settingsAsset);
            Debug.Log("[ChineseFont] 已将中文字体注册为 TMP Settings fallback 字体。");
        }

        /// <summary>
        /// 在 Windows 系统中查找中文字体
        /// </summary>
        private static Font FindSystemChineseFont()
        {
            // Windows 常见中文字体路径
            string[] fontPaths =
            {
                @"C:\Windows\Fonts\simhei.ttf",      // 黑体
                @"C:\Windows\Fonts\Deng.ttf",         // 等线
                @"C:\Windows\Fonts\Dengb.ttf",        // 等线 粗体
                @"C:\Windows\Fonts\SIMYOU.ttf",       // 幼圆
                @"C:\Windows\Fonts\STKAITI.ttf",      // 楷体
                @"C:\Windows\Fonts\STFANGSO.ttf",     // 仿宋
                @"C:\Windows\Fonts\simsun.ttf",       // 宋体（部分系统）
            };

            foreach (string path in fontPaths)
            {
                if (File.Exists(path))
                {
                    Debug.Log($"[ChineseFont] 找到系统字体: {Path.GetFileName(path)}");
                    return new Font(path);
                }
            }

            // 后备方案：通过操作系统字体名称创建
            string[] fontNames = { "SimHei", "Microsoft YaHei", "DengXian", "SimSun", "FangSong", "KaiTi" };
            foreach (string name in fontNames)
            {
                var font = Font.CreateDynamicFontFromOSFont(name, 16);
                if (font != null)
                {
                    Debug.Log($"[ChineseFont] 使用操作系统字体: {name}");
                    return font;
                }
            }

            return null;
        }

        /// <summary>
        /// 收集游戏 UI 中所有需要用到的字符
        /// </summary>
        private static string GetAllNeededCharacters()
        {
            return
                // 主菜单按钮
                "开始游戏关卡选择设置退出" +
                // HUD
                "第关目标剩余金钱时" +
                // 结果面板
                "过关！完成挑战失败得分下一重试" +
                // 暂停面板
                "暂停继续重新返回菜单" +
                // 其他
                "钓渔人捕获大师间计分" +
                // 数字和符号
                "0123456789$: " +
                // 拉丁字母
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        }
    }
}
