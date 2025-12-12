using Core.Feature.SceneManagement.Abstractions;
using Cysharp.Threading.Tasks;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 空过渡实现：禁用视觉过渡时使用。
    /// </summary>
    public sealed class NoSceneTransition : ISelectableSceneTransition
    {
        public SceneTransitionMode Mode => SceneTransitionMode.None;

        public UniTask PlayOutAsync(string fromScene, string toScene, string description = null)
        {
            return UniTask.CompletedTask;
        }

        public UniTask PlayInAsync(string toScene, string description = null)
        {
            return UniTask.CompletedTask;
        }
    }
}
