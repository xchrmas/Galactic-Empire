// The entire galaxy - a graph of connected star systems.
// Generated procedurally at game start, then persisted.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GalacticEmpire.Feature.Galaxy.Domain
{
    /// <summary>The full galaxy map - all sectors and their connections.</summary>
    public sealed record GalaxyMapEntity
    {
        public Guid Id { get; init; }
        public IReadOnlyList<SectorEntity> Sectors { get; init; }

        // Quick stats
        public int TotalSectors    => Sectors.Count;
        public int DiscoveredCount => Sectors.Count(s => s.IsDiscovered);
        public int OwnedCount      => Sectors.Count(s => s.IsOwned);

        // How much of the galaxy has been explored (0.0 to 1.0)
            public float ExplorationProgress => TotalSectors > 0 ? (float)DiscoveredCount / TotalSectors : 0f;

        /// <summary>Creates a new empty galaxy ready for procedural generation.</summary>
        public static GalaxyMapEntity Create()
        {
            return new GalaxyMapEntity
            {
                Id  = Guid.NewGuid(),
                Sectors = new List<SectorEntity>().AsReadOnly()
            };
        }

        /// <summary>Creates galaxy from a pre-built sector list - used by the generator.</summary>
        public static GalaxyMapEntity CreateFromSectors(IReadOnlyList<SectorEntity> sectors)
        {
            if (sectors == null || sectors.Count == 0)
                throw new ArgumentException("Galaxy needs at least one sector.", nameof(sectors));

            return new GalaxyMapEntity
            {
                Id      = Guid.NewGuid(),
                Sectors = sectors
            };
        }

        /// <summary>Finds a sector by ID.</summary>
        public SectorEntity GetSector(Guid sectorId)
        {
            return Sectors.FirstOrDefault(s => s.Id == sectorId);
        }

        /// <summary>Returns all neighbours of a given sector.</summary>
        public IReadOnlyList<SectorEntity> GetNeighbours(Guid sectorId)
        {
            var sector = GetSector(sectorId);
            if (sector == null)
                return new List<SectorEntity>().AsReadOnly();

            return sector.NeighbourIds
                .Select(id => GetSector(id))
                .Where(s => s != null)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>Returns a new galaxy with an updated sector (after discovery, claim etc).</summary>
        public GalaxyMapEntity UpdateSector(SectorEntity updated)
        {
            var updatedSectors = Sectors
                .Select(s => s.Id == updated.Id ? updated : s)
                .ToList()
                .AsReadOnly();

            return this with { Sectors = updatedSectors };
        }

        /// <summary>Returns all sectors owned by the player.</summary>
        public IReadOnlyList<SectorEntity> GetOwnedSectors()
        {
            return Sectors.Where(s => s.IsOwned).ToList().AsReadOnly();
        }

        /// <summary>Returns all discovered but unowned sectors - potential targets.</summary>
        public IReadOnlyList<SectorEntity> GetDiscoveredUnownedSectors()
        {
            return Sectors
                .Where(s => s.IsDiscovered && !s.IsOwned)
                .ToList()
                .AsReadOnly();
        }
    }
}
