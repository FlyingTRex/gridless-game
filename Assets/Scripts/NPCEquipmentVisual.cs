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
        instance.transform.localPosition = req.attachPositionOffset;
        instance.transform.localRotation = Quaternion.Euler(req.attachEulerOffset);

        // A dropped-pickup prefab's own interactable/physics components
        // (Rigidbody, Collider, Pickup/ResourceNode) don't make sense on
        // something rigidly bone-parented — strip them so this reads as
        // pure decoration, not a second, independently-interactable
        // pickup riding along on the NPC's hand.
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
        foreach (var rb in instance.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
        foreach (var col in instance.GetComponentsInChildren<Collider>()) Destroy(col);
        foreach (var pickup in instance.GetComponentsInChildren<Pickup>()) Destroy(pickup);
        foreach (var node in instance.GetComponentsInChildren<ResourceNode>()) Destroy(node);
    }
}
