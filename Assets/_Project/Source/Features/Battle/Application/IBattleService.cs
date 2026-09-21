// Contract for starting and managing battles.
// Presentation layer calls this - never touches BattleEntity directly.

using System;
using GalacticEmpire.Feature.Battle.Domain;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Battle.Application
{
    /// <summary>Defines battle use cases for the presentation layer.</summary>
    public interface IBattleService
    {
        // Start a battle between two fleets in a sector - returns the result immediately
        BattleResult StartBattle(FleetEntity attacker, FleetEntity defender, Guid sectorId);

        // Returns the last battle result, null if no battle happened yet
        BattleResult GetLastResult();

        // Creates and starts a battle without resolving it - used by BattlePresenter to
        // drive the fight tick by tick via CombatTickService.Tick for real-time display.
        BattleEntity CreateBattle(FleetEntity attacker, FleetEntity defender, Guid sectorId);

        // Persists the outcome of a battle driven tick by tick by BattlePresenter -
        // same fleet-update logic StartBattle uses internally for the instant path,
        // exposed here so Presentation never touches the repository directly.
        BattleResult ApplyBattleResult(BattleEntity finishedBattle);
    }
}
