// Entry point - configures VContainer DI container.

using GalacticEmpire.Core;
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
        [SerializeField] private GameConfigSO _config;

        protected override void Configure(IContainerBuilder builder)
        {
            // Config is needed everywhere
            builder.RegisterInstance(_config);

            // Repositories - registered as interfaces so nothing depends on the SO directly
            builder.RegisterInstance(_fleetRepository).As<IFleetRepository>();
            builder.RegisterInstance(_stationRepository).As<IStationRepository>();
            builder.RegisterInstance(_resourceRepository).As<IResourceRepository>();

            // Production service - runs the economy tick loop
            builder.Register<IResourceService, ResourceProductionService>(VContainer.Lifetime.Singleton);

            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
