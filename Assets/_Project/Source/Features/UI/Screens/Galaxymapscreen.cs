// Galaxy map screen - shows sector info when the player clicks a sector.
// Lets the player pick an idle fleet and dispatch it to the selected sector.
//
// Does NOT reference IEncounterService or BattlePresenter directly - both
// live in Battle.Application/Battle.Presentation, and Battle.Presentation
// references UI.Screens (for BattleHUDScreen), so a direct reference back
// from here would create a circular assembly dependency (UI.Screens ->
// Battle.Presentation -> UI.Screens), which Unity refuses to compile.
// Instead this raises OnFleetDispatchedToSector, and GameEntryPoint (which
// already references every layer) wires the encounter check from there.
// See features/battle.md for the full encounter flow.

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

        // Set by GameEntryPoint via SetBattleInProgressCheck - lets this screen
        // block Dispatch while a battle is running without referencing
        // BattlePresenter directly (see the circular-dependency note above).
        private Func<bool> _isBattleInProgress;

        private SectorEntity _selectedSector;
        private FleetEntity[] _idleFleets = Array.Empty<FleetEntity>();

        // Raised after the screen finishes its show/hide fade - GalaxyMapPresenter
        // uses these to move the camera to/from the galaxy map view
        public event Action OnShown;
        public event Action OnHidden;

        // Raised right after a successful Dispatch - GameEntryPoint subscribes
        // to this to check IEncounterService and hand off to BattlePresenter,
        // keeping this screen free of any Battle-layer reference.
        public event Action<FleetEntity, Guid> OnFleetDispatchedToSector;

        public void Initialize(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        /// <summary>
        /// Lets GameEntryPoint plug in a "is a battle currently running" check
        /// without this screen needing to reference BattlePresenter's type.
        /// </summary>
        public void SetBattleInProgressCheck(Func<bool> isBattleInProgress)
        {
            _isBattleInProgress = isBattleInProgress;
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

            if (_isBattleInProgress != null && _isBattleInProgress())
            {
                _dispatchFeedback.text = "A battle is already in progress.";
                return;
            }

            try
            {
                var dispatchedFleet = _fleetService.Dispatch(new DispatchFleetCommand(fleet.Id, _selectedSector.Id));
                _dispatchFeedback.text = $"{fleet.Name} dispatched to {_selectedSector.Name}.";
                RefreshFleetDropdown();

                OnFleetDispatchedToSector?.Invoke(dispatchedFleet, _selectedSector.Id);
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
