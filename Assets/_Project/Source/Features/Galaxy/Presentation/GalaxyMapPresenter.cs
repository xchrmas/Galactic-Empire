// Provides the link between VContainer and GalaxyMapRenderer's MonoBehavior dependency injection.

using GalacticEmpire.Feature.Galaxy.Application;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GalacticEmpire.Feature.Galaxy.Presentation
{
    /// <summary>Initializes the galaxy map renderer with injected dependencies.</summary>
    public sealed class GalaxyMapPresenter : MonoBehaviour
    {
        [SerializeField] private GalaxyMapRenderer _renderer;

        // VContainer injects this via method injection
        [Inject]
        public void Construct(IGalaxyService galaxyService)
        {
            if (_renderer == null)
            {
                Debug.LogError("[GalaxyMapPresenter] GalaxyMapRenderer not assigned.");
                return;
            }

            _renderer.Initialize(galaxyService);
        }
    }
}
