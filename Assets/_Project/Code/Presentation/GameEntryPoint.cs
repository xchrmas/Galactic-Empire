// // Main game loop - controlled by VContainer, no MonoBehaviour

using System;
using UnityEngine;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    public sealed class GameEntryPoint : IInitializable, ITickable, IDisposable
    {
        public void Initialize()
        {
            Debug.Log("[GameEntryPoint] Galactic Empire initialized.");
        }

        public void Tick() { }

        public void Dispose()
        {
            Debug.Log("[GameEntryPoint] Shutting down.");
        }
    }
}
