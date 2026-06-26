// ArtResources.cs
// 美术资源路径常量与加载工具。
// 所有 Sprite 通过 Resources.Load 在代码中直接加载，无需 Inspector 手动拖拽。

using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 美术资源路径常量。对应 Assets/Resources/Art/ 下的目录结构。
    /// </summary>
    public static class ArtResourcePath
    {
        // ─── 背景 ───
        public const string GameBg        = "Art/Backgrounds/game_bg";
        public const string UnderwaterBg  = "Art/Backgrounds/underwater_bg";
        public const string SkyBg         = "Art/Backgrounds/sky_bg";
        public const string WaterSurface  = "Art/Backgrounds/water_surface";
        public const string Seabed        = "Art/Backgrounds/seabed";

        // ─── 角色 ───
        public const string FishermanIdle = "Art/Characters/fisherman_idle";
        public const string FishermanCast = "Art/Characters/fisherman";
        public const string Boat          = "Art/Characters/boat";

        // ─── 钩爪 ───
        public const string Hook          = "Art/Hook/hook";
        public const string Chain         = "Art/Hook/chain";

        // ─── Logo ───
        public const string GameLogo      = "Art/Logo/game_logo";

        // ─── UI ───
        public const string HudBg         = "Art/UI/hud_bg";
        public const string IconCoin      = "Art/UI/icon_coin";
        public const string IconClock     = "Art/UI/icon_clock";
        public const string IconTarget    = "Art/UI/icon_target";
        public const string IconFish      = "Art/UI/icon_fish";
        public const string IconStar      = "Art/UI/icon_star";
        public const string IconBomb      = "Art/UI/icon_bomb";
        public const string MenuBg        = "Art/UI/menu_bg";
        public const string PanelBg       = "Art/UI/panel_bg";
        public const string BtnNormal     = "Art/UI/btn_normal";
        public const string BtnHover      = "Art/UI/btn_hover";
        public const string BtnPressed    = "Art/UI/btn_pressed";
        public const string BtnGreen      = "Art/UI/btn_green";
        public const string BtnRed        = "Art/UI/btn_red";

        // ─── 物品 ───
        /// <summary>
        /// 根据物品类型自动推导 Sprite 资源路径。
        /// 约定: ItemType 枚举名 PascalCase → 文件名 snake_case
        /// 例如 SmallFish → Art/Items/small_fish, TreasureChest → Art/Items/treasure_chest
        /// 只需在 Resources/Art/Items/ 下放置对应 .png 即可生效，无需修改代码。
        /// </summary>
        public static string GetItemPath(ItemType type)
        {
            return "Art/Items/" + ItemTypeToSnakeCase(type);
        }

        /// <summary>PascalCase → snake_case 转换</summary>
        private static string ItemTypeToSnakeCase(ItemType type)
        {
            var name = type.ToString();
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0)
                    result.Append('_');
                result.Append(char.ToLowerInvariant(name[i]));
            }
            return result.ToString();
        }

        /// <summary>加载 Sprite（封装 Resources.Load，失败时输出警告）</summary>
        public static Sprite Load(string path)
        {
            var s = Resources.Load<Sprite>(path);
            if (s == null)
                Debug.LogWarning($"[ArtResources] 未找到资源: {path}");
            return s;
        }
    }
}
