// GameManager.cs
// 游戏总管理器（单例）：协调各子系统，管理游戏状态与关卡流程。
// 流程：开始关卡 -> 生成物品 -> 开始计时 -> 玩游戏 -> 时间到判断是否达标 -> 过关/失败
// 引用UIManager与NetworkClient的接口（由其他开发者实现）。

using System;
using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 游戏整体状态。
    /// </summary>
    public enum GameState
    {
        Menu,           // 主菜单
        Playing,        // 游戏中
        Paused,         // 暂停
        LevelComplete,  // 关卡完成
        LevelFailed,    // 关卡失败
        GameOver        // 全部关卡通关
    }

    /// <summary>
    /// UIManager接口（由其他开发者实现）。GameManager通过此接口调用UI。
    /// </summary>
    public interface IUIManager
    {
        void ShowHUD();
        void ShowLevelComplete(int levelId, int score, int target);
        void ShowLevelFailed(int levelId, int score, int target);
        void UpdateScore(int current);
        void UpdateTimer(float remaining);
        void UpdateTarget(int target);
    }

    /// <summary>
    /// NetworkClient接口（由其他开发者实现）。用于上报分数。
    /// </summary>
    public interface INetworkClient
    {
        void ReportLevelResult(int levelId, int score, bool success);
    }

    /// <summary>
    /// 游戏总管理器（单例）。
    /// 协调HookController, ItemSpawner, ScoreManager, TimerManager, UIManager, NetworkClient。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("子系统引用")]
        [SerializeField] private FishermanController fisherman;
        [SerializeField] private HookController hook;
        [SerializeField] private ItemSpawner spawner;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private TimerManager timerManager;

        [Header("背景美术")]
        [Tooltip("游戏主背景 SpriteRenderer")]
        [SerializeField] private SpriteRenderer gameBackground;
        [Tooltip("水下背景 SpriteRenderer（叠加层）")]
        [SerializeField] private SpriteRenderer underwaterBackground;
        [Tooltip("天空背景 SpriteRenderer")]
        [SerializeField] private SpriteRenderer skyBackground;
        [Tooltip("水面 SpriteRenderer")]
        [SerializeField] private SpriteRenderer waterSurface;
        [Tooltip("海底 SpriteRenderer")]
        [SerializeField] private SpriteRenderer seabed;

        [Header("关卡配置")]
        [SerializeField] private LevelConfig levelConfig;

        [Header("外部接口（运行时注入）")]
        [SerializeField] private MonoBehaviour uiManagerMono;
        [SerializeField] private MonoBehaviour networkClientMono;

        private IUIManager uiManager;
        private INetworkClient networkClient;

        [Header("状态")]
        [SerializeField] private GameState state = GameState.Menu;
        [SerializeField] private int currentLevelIndex = -1;
        [SerializeField] private int currentLevelId = 0;

        /// <summary>当前游戏状态</summary>
        public GameState State => state;

        /// <summary>当前关卡索引（0-based）</summary>
        public int CurrentLevelIndex => currentLevelIndex;

        /// <summary>当前关卡ID</summary>
        public int CurrentLevelId => currentLevelId;

        /// <summary>当前关卡数据</summary>
        public LevelData CurrentLevelData { get; private set; }

        /// <summary>游戏状态变化事件</summary>
        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 注入接口实现
            if (uiManagerMono is IUIManager ui) uiManager = ui;
            if (networkClientMono is INetworkClient net) networkClient = net;
        }

        private void Start()
        {
            // 代码直接加载背景 Sprite（无需 Inspector 拖拽）
            if (gameBackground != null)      gameBackground.sprite      = ArtResourcePath.Load(ArtResourcePath.GameBg);
            if (underwaterBackground != null) underwaterBackground.sprite = ArtResourcePath.Load(ArtResourcePath.UnderwaterBg);
            if (skyBackground != null)        skyBackground.sprite        = ArtResourcePath.Load(ArtResourcePath.SkyBg);
            if (waterSurface != null)         waterSurface.sprite         = ArtResourcePath.Load(ArtResourcePath.WaterSurface);
            if (seabed != null)               seabed.sprite               = ArtResourcePath.Load(ArtResourcePath.Seabed);

            // 绑定事件
            if (hook != null)
            {
                hook.OnItemDelivered += HandleItemDelivered;
            }
            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged += (score) =>
                {
                    uiManager?.UpdateScore(score);
                };
                scoreManager.OnTargetReached += HandleTargetReached;
            }
            if (timerManager != null)
            {
                timerManager.OnTimeChanged += (t) =>
                {
                    uiManager?.UpdateTimer(t);
                };
                timerManager.OnTimeUp += HandleTimeUp;
            }
        }

        private void OnDestroy()
        {
            if (hook != null) hook.OnItemDelivered -= HandleItemDelivered;
        }

        /// <summary>开始新游戏（从第一关）</summary>
        public void StartNewGame()
        {
            currentLevelIndex = -1;
            StartNextLevel();
        }

        /// <summary>开始下一关</summary>
        public void StartNextLevel()
        {
            if (levelConfig == null)
            {
                Debug.LogError("[GameManager] LevelConfig未配置");
                return;
            }
            currentLevelIndex++;
            if (currentLevelIndex >= levelConfig.LevelCount)
            {
                SetState(GameState.GameOver);
                return;
            }
            var level = levelConfig.GetLevelByIndex(currentLevelIndex);
            StartLevel(level);
        }

        /// <summary>开始指定关卡</summary>
        public void StartLevel(LevelData level)
        {
            if (level == null || !level.Validate())
            {
                Debug.LogError("[GameManager] 关卡数据无效");
                return;
            }
            CurrentLevelData = level;
            currentLevelId = level.levelId;

            // 生成物品
            if (spawner != null) spawner.SpawnLevel(level);

            // 初始化分数
            if (scoreManager != null)
            {
                int carry = scoreManager.TotalMoney;
                scoreManager.InitLevel(level.targetMoney, carry);
            }

            // 重置渔夫与钩爪
            if (fisherman != null) fisherman.ResetFisherman();
            else if (hook != null) hook.ResetHook();

            // 显示HUD
            uiManager?.ShowHUD();
            uiManager?.UpdateTarget(level.targetMoney);
            uiManager?.UpdateScore(scoreManager != null ? scoreManager.CurrentMoney : 0);

            // 开始计时
            if (timerManager != null) timerManager.StartTimer(level.timeLimit);

            SetState(GameState.Playing);
        }

        /// <summary>重试当前关卡</summary>
        public void RetryCurrentLevel()
        {
            if (CurrentLevelData != null) StartLevel(CurrentLevelData);
        }

        /// <summary>暂停</summary>
        public void PauseGame()
        {
            if (state != GameState.Playing) return;
            if (timerManager != null) timerManager.Pause();
            if (hook != null) hook.SetLocked(true);
            if (fisherman != null) fisherman.InputEnabled = false;
            SetState(GameState.Paused);
        }

        /// <summary>恢复</summary>
        public void ResumeGame()
        {
            if (state != GameState.Paused) return;
            if (timerManager != null) timerManager.Resume();
            if (hook != null) hook.SetLocked(false);
            if (fisherman != null) fisherman.InputEnabled = true;
            SetState(GameState.Playing);
        }

        /// <summary>使用炸弹释放当前物品</summary>
        public void UseBomb()
        {
            if (state != GameState.Playing) return;
            if (fisherman != null) fisherman.UseBomb();
        }

        private void HandleItemDelivered(CatchableItem item)
        {
            if (item == null) return;
            if (scoreManager != null)
            {
                scoreManager.AddMoney(item.BaseValue);
            }
            // 销毁已交付的物品
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }

        private void HandleTargetReached()
        {
            // 达标后仍可继续抓直到时间结束
            Debug.Log("[GameManager] 已达标");
        }

        private void HandleTimeUp()
        {
            // 时间到，判断是否达标
            bool success = scoreManager != null && scoreManager.IsTargetReached;
            int score = scoreManager != null ? scoreManager.CurrentMoney : 0;
            int target = CurrentLevelData != null ? CurrentLevelData.targetMoney : 0;

            // 上报分数
            networkClient?.ReportLevelResult(currentLevelId, score, success);

            if (success)
            {
                SetState(GameState.LevelComplete);
                uiManager?.ShowLevelComplete(currentLevelId, score, target);
            }
            else
            {
                SetState(GameState.LevelFailed);
                uiManager?.ShowLevelFailed(currentLevelId, score, target);
            }
        }

        private void SetState(GameState newState)
        {
            if (state == newState) return;
            state = newState;
            OnStateChanged?.Invoke(state);
        }

        /// <summary>注入UIManager（运行时）</summary>
        public void SetUIManager(IUIManager ui)
        {
            uiManager = ui;
        }

        /// <summary>注入NetworkClient（运行时）</summary>
        public void SetNetworkClient(INetworkClient net)
        {
            networkClient = net;
        }

        /// <summary>返回主菜单</summary>
        public void ReturnToMenu()
        {
            if (spawner != null) spawner.ClearItems();
            if (timerManager != null) timerManager.Reset();
            SetState(GameState.Menu);
        }
    }
}
