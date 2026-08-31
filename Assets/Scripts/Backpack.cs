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

    private string lastSyncedSignature = string.Empty;
    private readonly Dictionary<string, int> lastSyncedCounts = new Dictionary<string, int>();

    private void Awake()
    {
        inventory = new Inventory(capacity);
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        syncedSlots.Callback += OnSyncedSlotsChanged;
    }

    private void OnDestroy()
    {
        syncedSlots.Callback -= OnSyncedSlotsChanged;
    }

    private void Update()
    {
        if (!isServer) return;

        string signature = ComputeSignature();
        if (signature == lastSyncedSignature) return;

        lastSyncedSignature = signature;
        RefreshSyncedSlots();
    }

    private void OnSyncedSlotsChanged(SyncList<PlayerInventory.SyncedInventorySlot>.Operation op, int index,
        PlayerInventory.SyncedInventorySlot oldItem, PlayerInventory.SyncedInventorySlot newItem)
    {
        if (isServer) return;
        ApplySyncedSlotsToLocalInventory();
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

    public void Complete(GameObject player)
    {
        var carrier = player.GetComponent<PlayerBackpack>();
        carrier?.PickUp(this);
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
