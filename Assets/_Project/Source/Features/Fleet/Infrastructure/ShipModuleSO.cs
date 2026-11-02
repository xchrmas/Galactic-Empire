// ScriptableObject configuration for an installable ship module (Stage 1c).
// Mirrors StationModuleSO's role for Station - designer-authored content
// asset, CreateEntity() stamps the Domain entity. No construction lifecycle
// fields (BuildTime etc.) - see ShipModuleEntity's header comment for why.

using GalacticEmpire.Feature.Fleet.Domain;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GalacticEmpire.Feature.Fleet.Infrastructure
{
    /// <summary>Defines a ship module's category, stat bonuses and cost.</summary>
    [CreateAssetMenu(fileName = "New Module", menuName = "GalacticEmpire/Fleet/Ship Module")]
    public sealed class ShipModuleSO : SerializedScriptableObject
    {
        [TitleGroup("Identity")]
        [Required, LabelText("Module Name")]
        [SerializeField] private string _moduleName;

        [TitleGroup("Identity")]
        [LabelText("Category")]
        [SerializeField] private ShipModuleCategory _category;

        [TitleGroup("Identity")]
        [PreviewField(64, ObjectFieldAlignment.Left)]
        [SerializeField] private Sprite _icon;

        [TitleGroup("Identity")]
        [MultiLineProperty(2)]
        [SerializeField] private string _description;

        [TitleGroup("Stat Bonuses")]
        [LabelText("Hull Bonus"), SuffixLabel("hp"), MinValue(0f)]
        [SerializeField] private float _hullBonus;

        [TitleGroup("Stat Bonuses")]
        [LabelText("Damage Bonus"), SuffixLabel("per shot"), MinValue(0f)]
        [SerializeField] private float _damageBonus;

        [TitleGroup("Stat Bonuses")]
        [LabelText("Speed Bonus"), SuffixLabel("units/sec"), MinValue(0f)]
        [SerializeField] private float _speedBonus;

        [TitleGroup("Build Cost")]
        [LabelText("Metal"), SuffixLabel("units"), MinValue(0f)]
        [SerializeField] private float _metalCost = 50f;

        [TitleGroup("Build Cost")]
        [LabelText("Energy"), SuffixLabel("units"), MinValue(0f)]
        [SerializeField] private float _energyCost = 25f;

        // Public accessors
        public string ModuleName => _moduleName;
        public ShipModuleCategory Category => _category;
        public Sprite Icon => _icon;
        public string Description => _description;

        public float HullBonus => _hullBonus;
        public float DamageBonus => _damageBonus;
        public float SpeedBonus => _speedBonus;

        public float MetalCost => _metalCost;
        public float EnergyCost => _energyCost;

        /// <summary>Spawns a ShipModuleEntity from this config.</summary>
        public ShipModuleEntity CreateEntity()
        {
            return ShipModuleEntity.Create(_moduleName, _category, _hullBonus, _damageBonus, _speedBonus);
        }

        [TitleGroup("Preview")]
        [Button("Preview Module Stats", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        private void PreviewStats()
        {
            Debug.Log($"[ShipModuleSO] {_moduleName} ({_category})\n" +
                      $"+{_hullBonus} Hull | +{_damageBonus} Damage | +{_speedBonus} Speed\n" +
                      $"Cost: {_metalCost} Metal + {_energyCost} Energy");
        }
    }
}
