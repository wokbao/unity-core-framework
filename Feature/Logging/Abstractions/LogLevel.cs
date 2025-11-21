namespace Core.Feature.Logging.Abstractions
{
    /// <summary>
    /// 日志等级，表示日志的严重程度。
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,          // 调试
        Information = 1,    // 信息
        Warning = 2,        // 警告
        Error = 3,          // 错误
        Critical = 4        // 致命错误
    }
}
