// All possible states an exploration mission can be in during its lifetime.

namespace GalacticEmpire.Feature.Exploration.Domain
{
    public enum MissionStatus
    {
        OutboundTravel,  // fleet is flying toward the target sector
        Arrived,         // fleet reached the target, awaiting resolution (discovery/battle/reward)
        ReturnTravel,    // fleet is flying back to the origin sector
        Completed        // fleet is back, mission is done
    }
}
