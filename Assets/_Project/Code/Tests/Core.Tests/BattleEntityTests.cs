// Tests for BattleEntity domain logic and CombatTickService simulation.

using System;
using System.Collections.Generic;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Domain;
using GalacticEmpire.Feature.Fleet.Domain;
using NUnit.Framework;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class BattleEntityTests
    {
        [Test]
        public void Create_WithValidFleets_StartsInPreparingStatus()
        {
            var battle = CreateTestBattle();

            Assert.That(battle.Status, Is.EqualTo(BattleStatus.Preparing));
            Assert.That(battle.Tick, Is.EqualTo(0));
            Assert.That(battle.IsFinished, Is.False);
        }

        [Test]
        public void Create_WithDestroyedAttacker_ThrowsInvalidOperationException()
        {
            var attacker = CreateFleet("Attacker", hull: 100f, damage: 25f, ships: 1);
            var destroyed = attacker.ApplyBattleDamage(attacker.Ships[0].Id, 999f);
            var defender = CreateFleet("Defender", hull: 100f, damage: 25f, ships: 1);

            Assert.Throws<InvalidOperationException>(() =>
                BattleEntity.Create(destroyed, defender, Guid.NewGuid()));
        }

        [Test]
        public void Start_ChangesBothFleetsToInBattle()
        {
            var battle = CreateTestBattle().Start();

            Assert.That(battle.Status, Is.EqualTo(BattleStatus.Active));
            Assert.That(battle.AttackerFleet.Status, Is.EqualTo(FleetStatus.InBattle));
            Assert.That(battle.DefenderFleet.Status, Is.EqualTo(FleetStatus.InBattle));
        }

        [Test]
        public void SimulateTick_ReducesShipHull()
        {
            var battle = CreateTestBattle().Start();
            float hullBefore = battle.DefenderFleet.TotalHull;

            var updated = battle.SimulateTick();

            Assert.That(updated.DefenderFleet.TotalHull, Is.LessThan(hullBefore));
        }

        [Test]
        public void SimulateTick_IncreasesTick()
        {
            var battle = CreateTestBattle().Start();

            var updated = battle.SimulateTick();

            Assert.That(updated.Tick, Is.EqualTo(1));
        }

        [Test]
        public void SimulateTick_DoesNotModifyOriginalBattle()
        {
            var battle = CreateTestBattle().Start();

            battle.SimulateTick();

            Assert.That(battle.Tick, Is.EqualTo(0));
        }

        [Test]
        public void SimulateTick_WhenAttackerDestroyed_StatusBecomesFinished()
        {
            // Defender has massive damage - attacker dies in one tick
            var attacker = CreateFleet("Attacker", hull: 1f, damage: 1f, ships: 1);
            var defender = CreateFleet("Defender", hull: 1000f, damage: 9999f, ships: 1);
            var battle = BattleEntity.Create(attacker, defender, Guid.NewGuid()).Start();

            var result = battle.SimulateTick();

            Assert.That(result.Status, Is.EqualTo(BattleStatus.Finished));
            Assert.That(result.IsFinished, Is.True);
        }

        [Test]
        public void GetResult_WhenBattleNotFinished_ThrowsInvalidOperationException()
        {
            var battle = CreateTestBattle().Start();

            Assert.Throws<InvalidOperationException>(() => battle.GetResult());
        }

        [Test]
        public void GetResult_ReturnsCorrectWinner()
        {
            var attacker = CreateFleet("Attacker", hull: 1000f, damage: 9999f, ships: 1);
            var defender = CreateFleet("Defender", hull: 1f, damage: 1f, ships: 1);
            var battle = BattleEntity.Create(attacker, defender, Guid.NewGuid()).Start();

            var ticked = battle.SimulateTick();
            var result = ticked.GetResult();

            Assert.That(result.IsDraw, Is.False);
            Assert.That(result.WinnerFleetId, Is.EqualTo(attacker.Id));
        }

        [Test]
        public void GetResult_WhenBothDestroyed_IsDraw()
        {
            // Both fleets kill each other in one tick
            var attacker = CreateFleet("Attacker", hull: 1f, damage: 9999f, ships: 1);
            var defender = CreateFleet("Defender", hull: 1f, damage: 9999f, ships: 1);
            var battle = BattleEntity.Create(attacker, defender, Guid.NewGuid()).Start();

            var ticked = battle.SimulateTick();
            var result = ticked.GetResult();

            Assert.That(result.IsDraw, Is.True);
        }

        private static BattleEntity CreateTestBattle()
        {
            var attacker = CreateFleet("Attacker", hull: 100f, damage: 25f, ships: 2);
            var defender = CreateFleet("Defender", hull: 100f, damage: 20f, ships: 2);
            return BattleEntity.Create(attacker, defender, Guid.NewGuid());
        }

        private static FleetEntity CreateFleet(string name, float hull, float damage, int ships)
        {
            var shipList = new List<ShipEntity>();
            for (int i = 0; i < ships; i++)
                shipList.Add(ShipEntity.Create($"Ship_{i}", hull, damage, 10f));
            return FleetEntity.Create(name, shipList.AsReadOnly());
        }
    }
}