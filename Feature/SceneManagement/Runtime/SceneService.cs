using System;
using Core.Feature.Loading.Abstractions;
using Core.Feature.Logging.Abstractions;
using Core.Feature.SceneManagement.Abstractions;
using Core.Feature.AssetManagement.Runtime;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
        // 静态注册点，确保 100% 能找到 Handler，绕过 FindObjectOfType 的潜在问题
        public static ISceneReadyHandler ActiveHandler;
        public event Action<SceneTransitionEvent> OnTransitionCompleted;

        private readonly ILogService _logService;
        private readonly ILoadingService _loadingService;
        private readonly IAssetProvider _assetProvider;
        private readonly ISceneTransition _transition;

        // 存储当前场景实例，用于卸载
        private SceneInstance _currentSceneInstance;

        public SceneService(
            ILogService logService,
            ILoadingService loadingService,
            IAssetProvider assetProvider,
            ISceneTransition sceneTransition = null)
        {
            _logService = logService;
            _loadingService = loadingService;
            _assetProvider = assetProvider;
            _transition = sceneTransition;
        }

        public async UniTask LoadSceneAsync(string sceneKey, bool useLoadingScreen = true, IProgress<float> progress = null)
        {
            _logService.Information(LogCategory.Core, $"开始加载场景 {sceneKey}");

            var transitionEvent = new SceneTransitionEvent(CurrentSceneKey, sceneKey);
            bool loadSucceeded = false;

            // -------------------------------------------------------------------------
            // 阶段一：阻断式加载（Loading Scope）
            // 在此作用域内，LoadingHud 会根据前台模式显示。
            // 作用域结束时，LoadingHud 自动消失，确保不干扰后续的视觉淡入。
            // -------------------------------------------------------------------------
            try
            {
                // 注意：这里使用 block scope 限制 using 的生命周期
                using (_loadingService?.Begin($"加载场景 {sceneKey}"))
                {
                    if (_currentSceneInstance.Scene.IsValid())
                    {
                        _loadingService?.BeginPhase("卸载当前场景");
                        await UnloadCurrentSceneAsync();
                        _loadingService?.EndPhase("卸载当前场景");
                    }

                    if (useLoadingScreen && _transition != null)
                    {
                        OnTransitionStarted?.Invoke(transitionEvent);
                        _loadingService?.BeginPhase("播放转场动画");
                        await _transition.PlayOutAsync(CurrentSceneKey, sceneKey, $"切换到 {sceneKey}");
                        _loadingService?.EndPhase("播放转场动画");
                    }
                    else
                    {
                        OnTransitionStarted?.Invoke(transitionEvent);
                    }

                    _loadingService?.BeginPhase("加载场景资源");

                    var progressReporter = _loadingService?.CreateProgressReporter($"加载场景 {sceneKey}", progress) ?? progress;

                    try
                    {
                        // 使用 IAssetProvider 加载场景
                        var sceneInstance = await _assetProvider.LoadSceneAsync(sceneKey, LoadSceneMode.Single);

                        // 加载成功
                        _currentSceneInstance = sceneInstance;
                        CurrentSceneKey = sceneKey;
                        progressReporter?.Report(1.0f);

                        _loadingService?.EndPhase("加载场景资源");
                        _logService.Information(LogCategory.Core, $"场景加载成功: {sceneKey}");
                    }
                    catch
                    {
                        _loadingService?.EndPhase("加载场景资源");
                        throw;
                    }

                    // 场景加载成功后，进行初始化等待
                    _logService.Information(LogCategory.Core, "场景加载资源完成，检查 ActiveHandler...");
                    var readyHandler = ActiveHandler;

                    if (readyHandler != null)
                    {
                        _loadingService?.BeginPhase("等待场景就绪");
                        _logService.Information(LogCategory.Core, $"等待 ISceneReadyHandler: {readyHandler.GetType().Name}...");
                        await readyHandler.WaitForSceneReadyAsync();
                        _logService.Information(LogCategory.Core, "场景视觉已就绪");
                        _loadingService?.EndPhase("等待场景就绪");
                    }
                    else
                    {
                        // Fallback logic
                        var method2 = UnityEngine.Object.FindObjectOfType<MonoBehaviour>() as ISceneReadyHandler;
                        if (method2 != null)
                        {
                            _logService.Information(LogCategory.Core, $"使用场景内找到的 Handler: {method2.GetType().Name} (Fallback)");
                            ActiveHandler = method2;
                            await method2.WaitForSceneReadyAsync();
                        }
                        else
                        {
                            _logService.Information(LogCategory.Core, "未找到 ISceneReadyHandler，直接进入视觉揭示阶段");
                        }
                    }

                    // 标记成功，允许进入阶段二
                    loadSucceeded = true;

                } // <--- END USING: LoadingHud 在此处自动消失（因为 ActiveForegroundOperations 归零）
            }
            catch (Exception e)
            {
                _logService.Error(LogCategory.Core, $"场景加载发生异常: {sceneKey}", e);
                throw;
            }

            // -------------------------------------------------------------------------
            // 阶段二：视觉揭示（Visual Reveal）
            // 此时 LoadingHud 已消失，转场遮罩（SortingOrder 9999）依然覆盖全屏并阻挡输入。
            // 我们平滑地 fade in，展示新场景。
            // -------------------------------------------------------------------------
            if (loadSucceeded)
            {
                if (useLoadingScreen && _transition != null)
                {
                    _logService.Information(LogCategory.Core, "开始播放淡入动画 (PlayInAsync)");
                    // 注意：此时不再包裹在 Loading Scope 中，属于纯视觉过渡
                    await _transition.PlayInAsync(sceneKey, $"切换完成 {sceneKey}");
                }

                OnTransitionCompleted?.Invoke(transitionEvent);
            }
        }

        public async UniTask UnloadSceneAsync(string sceneKey)
        {
            await UniTask.CompletedTask;
        }

        private async UniTask UnloadCurrentSceneAsync()
        {
            if (!_currentSceneInstance.Scene.IsValid())
            {
                return;
            }

            var scene = _currentSceneInstance.Scene;

            // 如果当前只挂着一个场景（常见于主菜单首场景），则不主动卸载。
            if (SceneManager.sceneCount <= 1 || !scene.IsValid())
            {
                // 降级为 Debug，避免在正常流程刷屏。
                _logService.Debug(LogCategory.Core, "跳过卸载当前场景（仅剩单场景或句柄无效）。");
                _currentSceneInstance = default;
                return;
            }

            _logService.Information(LogCategory.Core, "卸载当前场景...");

            try
            {
                await _assetProvider.UnloadSceneAsync(_currentSceneInstance);
            }
            catch (Exception ex)
            {
                _logService.Error(LogCategory.Core, "卸载场景异常", ex);
            }

            _currentSceneInstance = default;
        }

        public void Dispose()
        {
            OnTransitionStarted = null;
            OnTransitionCompleted = null;
        }
    }
}
