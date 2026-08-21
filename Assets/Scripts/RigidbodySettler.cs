using UnityEngine;

// Freezes a dropped/scattered physics object once it's settled (2026-08-20,
// friction/rolling-down-hills fix — see BUGS_AND_ENHANCEMENTS.md). Standalone
// and script-agnostic on purpose: the ~132 Rigidbody-bearing prefabs in this
// project span many different owning scripts (Pickup, Tool, ResourceNode/
// Chunk, SkillBook, ...), so this doesn't hook into any of them -- it just
// watches its own Rigidbody's velocity and goes kinematic once it's been
// below settleVelocityThreshold for settleDuration seconds in a row. This is
// a defense-in-depth safety net on top of the new high-friction Terrain
// PhysicMaterial (Maximum combine), not a replacement for it -- friction
// alone should stop most sliding, but slow numerical creep on a slope is a
// real, common Unity issue even at correct friction values, especially near
// moving colliders like the player/NPCs constantly walking past a pile of
// dropped items. Once frozen, code can still move the object directly
// (Pickup.Respawn() sets transform.position regardless of isKinematic), so
// this doesn't interfere with anything that already teleports these objects.
[RequireComponent(typeof(Rigidbody))]
public class RigidbodySettler : MonoBehaviour
{
    [SerializeField] private float settleVelocityThreshold = 0.05f;
    [SerializeField] private float settleDuration = 1.5f;

    private Rigidbody rb;
    private float belowThresholdTimer;
    private bool settled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (settled || rb.isKinematic) return;

        if (rb.linearVelocity.sqrMagnitude < settleVelocityThreshold * settleVelocityThreshold)
        {
            belowThresholdTimer += Time.fixedDeltaTime;
            if (belowThresholdTimer >= settleDuration)
            {
                rb.isKinematic = true;
                settled = true;
            }
        }
        else
        {
            belowThresholdTimer = 0f;
        }
    }
}
