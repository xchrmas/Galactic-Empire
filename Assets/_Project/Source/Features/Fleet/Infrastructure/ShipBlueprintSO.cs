// ScriptableObject configuration for the ship type.
// For designers and customizers, all ship characteristics.
//
// Moved from Source/Features/Fleet/Domain/ to Infrastructure/ (2026-10-21,
// alongside Stage 1c) - SO/content-config classes belong in Infrastructure
// per the Station pattern (StationModuleSO), Domain stays pure data+logic
// with no UnityEngine/Odin dependency. Moving this required removing
// ShipBlueprintSO from IFleetService's CreateFleetCommand/BuildShipCommand
// (Application must never reference Infrastructure - same rule Station's
// PlaceModuleCommand follows) - see IFleetService.cs and
// FleetManagementScreen.cs for the caller-side change: the Presentation
// caller now calls CreateEntity()/reads costs itself and passes a built
// ShipEntity + its cost into the command, instead of passing the SO through.
//
// Stage 1c: added _slotCategories, so a blueprint now defines how many
// module slots of which ShipModuleCategory the ship has, instead of (only)
// fixed combat stats. CreateEntity() builds the ship with those slots via
// ShipEntity.CreateWithSlots - MaxHull/Damage/Speed below are the ship's
// BASE stats before any module is installed, same meaning they always had.

using System.Collections.Generic;
using GalacticEmpire.Feature.Fleet.Domain;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GalacticEmpire.Feature.Fleet.Infrastructure
{
    /// <summary>Defines stats, cost, slots and visual data for a ship class.</summary>
    [CreateAssetMenu(fileName = "New Ship", menuName = "GalacticEmpire/Fleet/Ship Blueprint")]
    public sealed class ShipBlueprintSO : SerializedScriptableObject
    {
        [TitleGroup("Identity")]
        [Required, LabelText("Ship Name")]
        [SerializeField] private string _shipName;

        [TitleGroup("Identity")]
        [LabelText("Type")]
        [SerializeField] private ShipType _shipType;

        [TitleGroup("Identity")]
        [Required, PreviewField(64, ObjectFieldAlignment.Left)]
        [SerializeField] private Sprite _icon;

        [TitleGroup("Identity")]
        [MultiLineProperty(2)]
        [SerializeField] private string _description;

        [TitleGroup("Combat Stats")]
        [LabelText("Max Hull"), SuffixLabel("hp"), MinValue(1f)]
        [SerializeField] private float _maxHull = 100f;

        [TitleGroup("Combat Stats")]
        [LabelText("Damage"), SuffixLabel("per shot"), MinValue(0f)]
        [SerializeField] private float _damage = 25f;

        [TitleGroup("Combat Stats")]
        [LabelText("Speed"), SuffixLabel("units/sec"), MinValue(0f)]
        [SerializeField] private float _speed = 10f;

        [TitleGroup("Module Slots")]
        [LabelText("Slot Categories"), ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("One entry per slot, in order. e.g. [Weapon, Weapon, Armor, Engine] gives the ship 4 slots.")]
        [SerializeField] private List<ShipModuleCategory> _slotCategories = new();

        [TitleGroup("Build Cost")]
        [LabelText("Metal"), SuffixLabel("units"), MinValue(0f)]
        [SerializeField] private float _metalCost = 100f;

        [TitleGroup("Build Cost")]
        [LabelText("Energy"), SuffixLabel("units"), MinValue(0f)]
        [SerializeField] private float _energyCost = 50f;

        [TitleGroup("Build Cost")]
        [LabelText("Build Time"), SuffixLabel("seconds"), MinValue(1f)]
        [SerializeField] private float _buildTime = 30f;

        [TitleGroup("Build Cost")]
        [LabelText("Requires Shipyard Level"), MinValue(1), MaxValue(10)]
        [SerializeField] private int _requiredShipyardLevel = 1;

        // Public accessors
        public string ShipName => _shipName;
        public ShipType Type  => _shipType;
        public Sprite Icon    => _icon;

        public string Description => _description;
        public float MaxHull => _maxHull;
        public float Damage  => _damage;
        public float Speed   => _speed;

        public IReadOnlyList<ShipModuleCategory> SlotCategories => _slotCategories;

        public float MetalCost  => _metalCost;
        public float EnergyCost => _energyCost;
        public float BuildTime  => _buildTime;
        public int RequiredShipyardLevel  => _requiredShipyardLevel;


        /// <summary>Spawns a ShipEntity from this blueprint, with empty module slots if any are defined.</summary>
        public ShipEntity CreateEntity()
        {
            return ShipEntity.CreateWithSlots(_shipName, _maxHull, _damage, _speed, _slotCategories);
        }

        [TitleGroup("Preview")]
        [Button("Preview Ship Stats", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        private void PreviewStats()
        {
            Debug.Log($"[ShipBlueprintSO] {_shipName} ({_shipType})\n" +
                      $"Hull: {_maxHull} | Damage: {_damage} | Speed: {_speed} | Slots: {_slotCategories.Count}\n" +
                      $"Cost: {_metalCost} Metal + {_energyCost} Energy | Build: {_buildTime}s\n" +
                      $"Requires Shipyard Lv.{_requiredShipyardLevel}");
        }
    }
}
