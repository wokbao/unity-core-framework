using System;
using Core.Feature.SceneManagement.Abstractions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 基于 UI Canvas 的黑场淡入淡出过渡，使用不随时间缩放的插值确保在加载期间也能平滑播放。
    /// </summary>
    public sealed class SceneFadeTransition : ISelectableSceneTransition, IDisposable
    {
        private readonly float _fadeOutDuration;
        private readonly float _fadeInDuration;
        private readonly CanvasGroup _canvasGroup;
        private readonly Image _overlay;
        private readonly GameObject _root;

        public SceneTransitionMode Mode => SceneTransitionMode.Fade;

        public SceneFadeTransition(
            float fadeOutDuration = 0.35f,
            float fadeInDuration = 0.3f,
            Color? overlayColor = null,
            int sortingOrder = 8000)
        {
            _fadeOutDuration = Mathf.Max(0.01f, fadeOutDuration);
            _fadeInDuration = Mathf.Max(0.01f, fadeInDuration);

            _root = new GameObject("SceneFadeTransition", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            _canvasGroup = _root.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var overlayObj = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlayObj.transform.SetParent(_root.transform, false);
            var rect = overlayObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _overlay = overlayObj.GetComponent<Image>();
            _overlay.color = overlayColor ?? new Color(0f, 0f, 0f, 0.95f);

            UnityEngine.Object.DontDestroyOnLoad(_root);
        }

        public async UniTask PlayOutAsync(string fromScene, string toScene, string description = null)
        {
            await FadeAsync(0f, 1f, _fadeOutDuration);
        }

        public async UniTask PlayInAsync(string toScene, string description = null)
        {
            await FadeAsync(1f, 0f, _fadeInDuration);
        }

        private async UniTask FadeAsync(float from, float to, float duration)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            var time = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / duration);
                _canvasGroup.alpha = Mathf.Lerp(from, to, t);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            _canvasGroup.alpha = to;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        public void Dispose()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }
        }
    }
}
