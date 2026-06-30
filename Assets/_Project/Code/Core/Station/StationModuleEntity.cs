// Immutable domain entity representing a single station module.

using System;

namespace GalacticEmpire.Core
{
    /// <summary>Immutable domain entity representing a station module </summary>
    public sealed record StationModuleEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public StationModuleType Type { get; init; }
        public StationModuleCategory Category { get; init; }

        // Current upgrade level (1 = base, max defined by config)
        public int Level { get; init; }

        // Resource production per second at current level
        public float ProductionRate { get; init; }
        public ResourceType ProducedResource { get; init; }

        // Build cost at current level
        public float BuildCostMetal { get; init; }
        public float BuildCostEnergy { get; init; }

        // True if the module is fully constructed
        public bool IsBuilt { get; init; }

        // Construction progress (0.0 to 1.0)
        public float ConstructionProgress { get; init; }

        public bool IsUnderConstruction => !IsBuilt && ConstructionProgress < 1f;

        /// <summary>Creates a new module ready for construction.</summary>
        public static StationModuleEntity Create(
            string name,
            StationModuleType type,
            StationModuleCategory category,
            ResourceType producedResource,

            float productionRate,
            float buildCostMetal,
            float buildCostEnergy)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Module name cannot be empty.", nameof(name));
            }

            if (productionRate < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(productionRate), "Production rate cannot be negative.");
            }

            return new StationModuleEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = type,
                Category = category,
                Level = 1,
                ProductionRate   = productionRate,
                ProducedResource = producedResource,
                BuildCostMetal   = buildCostMetal,
                BuildCostEnergy  = buildCostEnergy,
                IsBuilt = false,
                ConstructionProgress = 0f
            };
        }

        /// <summary>Returns a new module with updated construction progress </summary>
        public StationModuleEntity AdvanceConstruction(float deltaProgress)
        {
            if (IsBuilt)
            {
                return this;
            }

            float newProgress = MathF.Min(1f, ConstructionProgress + deltaProgress);
            bool completed = newProgress >= 1f;

            return this with
            {
                ConstructionProgress = newProgress,
                IsBuilt = completed
            };
        }

        /// <summary>Returns a new module upgraded to the next level.</summary>
        public StationModuleEntity Upgrade(float productionRateBonus)
        {
            if (!IsBuilt)
            {
                throw new InvalidOperationException("Cannot upgrade a module that is not built.");
            }

            return this with
            {
                Level = Level + 1,
                ProductionRate = ProductionRate + productionRateBonus
            };
        }
    }
}
