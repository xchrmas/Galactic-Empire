// Defines station module categories and types.

namespace GalacticEmpire.Core
{
    /// <summary>High-level category of a station module.</summary>
    public enum StationModuleCategory
    {
        Production, // generates resources over time
        Military,   // enables ship construction and defense
        Support     // provides empire-wide bonuses
    }

    /// <summary>Specific type station module.</summary>
    public enum StationModuleType
    {
        // Production
        MetalMine,        // produces Metal over time
        EnergyReactor,    // produces Energy over time
        CrystalExtractor, // produces Crystals over time

        // Military
        Shipyard,         // unlocks ship construction
        WeaponsPlatform,  // adds defense turrets
        ShieldGenerator,  // provides station shield

        // Support
        CommandCenter,    // required - station core module
        ResearchLab,      // unlocks technology upgrades
        TradeHub          // enables resource trading
    }
}
