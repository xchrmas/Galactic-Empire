// Immutable domain entity representing the player's space station

using System;
using System.Collections.Generic;
using System.Linq;

namespace GalacticEmpire.Core
{
    /// <summary>Immutable domain entity representing the player's space station </summary>
    public sealed record StationEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; }

        // Grid of cells — gridSize x gridSize
        public IReadOnlyList<GridCell> Grid { get; init; }

        // All modules placed on the station
        public IReadOnlyList<StationModuleEntity> Modules { get; init; }

        public int GridSize { get; init; }

        public int TotalModules => Modules.Count;
        public int BuiltModules  => Modules.Count(m => m.IsBuilt);

        /// <summary>Creates a new empty station with a clean grid </summary>
        public static StationEntity Create(string name, int gridSize = 6)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Station name cannot be empty.", nameof(name));
            }

            if (gridSize < 2 || gridSize > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(gridSize), "Grid size must be between 2 and 20.");
            }

            // Build empty grid
            var cells = new List<GridCell>();
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    cells.Add(GridCell.Empty(x, y));
                }
            }

            return new StationEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Grid = cells.AsReadOnly(),
                Modules = new List<StationModuleEntity>().AsReadOnly(),
                GridSize = gridSize
            };
        }

        /// <summary>Returns a new station with a module placed at the given grid position </summary>
        public StationEntity PlaceModule(StationModuleEntity module, int x, int y)
        {
            var cell = GetCell(x, y);

            if (!cell.IsEmpty)
            {
                throw new InvalidOperationException($"Cell ({x},{y}) is already occupied.");
            }

            // Update the cell
            var updatedGrid = Grid.Select(c =>
                c.X == x && c.Y == y ? c.PlaceModule(module.Id) : c
            ).ToList();

            var updatedModules = Modules.Append(module).ToList();

            return this with
            {
                Grid = updatedGrid.AsReadOnly(),
                Modules = updatedModules.AsReadOnly()
            };
        }

        /// <summary>Returns a new station with a module removed from the given position.</summary>
        public StationEntity RemoveModule(int x, int y)
        {
            var cell = GetCell(x, y);

            if (cell.IsEmpty)
            {
                throw new InvalidOperationException($"Cell ({x},{y}) is empty — nothing to remove.");
            }

            var updatedGrid = Grid.Select(c =>
                c.X == x && c.Y == y ? c.RemoveModule() : c
            ).ToList();

            var updatedModules = Modules
                .Where(m => m.Id != cell.ModuleId)
                .ToList();

            return this with
            {
                Grid    = updatedGrid.AsReadOnly(),
                Modules = updatedModules.AsReadOnly()
            };
        }

        /// <summary>Calculates total resource production per second for a given resource type </summary>
        public float GetTotalProduction(ResourceType resource)
        {
            return Modules
                .Where(m => m.IsBuilt && m.ProducedResource == resource)
                .Sum(m => m.ProductionRate);
        }

        /// <summary>Returns the grid cell at the given coordinates.</summary>
        public GridCell GetCell(int x, int y)
        {
            var cell = Grid.FirstOrDefault(c => c.X == x && c.Y == y);

            if (cell == null)
            {
                throw new ArgumentOutOfRangeException($"Cell ({x},{y}) is outside the grid bounds.");
            }

            return cell;
        }
    }
}
