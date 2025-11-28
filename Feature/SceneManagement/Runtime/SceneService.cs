using System;
using System.Threading;
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
    public sealed class SceneService : ISceneService, IDisposable
    {
        public string CurrentSceneKey { get; private set; }

        private readonly ILogService _logService;
        private AsyncOperationHandle<SceneInstance> _currentSceneHandle;

        public SceneService(ILogService logService)
        {
            _logService = logService;
        }

        public async UniTask LoadSceneAsync(string sceneKey, bool useLoadingScreen = true, IProgress<float> progress = null)
        {
            _logService.Information(LogCategory.Core, $"开始加载场景: {sceneKey}");

            // 如果有旧场景是通过 Addressables 加载的，先卸载
            // 注意：这里假设我们是单场景架构（Single Scene Architecture），每次只保留一个主场景
            if (_currentSceneHandle.IsValid())
            {
                await UnloadCurrentSceneAsync();
            }

            // TODO: 如果 useLoadingScreen 为 true，这里应该先加载 Loading 界面
            // await ShowLoadingScreen();

            try
            {
                // 使用 Addressables 加载场景
                // LoadSceneMode.Single 会自动卸载当前非 DontDestroyOnLoad 的场景
                var handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Single);
                
                // 进度追踪
                await handle.ToUniTask(progress: progress);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentSceneHandle = handle;
                    CurrentSceneKey = sceneKey;
                    _logService.Information(LogCategory.Core, $"场景加载成功: {sceneKey}");
                }
                else
                {
                    _logService.Error(LogCategory.Core, $"场景加载失败: {sceneKey}");
                    throw new Exception($"Failed to load scene: {sceneKey}");
                }
            }
            catch (Exception e)
            {
                _logService.Error(LogCategory.Core, $"场景加载发生异常: {sceneKey}", e);
                throw;
            }

            // TODO: 隐藏 Loading 界面
            // await HideLoadingScreen();
        }

        public async UniTask UnloadSceneAsync(string sceneKey)
        {
            // 主要用于卸载叠加场景（Additive），目前暂未实现多场景管理逻辑
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
