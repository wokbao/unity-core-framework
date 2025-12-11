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
using Core.Runtime.Startup;
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
        [Header("核心配置")] [SerializeField] [Tooltip("核心配置清单，定义启动时需要加载的基础设施配置")]
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
            // 说明：保留两种过渡方案，默认启用影院式黑条方案；如需纯黑场淡入淡出，注释掉下行注册，启用下方 SceneFadeTransition 注册。
            // 差异与扩展建议：
            // - SceneCinematicTransition：上下黑条闭合 + 叠加淡入淡出，观感更“电影化”，无需美术资源即可用；可替换条高、速度、遮罩色，或换成自定义遮罩纹理。
            // - SceneFadeTransition（备用）：纯黑场淡入淡出，最简、无额外元素；可快速接入自定义遮罩贴图/噪点/光圈等素材，由美术替换 Image 颜色/材质即可。
            // - 后续优化：可抽象 ISceneTransition 实现工厂，根据场景/平台选择不同过渡；可把参数配置化（ScriptableObject）给美术/策划调整。
            builder.Register<SceneCinematicTransition>(Lifetime.Singleton)
                .WithParameter("barHeightRatio", 0.18f)
                .WithParameter("barCloseDuration", 0.35f)
                .WithParameter("barOpenDuration", 0.3f)
                .WithParameter("fadeOutDuration", 0.25f)
                .WithParameter("fadeInDuration", 0.25f)
                .WithParameter("overlayColor", new Color(0f, 0f, 0f, 0.95f))
                .WithParameter("sortingOrder", 8000)
                .As<ISceneTransition>();
            // 备用方案：纯黑场淡入淡出。若需切换方案，注释上方 SceneCinematicTransition，取消下行注释。
            // builder.Register<SceneFadeTransition>(Lifetime.Singleton)
            //     .WithParameter("fadeOutDuration", 0.35f)
            //     .WithParameter("fadeInDuration", 0.3f)
            //     .WithParameter("overlayColor", new Color(0f, 0f, 0f, 0.95f))
            //     .WithParameter("sortingOrder", 8000)
            //     .As<ISceneTransition>();
            builder.Register<SceneService>(Lifetime.Singleton)
                .As<ISceneService>();

            // 2. 核心服务初始化器
            builder.RegisterEntryPoint<CoreBootstrapper>();
            builder.RegisterEntryPoint<StartupRunner>();
        }
    }
}
