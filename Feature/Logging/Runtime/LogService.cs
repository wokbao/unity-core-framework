using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Feature.Logging.Abstractions;
using Core.Feature.Logging.ScriptableObjects;
using R3;

namespace Core.Feature.Logging.Runtime
{
    /// <summary>
    /// 日志服务默认实现：负责等级过滤、广播日志并分发到各个接收器。
    /// </summary>
    public sealed class LogService : ILogService, IDisposable
    {
        private readonly IList<ILogSink> logSinks;
        private readonly Subject<LogEntry> subject;
        private readonly LogLevel globalMinimumLevel;
        private readonly Dictionary<LogCategory, CategoryRule> categoryRules;

        public Observable<LogEntry> LogStream => subject;

        public LogService(
            IEnumerable<ILogSink> sinks,
            LoggingConfig config)
        {
            logSinks = new List<ILogSink>(sinks ?? Array.Empty<ILogSink>());
            subject = new Subject<LogEntry>();

            globalMinimumLevel = config != null
                ? config.minimumLogLevel
                : LogLevel.Debug;

            categoryRules = BuildCategoryRules(config);
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
            // 全局等级过滤
            if (level < globalMinimumLevel)
            {
                return;
            }

            // 分类规则过滤（如果有）
            if (categoryRules.TryGetValue(category, out var rule))
            {
                if (!rule.Enabled || level < rule.MinimumLevel)
                {
                    return;
                }
            }

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

            var count = logSinks.Count;
            for (var index = 0; index < count; index += 1)
            {
                logSinks[index].Write(entry);
            }
        }

        private static Dictionary<LogCategory, CategoryRule> BuildCategoryRules(LoggingConfig config)
        {
            var result = new Dictionary<LogCategory, CategoryRule>();

            if (config?.categoryOverrides == null)
            {
                return result;
            }

            foreach (var @override in config.categoryOverrides)
            {
                if (result.ContainsKey(@override.category))
                {
                    continue;
                }

                result[@override.category] = new CategoryRule(
                    @override.enabled,
                    @override.minimumLogLevel);
            }

            return result;
        }

        private readonly struct CategoryRule
        {
            public readonly bool Enabled;
            public readonly LogLevel MinimumLevel;

            public CategoryRule(bool enabled, LogLevel minimumLevel)
            {
                Enabled = enabled;
                MinimumLevel = minimumLevel;
            }
        }

        public void Dispose()
        {
            subject?.OnCompleted();
            subject?.Dispose();
        }
    }
}
