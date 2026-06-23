// Entry point — configures VContainer DI container.

using GalacticEmpire.Core;
using GalacticEmpire.Infrastructure;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Application entry point. Configures the DI container.</summary>
    public sealed class GameBootstrapper : LifetimeScope
    {
        // The FleetRepository asset assigned in the Inspector.
        // VContainer will inject this wherever IFleetRepository is requested.
        [SerializeField] private FleetRepositorySO _fleetRepository;

        /// <summary>Registers all game dependencies into the DI container.</summary>
        protected override void Configure(IContainerBuilder builder)
        {
            // Register the ScriptableObject instance as IFleetRepository.
            // Any class that asks for IFleetRepository gets this exact asset.
            builder.RegisterInstance(_fleetRepository).As<IFleetRepository>();

            // Register the main game loop controller.
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
