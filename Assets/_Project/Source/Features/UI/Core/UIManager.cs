// Central screen manager - controls which screen is visible.
// Only one screen can be active at a time (except HUD which overlays everything).

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GalacticEmpire.Core;
using UnityEngine;

namespace GalacticEmpire.Feature.UI.Core
{
    /// <summary>Manages screen transitions across the entire game UI.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        // All screens registered at startup
        private readonly Dictionary<Type, IScreen> _screens = new();

        private IScreen _currentScreen;

        /// <summary>Register a screen so UIManager can show/hide it.</summary>
        public void Register<T>(T screen) where T : IScreen
        {
            _screens[typeof(T)] = screen;
            GELogger.Info(LogCategory.UI, $"Screen registered: {typeof(T).Name}");
        }

        /// <summary>Show a screen - hides the current one first.</summary>
        public async UniTask ShowAsync<T>() where T : IScreen
        {
            if (!_screens.TryGetValue(typeof(T), out var screen))
            {
                GELogger.Error(LogCategory.UI, $"Screen not registered: {typeof(T).Name}");
                return;
            }

            // Hide current screen before showing the new one
            if (_currentScreen != null && _currentScreen.IsVisible)
                await _currentScreen.HideAsync();

            _currentScreen = screen;
            await screen.ShowAsync();

            GELogger.Info(LogCategory.UI, $"Showing screen: {typeof(T).Name}");
        }

        /// <summary>Hide a specific screen without showing another.</summary>
        public async UniTask HideAsync<T>() where T : IScreen
        {
            if (!_screens.TryGetValue(typeof(T), out var screen))
                return;

            if (screen.IsVisible)
                await screen.HideAsync();
        }

        /// <summary>Returns true if the given screen is currently visible.</summary>
        public bool IsVisible<T>() where T : IScreen
        {
            return _screens.TryGetValue(typeof(T), out var screen) && screen.IsVisible;
        }
    }
}