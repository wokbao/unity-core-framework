using System.Threading;
using Core.Feature.Localization.Abstractions;
using Cysharp.Threading.Tasks;

namespace Core.Feature.Localization.Runtime
{
    /// <summary>
    /// 本地化服务的默认实现。
    /// 当前版本仅做透传（Pass-through），直接返回 Key 或默认格式，待未来接入具体配置表。
    /// </summary>
    public sealed class LocalizationService : ILocalizationService
    {
        public string GetText(string key, params object[] args)
        {
            // TODO: 未来接入真实的本地化数据源
            // 目前直接返回 Key 作为 fallback，方便调试
            if (args != null && args.Length > 0)
            {
                return string.Format(key, args);
            }
            return key;
        }

        public UniTask<string> GetTextAsync(string key, CancellationToken ct = default)
        {
            return UniTask.FromResult(GetText(key));
        }
    }
}
