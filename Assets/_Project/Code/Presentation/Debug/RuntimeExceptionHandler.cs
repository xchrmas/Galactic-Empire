// Presentation Catches all unhandled exceptions at runtime.


using System;
using GalacticEmpire.Core;
using UnityEngine;

namespace GalacticEmpire.Presentation
{
    /// <summary>Global runtime exception handler — attach to [GameBootstrapper] GameObject.</summary>
    public sealed class RuntimeExceptionHandler : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;

        private void Awake()
        {
            // Subscribe to all unhandled exceptions and log errors
            Application.logMessageReceived += OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            GELogger.Info(LogCategory.System, "RuntimeExceptionHandler initialized.");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        }

        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            // Only handle errors and exceptions — ignore regular logs
            if (type != LogType.Error && type != LogType.Exception)
                return;

            GELogger.Error(LogCategory.System, $"{message}\n{stackTrace}");

#if !UNITY_EDITOR
            // In Release builds show a user-friendly message
            ShowErrorUI(message);
#endif
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = args.ExceptionObject as Exception;
            string message = ex?.Message ?? "Unknown error";

            GELogger.Error(LogCategory.System, "Unhandled exception caught.", ex);

#if !UNITY_EDITOR
            ShowErrorUI(message);
#endif
        }

        private void ShowErrorUI(string message)
        {
            // TODO: replace with proper UI popup in Phase 7
            Debug.LogError($"[GalacticEmpire] Something went wrong. Log saved to: {GELogger.GetLogFilePath()}");
        }
    }
}
