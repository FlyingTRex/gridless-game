using UnityEngine;

// Drives the KI Humanoid dummy's Animator from whatever's actually moving
// this NPC around, without needing to know which script that is (NPCWander's
// idle-wander vs. NPCGathering's job-pathing/return-to-deposit) -- Speed is
// computed from raw frame-to-frame position displacement instead of reaching
// into either mover's private state, so it stays correct automatically even
// if a future movement source is added. Sibling to NPCVisualGroundFix on the
// same prefab (2026-08-13, NPC animation build -- see MVP2_PLANNING.md item
// 4 and this session's approved plan).
public class NPCAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsWorkingParam = Animator.StringToHash("IsWorking");
    private static readonly int WorkTypeParam = Animator.StringToHash("WorkType");

    // Optional -- an NPC with no NPCGathering (not hired/assigned a job yet)
    // just never enters a Work state, same "optional sibling" convention
    // NPCGathering.hiring already uses.
    private NPCGathering gathering;

    private Vector3 lastPosition;

    private void Awake()
    {
        gathering = GetComponent<NPCGathering>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (animator == null) return;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        lastPosition = transform.position;
        animator.SetFloat(SpeedParam, speed);

        bool working = gathering != null && gathering.IsActingOnTarget;
        var workType = gathering != null ? gathering.CurrentWorkAnimation : NPCJobDefinition.WorkAnimationType.None;
        animator.SetBool(IsWorkingParam, working);
        animator.SetInteger(WorkTypeParam, (int)workType);
    }
}
