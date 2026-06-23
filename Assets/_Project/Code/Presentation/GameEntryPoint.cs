
using System;
using GalacticEmpire.Core;
using UnityEngine;
using VContainer.Unity;

namespace GalacticEmpire.Presentation
{
    /// <summary>Controls the main game lifecycle via VContainer interfaces.</summary>
    public sealed class GameEntryPoint : IInitializable, IDisposable
    {
        private readonly IFleetRepository _fleetRepository;

        // VContainer automatically injects IFleetRepository via constructor injection.
        // No need to find or create it manually — DI handles this.
        public GameEntryPoint(IFleetRepository fleetRepository)
        {
            _fleetRepository = fleetRepository;
        }

        /// <summary>Called once on scene start.</summary>
        public void Initialize()
        {
            // Test: create a ship and add it to the fleet via repository
            var ship = ShipEntity.Create("Destroyer", maxHull: 100f, damage: 25f, speed: 10f);
            _fleetRepository.Add(ship);

            var fleet = _fleetRepository.GetAll();
            Debug.Log($"[GameEntryPoint] Galactic Empire initialized. Fleet size: {fleet.Count}");
        }

        /// <summary>Called on scene unload — cleanup.</summary>
        public void Dispose()
        {
            Debug.Log("[GameEntryPoint] Shutting down.");
        }
    }
}
