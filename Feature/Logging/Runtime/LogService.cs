using System;
using System.Collections.Generic;
using Core.Feature.Logging.Abstractions;
using Core.Feature.Logging.ScriptableObjects;
using R3;
using System.Runtime.CompilerServices;

namespace Core.Feature.Logging.Runtime
{
    /// <summary>
    /// 日志服务默认实现：负责等级过滤、广播日志并分发到各个接收器。
    /// </summary>
    public sealed class LogService : ILogService, IDisposable
    {
        private readonly IList<ILogSink> logSinks;
        private readonly Subject<LogEntry> subject;
        private readonly LogLevel minimumLogLevel;

        public Observable<LogEntry> LogStream => subject;

        public LogService(
            IEnumerable<ILogSink> sinks,
            LoggingConfig config)
        {
            logSinks = new List<ILogSink>(sinks ?? Array.Empty<ILogSink>());
            subject = new Subject<LogEntry>();

            minimumLogLevel = config != null
                ? config.minimumLogLevel
                : LogLevel.Debug;
        }

        public void Debug(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Debug, category, message, null, callerPath, callerMemberName, callerLineNumber);

        public void Information(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Information, category, message, null, callerPath, callerMemberName, callerLineNumber);

        public void Warning(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Warning, category, message, null, callerPath, callerMemberName, callerLineNumber);

        public void Error(
            LogCategory category,
            string message,
            Exception exception = null,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Error, category, message, exception, callerPath, callerMemberName, callerLineNumber);

        public void Critical(
            LogCategory category,
            string message,
            Exception exception = null,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Critical, category, message, exception, callerPath, callerMemberName, callerLineNumber);

        private void WriteInternal(
            LogLevel level,
            LogCategory category,
            string message,
            Exception exception = null,
            string callerPath = "",
            string callerMemberName = "",
            int callerLineNumber = 0)
        {
            if (level < minimumLogLevel)
                return;

            var entry = new LogEntry(
                DateTime.Now,
                level,
                category,
                message,
                exception,
                callerPath,
                callerMemberName,
                callerLineNumber);

            subject.OnNext(entry);

            foreach (var sink in logSinks)
            {
                sink.Write(entry);
            }
        }

        public void Dispose()
        {
            subject?.OnCompleted();
            subject?.Dispose();
        }
    }
}
