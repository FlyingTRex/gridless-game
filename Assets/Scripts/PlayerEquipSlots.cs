// Shared equip-slot name constants (EFFICIENCY_AUDIT.md, 2026-08-15) —
// `{ "Left Hand", "Right Hand" }` was independently declared as its own
// private static readonly field in 13 separate equip-carrier scripts
// (PlayerBackpack, PlayerBelt, PlayerBoot, PlayerCanteen, PlayerHealthMonitor,
// PlayerJeans, PlayerLoot, PlayerMiningFaceShield, PlayerNavComputer,
// PlayerRangedCombat, PlayerShirt, PlayerSunglasses, PlayerTool) before
// being consolidated here. Each of those keeps its own local `HandSlots`
// name (just delegated to this shared array) rather than having every
// call site renamed, to keep this a low-risk mechanical change.
public static class PlayerEquipSlots
{
    public static readonly string[] Hands = { "Left Hand", "Right Hand" };
}
