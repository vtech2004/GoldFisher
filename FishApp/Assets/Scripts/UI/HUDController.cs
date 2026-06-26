// HUD控制器
// 负责显示游戏中的金钱、时间、目标金额、关卡等信息
// 时间不足时文字变红警告

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FishGame
{
    /// <summary>
    /// HUD控制器，管理游戏主界面信息显示
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("文本显示组件")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI targetText;
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("HUD 背景与图标")]
        [Tooltip("HUD 背景 Image")]
        [SerializeField] private Image hudBackground;
        [Tooltip("金币图标 Image")]
        [SerializeField] private Image coinIcon;
        [Tooltip("时钟图标 Image")]
        [SerializeField] private Image clockIcon;
        [Tooltip("目标图标 Image")]
        [SerializeField] private Image targetIcon;
        [Tooltip("鱼图标 Image")]
        [SerializeField] private Image fishIcon;
        [Tooltip("星星图标 Image")]
        [SerializeField] private Image starIcon;
        [Tooltip("炸弹道具图标 Image")]
        [SerializeField] private Image bombIcon;

        [Header("时间警告配置")]
        [SerializeField] private float warningTimeThreshold = 10f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = Color.red;

        // 时间警告状态
        private bool _isWarning = false;

        /// <summary>
        /// 更新金钱显示
        /// </summary>
        /// <param name="money">当前金钱</param>
        public void UpdateScore(int money)
        {
            if (moneyText != null)
            {
                moneyText.text = $"$ {money}";
            }
        }

        /// <summary>
        /// 更新倒计时显示
        /// </summary>
        /// <param name="time">剩余时间（秒）</param>
        public void UpdateTimer(float time)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";

                // 时间警告变色
                UpdateWarningState(time <= warningTimeThreshold && time > 0);
            }
        }

        /// <summary>
        /// 更新目标金额显示
        /// </summary>
        /// <param name="target">目标金额</param>
        public void UpdateTarget(int target)
        {
            if (targetText != null)
            {
                targetText.text = $"目标: ${target}";
            }
        }

        /// <summary>
        /// 更新关卡显示
        /// </summary>
        /// <param name="level">关卡编号</param>
        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"第 {level} 关";
            }
        }

        /// <summary>
        /// 更新警告状态
        /// </summary>
        /// <param name="shouldWarn">是否应该警告</param>
        private void UpdateWarningState(bool shouldWarn)
        {
            if (_isWarning == shouldWarn)
            {
                return;
            }

            _isWarning = shouldWarn;

            if (timerText != null)
            {
                timerText.color = shouldWarn ? warningColor : normalColor;
            }
        }

        /// <summary>
        /// 重置HUD状态
        /// </summary>
        public void ResetHUD()
        {
            UpdateScore(0);
            UpdateTimer(0);
            UpdateTarget(0);
            UpdateLevel(1);
            UpdateWarningState(false);
            ApplyIcons();
        }

        /// <summary>
        /// 应用所有图标和背景 Sprite
        /// </summary>
        private void ApplyIcons()
        {
            if (hudBackground != null) hudBackground.sprite = ArtResourcePath.Load(ArtResourcePath.HudBg);
            if (coinIcon != null)      coinIcon.sprite      = ArtResourcePath.Load(ArtResourcePath.IconCoin);
            if (clockIcon != null)     clockIcon.sprite     = ArtResourcePath.Load(ArtResourcePath.IconClock);
            if (targetIcon != null)    targetIcon.sprite    = ArtResourcePath.Load(ArtResourcePath.IconTarget);
            if (fishIcon != null)      fishIcon.sprite      = ArtResourcePath.Load(ArtResourcePath.IconFish);
            if (starIcon != null)      starIcon.sprite      = ArtResourcePath.Load(ArtResourcePath.IconStar);
            if (bombIcon != null)      bombIcon.sprite      = ArtResourcePath.Load(ArtResourcePath.IconBomb);
        }

        /// <summary>
        /// 显示HUD
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏HUD
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
