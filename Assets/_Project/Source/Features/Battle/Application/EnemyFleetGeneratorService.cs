// Generates an NPC fleet for a hostile sector from its ThreatLevel.
// Deterministic per sector (seeded by SectorId) so the same sector always
// rolls the same garrison strength - only the number of ships varies within
// a small deterministic band, not the whole fleet composition.
//
// Doesn't use ShipBlueprintSO - that would tie NPC generation to Inspector-
// assigned content assets (the pattern StationModuleSO/ship blueprints use
// for player-facing content). NPC ships are plain stat rolls off ThreatLevel,
// built directly via ShipEntity.Create, no content authoring required.
//
// Role variety (BACKLOG-27, 2026-10-15): a garrison's ships are no longer
// all identical "Raider N" stat lines. Each ship rolls an EnemyShipRole
// (Raider/Heavy/Striker) from the SAME seeded Random used for ship count,
// so a given sector's garrison composition stays deterministic too. Role
// multipliers (GameConfigSO.EnemyHeavy*/EnemyStriker*) are applied ON TOP
// of the existing Base+ThreatScale formula rather than replacing it, and
// are chosen so hull*damage per ship stays close to a plain Raider's -
// this changes HOW a garrison's power is distributed across its ships, not
// how much total power a garrison has at a given ThreatLevel, preserving
// the 2026-09-24 balance session's tuning (see EnemyBaseHull etc. below).
//
// A single-ship garrison always rolls Raider - the player's 1-ship starting
// fleet is the most common early encounter and the balance session tuned
// specifically against a Raider in that case, so this keeps that exact
// matchup byte-for-byte unchanged. Role variety only shows up once a
// garrison has 2+ ships, which already only happens at higher ThreatLevel
// (see EnemyMinShipsPerSector/EnemyMaxShipsPerSector below).
//
// 2026-10-21 (Stage 1c): added "using GalacticEmpire.Feature.Fleet.Domain;"
// - ShipEntity migrated there from GalacticEmpire.Core. ShipEntity.Create(...)
// call below is unchanged (Stage 1c only adds the new CreateWithSlots
// factory alongside it). NPC ships generated here still have zero slots
// (empty Slots list, the default) - intentional, matches this file's
// existing "Doesn't use ShipBlueprintSO" design note: NPC ships stay plain
// stat rolls, no module system needed for them.

using System;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Domain;
using GalacticEmpire.Feature.Fleet.Domain;

namespace GalacticEmpire.Feature.Battle.Application
{
    /// <summary>Builds NPC fleets scaled to a sector's threat level.</summary>
    public sealed class EnemyFleetGeneratorService
    {
        private readonly GameConfigSO _config;

        public EnemyFleetGeneratorService(GameConfigSO config)
        {
            _config = config;
        }

        /// <summary>Generates a deterministic NPC fleet for the given sector and threat level.</summary>
        public EnemyFleetEntity Generate(Guid sectorId, float threatLevel)
        {
            // Deterministic seed - same sector always rolls the same garrison
            var random = new Random(sectorId.GetHashCode());

            int baseCount = Math.Clamp(
                (int)MathF.Round(_config.EnemyMinShipsPerSector +
                    threatLevel * (_config.EnemyMaxShipsPerSector - _config.EnemyMinShipsPerSector)),
                _config.EnemyMinShipsPerSector,
                _config.EnemyMaxShipsPerSector);

            // Small deterministic variance around the threat-scaled base count -
            // same sector always rolls the same number, but not every sector at
            // the same threat level looks identical.
            int shipCount = Math.Clamp(
                baseCount + random.Next(-1, 2),
                _config.EnemyMinShipsPerSector,
                _config.EnemyMaxShipsPerSector);

            var ships = new ShipEntity[shipCount];
            for (int i = 0; i < shipCount; i++)
            {
                var role = RollRole(shipCount, threatLevel, random);
                var (hullMultiplier, damageMultiplier) = GetRoleMultipliers(role);

                float hull = (_config.EnemyBaseHull + threatLevel * _config.EnemyThreatHullScale) * hullMultiplier;
                float damage = (_config.EnemyBaseDamage + threatLevel * _config.EnemyThreatDamageScale) * damageMultiplier;

                ships[i] = ShipEntity.Create(
                    name: $"{role} {i + 1}",
                    maxHull: hull,
                    damage: damage,
                    speed: _config.DefaultShipSpeed);
            }

            GELogger.Info(LogCategory.Battle,
                $"Generated enemy garrison for sector {sectorId}: {shipCount} ship(s) at threat {threatLevel:F2}.");

            return EnemyFleetEntity.Create("Raider Fleet", sectorId, Array.AsReadOnly(ships));
        }

        // Single-ship garrisons always stay Raider (see the class comment above
        // for why - preserves the balance-tuned 1v1 matchup exactly). Larger
        // garrisons lean more Raider at low ThreatLevel and pick up a growing
        // chance of Heavy/Striker as ThreatLevel rises - higher-threat sectors
        // feel more militarized, not just more numerous.
        private static EnemyShipRole RollRole(int shipCount, float threatLevel, Random random)
        {
            if (shipCount <= 1)
                return EnemyShipRole.Raider;

            float specializedChance = Math.Clamp(threatLevel * 0.6f, 0f, 0.6f);
            float roll = (float)random.NextDouble();

            if (roll >= specializedChance)
                return EnemyShipRole.Raider;

            // Split the remaining chance evenly between Heavy and Striker.
            return (float)random.NextDouble() < 0.5f ? EnemyShipRole.Heavy : EnemyShipRole.Striker;
        }

        private (float hullMultiplier, float damageMultiplier) GetRoleMultipliers(EnemyShipRole role)
        {
            return role switch
            {
                EnemyShipRole.Heavy => (_config.EnemyHeavyHullMultiplier, _config.EnemyHeavyDamageMultiplier),
                EnemyShipRole.Striker => (_config.EnemyStrikerHullMultiplier, _config.EnemyStrikerDamageMultiplier),
                _ => (1f, 1f)
            };
        }
    }
}
