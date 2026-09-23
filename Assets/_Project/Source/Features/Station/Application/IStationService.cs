// Commands and service contract for station operations.
using GalacticEmpire.Feature.Station.Domain;

namespace GalacticEmpire.Feature.Station.Application
{
    // creates an entity using StationModuleSO.CreateEntity() and passes it here.
    public sealed record PlaceModuleCommand(StationModuleEntity Module, int X, int Y);
    public sealed record RemoveModuleCommand(int X, int Y);

    /// <summary>All station use cases go through here.</summary>
    public interface IStationService
    {
        // Returns the player's station
        StationEntity GetStation();

        // Places a new module at the given grid position - checks resources and module limit first
        StationEntity PlaceModule(PlaceModuleCommand cmd);

        // Removes the module at the given grid position
        StationEntity RemoveModule(RemoveModuleCommand cmd);

        // Returns the existing station, or creates and persists a new one if
        // none exists yet - called once at boot, mirrors IGalaxyService's
        // get-or-create shape for GetGalaxy/GenerateGalaxy
        StationEntity EnsureStation(string name, int gridSize);

        // Removes the saved station - used to reset state at game start so
        // Editor Play sessions always start with a fresh station
        void ClearStation();
    }
}
