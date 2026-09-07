// Provides the link between VContainer and GalaxyMapRenderer's MonoBehavior dependency injection.
// Also forwards sector click events from the renderer to the GalaxyMapScreen.
//
// Uses a dedicated orthographic camera for the galaxy map view instead of moving
// the shared Main Camera - the main camera carries SGT free-look scripts and a
// huge (scale 1000) decorative background object that kept interfering with any
// position/rotation set on it. A separate camera sidesteps all of that: it only
// renders the sector layer, is created fresh each time, and is destroyed on hide.

using GalacticEmpire.Feature.Fleet.Application;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.UI.Screens;
using UnityEngine;
using VContainer;


namespace GalacticEmpire.Feature.Galaxy.Presentation
{
    /// <summary>Initializes the galaxy map renderer and wires sector clicks to the UI.</summary>
    public sealed class GalaxyMapPresenter : MonoBehaviour
    {
        [SerializeField] private GalaxyMapRenderer _renderer;
        [SerializeField] private GalaxyMapScreen _galaxyMapScreen;

        // How far the orthographic camera can see each direction from center -
        // sectors spread up to 100 units from origin (see GalaxyGeneratorService),
        // so this needs comfortable headroom around that.
        [SerializeField] private float _orthographicSize = 50f;
        [SerializeField] private float _framingPadding = 3f;

        private Camera _mapCamera;

        // VContainer injects this via method injection
        [Inject]
        public void Construct(IGalaxyService galaxyService, IFleetService fleetService)
        {
            if (_renderer == null)
            {
                Debug.LogError("[GalaxyMapPresenter] GalaxyMapRenderer not assigned.");
                return;
            }

            _renderer.Initialize(galaxyService);

            if (_galaxyMapScreen == null)
            {
                Debug.LogError("[GalaxyMapPresenter] GalaxyMapScreen not assigned.");
                return;
            }

            _galaxyMapScreen.Initialize(fleetService);
            _renderer.OnSectorSelected += _galaxyMapScreen.ShowSectorInfo;

            _galaxyMapScreen.OnShown += HandleGalaxyMapShown;
            _galaxyMapScreen.OnHidden += HandleGalaxyMapHidden;
        }

        private void HandleGalaxyMapShown()
        {
            if (_mapCamera != null)
                return;

            var go = new GameObject("GalaxyMapCamera");
            go.transform.SetParent(_renderer.transform, false);

            _mapCamera = go.AddComponent<Camera>();
            _mapCamera.orthographic = true;
            _mapCamera.clearFlags = CameraClearFlags.SolidColor;
            _mapCamera.backgroundColor = Color.black;
            _mapCamera.depth = 100f; // renders on top of the main camera
            _mapCamera.cullingMask = ~0; // sees everything on the default layer, including sectors

            FrameActiveSectors(go.transform);
        }

        private void FrameActiveSectors(Transform cameraTransform)
        {
            // Auto-frame around whatever sectors are actually visible right now,
            // instead of guessing a fixed camera distance/size - the sector count
            // and spread depend on how much of the galaxy has been discovered.
            if (_renderer.TryGetActiveSectorsBounds(out var center, out var radius))
            {
                cameraTransform.localPosition = center + new Vector3(0f, 0f, -50f);
                _mapCamera.orthographicSize = radius + _framingPadding;
            }
            else
            {
                cameraTransform.localPosition = new Vector3(0f, 0f, -50f);
                _mapCamera.orthographicSize = _orthographicSize;
            }
        }

        private void HandleGalaxyMapHidden()
        {
            if (_mapCamera == null)
                return;

            Destroy(_mapCamera.gameObject);
            _mapCamera = null;
        }

        private void OnDestroy()
        {
            if (_renderer != null && _galaxyMapScreen != null)
                _renderer.OnSectorSelected -= _galaxyMapScreen.ShowSectorInfo;

            if (_galaxyMapScreen != null)
            {
                _galaxyMapScreen.OnShown -= HandleGalaxyMapShown;
                _galaxyMapScreen.OnHidden -= HandleGalaxyMapHidden;
            }

            if (_mapCamera != null)
                Destroy(_mapCamera.gameObject);
        }
    }
}
