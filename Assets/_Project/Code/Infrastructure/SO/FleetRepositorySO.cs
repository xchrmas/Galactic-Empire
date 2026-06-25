// ScriptableObject implementation of IFleetRepository.


using System;
using System.Collections.Generic;
using GalacticEmpire.Core;
using UnityEngine;

namespace GalacticEmpire.Infrastructure
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
    }
}
