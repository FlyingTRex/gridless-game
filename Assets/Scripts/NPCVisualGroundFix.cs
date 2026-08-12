using UnityEngine;

// Continuous self-correction: Mecanim's Humanoid retargeting shifts the
// character's effective root height slightly differently than the raw
// bind-pose bounds used to position Visual in the prefab, once a real
// AnimatorController/clip actually drives it -- confirmed live (2026-08-11,
// Ben: NPC stood partially sunk into the ground once the idle pose
// animation was applied, despite feet lining up correctly in the raw
// bind pose).
//
// v1 of this fix corrected once on the first LateUpdate after enable, then
// disabled itself -- Ben's live retest still showed sinking. Working theory
// (not live-confirmed, batch mode can't reliably evaluate Humanoid
// retargeting per CLAUDE.md): the one-shot correction likely ran before the
// Animator evaluated its first real pose on scene load, measured
// near-bind-pose bounds (already correctly grounded per the v0.3.5-dev
// changelog entry), computed ~zero correction, and permanently disabled
// itself before the true post-animation offset appeared a frame or two
// later. Fix: correct every frame instead of once, so it can't get stuck on
// a stale early measurement regardless of which frame the Animator actually
// settles on -- also makes this robust to any future idle animation with a
// vertical bob, which a one-shot correction could never track correctly
// anyway. X/Z are captured once and held fixed so a bounds asymmetry can't
// introduce sideways drift; only Y is corrected every frame.
//
// Cheap at the current NPC count (one renderer each, ~6 NPCs in-scene) --
// revisit with an update-interval throttle if NPC count grows much further.
public class NPCVisualGroundFix : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private Renderer[] renderers;

    private float baseLocalX;
    private float baseLocalZ;
    private bool initialized;

    private void LateUpdate()
    {
        if (visual == null || renderers == null || renderers.Length == 0) { enabled = false; return; }

        if (!initialized)
        {
            baseLocalX = visual.localPosition.x;
            baseLocalZ = visual.localPosition.z;
            initialized = true;
        }

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        float feetOffset = b.min.y - transform.position.y;

        Vector3 pos = visual.localPosition;
        visual.localPosition = new Vector3(baseLocalX, pos.y - feetOffset, baseLocalZ);
    }
}
