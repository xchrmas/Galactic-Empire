// Commands and service contract for fleet operations.
// Keeping commands here avoids cross-namespace visibility issues.

using System;
using System.Collections.Generic;
using GalacticEmpire.Core;
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

        // Returns a single fleet by ID, null if not found
        FleetEntity GetById(Guid fleetId);

        // Creates a new fleet and returns it
        FleetEntity CreateFleet(CreateFleetCommand cmd);

        // Registers a fleet built from ships that already exist (e.g. the
        // starting fleet at game boot) - skips the resource cost in CreateFleet
        FleetEntity RegisterStartingFleet(string name, IReadOnlyList<ShipEntity> ships);

        // Dispatches a fleet - changes status to Moving
        FleetEntity Dispatch(DispatchFleetCommand cmd);

        // Recalls a fleet back to station
        FleetEntity Recall(RecallFleetCommand cmd);

        // Queues a ship for construction
        void BuildShip(BuildShipCommand cmd);

        // Replaces the tracked fleet with its post-battle state (surviving ships,
        // Status back to Idle) - BattlePresenter calls this after a real-time
        // battle finishes, since FleetService is the only place that knows about
        // the in-memory _fleets list a battle result needs to update.
        void SyncFleetAfterBattle(FleetEntity updatedFleet);

        // Creates the player's starting ship and registers it as the starting
        // fleet - called once at boot, mirrors IGalaxyService's get-or-create
        // shape. Safe to call more than once (no-op if a fleet already exists),
        // though in practice it only runs right after ClearFleet() at startup.
        FleetEntity EnsureStartingFleet(string fleetName, string shipName, float maxHull, float damage, float speed);

        // Removes all fleets and ships - used to reset state at game start so
        // Editor Play sessions don't accumulate leftover fleets from prior runs
        void ClearFleet();
    }
}
