using Core.Feature.AssetManagement.Runtime;
using Core.Feature.Logging.Runtime;
using Core.Feature.Logging.ScriptableObjects;
using Core.Runtime.Installation;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace Core.Bootstrap
{
    // 作为 Core_Bootstrap 常驻根 Scope，负责事件/日志/网络/配置等基础设施并为其他 Scope 提供父容器。
    public sealed class CoreLifetimeScope : LifetimeScope
    {
        [SerializeField] private LoggingConfig loggingConfig;
        [SerializeField] private string loggingConfigAddress = "LoggingConfig";

        protected override void Configure(IContainerBuilder builder)
        {
            // 注册资产提供器单例
            var provider = new AddressablesAssetProvider();
            builder.RegisterInstance(provider).As<IAssetProvider>();

            // 组装日志安装器配置并执行安装
            var options = new LoggingInstallerOptions
            {
                AddressKey = loggingConfigAddress,
                Config = loggingConfig
            };

            builder.RegisterInstance(options);
            builder.RegisterInstaller(() => new LoggingInstaller(provider, options));

            // 保存最终配置引用，避免重复加载
            loggingConfig = options.Config;

            // TODO: 其他 Core 系统注册
        }
    }
}
