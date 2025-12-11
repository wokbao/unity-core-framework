using System;
using Core.Feature.Loading.Abstractions;
using Core.Feature.Logging.Abstractions;
using Core.Feature.SceneManagement.Abstractions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 场景管理服务的默认实现。
    /// </summary>
    public sealed class SceneService : ISceneService, IDisposable
    {
        public string CurrentSceneKey { get; private set; }

        private readonly ILogService _logService;
        private readonly ILoadingService _loadingService;
        private readonly ISceneTransition _transition;
        private AsyncOperationHandle<SceneInstance> _currentSceneHandle;

        public SceneService(
            ILogService logService,
            ILoadingService loadingService,
            ISceneTransition sceneTransition = null)
        {
            _logService = logService;
            _loadingService = loadingService;
            _transition = sceneTransition;
        }

        public async UniTask LoadSceneAsync(string sceneKey, bool useLoadingScreen = true, IProgress<float> progress = null)
        {
            _logService.Information(LogCategory.Core, $"开始加载场景 {sceneKey}");

            using var loadingScope = _loadingService?.Begin($"加载场景 {sceneKey}");

            // 如果有旧场景是通过 Addressables 加载的，先卸载
            if (_currentSceneHandle.IsValid())
            {
                await UnloadCurrentSceneAsync();
            }

            // 过渡前（淡出）
            if (useLoadingScreen && _transition != null)
            {
                await _transition.PlayOutAsync(CurrentSceneKey, sceneKey, $"切换到 {sceneKey}");
            }

            try
            {
                // 使用 Addressables 加载场景
                // LoadSceneMode.Single 会自动卸载当前非 DontDestroyOnLoad 的场景
                var handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Single);

                // 进度追踪：传递给外部并同步到 LoadingService
                var progressReporter = _loadingService?.CreateProgressReporter($"加载场景 {sceneKey}", progress) ?? progress;
                await handle.ToUniTask(progress: progressReporter);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentSceneHandle = handle;
                    CurrentSceneKey = sceneKey;
                    _logService.Information(LogCategory.Core, $"场景加载成功: {sceneKey}");
                }
                else
                {
                    _logService.Error(LogCategory.Core, $"场景加载失败: {sceneKey}");
                    throw new Exception($"场景加载失败: {sceneKey}");
                }
            }
            catch (Exception e)
            {
                _logService.Error(LogCategory.Core, $"场景加载发生异常: {sceneKey}", e);
                throw;
            }
            finally
            {
                // 过渡后（淡入），即便失败也尝试恢复视觉状态
                if (useLoadingScreen && _transition != null)
                {
                    await _transition.PlayInAsync(sceneKey, $"切换完成 {sceneKey}");
                }
            }
        }

        public async UniTask UnloadSceneAsync(string sceneKey)
        {
            // 主要用于卸载叠加场景（Additive），当前暂未实现多场景管理逻辑
            // 如果是卸载主场景，通常直接 Load 新场景即可
            await UniTask.CompletedTask;
        }

        private async UniTask UnloadCurrentSceneAsync()
        {
            if (_currentSceneHandle.IsValid())
            {
                _logService.Information(LogCategory.Core, "卸载当前场景...");
                await Addressables.UnloadSceneAsync(_currentSceneHandle);
                _currentSceneHandle = default;
            }
        }

        public void Dispose()
        {
            // 可以在这里做一些清理工作
        }
    }
}
