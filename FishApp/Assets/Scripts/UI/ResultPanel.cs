// 结果面板（通用）
// 用于显示过关和失败界面，可复用
// 过关：显示目标/得分、下一关按钮、返回菜单按钮
// 失败：显示目标/得分、重试按钮、返回菜单按钮

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FishGame
{
    /// <summary>
    /// 结果面板控制器，支持过关和失败两种模式
    /// </summary>
    public class ResultPanel : MonoBehaviour
    {
        [Header("文本显示组件")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI targetText;  // 目标金额文本
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("按钮引用")]
        [SerializeField] private Button nextLevelButton;     // 下一关按钮（过关显示）
        [SerializeField] private Button retryButton;         // 重试按钮（失败显示）
        [SerializeField] private Button returnMenuButton;    // 返回菜单按钮

        [Header("面板背景")]
        [Tooltip("面板背景 Image")]
        [SerializeField] private Image panelBackground;

        [Header("颜色配置")]
        [SerializeField] private Color winColor = new Color(1f, 0.84f, 0f); // 金色
        [SerializeField] private Color failColor = new Color(0.8f, 0.2f, 0.2f); // 红色

        // 事件
        public event Action OnNextLevel;
        public event Action OnRetry;
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
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(HandleNextLevel);
            }
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetry);
            }
            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.AddListener(HandleReturnMenu);
            }
        }

        /// <summary>
        /// 显示过关界面
        /// </summary>
        /// <param name="level">关卡编号</param>
        /// <param name="score">得分（当前金钱）</param>
        /// <param name="target">目标金额</param>
        public void ShowLevelComplete(int level, int score, int target)
        {
            gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "过关！";
                titleText.color = winColor;
            }

            if (levelText != null)
            {
                levelText.text = $"第 {level} 关 完成";
            }

            if (targetText != null)
            {
                targetText.text = $"目标: ${target}";
                targetText.gameObject.SetActive(true);
            }

            if (scoreText != null)
            {
                scoreText.text = $"得分: ${score}";
            }

            // 过关模式：显示下一关按钮，隐藏重试按钮
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(true);
            }
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(false);
            }

            Debug.Log($"[ResultPanel] 显示过关界面: level={level}, score={score}, target={target}");
        }

        /// <summary>
        /// 显示失败界面
        /// </summary>
        /// <param name="level">关卡编号</param>
        /// <param name="score">得分（当前金钱）</param>
        /// <param name="target">目标金额</param>
        public void ShowLevelFailed(int level, int score, int target)
        {
            gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "挑战失败";
                titleText.color = failColor;
            }

            if (levelText != null)
            {
                levelText.text = $"第 {level} 关";
            }

            if (targetText != null)
            {
                targetText.text = $"目标: ${target}";
                targetText.gameObject.SetActive(true);
            }

            if (scoreText != null)
            {
                scoreText.text = $"得分: ${score}";
            }

            // 失败模式：隐藏下一关按钮，显示重试按钮
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(false);
            }
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
            }

            Debug.Log($"[ResultPanel] 显示失败界面: level={level}, score={score}, target={target}");
        }

        private void HandleNextLevel()
        {
            Debug.Log("[ResultPanel] 点击下一关");
            OnNextLevel?.Invoke();
        }

        private void HandleRetry()
        {
            Debug.Log("[ResultPanel] 点击重试");
            OnRetry?.Invoke();
        }

        private void HandleReturnMenu()
        {
            Debug.Log("[ResultPanel] 点击返回菜单");
            OnReturnMenu?.Invoke();
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
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(HandleNextLevel);
            }
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(HandleRetry);
            }
            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.RemoveListener(HandleReturnMenu);
            }
        }
    }
}
