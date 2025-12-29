using System;
using System.Runtime.CompilerServices;
using R3;

namespace Core.Feature.Logging.Abstractions
{
    public interface ILogService
    {
        /// <summary>
        /// 日志流（R3 使用 Observable，不同于 IObservable）。
        /// </summary>
        Observable<LogEntry> LogStream { get; }

        void Debug(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0);

        void Information(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0);

        void Warning(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0);

        void Error(
            LogCategory category,
            string message,
            Exception exception = null,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0);

        void Critical(
            LogCategory category,
            string message,
            Exception exception = null,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0);
    }
}
