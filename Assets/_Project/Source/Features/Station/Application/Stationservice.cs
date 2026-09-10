// Handles all station use cases - checks resources and module limit, persists state.

using System;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Station.Domain;

namespace GalacticEmpire.Feature.Station.Application
{
    /// <summary>Executes station use cases - module placement, removal.</summary>
    public sealed class StationService : IStationService
    {
        private readonly IStationRepository _stationRepository;
        private readonly IResourceRepository _resourceRepository;
        private readonly GameConfigSO _config;

        public StationService(
            IStationRepository stationRepository,
            IResourceRepository resourceRepository,
            GameConfigSO config)
        {
            _stationRepository  = stationRepository;
            _resourceRepository = resourceRepository;
            _config              = config;
        }

        /// <summary>Returns the player's station.</summary>
        public StationEntity GetStation() => _stationRepository.Get();

        /// <summary>Places a new module - checks module limit and resources first.</summary>
        public StationEntity PlaceModule(PlaceModuleCommand cmd)
        {
            var station = GetStationOrThrow();

            if (station.TotalModules >= _config.MaxStationModules)
                throw new InvalidOperationException($"Module limit reached ({_config.MaxStationModules}).");

            var wallet = _resourceRepository.Get();

            if (!wallet.CanAfford(ResourceType.Metal, cmd.Module.BuildCostMetal))
                throw new InvalidOperationException($"Not enough Metal. Need {cmd.Module.BuildCostMetal}.");

            if (!wallet.CanAfford(ResourceType.Energy, cmd.Module.BuildCostEnergy))
                throw new InvalidOperationException($"Not enough Energy. Need {cmd.Module.BuildCostEnergy}.");

            wallet = wallet.Spend(ResourceType.Metal, cmd.Module.BuildCostMetal);
            wallet = wallet.Spend(ResourceType.Energy, cmd.Module.BuildCostEnergy);
            _resourceRepository.Save(wallet);

            var updated = station.PlaceModule(cmd.Module, cmd.X, cmd.Y);
            _stationRepository.Save(updated);

            GELogger.Info(LogCategory.Station,
                $"Module '{cmd.Module.Name}' placed at ({cmd.X},{cmd.Y}).");

            return updated;
        }

        /// <summary>Removes the module at the given position.</summary>
        public StationEntity RemoveModule(RemoveModuleCommand cmd)
        {
            var station = GetStationOrThrow();

            var updated = station.RemoveModule(cmd.X, cmd.Y);
            _stationRepository.Save(updated);

            GELogger.Info(LogCategory.Station, $"Module removed at ({cmd.X},{cmd.Y}).");

            return updated;
        }

        private StationEntity GetStationOrThrow()
        {
            var station = _stationRepository.Get();
            if (station == null)
                throw new InvalidOperationException("No station exists yet.");
            return station;
        }
    }
}