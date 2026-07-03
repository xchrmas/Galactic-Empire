

// Main game loop-managed by VContainer
using System;
using GalacticEmpire.Core;
using UnityEngine;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Controls the main game lifecycle via VContainer interfaces.</summary>
    public sealed class GameEntryPoint : IInitializable, IDisposable
    {
        private readonly IFleetRepository _fleetRepository;
        private readonly IStationRepository _stationRepository;
        private readonly GameConfigSO _config;

        // VContainer injects all dependencies via constructor automatically
        public GameEntryPoint(
            IFleetRepository fleetRepository,
            IStationRepository stationRepository,
            GameConfigSO config)
        {
            _fleetRepository = fleetRepository;
            _stationRepository = stationRepository;
            _config = config;
        }

        /// <summary>Called once on scene start.</summary>
        public void Initialize()
        {
            GELogger.Info(LogCategory.System, "Galactic Empire initializing");

            InitializeStation();
            InitializeFleet();

            GELogger.Info(LogCategory.System, "Galactic Empire initialized. All systems online");
        }

        /// <summary>Called on scene unload — cleanup.</summary>
        public void Dispose()
        {
            GELogger.Info(LogCategory.System, "Galactic Empire shutting down");
        }

        private void InitializeStation()
        {
            if (!_stationRepository.HasStation())
            {
                // Create default station on first launch
                var station = StationEntity.Create(
                    "Galactic Empire HQ",
                    gridSize: _config.StationGridSize);

                _stationRepository.Save(station);

                GELogger.Info(LogCategory.Station,
                    $"Station created: {station.Name} | Grid: {station.GridSize}x{station.GridSize}");
            }
            else
            {
                var station = _stationRepository.Get();
                GELogger.Info(LogCategory.Station,
                    $"Station loaded: {station.Name} | Modules: {station.TotalModules}");
            }
        }

        private void InitializeFleet()
        {
            var ship = ShipEntity.Create(
                "Destroyer I",
                maxHull: _config.MaxFleetSize,
                damage: 25f,
                speed: _config.DefaultShipSpeed);

            _fleetRepository.Add(ship);

            GELogger.Info(LogCategory.Fleet,
                $"Fleet initialized. Ships: {_fleetRepository.GetAll().Count}");
        }
    }
}
