// ScriptableObject configuration for the ship type.
// For designers and customizers, all ship characteristics

using GalacticEmpire.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GalacticEmpire.Feature.Fleet.Infrastructure
{
    /// <summary>Defines stats, cost and visual data for a ship class.</summary>
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


        public float MetalCost  => _metalCost;
        public float EnergyCost => _energyCost;
        public float BuildTime  => _buildTime;
        public int RequiredShipyardLevel  => _requiredShipyardLevel;


        // Spawns a ShipEntity from this blueprint
        public ShipEntity CreateEntity()
        {
            return ShipEntity.Create(_shipName, _maxHull, _damage, _speed);
        }

        [TitleGroup("Preview")]
        [Button("Preview Ship Stats", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        private void PreviewStats()
        {
            Debug.Log($"[ShipBlueprintSO] {_shipName} ({_shipType})\n" +
                      $"Hull: {_maxHull} | Damage: {_damage} | Speed: {_speed}\n" +
                      $"Cost: {_metalCost} Metal + {_energyCost} Energy | Build: {_buildTime}s\n" +
                      $"Requires Shipyard Lv.{_requiredShipyardLevel}");
        }
    }
}
