using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Backpack : NetworkBehaviour, IInteractable, IInventoryHolder
{
    // Excluded from the player's own camera (see Main Camera's cullingMask in
    // TestScene) so worn gear doesn't fill the screen if you turn to look at
    // your own back. Only applied while worn — SetCarried resets it to Default
    // on drop/unequip so a world-sitting backpack stays visible.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int capacity = 8;

    private Inventory inventory;
    private Rigidbody body;
    private Collider col;

    public Inventory Inventory => inventory;
    public ItemDefinition ItemDefinition => itemDefinition;
    public string DisplayName => itemDefinition != null ? itemDefinition.itemName : "Backpack";

    public string Prompt => $"Pick up {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public bool CanEquipToSlot(string slotName) => slotName == "Back";

    // FIXED (2026-08-30, found live: no sync mechanism existed for a worn
    // Backpack's contents at all -- this was a plain MonoBehaviour with a
    // local-only Inventory, unlike PlayerInventory/PlayerEquipment which
    // both already got a real SyncList earlier this same multiplayer pass.
    // Same shape as PlayerInventory.syncedSlots (reusing its own public
    // SyncedInventorySlot struct directly rather than duplicating it) --
    // server polls a signature and rebuilds the list on change, client
    // applies only the delta so a client-local addition (nothing else in
    // this project's inventory system routes Backpack mutation through a
    // Command yet either) isn't destructively overwritten.
    public readonly SyncList<PlayerInventory.SyncedInventorySlot> syncedSlots =
        new SyncList<PlayerInventory.SyncedInventorySlot>();

    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): same gap as
    // PlayerInventory's own main inventory -- ComputeSignature/
    // RefreshSyncedSlots below skip equipment-backed slots entirely, so an
    // equippable stashed loose inside a worn Backpack (a Canteen, a Tool,
    // ...) never synced to a remote client. Reuses PlayerInventory's own
    // SyncedEquipmentInventorySlot struct and the identical identity-based
    // (netId) reconciliation shape -- see that class's own comment for the
    // full reasoning.
    public readonly SyncList<PlayerInventory.SyncedEquipmentInventorySlot> syncedEquipmentSlots =
        new SyncList<PlayerInventory.SyncedEquipmentInventorySlot>();

    private string lastSyncedSignature = string.Empty;
    private string lastSyncedEquipmentSignature = string.Empty;
    private readonly Dictionary<string, int> lastSyncedCounts = new Dictionary<string, int>();

    private void Awake()
    {
        inventory = new Inventory(capacity);
        body = GetComponent<Rigidbody>();
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
        // PlayerInventory's own equipment-sync retry: the equipped
        // object's spawn message can arrive after this SyncList update.
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

    // FIXED (2026-08-31) -- same delta-based rewrite as PlayerInventory's
    // own equipment reconciliation (see that class's own comment for full
    // reasoning): full-enforcement fought a local-only move into/out of a
    // Backpack the same way it fought moving into a StorageBox, since
    // neither is Command-routed yet. Now only acts on the CHANGE between
    // successive broadcasts.
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

    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): routed
    // through PlayerInventory.RequestPickUpEquipment (a real Command) --
    // used to call PlayerBackpack.PickUp directly, entirely client-side.
    public void Complete(GameObject player)
    {
        player.GetComponent<PlayerInventory>()?.RequestPickUpEquipment(this);
    }

    // Fully hides the object while it's stashed in a regular inventory slot
    // rather than sitting in the world or worn on the back.
    public void Stash()
    {
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Worn on the back (visible, non-collidable, follows the player) when
    // anchor is set, or released back into the world as a normal physical
    // object when anchor is null.
    public void SetCarried(bool value, Transform anchor)
    {
        if (value) Despawn.CancelOn(gameObject);

        gameObject.SetActive(true);
        col.enabled = !value;
        body.isKinematic = value;
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
}
