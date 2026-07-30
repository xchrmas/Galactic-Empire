// Handles all galaxy use cases - discovery, claiming, navigation.
// Generation is delegated to GalaxyGeneratorService.

using System;
using System.Collections.Generic;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Galaxy.Domain;

namespace GalacticEmpire.Feature.Galaxy.Application
{
    /// <summary>Executes galaxy use cases - explore, claim, navigate.</summary>
    public sealed class GalaxyService : IGalaxyService
    {
        private readonly IGalaxyRepository  _galaxyRepository;
        private readonly GalaxyGeneratorService  _generator;
        private readonly GameConfigSO  _config;

        public GalaxyService(
            IGalaxyRepository galaxyRepository,
            GalaxyGeneratorService generator,
            GameConfigSO config)
        {
            _galaxyRepository = galaxyRepository;
            _generator = generator;
            _config = config;
        }

        /// <summary>Returns the current galaxy map.</summary>
        public GalaxyMapEntity GetGalaxy()
        {
            return _galaxyRepository.Get();
        }

        /// <summary>Generates a fresh galaxy - only called on first launch.</summary>
        public GalaxyMapEntity GenerateGalaxy(int sectorCount)
        {
            var galaxy = _generator.Generate(sectorCount);
            _galaxyRepository.Save(galaxy);

            GELogger.Info(LogCategory.System,
                $"Galaxy generated: {galaxy.TotalSectors} sectors.");

            return galaxy;
        }

        /// <summary>Fleet entered a sector - reveal it on the map.</summary>
        public SectorEntity DiscoverSector(Guid sectorId)
        {
            var galaxy  = GetGalaxyOrThrow();
            var sector  = GetSectorOrThrow(galaxy, sectorId);
            var updated = sector.Discover();

            _galaxyRepository.Save(galaxy.UpdateSector(updated));

            GELogger.Info(LogCategory.System, $"Sector discovered: {updated.Name} ({updated.Type})");
            return updated;
        }

        /// <summary>Player conquered a sector.</summary>
        public SectorEntity ClaimSector(Guid sectorId)
        {
            var galaxy  = GetGalaxyOrThrow();
            var sector  = GetSectorOrThrow(galaxy, sectorId);
            var updated = sector.Claim();

            _galaxyRepository.Save(galaxy.UpdateSector(updated));

            GELogger.Info(LogCategory.System, $"Sector claimed: {updated.Name}");
            return updated;
        }

        /// <summary>Player lost a sector to enemy forces.</summary>
        public SectorEntity LoseSector(Guid sectorId)
        {
            var galaxy  = GetGalaxyOrThrow();
            var sector  = GetSectorOrThrow(galaxy, sectorId);
            var updated = sector.Lose();

            _galaxyRepository.Save(galaxy.UpdateSector(updated));

            GELogger.Info(LogCategory.System, $"Sector lost: {updated.Name}");
            return updated;
        }

        /// <summary>Returns all sectors directly reachable from a given sector.</summary>
        public IReadOnlyList<SectorEntity> GetReachableSectors(Guid sectorId)
        {
            var galaxy = GetGalaxyOrThrow();
            return galaxy.GetNeighbours(sectorId);
        }

        private GalaxyMapEntity GetGalaxyOrThrow()
        {
            var galaxy = _galaxyRepository.Get();
            if (galaxy == null)
                throw new InvalidOperationException("Galaxy not generated yet.");
            return galaxy;
        }

        private static SectorEntity GetSectorOrThrow(GalaxyMapEntity galaxy, Guid sectorId)
        {
            var sector = galaxy.GetSector(sectorId);
            if (sector == null)
                throw new InvalidOperationException($"Sector {sectorId} not found.");
            return sector;
        }
    }
}
