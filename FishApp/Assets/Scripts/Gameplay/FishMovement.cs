// FishMovement.cs
// 水中物品随机游动：在生成区域内随机改变方向，遇到边界反弹，被抓后停止移动。

using UnityEngine;

namespace FishGame
{
    public class FishMovement : MonoBehaviour
    {
        [Header("移动配置")]
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float dirChangeInterval = 2f;
        [SerializeField] private float dirChangeVariance = 1f;

        [Header("边界")]
        [SerializeField] private float boundLeft   = -8f;
        [SerializeField] private float boundRight  =  8f;
        [SerializeField] private float boundTop    =  1f;
        [SerializeField] private float boundBottom = -7f;

        [Header("翻转")]
        [SerializeField] private bool flipSpriteWithDirection = true;

        public float MoveSpeed => moveSpeed;

        private Vector2 moveDir;
        private float nextDirChangeTime;
        private SpriteRenderer sr;
        private CatchableItem item;
        private bool initialFlipX;

        private void Awake()
        {
            sr   = GetComponent<SpriteRenderer>();
            item = GetComponent<CatchableItem>();
            if (sr != null) initialFlipX = sr.flipX;
            PickRandomDirection();
        }

        private void Start()
        {
            Debug.Log($"[FishMovement] Start on {gameObject.name}, speed={moveSpeed}, dir={moveDir}");
        }

        private void Update()
        {
            if (item != null && item.IsCaught) return;

            if (Time.time >= nextDirChangeTime)
                PickRandomDirection();

            Vector3 pos = transform.position;
            pos.x += moveDir.x * moveSpeed * Time.deltaTime;
            pos.y += moveDir.y * moveSpeed * Time.deltaTime;

            if (pos.x <= boundLeft)   { pos.x = boundLeft;   moveDir.x =  Mathf.Abs(moveDir.x); }
            if (pos.x >= boundRight)  { pos.x = boundRight;  moveDir.x = -Mathf.Abs(moveDir.x); }
            if (pos.y >= boundTop)    { pos.y = boundTop;    moveDir.y = -Mathf.Abs(moveDir.y); }
            if (pos.y <= boundBottom) { pos.y = boundBottom;  moveDir.y =  Mathf.Abs(moveDir.y); }

            transform.position = pos;

            if (flipSpriteWithDirection && sr != null)
                sr.flipX = moveDir.x < 0 ? !initialFlipX : initialFlipX;
        }

        private void PickRandomDirection()
        {
            float angle = Random.Range(0f, 360f);
            moveDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            nextDirChangeTime = Time.time + dirChangeInterval + Random.Range(-dirChangeVariance, dirChangeVariance);
        }

        public void Setup(float speed, Rect bounds)
        {
            moveSpeed    = speed;
            boundLeft    = bounds.xMin;
            boundRight   = bounds.xMax;
            boundTop     = bounds.yMax;
            boundBottom  = bounds.yMin;
            PickRandomDirection();
        }
    }
}
