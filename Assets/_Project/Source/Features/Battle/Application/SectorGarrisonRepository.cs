// In-memory sector garrison state - Dictionary<sectorId, garrison>.
// A sector goes through three states: never visited (not in _generated),
// guarded (in _garrisons), or cleared (in _generated but not in _garrisons).
// The distinction matters so EncounterService never regenerates a fresh
// enemy fleet in a sector the player already cleared.

using System;
using System.Collections.Generic;
using GalacticEmpire.Feature.Battle.Domain;

namespace GalacticEmpire.Feature.Battle.Application
{
    /// <summary>In-memory implementation of ISectorGarrisonRepository.</summary>
    public sealed class SectorGarrisonRepository : ISectorGarrisonRepository
    {
        private readonly Dictionary<Guid, EnemyFleetEntity> _garrisons = new();
        private readonly HashSet<Guid> _generated = new();

        public EnemyFleetEntity Get(Guid sectorId)
        {
            return _garrisons.TryGetValue(sectorId, out var fleet) ? fleet : null;
        }

        public void Set(Guid sectorId, EnemyFleetEntity enemyFleet)
        {
            if (enemyFleet == null)
                throw new ArgumentNullException(nameof(enemyFleet));

            _garrisons[sectorId] = enemyFleet;
            _generated.Add(sectorId);
        }

        public void Clear(Guid sectorId)
        {
            _garrisons.Remove(sectorId);
            _generated.Add(sectorId);
        }

        public bool HasBeenGenerated(Guid sectorId) => _generated.Contains(sectorId);
    }
}
