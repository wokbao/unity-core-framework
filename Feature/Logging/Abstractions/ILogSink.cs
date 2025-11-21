namespace Core.Feature.Logging.Abstractions
{
    /// <summary>
    /// 日志接收器接口。
    /// 用于将日志写入不同输出（Unity 控制台 / 文件 / 网络等）。
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// 向接收器写入一条日志。
        /// </summary>
        void Write(LogEntry entry);
    }
}
