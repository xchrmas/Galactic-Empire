// A fleet is a named group of ships that moves and fights together.
// All mutations return a new record - nothing here is mutable.

using System;
using System.Collections.Generic;
using System.Linq;
using GalacticEmpire.Core;

namespace GalacticEmpire.Feature.Fleet.Domain
{
    /// <summary>Aggregate root representing a named group of ships.</summary>
    public sealed record FleetEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public FleetStatus Status { get; init; }
        public IReadOnlyList<ShipEntity> Ships { get; init; }

        // Computed from ships - always up to date
        public int ShipCount => Ships.Count;
        public bool IsDestroyed => Status == FleetStatus.Destroyed || Ships.Count == 0;
        public float TotalHull => Ships.Sum(s => s.Hull);
        public float TotalDamage => Ships.Sum(s => s.Damage);
        public float CombatPower => TotalHull * TotalDamage;

        /// <summary>Creates a new fleet ready for duty.</summary>
        public static FleetEntity Create(string name, IReadOnlyList<ShipEntity> ships)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Fleet needs a name.", nameof(name));

            if (ships == null || ships.Count == 0)
                throw new ArgumentException("Can't create an empty fleet.", nameof(ships));

            return new FleetEntity
            {
                Id     = Guid.NewGuid(),
                Name   = name,
                Status = FleetStatus.Idle,
                Ships  = ships
            };
        }

        /// <summary>Send the fleet to a target - changes status to Moving.</summary>
        public FleetEntity Dispatch()
        {
            if (IsDestroyed)
                throw new InvalidOperationException($"Fleet {Name} is destroyed and cannot be dispatched.");

            if (Status == FleetStatus.InBattle)
                throw new InvalidOperationException($"Fleet {Name} is already in battle.");

            return this with { Status = FleetStatus.Moving };
        }

        /// <summary>Recall the fleet back to station.</summary>
        public FleetEntity Recall()
        {
            if (IsDestroyed)
                throw new InvalidOperationException($"Fleet {Name} is destroyed.");

            return this with { Status = FleetStatus.Idle };
        }

        /// <summary>Engage the enemy - fleet enters battle.</summary>
        public FleetEntity EnterBattle()
        {
            if (IsDestroyed)
                throw new InvalidOperationException($"Fleet {Name} is destroyed.");

            return this with { Status = FleetStatus.InBattle };
        }

        /// <summary>Fall back from combat.</summary>
        public FleetEntity Retreat()
        {
            return this with { Status = FleetStatus.Retreating };
        }

        /// <summary>Apply damage results from a battle tick - updates individual ships.</summary>
        public FleetEntity ApplyBattleDamage(Guid shipId, float damage)
        {
            var updatedShips = Ships
                .Select(s => s.Id == shipId ? s.TakeDamage(damage) : s)
                .Where(s => s.IsAlive)
                .ToList()
                .AsReadOnly();

            var newStatus = updatedShips.Count == 0
                ? FleetStatus.Destroyed
                : Status;

            return this with { Ships = updatedShips, Status = newStatus };
        }

        /// <summary>Merge another fleet into this one.</summary>
        public FleetEntity MergeWith(FleetEntity other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (other.IsDestroyed)
                throw new InvalidOperationException("Can't merge a destroyed fleet.");

            var combined = Ships.Concat(other.Ships).ToList().AsReadOnly();
            return this with { Ships = combined, Status = FleetStatus.Idle };
        }
    }
}
