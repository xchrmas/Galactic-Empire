// All possible states a battle can be in.

namespace GalacticEmpire.Feature.Battle.Domain
{
    public enum BattleStatus
    {
        Preparing,  // fleets moving into position
        Active,     // combat is ongoing
        Finished,   // battle concluded - winner decided
        Draw        // both fleets destroyed each other
    }
}
