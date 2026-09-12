// Sits on a single grid cell GameObject - forwards mouse clicks to whoever is listening.
// Requires a Collider2D on the same object to receive OnMouseDown.

using System;
using GalacticEmpire.Feature.Station.Domain;
using UnityEngine;

namespace GalacticEmpire.Feature.Station.Presentation
{
    /// <summary>Detects clicks on a grid cell's visual and raises an event with its data.</summary>
    public sealed class StationCellClickHandler : MonoBehaviour
    {
        public GridCell Cell { get; private set; }

        public event Action<GridCell> OnClicked;

        // Called by StationGridRenderer right after spawning the cell object
        public void Bind(GridCell cell)
        {
            Cell = cell;
        }

        private void OnMouseDown()
        {
            OnClicked?.Invoke(Cell);
        }
    }
}
