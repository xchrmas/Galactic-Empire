// Ship Builder screen - Stage 1c (modular ships). Lists the selected ship's
// module slots and lets the player install/remove modules into them.
//
// Variant C (chosen over a Station-style grid renderer): a ship's slots are
// a flat, pre-typed list, not a 2D grid - and unlike a station, a ship in
// the builder isn't a physical place on the scene the player clicks into.
// So this is a plain UI Toolkit list screen (same shape as
// FleetManagementScreen), no dedicated camera, no Renderer/Presenter pair,
// no click handlers. That's a deliberate departure from the Renderer/
// Presenter pattern in MASTER.md Section 5 for this one screen - the
// pattern exists for things the player clicks on in world space, and a
// ship slot list isn't that.
//
// Lives in Source/Features/UI/Screens/ alongside every other *Screen.cs,
// same ScreenBase convention as GalaxyMapScreen/StationBuilderScreen.

using System;
using System.Linq;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.Fleet.Infrastructure;
using GalacticEmpire.Feature.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GalacticEmpire.Feature.UI.Screens
{
    /// <summary>Ship module configuration overlay - lists slots, install/remove modules (Stage 1c).</summary>
    public sealed class ShipBuilderScreen : ScreenBase
    {
        [SerializeField] private UIDocument _document;

        // Available modules to install, grouped by category at render time -
        // dragged in via Inspector, same pattern as FleetManagementScreen's
        // _availableBlueprints / StationBuilderPresenter's Available Modules.
        [SerializeField] private ShipModuleSO[] _availableModules = Array.Empty<ShipModuleSO>();

        private Label _shipNameLabel;
        private Label _shipStatsLabel;
        private ScrollView _slotList;
        private ScrollView _moduleList;
        private Label _feedback;

        private IFleetService _fleetService;
        private Guid _currentShipId;
        private int _selectedSlotIndex = -1;
        private bool _uiInitialized;

        public void Initialize(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        /// <summary>Entry point - selects which ship this screen configures, called right before ShowAsync.</summary>
        public void SetShip(Guid shipId)
        {
            _currentShipId = shipId;
            _selectedSlotIndex = -1;
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
                Debug.LogError("[ShipBuilderScreen] UIDocument not assigned.");
                return;
            }

            var root = _document.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("[ShipBuilderScreen] rootVisualElement is null. UIDocument may not be enabled yet.");
                return;
            }

            _shipNameLabel = root.Q<Label>("ship-name-label");
            _shipStatsLabel = root.Q<Label>("ship-stats-label");
            _slotList = root.Q<ScrollView>("slot-list");
            _moduleList = root.Q<ScrollView>("module-list");
            _feedback = root.Q<Label>("ship-builder-feedback");

            _uiInitialized = true;
        }

        /// <summary>Rebuilds the slot list, stat summary and (if a slot is selected) the available-module list.</summary>
        public void Refresh()
        {
            if (_slotList == null)
                return;

            _feedback.text = "";

            var ship = _fleetService.FindShip(_currentShipId);
            if (ship == null)
            {
                _shipNameLabel.text = "No ship selected.";
                _shipStatsLabel.text = "";
                _slotList.Clear();
                _moduleList?.Clear();
                return;
            }

            _shipNameLabel.text = ship.Name;
            _shipStatsLabel.text = $"Hull: {ship.MaxHull:F0} | Damage: {ship.Damage:F1} | Speed: {ship.Speed:F1}";

            RenderSlotList(ship);
            RenderModuleList(ship);
        }

        private void RenderSlotList(ShipEntity ship)
        {
            _slotList.Clear();

            if (ship.Slots.Count == 0)
            {
                _slotList.Add(new Label("This ship has no module slots.")
                {
                    style = { fontSize = 12, color = new Color(180f / 255f, 180f / 255f, 200f / 255f) }
                });
                return;
            }

            foreach (var slot in ship.Slots)
                _slotList.Add(BuildSlotRow(ship, slot));
        }

        private VisualElement BuildSlotRow(ShipEntity ship, ShipSlot slot)
        {
            bool isSelected = slot.SlotIndex == _selectedSlotIndex;

            var row = new VisualElement
            {
                style =
                {
                    marginBottom = 6,
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 10,
                    paddingRight = 10,
                    backgroundColor = isSelected
                        ? new Color(30f / 255f, 80f / 255f, 60f / 255f, 0.9f)
                        : new Color(20f / 255f, 30f / 255f, 50f / 255f, 0.8f),
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3
                }
            };

            string statusText = slot.IsEmpty
                ? $"Slot {slot.SlotIndex} ({slot.Category}) - empty"
                : $"Slot {slot.SlotIndex} ({slot.Category}) - {slot.InstalledModule.Name}";

            row.Add(new Label(statusText)
            {
                style = { fontSize = 12, color = new Color(200f / 255f, 220f / 255f, 255f / 255f) }
            });

            if (slot.IsEmpty)
            {
                var selectButton = new Button(() => HandleSlotSelected(slot.SlotIndex)) { text = isSelected ? "SELECTED" : "SELECT" };
                selectButton.SetEnabled(!isSelected);
                selectButton.style.height = 22;
                selectButton.style.fontSize = 10;
                selectButton.style.marginTop = 4;
                row.Add(selectButton);
            }
            else
            {
                var removeButton = new Button(() => HandleRemoveModulePressed(ship.Id, slot.SlotIndex)) { text = "REMOVE" };
                removeButton.style.height = 22;
                removeButton.style.fontSize = 10;
                removeButton.style.marginTop = 4;
                removeButton.style.backgroundColor = new Color(120f / 255f, 30f / 255f, 30f / 255f, 0.8f);
                removeButton.style.color = new Color(255f / 255f, 200f / 255f, 180f / 255f);
                row.Add(removeButton);
            }

            return row;
        }

        private void RenderModuleList(ShipEntity ship)
        {
            if (_moduleList == null)
                return;

            _moduleList.Clear();

            if (_selectedSlotIndex < 0)
            {
                _moduleList.Add(new Label("Select an empty slot to see available modules.")
                {
                    style = { fontSize = 11, color = new Color(150f / 255f, 150f / 255f, 170f / 255f) }
                });
                return;
            }

            var slot = ship.Slots.FirstOrDefault(s => s.SlotIndex == _selectedSlotIndex);
            if (slot == null || !slot.IsEmpty)
            {
                // Slot got filled or ship changed under us - drop the stale selection.
                _selectedSlotIndex = -1;
                _moduleList.Add(new Label("Select an empty slot to see available modules.")
                {
                    style = { fontSize = 11, color = new Color(150f / 255f, 150f / 255f, 170f / 255f) }
                });
                return;
            }

            var matchingModules = _availableModules.Where(m => m != null && m.Category == slot.Category).ToList();

            if (matchingModules.Count == 0)
            {
                _moduleList.Add(new Label($"No available {slot.Category} modules.")
                {
                    style = { fontSize = 11, color = new Color(150f / 255f, 150f / 255f, 170f / 255f) }
                });
                return;
            }

            foreach (var module in matchingModules)
            {
                var button = new Button(() => HandleInstallModulePressed(ship.Id, module))
                {
                    text = $"{module.ModuleName} (+{module.HullBonus:F0} Hull, +{module.DamageBonus:F1} Dmg, +{module.SpeedBonus:F1} Spd) " +
                           $"- {module.MetalCost} Metal + {module.EnergyCost} Energy"
                };
                button.style.height = 30;
                button.style.fontSize = 10;
                button.style.marginBottom = 4;
                button.style.backgroundColor = new Color(20f / 255f, 60f / 255f, 120f / 255f, 0.8f);
                button.style.color = new Color(180f / 255f, 220f / 255f, 255f / 255f);

                _moduleList.Add(button);
            }
        }

        private void HandleSlotSelected(int slotIndex)
        {
            _selectedSlotIndex = slotIndex;
            Refresh();
        }

        private void HandleInstallModulePressed(Guid shipId, ShipModuleSO moduleSo)
        {
            try
            {
                var module = moduleSo.CreateEntity();
                var cmd = new InstallModuleCommand(shipId, _selectedSlotIndex, module, moduleSo.MetalCost, moduleSo.EnergyCost);

                _fleetService.InstallModule(cmd);

                _feedback.text = $"{module.Name} installed.";
                _selectedSlotIndex = -1;
                Refresh();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
            {
                _feedback.text = ex.Message;
            }
        }

        private void HandleRemoveModulePressed(Guid shipId, int slotIndex)
        {
            try
            {
                _fleetService.RemoveModule(new RemoveModuleCommand(shipId, slotIndex));
                _feedback.text = "Module removed.";
                Refresh();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
            {
                _feedback.text = ex.Message;
            }
        }
    }
}
