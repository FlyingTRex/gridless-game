using System.Collections.Generic;
using Mirror;
using UnityEngine;

// A stationary world container (a chest/box placed in the level). Doesn't
// implement IInteractable — there's no pickup/use prompt. Instead
// InventoryScreen auto-detects any box within range every frame the Inventory
// tab of PlayerMenuScreen is open and draws its contents alongside the
// player's own inventory, so storing/retrieving items just means walking up
// and pressing Tab.
//
// IEquippable added 2026-08-21 (found live: picking a box up and dropping it
// again lost its custom name and floated on landing). The old pickup path
// converted the box into a bare pickupItem stack via PlayerLoot.Receive,
// destroying this exact GameObject/component -- boxName, SaveId, everything
// was gone the moment it was picked up. Reusing the same Stash/SetCarried
// mechanism Backpack/Canteen already use means the original GameObject
// persists (just hidden) while carried, so its name and identity survive
// automatically -- no new state-carrying plumbing needed. Re-placing it is
// a real aimed placement through PlayerBuilding (see ArmExistingPiece),
// not a physical "drop" -- a StorageBox is a permanent structure, not
// temporary carried gear, so it shouldn't get PlayerDropping's despawn
// timer or need a Rigidbody just to physically settle on landing.
[RequireComponent(typeof(SaveId))]
[RequireComponent(typeof(Collider))]
public class StorageBox : NetworkBehaviour, IRenameable, IInteractable, IEquippable, IInventoryHolder
{
    // Matches Backpack/other equippables' WornEquipment-layer convention —
    // excluded from the player's own camera while carried so it doesn't
    // fill the screen if the player looks down at their own hands.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    private Collider col;

    // Every enabled box registers here so InventoryScreen can find nearby
    // ones with a simple distance check instead of a physics query.
    public static readonly List<StorageBox> Active = new List<StorageBox>();

    // FIXED (2026-08-26, found live with traskmi): was a plain field --
    // a rename never replicated to anyone but whoever performed it. See
    // BUGS_AND_ENHANCEMENTS.md. SyncVar + NetworkBehaviour is the whole
    // fix; PlayerRenaming now routes the actual write through a Command.
    [SyncVar] private string boxName = "Storage Box";
    [SerializeField] private int capacity = 20;

    // The portable ItemDefinition this box becomes when picked up (see
    // Complete below). Its own worldPickupPrefab points right back at this
    // same StorageBox prefab — dropping/placing it later spawns a real,
    // working box again, not an inert prop. Null (unset) means this
    // instance simply can't be picked up (e.g. if a future variant
    // shouldn't be portable) — Complete no-ops in that case.
    [SerializeField] private ItemDefinition pickupItem;

    // The Bookshelf (NPC_TRAINING_PLANNING.md, 2026-08-16) is deliberately
    // just a flagged StorageBox, not a separate component -- it needs the
    // exact same rename/pickup/InventoryScreen-auto-detection behavior a
    // plain box already has, just restricted to skill books. True computes
    // restrictedTo from a live ItemDatabase scan at Awake (see below)
    // instead of a hand-authored item list, so any future skill-book item
    // is automatically allowed with no per-instance authoring needed.
    [SerializeField] private bool restrictToSkillBooks;

    // Ownership gate (2026-08-21, MVP2B_PLANNING.md item 1) -- found in a
    // "be mean" pass before building VendorStall: this box has no access
    // concept at all today, any visitor can already walk up and open one
    // directly (via InventoryScreen's proximity auto-detection, not even
    // gated by IInteractable) or pick it up outright once empty. Reusing
    // a plain StorageBox as a vendor's stock container as designed would
    // make the entire buy/sell mechanic pointless -- just open the box
    // and take everything for free. Defaults true so every existing and
    // future player-placed box keeps working exactly as before (zero
    // regression) -- a VendorStall's own stock box is the first instance
    // ever explicitly created with this false. A plain bool is enough for
    // single-player today; the check is isolated to FindNearby/Complete
    // below specifically so it can become a real owner-identity/Team-
    // membership check later without touching every call site again.
    [SerializeField] private bool isPlayerOwned = true;

    private Inventory inventory;

    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): found while
    // checking whether the just-fixed PlayerInventory/Backpack equipment-
    // sync gap (Pattern D) also applied here -- it turned out worse.
    // StorageBox had NO sync mechanism at all for its contents, not just
    // equipment-backed slots -- a remote client's own local `inventory`
    // was whatever it started as (empty, from Awake's fresh Inventory()
    // call) and never once heard from the server's real contents. Same
    // dual SyncList shape Backpack.cs already established (reusing
    // PlayerInventory's own struct types) -- gives this box both the
    // plain-item sync it never had and the equipment-item sync in one
    // pass, rather than fixing them separately.
    public readonly SyncList<PlayerInventory.SyncedInventorySlot> syncedSlots =
        new SyncList<PlayerInventory.SyncedInventorySlot>();
    public readonly SyncList<PlayerInventory.SyncedEquipmentInventorySlot> syncedEquipmentSlots =
        new SyncList<PlayerInventory.SyncedEquipmentInventorySlot>();

    private string lastSyncedSignature = string.Empty;
    private string lastSyncedEquipmentSignature = string.Empty;
    private readonly Dictionary<string, int> lastSyncedCounts = new Dictionary<string, int>();

    public string DisplayName => boxName;
    public Inventory Inventory => inventory;
    public bool IsPlayerOwned => isPlayerOwned;
    public void SetPlayerOwned(bool value) => isPlayerOwned = value;
    // Read by PlayerBuilding.ArmExistingPiece's cancel path (2026-08-21) to
    // restore this box back into the player's inventory via
    // Inventory.AddEquipmentItem, same convention as Backpack.ItemDefinition.
    public ItemDefinition PickupItem => pickupItem;

    // Ben's call (2026-08-09): must be empty to pick up — simple and safe,
    // no risk of silently losing stored items. No tool required, unlike
    // PlayerPieceUpgrade's Hammer-gated upgrade/destroy on build pieces —
    // this is a plain "pick up my furniture" interaction. StorageBox *is*
    // also placed through PlayerBuilding today (StorageBoxPiece.asset,
    // added since this comment was first written) so a placed instance
    // does carry a real PlacedPiece — Complete() below doesn't touch that
    // component at all, it's preserved across the whole pickup/re-place
    // round trip via ArmExistingPiece.
    public string Prompt => !isPlayerOwned
        ? $"{boxName} (not yours)"
        : inventory != null && inventory.Slots.Count > 0
            ? $"{boxName} (must be empty to pick up)"
            : $"Pick up {boxName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    // Never offered as an explicit body-slot equip target (Chest/Back/
    // Waist/etc.) via drag-and-drop — a StorageBox only ever rides in a
    // hand slot or a Backpack's own cargo as a side effect of ReceiveEquipment's
    // normal priority order, same as Belt/Boot already do.
    public bool CanEquipToSlot(string slotName) => false;

    // Mirrors PlayerBackpack.PickUp's exact fallback shape: try
    // PlayerLoot's normal equipment-pickup priority (equipped Backpack,
    // then a free hand) first, then fall back to stashing directly into
    // the main inventory as a last resort before giving up.
    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): routed
    // through PlayerInventory.RequestPickUpEquipment (a real Command) --
    // used to run this whole method (including the isPlayerOwned/empty
    // guard below) entirely client-side. The guard itself is re-checked
    // server-side too (PlayerInventory.CmdPickUpEquipment), not just here
    // — this client-side copy stays only so a genuinely non-pickupable box
    // doesn't even attempt the round trip.
    public void Complete(GameObject player)
    {
        if (!isPlayerOwned || pickupItem == null || inventory.Slots.Count > 0) return;

        player.GetComponent<PlayerInventory>()?.RequestPickUpEquipment(this);
    }

    // Fully hides the object while it's stashed in a regular inventory
    // slot rather than sitting placed in the world or carried in hand —
    // same shape as Backpack.Stash.
    public void Stash()
    {
        gameObject.SetActive(false);
        transform.SetParent(null, false);
    }

    // Carried (visible, non-collidable, follows anchor — usually the
    // player root, since a box has no dedicated hand-bone attach point)
    // when anchor is set. anchor == null is the re-placement half of this
    // round trip: PlayerBuilding.Confirm sets the real world position/
    // rotation immediately after this call, same as it already does for
    // a freshly-built piece — this method only handles visibility/
    // parenting/collision, never position, matching every other
    // IEquippable's own convention.
    public void SetCarried(bool value, Transform anchor)
    {
        gameObject.SetActive(true);
        col.enabled = !value;
        SetLayerRecursively(transform, value ? WornEquipmentLayer : DefaultLayer);

        if (value)
        {
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            transform.SetParent(null, true);
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }

    private void Awake()
    {
        inventory = new Inventory(capacity, restrictToSkillBooks ? ComputeSkillBookItems() : null);
        col = GetComponent<Collider>();
        syncedSlots.Callback += OnSyncedSlotsChanged;
        syncedEquipmentSlots.Callback += OnSyncedEquipmentSlotsChanged;
    }

    private void OnDestroy()
    {
        syncedSlots.Callback -= OnSyncedSlotsChanged;
        syncedEquipmentSlots.Callback -= OnSyncedEquipmentSlotsChanged;
    }

    private void Update()
    {
        if (isServer)
        {
            string signature = ComputeSignature();
            if (signature != lastSyncedSignature)
            {
                lastSyncedSignature = signature;
                RefreshSyncedSlots();
            }

            string equipmentSignature = ComputeEquipmentSignature();
            if (equipmentSignature != lastSyncedEquipmentSignature)
            {
                lastSyncedEquipmentSignature = equipmentSignature;
                RefreshSyncedEquipmentSlots();
            }
            return;
        }

        // Client-side retry -- same reasoning as PlayerEquipment/
        // PlayerInventory/Backpack's own equipment-sync retry: the
        // equipped object's spawn message can arrive after this SyncList
        // update.
        ApplySyncedEquipmentSlotsToLocalInventory();
    }

    private void OnSyncedSlotsChanged(SyncList<PlayerInventory.SyncedInventorySlot>.Operation op, int index,
        PlayerInventory.SyncedInventorySlot oldItem, PlayerInventory.SyncedInventorySlot newItem)
    {
        if (isServer) return;
        ApplySyncedSlotsToLocalInventory();
    }

    private void OnSyncedEquipmentSlotsChanged(SyncList<PlayerInventory.SyncedEquipmentInventorySlot>.Operation op, int index,
        PlayerInventory.SyncedEquipmentInventorySlot oldItem, PlayerInventory.SyncedEquipmentInventorySlot newItem)
    {
        if (isServer) return;
        ApplySyncedEquipmentSlotsToLocalInventory();
    }

    // Additive-delta reconciliation, same shape/reasoning as
    // PlayerInventory.ApplySyncedSlotsToLocalInventory -- an item added to
    // this box via a path that isn't yet Command-routed (deposits still
    // aren't, see MULTIPLAYER_INTERACTION_AUDIT.md's open items) stays
    // untouched rather than being silently wiped by a destructive rebuild.
    private void ApplySyncedSlotsToLocalInventory()
    {
        var newCounts = new Dictionary<string, int>();
        foreach (var slot in syncedSlots)
        {
            if (string.IsNullOrEmpty(slot.itemId)) continue;
            newCounts[slot.itemId] = newCounts.TryGetValue(slot.itemId, out var c) ? c + slot.count : slot.count;
        }

        var allItemIds = new HashSet<string>(lastSyncedCounts.Keys);
        allItemIds.UnionWith(newCounts.Keys);

        foreach (var itemId in allItemIds)
        {
            int oldCount = lastSyncedCounts.TryGetValue(itemId, out var oc) ? oc : 0;
            int newCount = newCounts.TryGetValue(itemId, out var nc) ? nc : 0;
            int delta = newCount - oldCount;
            if (delta == 0) continue;

            var item = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(itemId) : null;
            if (item == null) continue;

            inventory.ApplyStackableDelta(item, delta);
        }

        lastSyncedCounts.Clear();
        foreach (var kvp in newCounts) lastSyncedCounts[kvp.Key] = kvp.Value;
    }

    private string ComputeSignature()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var slot in inventory.Slots)
        {
            if (slot.equipment != null) continue;
            sb.Append(slot.item != null ? slot.item.name : "null").Append(':').Append(slot.count).Append('|');
        }
        return sb.ToString();
    }

    private void RefreshSyncedSlots()
    {
        syncedSlots.Clear();
        foreach (var slot in inventory.Slots)
        {
            if (slot.equipment != null) continue;
            string id = ItemDatabase.Instance.IdFor(slot.item);
            if (id == null) continue;
            syncedSlots.Add(new PlayerInventory.SyncedInventorySlot { itemId = id, count = slot.count });
        }
    }

    private static uint NetIdFor(IEquippable equipment)
    {
        if (equipment is Component component && component != null && component.TryGetComponent(out NetworkIdentity identity))
            return identity.netId;
        return 0;
    }

    private string ComputeEquipmentSignature()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var slot in inventory.Slots)
        {
            if (slot.equipment == null) continue;
            sb.Append(NetIdFor(slot.equipment)).Append('|');
        }
        return sb.ToString();
    }

    private void RefreshSyncedEquipmentSlots()
    {
        syncedEquipmentSlots.Clear();
        foreach (var slot in inventory.Slots)
        {
            if (slot.equipment == null) continue;
            uint netId = NetIdFor(slot.equipment);
            if (netId == 0) continue;
            string id = ItemDatabase.Instance.IdFor(slot.item);
            if (id == null) continue;
            syncedEquipmentSlots.Add(new PlayerInventory.SyncedEquipmentInventorySlot { itemId = id, netId = netId });
        }
    }

    // FIXED (2026-08-31, found live: dragging a Stone Knife into a box
    // snapped it back into the player's own main inventory) -- same
    // delta-based rewrite as PlayerInventory's own equipment
    // reconciliation (see that class's own comment for full reasoning):
    // full-enforcement fought a local-only move into this box, since
    // depositing isn't Command-routed yet. Now only acts on the CHANGE
    // between successive broadcasts.
    private readonly HashSet<uint> lastSyncedEquipmentNetIds = new HashSet<uint>();

    private void ApplySyncedEquipmentSlotsToLocalInventory()
    {
        var newNetIds = new HashSet<uint>();
        var itemIdByNetId = new Dictionary<uint, string>();
        foreach (var entry in syncedEquipmentSlots)
        {
            if (entry.netId == 0) continue;
            newNetIds.Add(entry.netId);
            itemIdByNetId[entry.netId] = entry.itemId;
        }

        foreach (var netId in lastSyncedEquipmentNetIds)
        {
            if (newNetIds.Contains(netId)) continue;
            RemoveLocalEquipmentByNetId(netId);
        }

        foreach (var netId in newNetIds)
        {
            if (lastSyncedEquipmentNetIds.Contains(netId)) continue;
            if (LocalHasEquipmentNetId(netId)) continue;
            if (!NetworkClient.spawned.TryGetValue(netId, out var identity)) continue;

            var equipment = identity.GetComponent(typeof(IEquippable)) as IEquippable;
            var item = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(itemIdByNetId[netId]) : null;
            if (equipment == null || item == null) continue;

            inventory.AddEquipmentItem(item, equipment);
        }

        lastSyncedEquipmentNetIds.Clear();
        foreach (var id in newNetIds) lastSyncedEquipmentNetIds.Add(id);
    }

    private bool LocalHasEquipmentNetId(uint netId)
    {
        foreach (var slot in inventory.Slots)
            if (slot.equipment != null && NetIdFor(slot.equipment) == netId) return true;
        return false;
    }

    private void RemoveLocalEquipmentByNetId(uint netId)
    {
        foreach (var slot in inventory.Slots)
        {
            if (slot.equipment != null && NetIdFor(slot.equipment) == netId)
            {
                inventory.RemoveEquipmentItem(slot.item);
                return;
            }
        }
    }

    // Every ItemDefinition whose worldPickupPrefab carries a SkillBook
    // component, computed fresh from ItemDatabase every time a restricted
    // Bookshelf wakes up -- not cached, not authored per-instance. A new
    // skill-book item is automatically allowed the moment it exists, same
    // "auto-populated, not hand-maintained" requirement DatabaseRepopulator
    // already established for the database itself (EFFICIENCY_AUDIT.md).
    private static ItemDefinition[] ComputeSkillBookItems()
    {
        var database = ItemDatabase.Instance;
        if (database == null) return null;

        var result = new List<ItemDefinition>();
        foreach (var item in database.AllItems)
        {
            if (item == null || item.worldPickupPrefab == null) continue;
            if (item.worldPickupPrefab.GetComponent<SkillBook>() != null)
                result.Add(item);
        }
        return result.ToArray();
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    // Every active box within range of position, nearest first. Shared by
    // InventoryScreen (the "(nearby)" contents section, storage picker)
    // and PlayerCrafting (letting a recipe draw on a nearby box's
    // materials) so both use the exact same distance rule.
    public static void FindNearby(Vector3 position, float range, List<StorageBox> result)
    {
        result.Clear();
        float rangeSq = range * range;

        foreach (var box in Active)
        {
            if (box == null || !box.isPlayerOwned) continue;
            float distSq = (box.transform.position - position).sqrMagnitude;
            if (distSq <= rangeSq)
                result.Add(box);
        }

        result.Sort((a, b) =>
            (a.transform.position - position).sqrMagnitude
                .CompareTo((b.transform.position - position).sqrMagnitude));
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        boxName = newName.Trim();
    }
}
