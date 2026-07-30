// Persists the galaxy map as a ScriptableObject asset.
// Odin Inspector shows live sector stats during Play mode.

using GalacticEmpire.Core;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Galaxy.Domain;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GalacticEmpire.Feature.Galaxy.Infrastructure
{
    /// <summary>Stores the galaxy map as a Unity asset.</summary>
    [CreateAssetMenu(fileName = "GalaxyRepository",
        menuName = "GalacticEmpire/Galaxy/Galaxy Repository")]
    public sealed class GalaxyRepositorySO : SerializedScriptableObject, IGalaxyRepository
    {
        [TitleGroup("Galaxy State")]
        [ShowInInspector, ReadOnly]
        private GalaxyMapEntity _galaxy;

        [TitleGroup("Galaxy State")]
        [ShowInInspector, ReadOnly, LabelText("Total Sectors")]
        private int TotalSectors => _galaxy?.TotalSectors ?? 0;

        [TitleGroup("Galaxy State")]
        [ShowInInspector, ReadOnly, LabelText("Discovered")]
        private int Discovered => _galaxy?.DiscoveredCount ?? 0;

        [TitleGroup("Galaxy State")]
        [ShowInInspector, ReadOnly, LabelText("Owned")]
        private int Owned => _galaxy?.OwnedCount ?? 0;

        [TitleGroup("Galaxy State")]
        [ShowInInspector, ReadOnly, LabelText("Exploration %")]
        private float ExplorationPct => (_galaxy?.ExplorationProgress ?? 0f) * 100f;

        /// <summary>Returns the current galaxy, null if not generated yet.</summary>
        public GalaxyMapEntity Get() => _galaxy;

        /// <summary>Saves the galaxy after generation or any state change.</summary>
        public void Save(GalaxyMapEntity galaxy)
        {
            _galaxy = galaxy;
            GELogger.Info(LogCategory.System,
                $"Galaxy saved: {galaxy.TotalSectors} sectors, {galaxy.OwnedCount} owned.");
        }

        /// <summary>True if a galaxy has been generated already.</summary>
        public bool HasGalaxy() => _galaxy != null;

        [TitleGroup("Debug")]
        [Button("Reset Galaxy", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        [ShowIf("HasGalaxy")]
        private void ResetGalaxy()
        {
            _galaxy = null;
            GELogger.Warning(LogCategory.System, "Galaxy reset.");
        }
    }
}
