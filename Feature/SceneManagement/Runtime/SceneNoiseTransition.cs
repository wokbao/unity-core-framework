using System.Threading;
using Core.Feature.SceneManagement.Abstractions;
using Cysharp.Threading.Tasks;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 噪点/胶片风格过渡（占位实现）：当前复用淡入淡出，后续可替换为自定义噪点遮罩。
    /// </summary>
    public sealed class SceneNoiseTransition : ISelectableSceneTransition
    {
        private readonly SceneFadeTransition _fallback;

        public SceneTransitionMode Mode => SceneTransitionMode.Noise;

        public SceneNoiseTransition(SceneFadeTransition fallback)
        {
            _fallback = fallback;
        }

        public UniTask PlayOutAsync(string fromScene, string toScene, string description, CancellationToken ct)
        {
            return _fallback.PlayOutAsync(fromScene, toScene, description, ct);
        }

        public UniTask PlayInAsync(string toScene, string description, CancellationToken ct)
        {
            return _fallback.PlayInAsync(toScene, description, ct);
        }
    }
}
