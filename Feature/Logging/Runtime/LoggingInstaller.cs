using Core.Feature.AssetManagement.Runtime;
using Core.Feature.Logging.Abstractions;
using Core.Feature.Logging.ScriptableObjects;
using Core.Runtime.Installation;
using UnityEngine;
using VContainer;

namespace Core.Feature.Logging.Runtime
{
    /// <summary>
    /// 日志系统安装器，负责将日志相关依赖注册到容器中
    /// 提供灵活的日志配置加载机制，支持多种配置来源和生命周期管理
    /// 可在不同的 LifetimeScope 中复用，确保日志系统的一致性
    /// </summary>
    public sealed class LoggingInstaller : IConfigInstaller<LoggingConfig, LoggingInstallerOptions>
    {
        /// <summary>
        /// 实际使用的日志配置实例
        /// 可能来自：
        /// 1. 构造函数传入的配置（如Inspector中拖拽）
        /// 2. 通过AssetProvider加载的Addressable资源
        /// </summary>
        public LoggingConfig Config { get; private set; }

        public LoggingInstallerOptions Options { get; }

        private readonly IAssetProvider assetProvider;

        // 默认构造：仅依赖 IAssetProvider，内部自动创建默认 Options（纯 Addressables 加载）
        public LoggingInstaller(IAssetProvider assetProvider)
            : this(assetProvider, null)
        {
        }

        [Inject]
        public LoggingInstaller(IAssetProvider assetProvider, LoggingInstallerOptions options)
        {
            this.assetProvider = assetProvider;
            Options = options ?? new LoggingInstallerOptions();
        }

        /// <summary>
        /// 将日志相关服务注册到依赖注入容器
        /// </summary>
        /// <param name="builder">容器构建器，用于注册服务和配置</param>
        public void Install(IContainerBuilder builder)
        {
            // 兜底：Inspector 没拖的话，通过资产提供器按 addressable key 拉取
            Config = Options?.Config;

            if (Config == null && !string.IsNullOrEmpty(Options?.AddressKey))
            {
                Config = LoadConfigBlocking();
            }

            if (Options != null)
            {
                Options.Config = Config;
            }

            if (Config != null)
            {
                // 注册日志配置实例到容器中，供其他服务使用
                builder.RegisterInstance(Config);
                // 成功加载配置时输出日志
                Debug.Log($"成功加载 LoggingConfig（key: {Options?.AddressKey}）");
            }

            // 根据配置启用 Unity 控制台输出
            if (Config == null || Config.enableUnityConsoleOutput)
            {
                // 注册Unity日志输出接收器，使用单例生命周期
                builder.Register<UnityLogSink>(Lifetime.Singleton)
                    .As<ILogSink>();
            }

            // TODO：如果以后有 FileLogSink，就在这里判断 enableFileOutput 再注册

            // 注册核心日志服务，使用单例生命周期，实现ILogService接口
            builder.Register<LogService>(Lifetime.Singleton)
                .As<ILogService>();

            // 暂时注释掉的日志面板注册
            // builder.RegisterComponentInHierarchy<LogPanel>();
        }

        /// <summary>
        /// 同步加载日志配置
        /// </summary>
        /// <returns>加载的日志配置实例，失败时返回null</returns>
        private LoggingConfig LoadConfigBlocking()
        {
            if (assetProvider == null)
            {
                Debug.LogWarning("未注入 IAssetProvider，无法通过 Addressables 加载 LoggingConfig。");
                return null;
            }

            try
            {
                return assetProvider.LoadAssetSync<LoggingConfig>(Options.AddressKey);
            }
            catch
            {
                Debug.LogWarning($"未找到 LoggingConfig（key: {Options.AddressKey}），日志系统将使用默认配置。");
                return null;
            }
        }
    }
}
