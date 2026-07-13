// Core.Tests Unit tests for StationEntity domain logic.

using System;
using NUnit.Framework;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class StationEntityTests
    {
        // Create()

        [Test]
        public void Create_WithValidData_CreatesEmptyGrid()
        {
            var station = StationEntity.Create("Home Base", gridSize: 6);

            Assert.That(station.Grid.Count, Is.EqualTo(36)); // 6x6
            Assert.That(station.TotalModules, Is.EqualTo(0));
        }

        [Test]
        public void Create_WithEmptyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                StationEntity.Create("", gridSize: 6));
        }

        [Test]
        public void Create_WithInvalidGridSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StationEntity.Create("Home Base", gridSize: 1));
        }

        // PlaceModule()

        [Test]
        public void PlaceModule_OnEmptyCell_AddsModuleToStation()
        {
            var station = StationEntity.Create("Home Base");
            var module  = CreateTestModule();

            var updated = station.PlaceModule(module, 0, 0);

            Assert.That(updated.TotalModules, Is.EqualTo(1));
            Assert.That(updated.GetCell(0, 0).IsEmpty, Is.False);
        }

        [Test]
        public void PlaceModule_DoesNotModifyOriginalStation()
        {
            var station = StationEntity.Create("Home Base");
            var module  = CreateTestModule();

            station.PlaceModule(module, 0, 0);

            Assert.That(station.TotalModules, Is.EqualTo(0));
        }

        [Test]
        public void PlaceModule_OnOccupiedCell_ThrowsInvalidOperationException()
        {
            var station = StationEntity.Create("Home Base");
            var module1 = CreateTestModule();
            var module2 = CreateTestModule();

            var updated = station.PlaceModule(module1, 0, 0);

            Assert.Throws<InvalidOperationException>(() =>
                updated.PlaceModule(module2, 0, 0));
        }

        // RemoveModule()

        [Test]
        public void RemoveModule_FromOccupiedCell_RemovesModule()
        {
            var station = StationEntity.Create("Home Base");
            var module  = CreateTestModule();
            var placed  = station.PlaceModule(module, 0, 0);

            var removed = placed.RemoveModule(0, 0);

            Assert.That(removed.TotalModules, Is.EqualTo(0));
            Assert.That(removed.GetCell(0, 0).IsEmpty, Is.True);
        }

        [Test]
        public void RemoveModule_FromEmptyCell_ThrowsInvalidOperationException()
        {
            var station = StationEntity.Create("Home Base");

            Assert.Throws<InvalidOperationException>(() =>
                station.RemoveModule(0, 0));
        }

        // GetTotalProduction()

        [Test]
        public void GetTotalProduction_WithBuiltModules_SumsProduction()
        {
            var station = StationEntity.Create("Home Base");
            var module  = CreateTestModule().AdvanceConstruction(1f); // fully built

            var updated = station.PlaceModule(module, 0, 0);

            float production = updated.GetTotalProduction(ResourceType.Metal);

            Assert.That(production, Is.EqualTo(10f));
        }

        [Test]
        public void GetTotalProduction_WithUnbuiltModules_ReturnsZero()
        {
            var station = StationEntity.Create("Home Base");
            var module  = CreateTestModule(); // not built yet

            var updated = station.PlaceModule(module, 0, 0);

            float production = updated.GetTotalProduction(ResourceType.Metal);

            Assert.That(production, Is.EqualTo(0f));
        }

        // GetCell()

        [Test]
        public void GetCell_OutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            var station = StationEntity.Create("Home Base", gridSize: 6);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                station.GetCell(99, 99));
        }

        // Helpers

        private static StationModuleEntity CreateTestModule()
        {
            return StationModuleEntity.Create(
                name: "Metal Mine",
                type: StationModuleType.MetalMine,
                category: StationModuleCategory.Production,

                producedResource: ResourceType.Metal,
                productionRate: 10f,
                buildCostMetal: 50f,
                buildCostEnergy: 20f);
        }
    }
}
