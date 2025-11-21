using UnityEngine;
using Core.Feature.Logging.Abstractions;

namespace Core.Feature.Logging.ScriptableObjects
{
    /// <summary>
    /// 日志系统配置文件。
    /// 控制日志等级、输出模式、过滤规则等。
    /// 所有日志服务初始化时会读取本配置。
    /// </summary>
    [CreateAssetMenu(
        fileName = "LoggingConfig",
        menuName = "Core/Logging/LoggingConfig",
        order = 1)]
    public sealed class LoggingConfig : ScriptableObject
    {
        [Header("最小日志等级")]
        [Tooltip("低于此等级的日志将被忽略")]
        public LogLevel minimumLogLevel = LogLevel.Debug;

        [Header("是否输出到 Unity 控制台 (UnityLogSink)")]
        public bool enableUnityConsoleOutput = true;

        [Header("是否输出到文件（未来可扩展 FileLogSink）")]
        public bool enableFileOutput = false;

        [Header("是否输出到外部（如网络、调试面板）")]
        public bool enableExternalOutput = false;
    }
}
