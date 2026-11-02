//Core Central game configuration asset

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

        [Header("Exploration")]
        [Tooltip("Travel time in seconds per hop (graph edge) between sectors, divided by fleet Speed.")]
        public float BaseSecondsPerHop = 10f;

        [Tooltip("Resources granted per point of sector ResourceRichness (0-1) on successful exploration.")]
        public float ExplorationRewardPerRichness = 100f;

        [Tooltip("Time in seconds between exploration mission simulation ticks.")]
        public float ExplorationTickRate = 1f;

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

        [Tooltip("Grid size - number of cells per row and column.")]
        public int StationGridSize = 6;

        [Tooltip("Size of each grid cell in world units.")]
        public float GridCellSize = 2f;

        [Header("Battle")]
        [Tooltip("Time in seconds between battle simulation ticks.")]
        public float BattleTickRate = 0.05f;

        [Tooltip("Maximum engagement range between ships in world units.")]
        public float MaxEngagementRange = 50f;

        [Tooltip("Global damage multiplier - use for difficulty scaling.")]
        public float DamageMultiplier = 1f;

        [Header("Enemy Fleets")]
        [Tooltip("Minimum sector ThreatLevel (0-1) required for an NPC garrison to spawn.")]
        public float EnemyEncounterThreatThreshold = 0.5f;

        [Tooltip("Minimum number of ships in a generated NPC garrison.")]
        public int EnemyMinShipsPerSector = 1;

        [Tooltip("Maximum number of ships in a generated NPC garrison.")]
        public int EnemyMaxShipsPerSector = 4;

        [Tooltip("Base hull for an NPC ship at ThreatLevel 0. Tuned (2026-09-24 balance " +
            "session) via simulation against the 1-ship, 50-hull/25-damage player starting " +
            "fleet so a typical encounter is close to a coinflip rather than a guaranteed " +
            "win or loss - see battle.md for the simulated win rates.")]
        public float EnemyBaseHull = 350f;

        [Tooltip("Additional hull granted at ThreatLevel 1, scaled linearly. Tuned " +
            "alongside EnemyBaseHull (2026-09-24 balance session) - see battle.md.")]
        public float EnemyThreatHullScale = 100f;

        [Tooltip("Base per-ship damage for an NPC ship at ThreatLevel 0. Tuned " +
            "(2026-09-24 balance session) against the 1-ship, 50-hull/25-damage player " +
            "starting fleet under the round-robin combat model in BattleEntity.SimulateTick.")]
        public float EnemyBaseDamage = 0.9f;

        [Tooltip("Per-ship damage change per point of ThreatLevel, scaled linearly. " +
            "NEGATIVE by design (2026-09-24 balance session): higher ThreatLevel spawns " +
            "MORE NPC ships (see EnemyMinShipsPerSector/EnemyMaxShipsPerSector), and " +
            "against a lone-ship player fleet every NPC ship's damage stacks onto that " +
            "one target each tick (round-robin only spreads damage when the RECEIVING " +
            "side has multiple ships). A negative scale keeps the NPC fleet's total " +
            "damage-per-tick roughly stable as ship count grows with threat, instead of " +
            "compounding both factors and one-shotting the player. See battle.md for the " +
            "worked numbers (targets ~30-50 ticks for a 1-ship player to lose to a typical " +
            "garrison across ThreatLevel 0.5-1.0).")]
        public float EnemyThreatDamageScale = -0.6f;

        [Header("Enemy Ship Roles (BACKLOG-27)")]
        [Tooltip("Hull multiplier for a Heavy-role NPC ship, applied on top of the " +
            "Base/ThreatScale formula above - NOT a new absolute stat. Paired with " +
            "EnemyHeavyDamageMultiplier so a Heavy ship's hull*damage (its CombatPower " +
            "contribution) stays close to a plain Raider's, keeping garrison total power " +
            "roughly unchanged from before role variety existed - only its distribution " +
            "across ships changes. A garrison with any ships above ThreatLevel 0.5 no " +
            "longer being all-identical Raiders.")]
        public float EnemyHeavyHullMultiplier = 1.5f;

        [Tooltip("Damage multiplier for a Heavy-role NPC ship - see EnemyHeavyHullMultiplier.")]
        public float EnemyHeavyDamageMultiplier = 0.67f;

        [Tooltip("Hull multiplier for a Striker-role NPC ship - the inverse trade-off from " +
            "Heavy (less hull, more damage per shot). See EnemyHeavyHullMultiplier.")]
        public float EnemyStrikerHullMultiplier = 0.67f;

        [Tooltip("Damage multiplier for a Striker-role NPC ship - see EnemyStrikerHullMultiplier.")]
        public float EnemyStrikerDamageMultiplier = 1.5f;

        [Header("Debug")]
        [Tooltip("Enable verbose logging in development builds.")]
        public bool VerboseLogging = true;

        [Tooltip("Show FPS overlay in development builds.")]
        public bool ShowFPSOverlay = true;

        [Tooltip("Show debug gizmos in scene view.")]
        public bool ShowDebugGizmos = true;
    }
}
