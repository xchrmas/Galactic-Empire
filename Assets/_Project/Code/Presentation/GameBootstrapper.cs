
//Entry point-configures VContainer DI container.

using GalacticEmpire.Core;
using GalacticEmpire.Infrastructure;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Application entry point. Configures the DI container</summary>
    public sealed class GameBootstrapper : LifetimeScope
    {
        [SerializeField] private FleetRepositorySO _fleetRepository;
        [SerializeField] private StationRepositorySO _stationRepository;
        [SerializeField] private GameConfigSO _config;

        /// <summary>Registers all game dependencies into the DI container</summary>
        protected override void Configure(IContainerBuilder builder)
        {
            // Register config — available everywhere in the app
            builder.RegisterInstance(_config);

            // Registering repositories as their interfaces - DI
            builder.RegisterInstance(_fleetRepository).As<IFleetRepository>();
            builder.RegisterInstance(_stationRepository).As<IStationRepository>();

            // Register the main game loop controller
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
