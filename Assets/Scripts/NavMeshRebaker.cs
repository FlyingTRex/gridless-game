using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

// NavMesh Phase 1 (2026-08-21, NPC_NAVIGATION_PLANNING.md) -- players
// build/destroy/upgrade structures live, so the baked-once-at-Phase-0
// navmesh goes stale the moment anything changes. Deliberately the
// simple version first (Ben's own call): a full-surface rebake on every
// change, not a perf-optimized bounded-region rebake -- worth revisiting
// if it turns out to hitch noticeably at real settlement scale, but not
// worth the extra complexity before that's actually confirmed to matter.
// Centralized here so PlayerBuilding/PlayerPieceUpgrade don't each need
// their own FindFirstObjectByType<NavMeshSurface> lookup.
public static class NavMeshRebaker
{
    // Logs unconditionally (not gated by any NPCJob.DebugEnabled checkbox --
    // there's no natural "which NPC" to hang this on) so a silent no-op
    // (no NavMeshSurface found) is never invisible again. Found live
    // 2026-08-21: couldn't tell from the debug log alone whether a rebake
    // requested by SaveManager.Load() actually ran.
    public static void RequestRebake()
    {
        var surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            DebugLog.Write("NavMeshRebaker", "RequestRebake: no NavMeshSurface found in scene -- rebake skipped");
            return;
        }
        surface.BuildNavMesh();
        DebugLog.Write("NavMeshRebaker", "RequestRebake: rebake complete");
    }

    // Unity's Destroy() only marks a GameObject for removal -- it stays
    // fully present (collider included) until the end of the current
    // frame. Rebaking immediately after a Destroy() call would still see
    // the about-to-be-removed object's geometry, so any caller that
    // destroys something (PlayerPieceUpgrade's Upgrade/DestroyPiece) needs
    // this delayed variant instead of RequestRebake(). host just needs to
    // be any live MonoBehaviour to run the coroutine on -- it's not
    // otherwise involved.
    public static void RequestRebakeDelayed(MonoBehaviour host)
    {
        host.StartCoroutine(RebakeNextFrame());
    }

    private static IEnumerator RebakeNextFrame()
    {
        yield return null;
        RequestRebake();
    }
}
