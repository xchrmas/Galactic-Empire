// ScriptableObject implementation of IFleetRepository.
//
// 2026-10-21: using GalacticEmpire.Core -> using GalacticEmpire.Feature.
// Fleet.Domain, since ShipEntity migrated there (Stage 1c). No other
// behavior change. NOTE: Unity serializes the ShipEntity type by full name
// in the FleetRepository.asset YAML - after this namespace change, any
// existing FleetRepository.asset's serialized _ships list will fail to
// deserialize the type and silently reset to empty on next load. This
// matches what happened implicitly during Station's Phase 7 Etap 3
// migration (ScriptableObject data resets, not preserved across a
// namespace move) - not a regression specific to this change, just the
// same known cost. Clear/recreate the FleetRepository asset's data (or
// just let GameEntryPoint's startup Clear() handle it) after applying.


using System;
using System.Collections.Generic;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.Fleet.Application;
using UnityEngine;

namespace GalacticEmpire.Feature.Fleet.Infrastructure
{
    /// <summary>Stores and manages the fleet as a ScriptableObject asset.</summary>
    [CreateAssetMenu(fileName = "FleetRepository", menuName = "GalacticEmpire/Fleet Repository")]


    public sealed class FleetRepositorySO : ScriptableObject, IFleetRepository
    {
        // Unity serializes this list — data persists between play sessions in the Editor
        [SerializeField]
        private List<ShipEntity> _ships = new();

        /// <summary>Returns all ships as a read-only list.</summary>
        public IReadOnlyList<ShipEntity> GetAll()
        {
            return _ships.AsReadOnly();
        }

        /// <summary>Finds a ship by ID, returns null if not found.</summary>
        public ShipEntity GetById(Guid id)
        {
            return _ships.Find(ship => ship.Id == id);
        }

        /// <summary>Adds a new ship to the fleet.</summary>
        public void Add(ShipEntity ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            _ships.Add(ship);
        }

        /// <summary>Replaces an existing ship with updated data.</summary>
        public void Replace(ShipEntity ship)
        {
            int index = _ships.FindIndex(s => s.Id == ship.Id);

            if (index < 0)
            {
                throw new InvalidOperationException($"Ship {ship.Id} not found in fleet.");
            }

            // Records are immutable — replace old entry with the updated ship
            _ships[index] = ship;
        }

        /// <summary>Removes a ship from the fleet by ID.</summary>
        public void Remove(Guid id)
        {
            int removed = _ships.RemoveAll(s => s.Id == id);

            if (removed == 0)
            {
                throw new InvalidOperationException($"Ship {id} not found in fleet.");
            }
        }
        /// <summary>Removes all ships - resets state at game start.</summary>
        public void Clear()
        {
            _ships.Clear();
        }
    }
}
