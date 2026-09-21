// Main game loop - no MonoBehaviour, VContainer manages the lifecycle.

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Application;
using GalacticEmpire.Feature.Battle.Presentation;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.UI.Core;
using GalacticEmpire.Feature.UI.Screens;
using UnityEngine;
using VContainer.Unity;
using GalacticEmpire.Feature.Station.Domain;

namespace GalacticEmpire.Presentation
{
    /// <summary>Starts all game systems on scene load.</summary>
    public sealed class GameEntryPoint : IInitializable, IDisposable
    {
        private readonly IFleetRepository _fleetRepository;
        private readonly IStationRepository _stationRepository;
        private readonly IResourceService _resourceService;
        private readonly IGalaxyService _galaxyService;
        private readonly IFleetService _fleetService;
        private readonly IEncounterService _encounterService;
        private readonly BattlePresenter _battlePresenter;
        private readonly UIManager _uiManager;
        private readonly MainMenuScreen _mainMenuScreen;
        private readonly HUDScreen _hudScreen;
        private readonly GalaxyMapScreen _galaxyMapScreen;
        private readonly StationBuilderScreen _stationBuilderScreen;
        private readonly FleetManagementScreen _fleetManagementScreen;
        private readonly BattleHUDScreen _battleHUDScreen;
        private readonly IResourceRepository _resourceRepository;
        private readonly GameConfigSO _config;

        private CancellationTokenSource _cts;

        public GameEntryPoint(
            IFleetRepository fleetRepository,
            IStationRepository stationRepository,
            IResourceService resourceService,
            IGalaxyService galaxyService,
            IFleetService fleetService,
            IEncounterService encounterService,
            BattlePresenter battlePresenter,
            UIManager uiManager,
            MainMenuScreen mainMenuScreen,
            HUDScreen hudScreen,
            GalaxyMapScreen galaxyMapScreen,
            StationBuilderScreen stationBuilderScreen,
            FleetManagementScreen fleetManagementScreen,
            BattleHUDScreen battleHUDScreen,
            IResourceRepository resourceRepository,
            GameConfigSO config)
        {
            _fleetRepository = fleetRepository;
            _stationRepository = stationRepository;
            _resourceService = resourceService;
            _galaxyService = galaxyService;
            _fleetService = fleetService;
            _encounterService = encounterService;
            _battlePresenter = battlePresenter;
            _uiManager = uiManager;
            _mainMenuScreen = mainMenuScreen;
            _hudScreen = hudScreen;
            _galaxyMapScreen = galaxyMapScreen;
            _stationBuilderScreen = stationBuilderScreen;
            _fleetManagementScreen = fleetManagementScreen;
            _battleHUDScreen = battleHUDScreen;
            _resourceRepository = resourceRepository;
            _config = config;
        }

        public void Initialize()
        {
            GELogger.Info(LogCategory.System, "Galactic Empire initializing...");

            // Repositories are ScriptableObject assets - Unity keeps their data
            // between Play sessions in the Editor. Clearing here guarantees every
            // run starts from the same clean state instead of accumulating leftovers.
            _stationRepository.Clear();
            _fleetRepository.Clear();
            _galaxyService.ClearGalaxy();

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

            var ships = new List<ShipEntity> { ship }.AsReadOnly();
            _fleetService.RegisterStartingFleet("Home Fleet", ships);

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
            RunEconomyLoopAsync(_cts.Token).Forget();

            var wallet = _resourceService.GetCurrentWallet();
            GELogger.Info(LogCategory.Economy,
                $"Economy started. Metal: {wallet.Get(ResourceType.Metal)} | Energy: {wallet.Get(ResourceType.Energy)}");
        }

        // Runs production tick and refreshes HUD every interval
        private async UniTaskVoid RunEconomyLoopAsync(CancellationToken ct)
        {
            GELogger.Info(LogCategory.Economy, "Production loop is running.");

            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(_config.BaseProductionRate),
                    cancellationToken: ct);

                _resourceService.Tick();

                // Refresh HUD if it's visible
                if (_hudScreen.IsVisible)
                {
                    _hudScreen.RefreshResources();
                }
            }
        }

        private async UniTaskVoid InitializeUI()
        {
            // Register all screens
            _uiManager.Register(_mainMenuScreen);
            _uiManager.Register(_hudScreen);
            _uiManager.Register(_galaxyMapScreen);
            _uiManager.Register(_stationBuilderScreen);
            _uiManager.Register(_fleetManagementScreen);
            _uiManager.Register(_battleHUDScreen);

            // Initialize HUD with resource repository
            _hudScreen.Initialize(_resourceRepository);
            _galaxyMapScreen.Initialize(_fleetService);
            _fleetManagementScreen.Initialize(_fleetService);

            // Show Main Menu first
            await _uiManager.ShowAsync<MainMenuScreen>();

            // When Play is pressed - switch to HUD
            _mainMenuScreen.OnPlayPressed += HandlePlayPressed;

            // Galaxy map, station builder and fleet management are overlays above HUD,
            // not full screen swaps
            _hudScreen.OnGalaxyPressed += HandleGalaxyPressed;
            _hudScreen.OnStationPressed += HandleStationPressed;
            _hudScreen.OnFleetPressed += HandleFleetPressed;

            // BattleHUDScreen has no HUD button and no toggle handler - BattlePresenter
            // shows/hides it directly when a real-time encounter starts/ends (see
            // features/battle.md). Registering it here only makes it known to
            // UIManager, matching every other screen's registration.

            // GalaxyMapScreen (in UI.Screens) can't reference IEncounterService or
            // BattlePresenter directly - Battle.Presentation already references
            // UI.Screens for BattleHUDScreen, so a reference back would be circular
            // (see the note in Galaxymapscreen.cs). GameEntryPoint sits above both
            // assemblies, so the wiring happens here instead.
            _galaxyMapScreen.SetBattleInProgressCheck(() => _battlePresenter.IsBattleInProgress);
            _galaxyMapScreen.OnFleetDispatchedToSector += HandleFleetDispatchedToSector;

            GELogger.Info(LogCategory.UI, "UI initialized. Main Menu shown.");
        }

        // No travel-time system exists yet (Dispatch is instant - see MASTER.md
        // Section 8 / BACKLOG-26) so arrival is checked immediately after Dispatch
        // rather than on a separate "fleet arrived" tick.
        private void HandleFleetDispatchedToSector(FleetEntity dispatchedFleet, Guid sectorId)
        {
            var enemyFleet = _encounterService.CheckForEncounter(sectorId);
            if (enemyFleet == null)
                return;

            _battlePresenter.StartEncounter(dispatchedFleet, enemyFleet, sectorId);
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

        private void HandleGalaxyPressed()
        {
            // Toggle: hide if already open, show otherwise - HUD stays visible either way
            if (_galaxyMapScreen.IsVisible)
                _galaxyMapScreen.HideAsync().Forget();
            else
                _galaxyMapScreen.ShowAsync().Forget();
        }

        private void HandleStationPressed()
        {
            if (_stationBuilderScreen.IsVisible)
                _stationBuilderScreen.HideAsync().Forget();
            else
                _stationBuilderScreen.ShowAsync().Forget();
        }

        private void HandleFleetPressed()
        {
            if (_fleetManagementScreen.IsVisible)
                _fleetManagementScreen.HideAsync().Forget();
            else
                _fleetManagementScreen.ShowAsync().Forget();
        }
    }
}
