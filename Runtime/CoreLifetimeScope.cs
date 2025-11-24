using Core.Feature.AssetManagement.Runtime;
using Core.Feature.Logging.Runtime;
using VContainer;
using VContainer.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 模块的根 LifetimeScope，负责注册跨系统的基础设施。
    /// </summary>
    public sealed class CoreLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IAssetProvider, AddressablesAssetProvider>(Lifetime.Singleton);
            builder.Register<LoggingInstaller>(Lifetime.Singleton);
        }
    }
}
