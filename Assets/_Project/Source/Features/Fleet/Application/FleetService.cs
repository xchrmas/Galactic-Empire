// Handles all fleet use cases - checks resources, builds entities, persists state.
// This is the only place where fleet business rules and resource checks combine.

using System;
using System.Collections.Generic;
using System.Linq;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.Station.Application;

namespace GalacticEmpire.Feature.Fleet.Application
{
    /// <summary>Executes fleet use cases - creation, dispatch, recall, ship construction.</summary>
    public sealed class FleetService : IFleetService
    {
        private readonly IFleetRepository    _fleetRepository;
        private readonly IResourceRepository _resourceRepository;
        private readonly GameConfigSO        _config;

        private readonly List<FleetEntity> _fleets = new();

        public FleetService(
            IFleetRepository fleetRepository,
            IResourceRepository resourceRepository,
            GameConfigSO config)
        {
            _fleetRepository    = fleetRepository;
            _resourceRepository = resourceRepository;
            _config             = config;
        }

        /// <summary>Returns all active fleets.</summary>
        public IReadOnlyList<FleetEntity> GetAll() => _fleets.AsReadOnly();

        /// <summary>Finds a fleet by ID.</summary>
        public FleetEntity GetById(Guid fleetId)
        {
            return _fleets.FirstOrDefault(f => f.Id == fleetId);
        }

        /// <summary>Creates a new fleet - checks resources and fleet size limit first.</summary>
        public FleetEntity CreateFleet(CreateFleetCommand cmd)
        {
            if (_fleets.Count >= _config.MaxFleetSize)
                throw new InvalidOperationException($"Fleet limit reached ({_config.MaxFleetSize}).");

            float totalMetal  = cmd.Blueprint.MetalCost  * cmd.ShipCount;
            float totalEnergy = cmd.Blueprint.EnergyCost * cmd.ShipCount;

            var wallet = _resourceRepository.Get();

            if (!wallet.CanAfford(ResourceType.Metal, totalMetal))
                throw new InvalidOperationException($"Not enough Metal. Need {totalMetal}.");

            if (!wallet.CanAfford(ResourceType.Energy, totalEnergy))
                throw new InvalidOperationException($"Not enough Energy. Need {totalEnergy}.");

            wallet = wallet.Spend(ResourceType.Metal, totalMetal);
            wallet = wallet.Spend(ResourceType.Energy, totalEnergy);
            _resourceRepository.Save(wallet);

            var ships = Enumerable.Range(0, cmd.ShipCount)
                .Select(_ => cmd.Blueprint.CreateEntity())
                .ToList()
                .AsReadOnly();

            var fleet = FleetEntity.Create(cmd.Name, ships);
            _fleets.Add(fleet);

            foreach (var ship in fleet.Ships)
                _fleetRepository.Add(ship);

            GELogger.Info(LogCategory.Fleet,
                $"Fleet '{fleet.Name}' created with {fleet.ShipCount} {cmd.Blueprint.ShipName}(s).");

            return fleet;
        }

        /// <summary>Dispatches a fleet toward a target.</summary>
        public FleetEntity Dispatch(DispatchFleetCommand cmd)
        {
            var fleet = GetFleetOrThrow(cmd.FleetId);
            var updated = fleet.Dispatch();

            UpdateFleet(fleet, updated);

            GELogger.Info(LogCategory.Fleet, $"Fleet '{fleet.Name}' dispatched.");
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

        /// <summary>Queues a single ship for construction.</summary>
        public void BuildShip(BuildShipCommand cmd)
        {
            var wallet = _resourceRepository.Get();

            if (!wallet.CanAfford(ResourceType.Metal, cmd.Blueprint.MetalCost))
                throw new InvalidOperationException($"Not enough Metal to build {cmd.Blueprint.ShipName}.");

            wallet = wallet.Spend(ResourceType.Metal, cmd.Blueprint.MetalCost);
            wallet = wallet.Spend(ResourceType.Energy, cmd.Blueprint.EnergyCost);
            _resourceRepository.Save(wallet);

            var ship = cmd.Blueprint.CreateEntity();
            _fleetRepository.Add(ship);

            GELogger.Info(LogCategory.Fleet, $"Ship '{ship.Name}' queued for construction.");
        }

        private FleetEntity GetFleetOrThrow(Guid fleetId)
        {
            var fleet = GetById(fleetId);
            if (fleet == null)
                throw new InvalidOperationException($"Fleet {fleetId} not found.");
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
