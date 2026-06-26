// 暂停面板
// 提供继续游戏、重新开始、返回主菜单功能

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FishGame
{
    /// <summary>
    /// 暂停面板控制器
    /// </summary>
    public class PausePanel : MonoBehaviour
    {
        [Header("按钮引用")]
        [SerializeField] private Button resumeButton;       // 继续游戏
        [SerializeField] private Button restartButton;     // 重新开始
        [SerializeField] private Button returnMenuButton;  // 返回主菜单

        [Header("面板背景")]
        [Tooltip("面板背景 Image")]
        [SerializeField] private Image panelBackground;

        [Header("可选引用")]
        [SerializeField] private TextMeshProUGUI titleText;

        // 事件
        public event Action OnResume;
        public event Action OnRestart;
        public event Action OnReturnMenu;

        private void Awake()
        {
            BindButtonEvents();
            if (panelBackground != null)
                panelBackground.sprite = ArtResourcePath.Load(ArtResourcePath.PanelBg);
        }

        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        private void BindButtonEvents()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(HandleResume);
            }
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestart);
            }
            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.AddListener(HandleReturnMenu);
            }
        }

        private void HandleResume()
        {
            Debug.Log("[PausePanel] 继续游戏");
            OnResume?.Invoke();
        }

        private void HandleRestart()
        {
            Debug.Log("[PausePanel] 重新开始");
            OnRestart?.Invoke();
        }

        private void HandleReturnMenu()
        {
            Debug.Log("[PausePanel] 返回主菜单");
            OnReturnMenu?.Invoke();
        }

        /// <summary>
        /// 显示暂停面板
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏暂停面板
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(HandleResume);
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestart);
            }
            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.RemoveListener(HandleReturnMenu);
            }
        }
    }
}
