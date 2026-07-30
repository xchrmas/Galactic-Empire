// All galaxy use cases go through here.
// Presentation never touches GalaxyMapEntity directly.

using System;
using System.Collections.Generic;
using GalacticEmpire.Feature.Galaxy.Domain;

namespace GalacticEmpire.Feature.Galaxy.Application
{
    /// <summary>Defines galaxy map use cases - exploration, claiming, navigation.</summary>
    public interface IGalaxyService
    {
        // Returns the full galaxy map
        GalaxyMapEntity GetGalaxy();

        // Generates a new galaxy - call once on first launch
        GalaxyMapEntity GenerateGalaxy(int sectorCount);

        // Fleet entered a sector - reveals it on the map
        SectorEntity DiscoverSector(Guid sectorId);

        // Player conquered a sector
        SectorEntity ClaimSector(Guid sectorId);

        // Player lost a sector to enemies
        SectorEntity LoseSector(Guid sectorId);

        // Returns all sectors reachable from a given sector
        IReadOnlyList<SectorEntity> GetReachableSectors(Guid sectorId);
    }
}
