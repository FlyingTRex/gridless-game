using Mirror;
using UnityEngine;

// Multiplayer Phase 3 sub-phase 2 rollout (2026-08-23): most
// worldPickupPrefab prefabs (78 of 127, the rest being the ~10
// equippable types with their own carrier-based flow) now carry a
// NetworkIdentity. Complete() below routes through a server-
// authoritative Command when networked, reusing the exact same logic
// this class already had (see ServerComplete) -- one shared conversion
// covering all 78 prefabs at once, not 78 separate ones, since they all
// share this one script. Falls back to the original local-only path for
// any prefab that doesn't have a NetworkIdentity (the 49 skipped ones,
// or any future addition that hasn't been converted yet).
public class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int quantity = 1;
    [SerializeField] private SkillDefinition trainedSkill;
    [SerializeField] private float skillGain = 0.05f;

    // Only set on persistent world resource points (e.g. the Stick
    // pickups) — never on items the player dropped or that scattered out
    // of a broken ResourceNode, which shouldn't reappear on their own.
    [SerializeField] private bool canRespawn = false;
    [SerializeField] private float respawnDelay = 180f;
    [SerializeField] private float respawnScatter = 0.5f;

    // A player-dropped item (manual Drop, unequip-with-full-inventory
    // fallback, or hand-eviction on pickup) despawns from the world if left
    // unpicked this long. Distinct from respawnDelay above — this deletes
    // a dropped item outright rather than restoring a resource point.
    private const float DespawnDelay = 120f; // 2 minutes

    private Vector3 spawnPosition;
    private Collider col;
    private Renderer[] renderers;
    // -1 means "not counting down" — either still sitting there unpicked
    // (the timer is held until something actually takes it) or respawn
    // isn't enabled at all.
    private float respawnAt = -1f;
    // -1 means "not a dropped item" — only Configure() (called exclusively
    // by PlayerDropping.DropFrom) starts this countdown; world-placed
    // pickups set up directly in the scene/prefab never get one.
    private float despawnAt = -1f;

    // Read by ResourceNode.TryHarvestForNPC to learn what a node's chunkPrefab
    // actually represents without needing to instantiate it first.
    public ItemDefinition Item => item;

    // Read by NPCGathering (2026-08-13) — same "job's family skill trains,
    // not the target's own trainedSkill field" convention ResourceNode/
    // ChoppableTree's SkillGain accessors already established.
    public float SkillGain => skillGain;

    // Peek-only, doesn't consume — lets NPCGathering weigh a candidate
    // (NPCEncumbrance.CanPickUp) before committing to it, same reason
    // ResourceNode has PeekYield alongside TryHarvestForNPC.
    public int Quantity => quantity;

    public string Prompt => item != null ? $"Pick up {item.itemName}" : "Pick up";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    private void Awake()
    {
        spawnPosition = transform.position;
        col = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void Configure(ItemDefinition newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;
        despawnAt = Time.time + DespawnDelay;
    }

    private void Update()
    {
        if (despawnAt >= 0f && Time.time >= despawnAt)
        {
            DestroySelf();
            return;
        }

        if (respawnAt < 0f || Time.time < respawnAt) return;
        Respawn();
    }

    // Networked-aware despawn, shared by every place this script deletes
    // itself outright (as opposed to the canRespawn hide-and-reappear
    // path, which never destroys the object at all). Found live,
    // 2026-08-27/28: 3 of 4 call sites used a plain local Destroy() —
    // correct for the ~49 still-unconverted prefabs with no
    // NetworkIdentity, but for one of the 78 that DO have one, a plain
    // Destroy() only removes it on whichever machine called it, leaving
    // it visibly stuck in the world for every other observer (matches
    // the "box stayed visible after traskmi picked it up" report).
    private void DestroySelf()
    {
        bool networked = TryGetComponent<NetworkIdentity>(out _);
        DebugLog.Write("Pickup", $"DestroySelf {name}: networked={networked} NetworkServer.active={NetworkServer.active} -> {(networked && NetworkServer.active ? "NetworkServer.Destroy" : "local Destroy")}");
        if (networked && NetworkServer.active)
            NetworkServer.Destroy(gameObject);
        else
            Destroy(gameObject);
    }

    public void Complete(GameObject player)
    {
        bool networked = TryGetComponent<NetworkIdentity>(out _);
        DebugLog.Write("Pickup", $"Complete {name} item={item?.itemName} qty={quantity}: networked={networked} NetworkClient.active={NetworkClient.active}");
        if (networked && NetworkClient.active)
        {
            var inv = player.GetComponent<PlayerInventory>();
            DebugLog.Write("Pickup", $"  -> routing via RequestCompletePickup, playerInventory found={inv != null}");
            inv?.RequestCompletePickup(this);
            return;
        }

        DebugLog.Write("Pickup", "  -> local-only path, calling ServerComplete directly");
        ServerComplete(player);
    }

    // The real pickup-resolution logic -- item/loot handoff, skill gain,
    // and either despawn or respawn-hide. Runs directly (local-only path)
    // for any prefab without a NetworkIdentity, or server-side only (via
    // PlayerInventory.CmdCompletePickup) for a networked one -- either
    // way this is the single source of truth for what "completing" a
    // pickup actually does, not duplicated logic.
    public void ServerComplete(GameObject player)
    {
        DebugLog.Write("Pickup", $"ServerComplete {name} item={item?.itemName} qty={quantity} player={player.name} isServer-context-check: NetworkServer.active={NetworkServer.active}");
        var loot = player.GetComponent<PlayerLoot>();
        int leftover;
        if (loot != null)
        {
            leftover = loot.Receive(item, quantity);
            DebugLog.Write("Pickup", $"  via PlayerLoot.Receive, leftover={leftover}");
        }
        else
        {
            var inventory = player.GetComponent<PlayerInventory>();
            leftover = inventory != null ? inventory.AddItem(item, quantity) : quantity;
            DebugLog.Write("Pickup", $"  via PlayerInventory.AddItem (no PlayerLoot found), leftover={leftover}");
        }

        if (leftover > 0)
        {
            // Nowhere to put it (backpack/hands full with PlayerLoot, or
            // inventory full without it) — leave the remainder on the
            // ground instead of deleting it.
            DebugLog.Write("Pickup", $"  leftover > 0, NOT destroying, item stays on ground with qty={leftover}");
            quantity = leftover;
            return;
        }

        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);

        if (canRespawn)
        {
            SetVisible(false);
            respawnAt = Time.time + respawnDelay;
        }
        else
        {
            DestroySelf();
        }
    }

    // NPC-compatible pickup (2026-08-13, see
    // NPC_JOB_GENERALIZATION_PLANNING.md section 3a) — mirrors
    // ResourceNode.TryHarvestForNPC's shape: no PlayerLoot/PlayerInventory
    // dependency, no skill-gain call inside (the caller trains the
    // assigned job's own family skill via SkillGain above, not this
    // pickup's trainedSkill field). Always takes the full quantity — an
    // NPC's cargo capacity was already checked by the caller
    // (NPCEncumbrance.CanPickUp) before committing to this target, unlike
    // the player path's partial-leftover handling above.
    public bool TryPickupForNPC(out ItemDefinition pickedItem, out int pickedQuantity)
    {
        pickedItem = item;
        pickedQuantity = quantity;
        if (item == null) return false;

        if (canRespawn)
        {
            SetVisible(false);
            respawnAt = Time.time + respawnDelay;
        }
        else
        {
            DestroySelf();
        }

        return true;
    }

    // Same spot it started at, with a small random horizontal shift so
    // respawns don't all land pixel-identical to the original.
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
