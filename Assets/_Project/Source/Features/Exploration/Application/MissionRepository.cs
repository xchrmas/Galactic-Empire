// Plain in-memory implementation - not a ScriptableObject, since nothing
// about mission state needs Inspector visibility or Odin debug buttons the
// way Fleet/Galaxy/Station's repositories do. Registered as a singleton in
// GameBootstrapper, same lifetime as those.

using System;
using System.Collections.Generic;
using System.Linq;
using GalacticEmpire.Feature.Exploration.Domain;

namespace GalacticEmpire.Feature.Exploration.Application
{
    /// <summary>In-memory store for missions currently in flight.</summary>
    public sealed class MissionRepository : IMissionRepository
    {
        private readonly Dictionary<Guid, MissionEntity> _missions = new();

        public IReadOnlyList<MissionEntity> GetActive()
        {
            return _missions.Values.ToList().AsReadOnly();
        }

        public MissionEntity GetById(Guid missionId)
        {
            return _missions.TryGetValue(missionId, out var mission) ? mission : null;
        }

        public MissionEntity GetByFleetId(Guid fleetId)
        {
            return _missions.Values.FirstOrDefault(m => m.FleetId == fleetId);
        }

        public void Save(MissionEntity mission)
        {
            if (mission == null)
                throw new ArgumentNullException(nameof(mission));

            _missions[mission.Id] = mission;
        }

        public void Remove(Guid missionId)
        {
            _missions.Remove(missionId);
        }

        public void Clear()
        {
            _missions.Clear();
        }
    }
}
