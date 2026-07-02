// Layer: Infrastructure | ScriptableObject implementation of IStationRepository.
// Stores the player's station state as a Unity asset.

using GalacticEmpire.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GalacticEmpire.Infrastructure
{
    /// <summary>Stores and manages the player's station as a ScriptableObject asset.</summary>
    [CreateAssetMenu(fileName = "StationRepository", menuName = "GalacticEmpire/Station/Station Repository")]
    public sealed class StationRepositorySO : SerializedScriptableObject, IStationRepository
    {
        [TitleGroup("Station State")]
        [ReadOnly, ShowInInspector]
        private StationEntity _station;

        [TitleGroup("Station State")]
        [ReadOnly, LabelText("Has Station")]
        [ShowInInspector]
        public bool HasStation() => _station != null;

        /// <summary>Returns the current station state.</summary>
        public StationEntity Get() => _station;

        /// <summary>Saves the current station state.</summary>
        public void Save(StationEntity station)
        {
            if (station == null)
            {
                GELogger.Warning(LogCategory.Station, "Attempted to save null station.");
                return;
            }

            _station = station;
            GELogger.Info(LogCategory.Station, $"Station saved: {station.Name} — {station.TotalModules} modules.");
        }

        [TitleGroup("Debug")]
        [Button("Create Default Station", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
        private void CreateDefaultStation()
        {
            _station = StationEntity.Create("Galactic Empire HQ", gridSize: 6);
            GELogger.Info(LogCategory.Station, $"Default station created: {_station.Name}");
        }

        [TitleGroup("Debug")]
        [Button("Reset Station", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        [ShowIf("HasStation")]
        private void ResetStation()
        {
            _station = null;
            GELogger.Warning(LogCategory.Station, "Station reset.");
        }
    }
}
