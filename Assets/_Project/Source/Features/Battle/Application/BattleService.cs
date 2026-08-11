// Orchestrates battle flow - delegates simulation to CombatTickService
// and updates fleet states after the battle ends.

using System;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Domain;
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

            // Update surviving ships in the repository
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

            return result;
        }

        /// <summary>Returns the last battle result.</summary>
        public BattleResult GetLastResult() => _lastResult;

        private void UpdateFleetAfterBattle(FleetEntity fleet)
        {
            if (fleet == null) return;

            // Update each surviving ship in the repository
            foreach (var ship in fleet.Ships)
                _fleetRepository.Replace(ship);
        }
    }
}