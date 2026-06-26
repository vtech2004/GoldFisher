// TimerManager.cs
// 计时器：倒计时管理，时间到触发事件，支持暂停/恢复。

using System;
using UnityEngine;

namespace FishGame
{
    /// <summary>
    /// 倒计时管理器。
    /// </summary>
    public class TimerManager : MonoBehaviour
    {
        [Header("状态（只读）")]
        [SerializeField] private float timeRemaining = 0f;
        [SerializeField] private bool isRunning = false;
        [SerializeField] private bool isPaused = false;

        /// <summary>剩余时间（秒）</summary>
        public float TimeRemaining => timeRemaining;

        /// <summary>是否运行中</summary>
        public bool IsRunning => isRunning;

        /// <summary>是否暂停</summary>
        public bool IsPaused => isPaused;

        /// <summary>剩余时间变化事件（参数：剩余秒数）</summary>
        public event Action<float> OnTimeChanged;

        /// <summary>时间到事件</summary>
        public event Action OnTimeUp;

        private bool timeUpNotified = false;

        private void Update()
        {
            if (!isRunning || isPaused) return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                isRunning = false;
                OnTimeChanged?.Invoke(0f);
                if (!timeUpNotified)
                {
                    timeUpNotified = true;
                    OnTimeUp?.Invoke();
                }
                return;
            }
            OnTimeChanged?.Invoke(timeRemaining);
        }

        /// <summary>开始倒计时</summary>
        public void StartTimer(float seconds)
        {
            timeRemaining = Mathf.Max(0f, seconds);
            isRunning = true;
            isPaused = false;
            timeUpNotified = false;
            OnTimeChanged?.Invoke(timeRemaining);
        }

        /// <summary>暂停</summary>
        public void Pause()
        {
            if (isRunning) isPaused = true;
        }

        /// <summary>恢复</summary>
        public void Resume()
        {
            isPaused = false;
        }

        /// <summary>停止</summary>
        public void Stop()
        {
            isRunning = false;
            isPaused = false;
        }

        /// <summary>重置并停止</summary>
        public void Reset()
        {
            isRunning = false;
            isPaused = false;
            timeRemaining = 0f;
            timeUpNotified = false;
        }

        /// <summary>增加时间（奖励道具）</summary>
        public void AddTime(float seconds)
        {
            timeRemaining += seconds;
            if (!isRunning && timeRemaining > 0f)
            {
                isRunning = true;
                timeUpNotified = false;
            }
            OnTimeChanged?.Invoke(timeRemaining);
        }
    }
}
