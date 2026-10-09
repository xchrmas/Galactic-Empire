// In-memory mission tracking. No Infrastructure layer exists yet for
// Exploration (see features/exploration.md Known gaps) - missions aren't
// persisted across Editor Play sessions any more than Fleet aggregates are
// (BACKLOG-26). This interface exists so ExplorationService doesn't hold a
// raw List<MissionEntity> itself, matching the IFleetRepository/
// IGalaxyRepository shape used everywhere else, and so a real persisted
// implementation can replace MissionRepository later without touching
// ExplorationService.

using System;
using System.Collections.Generic;
using GalacticEmpire.Feature.Exploration.Domain;

namespace GalacticEmpire.Feature.Exploration.Application
{
    /// <summary>Tracks missions currently in flight.</summary>
    public interface IMissionRepository
    {
        // All missions not yet Completed
        IReadOnlyList<MissionEntity> GetActive();

        // Single mission by ID, null if not found or already completed
        MissionEntity GetById(Guid missionId);

        // Finds the active mission for a fleet, null if that fleet has none
        MissionEntity GetByFleetId(Guid fleetId);

        // Adds a new mission or overwrites an existing one by Id
        void Save(MissionEntity mission);

        // Removes a mission once Completed - no further tracking needed
        void Remove(Guid missionId);

        // Removes all missions - used to reset state at game start
        void Clear();
    }
}
