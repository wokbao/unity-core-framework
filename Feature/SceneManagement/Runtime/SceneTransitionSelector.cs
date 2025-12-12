using System.Collections.Generic;
using System.Linq;
using Core.Feature.Logging.Abstractions;
using Core.Feature.SceneManagement.Abstractions;
using Cysharp.Threading.Tasks;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 根据配置选择具体的场景过渡实现，未匹配到时回退到默认方案。
    /// </summary>
    public sealed class SceneTransitionSelector : ISceneTransition
    {
        private readonly SceneTransitionConfig _config;
        private readonly IReadOnlyDictionary<SceneTransitionMode, ISelectableSceneTransition> _transitionMap;
        private readonly ISelectableSceneTransition _fallback;
        private readonly ISelectableSceneTransition _noTransition;
        private readonly ILogService _logService;

        public SceneTransitionSelector(
            SceneTransitionConfig config,
            IEnumerable<ISelectableSceneTransition> transitions,
            ILogService logService)
        {
            _config = config ?? SceneTransitionConfig.Default;
            _logService = logService;

            var dict = new Dictionary<SceneTransitionMode, ISelectableSceneTransition>();
            foreach (var transition in transitions)
            {
                dict[transition.Mode] = transition;
            }

            _transitionMap = dict;
            _fallback = dict.TryGetValue(SceneTransitionMode.Fade, out var fade)
                ? fade
                : dict.Values.FirstOrDefault();
            _noTransition = dict.TryGetValue(SceneTransitionMode.None, out var none)
                ? none
                : null;
        }

        public UniTask PlayOutAsync(string fromScene, string toScene, string description = null)
        {
            return Resolve().PlayOutAsync(fromScene, toScene, description);
        }

        public UniTask PlayInAsync(string toScene, string description = null)
        {
            return Resolve().PlayInAsync(toScene, description);
        }

        private ISceneTransition Resolve()
        {
            if (!_config.enableTransition)
            {
                return _noTransition ?? _fallback ?? new NoSceneTransition();
            }

            if (_transitionMap.TryGetValue(_config.mode, out var transition))
            {
                return transition;
            }

            _logService?.Warning(LogCategory.Core,
                $"[SceneTransition] 未找到模式 {_config.mode} 的过渡实现，回退到默认淡入淡出。");

            return _fallback ?? _transitionMap.Values.FirstOrDefault() ?? new NoSceneTransition();
        }
    }
}
