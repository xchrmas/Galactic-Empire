//  Contract for fleet data access.

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

        // Replaces an existing ship with updated data (after damage, repair, etc.)
        void Replace(ShipEntity ship);

        // Removes a ship from the fleet permanently
        void Remove(Guid id);
    }
}
