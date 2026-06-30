// Contract for station data access

namespace GalacticEmpire.Core
{
    /// <summary>Defines station data access operations </summary>
    public interface IStationRepository
    {
        // Returns the player's station, or null if not yet created
        StationEntity Get();

        // Saves the current station state
        void Save(StationEntity station);

        // Returns true if a station has been created
        bool HasStation();
    }
}
