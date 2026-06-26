// 主菜单面板
// 提供开始游戏、关卡选择、设置、退出游戏等按钮
// 按钮事件通过UnityEvent绑定

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FishGame
{
    /// <summary>
    /// 主菜单面板控制器
    /// </summary>
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("按钮引用")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitGameButton;

        [Header("主菜单背景与Logo")]
        [Tooltip("菜单背景 Image")]
        [SerializeField] private Image menuBackground;
        [Tooltip("游戏 Logo Image")]
        [SerializeField] private Image logoImage;

        [Header("可选引用")]
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TextMeshProUGUI titleText;

        // 按钮事件（可在Inspector中额外绑定）
        public event Action OnStartGame;
        public event Action OnLevelSelect;
        public event Action OnSettings;
        public event Action OnExitGame;

        private void Awake()
        {
            BindButtonEvents();
            ApplyVisuals();
        }

        /// <summary>
        /// 应用背景、Logo 等美术资源（从 Resources 直接加载）
        /// </summary>
        private void ApplyVisuals()
        {
            if (menuBackground != null)
                menuBackground.sprite = ArtResourcePath.Load(ArtResourcePath.MenuBg);
            if (logoImage != null)
                logoImage.sprite = ArtResourcePath.Load(ArtResourcePath.GameLogo);
        }

        /// <summary>
        /// 为所有按钮设置统一 Sprite 样式（从 Resources 加载）
        /// </summary>
        public void ApplyButtonStyle()
        {
            var normal  = ArtResourcePath.Load(ArtResourcePath.BtnNormal);
            var hover   = ArtResourcePath.Load(ArtResourcePath.BtnHover);
            var pressed = ArtResourcePath.Load(ArtResourcePath.BtnPressed);
            if (startGameButton != null)    ApplyButtonSprites(startGameButton, normal, hover, pressed);
            if (levelSelectButton != null)  ApplyButtonSprites(levelSelectButton, normal, hover, pressed);
            if (settingsButton != null)     ApplyButtonSprites(settingsButton, normal, hover, pressed);
            if (exitGameButton != null)     ApplyButtonSprites(exitGameButton, normal, hover, pressed);
        }

        private void ApplyButtonSprites(Button btn, Sprite normal, Sprite hover, Sprite pressed)
        {
            var transition = btn.transition;
            if (transition != Selectable.Transition.SpriteSwap) return;
            SpriteState ss = btn.spriteState;
            if (normal != null && btn.image != null) btn.image.sprite = normal;
            if (hover != null) ss.highlightedSprite = hover;
            if (pressed != null) ss.pressedSprite = pressed;
            btn.spriteState = ss;
        }

        /// <summary>
        /// 绑定按钮点击事件
        /// </summary>
        private void BindButtonEvents()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(HandleStartGame);
            }

            if (levelSelectButton != null)
            {
                levelSelectButton.onClick.AddListener(HandleLevelSelect);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettings);
            }

            if (exitGameButton != null)
            {
                exitGameButton.onClick.AddListener(HandleExitGame);
            }
        }

        private void HandleStartGame()
        {
            Debug.Log("[MainMenuPanel] 开始游戏");
            OnStartGame?.Invoke();
        }

        private void HandleLevelSelect()
        {
            Debug.Log("[MainMenuPanel] 关卡选择");
            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(true);
            }
            OnLevelSelect?.Invoke();
        }

        private void HandleSettings()
        {
            Debug.Log("[MainMenuPanel] 设置");
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
            OnSettings?.Invoke();
        }

        private void HandleExitGame()
        {
            Debug.Log("[MainMenuPanel] 退出游戏");
            OnExitGame?.Invoke();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // 清理按钮事件绑定
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(HandleStartGame);
            }
            if (levelSelectButton != null)
            {
                levelSelectButton.onClick.RemoveListener(HandleLevelSelect);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettings);
            }
            if (exitGameButton != null)
            {
                exitGameButton.onClick.RemoveListener(HandleExitGame);
            }
        }
    }
}
