using System;
using Cysharp.Threading.Tasks;

namespace Core.Feature.SceneManagement.Abstractions
{
    /// <summary>
    /// 场景管理服务接口
    /// 负责场景的异步加载、卸载以及加载进度的追踪
    /// </summary>
    public interface ISceneService
    {
        /// <summary>
        /// 当前激活的场景名称（或 Key）
        /// </summary>
        string CurrentSceneKey { get; }

        event Action<SceneTransitionEvent> OnTransitionStarted;

        event Action<SceneTransitionEvent> OnTransitionCompleted;

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneKey">场景的 Addressable Key 或名称</param>
        /// <param name="useLoadingScreen">是否显示加载界面（过渡场景）</param>
        /// <param name="progress">加载进度回调 (0.0 - 1.0)</param>
        /// <returns>UniTask</returns>
        UniTask LoadSceneAsync(string sceneKey, bool useLoadingScreen = true, IProgress<float> progress = null);

        /// <summary>
        /// 异步卸载场景（通常用于卸载叠加场景）
        /// </summary>
        /// <param name="sceneKey">场景的 Addressable Key 或名称</param>
        /// <returns>UniTask</returns>
        UniTask UnloadSceneAsync(string sceneKey);
    }
}
