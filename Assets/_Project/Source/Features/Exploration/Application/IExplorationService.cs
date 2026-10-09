// Commands and service contract for exploration missions.
// Keeping the command here avoids cross-namespace visibility issues,
// matching the convention IFleetService.cs/IStationService.cs already use.

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Feature.Exploration.Domain;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Exploration.Application
{
    // Command - immutable data carrier, no logic inside. Carries the fleet's
    // post-Dispatch state (GalaxyMapScreen.OnFleetDispatchedToSector already
    // calls IFleetService.Dispatch before raising its event - this service
    // must not call Dispatch a second time) rather than just a FleetId.
    public sealed record StartMissionCommand(FleetEntity DispatchedFleet, Guid TargetSectorId);

    /// <summary>All exploration use cases go through here.</summary>
    public interface IExplorationService
    {
        // Returns all missions currently in flight (not yet Completed)
        IReadOnlyList<MissionEntity> GetActiveMissions();

        // Starts tracking a fleet's round trip after it has already been
        // dispatched (Status is already Moving) - travel time is computed
        // from hop distance and fleet speed
        MissionEntity StartMission(StartMissionCommand command);

        // Runs until the CancellationToken fires - VContainer handles cleanup
        // automatically, same shape as IResourceService.StartProductionLoopAsync
        UniTask StartMissionTickLoopAsync(CancellationToken ct);

        // Single tick - handy for tests and manual triggers, same shape as
        // IResourceService.Tick(). Advances every active mission by one check:
        // resolves arrivals (discovery + encounter check) and completions
        // (fleet back home, status to Idle).
        void Tick();

        // Called once a fleet's encounter at its target sector is resolved
        // (win or loss/draw) - starts the return trip on a win, leaves the
        // mission at Arrived otherwise (fleet stays at the sector; repair/
        // recall is Stage 1b, out of scope here).
        void NotifyEncounterResolved(Guid fleetId, bool playerWon);

        // Lets the composition root (GameEntryPoint) supply a check for
        // "does this sector have an unresolved encounter" without this
        // service referencing Battle.Application directly - same pattern as
        // GalaxyMapScreen.SetBattleInProgressCheck(Func&lt;bool&gt;). Called once
        // during startup wiring, before the tick loop can reach any mission.
        void SetEncounterCheck(Func<MissionEntity, bool> encounterCheck);

        // Removes all missions - used to reset state at game start, same
        // shape as ClearFleet()/ClearGalaxy()/ClearStation()
        void ClearMissions();
    }
}
