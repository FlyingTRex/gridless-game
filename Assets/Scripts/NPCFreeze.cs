using Mirror;
using UnityEngine;

// Multiplayer, 2026-08-23 -- converted to NetworkBehaviour, isServer
// guard on Update() below, same cleanup pass as NPCWander/NPCFlee/
// NPCVitals.
//
// 2026-08-25: SetFrozen now routes through a real Command
// (NPCHiringScreen.RequestSetFrozen), closing the gap this comment used
// to flag. That made IsFrozen need to become a real [SyncVar] too --
// SetFrozen now only ever runs on the SERVER's copy of this object, so
// a plain local bool would only ever update there, leaving the actual
// calling client's own checkbox (which reads IsFrozen locally) stuck
// showing stale state next time the menu reopens. A SyncVar replicates
// the real server truth to every observer instead.
//
// Manual "stay put" toggle for any NPC (2026-08-17, BUGS_AND_ENHANCEMENTS.md
// "NPC management"), independent of NPCDialogue's own pause-during-Talk
// mechanism. Built generic/reusable on purpose -- Ben's explicit ask that
// this should also work for a future Traveling Trader once one exists,
// which won't be an NPCHiring at all (COMMERCE_PLANNING.md). Every
// behavior component reference is optional (GetComponent, no
// RequireComponent) so this attaches cleanly to any NPC regardless of
// which job/movement components it happens to have.
//
// Re-asserts SetPaused(true) every frame while frozen instead of a single
// one-shot call, so it wins over any other system (Talk starting/ending,
// a job component's own state machine) that might otherwise try to
// unpause the same component -- Freeze is meant to be the highest-
// priority override, not one more competing caller.
public class NPCFreeze : NetworkBehaviour
{
    private NPCWander wander;
    private NPCGathering gathering;
    private NPCCrafting crafting;
    private NPCGuarding guarding;

    [SyncVar] private bool isFrozen;
    public bool IsFrozen => isFrozen;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        gathering = GetComponent<NPCGathering>();
        crafting = GetComponent<NPCCrafting>();
        guarding = GetComponent<NPCGuarding>();
    }

    public void SetFrozen(bool frozen) => isFrozen = frozen;

    // NPCTraining deliberately not included -- it has no pause concept of
    // its own (only CancelTraining, which would discard the training
    // session entirely rather than just holding it in place, not an
    // equivalent to what Freeze means for every other component here).
    private void Update()
    {
        if (!isServer) return;
        if (!IsFrozen) return;

        wander?.SetPaused(true);
        gathering?.SetPaused(true);
        crafting?.SetPaused(true);
        guarding?.SetPaused(true);
    }
}
