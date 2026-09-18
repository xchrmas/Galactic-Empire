// Fleet management screen - lists all fleets with their status, lets the
// player recall a moving/idle fleet and build a new fleet from a blueprint.
// Dispatch stays on GalaxyMapScreen - that's where the target sector is
// actually chosen, no point duplicating sector selection here.

using System;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GalacticEmpire.Feature.UI.Screens
{
    /// <summary>Fleet overview overlay - fleet list, recall, create fleet from blueprint.</summary>
    public sealed class FleetManagementScreen : ScreenBase
    {
        [SerializeField] private UIDocument _document;

        // Available ship blueprints for the create-fleet section - dragged in via
        // Inspector, same pattern StationBuilderPresenter uses for its module list.
        [SerializeField] private ShipBlueprintSO[] _availableBlueprints = Array.Empty<ShipBlueprintSO>();

        private ScrollView _fleetList;
        private VisualElement _createSection;
        private ScrollView _blueprintList;
        private TextField _fleetNameField;
        private IntegerField _shipCountField;
        private Label _feedback;

        private IFleetService _fleetService;
        private bool _uiInitialized;

        public void Initialize(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        protected override void OnShow()
        {
            InitializeUIIfNeeded();
            Refresh();
        }

        private void InitializeUIIfNeeded()
        {
            if (_uiInitialized)
                return;

            if (_document == null)
            {
                Debug.LogError("[FleetManagementScreen] UIDocument not assigned.");
                return;
            }

            var root = _document.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("[FleetManagementScreen] rootVisualElement is null. UIDocument may not be enabled yet.");
                return;
            }

            _fleetList = root.Q<ScrollView>("fleet-list");
            _createSection = root.Q<VisualElement>("create-section");
            _blueprintList = root.Q<ScrollView>("blueprint-list");
            _fleetNameField = root.Q<TextField>("fleet-name-field");
            _shipCountField = root.Q<IntegerField>("ship-count-field");
            _feedback = root.Q<Label>("fleet-feedback");

            _uiInitialized = true;
        }

        /// <summary>Rebuilds the fleet list and blueprint list from current service state.</summary>
        public void Refresh()
        {
            if (_fleetList == null)
                return;

            _feedback.text = "";
            RenderFleetList();
            RenderBlueprintList();
        }

        private void RenderFleetList()
        {
            _fleetList.Clear();

            var fleets = _fleetService.GetAll();

            if (fleets.Count == 0)
            {
                _fleetList.Add(new Label("No fleets yet.")
                {
                    style = { fontSize = 12, color = new Color(180f / 255f, 180f / 255f, 200f / 255f) }
                });
                return;
            }

            foreach (var fleet in fleets)
                _fleetList.Add(BuildFleetRow(fleet));
        }

        private VisualElement BuildFleetRow(FleetEntity fleet)
        {
            var row = new VisualElement
            {
                style =
                {
                    marginBottom = 8,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    backgroundColor = new Color(20f / 255f, 30f / 255f, 50f / 255f, 0.8f),
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3
                }
            };

            row.Add(new Label($"{fleet.Name} - {fleet.Status}")
            {
                style = { fontSize = 13, color = new Color(200f / 255f, 220f / 255f, 255f / 255f), unityFontStyleAndWeight = FontStyle.Bold }
            });

            row.Add(new Label($"Ships: {fleet.ShipCount} | Combat Power: {fleet.CombatPower:F0}")
            {
                style = { fontSize = 11, color = new Color(150f / 255f, 170f / 255f, 200f / 255f), marginBottom = 6 }
            });

            if (fleet.Status == FleetStatus.Moving && fleet.TargetSectorId.HasValue)
            {
                row.Add(new Label($"Target sector: {fleet.TargetSectorId.Value}")
                {
                    style = { fontSize = 10, color = new Color(120f / 255f, 200f / 255f, 255f / 255f), marginBottom = 6 }
                });
            }

            bool canRecall = fleet.Status == FleetStatus.Moving || fleet.Status == FleetStatus.InBattle;

            var recallButton = new Button(() => HandleRecallPressed(fleet)) { text = "RECALL" };
            recallButton.style.height = 26;
            recallButton.style.fontSize = 11;
            recallButton.style.backgroundColor = new Color(120f / 255f, 30f / 255f, 30f / 255f, 0.8f);
            recallButton.style.color = new Color(255f / 255f, 200f / 255f, 180f / 255f);
            recallButton.SetEnabled(canRecall);

            row.Add(recallButton);

            return row;
        }

        private void RenderBlueprintList()
        {
            if (_blueprintList == null)
                return;

            _blueprintList.Clear();

            foreach (var blueprint in _availableBlueprints)
            {
                var button = new Button(() => HandleCreateFleetPressed(blueprint))
                {
                    text = $"{blueprint.ShipName} ({blueprint.MetalCost} Metal + {blueprint.EnergyCost} Energy)"
                };
                button.style.height = 30;
                button.style.fontSize = 11;
                button.style.marginBottom = 4;
                button.style.backgroundColor = new Color(20f / 255f, 60f / 255f, 120f / 255f, 0.8f);
                button.style.color = new Color(180f / 255f, 220f / 255f, 255f / 255f);

                _blueprintList.Add(button);
            }
        }

        private void HandleRecallPressed(FleetEntity fleet)
        {
            try
            {
                _fleetService.Recall(new RecallFleetCommand(fleet.Id));
                _feedback.text = $"{fleet.Name} recalled.";
                Refresh();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                _feedback.text = ex.Message;
            }
        }

        private void HandleCreateFleetPressed(ShipBlueprintSO blueprint)
        {
            string fleetName = _fleetNameField != null && !string.IsNullOrWhiteSpace(_fleetNameField.value)
                ? _fleetNameField.value
                : $"{blueprint.ShipName} Fleet";

            int shipCount = _shipCountField != null && _shipCountField.value > 0
                ? _shipCountField.value
                : 1;

            try
            {
                var fleet = _fleetService.CreateFleet(new CreateFleetCommand(fleetName, blueprint, shipCount));
                _feedback.text = $"{fleet.Name} created with {fleet.ShipCount} ship(s).";
                Refresh();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                _feedback.text = ex.Message;
            }
        }
    }
}
