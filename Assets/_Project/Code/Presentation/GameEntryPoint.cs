// Main game loop - no MonoBehaviour, VContainer manages the lifecycle.

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Application;
using GalacticEmpire.Feature.Battle.Presentation;
using GalacticEmpire.Feature.Exploration.Application;
using GalacticEmpire.Feature.Exploration.Domain;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Fleet.Domain;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.UI.Core;
using GalacticEmpire.Feature.UI.Screens;
using UnityEngine;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Starts all game systems on scene load.</summary>
    public sealed class GameEntryPoint : IInitializable, IDisposable
    {
        private readonly IStationService _stationService;
        private readonly IResourceService _resourceService;
        private readonly IGalaxyService _galaxyService;
        private readonly IFleetService _fleetService;
        private readonly IEncounterService _encounterService;
        private readonly IExplorationService _explorationService;
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
        private CancellationTokenSource _explorationCts;

        public GameEntryPoint(
            IStationService stationService,
            IResourceService resourceService,
            IGalaxyService galaxyService,
            IFleetService fleetService,
            IEncounterService encounterService,
            IExplorationService explorationService,
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
            _stationService = stationService;
            _resourceService = resourceService;
            _galaxyService = galaxyService;
            _fleetService = fleetService;
            _encounterService = encounterService;
            _explorationService = explorationService;
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
            // Routed through each feature's service, not the repository directly -
            // see MASTER.md Section 5 "Startup state reset".
            _stationService.ClearStation();
            _fleetService.ClearFleet();
            _galaxyService.ClearGalaxy();
            _explorationService.ClearMissions();

            InitializeStation();
            InitializeFleet();
            InitializeGalaxy();
            StartEconomy();
            StartExploration();
            InitializeUI().Forget();

            GELogger.Info(LogCategory.System, "All systems online.");
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _explorationCts?.Cancel();
            _explorationCts?.Dispose();
            GELogger.Info(LogCategory.System, "Shutting down.");
        }

        private void InitializeStation()
        {
            _stationService.EnsureStation("Galactic Empire HQ", _config.StationGridSize);
        }

        private void InitializeFleet()
        {
            _fleetService.EnsureStartingFleet("Home Fleet", "Destroyer I", _config.MaxFleetSize, 25f, _config.DefaultShipSpeed);
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

        private void StartExploration()
        {
            _explorationCts = new CancellationTokenSource();
            _explorationService.StartMissionTickLoopAsync(_explorationCts.Token).Forget();

            // Exploration never calls into Battle.Application directly
            // (MASTER.md Section 3) - this callback lets ExplorationService
            // ask "does this sector have an encounter" without referencing
            // IEncounterService/BattlePresenter itself. Same shape as
            // _galaxyMapScreen.SetBattleInProgressCheck below.
            _explorationService.SetEncounterCheck(ResolveEncounterForMission);
        }

        // Called by ExplorationService when a mission's fleet arrives at its
        // target sector. Returns true (and starts the real-time battle) if
        // the sector has an unresolved NPC garrison, false if it's clear.
        private bool ResolveEncounterForMission(MissionEntity mission)
        {
            var enemyFleet = _encounterService.CheckForEncounter(mission.TargetSectorId);
            if (enemyFleet == null)
                return false;

            var fleet = _fleetService.GetById(mission.FleetId);
            if (fleet == null)
                return false;

            _battlePresenter.StartEncounter(fleet, enemyFleet, mission.TargetSectorId);
            return true;
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
            _fleetManagementScreen.Initialize(_fleetService, _explorationService);

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

            // Auto-return (VISION.md decision 2) needs to know the encounter's
            // outcome - BattlePresenter raises this once a real-time battle
            // finishes and the result has been applied.
            _battlePresenter.OnEncounterResolved += _explorationService.NotifyEncounterResolved;

            GELogger.Info(LogCategory.UI, "UI initialized. Main Menu shown.");
        }

        // Dispatch is no longer instant (Exploration Stage 1a) - the fleet
        // starts a MissionEntity instead of resolving arrival on the spot.
        // Encounter/discovery/reward happen when ExplorationService's tick
        // loop detects the fleet has reached its target - see
        // ResolveEncounterForMission above and ExplorationService.ResolveArrival.
        private void HandleFleetDispatchedToSector(FleetEntity dispatchedFleet, Guid sectorId)
        {
            _explorationService.StartMission(new StartMissionCommand(dispatchedFleet, sectorId));
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
            // Routed through UIManager (not called directly on the screen) so
            // Galaxy/Station/Fleet stay mutually exclusive - ToggleAsync hides
            // whichever of the other two was open before showing this one.
            _uiManager.ToggleAsync<GalaxyMapScreen>().Forget();
        }

        private void HandleStationPressed()
        {
            _uiManager.ToggleAsync<StationBuilderScreen>().Forget();
        }

        private void HandleFleetPressed()
        {
            _uiManager.ToggleAsync<FleetManagementScreen>().Forget();
        }
    }
}
