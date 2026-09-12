// HUD screen - always visible during gameplay.
// Shows resources, fleet count and sector info.

using GalacticEmpire.Core;
using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GalacticEmpire.Feature.UI.Screens
{
    /// <summary>Gameplay HUD showing resources and empire status.</summary>
    public sealed class HUDScreen : ScreenBase
    {
        [SerializeField] private UIDocument _document;

        private Label _metalLabel;
        private Label _energyLabel;
        private Label _crystalsLabel;
        private Label _darkMatterLabel;
        private Button _galaxyButton;
        private Button _stationButton;

        private IResourceRepository _resourceRepository;
        private bool _uiInitialized;

        public event System.Action OnGalaxyPressed;
        public event System.Action OnStationPressed;

        public void Initialize(IResourceRepository resourceRepository)
        {
            _resourceRepository = resourceRepository;
        }

        protected override void OnShow()
        {
            InitializeUIIfNeeded();
            RefreshResources();
        }

        private void InitializeUIIfNeeded()
        {
            if (_uiInitialized) return;

            if (_document == null)
            {
                Debug.LogError("[HUDScreen] UIDocument not assigned.");
                return;
            }

            var root = _document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[HUDScreen] rootVisualElement is null. UIDocument may not be enabled yet.");
                return;
            }

            _metalLabel = root.Q<Label>("metal-value");
            _energyLabel = root.Q<Label>("energy-value");
            _crystalsLabel = root.Q<Label>("crystals-value");
            _darkMatterLabel = root.Q<Label>("dark-matter-value");
            _galaxyButton = root.Q<Button>("galaxy-button");
            _stationButton = root.Q<Button>("station-button");

            if (_metalLabel == null) Debug.LogWarning("[HUDScreen] Label 'metal-value' not found in UXML.");
            if (_energyLabel == null) Debug.LogWarning("[HUDScreen] Label 'energy-value' not found in UXML.");
            if (_crystalsLabel == null) Debug.LogWarning("[HUDScreen] Label 'crystals-value' not found in UXML.");
            if (_darkMatterLabel == null) Debug.LogWarning("[HUDScreen] Label 'dark-matter-value' not found in UXML.");
            if (_galaxyButton == null) Debug.LogWarning("[HUDScreen] Button 'galaxy-button' not found in UXML.");
            if (_stationButton == null) Debug.LogWarning("[HUDScreen] Button 'station-button' not found in UXML.");

            if (_galaxyButton != null)
                _galaxyButton.clicked += HandleGalaxyPressed;

            if (_stationButton != null)
                _stationButton.clicked += HandleStationPressed;

            _uiInitialized = true;
        }

        private void HandleGalaxyPressed() => OnGalaxyPressed?.Invoke();
        private void HandleStationPressed() => OnStationPressed?.Invoke();

        /// <summary>Call this every production tick to update resource display.</summary>
        public void RefreshResources()
        {
            if (_resourceRepository == null) return;

            var wallet = _resourceRepository.Get();

            if (_metalLabel != null)
                _metalLabel.text = $"{wallet.Get(ResourceType.Metal):F0}";

            if (_energyLabel != null)
                _energyLabel.text = $"{wallet.Get(ResourceType.Energy):F0}";

            if (_crystalsLabel != null)
                _crystalsLabel.text = $"{wallet.Get(ResourceType.Crystals):F0}";

            if (_darkMatterLabel != null)
                _darkMatterLabel.text = $"{wallet.Get(ResourceType.DarkMatter):F0}";
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_galaxyButton != null)
                _galaxyButton.clicked -= HandleGalaxyPressed;

            if (_stationButton != null)
                _stationButton.clicked -= HandleStationPressed;
        }
    }
}
