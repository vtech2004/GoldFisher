// ScoreManager.cs
// 分数/金钱管理：当前金钱、目标金钱、累计金钱，添加金钱与达标判断，事件通知。

using System;
using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 分数/金钱管理器。
    /// 负责本关金钱累计、目标判断、累计金钱统计。
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("当前状态（只读）")]
        [SerializeField] private int currentMoney = 0;
        [SerializeField] private int targetMoney = 0;
        [SerializeField] private int totalMoney = 0;

        /// <summary>当前关卡金钱</summary>
        public int CurrentMoney => currentMoney;

        /// <summary>当前关卡目标金钱</summary>
        public int TargetMoney => targetMoney;

        /// <summary>累计金钱（跨关卡）</summary>
        public int TotalMoney => totalMoney;

        /// <summary>是否已达标</summary>
        public bool IsTargetReached => currentMoney >= targetMoney;

        /// <summary>金钱变化事件（参数：新当前金钱）</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>达标事件</summary>
        public event Action OnTargetReached;

        private bool targetReachedNotified = false;

        /// <summary>初始化关卡金钱</summary>
        public void InitLevel(int target, int carryOverTotal)
        {
            targetMoney = Mathf.Max(0, target);
            currentMoney = 0;
            totalMoney = Mathf.Max(0, carryOverTotal);
            targetReachedNotified = false;
            OnScoreChanged?.Invoke(currentMoney);
        }

        /// <summary>添加金钱</summary>
        public void AddMoney(int amount)
        {
            if (amount == 0) return;
            currentMoney = Mathf.Max(0, currentMoney + amount);
            totalMoney = Mathf.Max(0, totalMoney + amount);
            OnScoreChanged?.Invoke(currentMoney);

            if (!targetReachedNotified && IsTargetReached)
            {
                targetReachedNotified = true;
                OnTargetReached?.Invoke();
            }
        }

        /// <summary>扣除金钱（用于购买道具等）</summary>
        public bool SpendMoney(int amount)
        {
            if (currentMoney < amount) return false;
            currentMoney -= amount;
            OnScoreChanged?.Invoke(currentMoney);
            return true;
        }

        /// <summary>重置当前关卡金钱（保留累计）</summary>
        public void ResetCurrent()
        {
            currentMoney = 0;
            targetReachedNotified = false;
            OnScoreChanged?.Invoke(currentMoney);
        }

        /// <summary>设置新目标</summary>
        public void SetTarget(int target)
        {
            targetMoney = Mathf.Max(0, target);
            targetReachedNotified = false;
        }
    }
}
