// Base class for all screens - handles fade animations via DOTween.
// Every screen fades in on show and fades out on hide.
// Override ShowAsync/HideAsync to add custom animations on top.

using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace GalacticEmpire.Feature.UI.Core
{
    /// <summary>Base MonoBehaviour for all UI screens with DOTween fade animations.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ScreenBase : MonoBehaviour, IScreen
    {
        [SerializeField] private float _fadeDuration = 0.3f;

        private CanvasGroup _canvasGroup;
        private CancellationTokenSource _cts;

        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>Fades the screen in and calls OnShow() for subclass logic.</summary>
        public virtual async UniTask ShowAsync()
        {
            CancelCurrentAnimation();
            _cts = new CancellationTokenSource();

            gameObject.SetActive(true);
            IsVisible = true;

            await _canvasGroup
                .DOFade(1f, _fadeDuration)
                .SetEase(Ease.OutCubic)
                .AsyncWaitForCompletion()
                .AsUniTask();

            OnShow();
        }

        /// <summary>Calls OnHide() then fades the screen out.</summary>
        public virtual async UniTask HideAsync()
        {
            CancelCurrentAnimation();
            _cts = new CancellationTokenSource();

            OnHide();
            IsVisible = false;

            await _canvasGroup
                .DOFade(0f, _fadeDuration)
                .SetEase(Ease.InCubic)
                .AsyncWaitForCompletion()
                .AsUniTask();

            gameObject.SetActive(false);
        }

        // Override these in subclasses to run logic on show/hide
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        private void CancelCurrentAnimation()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        protected virtual void OnDestroy()
        {
            CancelCurrentAnimation();
        }
    }
}
