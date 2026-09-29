// A battle between two fleets in a galaxy sector.
// Immutable aggregate - every tick returns a new BattleEntity.

using System;
using System.Linq;
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
        /// Each living ship on one side deals its own Damage to a single ship on the
        /// other side, round-robin by index over the target side's currently-living
        /// ships (ship i targets livingDefenders[i % livingDefenders.Count]). Both
        /// sides fire simultaneously against the state at the START of the tick, same
        /// as before - neither side benefits from acting "first".
        ///
        /// Replaces the previous "whole fleet's TotalDamage into Ships[0]" behaviour
        /// (balance tuning session, 2026-09-24): that formula meant a numerically
        /// superior fleet always deleted a single-ship opponent in one tick regardless
        /// of hull, since N attackers' damage always summed onto the one available
        /// target. Round-robin fixes this for any fight where the RECEIVING side has
        /// more than one ship - each attacker now only ever deals its own damage to
        /// one ship, so surviving a numerically superior fleet is possible if hull is
        /// high enough. Note: this does NOT change outcomes when the receiving side has
        /// only a single ship (e.g. player's 1-ship starting fleet) - with one possible
        /// target, every attacker still round-robins onto that same lone ship, so total
        /// damage to it per tick is still the sum of all attackers' individual damage.
        /// That specific case (1 ship vs N) is instead addressed by the Enemy* damage/hull
        /// constants in GameConfigSO, tuned this same session to stretch such fights to
        /// roughly 30-50 ticks instead of one-shotting - see battle.md for the numbers.
        /// </summary>
        public BattleEntity SimulateTick()
        {
            if (Status != BattleStatus.Active)
                return this;

            var updatedAttacker = ApplyRoundRobinDamage(source: DefenderFleet, target: AttackerFleet);
            var updatedDefender = ApplyRoundRobinDamage(source: AttackerFleet, target: DefenderFleet);

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

        // Round-robin: every living ship in `source` deals its own Damage to one ship
        // in `target`, cycling through target's currently-living ships by index. All
        // damage is computed against `target`'s state at the start of the tick (the
        // living-ship list is captured once, up front) so multiple source ships aimed
        // at the same cycle position stack onto that one target ship within this tick,
        // exactly like two ships focusing the same enemy - not against a shifting
        // mid-tick state.
        private static FleetEntity ApplyRoundRobinDamage(FleetEntity source, FleetEntity target)
        {
            if (source.IsDestroyed || target.IsDestroyed)
                return target;

            var livingTargets = target.Ships.Where(s => s.IsAlive).ToList();
            if (livingTargets.Count == 0)
                return target;

            var livingSources = source.Ships.Where(s => s.IsAlive).ToList();

            var updatedFleet = target;
            for (int i = 0; i < livingSources.Count; i++)
            {
                float damage = livingSources[i].Damage;
                if (damage <= 0f)
                    continue;

                var targetShip = livingTargets[i % livingTargets.Count];
                updatedFleet = updatedFleet.ApplyBattleDamage(targetShip.Id, damage);
            }

            return updatedFleet;
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
