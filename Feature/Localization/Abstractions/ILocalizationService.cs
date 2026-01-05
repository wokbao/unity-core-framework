using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

namespace Core.Feature.Localization.Abstractions
{
    /// <summary>
    /// 提供本地化文本的服务接口。
    /// </summary>
    /// <remarks>
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>根据 Key 获取本地化文本</item>
    ///   <item>支持运行时语言切换</item>
    ///   <item>支持 Smart String 变量替换</item>
    /// </list>
    /// </remarks>
    public interface ILocalizationService
    {
        /// <summary>
        /// 当前选中的 Locale。
        /// </summary>
        Locale CurrentLocale { get; }

        /// <summary>
        /// 当语言切换时触发。
        /// </summary>
        event Action<Locale> OnLocaleChanged;

        /// <summary>
        /// 根据 Key 获取本地化文本。
        /// </summary>
        /// <param name="tableReference">String Table 引用（表名或 GUID）</param>
        /// <param name="entryKey">条目 Key</param>
        /// <param name="args">格式化参数（支持 Smart String）</param>
        /// <returns>格式化后的本地化文本</returns>
        string GetText(string tableReference, string entryKey, params object[] args);

        /// <summary>
        /// 使用默认表获取本地化文本（简化 API）。
        /// </summary>
        /// <param name="key">条目 Key</param>
        /// <param name="args">格式化参数</param>
        /// <returns>本地化文本</returns>
        string GetText(string key, params object[] args);

        /// <summary>
        /// 异步获取本地化文本。
        /// </summary>
        /// <param name="tableReference">String Table 引用</param>
        /// <param name="entryKey">条目 Key</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>本地化文本</returns>
        UniTask<string> GetTextAsync(string tableReference, string entryKey, CancellationToken ct = default);

        /// <summary>
        /// 异步获取本地化文本（使用默认表）。
        /// </summary>
        /// <param name="key">条目 Key</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>本地化文本</returns>
        UniTask<string> GetTextAsync(string key, CancellationToken ct = default);

        /// <summary>
        /// 异步切换语言。
        /// </summary>
        /// <param name="localeCode">Locale 代码（如 "zh-CN", "en"）</param>
        /// <param name="ct">取消令牌</param>
        UniTask SetLocaleAsync(string localeCode, CancellationToken ct = default);

        /// <summary>
        /// 获取所有可用的 Locale。
        /// </summary>
        System.Collections.Generic.IReadOnlyList<Locale> AvailableLocales { get; }
    }
}
