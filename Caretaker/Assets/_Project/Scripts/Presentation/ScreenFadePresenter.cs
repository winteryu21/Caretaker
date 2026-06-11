using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Presents a full-screen fade overlay for camera and scene transitions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFadePresenter : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private Color _fadeColor = Color.black;
        [SerializeField] private int _sortingOrder = 32767;

        /// <summary>Finds an existing fade presenter or creates one for runtime transitions.</summary>
        public static ScreenFadePresenter GetOrCreate()
        {
            ScreenFadePresenter presenter = FindAnyObjectByType<ScreenFadePresenter>();
            if (presenter != null)
            {
                return presenter;
            }

            GameObject presenterObject = new("ScreenFadePresenter");
            return presenterObject.AddComponent<ScreenFadePresenter>();
        }

        /// <summary>Immediately hides the fade overlay.</summary>
        public void HideImmediate()
        {
            EnsureOverlay();
            SetAlpha(0f);
        }

        /// <summary>Immediately shows the fade overlay.</summary>
        public void ShowImmediate()
        {
            EnsureOverlay();
            SetAlpha(1f);
        }

        /// <summary>Fades the overlay to full opacity.</summary>
        public IEnumerator FadeOut(float duration)
        {
            yield return FadeTo(1f, duration);
        }

        /// <summary>Fades the overlay to transparent.</summary>
        public IEnumerator FadeIn(float duration)
        {
            yield return FadeTo(0f, duration);
        }

        /// <summary>Fades the overlay to the requested alpha.</summary>
        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            EnsureOverlay();

            float resolvedTargetAlpha = Mathf.Clamp01(targetAlpha);
            if (duration <= 0f)
            {
                SetAlpha(resolvedTargetAlpha);
                yield break;
            }

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, resolvedTargetAlpha, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetAlpha(resolvedTargetAlpha);
        }

        private void Awake()
        {
            EnsureOverlay();
            SetAlpha(0f);
        }

        private void EnsureOverlay()
        {
            if (_canvas == null)
            {
                GameObject canvasObject = new("ScreenFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
                canvasObject.transform.SetParent(transform, false);

                _canvas = canvasObject.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = _sortingOrder;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                _canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            }
            else
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = _sortingOrder;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = _canvas.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (_fadeImage == null)
            {
                GameObject imageObject = new("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(_canvas.transform, false);

                RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                _fadeImage = imageObject.GetComponent<Image>();
            }

            _fadeImage.color = _fadeColor;
            _fadeImage.raycastTarget = true;
            _canvasGroup.interactable = false;
        }

        private void SetAlpha(float alpha)
        {
            _canvasGroup.alpha = Mathf.Clamp01(alpha);
            _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0.001f;
        }
    }
}
