// Immutable snapshot of empire resources at a point in time.

using System;
using System.Collections.Generic;

namespace GalacticEmpire.Core
{
    /// <summary>Immutable record of all empire resources.</summary>
    public sealed record ResourceWallet
    {
        // All resource amounts — read-only from outside
        public IReadOnlyDictionary<ResourceType, float> Resources { get; init; }

        // Factory — creates a wallet with zero resources
        public static ResourceWallet Empty()
        {
            var resources = new Dictionary<ResourceType, float>
            {
                { ResourceType.Metal,0f },
                { ResourceType.Energy, 0f },
                { ResourceType.Crystals,0f },
                { ResourceType.DarkMatter,0f }
            };

            return new ResourceWallet { Resources = resources };
        }

        // Factory — creates a wallet with starting resources for a new empire
        public static ResourceWallet StartingResources()
        {
            var resources = new Dictionary<ResourceType, float>
            {
                { ResourceType.Metal,500f },
                { ResourceType.Energy,300f },
                { ResourceType.Crystals,50f  },
                { ResourceType.DarkMatter,0f   }
            };

            return new ResourceWallet { Resources = resources };
        }

        // Returns the amount of a specific resource
        public float Get(ResourceType type)
        {
            return Resources.TryGetValue(type, out float amount) ? amount : 0f;
        }

        // Returns a NEW wallet with added resources — never mutates this one
        public ResourceWallet Add(ResourceType type, float amount)
        {
            if (amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            var updated = new Dictionary<ResourceType, float>(Resources)
            {
                [type] = Get(type) + amount
            };

            return new ResourceWallet { Resources = updated };
        }

        // Returns a NEW wallet after spending resources
        // Throws if not enough resources available
        public ResourceWallet Spend(ResourceType type, float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
            }

            float current = Get(type);

            if (current < amount)
            {
                throw new InvalidOperationException(
                    $"Not enough {type}. Have {current}, need {amount}.");
            }

            var updated = new Dictionary<ResourceType, float>(Resources)
            {
                [type] = current - amount
            };

            return new ResourceWallet { Resources = updated };
        }

        // Returns true if the empire can afford the given cost
        public bool CanAfford(ResourceType type, float amount)
        {
            return Get(type) >= amount;
        }
    }
}
