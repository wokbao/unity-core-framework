using Core.Feature.AssetManagement.Runtime;
using Core.Feature.Logging.Abstractions;
using Core.Feature.Logging.Runtime;
using Core.Feature.SceneManagement.Abstractions;
using Core.Feature.SceneManagement.Runtime;
using Core.Runtime.Configuration;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 模块的根 LifetimeScope
    /// 
    /// <para><b>职责范围</b>：</para>
    /// 注册跨系统、跨场景的基础设施服务，这些服务：
    /// <list type="bullet">
    ///   <item>不依赖具体游戏业务逻辑</item>
    ///   <item>可以被所有子容器（Game、Menu、Gameplay）访问</item>
    ///   <item>生命周期通常为 Singleton，贯穿整个应用生命周期</item>
    /// </list>
    /// 
    /// <para><b>与其他 LifetimeScope 的关系</b>：</para>
    /// 通过 Unity Inspector 配置，作为 GameLifetimeScope 的父容器，
    /// 使得游戏层和场景层可以访问所有基础设施服务。
    /// 
    /// <para><b>配置管理</b>：</para>
    /// <list type="bullet">
    ///   <item>使用 ConfigManifest 统一管理所有核心配置</item>
    ///   <item>配置在服务注册前完成加载和验证</item>
    ///   <item>添加新配置无需修改代码，仅需在清单中配置</item>
    /// </list>
    /// </summary>
    public sealed class CoreLifetimeScope : LifetimeScope
    {
        [Header("核心配置")]
        [SerializeField]
        [Tooltip("核心配置清单，定义所有需要在启动时加载的基础设施配置")]
        private ConfigManifest _coreConfigManifest;

        protected override void Configure(IContainerBuilder builder)
        {
            // ========================================
            // 0. 加载并注册所有核心配置（同步阻塞）
            // ========================================
            // 说明：
            // 1. ConfigLoader 内部使用 Addressables.LoadAssetAsync().WaitForCompletion()
            // 2. WaitForCompletion() 会阻塞当前线程，直到配置完全加载完成
            // 3. 虽然是阻塞的，但配置文件很小（几KB），加载时间可忽略（~1-5ms）
            // 4. 配置必须在服务注册之前完成，因为服务依赖配置（如 LogService 需要 LoggingConfig）
            // 5. 这种同步加载方式是合理的权衡：简单、可靠、性能影响小

            ConfigLoadResult configResult = null;

            if (_coreConfigManifest != null)
            {
                // 加载配置清单中定义的所有配置
                // 当前：LoggingConfig
                // 未来：ObjectPoolConfig, EventBusConfig 等（仅需在清单中添加，无需修改代码）
                configResult = ConfigLoader.LoadFromManifest(_coreConfigManifest);

                // 将加载的配置注册到 DI 容器
                // 使配置可以通过构造函数注入到服务中
                ConfigRegistry.RegisterToContainer(builder, configResult);

                // 执行到这里时，所有配置已经：
                // ✓ 从 Addressables 加载完成
                // ✓ 注册到 DI 容器
                // ✓ 可以被后续的服务注入使用
            }
            else
            {
                Debug.LogWarning("[CoreLifetimeScope] 未设置核心配置清单，跳过配置加载");
            }

            // ========================================
            // 1. 注册基础设施服务
            // ========================================
            // 重要：所有配置已在上面加载完成，现在可以安全注册依赖配置的服务

            // 日志系统：日志记录、过滤、分发
            // 注册日志输出接收器（Unity 控制台）
            builder.Register<UnityLogSink>(Lifetime.Singleton)
                .As<ILogSink>();

            // 注册核心日志服务
            // 依赖：LoggingConfig（已在步骤 0 中加载并注册）
            builder.Register<LogService>(Lifetime.Singleton)
                .As<ILogService>();

            // 资源管理：Addressables 资源加载、缓存与释放
            // 用途：统一的资源加载入口，支持异步加载、预加载、实例化等
            // 依赖：ILogService（用于记录资源加载日志）
            builder.Register<AddressablesAssetProvider>(Lifetime.Singleton)
                .As<IAssetProvider>();

            // 场景管理：场景异步加载与卸载、加载进度追踪
            // 用途：统一的场景加载入口，支持 Addressables 场景管理
            // 依赖：ILogService（用于记录场景加载日志）
            builder.Register<SceneService>(Lifetime.Singleton)
                .As<ISceneService>();

            // ========================================
            // 2. 核心服务初始化器
            // ========================================
            // 在所有服务注册完成后，注册启动器来初始化这些服务
            // CoreBootstrapper 会在容器构建完成后自动执行 Start() 方法
            // 依赖：所有上面注册的服务
            builder.RegisterEntryPoint<CoreBootstrapper>();
            // ========================================
            // TODO: 待补充的基础设施服务
            // ========================================

            // TODO: 输入管理（InputManager / InputService）
            // - 统一的输入抽象层，支持键盘、鼠标、触摸、手柄等
            // - 输入事件的订阅与分发
            // - 输入映射和重绑定支持
            // builder.Register<IInputService, InputService>(Lifetime.Singleton);

            // TODO: 持久化/存档系统（SaveSystem / PersistenceService）
            // - 玩家数据的保存与加载
            // - 支持本地存储、云存档等
            // - 数据序列化/反序列化
            // builder.Register<ISaveService, SaveService>(Lifetime.Singleton);

            // TODO: 配置管理（ConfigManager）
            // - 游戏配置表的加载与访问
            // - 支持热更新配置
            // - 配置数据缓存
            // builder.Register<IConfigManager, ConfigManager>(Lifetime.Singleton);

            // TODO: 事件总线（EventBus / MessageBroker）
            // - 全局事件的发布与订阅
            // - 解耦系统间的通信
            // - 支持事件优先级和过滤
            // builder.Register<IEventBus, EventBus>(Lifetime.Singleton);

            // TODO: 时间管理（TimeManager）
            // - 游戏时间缩放（暂停、加速、慢动作）
            // - 计时器管理
            // - 帧率独立的时间系统
            // builder.Register<ITimeManager, TimeManager>(Lifetime.Singleton);

            // TODO: 场景管理（SceneLoader / SceneService）
            // - 场景异步加载与卸载
            // - 加载进度追踪
            // - 场景过渡效果管理
            // builder.Register<ISceneService, SceneService>(Lifetime.Singleton);

            // TODO: 网络服务（NetworkService）- 如果是联网游戏
            // - HTTP/WebSocket 通信
            // - 网络请求队列和重试
            // - 连接状态管理
            // builder.Register<INetworkService, NetworkService>(Lifetime.Singleton);

            // TODO: 本地化/多语言（LocalizationService）
            // - 多语言文本管理
            // - 运行时语言切换
            // - 本地化资源加载
            // builder.Register<ILocalizationService, LocalizationService>(Lifetime.Singleton);

            // TODO: 对象池（ObjectPoolManager）
            // - GameObject 和普通对象的池化管理
            // - 自动扩容和收缩
            // - 性能优化，减少 GC
            // builder.Register<IObjectPoolManager, ObjectPoolManager>(Lifetime.Singleton);

            // ========================================
            // 注意事项
            // ========================================
            // 1. 所有注册的服务应该是无状态的或状态独立的
            // 2. 避免在 Core 层注册游戏业务逻辑相关的服务
            // 3. 优先使用接口注册，便于测试和替换实现
            // 4. 考虑服务之间的依赖关系，必要时使用工厂模式

            //
        }
    }
}
