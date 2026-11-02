// Handles all fleet use cases - checks resources, builds entities, persists state.
// This is the only place where fleet business rules and resource checks combine.
//
// 2026-10-21 (Stage 1c): CreateFleet/BuildShip no longer call
// cmd.Blueprint.CreateEntity() - commands now carry already-built ShipEntity
// instances and pre-computed costs, since ShipBlueprintSO moved to
// Infrastructure and this assembly (Application) must not reference it. See
// IFleetService.cs's header comment and FleetManagementScreen.cs for the
// caller-side change. Also adds InstallModule/RemoveModule (module fit-out
// use cases) and FindShip (locates a ship across all tracked fleets).

using System;
using System.Collections.Generic;
using System.Linq;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Station.Application;

namespace GalacticEmpire.Feature.Fleet.Application
{
    /// <summary>Executes fleet use cases - creation, dispatch, recall, ship construction.</summary>
    public sealed class FleetService : IFleetService
    {
        private readonly IFleetRepository    _fleetRepository;
        private readonly IResourceRepository _resourceRepository;
        private readonly IGalaxyService      _galaxyService;
        private readonly GameConfigSO        _config;

        private readonly List<FleetEntity> _fleets = new();

        public FleetService(
            IFleetRepository fleetRepository,
            IResourceRepository resourceRepository,
            IGalaxyService galaxyService,
            GameConfigSO config)
        {
            _fleetRepository    = fleetRepository;
            _resourceRepository = resourceRepository;
            _galaxyService      = galaxyService;
            _config             = config;
        }

        /// <summary>Returns all active fleets.</summary>
        public IReadOnlyList<FleetEntity> GetAll() => _fleets.AsReadOnly();

        /// <summary>Finds a fleet by ID.</summary>
        /// <summary>Registers a fleet from ships that already exist - no resource cost. Used for the starting fleet at boot.</summary>
        public FleetEntity RegisterStartingFleet(string name, IReadOnlyList<ShipEntity> ships)
        {
            var fleet = FleetEntity.Create(name, ships);
            _fleets.Add(fleet);

            GELogger.Info(LogCategory.Fleet, $"Fleet '{fleet.Name}' registered with {fleet.ShipCount} ship(s).");

            return fleet;
        }

        public FleetEntity GetById(Guid fleetId)
        {
            return _fleets.FirstOrDefault(f => f.Id == fleetId);
        }

        /// <summary>Finds a ship by id across every tracked fleet - null if none has it (Stage 1c).</summary>
        public ShipEntity FindShip(Guid shipId)
        {
            return _fleets
                .SelectMany(f => f.Ships)
                .FirstOrDefault(s => s.Id == shipId);
        }

        /// <summary>Creates a new fleet from already-built ships - checks resources and fleet size limit first.</summary>
        public FleetEntity CreateFleet(CreateFleetCommand cmd)
        {
            if (_fleets.Count >= _config.MaxFleetSize)
                throw new InvalidOperationException($"Fleet limit reached ({_config.MaxFleetSize}).");

            var wallet = _resourceRepository.Get();

            if (!wallet.CanAfford(ResourceType.Metal, cmd.TotalMetalCost))
                throw new InvalidOperationException($"Not enough Metal. Need {cmd.TotalMetalCost}.");

            if (!wallet.CanAfford(ResourceType.Energy, cmd.TotalEnergyCost))
                throw new InvalidOperationException($"Not enough Energy. Need {cmd.TotalEnergyCost}.");

            wallet = wallet.Spend(ResourceType.Metal, cmd.TotalMetalCost);
            wallet = wallet.Spend(ResourceType.Energy, cmd.TotalEnergyCost);
            _resourceRepository.Save(wallet);

            var fleet = FleetEntity.Create(cmd.Name, cmd.Ships);
            _fleets.Add(fleet);

            foreach (var ship in fleet.Ships)
                _fleetRepository.Add(ship);

            GELogger.Info(LogCategory.Fleet,
                $"Fleet '{fleet.Name}' created with {fleet.ShipCount} ship(s).");

            return fleet;
        }

        /// <summary>Dispatches a fleet toward a target sector - validates the sector is reachable first.</summary>
        public FleetEntity Dispatch(DispatchFleetCommand cmd)
        {
            var fleet = GetFleetOrThrow(cmd.FleetId);

            var galaxy = _galaxyService.GetGalaxy();
            var targetSector = galaxy?.GetSector(cmd.TargetSectorId);

            if (targetSector == null)
                throw new InvalidOperationException($"Sector {cmd.TargetSectorId} not found.");

            // Reachability, not discovery - fog of war is a Presentation/
            // Exploration concern now (a mission discovers its target sector
            // on arrival, see features/exploration.md). Requiring
            // IsDiscovered here would make genuine exploration impossible,
            // since nothing discovers a sector before Dispatch anymore.
            var homeSector = galaxy.GetOwnedSectors().FirstOrDefault();
            if (homeSector == null)
                throw new InvalidOperationException("No owned (home) sector - cannot validate dispatch target.");

            if (!galaxy.IsReachable(homeSector.Id, targetSector.Id))
                throw new InvalidOperationException($"Sector {targetSector.Name} is not reachable.");

            var updated = fleet.Dispatch(cmd.TargetSectorId);

            UpdateFleet(fleet, updated);

            GELogger.Info(LogCategory.Fleet, $"Fleet '{fleet.Name}' dispatched to {targetSector.Name}.");
            return updated;
        }

        /// <summary>Recalls a fleet back to station.</summary>
        public FleetEntity Recall(RecallFleetCommand cmd)
        {
            var fleet = GetFleetOrThrow(cmd.FleetId);
            var updated = fleet.Recall();

            UpdateFleet(fleet, updated);

            GELogger.Info(LogCategory.Fleet, $"Fleet '{fleet.Name}' recalled.");
            return updated;
        }

        /// <summary>Recalls a fleet AND heals it - the "arrives home" case (Stage 1b).</summary>
        public FleetEntity RecallAndRepair(RecallFleetCommand cmd)
        {
            var fleet = GetFleetOrThrow(cmd.FleetId);
            var updated = fleet.Recall().Repair();

            UpdateFleet(fleet, updated);

            GELogger.Info(LogCategory.Fleet, $"Fleet '{fleet.Name}' recalled and repaired.");
            return updated;
        }

        /// <summary>Queues a single already-built ship for construction.</summary>
        public void BuildShip(BuildShipCommand cmd)
        {
            var wallet = _resourceRepository.Get();

            if (!wallet.CanAfford(ResourceType.Metal, cmd.MetalCost))
                throw new InvalidOperationException($"Not enough Metal to build {cmd.Ship.Name}.");

            wallet = wallet.Spend(ResourceType.Metal, cmd.MetalCost);
            wallet = wallet.Spend(ResourceType.Energy, cmd.EnergyCost);
            _resourceRepository.Save(wallet);

            _fleetRepository.Add(cmd.Ship);

            GELogger.Info(LogCategory.Fleet, $"Ship '{cmd.Ship.Name}' queued for construction.");
        }

        /// <summary>
        /// Installs a module into one of a ship's slots (Stage 1c) - checks
        /// resource affordability, then finds which fleet the ship belongs
        /// to and replaces it there via FleetEntity.ReplaceShip (which itself
        /// enforces the fleet being Idle).
        /// </summary>
        public ShipEntity InstallModule(InstallModuleCommand cmd)
        {
            var wallet = _resourceRepository.Get();

            if (!wallet.CanAfford(ResourceType.Metal, cmd.MetalCost))
                throw new InvalidOperationException($"Not enough Metal to install {cmd.Module.Name}.");

            if (!wallet.CanAfford(ResourceType.Energy, cmd.EnergyCost))
                throw new InvalidOperationException($"Not enough Energy to install {cmd.Module.Name}.");

            var fleet = FindFleetContainingShip(cmd.ShipId);
            var ship = fleet.Ships.First(s => s.Id == cmd.ShipId);

            var updatedShip = ship.InstallModule(cmd.SlotIndex, cmd.Module);
            var updatedFleet = fleet.ReplaceShip(updatedShip);

            wallet = wallet.Spend(ResourceType.Metal, cmd.MetalCost);
            wallet = wallet.Spend(ResourceType.Energy, cmd.EnergyCost);
            _resourceRepository.Save(wallet);

            UpdateFleet(fleet, updatedFleet);
            _fleetRepository.Replace(updatedShip);

            GELogger.Info(LogCategory.Fleet,
                $"Installed '{cmd.Module.Name}' into slot {cmd.SlotIndex} of '{ship.Name}'.");

            return updatedShip;
        }

        /// <summary>Removes a module from one of a ship's slots (Stage 1c) - no resource refund.</summary>
        public ShipEntity RemoveModule(RemoveModuleCommand cmd)
        {
            var fleet = FindFleetContainingShip(cmd.ShipId);
            var ship = fleet.Ships.First(s => s.Id == cmd.ShipId);

            var updatedShip = ship.RemoveModule(cmd.SlotIndex);
            var updatedFleet = fleet.ReplaceShip(updatedShip);

            UpdateFleet(fleet, updatedFleet);
            _fleetRepository.Replace(updatedShip);

            GELogger.Info(LogCategory.Fleet,
                $"Removed module from slot {cmd.SlotIndex} of '{ship.Name}'.");

            return updatedShip;
        }

        /// <summary>
        /// Replaces the tracked fleet with its post-battle state - called by
        /// BattlePresenter once a real-time battle finishes. Looks up by ID rather
        /// than by the old record instance (unlike Dispatch/Recall's UpdateFleet),
        /// since the caller only has the final BattleEntity's fleet, not the
        /// original reference from _fleets.
        /// </summary>
        public void SyncFleetAfterBattle(FleetEntity updatedFleet)
        {
            if (updatedFleet == null)
                throw new ArgumentNullException(nameof(updatedFleet));

            int index = _fleets.FindIndex(f => f.Id == updatedFleet.Id);
            if (index < 0)
            {
                GELogger.Warning(LogCategory.Fleet,
                    $"SyncFleetAfterBattle: fleet {updatedFleet.Id} not found, nothing to update.");
                return;
            }

            // Surviving fleet returns to Idle AND heals (Stage 1b - "fleets
            // heal at the station") - destroyed fleets keep their Destroyed
            // status as set by FleetEntity.ApplyBattleDamage during combat,
            // nothing left to repair.
            var synced = updatedFleet.IsDestroyed ? updatedFleet : updatedFleet.Recall().Repair();
            _fleets[index] = synced;

            GELogger.Info(LogCategory.Fleet,
                $"Fleet '{synced.Name}' synced after battle - {synced.ShipCount} ship(s) remaining.");
        }

        /// <summary>Creates the starting ship and registers the starting fleet - no-op if a fleet already exists.</summary>
        public FleetEntity EnsureStartingFleet(string fleetName, string shipName, float maxHull, float damage, float speed)
        {
            if (_fleets.Count > 0)
                return _fleets[0];

            var ship = ShipEntity.Create(shipName, maxHull, damage, speed);
            _fleetRepository.Add(ship);

            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            var fleet = RegisterStartingFleet(fleetName, ships);

            GELogger.Info(LogCategory.Fleet, $"Fleet ready. Ships: {_fleetRepository.GetAll().Count}");

            return fleet;
        }

        /// <summary>Removes all fleets and ships - resets state at game start.</summary>
        public void ClearFleet()
        {
            _fleetRepository.Clear();
            _fleets.Clear();
        }

        /// <summary>Removes a single destroyed fleet entry - the player dismissing a dead fleet from the list.</summary>
        public void RemoveFleet(Guid fleetId)
        {
            var fleet = GetFleetOrThrow(fleetId);

            if (!fleet.IsDestroyed)
                throw new InvalidOperationException($"Fleet '{fleet.Name}' is not destroyed - recall it instead of removing it.");

            _fleets.Remove(fleet);

            GELogger.Info(LogCategory.Fleet, $"Fleet '{fleet.Name}' (destroyed) removed from the list.");
        }

        private FleetEntity GetFleetOrThrow(Guid fleetId)
        {
            var fleet = GetById(fleetId);
            if (fleet == null)
                throw new InvalidOperationException($"Fleet {fleetId} not found.");
            return fleet;
        }

        private FleetEntity FindFleetContainingShip(Guid shipId)
        {
            var fleet = _fleets.FirstOrDefault(f => f.Ships.Any(s => s.Id == shipId));
            if (fleet == null)
                throw new InvalidOperationException($"Ship {shipId} not found in any fleet.");
            return fleet;
        }

        private void UpdateFleet(FleetEntity old, FleetEntity updated)
        {
            int index = _fleets.IndexOf(old);
            if (index >= 0)
                _fleets[index] = updated;
        }
    }
}
