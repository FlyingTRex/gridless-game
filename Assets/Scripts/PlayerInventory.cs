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

    // Fourth slice (2026-08-23) -- the real equip/unequip Command: moves
    // an item between the main inventory and a named PlayerEquipment body
    // slot (or the reverse), reusing InventoryTransfer.Move rather than
    // reimplementing its already-correct equipment-aware logic (see
    // InventoryTransfer.cs's own header on why a plain RemoveItem/AddItem
    // pair would silently drop an equipped instance's carrier reference).
    // Containers are identified by a simple string key ("main" for this
    // Inventory, a PlayerEquipment slot name, or "worn:<slotName>" for
    // that slot's worn IInventoryHolder's own nested Inventory -- e.g. a
    // worn Backpack's contents) since a Command can't carry a raw
    // Inventory reference across the wire. Live-confirmed via a
    // temporary debug keybind (removed): both directions moved
    // correctly, the equipment slot visibly updated, and the item
    // re-stacked correctly back in the main inventory.
    //
    // Second slice (2026-08-23) -- extended to the "worn:" case and
    // wired into InventoryScreen.cs's generic drag-drop path
    // (TryDrop/ContainerKeyFor) for the single most common non-main
    // container: a worn Backpack's nested inventory. Still scope-
    // bounded to containers resolvable from the Player's own
    // NetworkBehaviours -- Furnace zones and NPC cargo aren't Player
    // state at all and stay local-only, a separate, larger task
    // (Furnace isn't even a NetworkBehaviour yet, and NPCs are an
    // entirely later phase per MULTIPLAYER_PLANNING.md).
    // MoveAsManyAsFit (not the fixed-quantity Move) to match
    // InventoryTransfer's own local-path semantics exactly -- a drag
    // that doesn't fully fit partially succeeds instead of failing
    // outright, same as it always has locally.
    public void RequestMove(string fromContainer, string toContainer, ItemDefinition item, int quantity)
    {
        string id = ItemDatabase.Instance.IdFor(item);
        if (id == null) return;
        CmdMoveItem(fromContainer, toContainer, id, quantity);
    }

    [Command]
    private void CmdMoveItem(string fromContainer, string toContainer, string itemId, int quantity)
    {
        var item = ItemDatabase.Instance.Find(itemId);
        if (item == null || quantity <= 0) return;

        var equipment = GetComponent<PlayerEquipment>();
        Inventory from = ResolveContainer(fromContainer, equipment);
        Inventory to = ResolveContainer(toContainer, equipment);
        if (from == null || to == null) return;

        InventoryTransfer.MoveAsManyAsFit(from, to, item, quantity);
    }

    // Public wrapper so other Player NetworkBehaviours' own Commands (e.g.
    // PlayerEating's RequestEatFrom) can resolve a container key without
    // duplicating ResolveContainer's logic -- same container-key scheme,
    // one source of truth.
    public Inventory ResolveContainerByKey(string key) => ResolveContainer(key, GetComponent<PlayerEquipment>());

    private Inventory ResolveContainer(string key, PlayerEquipment equipment)
    {
        if (key == "main") return inventory;
        if (equipment == null) return null;

        if (key.StartsWith("worn:"))
        {
            string slotName = key.Substring("worn:".Length);
            return (equipment.GetEquipped(slotName) as IInventoryHolder)?.Inventory;
        }

        return equipment.GetSlot(key);
    }

    // Fifth slice (2026-08-23) -- routes a networked Pickup's Complete()
    // through a server-authoritative Command, reusing Pickup.ServerComplete
    // unchanged. Identifies the target by its NetworkIdentity, same
    // pattern as PlayerBackpack's equip/unequip Commands.
    public void RequestCompletePickup(Pickup pickup)
    {
        if (pickup == null || !pickup.TryGetComponent(out NetworkIdentity identity)) return;
        CmdCompletePickup(identity);
    }

    [Command]
    private void CmdCompletePickup(NetworkIdentity pickupIdentity)
    {
        var pickup = pickupIdentity != null ? pickupIdentity.GetComponent<Pickup>() : null;
        pickup?.ServerComplete(gameObject);
    }

    // Sixth slice (2026-08-23) -- ONE generic equip/unequip Command
    // covering every equippable type, mirroring InventoryScreen.cs's own
    // EquipToSlotDispatch/UnequipDispatch switch statements, rather than
    // a separate Command pair per carrier the way the earlier Backpack
    // pilot did (that approach doesn't scale to ~10 more types).
    // Simplification vs. the real UI: always removes from the main
    // inventory, not wherever the item might actually be sitting (a
    // Backpack's nested Inventory, a worn Belt, etc.) -- the same known
    // limitation the underlying carriers' own single-source overloads
    // already have documented, not a new gap introduced here. Live-
    // confirmed via a temporary debug keybind (removed): equipped a real
    // Belt (Waist slot) and a real Boot (Feet slot) through this same
    // shared Command, zero exceptions -- proves the generic dispatch
    // correctly covers multiple distinct carrier types, not just one.
    public void RequestEquipInstance(IEquippable equipment, string slotName)
    {
        if (equipment == null || equipment is not Component component) return;
        if (!component.TryGetComponent(out NetworkIdentity identity)) return;
        CmdEquipInstance(identity, slotName);
    }

    [Command]
    private void CmdEquipInstance(NetworkIdentity itemIdentity, string slotName)
    {
        var equipment = itemIdentity != null ? itemIdentity.GetComponent(typeof(IEquippable)) as IEquippable : null;
        if (equipment == null || !equipment.CanEquipToSlot(slotName)) return;

        switch (equipment)
        {
            case Backpack backpack: GetComponent<PlayerBackpack>()?.Equip(backpack, inventory); break;
            case Belt belt: GetComponent<PlayerBelt>()?.Equip(belt, inventory); break;
            case Boot boot: GetComponent<PlayerBoot>()?.Equip(boot, inventory); break;
            case Sunglasses sunglasses: GetComponent<PlayerSunglasses>()?.Equip(sunglasses, inventory); break;
            case MiningFaceShield shield: GetComponent<PlayerMiningFaceShield>()?.Equip(shield, inventory); break;
            case Canteen canteen: GetComponent<PlayerCanteen>()?.EquipTo(canteen, slotName, inventory); break;
            case NavigationComputer navComputer: GetComponent<PlayerNavComputer>()?.EquipTo(navComputer, slotName, inventory); break;
            case PersonalHealthMonitor monitor: GetComponent<PlayerHealthMonitor>()?.EquipTo(monitor, slotName, inventory); break;
            case Tool tool: GetComponent<PlayerTool>()?.EquipTo(tool, slotName, inventory); break;
            case Shirt shirt: GetComponent<PlayerShirt>()?.Equip(shirt, inventory); break;
            case Jeans jeans: GetComponent<PlayerJeans>()?.Equip(jeans, inventory); break;
        }
    }

    public void RequestUnequipInstance(IEquippable equipment)
    {
        if (equipment == null || equipment is not Component component) return;
        if (!component.TryGetComponent(out NetworkIdentity identity)) return;
        CmdUnequipInstance(identity);
    }

    [Command]
    private void CmdUnequipInstance(NetworkIdentity itemIdentity)
    {
        var equipment = itemIdentity != null ? itemIdentity.GetComponent(typeof(IEquippable)) as IEquippable : null;
        if (equipment == null) return;

        switch (equipment)
        {
            case Backpack backpack: GetComponent<PlayerBackpack>()?.Unequip(backpack); break;
            case Belt belt: GetComponent<PlayerBelt>()?.Unequip(belt); break;
            case Boot boot: GetComponent<PlayerBoot>()?.Unequip(boot); break;
            case Canteen canteen: GetComponent<PlayerCanteen>()?.Unequip(canteen); break;
            case NavigationComputer navComputer: GetComponent<PlayerNavComputer>()?.Unequip(navComputer); break;
            case PersonalHealthMonitor monitor: GetComponent<PlayerHealthMonitor>()?.Unequip(monitor); break;
            case Sunglasses sunglasses: GetComponent<PlayerSunglasses>()?.Unequip(sunglasses); break;
            case MiningFaceShield shield: GetComponent<PlayerMiningFaceShield>()?.Unequip(shield); break;
            case Tool tool: GetComponent<PlayerTool>()?.Unequip(tool); break;
            case Shirt shirt: GetComponent<PlayerShirt>()?.Unequip(shirt); break;
            case Jeans jeans: GetComponent<PlayerJeans>()?.Unequip(jeans); break;
        }
    }
}
