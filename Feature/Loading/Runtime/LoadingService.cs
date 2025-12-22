using System;
using System.Threading;
using Core.Feature.Loading.Abstractions;
using UnityEngine;

namespace Core.Feature.Loading.Runtime
{
    /// <summary>
    /// 默认加载服务实现，支持嵌套计数与进度/描述同步。
    /// </summary>
    public sealed class LoadingService : ILoadingService, IDisposable
    {
        private readonly object _lock = new();
        private int _activeOperations;
        private float _progress;
        private string _description;

        public LoadingState State => new(IsLoading, Progress, Description, ActiveOperations);

        public bool IsLoading => ActiveOperations > 0;
        public float Progress => _progress;
        public string Description => _description;
        public int ActiveOperations => Volatile.Read(ref _activeOperations);

        public event Action<LoadingState> OnStateChanged;

        public IDisposable Begin(string description = null)
        {
            Interlocked.Increment(ref _activeOperations);
            UpdateState(Progress, description, publish: true);
            return new LoadingScope(this);
        }

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
                }
            }

            if (publish)
            {
                OnStateChanged?.Invoke(State);
            }
        }

        private void EndScope()
        {
            var remaining = Interlocked.Decrement(ref _activeOperations);
            if (remaining < 0)
            {
                Interlocked.Exchange(ref _activeOperations, 0);
            }

            UpdateState(Progress >= 1f ? 1f : Progress, Description, publish: true);
        }

        /// <summary>
        /// 释放资源，清理事件订阅。
        /// </summary>
        public void Dispose()
        {
            OnStateChanged = null;
        }

        private sealed class LoadingScope : IDisposable
        {
            private LoadingService _owner;

            public LoadingScope(LoadingService owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                _owner?.EndScope();
                _owner = null;
            }
        }
    }
}

