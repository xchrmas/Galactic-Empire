// Galaxy map screen - shows sector info when the player clicks a sector.
// Lets the player pick an idle fleet and dispatch it to the selected sector.

using System;
using System.Linq;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.Galaxy.Domain;
using GalacticEmpire.Feature.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GalacticEmpire.Feature.UI.Screens
{
    /// <summary>Galaxy map overlay - sector info panel and fleet dispatch.</summary>
    public sealed class GalaxyMapScreen : ScreenBase
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _sectorPanel;
        private Label _sectorName;
        private Label _sectorType;
        private Label _sectorRichness;
        private Label _sectorThreat;
        private Label _sectorStatus;
        private DropdownField _fleetDropdown;
        private Button _dispatchButton;
        private Label _dispatchFeedback;

        private IFleetService _fleetService;
        private bool _uiInitialized;

        private SectorEntity _selectedSector;
        private FleetEntity[] _idleFleets = Array.Empty<FleetEntity>();

        // Raised after the screen finishes its show/hide fade - GalaxyMapPresenter
        // uses these to move the camera to/from the galaxy map view
        public event Action OnShown;
        public event Action OnHidden;

        public void Initialize(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        protected override void OnShow()
        {
            InitializeUIIfNeeded();
            OnShown?.Invoke();
        }

        protected override void OnHide()
        {
            OnHidden?.Invoke();
        }

        private void InitializeUIIfNeeded()
        {
            if (_uiInitialized)
                return;

            if (_document == null)
            {
                Debug.LogError("[GalaxyMapScreen] UIDocument not assigned.");
                return;
            }

            var root = _document.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("[GalaxyMapScreen] rootVisualElement is null. UIDocument may not be enabled yet.");
                return;
            }

            _sectorPanel = root.Q<VisualElement>("sector-panel");
            _sectorName = root.Q<Label>("sector-name");
            _sectorType = root.Q<Label>("sector-type");
            _sectorRichness = root.Q<Label>("sector-richness");
            _sectorThreat = root.Q<Label>("sector-threat");
            _sectorStatus = root.Q<Label>("sector-status");
            _fleetDropdown = root.Q<DropdownField>("fleet-dropdown");
            _dispatchButton = root.Q<Button>("dispatch-button");
            _dispatchFeedback = root.Q<Label>("dispatch-feedback");

            if (_dispatchButton != null)
                _dispatchButton.clicked += HandleDispatchPressed;

            _uiInitialized = true;
        }

        /// <summary>Call this from GalaxyMapPresenter when the player clicks a sector.</summary>
        public void ShowSectorInfo(SectorEntity sector)
        {
            _selectedSector = sector;

            if (_sectorPanel == null)
                return;

            _sectorPanel.style.display = DisplayStyle.Flex;

            _sectorName.text = sector.Name;
            _sectorType.text = $"Type: {sector.Type}";
            _sectorRichness.text = $"Resource Richness: {sector.ResourceRichness * 100f:F0}%";
            _sectorThreat.text = $"Threat Level: {sector.ThreatLevel * 100f:F0}%";
            _sectorStatus.text = sector.IsOwned ? "Owned" : "Unowned";
            _dispatchFeedback.text = "";

            RefreshFleetDropdown();
        }

        private void RefreshFleetDropdown()
        {
            if (_fleetService == null || _fleetDropdown == null)
                return;

            _idleFleets = _fleetService.GetAll()
                .Where(f => f.Status == FleetStatus.Idle)
                .ToArray();

            var names = _idleFleets.Select(f => f.Name).ToList();

            _fleetDropdown.choices = names;
            _fleetDropdown.value = names.Count > 0 ? names[0] : "";
            _fleetDropdown.SetEnabled(names.Count > 0);

            if (_dispatchButton != null)
                _dispatchButton.SetEnabled(names.Count > 0);
        }

        private void HandleDispatchPressed()
        {
            if (_selectedSector == null || _fleetDropdown == null)
                return;

            var fleet = _idleFleets.FirstOrDefault(f => f.Name == _fleetDropdown.value);

            if (fleet == null)
            {
                _dispatchFeedback.text = "Select a fleet first.";
                return;
            }

            try
            {
                _fleetService.Dispatch(new DispatchFleetCommand(fleet.Id, _selectedSector.Id));
                _dispatchFeedback.text = $"{fleet.Name} dispatched to {_selectedSector.Name}.";
                RefreshFleetDropdown();
            }
            catch (InvalidOperationException ex)
            {
                _dispatchFeedback.text = ex.Message;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_dispatchButton != null)
                _dispatchButton.clicked -= HandleDispatchPressed;
        }
    }
}
