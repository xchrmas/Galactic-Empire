// Sits on a single sector GameObject - forwards mouse clicks to whoever is listening.
// Requires a Collider2D on the same object to receive OnMouseDown.

using System;
using GalacticEmpire.Feature.Galaxy.Domain;
using UnityEngine;

namespace GalacticEmpire.Feature.Galaxy.Presentation
{
    /// <summary>Detects clicks on a sector's visual and raises an event with its data.</summary>
    public sealed class SectorClickHandler : MonoBehaviour
    {
        public SectorEntity Sector { get; private set; }

        public event Action<SectorEntity> OnClicked;

        // Called by GalaxyMapRenderer right after spawning the sector object
        public void Bind(SectorEntity sector)
        {
            Sector = sector;
        }

        private void OnMouseDown()
        {
            OnClicked?.Invoke(Sector);
        }
    }
}
