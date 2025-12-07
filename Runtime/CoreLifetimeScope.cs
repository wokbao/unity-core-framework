using Core.Feature.AssetManagement.Runtime;
using Core.Feature.Logging.Abstractions;
using Core.Feature.Logging.Runtime;
using Core.Feature.Logging.ScriptableObjects;
using Core.Feature.SceneManagement.Abstractions;
using Core.Feature.SceneManagement.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
    /// </summary>
    public sealed class CoreLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // ========================================
            // 0. 初始化 Addressables 并加载核心配置
            // ========================================
            // 注意：为了满足依赖注入（LogService 需要 LoggingConfig），我们需要在 Configure 阶段同步加载配置。
            // 虽然 Addressables 是异步的，但在启动阶段使用 WaitForCompletion 是可接受的权衡。

            var configLoadHandle = Addressables.LoadAssetAsync<LoggingConfig>("LoggingConfig");
            var loggingConfig = configLoadHandle.WaitForCompletion();

            if (configLoadHandle.Status == AsyncOperationStatus.Succeeded && loggingConfig != null)
            {
                builder.RegisterInstance(loggingConfig);
            }
            else
            {
                Debug.LogWarning("Addressables 加载 'LoggingConfig' 失败！使用默认设置。");
                builder.RegisterInstance(ScriptableObject.CreateInstance<LoggingConfig>());
            }

            // ========================================
            // 1. 注册基础设施服务
            // ========================================

            // 日志系统：日志记录、过滤、分发
            // 注册日志输出接收器（Unity 控制台）
            builder.Register<UnityLogSink>(Lifetime.Singleton)
                .As<ILogSink>();

            // 注册核心日志服务
            builder.Register<LogService>(Lifetime.Singleton)
                .As<ILogService>();

            // 资源管理：Addressables 资源加载、缓存与释放
            // 用途：统一的资源加载入口，支持异步加载、预加载、实例化等
            // 依赖：ILogService（用于记录资源加载日志）
            builder.Register<AddressablesAssetProvider>(Lifetime.Singleton)
                .As<IAssetProvider>();

            // 场景管理：场景异步加载与卸载、加载进度追踪
            // 用途：统一的场景加载入口，支持 Addressables 场景管理
            builder.Register<SceneService>(Lifetime.Singleton)
                .As<ISceneService>();

            // ========================================
            // 核心服务初始化器
            // ========================================
            // 在所有服务注册完成后，注册启动器来初始化这些服务
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
