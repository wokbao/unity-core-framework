using System;
using Core.Feature.SceneManagement.Abstractions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Feature.SceneManagement.Runtime
{
    /// <summary>
    /// 影院式过渡：上下黑条闭合 + 叠加淡入淡出，使用不受时间缩放影响的插值。
    /// 设计目标：默认即可获得更具“大片感”的场景切换效果，无需额外资源。
    /// </summary>
    public sealed class SceneCinematicTransition : ISelectableSceneTransition, IDisposable
    {
        private readonly float _barHeightRatio;
        private readonly float _fadeOutDuration;
        private readonly float _fadeInDuration;
        private readonly float _barCloseDuration;
        private readonly float _barOpenDuration;
        private readonly Color _overlayColor;
        private readonly int _sortingOrder;
        public SceneTransitionMode Mode => SceneTransitionMode.Cinematic;

        private readonly GameObject _root;
        private readonly CanvasGroup _canvasGroup;
        private readonly RectTransform _topBar;
        private readonly RectTransform _bottomBar;
        private readonly Image _overlay;
        private readonly float _barHeight;

        public SceneCinematicTransition(
            float barHeightRatio = 0.18f,
            float barCloseDuration = 0.35f,
            float barOpenDuration = 0.3f,
            float fadeOutDuration = 0.25f,
            float fadeInDuration = 0.25f,
            Color? overlayColor = null,
            int sortingOrder = 8000)
        {
            _barHeightRatio = Mathf.Clamp01(barHeightRatio);
            _barCloseDuration = Mathf.Max(0.01f, barCloseDuration);
            _barOpenDuration = Mathf.Max(0.01f, barOpenDuration);
            _fadeOutDuration = Mathf.Max(0.01f, fadeOutDuration);
            _fadeInDuration = Mathf.Max(0.01f, fadeInDuration);
            _overlayColor = overlayColor ?? new Color(0f, 0f, 0f, 0.95f);
            _sortingOrder = sortingOrder;

            _barHeight = Screen.height * _barHeightRatio;

            _root = new GameObject("SceneCinematicTransition", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _sortingOrder;

            _canvasGroup = _root.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f; // 过渡始终可见，由内部条与遮罩控制实际透明度
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // 覆盖层（淡入淡出用）
            var overlayObj = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlayObj.transform.SetParent(_root.transform, false);
            var overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            _overlay = overlayObj.GetComponent<Image>();
            _overlay.color = _overlayColor;
            _overlay.canvasRenderer.SetAlpha(0f);

            _topBar = CreateBar("TopBar", new Vector2(0.5f, 1f), new Vector2(1f, 1f));
            _bottomBar = CreateBar("BottomBar", new Vector2(0.5f, 0f), new Vector2(1f, 0f));

            UnityEngine.Object.DontDestroyOnLoad(_root);
        }

        public async UniTask PlayOutAsync(string fromScene, string toScene, string description = null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            // 同时执行：黑条合拢 + 黑色叠加淡入
            await UniTask.WhenAll(
                AnimateBarsAsync(openToClose: true, _barCloseDuration),
                FadeOverlayAsync(0f, 1f, _fadeOutDuration)
            );
        }

        public async UniTask PlayInAsync(string toScene, string description = null)
        {
            // 先淡出叠加，再打开黑条
            await FadeOverlayAsync(1f, 0f, _fadeInDuration);
            await AnimateBarsAsync(openToClose: false, _barOpenDuration);

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private RectTransform CreateBar(string name, Vector2 pivot, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, anchor.y);
            rect.anchorMax = new Vector2(1f, anchor.y);
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(0f, _barHeight);
            rect.anchoredPosition = anchor.y >= 0.5f ? new Vector2(0f, _barHeight) : new Vector2(0f, -_barHeight);

            var img = go.GetComponent<Image>();
            img.color = Color.black;
            return rect;
        }

        private async UniTask AnimateBarsAsync(bool openToClose, float duration)
        {
            var elapsed = 0f;
            var topStart = openToClose ? _barHeight : 0f;
            var topEnd = openToClose ? 0f : _barHeight;
            var bottomStart = openToClose ? -_barHeight : 0f;
            var bottomEnd = openToClose ? 0f : -_barHeight;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                _topBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(topStart, topEnd, t));
                _bottomBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(bottomStart, bottomEnd, t));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            _topBar.anchoredPosition = new Vector2(0f, topEnd);
            _bottomBar.anchoredPosition = new Vector2(0f, bottomEnd);
        }

        private async UniTask FadeOverlayAsync(float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var alpha = Mathf.Lerp(from, to, t);
                _overlay.canvasRenderer.SetAlpha(alpha);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _overlay.canvasRenderer.SetAlpha(to);
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
