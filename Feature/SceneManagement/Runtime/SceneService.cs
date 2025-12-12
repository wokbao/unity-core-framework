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
        public event Action<SceneTransitionEvent> OnTransitionStarted;
        public event Action<SceneTransitionEvent> OnTransitionCompleted;

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
            var transitionEvent = new SceneTransitionEvent(CurrentSceneKey, sceneKey);

            if (_currentSceneHandle.IsValid())
            {
                await UnloadCurrentSceneAsync();
            }

            if (useLoadingScreen && _transition != null)
            {
                OnTransitionStarted?.Invoke(transitionEvent);
                await _transition.PlayOutAsync(CurrentSceneKey, sceneKey, $"切换到 {sceneKey}");
            }
            else
            {
                OnTransitionStarted?.Invoke(transitionEvent);
            }

            var loadSucceeded = false;
            try
            {
                var handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Single);
                var progressReporter = _loadingService?.CreateProgressReporter($"加载场景 {sceneKey}", progress) ?? progress;
                await handle.ToUniTask(progress: progressReporter);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentSceneHandle = handle;
                    CurrentSceneKey = sceneKey;
                    _logService.Information(LogCategory.Core, $"场景加载成功: {sceneKey}");
                    loadSucceeded = true;
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
                if (useLoadingScreen && _transition != null)
                {
                    await _transition.PlayInAsync(sceneKey, $"切换完成 {sceneKey}");
                }

                if (loadSucceeded)
                {
                    OnTransitionCompleted?.Invoke(transitionEvent);
                }
            }
        }

        public async UniTask UnloadSceneAsync(string sceneKey)
        {
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
            // 这里可以做一些清理工作
        }
    }
}
