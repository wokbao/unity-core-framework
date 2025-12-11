using Cysharp.Threading.Tasks;

namespace Core.Feature.SceneManagement.Abstractions
{
    /// <summary>
    /// 场景过渡接口：负责进出场景时的视觉过渡（如淡入淡出、动画遮罩等）。
    /// </summary>
    public interface ISceneTransition
    {
        /// <summary>
        /// 场景切换前的过渡（通常为淡出）。
        /// </summary>
        /// <param name="fromScene">当前场景 Key，可为空</param>
        /// <param name="toScene">目标场景 Key</param>
        /// <param name="description">可选描述，便于日志或 HUD</param>
        UniTask PlayOutAsync(string fromScene, string toScene, string description = null);

        /// <summary>
        /// 场景切换后的过渡（通常为淡入）。
        /// </summary>
        /// <param name="toScene">目标场景 Key</param>
        /// <param name="description">可选描述，便于日志或 HUD</param>
        UniTask PlayInAsync(string toScene, string description = null);
    }
}
