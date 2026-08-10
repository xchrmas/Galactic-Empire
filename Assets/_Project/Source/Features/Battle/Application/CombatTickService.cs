// Runs the battle simulation tick by tick.
// Phase 1: pure C# logic, no DOTS yet.
// Phase 2: will be replaced with Burst-compiled ECS systems.

using System;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Domain;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Battle.Application
{
    /// <summary>Simulates a battle between two fleets tick by tick.</summary>
    public sealed class CombatTickService
    {
        private readonly GameConfigSO _config;

        // Safety cap - prevents infinite loops if something goes wrong
        private const int MaxTicks = 1000;

        public CombatTickService(GameConfigSO config)
        {
            _config = config;
        }

        /// <summary>
        /// Runs a full battle to completion and returns the result.
        /// Used for instant resolution - no animation delay.
        /// </summary>
        public BattleResult ResolveBattle(FleetEntity attacker, FleetEntity defender, Guid sectorId)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));

            var battle = BattleEntity.Create(attacker, defender, sectorId).Start();

            GELogger.Info(LogCategory.Battle,
                $"Battle started in sector {sectorId}. " +
                $"Attacker: {attacker.Name} ({attacker.ShipCount} ships) vs " +
                $"Defender: {defender.Name} ({defender.ShipCount} ships)");

            // Simulate ticks until battle ends or safety cap reached
            int ticks = 0;
            while (!battle.IsFinished && ticks < MaxTicks)
            {
                battle = battle.SimulateTick();
                ticks++;
            }

            if (ticks >= MaxTicks)
                GELogger.Warning(LogCategory.Battle, $"Battle hit max tick limit ({MaxTicks}). Forcing draw.");

            var result = battle.GetResult();

            LogResult(result);
            return result;
        }

        /// <summary>
        /// Simulates a single tick - used for real-time battle visualization in Phase 2.
        /// </summary>
        public BattleEntity Tick(BattleEntity battle)
        {
            if (battle.IsFinished)
                return battle;

            return battle.SimulateTick();
        }

        private static void LogResult(BattleResult result)
        {
            if (result.IsDraw)
            {
                GELogger.Info(LogCategory.Battle,
                    $"Battle ended in a DRAW after {result.TotalTicks} ticks.");
                return;
            }

            GELogger.Info(LogCategory.Battle,
                $"Battle finished after {result.TotalTicks} ticks. " +
                $"Winner: Fleet {result.WinnerFleetId}");
        }
    }
}
