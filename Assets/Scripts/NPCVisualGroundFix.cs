using UnityEngine;

// One-time self-correction: Mecanim's Humanoid retargeting shifts the
// character's effective root height slightly differently than the raw
// bind-pose bounds used to position Visual in the prefab, once a real
// AnimatorController/clip actually drives it -- confirmed live (2026-08-11,
// Ben: NPC stood partially sunk into the ground once the idle pose
// animation was applied, despite feet lining up correctly in the raw
// bind pose). Corrects once, a frame after the Animator has actually
// evaluated a pose, rather than hand-tuning a static prefab offset that
// can't be reliably previewed via batch-mode rendering (confirmed
// separately that Animator.Update() in batch/edit mode doesn't reliably
// reflect real Play-mode Humanoid retargeting).
public class NPCVisualGroundFix : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private Renderer[] renderers;

    private bool corrected;

    private void LateUpdate()
    {
        if (corrected) return;
        if (visual == null || renderers == null || renderers.Length == 0) { enabled = false; return; }

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        float feetOffset = b.min.y - transform.position.y;
        visual.localPosition -= new Vector3(0, feetOffset, 0);

        corrected = true;
        enabled = false;
    }
}
