
// Real-time FPS and memory overlay for development builds.
using GalacticEmpire.Core;
using UnityEngine;

namespace GalacticEmpire.Presentation
{
    /// <summary>Shows FPS, frame time, and memory usage overlay. Dev builds only.</summary>
    public sealed class FPSOverlay : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;

        private float _deltaTime;
        private float _updateInterval = 0.5f;
        private float _elapsed;
        private float _fps;
        private float _frameMs;

        private GUIStyle _style;

        private void Awake()
        {
            // Auto-disable in Release builds
     #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
            return;
#endif
            DontDestroyOnLoad(gameObject);
            GELogger.Info(LogCategory.System, "FPSOverlay initialized.");
        }

        private void Update()
        {
            if (_config && !_config.ShowFPSOverlay)
            {
                enabled = false;
                return;
            }

            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            _elapsed += Time.unscaledDeltaTime;

            if (!(_elapsed >= _updateInterval))
            {
                return;
            }

            _fps     = 1.0f / _deltaTime;
            _frameMs = _deltaTime * 1000f;
            _elapsed = 0f;
        }

        private void OnGUI()
        {
         #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_style == null)
            {
                InitStyle();
            }

            float memoryMb = System.GC.GetTotalMemory(false) / 1048576f;

            string text = $"FPS: {_fps:0}\n" +
                          $"Frame: {_frameMs:0.0}ms\n" +
                          $"Memory: {memoryMb:0.0} MB";

            // Background
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(8, 8, 140, 58), Texture2D.whiteTexture);

            // Text
            GUI.color = GetFPSColor(_fps);
            GUI.Label(new Rect(12, 10, 140, 60), text, _style);
            GUI.color = Color.white;
#endif
        }

        private void InitStyle()
        {
            _style = new GUIStyle
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
        }

        private static Color GetFPSColor(float fps)
        {
            // Green = good, yellow = ok, red = bad
            if (fps >= 60f) return Color.green;
            if (fps >= 30f) return Color.yellow;
            return Color.red;
        }
    }
}
