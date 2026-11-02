// Categories of ship module (Stage 1c, modular ships). Mirrors
// StationModuleCategory's role for Station, but these are the categories
// a ShipSlot can be typed to accept - a slot's Category must match an
// installed module's Category (see ShipSlot.Install).

namespace GalacticEmpire.Feature.Fleet.Domain
{
    public enum ShipModuleCategory
    {
        Weapon,
        Armor,
        Engine,
        Utility
    }
}
