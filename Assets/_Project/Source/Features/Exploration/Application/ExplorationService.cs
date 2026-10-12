// Orchestrates fleet missions: travel time, arrival (discovery + encounter
// check), reward grant, auto-return, completion. Owns its own UniTask tick
// loop - same shape as ResourceProductionService.StartProductionLoopAsync,
// not a shared "game tick" source (none exists in the project; Battle's
// CombatTickService is driven the same way, by BattlePresenter's own loop).
//
// Travel time is computed from hop distance (BFS over GalaxyMapEntity's
// neighbour graph), not Vector2 distance - SectorEntity.Position is a Unity
// type that VISION.md decision 5 already flags as a Stage 2 multiplayer
// blocker, so this formula stays independent of it and survives that future
// refactor unchanged.
//
// Auto-return (VISION.md decision 2) depends on knowing whether the fleet
// won its encounter - BattlePresenter.OnEncounterResolved is the signal;
// GameEntryPoint wires it to NotifyEncounterResolved below.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Exploration.Domain;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Galaxy.Domain;
using GalacticEmpire.Feature.Station.Application;

// FleetEntity itself (not just IFleetService) is referenced because
// StartMissionCommand carries the already-dispatched fleet - see
// IExplorationService.cs for why this service must not call Dispatch itself.

namespace GalacticEmpire.Feature.Exploration.Application
{
    /// <summary>Drives fleet exploration missions from dispatch to return.</summary>
    public sealed class ExplorationService : IExplorationService
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IFleetService _fleetService;
        private readonly IGalaxyService _galaxyService;
        private readonly IResourceService _resourceService;
        private readonly IClock _clock;
        private readonly GameConfigSO _config;

        // Missions waiting at Arrived with an unresolved encounter - excluded
        // from Tick()'s HasReturnedHome/HasReachedTarget checks until
        // NotifyEncounterResolved fires. Arrived missions with no encounter
        // never enter this set - they start their return trip immediately.
        private readonly HashSet<Guid> _missionsAwaitingEncounter = new();

        public ExplorationService(
            IMissionRepository missionRepository,
            IFleetService fleetService,
            IGalaxyService galaxyService,
            IResourceService resourceService,
            IClock clock,
            GameConfigSO config)
        {
            _missionRepository = missionRepository;
            _fleetService = fleetService;
            _galaxyService = galaxyService;
            _resourceService = resourceService;
            _clock = clock;
            _config = config;
        }

        /// <summary>All missions currently in flight.</summary>
        public IReadOnlyList<MissionEntity> GetActiveMissions()
        {
            return _missionRepository.GetActive();
        }

        /// <summary>Starts tracking an already-dispatched fleet's round trip.</summary>
        public MissionEntity StartMission(StartMissionCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (command.DispatchedFleet == null)
                throw new ArgumentNullException(nameof(command.DispatchedFleet));

            var galaxy = _galaxyService.GetGalaxy();
            var originSector = galaxy.GetOwnedSectors().FirstOrDefault();

            if (originSector == null)
                throw new InvalidOperationException("No owned (home) sector - cannot compute mission origin.");

            int hops = GetHopDistance(galaxy, originSector.Id, command.TargetSectorId);
            if (hops < 0)
                throw new InvalidOperationException($"Sector {command.TargetSectorId} is not reachable from the home sector.");

            var travelDuration = ComputeTravelDuration(hops, command.DispatchedFleet.Speed);

            var mission = MissionEntity.Create(
                command.DispatchedFleet.Id,
                originSector.Id,
                command.TargetSectorId,
                travelDuration,
                _clock);

            _missionRepository.Save(mission);

            GELogger.Info(LogCategory.System,
                $"Mission started: fleet {command.DispatchedFleet.Name} -> sector {command.TargetSectorId}, " +
                $"{hops} hop(s), ETA {travelDuration.TotalSeconds:F0}s.");

            return mission;
        }

        /// <summary>Runs indefinitely until cancelled - VContainer issues a token when unloading the scene.</summary>
        public async UniTask StartMissionTickLoopAsync(CancellationToken ct)
        {
            GELogger.Info(LogCategory.System, "Exploration tick loop is running.");

            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_config.ExplorationTickRate),
                    cancellationToken: ct);

                Tick();
            }

            GELogger.Info(LogCategory.System, "Exploration tick loop stopped cleanly.");
        }

        /// <summary>Advances every active mission by one check.</summary>
        public void Tick()
        {
            // Snapshot first - resolving a mission can remove it from the
            // repository mid-loop (Complete), so iterate over a copy.
            var missions = _missionRepository.GetActive();

            foreach (var mission in missions)
            {
                if (mission.IsOutbound && mission.HasReachedTarget(_clock))
                {
                    ResolveArrival(mission);
                    continue;
                }

                if (mission.Status == MissionStatus.ReturnTravel && mission.HasReturnedHome(_clock))
                {
                    ResolveCompletion(mission);
                }
            }
        }

        /// <summary>Encounter outcome for a mission's fleet - starts the return trip on a win.</summary>
        public void NotifyEncounterResolved(Guid fleetId, bool playerWon)
        {
            var mission = _missionRepository.GetByFleetId(fleetId);
            if (mission == null || mission.Status != MissionStatus.Arrived)
                return;

            _missionsAwaitingEncounter.Remove(mission.Id);

            if (!playerWon)
            {
                // Fleet lost or drew - stays at the sector. No auto-return,
                // no reward. Repair/recall/respawn is Stage 1b, out of scope.
                GELogger.Info(LogCategory.System,
                    $"Mission {mission.Id}: encounter not won, fleet remains at sector {mission.TargetSectorId}.");
                return;
            }

            GrantRewardAndStartReturn(mission);
        }

        /// <summary>True once the mission has reached its target and discovery/encounter resolution begins.</summary>
        private void ResolveArrival(MissionEntity mission)
        {
            var arrived = mission.MarkArrived();
            _missionRepository.Save(arrived);

            var discoveredSector = _galaxyService.DiscoverSector(arrived.TargetSectorId);

            GELogger.Info(LogCategory.System,
                $"Mission {arrived.Id}: fleet reached {discoveredSector.Name}.");

            // Encounter resolution is intentionally NOT handled here - this
            // service has no dependency on Battle (MASTER.md Section 3
            // Dependency Flow / the GalaxyMapScreen.OnFleetDispatchedToSector
            // precedent: Battle depends on Fleet/Galaxy, never the reverse).
            // GameEntryPoint checks for an encounter after this call returns
            // and, if one exists, marks the mission as awaiting resolution;
            // otherwise the mission's return starts immediately below.
            if (_encounterCheck != null && _encounterCheck.Invoke(arrived))
            {
                _missionsAwaitingEncounter.Add(arrived.Id);
                return;
            }

            GrantRewardAndStartReturn(arrived);
        }

        private Func<MissionEntity, bool> _encounterCheck;

        // Called once by GameEntryPoint - lets this service ask "does this
        // sector have an unresolved encounter" without referencing
        // Battle.Application directly. Same pattern as
        // GalaxyMapScreen.SetBattleInProgressCheck(Func&lt;bool&gt;). The callback
        // should return true if an encounter was found and started
        // (BattlePresenter.StartEncounter already called), false if the
        // sector was clear. See MASTER.md Section 3 for why Exploration
        // cannot depend on Battle directly (same reasoning as Fleet/Galaxy).
        public void SetEncounterCheck(Func<MissionEntity, bool> encounterCheck)
        {
            _encounterCheck = encounterCheck;
        }

        private void GrantRewardAndStartReturn(MissionEntity mission)
        {
            var galaxy = _galaxyService.GetGalaxy();
            var sector = galaxy.GetSector(mission.TargetSectorId);

            if (sector != null && sector.ResourceRichness > 0f)
            {
                float reward = sector.ResourceRichness * _config.ExplorationRewardPerRichness;
                _resourceService.GrantResources(ResourceType.Metal, reward);
            }

            var fleet = _fleetService.GetById(mission.FleetId);
            float speed = fleet?.Speed ?? 0f;

            if (speed <= 0f)
            {
                // Fleet has no living ships (destroyed mid-encounter, edge
                // case) - nothing left to send home. Drop the mission.
                _missionRepository.Remove(mission.Id);
                return;
            }

            int hops = GetHopDistance(galaxy, mission.TargetSectorId, mission.OriginSectorId);
            var returnDuration = ComputeTravelDuration(hops, speed);

            var returning = mission.StartReturn(returnDuration, _clock);
            _missionRepository.Save(returning);

            GELogger.Info(LogCategory.System,
                $"Mission {returning.Id}: returning home, ETA {returnDuration.TotalSeconds:F0}s.");
        }

        private void ResolveCompletion(MissionEntity mission)
        {
            var completed = mission.Complete();
            _missionRepository.Remove(completed.Id);

            // Bring the fleet back to Idle - same Recall() call
            // FleetService.SyncFleetAfterBattle already uses for a surviving
            // fleet after combat, so this matches an existing precedent
            // rather than introducing new FleetStatus semantics. Skipped
            // only if the fleet no longer exists or was destroyed (e.g. lost
            // its last ship after NotifyEncounterResolved but somehow still
            // reached here) - Recall() throws on a destroyed fleet, and
            // there is nothing to recall if it's gone.
            var fleet = _fleetService.GetById(completed.FleetId);
            if (fleet != null && !fleet.IsDestroyed)
                _fleetService.Recall(new RecallFleetCommand(completed.FleetId));

            GELogger.Info(LogCategory.System, $"Mission {completed.Id}: fleet is home.");
        }

        /// <summary>BFS hop count between two sectors, -1 if unreachable.</summary>
        private static int GetHopDistance(GalaxyMapEntity galaxy, Guid fromSectorId, Guid toSectorId)
        {
            if (fromSectorId == toSectorId)
                return 0;

            var visited = new HashSet<Guid> { fromSectorId };
            var frontier = new Queue<(Guid sectorId, int depth)>();
            frontier.Enqueue((fromSectorId, 0));

            while (frontier.Count > 0)
            {
                var (currentId, depth) = frontier.Dequeue();

                foreach (var neighbour in galaxy.GetNeighbours(currentId))
                {
                    if (neighbour.Id == toSectorId)
                        return depth + 1;

                    if (visited.Add(neighbour.Id))
                        frontier.Enqueue((neighbour.Id, depth + 1));
                }
            }

            return -1;
        }

        // MissionEntity.Create/StartReturn both require a strictly positive
        // duration - clamp hops to at least 1 (a direct neighbour) so a very
        // high fleet Speed can never round a real trip down to zero seconds.
        private TimeSpan ComputeTravelDuration(int hops, float fleetSpeed)
        {
            float speed = fleetSpeed > 0f ? fleetSpeed : 1f;
            int clampedHops = Math.Max(hops, 1);
            double seconds = Math.Max(clampedHops * _config.BaseSecondsPerHop / speed, 0.01);
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>Removes all missions - resets state at game start.</summary>
        public void ClearMissions()
        {
            _missionsAwaitingEncounter.Clear();
            _missionRepository.Clear();
        }
    }
}
