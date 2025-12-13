using System.Collections.Generic;

namespace Core.Runtime.Configuration
{
    /// <summary>
    /// 通用的配置清单接口，Core/Game 均可实现，便于复用加载器。
    /// </summary>
    public interface IConfigManifest
    {
        IReadOnlyList<ConfigManifest.ConfigEntry> Entries { get; }
    }
}
