
// A single star system on the galaxy map.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GalacticEmpire.Feature.Galaxy.Domain
{
    /// <summary>Represents a single star system on the galaxy map.</summary>
    public sealed record SectorEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public SectorType Type { get; init; }

        // Position in 2D galaxy space - used for rendering and distance calculations
        public Vector2 Position { get; init; }

        // Graph connections - neighbouring sectors reachable from this one
        public IReadOnlyList<Guid> NeighbourIds { get; init; }

        // Fog of war - starts false, set to true when fleet enters
        public bool IsDiscovered { get; init; }

        // True if the player controls this sector
        public bool IsOwned { get; init; }

        // Resource richness 0.0 to 1.0 - drives how much the sector produces
        public float ResourceRichness { get; init; }

        // Threat level 0.0 to 1.0 - drives enemy fleet strength
        public float ThreatLevel { get; init; }

        public bool IsExplored => IsDiscovered;

        /// <summary>Creates a new undiscovered sector at the given position.</summary>
        public static SectorEntity Create(
            string name,
            SectorType type,
            Vector2 position,
            float resourceRichness = 0.5f,
            float threatLevel = 0.5f)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Sector needs a name.", nameof(name));

            if (resourceRichness < 0f || resourceRichness > 1f)
                throw new ArgumentOutOfRangeException(nameof(resourceRichness), "Must be between 0 and 1.");

            if (threatLevel < 0f || threatLevel > 1f)
                throw new ArgumentOutOfRangeException(nameof(threatLevel), "Must be between 0 and 1.");

            return new SectorEntity
            {
                Id     = Guid.NewGuid(),
                Name   = name,
                Type   = type,

                Position      = position,
                NeighbourIds  = new List<Guid>().AsReadOnly(),
                IsDiscovered  = false,

                IsOwned  = false,
                ResourceRichness = resourceRichness,
                ThreatLevel  = threatLevel
            };
        }

        /// <summary>Returns a new sector with updated neighbour connections.</summary>
        public SectorEntity WithNeighbours(IReadOnlyList<Guid> neighbourIds)
        {
            return this with { NeighbourIds = neighbourIds };
        }

        /// <summary>Player's fleet entered this sector - reveal it.</summary>
        public SectorEntity Discover()
        {
            return this with { IsDiscovered = true };
        }

        /// <summary>Player conquered this sector.</summary>
        public SectorEntity Claim()
        {
            if (!IsDiscovered)
                throw new InvalidOperationException($"Can't claim undiscovered sector {Name}.");

            return this with { IsOwned = true };
        }

        /// <summary>Sector was lost to enemy forces.</summary>
        public SectorEntity Lose()
        {
            return this with { IsOwned = false };
        }
    }
}
