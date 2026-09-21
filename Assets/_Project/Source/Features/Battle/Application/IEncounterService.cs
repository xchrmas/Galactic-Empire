// Orchestrates whether an arriving fleet meets an NPC garrison in a sector.
// Keeps Fleet.Application (FleetService.Dispatch) free of any Battle-domain
// knowledge - Presentation calls this after a successful Dispatch, not the
// other way around. See MASTER.md Section 3 Dependency Flow: Battle already
// depends on Fleet (IFleetRepository), never the reverse.

using System;
using GalacticEmpire.Feature.Battle.Domain;

namespace GalacticEmpire.Feature.Battle.Application
{
    /// <summary>Checks sectors for NPC garrisons and resolves them once defeated.</summary>
    public interface IEncounterService
    {
        // Returns the NPC fleet guarding this sector - generates one on first visit
        // if the sector is hostile enough, returns null if the sector is clear
        // (never had a garrison, or already cleared by the player).
        EnemyFleetEntity CheckForEncounter(Guid sectorId);

        // Marks the sector's garrison as cleared - call after the enemy is defeated.
        void ResolveEncounter(Guid sectorId);
    }
}
