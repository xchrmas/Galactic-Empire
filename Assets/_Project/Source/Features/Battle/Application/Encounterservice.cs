// Lazily generates and remembers NPC garrisons per sector.
// First visit to a sector above the threat threshold rolls a garrison via
// EnemyFleetGeneratorService and remembers it in ISectorGarrisonRepository.
// Every later visit returns the same remembered result - a cleared sector
// stays clear, an unbeaten garrison stays the same fleet, until the process
// restarts (garrisons are in-memory only, see SectorGarrisonRepository).

using System;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Battle.Domain;
using GalacticEmpire.Feature.Galaxy.Application;

namespace GalacticEmpire.Feature.Battle.Application
{
    /// <summary>Checks sectors for NPC garrisons, generating them lazily on first visit.</summary>
    public sealed class EncounterService : IEncounterService
    {
        private readonly ISectorGarrisonRepository _garrisonRepository;
        private readonly EnemyFleetGeneratorService _generator;
        private readonly IGalaxyService _galaxyService;
        private readonly GameConfigSO _config;

        public EncounterService(
            ISectorGarrisonRepository garrisonRepository,
            EnemyFleetGeneratorService generator,
            IGalaxyService galaxyService,
            GameConfigSO config)
        {
            _garrisonRepository = garrisonRepository;
            _generator = generator;
            _galaxyService = galaxyService;
            _config = config;
        }

        /// <summary>Returns the sector's NPC garrison, generating one on first visit if hostile enough.</summary>
        public EnemyFleetEntity CheckForEncounter(Guid sectorId)
        {
            // Already visited - return whatever is remembered (garrison or cleared)
            if (_garrisonRepository.HasBeenGenerated(sectorId))
                return _garrisonRepository.Get(sectorId);

            var galaxy = _galaxyService.GetGalaxy();
            var sector = galaxy?.GetSector(sectorId);

            if (sector == null || sector.ThreatLevel < _config.EnemyEncounterThreatThreshold)
            {
                // Sector too safe to have a garrison - remember it as generated
                // (empty) so we never re-roll it on a later visit.
                _garrisonRepository.Clear(sectorId);
                return null;
            }

            var enemyFleet = _generator.Generate(sectorId, sector.ThreatLevel);
            _garrisonRepository.Set(sectorId, enemyFleet);

            GELogger.Info(LogCategory.Battle,
                $"Encounter triggered in sector {sector.Name}: {enemyFleet.ShipCount} enemy ship(s).");

            return enemyFleet;
        }

        /// <summary>Marks the sector's garrison as cleared after the player wins.</summary>
        public void ResolveEncounter(Guid sectorId)
        {
            _garrisonRepository.Clear(sectorId);
            GELogger.Info(LogCategory.Battle, $"Sector {sectorId} garrison cleared.");
        }

        /// <summary>
        /// Read-only - true if the sector has no live, unbeaten garrison
        /// (Stage 1d, Claiming). Deliberately does NOT call CheckForEncounter
        /// or touch EnemyFleetGeneratorService - that method generates a
        /// garrison as a side effect on first visit, which a Claim-button
        /// availability check must never trigger. A sector that has never
        /// been visited (HasBeenGenerated false) is treated as clear: by the
        /// time a sector is IsDiscovered (Claim's own precondition in
        /// SectorEntity.Claim), ExplorationService.ResolveArrival has
        /// already called the encounter check once for it via
        /// GameEntryPoint's wiring, so HasBeenGenerated is normally already
        /// true for any discovered sector that could ever have had a
        /// garrison. The false branch mainly covers a safe/low-threat
        /// sector, which legitimately never gets a garrison at all.
        /// </summary>
        public bool IsSectorClear(Guid sectorId)
        {
            if (!_garrisonRepository.HasBeenGenerated(sectorId))
                return true;

            return _garrisonRepository.Get(sectorId) == null;
        }
    }
}
