using System.Collections.Generic;

namespace Core.Feature.Loading.Abstractions
{
    /// <summary>
    /// 加载性能遥测接口，用于记录和分析加载操作的性能指标。
    /// </summary>
    public interface ILoadingTelemetry
    {
        /// <summary>
        /// 记录加载操作开始。
        /// </summary>
        /// <param name="operationId">操作唯一标识</param>
        /// <param name="description">操作描述</param>
        void RecordLoadingStart(string operationId, string description);

        /// <summary>
        /// 记录加载操作结束。
        /// </summary>
        /// <param name="operationId">操作唯一标识</param>
        /// <param name="duration">操作耗时（秒）</param>
        void RecordLoadingEnd(string operationId, float duration);

        /// <summary>
        /// 记录加载阶段开始。
        /// </summary>
        /// <param name="phaseName">阶段名称</param>
        void RecordPhaseStart(string phaseName);

        /// <summary>
        /// 记录加载阶段结束。
        /// </summary>
        /// <param name="phaseName">阶段名称</param>
        /// <param name="duration">阶段耗时（秒）</param>
        void RecordPhaseEnd(string phaseName, float duration);

        /// <summary>
        /// 获取当前加载性能指标。
        /// </summary>
        LoadingMetrics GetMetrics();

        /// <summary>
        /// 重置所有指标。
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// 加载性能指标数据。
    /// </summary>
    public struct LoadingMetrics
    {
        /// <summary>
        /// 总加载时间（秒）。
        /// </summary>
        public float TotalLoadingTime;

        /// <summary>
        /// 平均加载时间（秒）。
        /// </summary>
        public float AverageLoadingTime;

        /// <summary>
        /// 总加载操作数。
        /// </summary>
        public int TotalOperations;

        /// <summary>
        /// 当前活动操作数。
        /// </summary>
        public int ActiveOperations;

        /// <summary>
        /// 各阶段耗时统计（阶段名称 → 耗时秒数）。
        /// </summary>
        public Dictionary<string, float> PhaseTimings;

        public LoadingMetrics(float totalTime, float avgTime, int totalOps, int activeOps, Dictionary<string, float> phaseTimings)
        {
            TotalLoadingTime = totalTime;
            AverageLoadingTime = avgTime;
            TotalOperations = totalOps;
            ActiveOperations = activeOps;
            PhaseTimings = phaseTimings;
        }
    }
}
