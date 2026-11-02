// A single module slot on a ship (Stage 1c). Mirrors GridCell's shape for
// Station (an immutable container that's either empty or occupied), but is
// 1D/typed instead of a 2D grid cell - a ship's slots are a flat list, each
// pre-typed to one ShipModuleCategory by its blueprint (e.g. "slot 0 is a
// Weapon slot"), not a free grid any module can occupy. There's no
// equivalent of Station's grid positioning because a ship builder screen
// lists slots, it doesn't render them spatially (see Variant C decision -
// a ship isn't a physical place on the scene the way a station is).

using System;

namespace GalacticEmpire.Feature.Fleet.Domain
{
    /// <summary>One typed module slot on a ship - empty or holding one installed module.</summary>
    public sealed record ShipSlot
    {
        public int SlotIndex { get; init; }
        public ShipModuleCategory Category { get; init; }
        public ShipModuleEntity InstalledModule { get; init; }

        public bool IsEmpty => InstalledModule == null;

        /// <summary>Creates an empty slot of the given category at the given index.</summary>
        public static ShipSlot CreateEmpty(int slotIndex, ShipModuleCategory category)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "Slot index cannot be negative.");

            return new ShipSlot
            {
                SlotIndex = slotIndex,
                Category = category,
                InstalledModule = null
            };
        }

        /// <summary>Returns a new slot with the given module installed - module category must match the slot's.</summary>
        public ShipSlot Install(ShipModuleEntity module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (module.Category != Category)
                throw new InvalidOperationException(
                    $"Cannot install a {module.Category} module into a {Category} slot.");

            if (!IsEmpty)
                throw new InvalidOperationException($"Slot {SlotIndex} already holds a module - remove it first.");

            return this with { InstalledModule = module };
        }

        /// <summary>Returns a new empty slot, discarding whatever module was installed.</summary>
        public ShipSlot RemoveModule()
        {
            if (IsEmpty)
                throw new InvalidOperationException($"Slot {SlotIndex} is already empty.");

            return this with { InstalledModule = null };
        }
    }
}
