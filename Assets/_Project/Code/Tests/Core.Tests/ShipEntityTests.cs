// Layer: Core.Tests - Unit tests for ShipEntity domain logic.
//
// 2026-10-21: added explicit "using GalacticEmpire.Feature.Fleet.Domain;" -
// ShipEntity migrated there from GalacticEmpire.Core (Stage 1c). This file
// previously had no using for ShipEntity's namespace at all; if that
// somehow compiled before, it was relying on GalacticEmpire.Core.Tests'
// namespace prefix matching GalacticEmpire.Core, which is not how C# type
// resolution works - this explicit using is correct either way.

using System;
using GalacticEmpire.Feature.Fleet.Domain;
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


        // Stage 1c - module slots

        [Test]
        public void CreateWithSlots_BuildsEmptySlotsInGivenOrder()
        {
            var categories = new[] { ShipModuleCategory.Weapon, ShipModuleCategory.Armor };

            var ship = ShipEntity.CreateWithSlots("Destroyer", 100f, 25f, 10f, categories);

            Assert.That(ship.Slots.Count, Is.EqualTo(2));
            Assert.That(ship.Slots[0].Category, Is.EqualTo(ShipModuleCategory.Weapon));
            Assert.That(ship.Slots[1].Category, Is.EqualTo(ShipModuleCategory.Armor));
            Assert.That(ship.Slots[0].IsEmpty, Is.True);
        }

        [Test]
        public void InstallModule_AddsBonusToMaxHullAndCurrentHull()
        {
            var categories = new[] { ShipModuleCategory.Armor };
            var ship = ShipEntity.CreateWithSlots("Destroyer", 100f, 25f, 10f, categories);
            var module = ShipModuleEntity.Create("Plating", ShipModuleCategory.Armor, 20f, 0f, 0f);

            var updated = ship.InstallModule(0, module);

            Assert.That(updated.MaxHull, Is.EqualTo(120f));
            Assert.That(updated.Hull, Is.EqualTo(120f));
            Assert.That(updated.Slots[0].IsEmpty, Is.False);
        }

        [Test]
        public void InstallModule_OnDamagedShip_PreservesExistingDamage()
        {
            var categories = new[] { ShipModuleCategory.Armor };
            var ship = ShipEntity.CreateWithSlots("Destroyer", 100f, 25f, 10f, categories).TakeDamage(40f);
            var module = ShipModuleEntity.Create("Plating", ShipModuleCategory.Armor, 20f, 0f, 0f);

            var updated = ship.InstallModule(0, module);

            // 100 - 40 damage = 60 hull, +20 bonus = 80, not a free heal to 120
            Assert.That(updated.Hull, Is.EqualTo(80f));
            Assert.That(updated.MaxHull, Is.EqualTo(120f));
        }

        [Test]
        public void InstallModule_WrongCategory_ThrowsInvalidOperationException()
        {
            var categories = new[] { ShipModuleCategory.Weapon };
            var ship = ShipEntity.CreateWithSlots("Destroyer", 100f, 25f, 10f, categories);
            var module = ShipModuleEntity.Create("Plating", ShipModuleCategory.Armor, 20f, 0f, 0f);

            Assert.Throws<InvalidOperationException>(() => ship.InstallModule(0, module));
        }

        [Test]
        public void RemoveModule_SubtractsBonusAndClampsCurrentHull()
        {
            var categories = new[] { ShipModuleCategory.Armor };
            var ship = ShipEntity.CreateWithSlots("Destroyer", 100f, 25f, 10f, categories);
            var module = ShipModuleEntity.Create("Plating", ShipModuleCategory.Armor, 20f, 0f, 0f);
            var withModule = ship.InstallModule(0, module);

            var removed = withModule.RemoveModule(0);

            Assert.That(removed.MaxHull, Is.EqualTo(100f));
            Assert.That(removed.Hull, Is.EqualTo(100f));
            Assert.That(removed.Slots[0].IsEmpty, Is.True);
        }

        [Test]
        public void InstallModule_InvalidSlotIndex_ThrowsArgumentOutOfRangeException()
        {
            var ship = ShipEntity.Create("Destroyer", 100f, 25f, 10f); // no slots
            var module = ShipModuleEntity.Create("Plating", ShipModuleCategory.Armor, 20f, 0f, 0f);

            Assert.Throws<ArgumentOutOfRangeException>(() => ship.InstallModule(0, module));
        }
    }
}
