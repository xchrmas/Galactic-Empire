// Defines what kind of sector a star system is.
// Affects enemy strength, resource richness and travel risk.

namespace GalacticEmpire.Feature.Galaxy.Domain
{
    public enum SectorType
    {
        Safe,       // player's home territory - no enemies
        Contested,  // minor enemies, good resources
        Hostile,    // strong enemies, rare resources
        Unknown,    // not yet explored - anything could be here
        Nebula,     // special - slows fleet movement, hides ships
        BlackHole   // dangerous - massive rewards if conquered
    }
}
