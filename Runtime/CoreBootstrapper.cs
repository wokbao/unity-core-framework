using Core.Feature.Logging.Abstractions;
using Core.Feature.SceneManagement.Abstractions;
using VContainer.Unity;

namespace Core.Bootstrap
{
    /// <summary>
    /// Core 层服务初始化器
    /// 负责在应用启动时按正确顺序初始化所有核心服务
    /// </summary>
    public sealed class CoreBootstrapper : IStartable
    {
        private readonly ILogService _logService;
        private readonly ISceneService _sceneService;

        public CoreBootstrapper(
            ILogService logService,
            ISceneService sceneService)
        {
            _logService = logService;
            _sceneService = sceneService;
        }

        public void Start()
        {
            _logService.Information(LogCategory.Core, "=== Core 服务初始化开始 ===");

            // 初始化日志服务
            InitializeLoggingService();

            // 初始化场景服务
            InitializeSceneService();

            // TODO: 初始化其他服务
            // InitializeObjectPoolService();
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
    }
}
