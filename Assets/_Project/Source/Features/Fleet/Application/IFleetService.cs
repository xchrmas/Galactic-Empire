// Commands and service contract for fleet operations.
// Keeping commands here avoids cross-namespace visibility issues.
//
// 2026-10-21 (Stage 1c): CreateFleetCommand/BuildShipCommand no longer carry
// ShipBlueprintSO - Application must never reference Infrastructure (same
// rule Station's PlaceModuleCommand follows, taking a StationModuleEntity
// instead of a StationModuleSO). The Presentation caller (FleetManagement-
// Screen) now calls blueprint.CreateEntity() and reads blueprint.MetalCost/
// EnergyCost itself, passing the built ShipEntity + its cost in instead.

using System;
using System.Collections.Generic;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Fleet.Application
{
    // Commands - immutable data carriers, no logic inside
    public sealed record CreateFleetCommand(string Name, IReadOnlyList<ShipEntity> Ships, float TotalMetalCost, float TotalEnergyCost);
    public sealed record DispatchFleetCommand(Guid FleetId, Guid TargetSectorId);
    public sealed record RecallFleetCommand(Guid FleetId);
    public sealed record BuildShipCommand(ShipEntity Ship, Guid TargetFleetId, float MetalCost, float EnergyCost);
    public sealed record InstallModuleCommand(Guid ShipId, int SlotIndex, ShipModuleEntity Module, float MetalCost, float EnergyCost);
    public sealed record RemoveModuleCommand(Guid ShipId, int SlotIndex);

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

        // Recalls a fleet AND heals it - the "arrives home" case (Stage 1b:
        // "fleets heal at the station"). A plain Recall() alone leaves
        // battle damage on survivors; this is what ExplorationService's
        // mission-return path uses, and what a future "fly fleet home
        // manually" UI action should use too, rather than plain Recall().
        FleetEntity RecallAndRepair(RecallFleetCommand cmd);

        // Queues a ship for construction
        void BuildShip(BuildShipCommand cmd);

        // Finds the ship with the given id across every tracked fleet, or
        // null if none has it (Stage 1c - ship builder needs this to locate
        // a ship by id regardless of which fleet it's currently in).
        ShipEntity FindShip(Guid shipId);

        // Installs a module into one of a ship's slots (Stage 1c) - checks
        // resource affordability first, same pattern as CreateFleet/BuildShip.
        // Only valid for a ship in an Idle fleet (see FleetService for why).
        ShipEntity InstallModule(InstallModuleCommand cmd);

        // Removes a module from one of a ship's slots (Stage 1c) - no
        // resource refund, same "modules are a sunk cost once fitted"
        // stance VISION.md takes on ship construction cost in general.
        ShipEntity RemoveModule(RemoveModuleCommand cmd);

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

        // Removes a single destroyed fleet entry - lets the player dismiss a
        // dead fleet from the list (Stage 1b: "no empty fleet" dead end).
        // Only valid for a destroyed fleet; a living fleet must be Recalled,
        // not removed, so it can't vanish mid-mission.
        void RemoveFleet(Guid fleetId);
    }
}
