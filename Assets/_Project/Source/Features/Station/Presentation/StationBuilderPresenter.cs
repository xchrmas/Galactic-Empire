// Provides the link between VContainer and StationGridRenderer's MonoBehaviour dependency injection.
// Also forwards grid cell click events from the renderer to the StationBuilderScreen.
//
// Uses a dedicated orthographic camera for the station builder view, same reasoning
// as GalaxyMapPresenter - keeps the shared Main Camera's SGT scripts and background
// out of the way instead of repositioning it.

using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.Station.Infrastructure;
using GalacticEmpire.Feature.UI.Screens;
using UnityEngine;
using VContainer;

namespace GalacticEmpire.Feature.Station.Presentation
{
    /// <summary>Initializes the station grid renderer and wires cell clicks to the UI.</summary>
    public sealed class StationBuilderPresenter : MonoBehaviour
    {
        [SerializeField] private StationGridRenderer _renderer;
        [SerializeField] private StationBuilderScreen _stationBuilderScreen;

        // Available module configs for building - dragged in via Inspector,
        // same pattern GameBootstrapper uses for repository/config SO references.
        [SerializeField] private StationModuleSO[] _availableModules;

        // 6x6 grid at GridCellSize=2f spans 12 units - this gives comfortable headroom
        [SerializeField] private float _orthographicSize = 10f;

        private Camera _builderCamera;

        // VContainer injects this via method injection
        [Inject]
        public void Construct(IStationService stationService)
        {
            if (_renderer == null)
            {
                Debug.LogError("[StationBuilderPresenter] StationGridRenderer not assigned.");
                return;
            }

            _renderer.Initialize(stationService);

            if (_stationBuilderScreen == null)
            {
                Debug.LogError("[StationBuilderPresenter] StationBuilderScreen not assigned.");
                return;
            }

            _stationBuilderScreen.Initialize(stationService, _availableModules);
            _renderer.OnCellSelected += _stationBuilderScreen.ShowCellInfo;
            _stationBuilderScreen.OnStationChanged += _renderer.Render;

            _stationBuilderScreen.OnShown += HandleStationBuilderShown;
            _stationBuilderScreen.OnHidden += HandleStationBuilderHidden;
        }

        private void HandleStationBuilderShown()
        {
            if (_builderCamera != null)
                return;

            var go = new GameObject("StationBuilderCamera");
            go.transform.SetParent(_renderer.transform, false);

            _builderCamera = go.AddComponent<Camera>();
            _builderCamera.orthographic = true;
            _builderCamera.clearFlags = CameraClearFlags.SolidColor;
            _builderCamera.backgroundColor = Color.black;
            _builderCamera.depth = 100f; // renders on top of the main camera
            _builderCamera.cullingMask = ~0; // sees everything on the default layer, including cells

            go.transform.localPosition = new Vector3(0f, 0f, -50f);
            _builderCamera.orthographicSize = _orthographicSize;
        }

        private void HandleStationBuilderHidden()
        {
            if (_builderCamera == null)
                return;

            Destroy(_builderCamera.gameObject);
            _builderCamera = null;
        }

        private void OnDestroy()
        {
            if (_renderer != null && _stationBuilderScreen != null)
            {
                _renderer.OnCellSelected -= _stationBuilderScreen.ShowCellInfo;
                _stationBuilderScreen.OnStationChanged -= _renderer.Render;
            }

            if (_stationBuilderScreen != null)
            {
                _stationBuilderScreen.OnShown -= HandleStationBuilderShown;
                _stationBuilderScreen.OnHidden -= HandleStationBuilderHidden;
            }

            if (_builderCamera != null)
                Destroy(_builderCamera.gameObject);
        }
    }
}
