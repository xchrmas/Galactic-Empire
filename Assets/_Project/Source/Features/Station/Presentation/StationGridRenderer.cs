// Renders the station's grid as clickable cells - empty cells show one color,
// occupied cells show another based on the module's category.

using System;
using System.Collections.Generic;
using System.Linq;
using GalacticEmpire.Feature.Station.Application;
using GalacticEmpire.Feature.Station.Domain;
using UnityEngine;

namespace GalacticEmpire.Feature.Station.Presentation
{
    /// <summary>Spawns and manages visual representations of all station grid cells.</summary>
    public sealed class StationGridRenderer : MonoBehaviour
    {
        [Header("Cell Visuals")]
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private float _cellSpacing = 2f;

        [Header("Cell Colors")]
        [SerializeField] private Color _emptyColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private Color _productionColor = new Color(0.2f, 0.8f, 0.4f);
        [SerializeField] private Color _militaryColor = new Color(0.8f, 0.3f, 0.2f);
        [SerializeField] private Color _supportColor = new Color(0.3f, 0.5f, 0.9f);

        private IStationService _stationService;
        private readonly List<GameObject> _cellObjects = new();

        // Raised when the player clicks any cell - empty or occupied
        public event Action<GridCell> OnCellSelected;

        // Call this from StationBuilderPresenter after VContainer injection
        public void Initialize(IStationService stationService)
        {
            _stationService = stationService;
            Render();
        }

        /// <summary>Clears and redraws every grid cell from the current station state.</summary>
        public void Render()
        {
            Clear();

            var station = _stationService.GetStation();
            if (station == null)
                return;

            foreach (var cell in station.Grid)
                SpawnCell(cell, station);
        }

        private void SpawnCell(GridCell cell, StationEntity station)
        {
            var go = _cellPrefab != null
                ? Instantiate(_cellPrefab, transform)
                : CreateDefaultCellObject();

            go.transform.localPosition = new Vector3(cell.X * _cellSpacing, cell.Y * _cellSpacing, 0f);
            go.name = $"Cell_{cell.X}_{cell.Y}";

            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = GetCellColor(cell, station);

            WireClickHandler(go, cell);

            _cellObjects.Add(go);
        }

        private void WireClickHandler(GameObject go, GridCell cell)
        {
            if (go.GetComponent<Collider2D>() == null)
                go.AddComponent<BoxCollider2D>();

            var handler = go.GetComponent<StationCellClickHandler>();
            if (handler == null)
                handler = go.AddComponent<StationCellClickHandler>();

            handler.Bind(cell);
            handler.OnClicked += HandleCellClicked;
        }

        private void HandleCellClicked(GridCell cell)
        {
            OnCellSelected?.Invoke(cell);
        }

        private Color GetCellColor(GridCell cell, StationEntity station)
        {
            if (cell.IsEmpty)
                return _emptyColor;

            var module = station.Modules.FirstOrDefault(m => m.Id == cell.ModuleId);
            if (module == null)
                return _emptyColor;

            return module.Category switch
            {
                StationModuleCategory.Production => _productionColor,
                StationModuleCategory.Military => _militaryColor,
                StationModuleCategory.Support => _supportColor,
                _ => _emptyColor
            };
        }

        private GameObject CreateDefaultCellObject()
        {
            var go = new GameObject("Cell");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();

            sr.sprite = CreateSquareSprite();

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            return go;
        }

        private static Sprite CreateSquareSprite()
        {
            int size = 64;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size); // pixelsPerUnit = size -> sprite is exactly 1 world unit wide
        }

        private void Clear()
        {
            foreach (var go in _cellObjects)
            {
                if (go == null) continue;

                var handler = go.GetComponent<StationCellClickHandler>();
                if (handler != null)
                    handler.OnClicked -= HandleCellClicked;

                Destroy(go);
            }

            _cellObjects.Clear();
        }
    }
}
