// HookController.cs
// 钩爪控制器：黄金矿工核心机制。
// 三个状态：Swinging(钟摆摆动) / Extending(伸出) / Retracting(收回)
// 钟摆以渔夫位置为支点，按下空格/点击后钩爪沿当前方向直线伸出，
// 碰到物品或边界后收回，收回速度受物品重量影响。

using System;
using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 钩爪状态枚举。
    /// </summary>
    public enum HookState
    {
        Swinging,   // 钟摆摆动中
        Extending,  // 向下伸出
        Retracting  // 收回
    }

    /// <summary>
    /// 钩爪控制器。挂在钩爪GameObject上，需要以渔夫位置为旋转支点。
    /// 推荐结构：Fisherman(空物体) -> Hook(挂本脚本)
    /// Hook以FishFather位置为锚点做钟摆运动。
    /// </summary>
    public class HookController : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("渔夫/支点Transform（钩爪绕此点摆动，收回时回到此点）")]
        [SerializeField] private Transform pivot;

        [Tooltip("钩爪自身Sprite/碰撞体所在的子物体（用于拉伸长度）")]
        [SerializeField] private Transform hookHead;

        [Header("美术资源")]
        [Tooltip("钩爪头 SpriteRenderer")]
        [SerializeField] private SpriteRenderer hookHeadRenderer;
        [Tooltip("钩爪锁链 SpriteRenderer")]
        [SerializeField] private SpriteRenderer chainRenderer;

        // Sprite 通过代码直接加载
        private Sprite hookSprite;
        private Sprite chainSprite;

        [Header("钟摆配置")]
        [Tooltip("最大摆动角度（度，相对竖直向下方向），建议80")]
        [SerializeField] private float swingAngleRange = 80f;

        [Tooltip("摆动角速度（度/秒）")]
        [SerializeField] private float swingSpeed = 90f;

        [Tooltip("摆动初始方向（1=向右，-1=向左）")]
        [SerializeField] private float initialSwingDir = 1f;

        [Header("伸出/收回配置")]
        [Tooltip("钩爪基础伸出速度（单位/秒）")]
        [SerializeField] private float extendSpeed = 12f;

        [Tooltip("钩爪基础收回速度（单位/秒，无物品时）")]
        [SerializeField] private float retractSpeed = 12f;

        [Tooltip("钩爪最小长度（距离支点）")]
        [SerializeField] private float minLength = 1.0f;

        [Tooltip("钩爪最大长度（屏幕边界检测用，超过此长度强制收回）")]
        [SerializeField] private float maxLength = 12f;

        [Tooltip("重量对收回速度影响系数：retractSpeed / (1 + weightFactor * caughtItem.Weight)")]
        [SerializeField] private float weightFactor = 1.0f;

        [Tooltip("收回速度下限（防止极重物品卡死）")]
        [SerializeField] private float minRetractSpeed = 1.5f;

        [Header("当前状态（只读）")]
        [SerializeField] private HookState state = HookState.Swinging;

        // —— 内部状态 ——
        private float currentAngle;         // 当前摆动角度（度，0=竖直向下）
        private float swingDir;             // 当前摆动方向
        private float currentLength;        // 当前钩爪伸出长度
        private CatchableItem caughtItem;   // 当前抓到的物品
        private bool isLocked = false;      // 是否被锁定（非Swinging状态不接受发射输入）

        /// <summary>当前状态</summary>
        public HookState State => state;

        /// <summary>当前抓到的物品（可能为null）</summary>
        public CatchableItem CaughtItem => caughtItem;

        /// <summary>是否处于可发射状态（Swinging且未锁定）</summary>
        public bool CanLaunch => state == HookState.Swinging && !isLocked;

        /// <summary>当钩爪抓到物品时触发（参数：物品）</summary>
        public event Action<CatchableItem> OnItemCaught;

        /// <summary>当钩爪收回到渔夫位置并交付物品时触发（参数：物品，可能为null）</summary>
        public event Action<CatchableItem> OnItemDelivered;

        private void Awake()
        {
            if (pivot == null) pivot = transform.parent;
            if (hookHead == null) hookHead = transform;
            currentAngle = 0f;
            swingDir = initialSwingDir >= 0 ? 1f : -1f;
            currentLength = minLength;
            ApplySprites();
            UpdateTransform();
        }

        /// <summary>应用钩爪和锁链 Sprite 到 SpriteRenderer（从 Resources 加载）</summary>
        private void ApplySprites()
        {
            hookSprite  = ArtResourcePath.Load(ArtResourcePath.Hook);
            chainSprite = ArtResourcePath.Load(ArtResourcePath.Chain);
            if (hookHeadRenderer != null && hookSprite != null)
                hookHeadRenderer.sprite = hookSprite;
            if (chainRenderer != null && chainSprite != null)
                chainRenderer.sprite = chainSprite;
        }

        private void Update()
        {
            switch (state)
            {
                case HookState.Swinging:
                    UpdateSwinging();
                    break;
                case HookState.Extending:
                    UpdateExtending();
                    break;
                case HookState.Retracting:
                    UpdateRetracting();
                    break;
            }
            UpdateTransform();
        }

        /// <summary>
        /// 发射钩爪（从Swinging切换到Extending）。
        /// 由FishermanController调用。
        /// </summary>
        public void Launch()
        {
            if (state != HookState.Swinging) return;
            state = HookState.Extending;
            currentLength = minLength;
        }

        /// <summary>
        /// 强制立即释放当前抓到的物品（用于炸弹道具）。
        /// </summary>
        public void ReleaseCaughtItem()
        {
            if (caughtItem != null)
            {
                caughtItem.OnReleased();
                // 将物品从钩爪上移除
                caughtItem.transform.SetParent(null);
                caughtItem = null;
            }
        }

        private void UpdateSwinging()
        {
            currentAngle += swingDir * swingSpeed * Time.deltaTime;
            if (currentAngle >= swingAngleRange)
            {
                currentAngle = swingAngleRange;
                swingDir = -1f;
            }
            else if (currentAngle <= -swingAngleRange)
            {
                currentAngle = -swingAngleRange;
                swingDir = 1f;
            }
            currentLength = minLength;
        }

        private void UpdateExtending()
        {
            currentLength += extendSpeed * Time.deltaTime;
            if (currentLength >= maxLength)
            {
                // 到达边界，开始收回（未抓到物品）
                currentLength = maxLength;
                state = HookState.Retracting;
            }
        }

        private void UpdateRetracting()
        {
            float speed = retractSpeed;
            if (caughtItem != null)
            {
                speed = retractSpeed / (1f + weightFactor * caughtItem.Weight);
                if (speed < minRetractSpeed) speed = minRetractSpeed;
            }
            currentLength -= speed * Time.deltaTime;

            if (currentLength <= minLength)
            {
                currentLength = minLength;
                // 收回到位，结算
                var delivered = caughtItem;
                if (delivered != null)
                {
                    // 将物品从钩爪脱离
                    delivered.transform.SetParent(null);
                    delivered.OnReleased();
                    caughtItem = null;
                }
                state = HookState.Swinging;
                OnItemDelivered?.Invoke(delivered);
            }
        }

        /// <summary>
        /// 更新钩爪 Transform：绕支点摆动，hookHead 沿方向伸出，Chain 动态拉伸填充两者之间。
        /// </summary>
        private void UpdateTransform()
        {
            if (pivot == null) return;

            // Hook 自身始终跟在支点上，只做旋转
            transform.position = pivot.position;
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);

            // hookHead 沿 Hook 局部 -Y 方向（即竖直向下+旋转后的方向）伸出 currentLength
            Vector3 extendDir = transform.up * -1f;  // 局部 -Y 在世界空间的朝向
            Vector3 hookHeadPos = pivot.position + extendDir * currentLength;

            if (hookHead != null && hookHead != transform)
                hookHead.position = hookHeadPos;

            // Chain：从支点到 hookHead 之间拉伸，长度 = currentLength，朝向随旋转
            if (chainRenderer != null)
            {
                chainRenderer.transform.position = pivot.position + extendDir * (currentLength * 0.5f);
                chainRenderer.transform.rotation = transform.rotation;

                // 宽度用 sprite 的实际宽高比保持一致，避免变形
                float spriteW = chainRenderer.size.x;
                if (chainRenderer.sprite != null)
                {
                    float ratio = chainRenderer.sprite.rect.width / chainRenderer.sprite.rect.height;
                    // 16/64 = 0.25，以目标高度反推合适宽度
                    // 用 PPU 换算：sprite宽 px / PPU = 世界单位宽
                    spriteW = chainRenderer.sprite.rect.width / chainRenderer.sprite.pixelsPerUnit;
                }
                chainRenderer.size = new Vector2(spriteW, currentLength);
            }
        }

        /// <summary>
        /// 碰撞检测：钩爪碰到可捕获物品时抓取。
        /// 要求hookHead上挂有Collider2D(trigger)，且物品也挂有Collider2D(trigger)。
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (state != HookState.Extending) return;
            if (caughtItem != null) return; // 已抓到物品，不再抓

            var item = other.GetComponent<CatchableItem>();
            if (item == null) return;
            if (item.IsCaught) return;

            // 抓取
            caughtItem = item;
            item.OnCaught();
            // 将物品父节点设为钩爪头，跟随收回
            if (hookHead != null)
            {
                item.transform.SetParent(hookHead);
            }
            else
            {
                item.transform.SetParent(transform);
            }
            item.transform.localPosition = Vector3.zero;

            // 切换到收回状态
            state = HookState.Retracting;
            OnItemCaught?.Invoke(item);
        }

        /// <summary>
        /// 重新初始化钩爪状态（关卡开始/重置时调用）。
        /// </summary>
        public void ResetHook()
        {
            if (caughtItem != null)
            {
                caughtItem.OnReleased();
                caughtItem.transform.SetParent(null);
                caughtItem = null;
            }
            state = HookState.Swinging;
            currentAngle = 0f;
            swingDir = initialSwingDir >= 0 ? 1f : -1f;
            currentLength = minLength;
            isLocked = false;
            UpdateTransform();
        }

        /// <summary>锁定/解锁钩爪（暂停时使用）</summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }
    }
}
