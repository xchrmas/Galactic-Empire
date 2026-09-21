// An ephemeral NPC fleet guarding a hostile sector.
// Unlike FleetEntity, this is never persisted - it exists only in the
// in-memory sector garrison (ISectorGarrisonRepository) until the player
// clears the sector or the process restarts. No Status/TargetSectorId,
// since NPC fleets don't move or get dispatched.

using System;
using System.Collections.Generic;
using System.Linq;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Battle.Domain
{
    /// <summary>Immutable NPC fleet, converted to a FleetEntity only for combat resolution.</summary>
    public sealed record EnemyFleetEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public Guid SectorId { get; init; }
        public IReadOnlyList<ShipEntity> Ships { get; init; }

        public int ShipCount => Ships.Count;
        public bool IsDestroyed => Ships.Count == 0 || Ships.All(s => !s.IsAlive);

        /// <summary>Creates a new NPC fleet guarding the given sector.</summary>
        public static EnemyFleetEntity Create(string name, Guid sectorId, IReadOnlyList<ShipEntity> ships)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Enemy fleet needs a name.", nameof(name));

            if (ships == null || ships.Count == 0)
                throw new ArgumentException("Can't create an empty enemy fleet.", nameof(ships));

            return new EnemyFleetEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                SectorId = sectorId,
                Ships = ships
            };
        }

        /// <summary>
        /// Converts this NPC fleet into a FleetEntity so it can enter a BattleEntity -
        /// BattleEntity/CombatTickService only know about FleetEntity, and there's no
        /// reason to duplicate that logic for NPC fleets. The result is never persisted
        /// back into this EnemyFleetEntity - NPC fleets are throwaway combat participants.
        /// </summary>
        public FleetEntity ToFleetEntity()
        {
            return FleetEntity.Create(Name, Ships);
        }
    }
}
