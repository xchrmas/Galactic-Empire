//  Core.Tests  Unit tests for StationModuleEntity domain logic.

using System;
using NUnit.Framework;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class StationModuleEntityTests
    {
        [Test]
        public void Create_WithValidData_StartsUnbuilt()
        {
            var module = CreateModule();

            Assert.That(module.IsBuilt, Is.False);
            Assert.That(module.ConstructionProgress, Is.EqualTo(0f));
            Assert.That(module.Level, Is.EqualTo(1));
        }

        [Test]
        public void Create_WithEmptyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                StationModuleEntity.Create("", StationModuleType.MetalMine,
                    StationModuleCategory.Production, ResourceType.Metal, 10f, 50f, 20f));
        }

        [Test]
        public void AdvanceConstruction_IncreasesProgress()
        {
            var module = CreateModule();

            var updated = module.AdvanceConstruction(0.5f);

            Assert.That(updated.ConstructionProgress, Is.EqualTo(0.5f));
            Assert.That(updated.IsBuilt, Is.False);
        }

        [Test]
        public void AdvanceConstruction_ReachingFullProgress_MarksAsBuilt()
        {
            var module = CreateModule();

            var updated = module.AdvanceConstruction(1f);

            Assert.That(updated.IsBuilt, Is.True);
            Assert.That(updated.IsUnderConstruction, Is.False);
        }

        [Test]
        public void AdvanceConstruction_OnAlreadyBuiltModule_DoesNothing()
        {
            var module = CreateModule().AdvanceConstruction(1f);

            var updated = module.AdvanceConstruction(0.5f);

            Assert.That(updated.ConstructionProgress, Is.EqualTo(1f));
        }

        [Test]
        public void Upgrade_OnBuiltModule_IncreasesLevelAndProduction()
        {
            var module = CreateModule().AdvanceConstruction(1f);

            var upgraded = module.Upgrade(productionRateBonus: 5f);

            Assert.That(upgraded.Level, Is.EqualTo(2));
            Assert.That(upgraded.ProductionRate, Is.EqualTo(15f));
        }

        [Test]
        public void Upgrade_OnUnbuiltModule_ThrowsInvalidOperationException()
        {
            var module = CreateModule();

            Assert.Throws<InvalidOperationException>(() =>
                module.Upgrade(5f));
        }

        private static StationModuleEntity CreateModule()
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
