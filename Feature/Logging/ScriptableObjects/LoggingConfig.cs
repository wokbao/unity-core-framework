using UnityEngine;
using Core.Feature.Logging.Abstractions;

namespace Core.Feature.Logging.ScriptableObjects
{
    /// <summary>
    /// 日志系统配置文件，控制全局等级、分类过滤和输出通道。
    /// </summary>
    [CreateAssetMenu(
        fileName = "LoggingConfig",
        menuName = "Core/Logging/LoggingConfig",
        order = 1)]
    public sealed class LoggingConfig : ScriptableObject
    {
        [Header("全局最小等级")]
        [Tooltip("低于此等级的日志将被忽略")]
        public LogLevel minimumLogLevel = LogLevel.Debug;

        [Header("分类覆盖（可选）")]
        [Tooltip("为指定分类单独设置开关和最小等级")]
        public LogCategoryOverride[] categoryOverrides = new LogCategoryOverride[0];

        [Header("输出通道")]
        [Tooltip("是否输出到 Unity 控制台 (UnityLogSink)")]
        public bool enableUnityConsoleOutput = true;

        [Tooltip("是否输出到文件（如 FileLogSink）")] public bool enableFileOutput = false;

        [Tooltip("是否输出到外部（网络、调试面板等）")] public bool enableExternalOutput = false;
    }

    [System.Serializable]
    public struct LogCategoryOverride
    {
        [Tooltip("要配置的日志分类")] public LogCategory category;

        [Tooltip("是否启用该分类")] public bool enabled;

        [Tooltip("该分类的最小日志等级")] public LogLevel minimumLogLevel;
    }
}
