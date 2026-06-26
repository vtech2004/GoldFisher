// UI总管理器
// 单例模式，管理所有UI面板的生命周期和切换
// 提供统一接口供GameManager调用，实现HUD更新、面板显示等功能

using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// UI总管理器（单例），管理所有UI面板
    /// 实现 IUIManager 接口供 GameManager 调用
    /// </summary>
    public class UIManager : MonoBehaviour, IUIManager
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[UIManager] 场景中未找到UIManager实例，请确保已添加UIManager到场景中");
                    }
                }
                return _instance;
            }
        }

        [Header("Canvas引用")]
        [SerializeField] private Canvas mainCanvas;

        [Header("面板引用")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private ResultPanel resultPanel;
        [SerializeField] private PausePanel pausePanel;

        [Header("动画配置")]
        [SerializeField] private bool enablePanelAnimation = true;
        [SerializeField] private float panelFadeDuration = 0.2f;

        // 当前显示状态
        private bool _isPaused = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 连线各面板的按钮事件 → GameManager
            if (mainMenuPanel != null)
                mainMenuPanel.OnStartGame += HandleStartGame;

            if (resultPanel != null)
            {
                resultPanel.OnNextLevel   += HandleNextLevel;
                resultPanel.OnRetry       += HandleRetry;
                resultPanel.OnReturnMenu  += HandleReturnToMenu;
            }

            if (pausePanel != null)
            {
                pausePanel.OnResume      += HandleResume;
                pausePanel.OnRestart     += HandleRetry;
                pausePanel.OnReturnMenu  += HandleReturnToMenu;
            }

            // 监听 GameManager 状态变化（ReturnToMenu 时自动显示菜单）
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleGameStateChanged;

            // 初始化：隐藏所有面板，然后显示主菜单
            HideAllPanels();
            ShowMainMenu();
        }

        private void HandleStartGame()
        {
            HideMainMenu();
            GameManager.Instance?.StartNewGame();
        }

        private void HandleNextLevel()
        {
            HideResultPanel();
            GameManager.Instance?.StartNextLevel();
        }

        private void HandleRetry()
        {
            HideResultPanel();
            HidePauseMenu();
            GameManager.Instance?.RetryCurrentLevel();
        }

        private void HandleReturnToMenu()
        {
            GameManager.Instance?.ReturnToMenu();
            ShowMainMenu();
        }

        private void HandleResume()
        {
            GameManager.Instance?.ResumeGame();
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Menu)
                ShowMainMenu();
        }

        // ============ HUD 相关接口 ============

        /// <summary>
        /// 显示游戏HUD
        /// </summary>
        public void ShowHUD()
        {
            if (hudController != null)
            {
                hudController.Show();
            }
            else
            {
                Debug.LogWarning("[UIManager] HUDController 未配置");
            }
        }

        /// <summary>
        /// 隐藏游戏HUD
        /// </summary>
        public void HideHUD()
        {
            if (hudController != null)
            {
                hudController.Hide();
            }
        }

        /// <summary>
        /// 更新当前金钱显示
        /// </summary>
        /// <param name="money">金钱数量</param>
        public void UpdateScore(int money)
        {
            if (hudController != null)
            {
                hudController.UpdateScore(money);
            }
        }

        /// <summary>
        /// 更新倒计时显示
        /// </summary>
        /// <param name="time">剩余时间（秒）</param>
        public void UpdateTimer(float time)
        {
            if (hudController != null)
            {
                hudController.UpdateTimer(time);
            }
        }

        /// <summary>
        /// 更新目标金额显示
        /// </summary>
        /// <param name="target">目标金额</param>
        public void UpdateTarget(int target)
        {
            if (hudController != null)
            {
                hudController.UpdateTarget(target);
            }
        }

        /// <summary>
        /// 更新关卡显示
        /// </summary>
        /// <param name="level">关卡编号</param>
        public void UpdateLevel(int level)
        {
            if (hudController != null)
            {
                hudController.UpdateLevel(level);
            }
        }

        // ============ 结果面板接口 ============

        /// <summary>
        /// 显示过关界面
        /// </summary>
        /// <param name="level">关卡编号</param>
        /// <param name="score">得分（当前金钱）</param>
        /// <param name="target">目标金额</param>
        public void ShowLevelComplete(int level, int score, int target)
        {
            HideHUD();

            if (resultPanel != null)
            {
                resultPanel.ShowLevelComplete(level, score, target);
            }
            else
            {
                Debug.LogWarning("[UIManager] ResultPanel 未配置");
            }
        }

        /// <summary>
        /// 显示失败界面
        /// </summary>
        /// <param name="level">关卡编号</param>
        /// <param name="score">得分（当前金钱）</param>
        /// <param name="target">目标金额</param>
        public void ShowLevelFailed(int level, int score, int target)
        {
            HideHUD();

            if (resultPanel != null)
            {
                resultPanel.ShowLevelFailed(level, score, target);
            }
            else
            {
                Debug.LogWarning("[UIManager] ResultPanel 未配置");
            }
        }

        // ============ 主菜单接口 ============

        /// <summary>
        /// 显示主菜单
        /// </summary>
        public void ShowMainMenu()
        {
            HideHUD();
            HidePauseMenu();
            HideResultPanel();

            if (mainMenuPanel != null)
            {
                mainMenuPanel.Show();
            }
            else
            {
                Debug.LogWarning("[UIManager] MainMenuPanel 未配置");
            }
        }

        /// <summary>
        /// 隐藏主菜单
        /// </summary>
        public void HideMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Hide();
            }
        }

        // ============ 暂停菜单接口 ============

        /// <summary>
        /// 显示暂停菜单
        /// </summary>
        public void ShowPauseMenu()
        {
            if (_isPaused)
            {
                return;
            }

            _isPaused = true;
            Time.timeScale = 0f;

            if (pausePanel != null)
            {
                pausePanel.Show();
            }
            else
            {
                Debug.LogWarning("[UIManager] PausePanel 未配置");
            }
        }

        /// <summary>
        /// 隐藏暂停菜单
        /// </summary>
        public void HidePauseMenu()
        {
            if (!_isPaused)
            {
                return;
            }

            _isPaused = false;
            Time.timeScale = 1f;

            if (pausePanel != null)
            {
                pausePanel.Hide();
            }
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 隐藏结果面板
        /// </summary>
        public void HideResultPanel()
        {
            if (resultPanel != null)
            {
                resultPanel.Hide();
            }
        }

        /// <summary>
        /// 隐藏所有面板
        /// </summary>
        public void HideAllPanels()
        {
            HideHUD();
            HideMainMenu();
            HideResultPanel();
            HidePauseMenu();
        }

        /// <summary>
        /// 获取HUD控制器
        /// </summary>
        public HUDController GetHUDController()
        {
            return hudController;
        }

        /// <summary>
        /// 获取主菜单面板
        /// </summary>
        public MainMenuPanel GetMainMenuPanel()
        {
            return mainMenuPanel;
        }

        /// <summary>
        /// 获取结果面板
        /// </summary>
        public ResultPanel GetResultPanel()
        {
            return resultPanel;
        }

        /// <summary>
        /// 获取暂停面板
        /// </summary>
        public PausePanel GetPausePanel()
        {
            return pausePanel;
        }

        /// <summary>
        /// 是否处于暂停状态
        /// </summary>
        public bool IsPaused()
        {
            return _isPaused;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            if (mainMenuPanel != null)
                mainMenuPanel.OnStartGame -= HandleStartGame;

            if (resultPanel != null)
            {
                resultPanel.OnNextLevel  -= HandleNextLevel;
                resultPanel.OnRetry      -= HandleRetry;
                resultPanel.OnReturnMenu -= HandleReturnToMenu;
            }

            if (pausePanel != null)
            {
                pausePanel.OnResume     -= HandleResume;
                pausePanel.OnRestart    -= HandleRetry;
                pausePanel.OnReturnMenu -= HandleReturnToMenu;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleGameStateChanged;

            // 确保时间恢复
            Time.timeScale = 1f;
        }
    }
}
