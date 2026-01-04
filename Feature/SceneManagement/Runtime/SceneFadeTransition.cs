using System;
using System.Threading;
using Core.Feature.SceneManagement.Abstractions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 基于 UI Canvas 的黑场淡入淡出过渡，使用不随时间缩放的插值确保在加载期间也能平滑播放。
    /// </summary>
    /// <remarks>
    /// 所有异步操作使用统一的 <see cref="CancellationToken"/>，在 <see cref="Dispose"/> 时自动取消。
    /// </remarks>
    public sealed class SceneFadeTransition : ISelectableSceneTransition, IDisposable
    {
        private readonly float _fadeOutDuration;
        private readonly float _fadeInDuration;
        private readonly CanvasGroup _canvasGroup;
        private readonly Image _overlay;
        private readonly GameObject _root;

        public SceneTransitionMode Mode => SceneTransitionMode.Fade;

        /// <summary>
        /// 内部取消令牌源，用于在 Dispose 时取消所有正在进行的操作
        /// </summary>
        private CancellationTokenSource _cts = new();
        private bool _isDisposed;

        public SceneFadeTransition(
            float fadeOutDuration = 0.35f,
            float fadeInDuration = 0.3f,
            Color? overlayColor = null,
            int sortingOrder = 9999)
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

        public async UniTask PlayOutAsync(string fromScene, string toScene, string description, CancellationToken ct)
        {
            if (_isDisposed) return;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            await FadeAsync(0f, 1f, _fadeOutDuration, linked.Token);
        }

        public async UniTask PlayInAsync(string toScene, string description, CancellationToken ct)
        {
            if (_isDisposed) return;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            await FadeAsync(1f, 0f, _fadeInDuration, linked.Token);
        }

        private async UniTask FadeAsync(float from, float to, float duration, CancellationToken ct)
        {
            var time = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            while (time < duration)
            {
                ct.ThrowIfCancellationRequested();

                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / duration);
                _canvasGroup.alpha = Mathf.Lerp(from, to, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _canvasGroup.alpha = to;

            // 只有当完全透明时才允许穿透
            var isVisible = to > 0.01f;
            _canvasGroup.blocksRaycasts = isVisible;
            _canvasGroup.interactable = isVisible;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }
        }
    }
}
