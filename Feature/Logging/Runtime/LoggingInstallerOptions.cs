using Core.Feature.Logging.ScriptableObjects;

namespace Core.Feature.Logging.Runtime
{
    /// <summary>
    /// 日志安装器配置参数，便于在 DI 中统一传递。
    /// </summary>
    public sealed class LoggingInstallerOptions
    {
        public string AddressKey = "LoggingConfig";
        public LoggingConfig Config;
    }
}
