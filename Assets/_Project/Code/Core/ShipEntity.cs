
using System;

namespace GalacticEmpire.Core
{
    /// <summary>Immutable domain entity representing a ship.</summary>
    public sealed record ShipEntity
    {
        // Unique identity of this ship instance
        public Guid Id { get; init; }
        public string Name { get; init; }

        // Combat stats
        public float Hull    { get; init; }
        public float MaxHull { get; init; }
        public float Damage  { get; init; }

        // Movement
        public float Speed { get; init; }

        // Computed from Hull — not stored, always up to date
        public bool IsAlive => Hull > 0f;
        public float HullPercent => MaxHull > 0f ? Hull / MaxHull : 0f;

        // Factory method — the only valid way to create a ship.
        // Validates all inputs so an invalid ship can never exist in the domain.
        public static ShipEntity Create(string name, float maxHull, float damage, float speed)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Ship name cannot be empty.", nameof(name));

            if (maxHull <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxHull), "Hull must be greater than zero.");

            if (speed < 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed cannot be negative.");

            return new ShipEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Hull = maxHull, // ship starts at full hull
                MaxHull = maxHull,
                Damage = damage,
                Speed  = speed
            };
        }

        // Records are immutable — domain behaviour always returns a NEW ship.
        // The original ship is never modified (functional approach).

        /// <summary>Returns a new ship with reduced hull after taking damage.</summary>
        public ShipEntity TakeDamage(float amount)
        {
             if (amount < 0f)
             {
                 throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");
             }

             return this with { Hull = MathF.Max(0f, Hull - amount) };
        }


        /// <summary>Returns a new ship restored to full hull.</summary>
        public ShipEntity Repair()
        {
            return this with { Hull = MaxHull };
        }
    }
}
