
// Layer: Presentation | Entry point — configures VContainer DI container.

using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    public sealed class GameBootstrapper : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
