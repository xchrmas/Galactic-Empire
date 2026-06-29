// Tests Performance Measures performance of Core fleet operations.


using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace GalacticEmpire.Core.Tests.Performance
{
    /// <summary>Performance benchmarks for fleet simulation operations.</summary>
    public sealed class FleetPerformanceTests
    {
        private const int SmallFleet  = 10;
        private const int MediumFleet = 100;
        private const int LargeFleet  = 1000;

        // ShipEntity.Create()

        [Test, Performance]
        public void Create_SmallFleet_Under1Ms()
        {
            Measure.Method(() =>
            {
                for (int i = 0; i < SmallFleet; i++)
                    ShipEntity.Create($"Ship_{i}", maxHull: 100f, damage: 25f, speed: 10f);
         })
            .WarmupCount(3)
            .MeasurementCount(10)
            .Run();
        }

        [Test, Performance]
        public void Create_LargeFleet_Under10Ms()
        {
            Measure.Method(() =>
            {
                for (int i = 0; i < LargeFleet; i++)
                    ShipEntity.Create($"Ship_{i}", maxHull: 100f, damage: 25f, speed: 10f);
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .Run();
        }

        // ShipEntity.TakeDamage()

        [Test, Performance]
        public void TakeDamage_MediumFleet_Under1Ms()
        {
            var fleet = CreateFleet(MediumFleet);

            Measure.Method(() =>
            {
                foreach (var ship in fleet)
                    ship.TakeDamage(10f);
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .Run();
        }

        [Test, Performance]
        public void TakeDamage_LargeFleet_Under5Ms()
        {
            var fleet = CreateFleet(LargeFleet);

            Measure.Method(() =>
            {
                foreach (var ship in fleet)
                    ship.TakeDamage(10f);
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .Run();
        }

        // ResourceWallet operations

        [Test, Performance]
        public void ResourceWallet_Add_1000Times_Under1Ms()
        {
            var wallet = ResourceWallet.StartingResources();

            Measure.Method(() =>
            {
                for (int i = 0; i < 1000; i++)
                    wallet.Add(ResourceType.Metal, 1f);
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .Run();
        }

        [Test, Performance]
        public void ResourceWallet_SpendAndAdd_1000Times_Under2Ms()
        {
            var wallet = ResourceWallet.Empty().Add(ResourceType.Metal, 999999f);

            Measure.Method(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    wallet = wallet.Spend(ResourceType.Metal, 1f);
                    wallet = wallet.Add(ResourceType.Metal, 1f);
                }
            })
            .WarmupCount(3)
            .MeasurementCount(10)
            .Run();
        }

        // Helpers

        private static List<ShipEntity> CreateFleet(int count)
        {
            var fleet = new List<ShipEntity>(count);
            for (int i = 0; i < count; i++)
                fleet.Add(ShipEntity.Create($"Ship_{i}", maxHull: 100f, damage: 25f, speed: 10f));
            return fleet;
        }
    }
}
