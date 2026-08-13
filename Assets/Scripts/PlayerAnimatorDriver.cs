using UnityEngine;

// Drives the player's KI Humanoid Visual body from FirstPersonController's
// movement state, mirroring NPCAnimatorDriver.cs's pattern: Speed is
// computed from raw frame-to-frame position displacement rather than
// reading movement internals directly, same reasoning as the NPC version
// (stays correct regardless of exactly how CharacterController.Move is
// driven that frame). Additionally tracks the previous stance to fire a
// StanceChanged trigger only on an actual change -- the Animator Controller
// gates its Any-State stance-select transitions on that trigger (not on
// Stance alone) so they don't re-trigger every frame and stomp the
// Idle<->Walk blending happening within the current stance (2026-08-13,
// player animation build -- see this session's approved plan).
public class PlayerAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private FirstPersonController controller;
    [SerializeField] private PlayerVitals vitals;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsSprintingParam = Animator.StringToHash("IsSprinting");
    private static readonly int StanceParam = Animator.StringToHash("Stance");
    private static readonly int StanceChangedParam = Animator.StringToHash("StanceChanged");

    private Vector3 lastPosition;
    private MovementStance lastStance;
    private bool initialized;

    // Read by PlayerBodyModel (2026-08-13) when the player toggles Male/
    // Female in the ` menu's Player tab -- both gendered Visual instances
    // exist simultaneously (SetActive-toggled, not Instantiate/Destroy),
    // so re-pointing this field is all that's needed to make the driver
    // animate whichever one is currently active.
    public void SetAnimator(Animator newAnimator) => animator = newAnimator;

    private void Awake()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (animator == null || controller == null || vitals == null) return;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        lastPosition = transform.position;
        animator.SetFloat(SpeedParam, speed);
        animator.SetBool(IsSprintingParam, vitals.IsSprinting);

        var stance = controller.CurrentStance;
        animator.SetInteger(StanceParam, (int)stance);
        if (!initialized || stance != lastStance)
        {
            animator.SetTrigger(StanceChangedParam);
            initialized = true;
        }
        lastStance = stance;
    }
}
