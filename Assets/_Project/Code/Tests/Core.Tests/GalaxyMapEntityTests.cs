// Tests for GalaxyMapEntity and SectorEntity domain logic.

using System;
using System.Collections.Generic;
using GalacticEmpire.Feature.Galaxy.Domain;
using NUnit.Framework;
using UnityEngine;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class GalaxyMapEntityTests
    {
        [Test]
        public void Create_ReturnsEmptyGalaxy()
        {
            var galaxy = GalaxyMapEntity.Create();

            Assert.That(galaxy.TotalSectors, Is.EqualTo(0));
            Assert.That(galaxy.ExplorationProgress, Is.EqualTo(0f));
        }

        [Test]
        public void CreateFromSectors_WithValidSectors_ReturnGalaxy()
        {
            var sectors = CreateTestSectors(3);

            var galaxy = GalaxyMapEntity.CreateFromSectors(sectors);

            Assert.That(galaxy.TotalSectors, Is.EqualTo(3));
        }

        [Test]
        public void CreateFromSectors_WithEmptyList_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                GalaxyMapEntity.CreateFromSectors(new List<SectorEntity>()));
        }

        [Test]
        public void GetSector_WithValidId_ReturnsSector()
        {
            var sector = CreateSector("Alpha");
            var galaxy = GalaxyMapEntity.CreateFromSectors(new List<SectorEntity> { sector });

            var found = galaxy.GetSector(sector.Id);

            Assert.That(found, Is.Not.Null);
            Assert.That(found.Name, Is.EqualTo("Alpha"));
        }

        [Test]
        public void GetSector_WithInvalidId_ReturnsNull()
        {
            var galaxy = GalaxyMapEntity.CreateFromSectors(CreateTestSectors(2));

            var found = galaxy.GetSector(Guid.NewGuid());

            Assert.That(found, Is.Null);
        }

        [Test]
        public void UpdateSector_ReplacesCorrectSector()
        {
            var sector = CreateSector("Alpha");
            var galaxy = GalaxyMapEntity.CreateFromSectors(new List<SectorEntity> { sector });

            var discovered = sector.Discover();
            var updated    = galaxy.UpdateSector(discovered);

            Assert.That(updated.GetSector(sector.Id).IsDiscovered, Is.True);
        }

        [Test]
        public void UpdateSector_DoesNotModifyOriginalGalaxy()
        {
            var sector = CreateSector("Alpha");
            var galaxy = GalaxyMapEntity.CreateFromSectors(new List<SectorEntity> { sector });

            galaxy.UpdateSector(sector.Discover());

            Assert.That(galaxy.GetSector(sector.Id).IsDiscovered, Is.False);
        }

        [Test]
        public void ExplorationProgress_UpdatesAfterDiscovery()
        {
            var sectors = CreateTestSectors(4);
            var galaxy  = GalaxyMapEntity.CreateFromSectors(sectors);

            // Discover 2 out of 4
            galaxy = galaxy.UpdateSector(sectors[0].Discover());
            galaxy = galaxy.UpdateSector(sectors[1].Discover());

            Assert.That(galaxy.ExplorationProgress, Is.EqualTo(0.5f));
        }

        [Test]
        public void GetOwnedSectors_ReturnsOnlyOwnedSectors()
        {
            var sector1 = CreateSector("Alpha").Discover().Claim();
            var sector2 = CreateSector("Beta");
            var galaxy  = GalaxyMapEntity.CreateFromSectors(new List<SectorEntity> { sector1, sector2 });

            var owned = galaxy.GetOwnedSectors();

            Assert.That(owned.Count, Is.EqualTo(1));
            Assert.That(owned[0].Name, Is.EqualTo("Alpha"));
        }

        [Test]
        public void GetDiscoveredUnownedSectors_ReturnsCorrectSectors()
        {
            var sector1 = CreateSector("Alpha").Discover().Claim(); // owned
            var sector2 = CreateSector("Beta").Discover();          // discovered not owned
            var sector3 = CreateSector("Gamma");                    // undiscovered
            var galaxy  = GalaxyMapEntity.CreateFromSectors(
                new List<SectorEntity> { sector1, sector2, sector3 });

            var targets = galaxy.GetDiscoveredUnownedSectors();

            Assert.That(targets.Count, Is.EqualTo(1));
            Assert.That(targets[0].Name, Is.EqualTo("Beta"));
        }

        [Test]
        public void SectorEntity_Claim_OnUndiscoveredSector_ThrowsInvalidOperationException()
        {
            var sector = CreateSector("Alpha");

            Assert.Throws<InvalidOperationException>(() => sector.Claim());
        }

        [Test]
        public void SectorEntity_Discover_ThenClaim_Works()
        {
            var sector = CreateSector("Alpha").Discover().Claim();

            Assert.That(sector.IsDiscovered, Is.True);
            Assert.That(sector.IsOwned, Is.True);
        }

        private static SectorEntity CreateSector(string name)
        {
            return SectorEntity.Create(name, SectorType.Safe, Vector2.zero);
        }

        private static IReadOnlyList<SectorEntity> CreateTestSectors(int count)
        {
            var list = new List<SectorEntity>();
            for (int i = 0; i < count; i++)
                list.Add(SectorEntity.Create($"Sector_{i}", SectorType.Safe, new Vector2(i, 0)));
            return list.AsReadOnly();
        }
    }
}
