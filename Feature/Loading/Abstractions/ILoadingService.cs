using System;

namespace Core.Feature.Loading.Abstractions
{
    /// <summary>
    /// 加载系统服务接口，支持嵌套计数、进度报告、生命周期钩子和加载阶段管理。
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
        /// 当前加载阶段名称（如 "正在加载资源..."、"正在初始化场景..." 等）。
        /// </summary>
        string CurrentPhase { get; }

        /// <summary>
        /// 状态变更回调（进度、描述、IsLoading 等任一变更时触发）。
        /// </summary>
        event Action<LoadingState> OnStateChanged;

        /// <summary>
        /// 首次开始加载时触发（ActiveOperations 从 0 变为 1）。
        /// </summary>
        event Action OnLoadingStarted;

        /// <summary>
        /// 所有加载操作完成时触发（ActiveOperations 从 N 变为 0）。
        /// </summary>
        event Action OnLoadingCompleted;

        /// <summary>
        /// 加载阶段变更时触发（BeginPhase/EndPhase 调用时）。
        /// </summary>
        event Action<string> OnPhaseChanged;

        /// <summary>
        /// 加载过程中发生错误时触发。
        /// </summary>
        event Action<Exception> OnLoadingError;

        /// <summary>
        /// 开启一个加载作用域，Dispose 时会自动结束（适用于嵌套计数）。
        /// <param name="mode">加载模式（前台阻塞/后台静默）</param>
        /// </summary>
        IDisposable Begin(string description = null, LoadingMode mode = LoadingMode.Foreground);

        /// <summary>
        /// 报告进度并可选更新描述。
        /// </summary>
        void ReportProgress(float progress, string description = null);

        /// <summary>
        /// 创建一个进度回调，自动同步到 LoadingService，并可联动外部进度。
        /// </summary>
        IProgress<float> CreateProgressReporter(string description = null, IProgress<float> linkedProgress = null);

        /// <summary>
        /// 开始一个新的加载阶段（用于细粒度进度追踪，如 "加载资源"、"初始化场景" 等）。
        /// </summary>
        void BeginPhase(string phaseName);

        /// <summary>
        /// 结束当前加载阶段。
        /// </summary>
        void EndPhase(string phaseName);

        /// <summary>
        /// 报告加载错误（会触发 OnLoadingError 事件）。
        /// </summary>
        void ReportError(Exception exception);
    }
}

