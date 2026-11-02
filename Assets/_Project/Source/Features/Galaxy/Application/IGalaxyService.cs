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

        // Player conquered a sector. Throws if the sector isn't discovered
        // yet, or (Stage 1d) if SetSectorClearCheck was wired and reports a
        // live garrison still present in the sector.
        SectorEntity ClaimSector(Guid sectorId);

        // Player lost a sector to enemies
        SectorEntity LoseSector(Guid sectorId);

        // Returns all sectors reachable from a given sector
        IReadOnlyList<SectorEntity> GetReachableSectors(Guid sectorId);

        // Removes the saved galaxy - used to reset state at game start
        void ClearGalaxy();

        // Stage 1d (Claiming) - lets GameEntryPoint plug in a "does this
        // sector still have a live garrison" check without Galaxy.Application
        // referencing Battle.Application directly (same constraint and same
        // callback shape as IExplorationService.SetEncounterCheck - Battle
        // already depends on Galaxy via IGalaxyService, so a reference back
        // from here would be circular). ClaimSector enforces this check at
        // the Application layer - not just as a UI-only guard in
        // GalaxyMapScreen - so no caller can bypass it, scripted or not.
        // Optional: if never wired, ClaimSector only enforces the
        // IsDiscovered precondition SectorEntity.Claim already had.
        void SetSectorClearCheck(Func<Guid, bool> isSectorClear);
    }
}
