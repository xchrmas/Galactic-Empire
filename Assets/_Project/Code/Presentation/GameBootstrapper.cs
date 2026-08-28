// Entry point - configures VContainer DI container.

using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Application;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Galaxy.Infrastructure;
using GalacticEmpire.Feature.Galaxy.Presentation;
using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.Station.Infrastructure;
using GalacticEmpire.Feature.UI.Core;
using GalacticEmpire.Feature.UI.Screens;
using GalacticEmpire.Infrastructure;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Wires up all dependencies for the game.</summary>
    // Runs after all default-order MonoBehaviours (including every ScreenBase)
    // so their Awake() - which sets up CanvasGroup - has already completed
    // by the time this builds the container and calls ShowAsync().
    [DefaultExecutionOrder(1000)]
    public sealed class GameBootstrapper : LifetimeScope
    {
        [SerializeField] private FleetRepositorySO _fleetRepository;
        [SerializeField] private StationRepositorySO _stationRepository;
        [SerializeField] private ResourceRepositorySO _resourceRepository;
        [SerializeField] private GalaxyRepositorySO _galaxyRepository;
        [SerializeField] private GalaxyMapPresenter _galaxyMapPresenter;

        [SerializeField] private UIManager _uiManager;
        [SerializeField] private MainMenuScreen _mainMenuScreen;
        [SerializeField] private HUDScreen _hudScreen;
        [SerializeField] private GalaxyMapScreen _galaxyMapScreen;
        [SerializeField] private GameConfigSO _config;

        protected override void Configure(IContainerBuilder builder)
        {
            // Config is needed everywhere
            builder.RegisterInstance(_config);

            // Repositories
            builder.RegisterInstance(_fleetRepository).As<IFleetRepository>();
            builder.RegisterInstance(_stationRepository).As<IStationRepository>();
            builder.RegisterInstance(_resourceRepository).As<IResourceRepository>();
            builder.RegisterInstance(_galaxyRepository).As<IGalaxyRepository>();

            // Services
            builder.Register<IResourceService, ResourceProductionService>(Lifetime.Singleton);
            builder.Register<IFleetService, FleetService>(Lifetime.Singleton);
            builder.Register<GalaxyGeneratorService>(Lifetime.Singleton);
            builder.Register<IGalaxyService, GalaxyService>(Lifetime.Singleton);
            builder.Register<CombatTickService>(Lifetime.Singleton);
            builder.Register<IBattleService, BattleService>(Lifetime.Singleton);

            // UI
            builder.RegisterComponent(_uiManager);
            builder.RegisterComponent(_mainMenuScreen);
            builder.RegisterComponent(_hudScreen);
            builder.RegisterComponent(_galaxyMapScreen);
            builder.RegisterComponent(_galaxyMapPresenter);

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
