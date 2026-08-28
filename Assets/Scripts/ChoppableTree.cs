using System.Collections.Generic;
using Mirror;
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
//
// Named ChoppableTree, not Tree — UnityEngine already has a built-in
// Tree component (part of the Terrain system). A class literally named
// Tree compiles, but Unity warns "AddComponent and GetComponent will not
// work with this script" and silently breaks generic lookups by type,
// which is exactly what this component needs (Awake's
// GetComponentsInChildren<Renderer> is fine — it's any
// GetComponent<Tree>()/AddComponent<Tree>() call elsewhere that would
// have resolved to the wrong type).
// FIXED (2026-08-28, found live -- "traskmi saw a skill increase, I
// couldn't pick up sticks"): this was plain MonoBehaviour, zero Mirror
// integration at all -- chopping a tree (the Log spawn AND the stump
// visual/regrow state) ran entirely on whichever machine's
// PlayerInteraction called Complete(), never reaching the server or any
// other observer. Trees are pre-placed TestScene.unity fixtures, not
// runtime-spawned (confirmed -- the only Instantiate in this file is the
// Log drop, already partially networked), so they work the same way
// StorageBox/Furnace/VillageFlag already do once converted: a real
// NetworkIdentity per scene instance (added via a throwaway batch
// script, same as tonight's "Small Storage Box" fix) plus this base-
// class conversion is enough for Mirror's own scene-object spawning to
// pick them up automatically, no manual NetworkServer.Spawn() needed.
public class ChoppableTree : NetworkBehaviour, IInteractable, INPCHarvestable
{
    [SerializeField] private GameObject logPrefab;
    [SerializeField] private int logCount = 3;
    [SerializeField] private float scatterForce = 1.2f;
    [SerializeField] private SkillDefinition trainedSkill;
    [SerializeField] private float skillGain = 0.5f;

    // What an NPC-felled tree yields directly into cargo (2026-08-13, see
    // NPC_JOB_GENERALIZATION_PLANNING.md section 2) -- the player path
    // keeps spawning logCount physical logPrefab instances via Complete()
    // below, untouched; an NPC has no "walk over and collect what I just
    // knocked loose" step, so TryHarvestForNPC yields logItem x logCount
    // straight to cargo instead, same INPCHarvestable split ResourceNode
    // already established (TryHarvestForNPC skips the scatter, Complete
    // keeps it). Set to the same Log ItemDefinition logPrefab's own
    // ResourceNode.pickupItem points at.
    [SerializeField] private ItemDefinition logItem;
    // <= 0 means the stump never regrows — same "0 disables it" reading
    // as ResourceNode.respawnDelay, kept consistent rather than inventing
    // a separate bool.
    [SerializeField] private float regrowDelay = 180f;
    [SerializeField] private ItemDefinition[] requiredTools;
    [SerializeField] private string requiredToolLabel = "Axe";

    private Collider col;
    private Transform stumpTransform;
    private Renderer[] treeRenderers;
    // Server-only scheduling -- Time.time is per-process, not meaningful
    // to broadcast, so only the server ever runs this timer (Update() is
    // now guarded by isServer). The actual stump/full-tree VISUAL is a
    // separate SyncVar below so every observer sees the same state.
    private float regrowAt = -1f;

    // Replaces the old `regrowAt >= 0f` check as the source of truth for
    // IsStump -- regrowAt is server-only now, but every observer
    // (including the acting player's own client) still needs to know
    // whether the tree is currently a stump. The hook drives the actual
    // renderer toggle, so setting this one field is enough to update the
    // visual on whichever machine it changes on (server included -- a
    // SyncVar hook fires locally too, not just on remote observers).
    [SyncVar(hook = nameof(OnStumpChanged))]
    private bool stumpActive;

    private void OnStumpChanged(bool oldValue, bool newValue) => SetStump(newValue);

    public string Prompt => IsStump
        ? "Stump (regrowing)"
        : (requiredTools != null && requiredTools.Length > 0 ? $"Hold to chop (requires {requiredToolLabel})" : "Hold to chop");

    public bool IsInstant => false;

    // Same skill-driven duration model as ResourceNode — see design-brief.md's
    // Interaction model note. Holding on a stump just wastes the hold, same
    // as punching one used to do nothing; not specially blocked.
    public float GetHoldDuration(GameObject player) =>
        player.GetComponent<PlayerSkills>().GetHoldDuration(trainedSkill);

    // INPCHarvestable (2026-08-13) — same public surface ResourceNode
    // exposes for NPCGathering's target search/tool-check/carry-check.
    public bool IsAvailable => !IsStump;
    public ItemDefinition[] RequiredTools => requiredTools;
    public float SkillGain => skillGain;

    private bool IsStump => stumpActive;

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
        if (!isServer) return;
        if (regrowAt < 0f || Time.time < regrowAt) return;
        Regrow();
    }

    // Called once the hold completes — replaces the old repeated-OnPunch/
    // hitsToChop counter, single-shot now that the wait is the gate.
    //
    // FIXED (2026-08-28): same dual-path dispatch Pickup.Complete()
    // already established -- a networked instance routes through a
    // Command so the real fell (Log spawn + stump state) always happens
    // server-side, regardless of who chopped it; the local-only path
    // stays for a prefab/instance with no NetworkIdentity (shouldn't
    // happen for a real scene tree post-fix, but keeps single-player/
    // offline testing working without a NetworkIdentity present).
    public void Complete(GameObject player)
    {
        if (IsStump) return;

        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active)
        {
            player.GetComponent<PlayerInteraction>()?.RequestChopTree(this);
            return;
        }

        ServerComplete(player);
    }

    // The real fell logic, always server-side for a networked instance
    // (see CmdChopTree on PlayerInteraction) or local-only otherwise --
    // same "one source of truth either way" shape Pickup.ServerComplete
    // already established.
    public void ServerComplete(GameObject player)
    {
        if (IsStump) return;

        if (requiredTools != null && requiredTools.Length > 0)
        {
            var equipment = player.GetComponent<PlayerEquipment>();
            if (equipment == null || !HasAnyRequiredToolInHand(equipment)) return;
        }

        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);

        for (int i = 0; i < logCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.5f;
            offset.y = Mathf.Abs(offset.y);
            var log = Instantiate(logPrefab, transform.position + Vector3.up * 0.3f + offset, Random.rotation);
            NetworkSpawnHelper.SpawnIfNetworked(log);

            if (log.TryGetComponent(out Rigidbody rb))
            {
                Vector3 dir = (Random.insideUnitSphere + Vector3.up).normalized;
                rb.AddForce(dir * scatterForce, ForceMode.Impulse);
            }
        }

        stumpActive = true;
        if (regrowDelay > 0f)
            regrowAt = Time.time + regrowDelay;
    }

    // Read-only — doesn't fell the tree. Lets NPCGathering check "could I
    // even carry this" via NPCEncumbrance.CanPickUp before committing.
    public bool PeekYield(out ItemDefinition item, out int count)
    {
        item = logItem;
        count = logCount;
        return IsAvailable && logItem != null;
    }

    // NPC-compatible fell (2026-08-13) — mirrors ResourceNode.
    // TryHarvestForNPC's exact split: no tool check (the caller already
    // verified RequiredTools against its own equipped tools), no scatter
    // (an NPC has no separate collect step), no skill-gain call (the
    // caller trains the assigned job's own family skill via SkillGain
    // above, not this tree's trainedSkill field — same "job's family
    // trains, not the node's" convention Mining already established).
    public bool TryHarvestForNPC(out ItemDefinition item, out int count)
    {
        if (!PeekYield(out item, out count)) return false;

        stumpActive = true;
        if (regrowDelay > 0f)
            regrowAt = Time.time + regrowDelay;
        return true;
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
        stumpActive = false;
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
