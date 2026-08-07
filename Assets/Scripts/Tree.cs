using System.Collections.Generic;
using UnityEngine;

// Chop-down mechanic for trees: hit hitsToChop times (with an Axe, same
// requiredTools/HasInHand gating ResourceNode uses), drop logCount Log
// instances scattered nearby, and swap the tree's own visual for a
// "Stump" child instead of fully disappearing — distinct from
// ResourceNode's hide-then-respawn-as-itself model (Boulder/ore nodes),
// since a chopped tree should visibly read as a stump in between, not
// vanish outright. Logs themselves reuse ResourceNode directly (see
// Log.prefab) — chopping one down to Planks (+ a chance of a Stick) is a
// plain punch-N-times-then-spawn-chunks case, same shape as everything
// else that uses it.
public class Tree : MonoBehaviour, IPunchable
{
    [SerializeField] private GameObject logPrefab;
    [SerializeField] private int logCount = 3;
    [SerializeField] private int hitsToChop = 3;
    [SerializeField] private float scatterForce = 1.2f;
    [SerializeField] private SkillDefinition trainedSkill;
    [SerializeField] private float skillGain = 0.5f;
    // <= 0 means the stump never regrows — same "0 disables it" reading
    // as ResourceNode.respawnDelay, kept consistent rather than inventing
    // a separate bool.
    [SerializeField] private float regrowDelay = 180f;
    [SerializeField] private ItemDefinition[] requiredTools;
    [SerializeField] private string requiredToolLabel = "Axe";

    private int hitsTaken;
    private Collider col;
    private Transform stumpTransform;
    private Renderer[] treeRenderers;
    private float regrowAt = -1f;

    public string Prompt => IsStump
        ? "Stump (regrowing)"
        : (requiredTools != null && requiredTools.Length > 0 ? $"Chop (requires {requiredToolLabel})" : "Chop");

    private bool IsStump => regrowAt >= 0f;

    private void Awake()
    {
        col = GetComponent<Collider>();

        stumpTransform = transform.Find("Stump");
        if (stumpTransform != null) stumpTransform.gameObject.SetActive(false);

        // Everything under this object that isn't the stump is "the tree"
        // — trunk plus however many leaf clusters the procedural mesh has
        // — so hiding/showing it doesn't need each one individually wired
        // up by hand.
        var all = GetComponentsInChildren<Renderer>(true);
        var treeList = new List<Renderer>();
        foreach (var r in all)
        {
            if (stumpTransform != null && r.transform.IsChildOf(stumpTransform)) continue;
            treeList.Add(r);
        }
        treeRenderers = treeList.ToArray();
    }

    private void Update()
    {
        if (regrowAt < 0f || Time.time < regrowAt) return;
        Regrow();
    }

    public void OnPunch(GameObject player)
    {
        if (IsStump) return;

        if (requiredTools != null && requiredTools.Length > 0)
        {
            var equipment = player.GetComponent<PlayerEquipment>();
            if (equipment == null || !HasAnyRequiredToolInHand(equipment)) return;
        }

        hitsTaken++;
        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);
        if (hitsTaken < hitsToChop) return;

        for (int i = 0; i < logCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.5f;
            offset.y = Mathf.Abs(offset.y);
            var log = Instantiate(logPrefab, transform.position + Vector3.up * 0.3f + offset, Random.rotation);

            if (log.TryGetComponent(out Rigidbody rb))
            {
                Vector3 dir = (Random.insideUnitSphere + Vector3.up).normalized;
                rb.AddForce(dir * scatterForce, ForceMode.Impulse);
            }
        }

        SetStump(true);
        if (regrowDelay > 0f)
            regrowAt = Time.time + regrowDelay;
    }

    private bool HasAnyRequiredToolInHand(PlayerEquipment equipment)
    {
        foreach (var tool in requiredTools)
        {
            if (tool != null && equipment.HasInHand(tool)) return true;
        }
        return false;
    }

    private void Regrow()
    {
        hitsTaken = 0;
        SetStump(false);
        regrowAt = -1f;
    }

    private void SetStump(bool isStump)
    {
        foreach (var r in treeRenderers)
            r.enabled = !isStump;

        if (stumpTransform != null)
            stumpTransform.gameObject.SetActive(isStump);

        // The collider stays enabled either way — a stump is still a
        // physical obstacle, unlike ResourceNode's chunks which fully
        // disappear when broken.
    }
}
