// Main game loop - no MonoBehaviour, VContainer manages the lifecycle.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Station.Application;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Starts all game systems on scene load.</summary>
    public sealed class GameEntryPoint : IInitializable, IDisposable
    {
        private readonly IFleetRepository   _fleetRepository;
        private readonly IStationRepository _stationRepository;
        private readonly IResourceService  _resourceService;
        private readonly IGalaxyService  _galaxyService;
        private readonly GameConfigSO _config;

        private CancellationTokenSource _cts;

        public GameEntryPoint(
            IFleetRepository fleetRepository,
            IStationRepository stationRepository,
            IResourceService resourceService,
            IGalaxyService galaxyService,
            GameConfigSO config)
        {
            _fleetRepository  = fleetRepository;
            _stationRepository = stationRepository;
            _resourceService = resourceService;
            _galaxyService = galaxyService;
            _config = config;
        }

        public void Initialize()
        {
            GELogger.Info(LogCategory.System, "Galactic Empire initializing...");

            InitializeStation();
            InitializeFleet();
            InitializeGalaxy();
            StartEconomy();

            GELogger.Info(LogCategory.System, "All systems online.");
        }

        public void Dispose()
        {
            // Stop the production loop cleanly on scene unload
            _cts?.Cancel();
            _cts?.Dispose();

            GELogger.Info(LogCategory.System, "Shutting down.");
        }

        private void InitializeStation()
        {
            if (!_stationRepository.HasStation())
            {
                var station = StationEntity.Create(
                    "Galactic Empire HQ",
                    gridSize: _config.StationGridSize);

                _stationRepository.Save(station);
                GELogger.Info(LogCategory.Station, $"Station created: {station.Name}");
            }
            else
            {
                var station = _stationRepository.Get();
                GELogger.Info(LogCategory.Station, $"Station loaded: {station.Name} | Modules: {station.TotalModules}");
            }
        }

        private void InitializeGalaxy()
        {
            var galaxy = _galaxyService.GetGalaxy();

            if (galaxy == null)
            {
                // First launch - generate a fresh galaxy
                galaxy = _galaxyService.GenerateGalaxy(sectorCount: 30);
                GELogger.Info(LogCategory.System,
                    $"Galaxy generated: {galaxy.TotalSectors} sectors.");
            }
            else
            {
                GELogger.Info(LogCategory.System,
                    $"Galaxy loaded: {galaxy.TotalSectors} sectors, " +
                    $"{galaxy.DiscoveredCount} discovered.");
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
            GELogger.Info(LogCategory.Fleet, $"Fleet ready. Ships: {_fleetRepository.GetAll().Count}");
        }

        private void StartEconomy()
        {
            // CancellationToken ties the loop lifetime to this entry point
            _cts = new CancellationTokenSource();
            _resourceService.StartProductionLoopAsync(_cts.Token).Forget();

            var wallet = _resourceService.GetCurrentWallet();
            GELogger.Info(LogCategory.Economy,
                $"Economy started. Metal: {wallet.Get(ResourceType.Metal)} | Energy: {wallet.Get(ResourceType.Energy)}");
        }
    }
}
