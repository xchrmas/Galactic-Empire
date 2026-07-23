// All ship classes available in the game.
// Each type has a different role on the battlefield.

namespace GalacticEmpire.Feature.Fleet.Infrastructure
{
    public enum ShipType
    {
        Fighter,    // fast and cheap - good for scouting
        Destroyer,  // balanced - the backbone of any fleet
        Cruiser,    // heavy firepower, slow
        Battleship, // massive hull, devastating damage
        Carrier     // deploys fighters, needs escort
    }
}
