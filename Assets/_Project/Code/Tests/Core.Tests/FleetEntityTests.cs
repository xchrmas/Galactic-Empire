// Layer: Core.Tests - Unit tests for FleetEntity domain logic.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class FleetEntityTests
    {
        private ShipEntity CreateTestShip(string name = "TestShip", float maxHull = 100f, float damage = 25f, float speed = 10f)
        {
            return ShipEntity.Create(name, maxHull, damage, speed);
        }

        // Create

        [Test]
        public void Create_WithValidData_ReturnsFleetWithIdleStatus()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();

            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            Assert.That(fleet.Name, Is.EqualTo("Alpha Fleet"));
            Assert.That(fleet.Status, Is.EqualTo(FleetStatus.Idle));
            Assert.That(fleet.ShipCount, Is.EqualTo(1));
            Assert.That(fleet.IsDestroyed, Is.False);
        }

        [Test]
        public void Create_WithMultipleShips_IncludesAllShips()
        {
            var ship1 = CreateTestShip("Destroyer");
            var ship2 = CreateTestShip("Cruiser");
            var ship3 = CreateTestShip("Battleship");
            var ships = new List<ShipEntity> { ship1, ship2, ship3 }.AsReadOnly();

            var fleet = FleetEntity.Create("Battle Fleet", ships);

            Assert.That(fleet.ShipCount, Is.EqualTo(3));
        }

        [Test]
        public void Create_WithEmptyName_ThrowsArgumentException()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();

            Assert.Throws<ArgumentException>(() => FleetEntity.Create("", ships));
        }

        [Test]
        public void Create_WithWhitespaceName_ThrowsArgumentException()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();

            Assert.Throws<ArgumentException>(() => FleetEntity.Create("   ", ships));
        }

        [Test]
        public void Create_WithNullShips_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => FleetEntity.Create("Alpha Fleet", null));
        }

        [Test]
        public void Create_WithEmptyShipsList_ThrowsArgumentException()
        {
            var ships = new List<ShipEntity>().AsReadOnly();

            Assert.Throws<ArgumentException>(() => FleetEntity.Create("Alpha Fleet", ships));
        }

        // Dispatch

        [Test]
        public void Dispatch_ChangesStatusToMoving()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            var dispatched = fleet.Dispatch();

            Assert.That(dispatched.Status, Is.EqualTo(FleetStatus.Moving));
        }

        [Test]
        public void Dispatch_DoesNotModifyOriginalFleet()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            fleet.Dispatch();

            Assert.That(fleet.Status, Is.EqualTo(FleetStatus.Idle));
        }

        [Test]
        public void Dispatch_WhenFleetIsDestroyed_ThrowsInvalidOperationException()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);
            var destroyed = fleet with { Status = FleetStatus.Destroyed };

            Assert.Throws<InvalidOperationException>(() => destroyed.Dispatch());
        }

        [Test]
        public void Dispatch_WhenFleetIsInBattle_ThrowsInvalidOperationException()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships).EnterBattle();

            Assert.Throws<InvalidOperationException>(() => fleet.Dispatch());
        }

        // Recall

        [Test]
        public void Recall_ChangesStatusToIdle()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships).Dispatch();

            var recalled = fleet.Recall();

            Assert.That(recalled.Status, Is.EqualTo(FleetStatus.Idle));
        }

        [Test]
        public void Recall_WhenFleetIsDestroyed_ThrowsInvalidOperationException()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);
            var destroyed = fleet with { Status = FleetStatus.Destroyed };

            Assert.Throws<InvalidOperationException>(() => destroyed.Recall());
        }

        // EnterBattle

        [Test]
        public void EnterBattle_ChangesStatusToInBattle()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships).Dispatch();

            var inBattle = fleet.EnterBattle();

            Assert.That(inBattle.Status, Is.EqualTo(FleetStatus.InBattle));
        }

        [Test]
        public void EnterBattle_WhenFleetIsDestroyed_ThrowsInvalidOperationException()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);
            var destroyed = fleet with { Status = FleetStatus.Destroyed };

            Assert.Throws<InvalidOperationException>(() => destroyed.EnterBattle());
        }

        // Retreat

        [Test]
        public void Retreat_ChangesStatusToRetreating()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships).Dispatch().EnterBattle();

            var retreating = fleet.Retreat();

            Assert.That(retreating.Status, Is.EqualTo(FleetStatus.Retreating));
        }

        // ApplyBattleDamage

        [Test]
        public void ApplyBattleDamage_ReducesTargetShipHull()
        {
            var ship = CreateTestShip("Destroyer", maxHull: 100f);
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            var damaged = fleet.ApplyBattleDamage(ship.Id, 30f);

            var updatedShip = damaged.Ships[0];
            Assert.That(updatedShip.Hull, Is.EqualTo(70f));
        }

        [Test]
        public void ApplyBattleDamage_RemovesDestroyedShips()
        {
            var ship = CreateTestShip("Destroyer", maxHull: 50f);
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            var damaged = fleet.ApplyBattleDamage(ship.Id, 999f);

            Assert.That(damaged.ShipCount, Is.EqualTo(0));
        }

        [Test]
        public void ApplyBattleDamage_WhenAllShipsDestroyed_SetsFleetToDestroyed()
        {
            var ship = CreateTestShip("Destroyer", maxHull: 50f);
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            var damaged = fleet.ApplyBattleDamage(ship.Id, 999f);

            Assert.That(damaged.Status, Is.EqualTo(FleetStatus.Destroyed));
            Assert.That(damaged.IsDestroyed, Is.True);
        }

        [Test]
        public void ApplyBattleDamage_WithMultipleShips_OnlyDamagesTarget()
        {
            var ship1 = CreateTestShip("Destroyer", maxHull: 100f);
            var ship2 = CreateTestShip("Cruiser", maxHull: 150f);
            var ships = new List<ShipEntity> { ship1, ship2 }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            var damaged = fleet.ApplyBattleDamage(ship1.Id, 30f);

            var damagedShip = damaged.Ships[0];
            var undamagedShip = damaged.Ships[1];
            Assert.That(damagedShip.Hull, Is.EqualTo(70f));
            Assert.That(undamagedShip.Hull, Is.EqualTo(150f));
        }

        // MergeWith

        [Test]
        public void MergeWith_CombinesShipsFromBothFleets()
        {
            var ship1 = CreateTestShip("Destroyer");
            var ship2 = CreateTestShip("Cruiser");
            var fleet1 = FleetEntity.Create("Alpha Fleet", new List<ShipEntity> { ship1 }.AsReadOnly());
            var fleet2 = FleetEntity.Create("Beta Fleet", new List<ShipEntity> { ship2 }.AsReadOnly());

            var merged = fleet1.MergeWith(fleet2);

            Assert.That(merged.ShipCount, Is.EqualTo(2));
        }

        [Test]
        public void MergeWith_SetsMergedFleetToIdle()
        {
            var ship1 = CreateTestShip("Destroyer");
            var ship2 = CreateTestShip("Cruiser");
            var fleet1 = FleetEntity.Create("Alpha Fleet", new List<ShipEntity> { ship1 }.AsReadOnly()).Dispatch();
            var fleet2 = FleetEntity.Create("Beta Fleet", new List<ShipEntity> { ship2 }.AsReadOnly());

            var merged = fleet1.MergeWith(fleet2);

            Assert.That(merged.Status, Is.EqualTo(FleetStatus.Idle));
        }

        [Test]
        public void MergeWith_WithNullOther_ThrowsArgumentNullException()
        {
            var ship = CreateTestShip();
            var fleet = FleetEntity.Create("Alpha Fleet", new List<ShipEntity> { ship }.AsReadOnly());

            Assert.Throws<ArgumentNullException>(() => fleet.MergeWith(null));
        }

        [Test]
        public void MergeWith_WithDestroyedFleet_ThrowsInvalidOperationException()
        {
            var ship1 = CreateTestShip("Destroyer");
            var ship2 = CreateTestShip("Cruiser");
            var fleet1 = FleetEntity.Create("Alpha Fleet", new List<ShipEntity> { ship1 }.AsReadOnly());
            var fleet2 = FleetEntity.Create("Beta Fleet", new List<ShipEntity> { ship2 }.AsReadOnly());
            var destroyed = fleet2 with { Status = FleetStatus.Destroyed };

            Assert.Throws<InvalidOperationException>(() => fleet1.MergeWith(destroyed));
        }

        // Properties

        [Test]
        public void TotalHull_ComputesCorrectly()
        {
            var ship1 = CreateTestShip("Destroyer", maxHull: 100f);
            var ship2 = CreateTestShip("Cruiser", maxHull: 150f);
            var ships = new List<ShipEntity> { ship1, ship2 }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            Assert.That(fleet.TotalHull, Is.EqualTo(250f));
        }

        [Test]
        public void TotalDamage_ComputesCorrectly()
        {
            var ship1 = CreateTestShip("Destroyer", damage: 25f);
            var ship2 = CreateTestShip("Cruiser", damage: 35f);
            var ships = new List<ShipEntity> { ship1, ship2 }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            Assert.That(fleet.TotalDamage, Is.EqualTo(60f));
        }

        [Test]
        public void CombatPower_EqualsHullTimedDamage()
        {
            var ship1 = CreateTestShip("Destroyer", maxHull: 100f, damage: 25f);
            var ship2 = CreateTestShip("Cruiser", maxHull: 150f, damage: 35f);
            var ships = new List<ShipEntity> { ship1, ship2 }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            Assert.That(fleet.CombatPower, Is.EqualTo(250f * 60f));
        }

        [Test]
        public void IsDestroyed_ReturnsFalseForHealthyFleet()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);

            Assert.That(fleet.IsDestroyed, Is.False);
        }

        [Test]
        public void IsDestroyed_ReturnsTrueWhenStatusIsDestroyed()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);
            var destroyed = fleet with { Status = FleetStatus.Destroyed };

            Assert.That(destroyed.IsDestroyed, Is.True);
        }

        [Test]
        public void IsDestroyed_ReturnsTrueWhenNoShips()
        {
            var ship = CreateTestShip();
            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = FleetEntity.Create("Alpha Fleet", ships);
            var noShips = fleet with { Ships = new List<ShipEntity>().AsReadOnly() };

            Assert.That(noShips.IsDestroyed, Is.True);
        }
    }
}
