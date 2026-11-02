// NPC ship roles within a generated garrison - adds composition variety
// (BACKLOG-27) without touching the Base/ThreatScale balance formula in
// GameConfigSO (tuned 2026-09-24, see EnemyFleetGeneratorService). Each
// role is a hull/damage MULTIPLIER applied on top of that formula, not a
// new absolute stat block, so a garrison's total CombatPower at a given
// ThreatLevel and ship count stays close to what it was before this enum
// existed - only how that power is distributed across ships changes.

namespace GalacticEmpire.Feature.Battle.Domain
{
    public enum EnemyShipRole
    {
        Raider,  // the original, unmodified stat line - x1 hull, x1 damage
        Heavy,   // tankier, hits softer - more hull, less damage per shot
        Striker  // glass cannon - less hull, hits harder per shot
    }
}
