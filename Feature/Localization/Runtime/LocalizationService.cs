using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Feature.Localization.Abstractions;
using Core.Feature.Logging.Abstractions;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Core.Feature.Localization.Runtime
{
    /// <summary>
    /// 本地化服务 - Unity Localization Package 包装器。
    /// </summary>
    /// <remarks>
    /// <para><b>功能</b>：</para>
    /// <list type="bullet">
    ///   <item>包装 Unity Localization API</item>
    ///   <item>提供同步/异步文本获取</item>
    ///   <item>支持运行时语言切换</item>
    ///   <item>支持 Smart String 变量替换</item>
    /// </list>
    /// </remarks>
    public sealed class LocalizationService : ILocalizationService, IDisposable
    {
        /// <summary>
        /// 默认 String Table 名称（可通过构造函数覆盖）
        /// </summary>
        public const string FallbackTableName = "UIStrings";

        private readonly ILogService _logService;
        private readonly string _defaultTableName;
        private bool _isInitialized;

        /// <summary>
        /// 获取当前选中的语言区域 (Locale)。
        /// </summary>
        public Locale CurrentLocale => LocalizationSettings.SelectedLocale;

        /// <summary>
        /// 获取所有已配置的可用语言区域列表。
        /// <para>如果未配置，则返回空数组。</para>
        /// </summary>
        public IReadOnlyList<Locale> AvailableLocales =>
            (IReadOnlyList<Locale>)LocalizationSettings.AvailableLocales?.Locales
            ?? Array.Empty<Locale>();

        /// <summary>
        /// 当语言区域发生变更时触发的事件。
        /// </summary>
        public event Action<Locale> OnLocaleChanged;

        /// <summary>
        /// 初始化本地化服务
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="options">本地化配置选项（可选，为空时使用默认配置）</param>
        public LocalizationService(ILogService logService, ILocalizationOptions options = null)
        {
            _logService = logService;

            // 使用 Null Object Pattern，避免空检查
            var effectiveOptions = options ?? DefaultLocalizationOptions.Instance;
            _defaultTableName = effectiveOptions.DefaultTableName;

            // 订阅 Unity Localization 事件
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            _isInitialized = true;

            // 调试：显示当前选中的语言
            var currentLocale = LocalizationSettings.SelectedLocale;
            _logService?.Information(LogCategory.Core,
                $"本地化服务初始化完成，默认表：{_defaultTableName}，当前语言：{currentLocale?.LocaleName ?? "未选择"}");
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <para>实现行为：</para>
        /// <list type="bullet">
        ///   <item>如果 Table 或 Key 为空，记录警告并返回 Key。</item>
        ///   <item>如果未找到条目，记录调试日志并返回经过格式化的 Key。</item>
        ///   <item>如果发生异常，记录错误日志并返回经过格式化的 Key。</item>
        /// </list>
        /// </remarks>
        public string GetText(string tableReference, string entryKey, params object[] args)
        {
            if (string.IsNullOrEmpty(tableReference) || string.IsNullOrEmpty(entryKey))
            {
                _logService?.Warning(LogCategory.Core, $"本地化 Key 无效：table={tableReference}, key={entryKey}");
                return entryKey ?? string.Empty;
            }

            try
            {
                var entry = LocalizationSettings.StringDatabase.GetTableEntry(tableReference, entryKey);

                if (entry.Entry == null)
                {
                    _logService?.Debug(LogCategory.Core, $"本地化条目不存在：{tableReference}/{entryKey}");
                    return FormatFallback(entryKey, args);
                }

                var localizedString = entry.Entry.GetLocalizedString(args);
                return localizedString;
            }
            catch (Exception ex)
            {
                _logService?.Error(LogCategory.Core, $"获取本地化文本失败：{tableReference}/{entryKey}", ex);
                return FormatFallback(entryKey, args);
            }
        }

        /// <inheritdoc/>
        public string GetText(string key, params object[] args)
        {
            return GetText(_defaultTableName, key, args);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 异步获取文本，支持等待初始化和加载过程。
        /// </remarks>
        public async UniTask<string> GetTextAsync(string tableReference, string entryKey, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(tableReference) || string.IsNullOrEmpty(entryKey))
            {
                return entryKey ?? string.Empty;
            }

            try
            {
                ct.ThrowIfCancellationRequested();

                // 确保初始化完成
                await LocalizationSettings.InitializationOperation.ToUniTask(cancellationToken: ct);

                var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableReference, entryKey);
                var result = await operation.ToUniTask(cancellationToken: ct);

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService?.Error(LogCategory.Core, $"异步获取本地化文本失败：{tableReference}/{entryKey}", ex);
                return entryKey;
            }
        }

        /// <inheritdoc/>
        public UniTask<string> GetTextAsync(string key, CancellationToken ct = default)
        {
            return GetTextAsync(_defaultTableName, key, ct);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 切换语言是异步操作，会等待初始化完成并加载必要的资源。
        /// </remarks>
        public async UniTask SetLocaleAsync(string localeCode, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(localeCode))
            {
                throw new ArgumentException("Locale 代码不能为空", nameof(localeCode));
            }

            ct.ThrowIfCancellationRequested();

            // 确保初始化完成
            await LocalizationSettings.InitializationOperation.ToUniTask(cancellationToken: ct);

            var targetLocale = LocalizationSettings.AvailableLocales.Locales
                .FirstOrDefault(l => l.Identifier.Code == localeCode);

            if (targetLocale == null)
            {
                _logService?.Warning(LogCategory.Core, $"找不到 Locale：{localeCode}");
                return;
            }

            LocalizationSettings.SelectedLocale = targetLocale;
            _logService?.Information(LogCategory.Core, $"语言已切换为：{targetLocale.LocaleName}");
        }

        /// <inheritdoc/>
        public UniTask SetLocaleAsync(SupportedLocale locale, CancellationToken ct = default)
        {
            return SetLocaleAsync(locale.ToCode(), ct);
        }

        /// <summary>
        /// 释放资源，取消对 Unity Localization 事件的订阅。
        /// </summary>
        public void Dispose()
        {
            if (_isInitialized)
            {
                LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
                _isInitialized = false;
            }

            OnLocaleChanged = null;
        }

        #region Private Methods

        /// <summary>
        /// 处理 Unity Localization 的语言变更事件。
        /// </summary>
        private void HandleLocaleChanged(Locale newLocale)
        {
            _logService?.Information(LogCategory.Core, $"语言已变更：{newLocale?.LocaleName ?? "null"}");
            OnLocaleChanged?.Invoke(newLocale);
        }

        /// <summary>
        /// 当本地化失败时，尝试使用参数格式化 Key 本身作为回退显示。
        /// </summary>
        private static string FormatFallback(string key, object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return key;
            }

            try
            {
                return string.Format(key, args);
            }
            catch
            {
                return key;
            }
        }

        #endregion
    }
}
