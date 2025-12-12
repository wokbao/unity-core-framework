using Core.Feature.SceneManagement.Abstractions;
using Cysharp.Threading.Tasks;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 快门风格过渡（占位实现）：当前复用淡入淡出，便于后续替换为快门动画。
    /// </summary>
    public sealed class SceneShutterTransition : ISelectableSceneTransition
    {
        private readonly SceneFadeTransition _fallback;

        public SceneTransitionMode Mode => SceneTransitionMode.Shutter;

        public SceneShutterTransition(SceneFadeTransition fallback)
        {
            _fallback = fallback;
        }

        public UniTask PlayOutAsync(string fromScene, string toScene, string description = null)
        {
            return _fallback.PlayOutAsync(fromScene, toScene, description);
        }

        public UniTask PlayInAsync(string toScene, string description = null)
        {
            return _fallback.PlayInAsync(toScene, description);
        }
    }
}
