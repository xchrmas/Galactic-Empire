// All possible states a fleet can be in during its lifetime.

namespace GalacticEmpire.Feature.Fleet.Domain
{
    public enum FleetStatus
    {
        Idle,        // docked at station, waiting for orders
        Moving,      // en route to a target sector
        InBattle,    // actively engaged in combat
        Retreating,  // pulling back after taking heavy losses
        Destroyed    // no ships remaining
    }
}
