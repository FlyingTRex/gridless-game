using System.Collections.Generic;
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

    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31, found live:
    // "crafted a Stone Knife, it never showed up in inventory" -- server
    // genuinely had it, PlayerCrafting.AddCraftedOutput's AddEquipmentItem
    // call succeeded, but syncedSlots/ComputeSignature/RefreshSyncedSlots
    // above always skip equipment-backed slots (`if (slot.equipment !=
    // null) continue`), so a Stone Knife -- or any other equippable output
    // -- sitting loose in the main inventory (not worn, not in a hand) was
    // never told to a remote client at all). A plain itemId+count entry
    // isn't enough here -- each occupant is a distinct LIVE object, not
    // just a stack, so this carries the object's own NetworkIdentity.netId
    // instead, same identity-based reconciliation PlayerEquipment.
    // syncedSlots already established for its own named slots -- this is
    // the same idea generalized to an unordered list instead of one entry
    // per fixed slot name.
    [System.Serializable]
    public struct SyncedEquipmentInventorySlot
    {
        public string itemId;
        public uint netId;
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

    // See SyncedEquipmentInventorySlot above -- the equipment-backed
    // counterpart to syncedSlots, polled and reconciled independently
    // since it tracks live object identity, not just item+count.
    public readonly SyncList<SyncedEquipmentInventorySlot> syncedEquipmentSlots = new SyncList<SyncedEquipmentInventorySlot>();

    private string lastSyncedSignature = string.Empty;
    private string lastSyncedEquipmentSignature = string.Empty;

    private void Awake()
    {
        inventory = new Inventory(capacity);
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
                DebugLog.Write("PlayerInventory", $"[SERVER] {gameObject.name} signature changed, pushing RefreshSyncedSlots -- new signature=\"{signature}\"");
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

        // Client-side retry, not just the Callback below -- the equipped
        // object's own NetworkIdentity spawn message can arrive AFTER this
        // component's own syncedEquipmentSlots update, same reasoning
        // PlayerEquipment.Update's own retry comment already explains.
        ApplySyncedEquipmentSlotsToLocalInventory();
    }

    // Client-side reconciliation (found live, 2026-08-28). Fires on every
    // observer, including the server's own local copy of this SyncList --
    // skip there, since the server's `inventory` is already the
    // authoritative source RefreshSyncedSlots just read FROM, not
    // something to overwrite from its own broadcast.
    private void OnSyncedSlotsChanged(SyncList<SyncedInventorySlot>.Operation op, int index, SyncedInventorySlot oldItem, SyncedInventorySlot newItem)
    {
        if (isServer) return;
        ApplySyncedSlotsToLocalInventory();
    }

    private void OnSyncedEquipmentSlotsChanged(SyncList<SyncedEquipmentInventorySlot>.Operation op, int index, SyncedEquipmentInventorySlot oldItem, SyncedEquipmentInventorySlot newItem)
    {
        if (isServer) return;
        ApplySyncedEquipmentSlotsToLocalInventory();
    }

    // Snapshot of syncedSlots as of the last reconciliation -- lets this
    // apply only the DELTA between old and new server-known state,
    // rather than a destructive full rebuild.
    private readonly Dictionary<string, int> lastSyncedCounts = new Dictionary<string, int>();

    // FIXED (2026-08-28, found live: "I can pick up a Skill Book, but it
    // doesn't show up in inventory" -- picked up correctly, then silently
    // WIPED). The original version of this method cleared every plain
    // stackable slot and rebuilt it purely from syncedSlots -- correct
    // for items added via a Command (the server genuinely knows about
    // them), but not all Pickup prefabs have a NetworkIdentity yet (see
    // Pickup.cs's own header comment: ~49 still don't) -- those still
    // take the original fully-local path, adding directly to this
    // client's own `inventory` with the server never finding out. A
    // clear-and-rebuild-from-server-truth reconciliation would silently
    // delete that local-only item the very next time ANYTHING else
    // changed this player's syncedSlots (any other pickup, drop, etc.),
    // since the server's broadcast never included it in the first place.
    //
    // Fixed to be additive instead: track the last known synced totals
    // per item, diff against the current broadcast, and apply only the
    // signed DELTA to the local inventory via Inventory.ApplyStackableDelta
    // (an AddItem for a gain, a RemoveItem for a loss). An item this
    // client added locally (never part of any synced snapshot, so its
    // delta is always 0) is never touched. This remains a real
    // reconciliation, not a blind trust-the-client scheme -- a genuine
    // server-side removal (traded, dropped via another path, etc.) still
    // correctly propagates as a negative delta.
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
            if (item == null)
            {
                DebugLog.Write("PlayerInventory", $"[CLIENT] {gameObject.name} delta for itemId=\"{itemId}\" delta={delta} but ItemDatabase.Find returned NULL -- skipped");
                continue;
            }

            DebugLog.Write("PlayerInventory", $"[CLIENT] {gameObject.name} applying delta: {item.itemName} delta={delta} (oldCount={oldCount} newCount={newCount})");
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
            syncedSlots.Add(new SyncedInventorySlot { itemId = id, count = slot.count });
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
            syncedEquipmentSlots.Add(new SyncedEquipmentInventorySlot { itemId = id, netId = netId });
        }
    }

    // FIXED (2026-08-31, found live: dragging a freshly-crafted Stone Knife
    // into a nearby StorageBox visibly snapped it right back into the main
    // inventory). The first version of this reconciled by "does local state
    // exactly match the CURRENT broadcast" every tick -- correct-looking,
    // but a full-enforcement comparison, unlike the additive-DELTA
    // philosophy ApplySyncedSlotsToLocalInventory (the plain-item sync)
    // already established: that one only acts on the CHANGE between
    // successive broadcasts, so a local-only move the server was never
    // told about (moving into a StorageBox isn't Command-routed yet, see
    // MULTIPLAYER_INTERACTION_AUDIT.md's still-open item 2) is silently
    // left alone -- exactly what let the Rock test in the same session
    // work correctly. Full-enforcement instead fights that local move
    // every single frame, since the broadcast still lists the item as
    // present. Fixed to use the identical delta shape: track the netId set
    // as of the last broadcast, and only add/remove what genuinely
    // *changed* between broadcasts -- an id present in both old and new
    // broadcasts is never touched, regardless of current local state.
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

        // Genuinely removed server-side since the last broadcast.
        foreach (var netId in lastSyncedEquipmentNetIds)
        {
            if (newNetIds.Contains(netId)) continue;
            RemoveLocalEquipmentByNetId(netId);
        }

        // Genuinely added server-side since the last broadcast.
        foreach (var netId in newNetIds)
        {
            if (lastSyncedEquipmentNetIds.Contains(netId)) continue;
            if (LocalHasEquipmentNetId(netId)) continue;
            if (!NetworkClient.spawned.TryGetValue(netId, out var identity)) continue;

            var equipment = identity.GetComponent(typeof(IEquippable)) as IEquippable;
            var item = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(itemIdByNetId[netId]) : null;
            if (equipment == null || item == null) continue;

            DebugLog.Write("PlayerInventory", $"[CLIENT] {gameObject.name} adding synced equipment: {item.itemName} netId={netId}");
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
    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31, found live:
    // a Stone Knife dragged into a nearby StorageBox stayed put locally
    // (the just-fixed snap-back regression) but never reached the server
    // at all -- host's own view of the box never showed it, a real
    // desync, not just a display gap. This container-key scheme
    // (ResolveContainer below) only ever covered containers reachable from
    // the Player's own NetworkBehaviours (main inventory, PlayerEquipment
    // slots, a worn IInventoryHolder) -- a world object like a StorageBox
    // isn't part of that tree at all. Extended with an optional
    // containerNetId per side, same "resolve by NetworkIdentity" pattern
    // PlayerDropping/PlayerBuilding's own Commands already use for exactly
    // this class of container -- a netId of 0 means "use the string key
    // instead," so every existing main/worn/slot caller is unaffected.
    public void RequestMove(string fromContainer, uint fromContainerNetId, string toContainer, uint toContainerNetId, ItemDefinition item, int quantity)
    {
        string id = ItemDatabase.Instance.IdFor(item);
        if (id == null) return;
        CmdMoveItem(fromContainer, fromContainerNetId, toContainer, toContainerNetId, id, quantity);
    }

    [Command]
    private void CmdMoveItem(string fromContainer, uint fromContainerNetId, string toContainer, uint toContainerNetId, string itemId, int quantity)
    {
        var item = ItemDatabase.Instance.Find(itemId);
        if (item == null || quantity <= 0) return;

        Inventory from = ResolveContainerOrNetId(fromContainer, fromContainerNetId);
        Inventory to = ResolveContainerOrNetId(toContainer, toContainerNetId);
        if (from == null || to == null) return;

        InventoryTransfer.MoveAsManyAsFit(from, to, item, quantity);
    }

    // Public wrapper so other Player NetworkBehaviours' own Commands (e.g.
    // PlayerEating's RequestEatFrom) can resolve a container key without
    // duplicating ResolveContainer's logic -- same container-key scheme,
    // one source of truth.
    public Inventory ResolveContainerByKey(string key) => ResolveContainer(key, GetComponent<PlayerEquipment>());

    private Inventory ResolveContainerOrNetId(string key, uint containerNetId)
    {
        if (containerNetId != 0)
        {
            return NetworkServer.spawned.TryGetValue(containerNetId, out var identity)
                ? (identity.GetComponent(typeof(IInventoryHolder)) as IInventoryHolder)?.Inventory
                : null;
        }

        return ResolveContainer(key, GetComponent<PlayerEquipment>());
    }

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
        NetworkIdentity identity = null;
        bool hasIdentity = pickup != null && pickup.TryGetComponent(out identity);
        DebugLog.Write("PlayerInventory", $"RequestCompletePickup on {gameObject.name}: pickup={pickup?.name} hasIdentity={hasIdentity} isOwned={(hasIdentity ? netIdentity.isOwned.ToString() : "n/a")}");
        if (!hasIdentity) return;
        CmdCompletePickup(identity);
    }

    [Command]
    private void CmdCompletePickup(NetworkIdentity pickupIdentity)
    {
        DebugLog.Write("PlayerInventory", $"CmdCompletePickup RECEIVED server-side on {gameObject.name}, pickupIdentity={pickupIdentity?.name}");
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
    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): used to
    // always equip from the main inventory only (a client had to resolve
    // and pass nothing else -- CmdEquipInstance just assumed `inventory`)
    // -- equipping an item sitting in a worn Backpack's nested cargo fell
    // through to InventoryScreen's local, unrouted carrier calls instead.
    // Now takes a source container key, same "main"/"worn:slotName"/plain-
    // slotName scheme ResolveContainerByKey/InventoryScreen.ContainerKeyFor
    // already established for Eating/Medicine/Reading/Writing -- reused
    // here rather than inventing a second resolution scheme.
    public void RequestEquipInstance(IEquippable equipment, string slotName, string sourceKey)
    {
        if (equipment == null || equipment is not Component component || sourceKey == null) return;
        if (!component.TryGetComponent(out NetworkIdentity identity)) return;
        CmdEquipInstance(identity, slotName, sourceKey);
    }

    [Command]
    private void CmdEquipInstance(NetworkIdentity itemIdentity, string slotName, string sourceKey)
    {
        var equipment = itemIdentity != null ? itemIdentity.GetComponent(typeof(IEquippable)) as IEquippable : null;
        if (equipment == null || !equipment.CanEquipToSlot(slotName)) return;

        var source = ResolveContainerByKey(sourceKey);
        if (source == null) return;

        switch (equipment)
        {
            case Backpack backpack: GetComponent<PlayerBackpack>()?.Equip(backpack, source); break;
            case Belt belt: GetComponent<PlayerBelt>()?.Equip(belt, source); break;
            case Boot boot: GetComponent<PlayerBoot>()?.Equip(boot, source); break;
            case Sunglasses sunglasses: GetComponent<PlayerSunglasses>()?.Equip(sunglasses, source); break;
            case MiningFaceShield shield: GetComponent<PlayerMiningFaceShield>()?.Equip(shield, source); break;
            case Canteen canteen: GetComponent<PlayerCanteen>()?.EquipTo(canteen, slotName, source); break;
            case NavigationComputer navComputer: GetComponent<PlayerNavComputer>()?.EquipTo(navComputer, slotName, source); break;
            case PersonalHealthMonitor monitor: GetComponent<PlayerHealthMonitor>()?.EquipTo(monitor, slotName, source); break;
            case Tool tool: GetComponent<PlayerTool>()?.EquipTo(tool, slotName, source); break;
            case Shirt shirt: GetComponent<PlayerShirt>()?.Equip(shirt, source); break;
            case Jeans jeans: GetComponent<PlayerJeans>()?.Equip(jeans, source); break;
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

    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31) -- world
    // pickup for every equipment-carrier type (Backpack, Belt, Boot,
    // Canteen, Jeans, MiningFaceShield, Shirt, Tool, plus StorageBox/
    // SkillBook which have no dedicated carrier and call straight into
    // PlayerLoot/AddEquipmentItem from their own Complete()) never had a
    // network hop at all -- entirely client-side, so a genuine remote
    // client's server never found out the item left the world. Same
    // generic-dispatch-by-runtime-type shape as CmdEquipInstance/
    // CmdUnequipInstance above, just for the "first contact" pickup moment.
    // NavigationComputer/PersonalHealthMonitor/Sunglasses have no live
    // world-pickup prefab in the project yet (checked 2026-08-31) --
    // intentionally left off this dispatch; add a case once one ships.
    public void RequestPickUpEquipment(IEquippable equipment)
    {
        if (equipment == null || equipment is not Component component) return;
        if (!component.TryGetComponent(out NetworkIdentity identity)) return;
        CmdPickUpEquipment(identity);
    }

    [Command]
    private void CmdPickUpEquipment(NetworkIdentity itemIdentity)
    {
        var equipment = itemIdentity != null ? itemIdentity.GetComponent(typeof(IEquippable)) as IEquippable : null;
        if (equipment == null) return;

        switch (equipment)
        {
            case Backpack backpack: GetComponent<PlayerBackpack>()?.PickUp(backpack); break;
            case Belt belt: GetComponent<PlayerBelt>()?.PickUp(belt); break;
            case Boot boot: GetComponent<PlayerBoot>()?.PickUp(boot); break;
            case Canteen canteen: GetComponent<PlayerCanteen>()?.PickUp(canteen); break;
            case MiningFaceShield shield: GetComponent<PlayerMiningFaceShield>()?.PickUp(shield); break;
            case Tool tool: GetComponent<PlayerTool>()?.PickUp(tool); break;
            case Shirt shirt: GetComponent<PlayerShirt>()?.PickUp(shirt); break;
            case Jeans jeans: GetComponent<PlayerJeans>()?.PickUp(jeans); break;
            case StorageBox box:
                if (box.IsPlayerOwned && box.PickupItem != null && box.Inventory.Slots.Count == 0)
                    ServerPickUpGeneric(box.PickupItem, box);
                break;
            case SkillBook book: ServerPickUpGeneric(book.ItemDefinition, book); break;
        }
    }

    // Shared fallback for the two types with no dedicated PlayerXxx carrier
    // -- same PlayerLoot-first-then-stash-into-main-inventory shape their
    // own Complete() methods already had, just running server-side now.
    private void ServerPickUpGeneric(ItemDefinition item, IEquippable equipment)
    {
        var loot = GetComponent<PlayerLoot>();
        if (loot != null && loot.ReceiveEquipment(item, equipment)) return;

        if (inventory.AddEquipmentItem(item, equipment))
            equipment.Stash();
    }
}
