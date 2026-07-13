// Layer: Core.Tests - Unit tests for ShipEntity domain logic.

using System;
using NUnit.Framework;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class ShipEntityTests
    {
        // Create

        [Test]
        public void Create_WithValidData_ReturnsShipWithFullHull()
        {
            var ship = ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: 10f);

            Assert.That(ship.Hull, Is.EqualTo(ship.MaxHull));
            Assert.That(ship.IsAlive, Is.True);
            Assert.That(ship.HullPercent, Is.EqualTo(1f));
        }

        [Test]
        public void Create_WithEmptyName_ThrowsArgumentException()
        {
              // An invalid ship must never exist — factory enforces this.
                Assert.Throws<ArgumentException>(() =>
                ShipEntity.Create("", maxHull: 100f, damage: 25f, speed: 10f));
        }

        [Test]
        public void Create_WithZeroHull_ThrowsArgumentOutOfRangeException()
        {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                ShipEntity.Create("Destroyer", maxHull: 0f, damage: 25f, speed: 10f));
        }

        [Test]
        public void Create_WithNegativeSpeed_ThrowsArgumentOutOfRangeException()
        {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: -1f));
        }

        // TakeDamage

        [Test]
        public void TakeDamage_ReducesHullByAmount()
        {
            var ship = ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: 10f);

            var damaged = ship.TakeDamage(30f);

            Assert.That(damaged.Hull, Is.EqualTo(70f));
        }

        [Test]
        public void TakeDamage_DoesNotModifyOriginalShip()
        {
            // Records are immutable — TakeDamage must return NEW ship.
            var ship = ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: 10f);

            var damaged = ship.TakeDamage(30f);


            Assert.That(ship.Hull, Is.EqualTo(100f));   // original unchanged
            Assert.That(damaged.Hull, Is.EqualTo(70f)); // new ship has damage
        }

        [Test]
        public void TakeDamage_ExceedingHull_ClampsToZero()
        {
            var ship = ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: 10f);

            var destroyed = ship.TakeDamage(999f);

            Assert.That(destroyed.Hull, Is.EqualTo(0f));
            Assert.That(destroyed.IsAlive, Is.False);
        }

        [Test]
        public void TakeDamage_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            var ship = ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: 10f);

            Assert.Throws<ArgumentOutOfRangeException>(() => ship.TakeDamage(-10f));
        }

        // Repair

        [Test]
        public void Repair_RestoresShipToFullHull()
        {
            var ship    = ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: 10f);
            var damaged = ship.TakeDamage(60f);

            var repaired = damaged.Repair();

            Assert.That(repaired.Hull, Is.EqualTo(repaired.MaxHull));
            Assert.That(repaired.IsAlive, Is.True);
        }

        [Test]
        public void Repair_DoesNotModifyDamagedShip()
        {
            var ship    = ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: 10f);
            var damaged = ship.TakeDamage(60f);

            damaged.Repair();

            // damaged ship must remain unchanged after Repair() call
            Assert.That(damaged.Hull, Is.EqualTo(40f));
        }

    }
}
