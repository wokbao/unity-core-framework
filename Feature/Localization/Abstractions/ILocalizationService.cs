using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Feature.Localization.Abstractions
{
    /// <summary>
    /// 提供本地化文本的服务接口。
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// 根据 Key 获取本地化文本。
        /// </summary>
        /// <param name="key">文本 Key</param>
        /// <param name="args">格式化参数</param>
        /// <returns>格式化后的本地化文本</returns>
        string GetText(string key, params object[] args);

        /// <summary>
        /// 异步获取本地化文本（预留给远程配置或懒加载）。
        /// </summary>
        /// <param name="key">文本 Key</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>本地化文本</returns>
        UniTask<string> GetTextAsync(string key, CancellationToken ct = default);
    }
}
