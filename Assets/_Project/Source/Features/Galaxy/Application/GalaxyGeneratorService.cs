// Procedurally generates the galaxy using Poisson Disk Sampling for
// natural-looking sector placement and proximity-based graph connections.

using System.Collections.Generic;
using System.Linq;
using GalacticEmpire.Core;
using GalacticEmpire.Feature.Galaxy.Domain;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GalacticEmpire.Feature.Galaxy.Application
{
    /// <summary>Generates a procedural galaxy with natural sector distribution.</summary>
    public sealed class GalaxyGeneratorService
    {
        private readonly GameConfigSO _config;

        private const float GalaxyRadius = 100f;
        private const float MinDistance = 12f;
        private const float MaxDistance = 30f;
        private const int   MaxNeighbours = 4;
        private const int   MaxTries = 30;


        public GalaxyGeneratorService(GameConfigSO config)
        {
            _config = config;
        }


        /// <summary>Generates a galaxy with the given number of sectors.</summary>
        public GalaxyMapEntity Generate(int sectorCount)
        {
            var positions = GeneratePositions(sectorCount);
            var sectors   = CreateSectors(positions);

            sectors  = ConnectNeighbours(sectors);

            return GalaxyMapEntity.CreateFromSectors(sectors.AsReadOnly());
        }

        // Poisson Disk Sampling - keeps sectors naturally spaced
        private static List<Vector2> GeneratePositions(int count)
        {
            var positions = new List<Vector2>();
            int tries  = 0;

            while (positions.Count < count && tries < count * MaxTries)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(0f, GalaxyRadius);

                var candidate = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

                    bool tooClose = positions.Any(p =>
                    Vector2.Distance(p, candidate) < MinDistance);



                if (!tooClose)
                {
                    positions.Add(candidate);
                }

                tries++;
            }

            return positions;
        }

        // Sector type based on distance from center - further = more dangerous
        private static List<SectorEntity> CreateSectors(List<Vector2> positions)
        {
            var sectors = new List<SectorEntity>();

            for (int i = 0; i < positions.Count; i++)
            {
                float dist = positions[i].magnitude / GalaxyRadius;
                var type = GetSectorType(dist);


                var sector = SectorEntity.Create(
                    name:  GenerateName(i),
                    type: type,

                    position: positions[i],
                    resourceRichness: GetRichness(type),
                    threatLevel: GetThreat(type));

                // Sector closest to center = player home base
                if (i == 0)
                {
                    sector = sector.Discover().Claim();
                }

                sectors.Add(sector);
            }

            return sectors;
        }

        // Connect each sector to nearest neighbours within MaxDistance
        private static List<SectorEntity> ConnectNeighbours(List<SectorEntity> sectors)
        {
            var connected = new List<SectorEntity>();

            foreach (var sector in sectors)
            {
                var neighbours = sectors
                    .Where(s => s.Id != sector.Id)
                    .Where(s => Vector2.Distance(sector.Position, s.Position) <= MaxDistance)
                    .OrderBy(s => Vector2.Distance(sector.Position, s.Position))
                    .Take(MaxNeighbours)
                    .Select(s => s.Id)
                    .ToList()
                    .AsReadOnly();

                connected.Add(sector.WithNeighbours(neighbours));
            }

            return connected;
        }

        private static SectorType GetSectorType(float dist)
        {
            if (dist < 0.15f) return SectorType.Safe;
            if (dist < 0.40f) return SectorType.Contested;
            if (dist < 0.65f) return SectorType.Hostile;
            if (dist < 0.80f) return Random.value > 0.7f ? SectorType.Nebula : SectorType.Hostile;
            if (dist < 0.95f) return SectorType.Unknown;

            return Random.value > 0.8f ? SectorType.BlackHole : SectorType.Unknown;
        }

        private static float GetRichness(SectorType type) => type switch
        {
            SectorType.Safe => Random.Range(0.3f, 0.5f),
            SectorType.Contested => Random.Range(0.4f, 0.7f),
            SectorType.Hostile   => Random.Range(0.6f, 0.9f),
            SectorType.Nebula    => Random.Range(0.5f, 0.8f),
            SectorType.Unknown   => Random.Range(0.7f, 1.0f),
            SectorType.BlackHole => 1.0f, _ => 0.5f
        };

        private static float GetThreat(SectorType type) => type switch
        {
            SectorType.Safe => 0f,
            SectorType.Contested => Random.Range(0.2f, 0.5f),
            SectorType.Hostile   => Random.Range(0.6f, 0.9f),
            SectorType.Nebula    => Random.Range(0.3f, 0.6f),
            SectorType.Unknown   => Random.Range(0.5f, 0.8f),
            SectorType.BlackHole => 1.0f, _ => 0.5f
        };

        private static string GenerateName(int index)
        {
            string[] prefixes = { "Alpha", "Beta", "Gamma", "Delta", "Epsilon",
                                  "Zeta", "Eta", "Theta", "Iota", "Kappa" };

            string[] suffixes = { "Prime", "Major", "Minor", "Rex", "Nova",
                                  "Vega", "Cygni", "Draconis", "Orionis", "Centauri" };


            string prefix = prefixes[index % prefixes.Length];
            string suffix = suffixes[index / prefixes.Length % suffixes.Length];
            return $"{prefix} {suffix}";
        }
    }
}
