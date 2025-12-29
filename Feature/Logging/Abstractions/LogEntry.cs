using System;

namespace Core.Feature.Logging.Abstractions
{
    /// <summary>
    /// 表示一条日志记录。
    /// </summary>
    public readonly struct LogEntry
    {
        /// <summary>
        /// 日志生成时间。
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 日志等级。
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// 日志类别（按功能域划分）。
        /// </summary>
        public LogCategory Category { get; }

        /// <summary>
        /// 日志内容。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 可选的异常对象。
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 日志调用者文件路径。
        /// </summary>
        public string CallerPath { get; }

        /// <summary>
        /// 日志调用者方法名。
        /// </summary>
        public string CallerMemberName { get; }

        /// <summary>
        /// 日志调用者所在行号。
        /// </summary>
        public int CallerLineNumber { get; }

        public LogEntry(
            DateTime timestamp,
            LogLevel level,
            LogCategory category,
            string message,
            Exception exception = null,
            string callerPath = "",
            string callerMemberName = "",
            int callerLineNumber = 0)
        {
            Timestamp = timestamp;
            Level = level;
            Category = category;
            Message = message ?? string.Empty;
            Exception = exception;
            CallerPath = callerPath ?? string.Empty;
            CallerMemberName = callerMemberName ?? string.Empty;
            CallerLineNumber = callerLineNumber;
        }

        /// <summary>
        /// 返回格式化后的日志文本。
        /// </summary>
        public override string ToString()
        {
            var location = string.IsNullOrEmpty(CallerMemberName)
                ? string.Empty
                : $"({CallerMemberName}:{CallerLineNumber})";

            if (Exception == null)
                return $"[{Timestamp:HH:mm:ss}][{Level}][{Category}] {Message} {location}".Trim();

            return $"[{Timestamp:HH:mm:ss}][{Level}][{Category}] {Message} {location}\n{Exception}".Trim();
        }
    }
}
