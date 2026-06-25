//Core Central game configuration asset.



using UnityEngine;

namespace GalacticEmpire.Core
{
    /// <summary>Central ScriptableObject for all game constants and tunable values.</summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "GalacticEmpire/Game Config")]
    public sealed class GameConfigSO : ScriptableObject
    {
        [Header("Fleet")]
        [Tooltip("Maximum number of ships allowed in a single fleet.")]
        public int MaxFleetSize = 50;

        [Tooltip("Default ship movement speed in units per second.")]
        public float DefaultShipSpeed = 10f;

        [Tooltip("Time in seconds between fleet simulation ticks.")]
        public float FleetTickRate = 0.1f;

        [Header("Economy")]
        [Tooltip("Starting metal for a new empire.")]
        public float StartingMetal = 500f;

        [Tooltip("Starting energy for a new empire.")]
        public float StartingEnergy = 300f;

        [Tooltip("Starting crystals for a new empire.")]
        public float StartingCrystals = 50f;

        [Tooltip("Base resource production rate per station module per second.")]
        public float BaseProductionRate = 1f;

        [Header("Station")]
        [Tooltip("Maximum number of modules a station can have.")]
        public int MaxStationModules = 20;

        [Tooltip("Grid size — number of cells per row and column.")]
        public int StationGridSize = 6;

        [Tooltip("Size of each grid cell in world units.")]
        public float GridCellSize = 2f;

        [Header("Battle")]
        [Tooltip("Time in seconds between battle simulation ticks.")]
        public float BattleTickRate = 0.05f;

        [Tooltip("Maximum engagement range between ships in world units.")]
        public float MaxEngagementRange = 50f;

        [Tooltip("Global damage multiplier — use for difficulty scaling.")]
        public float DamageMultiplier = 1f;

        [Header("Debug")]
        [Tooltip("Enable verbose logging in development builds.")]
        public bool VerboseLogging = true;

        [Tooltip("Show FPS overlay in development builds.")]
        public bool ShowFPSOverlay = true;

        [Tooltip("Show debug gizmos in scene view.")]
        public bool ShowDebugGizmos = true;
    }
}
