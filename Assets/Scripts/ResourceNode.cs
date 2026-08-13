using UnityEngine;

public class ResourceNode : MonoBehaviour, IInteractable, ISecondaryInteractable
{
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private int chunkCount = 3;
    [SerializeField] private float scatterForce = 1.2f;
    [SerializeField] private SkillDefinition trainedSkill;
    [SerializeField] private float skillGain = 0.5f;
    // <= 0 means this node doesn't respawn at all — it's destroyed
    // outright when broken instead. For a node that's itself a one-off
    // spawn (e.g. a Log dropped by a chopped Tree — see Tree.cs), there's
    // no sensible "same spot" to respawn at the way a fixed Boulder/ore
    // node has.
    [SerializeField] private float respawnDelay = 180f;
    [SerializeField] private float respawnScatter = 0.5f;

    // Optional extra spawn alongside the guaranteed chunkPrefab/chunkCount
    // — rolled once per break, independently of chunkCount. Null (default,
    // 0 chance) means no bonus, same "most nodes don't need this"
    // convention as CraftingRecipe.bonusItem — but unlike that one, this
    // IS a real chance (e.g. chopping a Log has a chance of also yielding
    // a Stick/branch), not a guarantee.
    [SerializeField] private GameObject bonusChunkPrefab;
    [SerializeField, Range(0f, 1f)] private float bonusChunkChance = 0f;

    // Null/empty (default) means no tool needed — punching bare-handed
    // works, same as Rock Node's existing behavior. Populate to gate a node
    // behind a specific tool (e.g. Copper Ore requires a Pickaxe, Tree
    // requires an Axe) — checked via PlayerEquipment.HasInHand, so the tool
    // has to actually be held in a hand, not just carried somewhere in
    // inventory. An array, not a single item, because a tool now comes in
    // 5 CraftTiers as of 2026-08-05 — any one of them satisfies the gate,
    // not just one specific tier's exact asset.
    [SerializeField] private ItemDefinition[] requiredTools;

    // Display name for the tool in Prompt below (e.g. "Pickaxe") —
    // independent of which exact tier is actually equipped, since any tier
    // in requiredTools satisfies the gate.
    [SerializeField] private string requiredToolLabel;

    // Null (default) means no disguise — this node always looks like
    // chunkPrefab's true resource and always yields it, same as every node
    // shipped before this. Set both materials to make a node look like
    // plain rock until the player has a Mining Face Shield equipped, at
    // which point it swaps to revealedMaterial — same reveal mechanism
    // already shipped for Sunglasses + the Secret Message Wall, generalized
    // into a gameplay effect instead of a pure visual one.
    [SerializeField] private Material hiddenMaterial;
    [SerializeField] private Material revealedMaterial;
    // What actually gets spawned if the node is broken while NOT revealed —
    // the ore goes undetected, so this should be a plain Small Rock chunk
    // prefab, not the real ore. Ignored entirely when hiddenMaterial is null.
    [SerializeField] private GameObject hiddenChunkPrefab;

    // Optional — lets this node also be picked up whole via the secondary
    // (F) interaction, as an alternative to breaking it via the primary
    // hold action. Null (default) means no secondary option — every
    // existing node (Boulders, ore, Tree) keeps its current single-action
    // behavior unchanged. Built for Log (2026-08-12): picking up the whole
    // log costs no tool, grants no skill XP (no skill/tool was actually
    // applied, unlike chopping), and removes the node outright — same
    // "no sensible respawn spot" reasoning as a one-off Log dropped by a
    // chopped Tree.
    [SerializeField] private ItemDefinition pickupItem;
    [SerializeField] private int pickupCount = 1;

    private Vector3 spawnPosition;
    private Collider col;
    private Renderer[] renderers;
    // -1 means "not counting down" — the node is either still standing
    // (the timer is held until it's actually broken) or mid-repositioning.
    private float respawnAt = -1f;

    // Looked up once rather than per-frame — this node isn't parented under
    // Player, so there's no cheap direct reference.
    private PlayerMiningFaceShield shieldWearer;

    public string Prompt => HasToolRequirement
        ? $"Hold to break (requires {requiredToolLabel})"
        : "Hold to break";

    public bool IsInstant => false;

    // Read by NPCMining (2026-08-10, Chunk 4 of the Hireable NPCs build)
    // to find/filter targets and check its own equipped tools without
    // needing PlayerEquipment, which it doesn't have.
    public bool IsAvailable => respawnAt < 0f;
    public ItemDefinition[] RequiredTools => requiredTools;
    public float SkillGain => skillGain;

    // Replaced the old hitsToBreak/punch-N-times model 2026-08-08 — see
    // design-brief.md's Interaction model note. Duration is skill-driven
    // (low tier takes longest), not fixed per node.
    public float GetHoldDuration(GameObject player) =>
        player.GetComponent<PlayerSkills>().GetHoldDuration(trainedSkill);

    private bool HasToolRequirement => requiredTools != null && requiredTools.Length > 0;

    private bool IsDisguised => hiddenMaterial != null;
    private bool IsRevealed => shieldWearer != null && shieldWearer.IsWorn;

    private void Awake()
    {
        spawnPosition = transform.position;
        col = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        if (IsDisguised)
            shieldWearer = FindFirstObjectByType<PlayerMiningFaceShield>();
    }

    private void Update()
    {
        if (IsDisguised)
        {
            var material = IsRevealed ? revealedMaterial : hiddenMaterial;
            foreach (var r in renderers)
                r.sharedMaterial = material;
        }

        if (respawnAt < 0f || Time.time < respawnAt) return;
        Respawn();
    }

    // Called once the hold completes (see PlayerInteraction) — replaces the
    // old repeated-OnPunch/hitsToBreak counter entirely, single-shot now
    // that the wait itself is the skill/tool gate.
    public void Complete(GameObject player)
    {
        if (HasToolRequirement)
        {
            var equipment = player.GetComponent<PlayerEquipment>();
            if (equipment == null || !HasAnyRequiredToolInHand(equipment)) return;
        }

        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);

        // Checked at the moment it actually breaks, not when the hold
        // started — a node revealed only partway through wouldn't make
        // sense to then punish, and this matches how every other tier/skill
        // check in this system is evaluated per-attempt, not locked in early.
        GameObject prefabToSpawn = (IsDisguised && !IsRevealed) ? hiddenChunkPrefab : chunkPrefab;

        for (int i = 0; i < chunkCount; i++)
            SpawnChunk(prefabToSpawn);

        if (bonusChunkPrefab != null && Random.value < bonusChunkChance)
            SpawnChunk(bonusChunkPrefab);

        if (respawnDelay <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        SetVisible(false);
        respawnAt = Time.time + respawnDelay;
    }

    // NPC-compatible break (2026-08-10, Chunk 4 of the Hireable NPCs
    // build). Mirrors Complete()'s yield/respawn logic but deliberately
    // skips the tool check -- NPCMining checks the NPC's own equipped
    // tools against RequiredTools itself before ever calling this, since
    // there's no PlayerEquipment to check here -- and returns the mined
    // item/count instead of spawning physical world pickups, since an NPC
    // has no separate "walk over and grab it" step to collect them with.
    // Always yields the true item, ignoring hiddenMaterial/disguise --
    // Mine Ore already requires a Mining Face Shield to even be assigned,
    // so treating an equipped NPC as always-revealed is a deliberate
    // simplification, not an oversight. bonusChunkPrefab is also skipped
    // for the same reason: keeps the NPC path predictable rather than
    // matching the player's exact roll-based yield.
    public bool TryMineForNPC(out ItemDefinition item, out int count)
    {
        if (!PeekYield(out item, out count)) return false;

        if (respawnDelay <= 0f)
        {
            Destroy(gameObject);
            return true;
        }

        SetVisible(false);
        respawnAt = Time.time + respawnDelay;
        return true;
    }

    // Read-only version of TryMineForNPC's yield resolution -- doesn't
    // break the node. Lets NPCMining check "could I even carry this" via
    // NPCEncumbrance.CanPickUp before committing to a target.
    //
    // Real ore nodes turn out to be multi-stage (discovered live verifying
    // Chunk 4, not assumed): Copper Ore Node's chunkPrefab is itself
    // ANOTHER ResourceNode (CopperOreChunk, no tool required), which only
    // THEN yields the real Pickup (CopperChunk, the actual CopperOre
    // item) -- mirroring the player's own two-step break-the-node-then-
    // break-the-chunk-then-pick-it-up flow. An NPC has no equivalent to
    // that multi-step physical process, so this walks the whole
    // chunkPrefab chain down to the real item and multiplies counts along
    // the way (3 chunks x 2 sub-chunks x 1 each = 6 total, for Copper Ore
    // Node) rather than stopping at the first (intermediate) stage. Same
    // guarded-depth-walk shape as IngredientMatching.Satisfies's baseItem
    // chain -- guards an accidental cycle in the data, not a real
    // expected case.
    private const int MaxChunkChainDepth = 5;

    public bool PeekYield(out ItemDefinition item, out int count)
    {
        item = null;
        count = 0;
        if (!IsAvailable) return false;
        return ResolveYieldChain(chunkPrefab, chunkCount, out item, out count, 0);
    }

    private static bool ResolveYieldChain(GameObject prefab, int multiplier, out ItemDefinition item, out int count, int depth)
    {
        item = null;
        count = 0;
        if (prefab == null || depth >= MaxChunkChainDepth) return false;

        if (prefab.TryGetComponent(out Pickup pickup))
        {
            if (pickup.Item == null) return false;
            item = pickup.Item;
            count = multiplier;
            return true;
        }

        if (prefab.TryGetComponent(out ResourceNode nested))
            return ResolveYieldChain(nested.chunkPrefab, multiplier * nested.chunkCount, out item, out count, depth + 1);

        return false;
    }

    private void SpawnChunk(GameObject prefab)
    {
        Vector3 offset = Random.insideUnitSphere * 0.3f;
        var chunk = Instantiate(prefab, transform.position + Vector3.up * 0.2f + offset, Random.rotation);

        if (chunk.TryGetComponent(out Rigidbody rb))
        {
            Vector3 dir = (Random.insideUnitSphere + Vector3.up).normalized;
            rb.AddForce(dir * scatterForce, ForceMode.Impulse);
        }
    }

    public string GetSecondaryPrompt(GameObject player) =>
        pickupItem != null ? $"Pick up {pickupItem.itemName}" : null;

    // Mirrors Pickup.Complete's leftover-handling exactly (PlayerLoot first,
    // PlayerInventory fallback, leave the node in place if there's nowhere
    // to put it) so a full inventory behaves identically whether the wood
    // came from a loose Pickup or this secondary action.
    public void CompleteSecondary(GameObject player)
    {
        if (pickupItem == null) return;

        var loot = player.GetComponent<PlayerLoot>();
        int leftover;
        if (loot != null)
        {
            leftover = loot.Receive(pickupItem, pickupCount);
        }
        else
        {
            var inventory = player.GetComponent<PlayerInventory>();
            leftover = inventory != null ? inventory.AddItem(pickupItem, pickupCount) : pickupCount;
        }

        if (leftover > 0) return;

        if (respawnDelay <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        SetVisible(false);
        respawnAt = Time.time + respawnDelay;
    }

    private bool HasAnyRequiredToolInHand(PlayerEquipment equipment)
    {
        foreach (var tool in requiredTools)
        {
            if (tool != null && equipment.HasInHand(tool)) return true;
        }
        return false;
    }

    // Same spot it started at, with a small random horizontal shift, fully
    // intact again so it can be broken again from scratch.
    private void Respawn()
    {
        Vector2 offset = Random.insideUnitCircle * respawnScatter;
        transform.position = spawnPosition + new Vector3(offset.x, 0f, offset.y);
        SetVisible(true);
        respawnAt = -1f;
    }

    private void SetVisible(bool visible)
    {
        if (col != null) col.enabled = visible;
        foreach (var r in renderers)
            r.enabled = visible;
    }
}
