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
    /// 实现了<see cref="ILogService"/>接口，提供了多级别、多类别的日志记录功能。
    /// </summary>
    public sealed class LogService : ILogService, IDisposable
    {
        /// <summary>
        /// 存储所有日志接收器的列表
        /// </summary>
        private readonly IList<ILogSink> _logSinks;

        /// <summary>
        /// 用于广播日志条目的Reactive Subject
        /// </summary>
        private readonly Subject<LogEntry> _subject;

        /// <summary>
        /// 全局最小日志等级，低于此等级的日志将被过滤
        /// </summary>
        private readonly LogLevel _globalMinimumLevel;

        /// <summary>
        /// 按日志类别存储的特定规则字典
        /// </summary>
        private readonly Dictionary<LogCategory, CategoryRule> _categoryRules;

        /// <summary>
        /// 标记服务是否已被释放
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 获取可观察的日志流，允许订阅所有日志条目
        /// </summary>
        public Observable<LogEntry> LogStream => _subject;

        /// <summary>
        /// 初始化LogService实例
        /// </summary>
        /// <param name="sinks">日志接收器集合，用于处理日志输出</param>
        /// <param name="config">日志配置，包含全局设置和类别覆盖规则</param>
        public LogService(
            IEnumerable<ILogSink> sinks,
            LoggingConfig config)
        {
            _logSinks = new List<ILogSink>(sinks ?? Array.Empty<ILogSink>());
            _subject = new Subject<LogEntry>();

            _globalMinimumLevel = config != null
                ? config.minimumLogLevel
                : LogLevel.Debug;

            _categoryRules = BuildCategoryRules(config);
        }

        /// <summary>
        /// 记录Debug级别日志
        /// </summary>
        /// <param name="category">日志类别</param>
        /// <param name="message">日志消息</param>
        /// <param name="callerPath">调用者文件路径（自动填充）</param>
        /// <param name="callerMemberName">调用者方法名（自动填充）</param>
        /// <param name="callerLineNumber">调用者行号（自动填充）</param>
        public void Debug(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Debug, category, message, null, callerPath, callerMemberName, callerLineNumber);

        /// <summary>
        /// 记录Information级别日志
        /// </summary>
        /// <param name="category">日志类别</param>
        /// <param name="message">日志消息</param>
        /// <param name="callerPath">调用者文件路径（自动填充）</param>
        /// <param name="callerMemberName">调用者方法名（自动填充）</param>
        /// <param name="callerLineNumber">调用者行号（自动填充）</param>
        public void Information(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Information, category, message, null, callerPath, callerMemberName, callerLineNumber);

        /// <summary>
        /// 记录Warning级别日志
        /// </summary>
        /// <param name="category">日志类别</param>
        /// <param name="message">日志消息</param>
        /// <param name="callerPath">调用者文件路径（自动填充）</param>
        /// <param name="callerMemberName">调用者方法名（自动填充）</param>
        /// <param name="callerLineNumber">调用者行号（自动填充）</param>
        public void Warning(
            LogCategory category,
            string message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Warning, category, message, null, callerPath, callerMemberName, callerLineNumber);

        /// <summary>
        /// 记录Error级别日志
        /// </summary>
        /// <param name="category">日志类别</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">关联的异常对象（可选）</param>
        /// <param name="callerPath">调用者文件路径（自动填充）</param>
        /// <param name="callerMemberName">调用者方法名（自动填充）</param>
        /// <param name="callerLineNumber">调用者行号（自动填充）</param>
        public void Error(
            LogCategory category,
            string message,
            Exception exception = null,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Error, category, message, exception, callerPath, callerMemberName, callerLineNumber);

        /// <summary>
        /// 记录Critical级别日志
        /// </summary>
        /// <param name="category">日志类别</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">关联的异常对象（可选）</param>
        /// <param name="callerPath">调用者文件路径（自动填充）</param>
        /// <param name="callerMemberName">调用者方法名（自动填充）</param>
        /// <param name="callerLineNumber">调用者行号（自动填充）</param>
        public void Critical(
            LogCategory category,
            string message,
            Exception exception = null,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
            => WriteInternal(LogLevel.Critical, category, message, exception, callerPath, callerMemberName, callerLineNumber);

        /// <summary>
        /// 内部日志写入方法，处理日志过滤和分发
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="category">日志类别</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">关联的异常对象（可选）</param>
        /// <param name="callerPath">调用者文件路径</param>
        /// <param name="callerMemberName">调用者方法名</param>
        /// <param name="callerLineNumber">调用者行号</param>
        private void WriteInternal(
            LogLevel level,
            LogCategory category,
            string message,
            Exception exception = null,
            string callerPath = "",
            string callerMemberName = "",
            int callerLineNumber = 0)
        {
            // 已释放保护：避免在应用退出时访问已释放的 Subject
            if (_isDisposed)
            {
                return;
            }

            // 全局等级过滤
            if (level < _globalMinimumLevel)
            {
                return;
            }

            // 分类规则过滤（如果有）
            if (_categoryRules.TryGetValue(category, out var rule))
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

            // 广播日志条目到可观察流
            _subject.OnNext(entry);

            // 将日志条目分发到所有注册的接收器
            var count = _logSinks.Count;
            for (var index = 0; index < count; index += 1)
            {
                _logSinks[index].Write(entry);
            }
        }

        /// <summary>
        /// 从配置构建类别规则字典
        /// </summary>
        /// <param name="config">日志配置对象</param>
        /// <returns>构建好的类别规则字典</returns>
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

        /// <summary>
        /// 表示特定日志类别的过滤规则
        /// </summary>
        private readonly struct CategoryRule
        {
            /// <summary>
            /// 是否启用此类别
            /// </summary>
            public readonly bool Enabled;

            /// <summary>
            /// 此类别允许的最小日志级别
            /// </summary>
            public readonly LogLevel MinimumLevel;

            /// <summary>
            /// 初始化类别规则
            /// </summary>
            /// <param name="enabled">是否启用</param>
            /// <param name="minimumLevel">最小日志级别</param>
            public CategoryRule(bool enabled, LogLevel minimumLevel)
            {
                Enabled = enabled;
                MinimumLevel = minimumLevel;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _subject?.OnCompleted();
            _subject?.Dispose();
        }
    }
}
