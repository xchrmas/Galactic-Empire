// Renders all galaxy sectors as interactive nodes on top of the SGT background.
// Each sector gets a GameObject with a sprite - color indicates type and ownership.

using System;
using System.Collections.Generic;
using GalacticEmpire.Feature.Galaxy.Application;
using GalacticEmpire.Feature.Galaxy.Domain;
using UnityEngine;

namespace GalacticEmpire.Feature.Galaxy.Presentation
{
    /// <summary>Spawns and manages visual representations of all galaxy sectors.</summary>
    public sealed class GalaxyMapRenderer : MonoBehaviour
    {
        [Header("Sector Visuals")]
        [SerializeField] private GameObject _sectorPrefab;
        [SerializeField] private float _sectorScale = 3f;

        [Header("Connection Lines")]
        [SerializeField] private Material _connectionMaterial;
        [SerializeField] private float _lineWidth = 0.02f;

        [Header("Sector Colors")]
        [SerializeField] private Color _safeColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _contestedColor = new Color(0.8f, 0.8f, 0.2f);
        [SerializeField] private Color _hostileColor = new Color(0.8f, 0.2f, 0.2f);

        [SerializeField] private Color _unknownColor = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color _nebulaColor = new Color(0.4f, 0.2f, 0.8f);
        [SerializeField] private Color _blackHoleColor = new Color(0.1f, 0.1f, 0.1f);
        [SerializeField] private Color _ownedColor = new Color(0.2f, 0.6f, 1.0f);


        private IGalaxyService _galaxyService;
        private readonly List<GameObject> _sectorObjects = new();
        private readonly List<LineRenderer> _connectionLines = new();

        // Raised when the player clicks a discovered sector
        public event Action<SectorEntity> OnSectorSelected;

        // Call this from GalaxyMapPresenter after VContainer injection
        public void Initialize(IGalaxyService galaxyService)
        {
            _galaxyService = galaxyService;
            Render();
        }

        /// <summary>Clears and redraws all sectors and connections.</summary>
        public void Render()
        {
            Clear();

            var galaxy = _galaxyService.GetGalaxy();
            if (galaxy == null)
                return;

            foreach (var sector in galaxy.Sectors)
                SpawnSector(sector);

            foreach (var sector in galaxy.Sectors)
                DrawConnections(sector, galaxy);
        }

        /// <summary>Center and radius (in local space) of all currently active sectors - used to frame the map camera automatically.</summary>
        public bool TryGetActiveSectorsBounds(out Vector3 center, out float radius)
        {
            center = Vector3.zero;
            radius = 0f;

            var activePositions = new List<Vector3>();
            foreach (var go in _sectorObjects)
            {
                if (go != null && go.activeSelf)
                    activePositions.Add(go.transform.localPosition);
            }

            if (activePositions.Count == 0)
                return false;

            foreach (var pos in activePositions)
                center += pos;
            center /= activePositions.Count;

            foreach (var pos in activePositions)
                radius = Mathf.Max(radius, Vector3.Distance(pos, center));

            // Never zero - a single sector still needs room to be visible
            radius = Mathf.Max(radius, 2f);

            return true;
        }

        private void SpawnSector(SectorEntity sector)
        {
            var go = _sectorPrefab != null
                ? Instantiate(_sectorPrefab, transform)
                : CreateDefaultSectorObject();

            // Position based on sector's 2D world position
            go.transform.localPosition = new Vector3(sector.Position.x, sector.Position.y, 0f);
            go.transform.localScale = Vector3.one * _sectorScale;
            go.name = sector.Name;

            // Color based on type and ownership
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = GetSectorColor(sector);

            // Undiscovered sectors are hidden and can't be clicked yet
            if (sector.IsDiscovered)
                WireClickHandler(go, sector);

            // Hide undiscovered sectors (fog of war)
            go.SetActive(sector.IsDiscovered);

            _sectorObjects.Add(go);
        }

        private void WireClickHandler(GameObject go, SectorEntity sector)
        {
            if (go.GetComponent<Collider2D>() == null)
                go.AddComponent<CircleCollider2D>();

            var handler = go.GetComponent<SectorClickHandler>();
            if (handler == null)
                handler = go.AddComponent<SectorClickHandler>();

            handler.Bind(sector);
            handler.OnClicked += HandleSectorClicked;
        }

        private void HandleSectorClicked(SectorEntity sector)
        {
            OnSectorSelected?.Invoke(sector);
        }

        private void DrawConnections(SectorEntity sector, GalaxyMapEntity galaxy)
        {
            if (!sector.IsDiscovered)
                return;

            foreach (var neighbourId in sector.NeighbourIds)
            {
                var neighbour = galaxy.GetSector(neighbourId);
                if (neighbour == null || !neighbour.IsDiscovered)
                    continue;

                // Avoid drawing duplicate lines
                if (string.Compare(sector.Id.ToString(), neighbourId.ToString()) > 0)
                    continue;

                var line = new GameObject($"Line_{sector.Name}_{neighbour.Name}")
                    .AddComponent<LineRenderer>();

                line.transform.SetParent(transform);
                line.material = _connectionMaterial != null
                    ? _connectionMaterial
                    : new Material(Shader.Find("Sprites/Default"));

                line.startWidth = _lineWidth;
                line.endWidth = _lineWidth;
                line.positionCount = 2;
                line.SetPosition(0, new Vector3(sector.Position.x, sector.Position.y, 0.1f));
                line.SetPosition(1, new Vector3(neighbour.Position.x, neighbour.Position.y, 0.1f));
                line.startColor = new Color(1f, 1f, 1f, 0.2f);
                line.endColor = new Color(1f, 1f, 1f, 0.2f);

                _connectionLines.Add(line);
            }
        }

        private GameObject CreateDefaultSectorObject()
        {
            var go = new GameObject("Sector");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();

            // Simple circle sprite as fallback
            sr.sprite = CreateCircleSprite();

            // Matches the circle sprite so clicks land where the visual is
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;

            return go;
        }

        private Color GetSectorColor(SectorEntity sector)
        {
            if (sector.IsOwned) return _ownedColor;

            return sector.Type switch
            {
                SectorType.Safe => _safeColor,
                SectorType.Contested => _contestedColor,
                SectorType.Hostile => _hostileColor,
                SectorType.Nebula => _nebulaColor,
                SectorType.BlackHole => _blackHoleColor,
                _ => _unknownColor
            };
        }

        private static Sprite CreateCircleSprite()
        {
            // Simple programmatic circle texture
            int size = 64;
            var texture = new Texture2D(size, size);
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f));
        }

        private void Clear()
        {
            foreach (var go in _sectorObjects)
            {
                if (go == null) continue;

                var handler = go.GetComponent<SectorClickHandler>();
                if (handler != null)
                    handler.OnClicked -= HandleSectorClicked;

                Destroy(go);
            }

            foreach (var line in _connectionLines)
                if (line != null) Destroy(line.gameObject);

            _sectorObjects.Clear();
            _connectionLines.Clear();
        }
    }
}
