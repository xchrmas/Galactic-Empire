// Represents a single cell in the station grid

using System;

namespace GalacticEmpire.Core
{
    /// <summary>Immutable grid cell — holds position and optional module </summary>
    public sealed record GridCell
    {
        // Grid coordinates
        public int X { get; init; }
        public int Y { get; init; }

        // Null means the cell is empty
        public Guid? ModuleId { get; init; }

        public bool IsEmpty => ModuleId == null;

        /// <summary>Creates an empty grid cell at the given coordinates </summary>
        public static GridCell Empty(int x, int y)
        {
            return new GridCell { X = x, Y = y, ModuleId = null };
        }

        /// <summary>Returns a new cell with a module placed in it </summary>
        public GridCell PlaceModule(Guid moduleId)
        {
            if (!IsEmpty)
            {
                throw new InvalidOperationException($"Cell ({X},{Y}) is already occupied.");
            }

            return this with { ModuleId = moduleId };
        }

        /// <summary>Returns a new empty cell after removing the module </summary>
        public GridCell RemoveModule()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException($"Cell ({X},{Y}) is already empty.");
            }

            return this with { ModuleId = null };
        }
    }
}
