// A battle between two fleets in a galaxy sector.
// Immutable aggregate - every tick returns a new BattleEntity.

using System;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Battle.Domain
{
    /// <summary>Aggregate representing an active battle between two fleets.</summary>
    public sealed record BattleEntity
    {
        public Guid Id { get; init; }
        public Guid SectorId { get; init; }
        public FleetEntity AttackerFleet { get; init; }
        public FleetEntity DefenderFleet { get; init; }
        public BattleStatus Status { get; init; }
        public int Tick { get; init; }

        public bool IsFinished => Status == BattleStatus.Finished || Status == BattleStatus.Draw;

        /// <summary>Creates a new battle between two fleets in a sector.</summary>
        public static BattleEntity Create(
            FleetEntity attacker,
            FleetEntity defender,
            Guid sectorId)
        {
            if (attacker == null)
                throw new ArgumentNullException(nameof(attacker));

            if (defender == null)
                throw new ArgumentNullException(nameof(defender));

            if (attacker.IsDestroyed)
                throw new InvalidOperationException("Attacker fleet is already destroyed.");

            if (defender.IsDestroyed)
                throw new InvalidOperationException("Defender fleet is already destroyed.");

            return new BattleEntity
            {
                Id = Guid.NewGuid(),
                SectorId = sectorId,
                AttackerFleet = attacker.EnterBattle(),
                DefenderFleet = defender.EnterBattle(),
                Status = BattleStatus.Preparing,
                Tick = 0
            };
        }

        /// <summary>Starts the battle - both fleets are ready to fight.</summary>
        public BattleEntity Start()
        {
            if (Status != BattleStatus.Preparing)
                throw new InvalidOperationException("Battle already started.");

            return this with { Status = BattleStatus.Active };
        }

        /// <summary>
        /// Advances the battle by one tick.
        /// Each fleet deals damage equal to its TotalDamage split across enemy ships.
        /// </summary>
        public BattleEntity SimulateTick()
        {
            if (Status != BattleStatus.Active)
                return this;

            // Each fleet deals its total damage to a random enemy ship
            var updatedAttacker = ApplyDamageToFleet(AttackerFleet, DefenderFleet.TotalDamage);
            var updatedDefender = ApplyDamageToFleet(DefenderFleet, AttackerFleet.TotalDamage);

            var newStatus = DetermineStatus(updatedAttacker, updatedDefender);

            return this with
            {
                AttackerFleet = updatedAttacker,
                DefenderFleet = updatedDefender,
                Status = newStatus,
                Tick = Tick + 1
            };
        }

        /// <summary>Builds the final BattleResult from this battle state.</summary>
        public BattleResult GetResult()
        {
            if (!IsFinished)
                throw new InvalidOperationException("Battle is not finished yet.");

            if (Status == BattleStatus.Draw)
            {
                return BattleResult.Draw(
                    battleId: Id,
                    attackerFleetId: AttackerFleet.Id,
                    defenderFleetId: DefenderFleet.Id,
                    ticks: Tick,
                    attackerLost: 0,
                    defenderLost: 0);
            }

            bool attackerWon = !AttackerFleet.IsDestroyed;
            var winner = attackerWon ? AttackerFleet : DefenderFleet;
            var loser = attackerWon ? DefenderFleet : AttackerFleet;

            return BattleResult.Victory(
                battleId: Id,
                winner: winner,
                loser: loser,
                attackerFleetId: AttackerFleet.Id,
                ticks: Tick,
                attackerLost: 0,
                defenderLost: 0);
        }

        // Deals damage to the first living ship in the fleet
        private static FleetEntity ApplyDamageToFleet(FleetEntity fleet, float damage)
        {
            if (fleet.IsDestroyed || damage <= 0f)
                return fleet;

            // Target the first living ship
            var target = fleet.Ships[0];
            return fleet.ApplyBattleDamage(target.Id, damage);
        }

        private static BattleStatus DetermineStatus(FleetEntity attacker, FleetEntity defender)
        {
            if (attacker.IsDestroyed && defender.IsDestroyed)
                return BattleStatus.Draw;

            if (attacker.IsDestroyed || defender.IsDestroyed)
                return BattleStatus.Finished;

            return BattleStatus.Active;
        }
    }
}