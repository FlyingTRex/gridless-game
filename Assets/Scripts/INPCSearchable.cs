using UnityEngine;

// Shared by any world object an NPCGathering job can walk up to and
// trigger, without that trigger putting anything directly into cargo —
// BerryBush/HerbBush's F-search action (2026-08-13, see
// NPC_JOB_GENERALIZATION_PLANNING.md section 3a). Unlike INPCHarvestable,
// triggering this only seeds the world with new Pickup objects; a
// following pass over loose Pickups is what actually gets them into cargo.
// Kept as its own interface rather than folded into INPCHarvestable so
// "TryHarvestForNPC succeeded" always means "cargo grew" — a search
// trigger doesn't make that promise.
public interface INPCSearchable
{
    bool IsAvailable { get; }
    Transform transform { get; }

    // No tool check inside, matching INPCHarvestable.TryHarvestForNPC's
    // convention — the caller already verified whatever this job needs
    // before calling this. Returns false (no-op) if on cooldown.
    bool TriggerSearchForNPC();
}
