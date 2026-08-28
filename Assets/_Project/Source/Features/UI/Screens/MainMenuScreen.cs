// Main menu screen - first thing player sees when launching the game.
// Shows Play button and game title.

using Cysharp.Threading.Tasks;
using GalacticEmpire.Feature.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GalacticEmpire.Feature.UI.Screens
{
    /// <summary>Main menu screen with Play button.</summary>
    public sealed class MainMenuScreen : ScreenBase
    {
        [SerializeField] private UIDocument _document;

        private Button _playButton;

        public event System.Action OnPlayPressed;



        private void OnEnable()
        {
            // Bound once - UIDocument builds rootVisualElement in its own OnEnable,
            // so this can't happen safely in Awake()
            if (_playButton != null)
                return;

            if (_document == null)
            {
                Debug.LogError("[MainMenuScreen] UIDocument not assigned.");
                return;
            }

            var root = _document.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("[MainMenuScreen] rootVisualElement is null. UIDocument may not be enabled yet.");
                return;
            }

            _playButton = root.Q<Button>("play-button");

            if (_playButton == null)
            {
                Debug.LogError("[MainMenuScreen] Button 'play-button' not found.");
                return;
            }

            _playButton.clicked += HandlePlayPressed;
        }

        protected override void OnShow()
        {
            if (_playButton != null)
            {
                _playButton.SetEnabled(true);
            }
        }

        protected override void OnHide()
        {
            if (_playButton != null)
            {
                _playButton.SetEnabled(false);
            }
        }

        private void HandlePlayPressed() => OnPlayPressed?.Invoke();

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_playButton != null)
            {
                _playButton.clicked -= HandlePlayPressed;
            }
        }
    }
}
