// Core defines WHAT operations exist — not HOW they are implemented.

using System;
using System.Collections.Generic;

namespace GalacticEmpire.Core
{
    /// <summary>Defines fleet data access operations.</summary>
    public interface IFleetRepository
    {
        // Returns all ships currently in the fleet
        IReadOnlyList<ShipEntity> GetAll();

        // Returns a single ship by its unique ID, or null if not found
        ShipEntity GetById(Guid id);

        // Adds a new ship to the fleet
        void Add(ShipEntity ship);

        // Updates an existing ship (after damage, repair, etc.)
        void Update(ShipEntity ship);

        // Removes a ship from the fleet permanently
        void Remove(Guid id);
    }
}
