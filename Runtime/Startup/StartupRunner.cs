using System;
using Core.Feature.SceneManagement.Abstractions;
using Core.Feature.Logging.Abstractions;
using Core.Runtime.Startup;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Core.Runtime.Startup
{
    /// <summary>
    /// 应用启动器：读取 StartupConfig，加载首个业务场景。
    /// </summary>
    public sealed class StartupRunner : IStartable
    {
        private readonly StartupConfig _config;
        private readonly ISceneService _sceneService;
        private readonly ILogService _logService;

        public StartupRunner(
            StartupConfig config,
            ISceneService sceneService,
            ILogService logService)
        {
            _config = config;
            _sceneService = sceneService;
            _logService = logService;
        }

        public void Start()
        {
            LoadInitialSceneAsync().Forget();
        }

        private async UniTaskVoid LoadInitialSceneAsync()
        {
            if (_config == null)
            {
                _logService.Error(LogCategory.Core, "[StartupRunner] 未注入 StartupConfig，无法加载首场景");
                return;
            }

            if (_sceneService == null)
            {
                _logService.Error(LogCategory.Core, "[StartupRunner] 未注入 ISceneService，无法加载首场景");
                return;
            }

            if (!_config.Validate(out var errors))
            {
                foreach (var err in errors)
                {
                    _logService.Error(LogCategory.Core, $"[StartupRunner] 配置错误: {err}");
                }
                return;
            }

            try
            {
                _logService.Information(LogCategory.Core, $"[StartupRunner] 加载首场景: {_config.firstSceneKey}");
                await _sceneService.LoadSceneAsync(_config.firstSceneKey, _config.useLoadingScreen);
            }
            catch (Exception ex)
            {
                _logService.Error(LogCategory.Core, $"[StartupRunner] 加载首场景失败: {_config.firstSceneKey}", ex);
            }
        }
    }
}
