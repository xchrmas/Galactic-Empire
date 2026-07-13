// Unit tests for ResourceWallet domain logic.

using System;
using NUnit.Framework;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class ResourceWalletTests
    {
        // Empty

        [Test]
        public void Empty_AllResourcesAreZero()
        {
            var wallet = ResourceWallet.Empty();

            Assert.That(wallet.Get(ResourceType.Metal),Is.EqualTo(0f));
            Assert.That(wallet.Get(ResourceType.Energy), Is.EqualTo(0f));
            Assert.That(wallet.Get(ResourceType.Crystals),Is.EqualTo(0f));
            Assert.That(wallet.Get(ResourceType.DarkMatter),Is.EqualTo(0f));
        }

        // StartingResources

        [Test]
        public void StartingResources_MetalIsPositive()
        {
            var wallet = ResourceWallet.StartingResources();

            Assert.That(wallet.Get(ResourceType.Metal), Is.GreaterThan(0f));
        }

        // Add()

        [Test]
        public void Add_IncreasesResourceAmount()
        {
            var wallet = ResourceWallet.Empty();

            var result = wallet.Add(ResourceType.Metal, 100f);

            Assert.That(result.Get(ResourceType.Metal), Is.EqualTo(100f));
        }

        [Test]
        public void Add_DoesNotModifyOriginalWallet()
        {

            var wallet = ResourceWallet.Empty();

            wallet.Add(ResourceType.Metal, 100f);

            Assert.That(wallet.Get(ResourceType.Metal), Is.EqualTo(0f));
        }

        [Test]
        public void Add_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            var wallet = ResourceWallet.Empty();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                wallet.Add(ResourceType.Metal, -50f));
        }

        // Spend()

        [Test]
        public void Spend_DecreasesResourceAmount()
        {
            ResourceWallet wallet = ResourceWallet.Empty().Add(ResourceType.Metal, 200f);

            ResourceWallet result = wallet.Spend(ResourceType.Metal, 80f);

            Assert.That(result.Get(ResourceType.Metal), Is.EqualTo(120f));
        }

        [Test]
        public void Spend_DoesNotModifyOriginalWallet()
        {
            ResourceWallet wallet = ResourceWallet.Empty().Add(ResourceType.Metal, 200f);

            wallet.Spend(ResourceType.Metal, 80f);

            Assert.That(wallet.Get(ResourceType.Metal), Is.EqualTo(200f));
        }

        [Test]
        public void Spend_NotEnoughResources_ThrowsInvalidOperationException()
        {
            // Empire cannot go into negative resources — domain rule enforced here
            ResourceWallet wallet = ResourceWallet.Empty();

            Assert.Throws<InvalidOperationException>(() =>
                wallet.Spend(ResourceType.Metal, 100f));
        }

        // CanAfford()

        [Test]
        public void CanAfford_WithEnoughResources_ReturnsTrue()
        {
            ResourceWallet wallet = ResourceWallet.Empty().Add(ResourceType.Energy, 300f);

            Assert.That(wallet.CanAfford(ResourceType.Energy, 200f), Is.True);
        }

        [Test]
        public void CanAfford_WithNotEnoughResources_ReturnsFalse()
        {
            ResourceWallet wallet = ResourceWallet.Empty();

            Assert.That(wallet.CanAfford(ResourceType.Energy, 100f), Is.False);
        }
    }
}
