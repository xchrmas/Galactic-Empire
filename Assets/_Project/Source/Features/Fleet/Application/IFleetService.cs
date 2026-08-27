// Commands and service contract for fleet operations.
// Keeping commands here avoids cross-namespace visibility issues.

using System;
using System.Collections.Generic;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Fleet.Application
{
    // Commands - immutable data carriers, no logic inside
    public sealed record CreateFleetCommand(string Name, ShipBlueprintSO Blueprint, int ShipCount);
    public sealed record DispatchFleetCommand(Guid FleetId, Guid TargetSectorId);
    public sealed record RecallFleetCommand(Guid FleetId);
    public sealed record BuildShipCommand(ShipBlueprintSO Blueprint, Guid TargetFleetId);

    /// <summary>All fleet use cases go through here.</summary>
    public interface IFleetService
    {
        // Returns all active fleets
        IReadOnlyList<FleetEntity> GetAll();

        // Returns  single fleet by ID, null if not found
        FleetEntity GetById(Guid fleetId);

        // Creates new fleet and returns it
        FleetEntity CreateFleet(CreateFleetCommand cmd);

        // Dispatches fleet - changes status to Moving
        FleetEntity Dispatch(DispatchFleetCommand cmd);

        // Recalls fleet back to station
        FleetEntity Recall(RecallFleetCommand cmd);

        // Queues ship for construction
        void BuildShip(BuildShipCommand cmd);
    }
}
