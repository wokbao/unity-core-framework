using System;

namespace Core.Feature.Loading.Abstractions
{
    /// <summary>
    /// 加载系统服务接口，支持嵌套计数、进度与描述文本。
    /// </summary>
    public interface ILoadingService
    {
        /// <summary>
        /// 当前加载状态快照。
        /// </summary>
        LoadingState State { get; }

        /// <summary>
        /// 是否存在正在进行的加载操作。
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// 当前进度（0~1）。
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// 当前描述文本。
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 当前活动加载操作数量（支持嵌套或并行计数）。
        /// </summary>
        int ActiveOperations { get; }

        /// <summary>
        /// 状态变更回调。
        /// </summary>
        event Action<LoadingState> OnStateChanged;

        /// <summary>
        /// 开启一个加载作用域，Dispose 时会自动结束（适用于嵌套计数）。
        /// </summary>
        IDisposable Begin(string description = null);

        /// <summary>
        /// 报告进度并可选更新描述。
        /// </summary>
        void ReportProgress(float progress, string description = null);

        /// <summary>
        /// 创建一个进度回调，自动同步到 LoadingService，并可联动外部进度。
        /// </summary>
        IProgress<float> CreateProgressReporter(string description = null, IProgress<float> linkedProgress = null);
    }
}
