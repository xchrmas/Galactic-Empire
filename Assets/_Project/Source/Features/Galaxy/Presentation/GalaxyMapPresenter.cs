// Provides the link between VContainer and GalaxyMapRenderer's MonoBehavior dependency injection.
// Also forwards sector click events from the renderer to the GalaxyMapScreen.

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
        }

        private void OnDestroy()
        {
            if (_renderer != null && _galaxyMapScreen != null)
                _renderer.OnSectorSelected -= _galaxyMapScreen.ShowSectorInfo;
        }
    }
}
