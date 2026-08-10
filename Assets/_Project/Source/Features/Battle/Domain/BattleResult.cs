// The outcome of a finished battle.
// Immutable snapshot - created once when battle ends.

using System;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Battle.Domain
{
    /// <summary>Immutable record of what happened in a battle.</summary>
    public sealed record BattleResult
    {
        public Guid BattleId { get; init; }
        public Guid WinnerFleetId { get; init; }
        public Guid LoserFleetId { get; init; }
        public bool IsDraw { get; init; }

        // Surviving fleets after battle
        public FleetEntity WinnerFleet { get; init; }
        public FleetEntity LoserFleet { get; init; }

        // Battle stats
        public int TotalTicks { get; init; }
        public int AttackerShipsLost { get; init; }
        public int DefenderShipsLost { get; init; }

        public bool AttackerWon => WinnerFleetId == AttackerFleetId;
        public Guid AttackerFleetId { get; init; }
        public Guid DefenderFleetId { get; init; }

        /// <summary>Creates a result with a clear winner.</summary>
        public static BattleResult Victory(
            Guid battleId,
            FleetEntity winner,
            FleetEntity loser,
            Guid attackerFleetId,
            int ticks,
            int attackerLost,
            int defenderLost)
        {
            return new BattleResult
            {
                BattleId = battleId,
                WinnerFleetId = winner.Id,
                LoserFleetId = loser.Id,
                WinnerFleet = winner,
                LoserFleet = loser,
                IsDraw = false,
                AttackerFleetId = attackerFleetId,
                DefenderFleetId = attackerFleetId == winner.Id ? loser.Id : winner.Id,
                TotalTicks = ticks,
                AttackerShipsLost = attackerLost,
                DefenderShipsLost = defenderLost
            };
        }

        /// <summary>Creates a draw result - both fleets wiped out.</summary>
        public static BattleResult Draw(
            Guid battleId,
            Guid attackerFleetId,
            Guid defenderFleetId,
            int ticks,
            int attackerLost,
            int defenderLost)
        {
            return new BattleResult
            {
                BattleId = battleId,
                IsDraw = true,
                AttackerFleetId = attackerFleetId,
                DefenderFleetId = defenderFleetId,
                TotalTicks = ticks,
                AttackerShipsLost = attackerLost,
                DefenderShipsLost = defenderLost
            };
        }
    }
}