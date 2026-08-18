// Main game loop - no MonoBehaviour, VContainer manages the lifecycle.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.UI.Core;
using GalacticEmpire.Feature.UI.Screens;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Starts all game systems on scene load.</summary>
    public sealed class GameEntryPoint : IInitializable, IDisposable
    {
        private readonly IFleetRepository _fleetRepository;
        private readonly IStationRepository _stationRepository;
        private readonly IResourceService _resourceService;
        private readonly IGalaxyService _galaxyService;
        private readonly UIManager _uiManager;
        private readonly MainMenuScreen _mainMenuScreen;
        private readonly HUDScreen _hudScreen;
        private readonly IResourceRepository _resourceRepository;
        private readonly GameConfigSO _config;

        private CancellationTokenSource _cts;

        public GameEntryPoint(
            IFleetRepository fleetRepository,
            IStationRepository stationRepository,
            IResourceService resourceService,
            IGalaxyService galaxyService,
            UIManager uiManager,
            MainMenuScreen mainMenuScreen,
            HUDScreen hudScreen,
            IResourceRepository resourceRepository,
            GameConfigSO config)
        {
            _fleetRepository = fleetRepository;
            _stationRepository = stationRepository;
            _resourceService = resourceService;
            _galaxyService = galaxyService;
            _uiManager = uiManager;
            _mainMenuScreen = mainMenuScreen;
            _hudScreen = hudScreen;
            _resourceRepository = resourceRepository;
            _config = config;
        }

        public void Initialize()
        {
            GELogger.Info(LogCategory.System, "Galactic Empire initializing...");

            InitializeStation();
            InitializeFleet();
            InitializeGalaxy();
            StartEconomy();
            InitializeUI().Forget();

            GELogger.Info(LogCategory.System, "All systems online.");
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            GELogger.Info(LogCategory.System, "Shutting down.");
        }

        private void InitializeStation()
        {
            if (!_stationRepository.HasStation())
            {
                var station = StationEntity.Create("Galactic Empire HQ", _config.StationGridSize);
                _stationRepository.Save(station);
                GELogger.Info(LogCategory.Station, $"Station created: {station.Name}");
            }
            else
            {
                var station = _stationRepository.Get();
                GELogger.Info(LogCategory.Station, $"Station loaded: {station.Name} | Modules: {station.TotalModules}");
            }
        }

        private void InitializeFleet()
        {
            var ship = ShipEntity.Create("Destroyer I", _config.MaxFleetSize, 25f, _config.DefaultShipSpeed);
            _fleetRepository.Add(ship);
            GELogger.Info(LogCategory.Fleet, $"Fleet ready. Ships: {_fleetRepository.GetAll().Count}");
        }

        private void InitializeGalaxy()
        {
            var galaxy = _galaxyService.GetGalaxy();

            if (galaxy == null)
            {
                galaxy = _galaxyService.GenerateGalaxy(sectorCount: 30);
                GELogger.Info(LogCategory.System, $"Galaxy generated: {galaxy.TotalSectors} sectors.");
            }
            else
            {
                GELogger.Info(LogCategory.System,
                    $"Galaxy loaded: {galaxy.TotalSectors} sectors, {galaxy.DiscoveredCount} discovered.");
            }
        }

        private void StartEconomy()
        {
            _cts = new CancellationTokenSource();
            _resourceService.StartProductionLoopAsync(_cts.Token).Forget();

            var wallet = _resourceService.GetCurrentWallet();
            GELogger.Info(LogCategory.Economy,
                $"Economy started. Metal: {wallet.Get(ResourceType.Metal)} | Energy: {wallet.Get(ResourceType.Energy)}");
        }

        private async UniTaskVoid InitializeUI()
        {
            GELogger.Info(LogCategory.UI, "InitializeUI called.");

            // Register all screens
            _uiManager.Register(_mainMenuScreen);
            _uiManager.Register(_hudScreen);

            // Initialize HUD with resource repository
            _hudScreen.Initialize(_resourceRepository);

            // Show Main Menu first
            await _uiManager.ShowAsync<MainMenuScreen>();

            // When Play is pressed - switch to HUD
            _mainMenuScreen.OnPlayPressed += HandlePlayPressed;

            GELogger.Info(LogCategory.UI, "UI initialized. Main Menu shown.");
        }

        private void HandlePlayPressed()
        {
            _mainMenuScreen.OnPlayPressed -= HandlePlayPressed;
            SwitchToGameAsync().Forget();
        }

        private async UniTaskVoid SwitchToGameAsync()
        {
            await _uiManager.HideAsync<MainMenuScreen>();
            await _uiManager.ShowAsync<HUDScreen>();

            GELogger.Info(LogCategory.UI, "Switched to game HUD.");
        }
    }
}
