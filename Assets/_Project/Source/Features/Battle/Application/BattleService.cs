// Orchestrates battle flow - delegates simulation to CombatTickService
// and updates fleet states after the battle ends.

using System;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Domain;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Battle.Application
{
    /// <summary>Starts battles and updates fleet states based on the outcome.</summary>
    public sealed class BattleService : IBattleService
    {
        private readonly CombatTickService _combatTickService;
        private readonly IFleetRepository _fleetRepository;

        private BattleResult _lastResult;

        public BattleService(CombatTickService combatTickService, IFleetRepository fleetRepository)
        {
            _combatTickService = combatTickService;
            _fleetRepository = fleetRepository;
        }

        /// <summary>Runs a full battle and updates both fleets with the outcome.</summary>
        public BattleResult StartBattle(FleetEntity attacker, FleetEntity defender, Guid sectorId)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));

            var result = _combatTickService.ResolveBattle(attacker, defender, sectorId);
            PersistResult(result);
            return result;
        }

        /// <summary>Returns the last battle result.</summary>
        public BattleResult GetLastResult() => _lastResult;

        /// <summary>Creates and starts a battle without resolving it - caller drives ticks.</summary>
        public BattleEntity CreateBattle(FleetEntity attacker, FleetEntity defender, Guid sectorId)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));

            GELogger.Info(LogCategory.Battle,
                $"Real-time battle started in sector {sectorId}. " +
                $"Attacker: {attacker.Name} ({attacker.ShipCount} ships) vs " +
                $"Defender: {defender.Name} ({defender.ShipCount} ships)");

            return BattleEntity.Create(attacker, defender, sectorId).Start();
        }

        /// <summary>Persists the outcome of a finished battle, tick-driven or instant alike.</summary>
        public BattleResult ApplyBattleResult(BattleEntity finishedBattle)
        {
            if (finishedBattle == null) throw new ArgumentNullException(nameof(finishedBattle));

            var result = finishedBattle.GetResult();
            PersistResult(result);
            return result;
        }

        // Shared tail for both the instant (StartBattle) and tick-driven
        // (ApplyBattleResult) paths - updates the repository and remembers
        // the outcome for GetLastResult.
        private void PersistResult(BattleResult result)
        {
            if (!result.IsDraw)
            {
                UpdateFleetAfterBattle(result.WinnerFleet);
                UpdateFleetAfterBattle(result.LoserFleet);
            }

            _lastResult = result;

            GELogger.Info(LogCategory.Battle,
                result.IsDraw
                    ? "Battle ended in a draw."
                    : $"Battle won by fleet {result.WinnerFleetId} in {result.TotalTicks} ticks.");
        }

        private void UpdateFleetAfterBattle(FleetEntity fleet)
        {
            if (fleet == null) return;


            foreach (var ship in fleet.Ships)
            {
                try
                {
                    _fleetRepository.Replace(ship);
                }
                catch (InvalidOperationException)
                {
                    // Ship not tracked in the repository - expected for NPC fleets,
                    // logged at Info (not Warning) since this is the normal path for
                    // every real-time battle against an EnemyFleetEntity opponent.
                    GELogger.Info(LogCategory.Battle,
                        $"Ship {ship.Id} not in IFleetRepository - skipped (NPC/ephemeral fleet).");
                }
            }
        }
    }
}
