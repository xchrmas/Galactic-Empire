// Generates an NPC fleet for a hostile sector from its ThreatLevel.
// Deterministic per sector (seeded by SectorId) so the same sector always
// rolls the same garrison strength - only the number of ships varies within
// a small deterministic band, not the whole fleet composition.
//
// Doesn't use ShipBlueprintSO - that would tie NPC generation to Inspector-
// assigned content assets (the pattern StationModuleSO/ship blueprints use
// for player-facing content). NPC ships are plain stat rolls off ThreatLevel,
// built directly via ShipEntity.Create, no content authoring required.

using System;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Domain;

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
                float hull = _config.EnemyBaseHull + threatLevel * _config.EnemyThreatHullScale;
                float damage = _config.EnemyBaseDamage + threatLevel * _config.EnemyThreatDamageScale;

                ships[i] = ShipEntity.Create(
                    name: $"Raider {i + 1}",
                    maxHull: hull,
                    damage: damage,
                    speed: _config.DefaultShipSpeed);
            }

            GELogger.Info(LogCategory.Battle,
                $"Generated enemy garrison for sector {sectorId}: {shipCount} ship(s) at threat {threatLevel:F2}.");

            return EnemyFleetEntity.Create("Raider Fleet", sectorId, Array.AsReadOnly(ships));
        }
    }
}
