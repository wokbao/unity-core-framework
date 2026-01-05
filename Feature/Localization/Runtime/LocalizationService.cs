using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Feature.Localization.Abstractions;
using Core.Feature.Logging.Abstractions;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

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

        public Locale CurrentLocale => LocalizationSettings.SelectedLocale;

        public IReadOnlyList<Locale> AvailableLocales =>
            (IReadOnlyList<Locale>)LocalizationSettings.AvailableLocales?.Locales
            ?? Array.Empty<Locale>();

        public event Action<Locale> OnLocaleChanged;

        /// <summary>
        /// 初始化本地化服务
        /// </summary>
        /// <param name="logService">日志服务</param>
        public LocalizationService(ILogService logService)
        {
            _logService = logService;
            _defaultTableName = FallbackTableName;

            // 订阅 Unity Localization 事件
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            _isInitialized = true;

            // 调试：显示当前选中的语言
            var currentLocale = LocalizationSettings.SelectedLocale;
            _logService?.Information(LogCategory.Core,
                $"本地化服务初始化完成，默认表：{_defaultTableName}，当前语言：{currentLocale?.LocaleName ?? "未选择"}");
        }

        /// <inheritdoc/>
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

        private void HandleLocaleChanged(Locale newLocale)
        {
            _logService?.Information(LogCategory.Core, $"语言已变更：{newLocale?.LocaleName ?? "null"}");
            OnLocaleChanged?.Invoke(newLocale);
        }

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
