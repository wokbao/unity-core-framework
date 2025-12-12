using Core.Feature.SceneManagement.Abstractions;
using UnityEngine;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 场景过渡配置（ScriptableObject），统一控制过渡开关与参数。
    /// 可在 Inspector 中切换过渡方案并调节时长/颜色/层级。
    /// </summary>
    [CreateAssetMenu(menuName = "Core/Scene Transition Config", fileName = "SceneTransitionConfig")]
    public sealed class SceneTransitionConfig : ScriptableObject
    {

        [Header("总开关")]
        public bool enableTransition = true;

        [Header("方案选择")]
        public SceneTransitionMode mode = SceneTransitionMode.Cinematic;

        [Header("公共参数")]
        [Range(0f, 1f)] public float overlayAlpha = 0.95f;
        public int sortingOrder = 8000;

        [Header("影院式（黑条 + 淡入淡出）")]
        [Range(0f, 0.5f)] public float barHeightRatio = 0.18f;
        public float barCloseDuration = 1.0f;
        public float barOpenDuration = 1.0f;
        public float cinematicFadeOutDuration = 0.8f;
        public float cinematicFadeInDuration = 0.8f;

        [Header("纯黑场（淡入淡出）")]
        public float fadeOutDuration = 0.35f;
        public float fadeInDuration = 0.3f;

        public Color OverlayColor => new Color(0f, 0f, 0f, Mathf.Clamp01(overlayAlpha));

        public static SceneTransitionConfig Default
        {
            get
            {
                var cfg = CreateInstance<SceneTransitionConfig>();
                cfg.enableTransition = true;
                cfg.mode = SceneTransitionMode.Cinematic;
                cfg.overlayAlpha = 0.95f;
                cfg.sortingOrder = 8000;
                cfg.barHeightRatio = 0.18f;
                cfg.barCloseDuration = 1.0f;
                cfg.barOpenDuration = 1.0f;
                cfg.cinematicFadeOutDuration = 0.8f;
                cfg.cinematicFadeInDuration = 0.8f;
                cfg.fadeOutDuration = 0.35f;
                cfg.fadeInDuration = 0.3f;
                return cfg;
            }
        }
    }
}
