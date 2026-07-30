// Contract for galaxy map persistence.
// Only one galaxy exists per game - Get() returns null on first launch.

using GalacticEmpire.Feature.Galaxy.Domain;

namespace GalacticEmpire.Feature.Galaxy.Application
{
    /// <summary>Defines galaxy map read/write operations.</summary>
    public interface IGalaxyRepository
    {
        // Returns the current galaxy, null if not yet generated
        GalaxyMapEntity Get();

        // Saves the galaxy after generation or state changes
        void Save(GalaxyMapEntity galaxy);

        // True if a galaxy has been generated already
        bool HasGalaxy();
    }
}
