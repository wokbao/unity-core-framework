using Core.Feature.AssetManagement.Runtime;
using Core.Feature.Loading.Abstractions;
using Core.Feature.Loading.Runtime;
using Core.Feature.Logging.Abstractions;
using Core.Feature.Logging.Runtime;
using Core.Feature.ObjectPooling.Abstractions;
using Core.Feature.ObjectPooling.Runtime;
using Core.Feature.SceneManagement.Abstractions;
using Core.Feature.SceneManagement.Runtime;
using Core.Feature.EventBus.Abstractions;
using Core.Feature.EventBus.Runtime;
using Core.Runtime.Configuration;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 模块的根 LifetimeScope，注册跨系统的基础设施服务。
    /// </summary>
    public sealed class CoreLifetimeScope : LifetimeScope
    {
        [Header("核心配置")]
        [SerializeField]
        [Tooltip("核心配置清单，定义启动时需要加载的基础设施配置")]
        private ConfigManifest _coreConfigManifest;

        protected override void Configure(IContainerBuilder builder)
        {
            // 0. 加载并注册所有核心配置（同步阻塞）
            ConfigLoadResult configResult = null;
            if (_coreConfigManifest != null)
            {
                configResult = ConfigLoader.LoadFromManifest(_coreConfigManifest);
                ConfigRegistry.RegisterToContainer(builder, configResult);
            }
            else
            {
                Debug.LogWarning("[CoreLifetimeScope] 未设置核心配置清单，跳过配置加载");
            }

            // 1. 注册基础设施服务
            // 日志系统
            builder.Register<UnityLogSink>(Lifetime.Singleton)
                .As<ILogSink>();
            builder.Register<LogService>(Lifetime.Singleton)
                .As<ILogService>();

            // 对象池
            builder.Register<ObjectPoolManager>(Lifetime.Singleton)
                .As<IObjectPoolManager>();

            // 事件总线
            builder.Register<EventBus>(Lifetime.Singleton)
                .As<IEventBus>();

            // 加载状态
            builder.Register<LoadingService>(Lifetime.Singleton)
                .As<ILoadingService>();

            // 资源管理
            builder.Register<AddressablesAssetProvider>(Lifetime.Singleton)
                .As<IAssetProvider>();

            // 场景管理
            builder.Register<SceneService>(Lifetime.Singleton)
                .As<ISceneService>();

            // 2. 核心服务初始化器
            builder.RegisterEntryPoint<CoreBootstrapper>();
        }
    }
}
