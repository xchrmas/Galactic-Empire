
// Centralized logger for Galactic Empire.

using System;
using System.IO;
using UnityEngine;

namespace GalacticEmpire.Core
{
    /// <summary>Log category-filter messages by system.</summary>
    public enum LogCategory
    {
        Core,
        Fleet,
        Station,
        Battle,
        Economy,
        Network,
        UI,
        ECS,
        System
    }

    /// <summary>Centralized logger with categories, severity, and file output.</summary>
    public static class GELogger
    {
        // Log file path-written next to the project
            private static readonly string LogFilePath =
            Path.Combine(Application.persistentDataPath, "GalacticEmpire.log");

        private static bool _initialized;

        private static void Init()
        {
            if (_initialized)
            {
                return;
            }

            File.WriteAllText(LogFilePath, $"=== Galactic Empire Log — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
            _initialized = true;
        }

        /// <summary>Logs an info message. Disabled in Release builds.</summary>
        public static void Info(LogCategory category, string message)
        {
     #if UNITY_EDITOR || DEVELOPMENT_BUILD
            string formatted = Format(category, "INFO", message);
            Debug.Log(formatted);
            Write(formatted);
     #endif
          }

        /// <summary>Logs warning. Visible in Release builds.</summary>
        public static void Warning(LogCategory category, string message)
        {
            string formatted = Format(category, "WARN", message);
            Debug.LogWarning(formatted);
            Write(formatted);
        }

        /// <summary>Logs an error. Always visible, always written to file.</summary>
        public static void Error(LogCategory category, string message, Exception ex = null)
        {
            string formatted = Format(category, "ERROR", message);

            if (ex != null)
            {
                formatted += $"\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            }

            Debug.LogError(formatted);
            Write(formatted);
        }

        /// <summary>Returns the path of the current log file.</summary>
        public static string GetLogFilePath() => LogFilePath;

        private static string Format(LogCategory category, string level, string message)
        {
            return $"[{DateTime.Now:HH:mm:ss}] [{level}] [{category}] {message}";
        }

        private static void Write(string message)
        {
            try
            {
                Init();
                File.AppendAllText(LogFilePath, message + "\n");
            }
            catch
            {
                // Never crash because of logging
            }
        }
    }
}
