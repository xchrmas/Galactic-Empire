

// Editor  Detects potential memory leaks after exiting Play mode.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GalacticEmpire.Editor
{
    /// <summary>Monitors memory after Play mode exits and reports potential leaks.</summary>
    [InitializeOnLoad]
    public static class MemoryLeakDetector
    {
        private static int _gameObjectCountBeforePlay;
        private static readonly List<string> _warnings = new();

        static MemoryLeakDetector()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Snapshot before game starts
                _gameObjectCountBeforePlay = GetActiveGameObjectCount();
                _warnings.Clear();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Check after game stops
                RunLeakChecks();
            }
        }

        [MenuItem("GalacticEmpire/🔍 Run Memory Leak Check")]
        public static void RunLeakChecks()
        {
            _warnings.Clear();

            CheckDontDestroyOnLoadObjects();
            CheckOrphanedGameObjects();
            CheckStaticReferences();

            PrintResults();
        }

        private static void CheckDontDestroyOnLoadObjects()
        {
            // DontDestroyOnLoad objects persist between scenes — check for unexpected ones
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (var obj in allObjects)
            {
                if (obj.scene.name == "DontDestroyOnLoad" && obj.hideFlags == HideFlags.None)
                {
                    _warnings.Add($"⚠️ DontDestroyOnLoad object still alive: '{obj.name}' - was it properly disposed?");
                }
            }
        }

        private static void CheckOrphanedGameObjects()
        {
            int currentCount = GetActiveGameObjectCount();
            int delta = currentCount - _gameObjectCountBeforePlay;

            if (delta > 5)
            {
                _warnings.Add($"⚠️ {delta} more GameObjects after Play than before. Possible leak — check Object Pooling.");
            }
        }

        private static void CheckStaticReferences()
        {
            // Check for common static leak patterns in loaded assemblies
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (!assembly.FullName.StartsWith("GalacticEmpire"))
                {
                    continue;
                }

                // Skip Editor assemblies — they intentionally hold static state
                if (assembly.FullName.Contains("Editor"))
                {
                    continue;
                }

                foreach (var type in assembly.GetTypes())
                {
                    foreach (var field in type.GetFields(
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public))
                    {
                        // Warn about static lists/dictionaries that could hold references
                        if ((field.FieldType.IsGenericType) &&
                            (field.FieldType.GetGenericTypeDefinition() == typeof(List<>) ||
                             field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                        {
                            var value = field.GetValue(null);
                            if (value != null)
                            {
                                _warnings.Add($"⚠️ Static collection '{type.Name}.{field.Name}' may hold references after Play.");
                            }
                        }
                    }
                }
            }
        }


        private static int GetActiveGameObjectCount()
        {
            return Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        }

        private static void PrintResults()
        {
            if (_warnings.Count == 0)
            {
                Debug.Log("✅ [MemoryLeakDetector] No memory leaks detected.");
                return;
            }

            Debug.LogWarning($"[MemoryLeakDetector] {_warnings.Count} potential issue(s) found:");

            foreach (var warning in _warnings)
            {
                Debug.LogWarning($"[MemoryLeakDetector] {warning}");
            }
        }
    }
}
