using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Feature.SceneManagement.Abstractions
{
    /// <summary>
    /// 场景过渡接口：负责进出场景时的视觉过渡（如淡入淡出、动画遮罩等）。
    /// </summary>
    /// <remarks>
    /// 所有方法必须接受 <see cref="CancellationToken"/>，确保在取消时优雅退出。
    /// </remarks>
    public interface ISceneTransition
    {
        /// <summary>
        /// 场景切换前的过渡（通常为淡出）。
        /// </summary>
        /// <param name="fromScene">当前场景 Key，可为空</param>
        /// <param name="toScene">目标场景 Key</param>
        /// <param name="description">可选描述，便于日志或 HUD</param>
        /// <param name="ct">取消令牌，用于中断过渡动画</param>
        UniTask PlayOutAsync(string fromScene, string toScene, string description, CancellationToken ct);

        /// <summary>
        /// 场景切换后的过渡（通常为淡入）。
        /// </summary>
        /// <param name="toScene">目标场景 Key</param>
        /// <param name="description">可选描述，便于日志或 HUD</param>
        /// <param name="ct">取消令牌，用于中断过渡动画</param>
        UniTask PlayInAsync(string toScene, string description, CancellationToken ct);
    }
}
