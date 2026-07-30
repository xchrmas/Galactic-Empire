// Entry point - configures VContainer DI container.

using GalacticEmpire.Core;
using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Galaxy.Infrastructure;
using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.Station.Infrastructure;
using GalacticEmpire.Infrastructure;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Wires up all dependencies for the game.</summary>
    public sealed class GameBootstrapper : LifetimeScope
    {
        [SerializeField] private FleetRepositorySO _fleetRepository;
        [SerializeField] private StationRepositorySO _stationRepository;
        [SerializeField] private ResourceRepositorySO _resourceRepository;
        [SerializeField] private GalaxyRepositorySO _galaxyRepository;
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

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
