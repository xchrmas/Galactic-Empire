// In-memory record of which NPC fleet currently guards which sector.
// Same pattern as FleetService's in-memory _fleets list (BACKLOG-26) - no
// persistence, tied to one client process. NPC garrisons are ephemeral by
// design (see EnemyFleetEntity), so this is a deliberate fit, not a gap.

using System;
using GalacticEmpire.Feature.Battle.Domain;

namespace GalacticEmpire.Feature.Battle.Application
{
    /// <summary>Tracks the NPC fleet guarding each sector, if any.</summary>
    public interface ISectorGarrisonRepository
    {
        // Returns the enemy fleet guarding this sector, null if none or already cleared
        EnemyFleetEntity Get(Guid sectorId);

        // Records a newly generated enemy fleet as the sector's garrison
        void Set(Guid sectorId, EnemyFleetEntity enemyFleet);

        // Removes the garrison - called once the enemy fleet is defeated
        void Clear(Guid sectorId);

        // True if this sector's garrison has already been generated (win or lose,
        // don't regenerate on every visit - see IEncounterService.CheckForEncounter)
        bool HasBeenGenerated(Guid sectorId);
    }
}
