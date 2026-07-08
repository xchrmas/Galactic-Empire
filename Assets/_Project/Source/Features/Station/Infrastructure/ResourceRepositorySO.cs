// A ScriptableObject that stores the empire resource wallet between sessions

using GalacticEmpire.Core;
using GalacticEmpire.Feature.Station.Application;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GalacticEmpire.Feature.Station.Infrastructure
{
    /// <summary>Persists the empire resource wallet as a Unity asset</summary>
    [CreateAssetMenu(fileName = "ResourceRepository", menuName = "GalacticEmpire/Economy/Resource Repository")]
    public sealed class ResourceRepositorySO : SerializedScriptableObject, IResourceRepository
    {

        // Showing this in Inspector so we can watch the economy tick in real time
        [TitleGroup("Economy State")]
        [ShowInInspector, ReadOnly]
        private ResourceWallet _wallet;


        [TitleGroup("Economy State")]
        [ShowInInspector, ReadOnly, LabelText("Metal")]
        private float Metal => _wallet?.Get(ResourceType.Metal) ?? 0f;


        [TitleGroup("Economy State")]
        [ShowInInspector, ReadOnly, LabelText("Energy")]
        private float Energy => _wallet?.Get(ResourceType.Energy) ?? 0f;


        [TitleGroup("Economy State")]
        [ShowInInspector, ReadOnly, LabelText("Crystals")]
        private float Crystals => _wallet?.Get(ResourceType.Crystals) ?? 0f;


        [TitleGroup("Economy State")]
        [ShowInInspector, ReadOnly, LabelText("Dark Matter")]
        private float DarkMatter => _wallet?.Get(ResourceType.DarkMatter) ?? 0f;

        /// <summary>Returns the current wallet-initialises with starting resources on first access</summary>
        public ResourceWallet Get()
        {
            // Lazy init-first time we read, give the player something to work with
            if (_wallet == null)
            {
                _wallet = ResourceWallet.StartingResources();
            }

            return _wallet;
        }

        /// <summary>Saves the wallet after each production tick or spend operation</summary>
        public void Save(ResourceWallet wallet)
        {
            _wallet = wallet;
        }

        [TitleGroup("Debug")]
        [Button("Reset to Starting Resources", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.2f)]
        private void ResetToStartingResources()
        {
            _wallet = ResourceWallet.StartingResources();
            GELogger.Warning(LogCategory.Economy, "Resource wallet reset to starting values.");
        }

        [TitleGroup("Debug")]
        [Button("Clear All Resources", ButtonSizes.Medium), GUIColor(1f, 0.3f, 0.3f)]
        private void ClearResources()
        {
            _wallet = ResourceWallet.Empty();
            GELogger.Warning(LogCategory.Economy, "Resource wallet cleared.");
        }
    }
}
