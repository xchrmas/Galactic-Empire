
// Editor Validates scene setup before entering Play mode.
using System.Collections.Generic;
using GalacticEmpire.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GalacticEmpire.Editor
{
    /// <summary>Validates scene configuration — runs automatically before Play or manually via menu.</summary>
    [InitializeOnLoad]
    public static class SceneValidator
    {
        private static readonly List<string> _errors   = new();
        private static readonly List<string> _warnings = new();

        // Auto-validate before entering Play mode
        static SceneValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            bool valid = Validate();

            if (valid)
            {
                return;
            }

            // Stop entering Play mode if critical errors found
            EditorApplication.isPlaying = false;
            SceneValidatorWindow.ShowWindow(_errors, _warnings);
        }

        [MenuItem("GalacticEmpire/✅ Validate Scene")]
        public static bool Validate()
        {
            _errors.Clear();
            _warnings.Clear();

            ValidateGameBootstrapper();
            ValidateNullReferences();
            ValidateRequiredAssets();

            PrintResults();
            return _errors.Count == 0;
        }

        private static void ValidateGameBootstrapper()
        {
            var bootstrapper = Object.FindFirstObjectByType<GameBootstrapper>();

            if (bootstrapper == null)
            {
                _errors.Add("❌ GameBootstrapper not found in scene. Add it to a GameObject.");
                return;
            }

            // Check that FleetRepository is assigned
            // Explicit cast required — SerializedObject accepts UnityEngine.Object
            var serialized = new SerializedObject(bootstrapper as UnityEngine.Object);

            var fleetRepo = serialized.FindProperty("_fleetRepository");
            if (fleetRepo != null && fleetRepo.objectReferenceValue == null)
            {
                _errors.Add("❌ GameBootstrapper: _fleetRepository is not assigned in Inspector.");
            }

            var config = serialized.FindProperty("_config");
            if (config != null && config.objectReferenceValue == null)
            {
                _warnings.Add("⚠️ GameBootstrapper: _config (GameConfigSO) is not assigned.");
            }
        }

        private static void ValidateNullReferences()
        {
            // Find all MonoBehaviours in the scene and check for missing references
            var allObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (var obj in allObjects)
            {
                if (obj == null)
                {
                    _errors.Add($"❌ Missing MonoBehaviour script on GameObject in scene.");
                    continue;
                }

                var serialized = new SerializedObject(obj);
                var property = serialized.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null &&
                        property.objectReferenceInstanceIDValue != 0)
                    {
                        _errors.Add($"❌ Missing reference on {obj.gameObject.name} → {property.displayName}");
                    }
                }
            }
        }

        private static void ValidateRequiredAssets()
        {
            // Check GameConfig asset exists
            var configs = AssetDatabase.FindAssets("t:GameConfigSO");
            if (configs.Length == 0)
            {
                _warnings.Add("⚠️ No GameConfigSO asset found. Create one via GalacticEmpire/Game Config.");
            }

            // Check FleetRepository asset exists
            var repos = AssetDatabase.FindAssets("FleetRepository");
            if (repos.Length == 0)
            {
                _warnings.Add("⚠️ No FleetRepository asset found in project.");
            }
        }

        private static void PrintResults()
        {
            if (_errors.Count == 0 && _warnings.Count == 0)
            {
                Debug.Log("✅ [SceneValidator] Scene is valid. All checks passed.");
                return;
            }

            foreach (var error in _errors)
            {
                Debug.LogError($"[SceneValidator] {error}");
            }

            foreach (var warning in _warnings)
            {
                Debug.LogWarning($"[SceneValidator] {warning}");
            }
        }
    }

    /// <summary>Editor window showing validation results.</summary>
    public class SceneValidatorWindow : EditorWindow
    {
        private List<string> _errors   = new();
        private List<string> _warnings = new();

        private Vector2 _scroll;

        public static void ShowWindow(List<string> errors, List<string> warnings)
        {
            var win = GetWindow<SceneValidatorWindow>("⚠️ Scene Validation");
            win._errors   = errors;
            win._warnings = warnings;

            win.minSize   = new Vector2(400, 300);
            win.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Scene Validation Results", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var error in _errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            foreach (var warning in _warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("🔄 Re-validate", GUILayout.Height(28)))
            {
                SceneValidator.Validate();
            }
        }
    }
}
