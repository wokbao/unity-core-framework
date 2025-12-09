using Core.Feature.Logging.Abstractions;
using Core.Feature.ObjectPooling.Abstractions;
using Core.Feature.SceneManagement.Abstractions;
using VContainer.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 层服务初始化器
    /// 
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>在应用启动时按正确顺序初始化所有核心服务</item>
    ///   <item>记录服务初始化过程的日志</item>
    ///   <item>为未来服务添加预留初始化接口</item>
    /// </list>
    /// 
    /// <para><b>工作原理</b>：</para>
    /// <list type="number">
    ///   <item>VContainer 在容器构建完成后自动调用 Start() 方法</item>
    ///   <item>所有依赖的服务通过构造函数自动注入</item>
    ///   <item>按顺序执行各服务的初始化方法</item>
    ///   <item>记录初始化开始和完成的日志</item>
    /// </list>
    /// 
    /// <para><b>扩展方式</b>：</para>
    /// 当添加新的核心服务时：
    /// <code>
    /// 1. 在构造函数中注入新服务
    /// public CoreBootstrapper(
    ///     ILogService logService,
    ///     ISceneService sceneService,
    ///     IObjectPoolManager objectPoolManager) // 新增
    /// { ... }
    /// 
    /// 2. 在 Start() 方法中调用初始化
    /// public void Start()
    /// {
    ///     // ...
    ///     InitializeObjectPoolService();
    /// }
    /// 
    /// 3. 添加初始化方法
    /// private void InitializeObjectPoolService()
    /// {
    ///     _logService.Information(LogCategory.Core, "初始化对象池服务...");
    ///     // 执行初始化逻辑
    /// }
    /// </code>
    /// 
    /// <para><b>注意事项</b>：</para>
    /// <list type="bullet">
    ///   <item>此类必须通过 RegisterEntryPoint 注册到 CoreLifetimeScope</item>
    ///   <item>初始化顺序很重要，确保服务依赖关系正确</item>
    ///   <item>某些服务可能不需要额外初始化（如日志服务）</item>
    /// </list>
    /// </summary>
    public sealed class CoreBootstrapper : IStartable
    {
        private readonly ILogService _logService;
        private readonly ISceneService _sceneService;
        private readonly IObjectPoolManager _objectPoolManager;

        public CoreBootstrapper(
            ILogService logService,
            ISceneService sceneService,
            IObjectPoolManager objectPoolManager)
        {
            _logService = logService;
            _sceneService = sceneService;
            _objectPoolManager = objectPoolManager;
        }

        public void Start()
        {
            _logService.Information(LogCategory.Core, "=== Core 服务初始化开始 ===");

            // 初始化日志服务
            InitializeLoggingService();

            // 初始化场景服务
            InitializeSceneService();

            // 初始化对象池
            InitializeObjectPoolService();

            // TODO: 初始化其他服务
            // InitializeConfigService();
            // InitializeEventBusService();

            _logService.Information(LogCategory.Core, "=== Core 服务初始化完成 ===");
        }

        private void InitializeLoggingService()
        {
            _logService.Information(LogCategory.Core, "初始化日志服务...");
            // 日志服务通常不需要额外初始化，构造时已完成
        }

        private void InitializeSceneService()
        {
            _logService.Information(LogCategory.Core, "初始化场景服务...");
            // 场景服务也不需要额外初始化
            // 如果未来需要预加载某些场景，可以在这里处理
        }

        private void InitializeObjectPoolService()
        {
            if (_objectPoolManager == null)
            {
                _logService.Error(LogCategory.Core, "对象池服务未注入，跳过初始化");
                return;
            }

            _logService.Information(LogCategory.Core, "初始化对象池服务...");
            // 当前无额外初始化逻辑，预留预热/配置入口
        }
    }
}
