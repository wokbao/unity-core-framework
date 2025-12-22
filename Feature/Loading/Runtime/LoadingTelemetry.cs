using System;
using System.Collections.Generic;
using Core.Feature.Loading.Abstractions;
using UnityEngine;

namespace Core.Feature.Loading.Runtime
{
    /// <summary>
    /// 加载性能遥测服务实现。
    /// </summary>
    public sealed class LoadingTelemetry : ILoadingTelemetry
    {
        private readonly Dictionary<string, float> _operationStartTimes = new();
        private readonly Dictionary<string, float> _phaseStartTimes = new();
        private readonly Dictionary<string, float> _phaseTimings = new();

        private float _totalLoadingTime;
        private int _totalOperations;
        private int _activeOperations;

        public void RecordLoadingStart(string operationId, string description)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                Debug.LogWarning("尝试记录加载开始时提供了空的操作 ID，已忽略");
                return;
            }

            _operationStartTimes[operationId] = Time.realtimeSinceStartup;
            _activeOperations++;
        }

        public void RecordLoadingEnd(string operationId, float duration)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                Debug.LogWarning("尝试记录加载结束时提供了空的操作 ID，已忽略");
                return;
            }

            if (_operationStartTimes.TryGetValue(operationId, out var startTime))
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                _totalLoadingTime += elapsed;
                _totalOperations++;
                _activeOperations--;

                _operationStartTimes.Remove(operationId);
            }
            else
            {
                // 直接使用传入的 duration
                _totalLoadingTime += duration;
                _totalOperations++;
                _activeOperations = Mathf.Max(0, _activeOperations - 1);
            }
        }

        public void RecordPhaseStart(string phaseName)
        {
            if (string.IsNullOrEmpty(phaseName))
            {
                Debug.LogWarning("尝试记录阶段开始时提供了空的阶段名称，已忽略");
                return;
            }

            _phaseStartTimes[phaseName] = Time.realtimeSinceStartup;
        }

        public void RecordPhaseEnd(string phaseName, float duration)
        {
            if (string.IsNullOrEmpty(phaseName))
            {
                Debug.LogWarning("尝试记录阶段结束时提供了空的阶段名称，已忽略");
                return;
            }

            float elapsed = duration;

            if (_phaseStartTimes.TryGetValue(phaseName, out var startTime))
            {
                elapsed = Time.realtimeSinceStartup - startTime;
                _phaseStartTimes.Remove(phaseName);
            }

            if (_phaseTimings.ContainsKey(phaseName))
            {
                _phaseTimings[phaseName] += elapsed;
            }
            else
            {
                _phaseTimings[phaseName] = elapsed;
            }
        }

        public LoadingMetrics GetMetrics()
        {
            float avgTime = _totalOperations > 0 ? _totalLoadingTime / _totalOperations : 0f;

            return new LoadingMetrics(
                _totalLoadingTime,
                avgTime,
                _totalOperations,
                _activeOperations,
                new Dictionary<string, float>(_phaseTimings)
            );
        }

        public void Reset()
        {
            _operationStartTimes.Clear();
            _phaseStartTimes.Clear();
            _phaseTimings.Clear();
            _totalLoadingTime = 0f;
            _totalOperations = 0;
            _activeOperations = 0;
        }
    }
}
