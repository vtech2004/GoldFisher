// FishermanController.cs
// 渔夫控制器：控制渔夫在屏幕顶部，处理玩家输入并发射钩爪，播放渔夫动画状态。

using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 渔夫动画状态。
    /// </summary>
    public enum FishermanAnimState
    {
        Idle,   // 空闲（钩爪摆动中）
        Casting,// 抛竿（钩爪伸出）
        Pulling // 拉杆（钩爪收回）
    }

    /// <summary>
    /// 渔夫控制器。挂在渔夫GameObject上。
    /// 负责输入处理、钩爪发射调度、动画状态切换。
    /// </summary>
    public class FishermanController : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("钩爪控制器")]
        [SerializeField] private HookController hook;

        [Tooltip("渔夫Animator（可选）")]
        [SerializeField] private Animator animator;

        [Header("美术资源")]
        [Tooltip("渔夫 SpriteRenderer")]
        [SerializeField] private SpriteRenderer fishermanRenderer;
        [Tooltip("小船 SpriteRenderer")]
        [SerializeField] private SpriteRenderer boatRenderer;

        // Sprite 通过代码直接加载，无需 Inspector 拖拽
        private Sprite fishermanIdleSprite;
        private Sprite fishermanSprite;
        private Sprite boatSprite;

        [Header("输入配置")]
        [Tooltip("发射键")]
        [SerializeField] private KeyCode launchKey = KeyCode.Space;

        [Tooltip("是否允许鼠标点击发射")]
        [SerializeField] private bool allowMouseClick = true;

        [Tooltip("是否允许输入（暂停时关闭）")]
        [SerializeField] private bool inputEnabled = true;

        [Header("动画参数名")]
        [SerializeField] private string animStateParam = "State";

        private FishermanAnimState currentAnim = FishermanAnimState.Idle;

        /// <summary>钩爪控制器引用</summary>
        public HookController Hook => hook;

        /// <summary>是否允许输入</summary>
        public bool InputEnabled
        {
            get => inputEnabled;
            set => inputEnabled = value;
        }

        private void Start()
        {
            // 代码直接加载美术 Sprite
            fishermanIdleSprite = ArtResourcePath.Load(ArtResourcePath.FishermanIdle);
            fishermanSprite     = ArtResourcePath.Load(ArtResourcePath.FishermanCast);
            boatSprite          = ArtResourcePath.Load(ArtResourcePath.Boat);

            if (fishermanRenderer != null && fishermanIdleSprite != null)
                fishermanRenderer.sprite = fishermanIdleSprite;
            if (boatRenderer != null && boatSprite != null)
                boatRenderer.sprite = boatSprite;
        }

        private void Update()
        {
            if (!inputEnabled) return;
            if (hook == null) return;

            // 检测发射输入
            if (hook.CanLaunch && (Input.GetKeyDown(launchKey) || (allowMouseClick && Input.GetMouseButtonDown(0))))
            {
                hook.Launch();
            }

            // 根据钩爪状态更新渔夫动画
            UpdateAnimState();
        }

        private void UpdateAnimState()
        {
            FishermanAnimState target;
            switch (hook.State)
            {
                case HookState.Swinging: target = FishermanAnimState.Idle; break;
                case HookState.Extending: target = FishermanAnimState.Casting; break;
                case HookState.Retracting: target = FishermanAnimState.Pulling; break;
                default: target = FishermanAnimState.Idle; break;
            }
            if (target != currentAnim)
            {
                currentAnim = target;
                // 切换渔夫 Sprite（空闲帧 / 拉杆帧）
                if (fishermanRenderer != null)
                {
                    switch (currentAnim)
                    {
                        case FishermanAnimState.Idle:
                            if (fishermanIdleSprite != null) fishermanRenderer.sprite = fishermanIdleSprite;
                            break;
                        case FishermanAnimState.Casting:
                        case FishermanAnimState.Pulling:
                            if (fishermanSprite != null) fishermanRenderer.sprite = fishermanSprite;
                            break;
                    }
                }
                if (animator != null)
                {
                    animator.SetInteger(animStateParam, (int)target);
                }
            }
        }

        /// <summary>外部强制发射钩爪（UI按钮调用）</summary>
        public void LaunchHook()
        {
            if (!inputEnabled || hook == null) return;
            if (hook.CanLaunch) hook.Launch();
        }

        /// <summary>使用炸弹释放当前抓到的物品</summary>
        public void UseBomb()
        {
            if (hook == null) return;
            hook.ReleaseCaughtItem();
        }

        /// <summary>重置渔夫状态</summary>
        public void ResetFisherman()
        {
            if (hook != null) hook.ResetHook();
            currentAnim = FishermanAnimState.Idle;
            if (animator != null) animator.SetInteger(animStateParam, (int)FishermanAnimState.Idle);
        }
    }
}
