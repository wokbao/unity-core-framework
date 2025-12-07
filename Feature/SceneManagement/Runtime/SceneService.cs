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
    /// <summary>
    /// 场景管理服务的默认实现
    /// 
    /// <para><b>职责</b>：</para>
    /// <list type="bullet">
    ///   <item>通过 Addressables 异步加载和卸载场景</item>
    ///   <item>追踪场景加载进度并支持进度回调</item>
    ///   <item>管理当前场景的引用和生命周期</item>
    ///   <item>记录场景加载过程的日志</item>
    /// </list>
    /// 
    /// <para><b>设计模式</b>：</para>
    /// <list type="bullet">
    ///   <item>单场景模式（Single Scene Architecture）：每次只保留一个主场景</item>
    ///   <item>加载新场景时自动卸载旧场景</item>
    ///   <item>使用 UniTask 提供流畅的异步体验</item>
    /// </list>
    /// 
    /// <para><b>使用示例</b>：</para>
    /// <code>
    /// // 简单加载
    /// await _sceneService.LoadSceneAsync("MainMenu");
    /// 
    /// // 带进度追踪的加载
    /// var progress = new Progress&lt;float&gt;(p => Debug.Log($"加载进度: {p * 100}%"));
    /// await _sceneService.LoadSceneAsync("Gameplay", useLoadingScreen: true, progress);
    /// </code>
    /// 
    /// <para><b>注意事项</b>：</para>
    /// <list type="bullet">
    ///   <item>场景资源必须通过 Addressables 管理并设置正确的 Key</item>
    ///   <item>加载模式为 LoadSceneMode.Single，会自动卸载非 DontDestroyOnLoad 的场景</item>
    ///   <item>暂不支持叠加场景（Additive Scene）功能</item>
    /// </list>
    /// </summary>
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
                    throw new Exception($"场景加载失败: {sceneKey}");
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
