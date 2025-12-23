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
        [Header("核心配置")]
        [SerializeField]
        [Tooltip("核心配置清单，定义启动时需要加载的基础设施配置")]
        private ConfigManifest _coreConfigManifest;

        [Header("场景过渡配置")]
        [SerializeField]
        [Tooltip("可选：配置过渡开关、方案与参数；为空则使用默认配置")]
        private SceneTransitionConfig _sceneTransitionConfig;

        /// <summary>
        /// 确保 CoreLifetimeScope 在场景切换时不被销毁
        /// </summary>
        protected override void Awake()
        {
            // 核心服务必须在整个应用生命周期内持续存在
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CoreLifetimeScope] 已标记为 DontDestroyOnLoad，核心服务将持久化");

            // 调用基类 Awake 完成 VContainer 初始化
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            Debug.Log("[CoreLifetimeScope] === Configure 开始执行 ===");

            // 0. 强制从缓存加载核心配置（必须由 Splash 场景预加载）
            ConfigLoadResult configResult = ConfigCache.GetCoreConfigs();

            if (configResult == null)
            {
                // 如果缓存为空，说明没有执行 Splash 场景预加载
#if UNITY_EDITOR
                // 编辑器模式：允许降级到同步加载（开发便利性）
                Debug.LogWarning("[CoreLifetimeScope] 配置缓存为空，降级到同步加载（仅开发模式）");
                Debug.LogWarning("[CoreLifetimeScope] 正式版本必须从 Game_Splash 场景启动！");

                if (_coreConfigManifest != null)
                {
                    configResult = ConfigLoader.LoadFromManifest(_coreConfigManifest);

                    // 同时缓存，避免其他组件重复加载
                    if (configResult != null)
                    {
                        ConfigCache.SetCoreConfigs(configResult);
                    }
                }
                else
                {
                    Debug.LogError("[CoreLifetimeScope] 核心配置清单未设置，无法初始化");
                    return;
                }
#else
                // 运行时：缓存为空是严重错误
                throw new System.InvalidOperationException(
                    "核心配置未加载！游戏必须从 Game_Splash 场景启动。\n" +
                    "如果是打包版本出现此错误，请检查 Build Settings 中 Game_Splash 是否为第一个场景。"
                );
#endif
            }

            // 注册配置到容器
            if (configResult != null)
            {
                ConfigRegistry.RegisterToContainer(builder, configResult);
            }
            else
            {
                Debug.LogError("[CoreLifetimeScope] 配置加载失败，核心服务将无法正常工作");
                return;
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

            // 加载性能遥测
            builder.Register<LoadingTelemetry>(Lifetime.Singleton)
                .As<ILoadingTelemetry>();

            // 加载状态
            builder.Register<LoadingService>(Lifetime.Singleton)
                .As<ILoadingService>();

            // 资源管理
            builder.Register<AddressablesAssetProvider>(Lifetime.Singleton)
                .As<IAssetProvider>();

            // 场景管理
            var sceneTransitionConfig = _sceneTransitionConfig != null
                ? _sceneTransitionConfig
                : SceneTransitionConfig.Default;

            builder.RegisterInstance(sceneTransitionConfig);

            builder.Register<NoSceneTransition>(Lifetime.Singleton)
                .As<ISelectableSceneTransition>();

            builder.Register<SceneCinematicTransition>(Lifetime.Singleton)
                .WithParameter("barHeightRatio", sceneTransitionConfig.barHeightRatio)
                .WithParameter("barCloseDuration", sceneTransitionConfig.barCloseDuration)
                .WithParameter("barOpenDuration", sceneTransitionConfig.barOpenDuration)
                .WithParameter("fadeOutDuration", sceneTransitionConfig.cinematicFadeOutDuration)
                .WithParameter("fadeInDuration", sceneTransitionConfig.cinematicFadeInDuration)
                .WithParameter("overlayColor", sceneTransitionConfig.OverlayColor)
                .WithParameter("sortingOrder", sceneTransitionConfig.sortingOrder)
                .As<ISelectableSceneTransition>();

            builder.Register<SceneFadeTransition>(Lifetime.Singleton)
                .WithParameter("fadeOutDuration", sceneTransitionConfig.fadeOutDuration)
                .WithParameter("fadeInDuration", sceneTransitionConfig.fadeInDuration)
                .WithParameter("overlayColor", sceneTransitionConfig.OverlayColor)
                .WithParameter("sortingOrder", sceneTransitionConfig.sortingOrder)
                .As<ISelectableSceneTransition>()
                .AsSelf();

            builder.Register<SceneShutterTransition>(Lifetime.Singleton)
                .As<ISelectableSceneTransition>();

            builder.Register<SceneNoiseTransition>(Lifetime.Singleton)
                .As<ISelectableSceneTransition>();

            builder.Register<SceneTransitionSelector>(Lifetime.Singleton)
                .As<ISceneTransition>();

            builder.Register<SceneService>(Lifetime.Singleton)
                .As<ISceneService>();

            // ========================================
            // 注意：不再需要 EntryPoint
            // ========================================
            // CoreBootstrapper 和 StartupRunner 已废弃
            // VContainer 会自动初始化所有注册的服务
            // Splash 场景的 SplashBootstrapper 负责启动流程
        }
    }
}
