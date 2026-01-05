using System;
using System.Collections.Generic;
using Core.Feature.Logging.Abstractions;
using Core.Feature.TimeManagement.Abstractions;
using UnityEngine;
using VContainer.Unity;

namespace Core.Feature.TimeManagement.Runtime
{
    /// <summary>
    /// 时间管理器默认实现
    /// </summary>
    /// <remarks>
    /// 实现 <see cref="ITickable"/> 以便 VContainer 每帧调用 Tick 更新计时器
    /// </remarks>
    public sealed class TimeManager : ITimeManager, ITickable, IDisposable
    {
        private readonly ILogService _logService;
        private readonly List<Timer> _activeTimers = new();
        private readonly List<Timer> _timersToRemove = new();

        private float _timeScaleBeforePause = 1f;
        private bool _isPaused;

        public float TimeScale
        {
            get => UnityEngine.Time.timeScale;
            set
            {
                if (Math.Abs(UnityEngine.Time.timeScale - value) > 0.001f)
                {
                    UnityEngine.Time.timeScale = value;
                    OnTimeScaleChanged?.Invoke(value);
                }
            }
        }

        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;
        public bool IsPaused => _isPaused;

        public event Action<bool> OnPauseChanged;
        public event Action<float> OnTimeScaleChanged;

        public TimeManager(ILogService logService)
        {
            _logService = logService;
            _logService?.Information(LogCategory.Core, "时间管理器初始化完成");
        }

        public void Pause()
        {
            if (_isPaused) return;

            _timeScaleBeforePause = TimeScale;
            TimeScale = 0f;
            _isPaused = true;

            OnPauseChanged?.Invoke(true);
            _logService?.Debug(LogCategory.Core, "游戏已暂停");
        }

        public void Resume()
        {
            if (!_isPaused) return;

            TimeScale = _timeScaleBeforePause;
            _isPaused = false;

            OnPauseChanged?.Invoke(false);
            _logService?.Debug(LogCategory.Core, "游戏已恢复");
        }

        public ITimer CreateTimer(float duration, Action onComplete, bool useUnscaledTime = false)
        {
            if (duration <= 0)
                throw new ArgumentException("计时器持续时间必须大于 0", nameof(duration));

            var timer = new Timer(duration, onComplete, false, useUnscaledTime);
            _activeTimers.Add(timer);
            return timer;
        }

        public ITimer CreateRepeatingTimer(float interval, Action onTick, bool useUnscaledTime = false)
        {
            if (interval <= 0)
                throw new ArgumentException("计时器间隔必须大于 0", nameof(interval));

            var timer = new Timer(interval, onTick, true, useUnscaledTime);
            _activeTimers.Add(timer);
            return timer;
        }

        public void CancelAllTimers()
        {
            foreach (var timer in _activeTimers)
            {
                timer.Cancel();
            }
            _activeTimers.Clear();
            _logService?.Debug(LogCategory.Core, "所有计时器已取消");
        }

        /// <summary>
        /// VContainer 每帧调用
        /// </summary>
        public void Tick()
        {
            if (_activeTimers.Count == 0) return;

            _timersToRemove.Clear();

            foreach (var timer in _activeTimers)
            {
                if (!timer.IsRunning)
                {
                    if (timer.IsCompleted || timer.IsCancelled)
                    {
                        _timersToRemove.Add(timer);
                    }
                    continue;
                }

                timer.Update();

                if (timer.IsCompleted && !timer.IsRepeating)
                {
                    _timersToRemove.Add(timer);
                }
            }

            foreach (var timer in _timersToRemove)
            {
                _activeTimers.Remove(timer);
            }
        }

        public void Dispose()
        {
            CancelAllTimers();
            OnPauseChanged = null;
            OnTimeScaleChanged = null;
        }

        #region Timer Implementation

        private sealed class Timer : ITimer
        {
            private readonly float _duration;
            private readonly Action _callback;
            private readonly bool _useUnscaledTime;

            private float _elapsedTime;
            private bool _isRunning = true;
            private bool _isCompleted;
            private bool _isCancelled;

            public float RemainingTime => Mathf.Max(0f, _duration - _elapsedTime);
            public float ElapsedTime => _elapsedTime;
            public bool IsRunning => _isRunning && !_isCompleted && !_isCancelled;
            public bool IsCompleted => _isCompleted;
            public bool IsRepeating { get; }
            public bool IsCancelled => _isCancelled;

            public Timer(float duration, Action callback, bool isRepeating, bool useUnscaledTime)
            {
                _duration = duration;
                _callback = callback;
                IsRepeating = isRepeating;
                _useUnscaledTime = useUnscaledTime;
            }

            public void Update()
            {
                if (!IsRunning) return;

                var delta = _useUnscaledTime
                    ? UnityEngine.Time.unscaledDeltaTime
                    : UnityEngine.Time.deltaTime;

                _elapsedTime += delta;

                if (_elapsedTime >= _duration)
                {
                    _callback?.Invoke();

                    if (IsRepeating)
                    {
                        _elapsedTime -= _duration;
                    }
                    else
                    {
                        _isCompleted = true;
                        _isRunning = false;
                    }
                }
            }

            public void Pause()
            {
                _isRunning = false;
            }

            public void Resume()
            {
                if (!_isCompleted && !_isCancelled)
                {
                    _isRunning = true;
                }
            }

            public void Cancel()
            {
                _isCancelled = true;
                _isRunning = false;
            }

            public void Reset()
            {
                _elapsedTime = 0f;
                _isCompleted = false;
                _isCancelled = false;
                _isRunning = true;
            }

            public void Dispose()
            {
                Cancel();
            }
        }

        #endregion
    }
}
