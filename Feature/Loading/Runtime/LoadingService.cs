using System;
using System.Collections.Generic;
using System.Threading;
using Core.Feature.Loading.Abstractions;
using UnityEngine;

namespace Core.Feature.Loading.Runtime
{
    /// <summary>
    /// 默认加载服务实现，支持嵌套计数、进度报告、生命周期钩子和性能遥测。
    /// </summary>
    public sealed class LoadingService : ILoadingService, IDisposable
    {
        private readonly object _lock = new();
        private readonly ILoadingTelemetry _telemetry;
        private readonly Dictionary<string, float> _phaseStartTimes = new();
        private int _activeOperations;
        private int _activeForegroundOperations;
        private float _progress;
        private string _description;
        private string _currentPhase;

        public LoadingState State => new(IsLoading, ShouldShowUi, Progress, Description, ActiveOperations);

        public bool IsLoading => ActiveOperations > 0;
        public bool ShouldShowUi => ActiveForegroundOperations > 0;
        public float Progress => _progress;
        public string Description => _description;
        public int ActiveOperations => Volatile.Read(ref _activeOperations);
        public int ActiveForegroundOperations => Volatile.Read(ref _activeForegroundOperations);
        public string CurrentPhase => _currentPhase;

        public event Action<LoadingState> OnStateChanged;
        public event Action OnLoadingStarted;
        public event Action OnLoadingCompleted;
        public event Action<string> OnPhaseChanged;
        public event Action<Exception> OnLoadingError;

        public LoadingService(ILoadingTelemetry telemetry = null)
        {
            _telemetry = telemetry;
        }

        public IDisposable Begin(string description = null, LoadingMode mode = LoadingMode.Foreground)
        {
            var operationId = Guid.NewGuid().ToString();
            var prevOps = Interlocked.Increment(ref _activeOperations) - 1;

            if (mode == LoadingMode.Foreground)
            {
                Interlocked.Increment(ref _activeForegroundOperations);
            }

            _telemetry?.RecordLoadingStart(operationId, description);

            if (prevOps == 0)
            {
                // 从 0 变为 1，首次开始加载
                OnLoadingStarted?.Invoke();
            }

            UpdateState(Progress, description, publish: true);
            return new LoadingScope(this, operationId, Time.realtimeSinceStartup, mode);
        }

        // ... (ReportProgress, CreateProgressReporter, BeginPhase, EndPhase, ReportError unchanged) ...
        public void ReportProgress(float progress, string description = null)
        {
            UpdateState(progress, description, publish: true);
        }

        public IProgress<float> CreateProgressReporter(string description = null, IProgress<float> linkedProgress = null)
        {
            return new Progress<float>(value =>
            {
                linkedProgress?.Report(value);
                UpdateState(value, description, publish: true);
            });
        }

        public void BeginPhase(string phaseName)
        {
            if (string.IsNullOrEmpty(phaseName))
            {
                Debug.LogWarning("尝试开始一个空阶段名称，已忽略");
                return;
            }

            lock (_lock)
            {
                _currentPhase = phaseName;
                _phaseStartTimes[phaseName] = Time.realtimeSinceStartup;
            }

            _telemetry?.RecordPhaseStart(phaseName);
            OnPhaseChanged?.Invoke(phaseName);
            UpdateState(Progress, phaseName, publish: true);
        }

        public void EndPhase(string phaseName)
        {
            if (string.IsNullOrEmpty(phaseName))
            {
                Debug.LogWarning("尝试结束一个空阶段名称，已忽略");
                return;
            }

            float duration = 0f;
            lock (_lock)
            {
                if (_currentPhase == phaseName)
                {
                    _currentPhase = null;
                }

                if (_phaseStartTimes.TryGetValue(phaseName, out var startTime))
                {
                    duration = Time.realtimeSinceStartup - startTime;
                    _phaseStartTimes.Remove(phaseName);
                }
            }

            _telemetry?.RecordPhaseEnd(phaseName, duration);
            OnPhaseChanged?.Invoke(null);
        }

        public void ReportError(Exception exception)
        {
            if (exception == null)
            {
                Debug.LogWarning("尝试报告 null 异常，已忽略");
                return;
            }

            Debug.LogError($"加载过程中发生错误：{exception.Message}\n{exception.StackTrace}");
            OnLoadingError?.Invoke(exception);
        }

        private void UpdateState(float progress, string description, bool publish)
        {
            lock (_lock)
            {
                _progress = Mathf.Clamp01(progress);
                if (!string.IsNullOrEmpty(description))
                {
                    _description = description;
                }

                if (!IsLoading)
                {
                    // 自动复位
                    _progress = 0f;
                    _description = null;
                    _currentPhase = null;
                    // _activeForegroundOperations should ideally be 0 if logic is correct
                }
            }

            if (publish)
            {
                OnStateChanged?.Invoke(State);
            }
        }

        private void EndScope(string operationId, float startTime, LoadingMode mode)
        {
            var remaining = Interlocked.Decrement(ref _activeOperations);
            if (remaining < 0)
            {
                Interlocked.Exchange(ref _activeOperations, 0);
                remaining = 0;
            }

            if (mode == LoadingMode.Foreground)
            {
                var remainingForeground = Interlocked.Decrement(ref _activeForegroundOperations);
                if (remainingForeground < 0)
                {
                    Interlocked.Exchange(ref _activeForegroundOperations, 0);
                }
            }

            float duration = Time.realtimeSinceStartup - startTime;
            _telemetry?.RecordLoadingEnd(operationId, duration);

            if (remaining == 0)
            {
                // 从 N 变为 0，所有加载完成
                OnLoadingCompleted?.Invoke();
            }

            UpdateState(Progress >= 1f ? 1f : Progress, Description, publish: true);
        }

        /// <summary>
        /// 释放资源，清理事件订阅。
        /// </summary>
        public void Dispose()
        {
            OnStateChanged = null;
            OnLoadingStarted = null;
            OnLoadingCompleted = null;
            OnPhaseChanged = null;
            OnLoadingError = null;
        }

        private sealed class LoadingScope : IDisposable
        {
            private LoadingService _owner;
            private string _operationId;
            private float _startTime;
            private LoadingMode _mode;

            public LoadingScope(LoadingService owner, string operationId, float startTime, LoadingMode mode)
            {
                _owner = owner;
                _operationId = operationId;
                _startTime = startTime;
                _mode = mode;
            }

            public void Dispose()
            {
                if (_owner != null)
                {
                    _owner.EndScope(_operationId, _startTime, _mode);
                    _owner = null;
                }
            }
        }
    }
}

