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

        // Read-only check (Stage 1d, Claiming) - true if the sector has no
        // live, unbeaten garrison: either it never had one (safe sector, or
        // simply never checked yet - see EncounterService for why that's
        // still safe to treat as clear) or it did and was already cleared.
        // Unlike CheckForEncounter, this NEVER generates a garrison as a
        // side effect - a Claim button polling this on every sector click
        // must not accidentally spawn enemies into sectors the player
        // hasn't engaged yet.
        bool IsSectorClear(Guid sectorId);
    }
}
