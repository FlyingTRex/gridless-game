using System.Collections.Generic;
using UnityEngine;

// Attaches a physical model for whatever tools/equipment an NPC has
// actually been given (NPCJob.EquippedTools) onto its Humanoid rig
// (2026-08-13, direct follow-up to the NPC animation build — Ben's ask:
// "have the npc equip to body, the equipment we give him"). Reuses each
// ItemDefinition's own worldPickupPrefab (the same mesh a dropped item
// uses) rather than a dedicated held-model asset — no such asset exists
// for any tool today, and this project's convention has always been "ship
// the obvious v1 from what already exists, tune live" rather than
// blocking on new art.
//
// Attach point (which bone, what local offset) is data on the
// ToolRequirement itself (NPCJobDefinition.cs), not hardcoded per label
// here — every job's requirement already carries a label
// ("Pickaxe"/"Axe"/"Mining Face Shield"/"Backpack"), so the natural place
// for "where does this attach" is right next to it, and tuning a position
// later is a data edit, not a code change.
[RequireComponent(typeof(NPCJob))]
public class NPCEquipmentVisual : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private NPCJob job;

    // Keyed by ToolRequirement.label, mirroring NPCJob.EquippedTools'
    // own keying, so a label whose equipped item changes (or disappears)
    // is easy to detect by comparing against what's currently attached.
    private readonly Dictionary<string, ItemDefinition> attachedItems = new Dictionary<string, ItemDefinition>();
    private readonly Dictionary<string, GameObject> attachedInstances = new Dictionary<string, GameObject>();

    private void Awake()
    {
        job = GetComponent<NPCJob>();
    }

    private void Update()
    {
        if (animator == null) return;

        var equipped = job.EquippedTools;

        // Detach anything no longer equipped, or whose equipped item
        // changed to a different tier/instance since last attached (a
        // reassignment cleared it, or — not possible today, but not
        // assumed impossible either — a tool could be re-given).
        List<string> stale = null;
        foreach (var kv in attachedItems)
        {
            if (equipped.TryGetValue(kv.Key, out var current) && current == kv.Value) continue;
            (stale ??= new List<string>()).Add(kv.Key);
        }
        if (stale != null)
            foreach (var label in stale)
                Detach(label);

        var toolRequirements = job.AssignedJob != null ? job.AssignedJob.toolRequirements : null;
        if (toolRequirements == null) return;

        foreach (var req in toolRequirements)
        {
            if (req == null || string.IsNullOrEmpty(req.label)) continue;
            if (attachedItems.ContainsKey(req.label)) continue; // already attached and matching, per the pass above
            if (!equipped.TryGetValue(req.label, out var item) || item == null) continue;

            Attach(req, item);
        }
    }

    private void Attach(ToolRequirement req, ItemDefinition item)
    {
        if (item.worldPickupPrefab == null) return;

        var bone = animator.GetBoneTransform(req.attachBone);
        if (bone == null) return;

        var instance = Instantiate(item.worldPickupPrefab, bone);

        // attachPositionOffset/attachEulerOffset are interpreted relative
        // to the NPC's own root transform (forward/right/up), not the
        // bone's own local axes -- a hand/chest/head bone's local space
        // reflects its bind-pose orientation, which is rig-specific and
        // not something to guess blind. Position still tracks the bone
        // going forward (still parented as its child, so it moves with
        // the bone during animation same as any child transform); this
        // only changes what the *initial* offset means, so a number like
        // "0.15 behind" reliably means behind the character, not
        // whatever direction that bone's Z axis happens to point.
        instance.transform.position = bone.position + transform.TransformVector(req.attachPositionOffset);
        instance.transform.rotation = transform.rotation * Quaternion.Euler(req.attachEulerOffset);

        // A dropped-pickup prefab's own physics/interaction shouldn't run
        // on something rigidly bone-parented — disable it rather than
        // Destroy() it. Destroy is the wrong tool here: Tool.cs (and
        // potentially other IEquippable types) RequireComponent(Rigidbody)/
        // RequireComponent(Collider), and Unity silently refuses to
        // destroy a component something else still requires (logs an
        // error, leaves it in place) — which left a live, non-kinematic,
        // gravity-affected Rigidbody on the instance. A Rigidbody child
        // isn't actually carried by its parent's Transform once physics
        // starts simulating it; it falls/drifts away under gravity
        // independent of the bone it was parented to, which is almost
        // certainly why the Pickaxe read as "not showing" at all (it fell
        // away) while the Backpack — apparently not RequireComponent-gated
        // the same way — merely ended up misplaced.
        StripWorldBehavior(instance);

        attachedItems[req.label] = item;
        attachedInstances[req.label] = instance;
    }

    private void Detach(string label)
    {
        if (attachedInstances.TryGetValue(label, out var instance) && instance != null)
            Destroy(instance);
        attachedInstances.Remove(label);
        attachedItems.Remove(label);
    }

    private static void StripWorldBehavior(GameObject instance)
    {
        // Kinematic, not destroyed — a kinematic Rigidbody is purely
        // transform-driven (no gravity/forces), which is exactly "follows
        // the bone it's parented to and nothing else."
        foreach (var rb in instance.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        foreach (var col in instance.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var pickup in instance.GetComponentsInChildren<Pickup>()) pickup.enabled = false;
        foreach (var node in instance.GetComponentsInChildren<ResourceNode>()) node.enabled = false;
    }
}
