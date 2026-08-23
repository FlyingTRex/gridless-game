using Mirror;
using UnityEngine;

// Multiplayer Phase 3 sub-phase 2 (MULTIPLAYER_PLANNING.md). First slice
// (2026-08-22): converted from MonoBehaviour to NetworkBehaviour as its
// own isolated step, base-class change only. Second slice, same session:
// a real SyncList mirroring this Inventory's plain stackable slots (item
// resolved by string ID, same as SaveManager/ItemDatabase.Find(id) already
// use for persistence -- Mirror doesn't natively sync a ScriptableObject
// reference). Equipment-carrying slots (a worn Backpack/Canteen/etc., a
// live GameObject+component, not just data) are deliberately excluded --
// same complexity boundary SAVE_LOAD_PLANNING.md already drew for
// persistence v1 ("full recursive nested-equipment capture" flagged as
// its hardest, separately-scoped piece). This slice only broadcasts
// server-owned state to observers -- it does NOT yet convert AddItem/
// RemoveItem (or the many other call sites that mutate `Inventory`
// directly via the exposed property) into Command-validated calls; that's
// still ahead.
[DisallowMultipleComponent]
public class PlayerInventory : NetworkBehaviour
{
    [System.Serializable]
    public struct SyncedInventorySlot
    {
        public string itemId;
        public int count;
    }

    [SerializeField] private int capacity = 4;

    private Inventory inventory;

    public Inventory Inventory => inventory;

    // Server-owned, broadcast to every observer. Rebuilt from `inventory`
    // whenever its signature changes -- polled rather than hooked into
    // every mutation site, since `Inventory` itself isn't instrumented
    // with change notifications and dozens of scripts mutate it directly
    // through the exposed `Inventory` property, not just through
    // AddItem/RemoveItem below. A known inefficiency (string-building
    // every frame to detect changes), acceptable for this first slice;
    // worth revisiting once real Command-driven mutation replaces most of
    // those direct call sites anyway.
    public readonly SyncList<SyncedInventorySlot> syncedSlots = new SyncList<SyncedInventorySlot>();

    private string lastSyncedSignature = string.Empty;

    private void Awake()
    {
        inventory = new Inventory(capacity);
    }

    private void Update()
    {
        if (!isServer) return;

        string signature = ComputeSignature();
        if (signature == lastSyncedSignature) return;

        lastSyncedSignature = signature;
        RefreshSyncedSlots();
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
            syncedSlots.Add(new SyncedInventorySlot { itemId = id, count = slot.count });
        }
    }

    // Returns the amount that did NOT fit (0 means everything was added).
    public int AddItem(ItemDefinition item, int quantity) => inventory.AddItem(item, quantity);

    public bool RemoveItem(ItemDefinition item, int quantity) => inventory.RemoveItem(item, quantity);

    public int GetCount(ItemDefinition item) => inventory.GetCount(item);

    // Third slice, same session -- proves the actual Command/validate/
    // apply shape works end to end now that Player has real connection
    // authority (GridlessNetworkManager.OnServerReady). Live-confirmed via
    // a temporary debug keybind (removed): client requested, server
    // validated and applied, correct item/quantity. Not yet wired into
    // any real caller (Pickup.cs and the rest still call AddItem directly,
    // which works fine host-alone but isn't a networked request from a
    // remote client) -- this is the proof the mechanism itself works
    // before converting real call sites over to it.
    public void RequestAddItem(ItemDefinition item, int quantity)
    {
        string id = ItemDatabase.Instance.IdFor(item);
        if (id == null) return;
        CmdAddItemById(id, quantity);
    }

    [Command]
    private void CmdAddItemById(string itemId, int quantity)
    {
        var item = ItemDatabase.Instance.Find(itemId);
        if (item == null || quantity <= 0) return;
        AddItem(item, quantity);
    }
}
