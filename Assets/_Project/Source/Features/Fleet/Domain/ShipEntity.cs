// Migrated from GalacticEmpire.Core (Code/Core/Fleet/ShipEntity.cs) to
// Source/Features/Fleet/Domain/ as part of Stage 1c (modular ships) - the
// same Vertical Slice migration Station went through in Phase 7 Etap 3 Step
// 0. Namespace changed GalacticEmpire.Core -> GalacticEmpire.Feature.Fleet.
// Domain; no other behavior change to the pre-existing members below.
//
// Stage 1c adds Slots (see ShipSlot/ShipModuleEntity). MaxHull/Damage/Speed
// remain plain fields, NOT computed properties summing the slots - they are
// recomputed once, at InstallModule/RemoveModule time, same as a blueprint
// stamping fixed stats today. This matters because Hull (current, battle-
// damaged hull) and MaxHull are different things: TakeDamage/Repair already
// treat MaxHull as a stable ceiling ("Repair" sets Hull = MaxHull). If
// MaxHull were computed live from Slots, repairing or reconfiguring mid-
// battle would retroactively change past damage math. Module slots are
// therefore only mutable while a ship is NOT in a fleet that's Moving/
// InBattle - enforced by ShipBuilderService (Application), not here in
// Domain, consistent with Domain never knowing about Fleet/Battle status.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GalacticEmpire.Feature.Fleet.Domain
{
    /// <summary>Immutable domain entity representing a ship.</summary>
    public sealed record ShipEntity
    {
        // Unique identity of this ship instance
        public Guid Id { get; init; }
        public string Name { get; init; }

        // Combat stats
        public float Hull    { get; init; }
        public float MaxHull { get; init; }
        public float Damage  { get; init; }

        // Movement
        public float Speed { get; init; }

        // Module slots (Stage 1c) - empty list for ships created before slots
        // existed (e.g. NPC ships via EnemyFleetGeneratorService, which never
        // call CreateWithSlots) or for blueprints that define zero slots.
        public IReadOnlyList<ShipSlot> Slots { get; init; } = Array.Empty<ShipSlot>();

        // Computed from Hull — not stored, always up to date
        public bool IsAlive => Hull > 0f;
        public float HullPercent => MaxHull > 0f ? Hull / MaxHull : 0f;

        // Factory method — the only valid way to create a ship.
        // Validates all inputs so an invalid ship can never exist in the domain.
        public static ShipEntity Create(string name, float maxHull, float damage, float speed)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Ship name cannot be empty.", nameof(name));

            if (maxHull <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxHull), "Hull must be greater than zero.");

            if (speed < 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed cannot be negative.");

            return new ShipEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Hull = maxHull, // ship starts at full hull
                MaxHull = maxHull,
                Damage = damage,
                Speed  = speed,
                Slots  = Array.Empty<ShipSlot>()
            };
        }

        /// <summary>
        /// Creates a ship with empty module slots of the given categories
        /// (Stage 1c) - used by ShipBlueprintSO.CreateEntity() when the
        /// blueprint defines slots. baseMaxHull/baseDamage/baseSpeed are the
        /// blueprint's unmodified stats, before any module bonus.
        /// </summary>
        public static ShipEntity CreateWithSlots(
            string name,
            float baseMaxHull,
            float baseDamage,
            float baseSpeed,
            IReadOnlyList<ShipModuleCategory> slotCategories)
        {
            var ship = Create(name, baseMaxHull, baseDamage, baseSpeed);

            if (slotCategories == null || slotCategories.Count == 0)
                return ship;

            var slots = slotCategories
                .Select((category, index) => ShipSlot.CreateEmpty(index, category))
                .ToList()
                .AsReadOnly();

            return ship with { Slots = slots };
        }

        // Records are immutable — domain behaviour always returns a NEW ship.
        // The original ship is never modified (functional approach).

        /// <summary>Returns a new ship with reduced hull after taking damage.</summary>
        public ShipEntity TakeDamage(float amount)
        {
             if (amount < 0f)
             {
                 throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");
             }

             return this with { Hull = MathF.Max(0f, Hull - amount) };
        }


        /// <summary>Returns a new ship restored to full hull.</summary>
        public ShipEntity Repair()
        {
            return this with { Hull = MaxHull };
        }

        /// <summary>
        /// Returns a new ship with the given module installed in the given
        /// slot, and MaxHull/Damage/Speed recomputed to include the module's
        /// bonus (Stage 1c). Current Hull is scaled up by the same absolute
        /// amount MaxHull increased by, so a full-hull ship stays full and a
        /// damaged ship keeps its existing damage rather than being silently
        /// topped up by the new slot - mirrors how a repaired hull plating
        /// module would work in practice (more max hull, not a free heal).
        /// </summary>
        public ShipEntity InstallModule(int slotIndex, ShipModuleEntity module)
        {
            var slot = GetSlotOrThrow(slotIndex);
            var updatedSlot = slot.Install(module);

            float hullDelta = module.HullBonus;

            var updatedSlots = Slots
                .Select(s => s.SlotIndex == slotIndex ? updatedSlot : s)
                .ToList()
                .AsReadOnly();

            return this with
            {
                Slots = updatedSlots,
                MaxHull = MaxHull + hullDelta,
                Hull = Hull + hullDelta,
                Damage = Damage + module.DamageBonus,
                Speed = Speed + module.SpeedBonus
            };
        }

        /// <summary>
        /// Returns a new ship with the slot's module removed and MaxHull/
        /// Damage/Speed reduced back by that module's bonus (Stage 1c).
        /// Current Hull is clamped to the new, lower MaxHull so it's never
        /// left reporting more hull than the ship can now hold.
        /// </summary>
        public ShipEntity RemoveModule(int slotIndex)
        {
            var slot = GetSlotOrThrow(slotIndex);

            if (slot.IsEmpty)
                throw new InvalidOperationException($"Slot {slotIndex} is already empty.");

            var removedModule = slot.InstalledModule;
            var updatedSlot = slot.RemoveModule();

            var updatedSlots = Slots
                .Select(s => s.SlotIndex == slotIndex ? updatedSlot : s)
                .ToList()
                .AsReadOnly();

            float newMaxHull = MaxHull - removedModule.HullBonus;

            return this with
            {
                Slots = updatedSlots,
                MaxHull = newMaxHull,
                Hull = MathF.Min(Hull, newMaxHull),
                Damage = Damage - removedModule.DamageBonus,
                Speed = Speed - removedModule.SpeedBonus
            };
        }

        private ShipSlot GetSlotOrThrow(int slotIndex)
        {
            var slot = Slots.FirstOrDefault(s => s.SlotIndex == slotIndex);
            if (slot == null)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Ship '{Name}' has no slot {slotIndex}.");
            return slot;
        }
    }
}
