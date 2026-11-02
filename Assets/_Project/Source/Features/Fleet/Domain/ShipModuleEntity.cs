// A single installable ship module (Stage 1c). Unlike StationModuleEntity,
// a ship module has no construction lifecycle - a module is either
// installed in a slot or it isn't, no AdvanceConstruction/Upgrade. Ship
// construction time/cost stays on ShipBlueprintSO (the hull itself), not
// on individual modules - modules are instant fit-outs chosen in the ship
// builder, not separately-built things.
//
// Stat bonuses are plain additive deltas applied on top of the ship's base
// hull/damage/speed (from its blueprint) when the module is installed via
// ShipEntity.InstallModule - see that method for why this is a one-time
// recompute rather than a live computed property.

using System;

namespace GalacticEmpire.Feature.Fleet.Domain
{
    /// <summary>An installable ship module - a stat bonus tied to a module category.</summary>
    public sealed record ShipModuleEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public ShipModuleCategory Category { get; init; }

        public float HullBonus { get; init; }
        public float DamageBonus { get; init; }
        public float SpeedBonus { get; init; }

        /// <summary>Creates a new module instance. Bonuses may be zero but not negative.</summary>
        public static ShipModuleEntity Create(
            string name,
            ShipModuleCategory category,
            float hullBonus,
            float damageBonus,
            float speedBonus)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Module name cannot be empty.", nameof(name));

            if (hullBonus < 0f)
                throw new ArgumentOutOfRangeException(nameof(hullBonus), "Hull bonus cannot be negative.");

            if (damageBonus < 0f)
                throw new ArgumentOutOfRangeException(nameof(damageBonus), "Damage bonus cannot be negative.");

            if (speedBonus < 0f)
                throw new ArgumentOutOfRangeException(nameof(speedBonus), "Speed bonus cannot be negative.");

            return new ShipModuleEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Category = category,
                HullBonus = hullBonus,
                DamageBonus = damageBonus,
                SpeedBonus = speedBonus
            };
        }
    }
}
