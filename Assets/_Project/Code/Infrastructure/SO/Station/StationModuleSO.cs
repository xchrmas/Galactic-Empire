// ScriptableObject config for a station module.
// Defines stats, costs and visual data for each module type.
using GalacticEmpire.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GalacticEmpire.Infrastructure
{
    /// <summary>ScriptableObject configuration asset for a station module type.</summary>
    [CreateAssetMenu(fileName = "New Module", menuName = "GalacticEmpire/Station/Module Config")]
    public sealed class StationModuleSO : SerializedScriptableObject
    {
        [TitleGroup("Identity")]
        [Required, LabelText("Module Name")]
        [SerializeField] private string _moduleName;

        [TitleGroup("Identity")]
        [Required, LabelText("Description")]
        [MultiLineProperty(3)]
        [SerializeField] private string _description;

        [TitleGroup("Identity")]
        [Required, PreviewField(64, ObjectFieldAlignment.Left)]
        [SerializeField] private Sprite _icon;

        [TitleGroup("Identity")]
        [LabelText("Type")]
        [SerializeField] private StationModuleType _moduleType;

        [TitleGroup("Identity")]
        [LabelText("Category")]
        [SerializeField] private StationModuleCategory _category;

        [TitleGroup("Production")]
        [LabelText("Produced Resource")]
        [SerializeField] private ResourceType _producedResource;

        [TitleGroup("Production")]
        [LabelText("Production Rate"), SuffixLabel("units/sec"), MinValue(0f)]
        [SerializeField] private float _productionRate;

        [TitleGroup("Build Cost")]
        [LabelText("Metal Cost"), SuffixLabel("units"), MinValue(0f)]
        [SerializeField] private float _buildCostMetal;

        [TitleGroup("Build Cost")]
        [LabelText("Energy Cost"), SuffixLabel("units"), MinValue(0f)]
        [SerializeField] private float _buildCostEnergy;

        [TitleGroup("Build Cost")]
        [LabelText("Build Time"), SuffixLabel("seconds"), MinValue(1f)]
        [SerializeField] private float _buildTime = 10f;

        [TitleGroup("Upgrade")]
        [LabelText("Max Level"), MinValue(1), MaxValue(10)]
        [SerializeField] private int _maxLevel = 5;

        [TitleGroup("Upgrade")]
        [LabelText("Production Bonus per Level"), SuffixLabel("units/sec"), MinValue(0f)]
        [SerializeField] private float _productionBonusPerLevel = 2f;


        // Public accessors
        public string ModuleName  => _moduleName;
        public string Description => _description;
        public Sprite Icon  => _icon;

        public StationModuleType Type  => _moduleType;
        public StationModuleCategory Category => _category;

        public ResourceType ProducedResource => _producedResource;
        public float ProductionRate   => _productionRate;
        public float BuildCostMetal   => _buildCostMetal;
        public float BuildCostEnergy  => _buildCostEnergy;

        public float BuildTime => _buildTime;
        public int MaxLevel => _maxLevel;
        public float ProductionBonusPerLevel => _productionBonusPerLevel;

        /// <summary>Creates a StationModuleEntity from this config.</summary>
        public StationModuleEntity CreateEntity()
        {
            return StationModuleEntity.Create(
                name: _moduleName,
                type: _moduleType,
                category: _category,

                producedResource: _producedResource,
                productionRate: _productionRate,
                buildCostMetal: _buildCostMetal,
                buildCostEnergy: _buildCostEnergy);
        }

        [TitleGroup("Preview")]
        [Button("Preview Module Stats", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        private void PreviewStats()
        {
            Debug.Log($"[StationModuleSO] {_moduleName}\n" +
                      $"Type: {_moduleType} | Category: {_category}\n" +
                      $"Production: {_productionRate} {_producedResource}/sec\n" +
                      $"Build Cost: {_buildCostMetal} Metal + {_buildCostEnergy} Energy\n" +
                      $"Build Time: {_buildTime}s | Max Level: {_maxLevel}");
        }
    }
}
