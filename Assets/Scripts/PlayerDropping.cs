using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerDropping : NetworkBehaviour
{
    [SerializeField] private GameObject droppedItemPrefab;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — equipment has no despawn concept of
    // its own (unlike Pickup), so a Despawn component is attached here
    // instead. Each equippable's own Stash() destroys it again the moment
    // it's picked back up (see Despawn.cs for why that matters).
    [SerializeField] private float equipmentDespawnDelay = 120f;

    private PlayerInventory playerInventory;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
    }

    public void Drop(ItemDefinition item) => DropFrom(playerInventory.Inventory, item);

    // Drops every unit of `item` in source — used where no quantity
    // choice makes sense (PlayerLoot evicting a hand slot to make room).
    public void DropFrom(Inventory source, ItemDefinition item) =>
        DropFrom(source, item, source != null ? source.GetCount(item) : 0);

    // Removes up to `quantity` of `item` from the given inventory (capped
    // to what's actually there) and spawns it as a physical pickup in the
    // world in front of the player. An equipment-backed slot ignores
    // quantity entirely — it's always exactly one real instance, nothing
    // to partially drop. Shared by the inventory screen's quantity-picker
    // Drop popup and PlayerLoot's hand-eviction path.
    public void DropFrom(Inventory source, ItemDefinition item, int quantity)
    {
        if (item == null || source == null || quantity <= 0) return;

        // Equipment-backed slot (Canteen, Backpack, etc.) — release the
        // real object via its own carried state instead of the generic
        // RemoveItem+spawn-a-Pickup path below, which would strip the
        // equipment reference and orphan the physical object (the gotcha
        // documented in CLAUDE.md).
        IEquippable equipment = null;
        foreach (var slot in source.Slots)
        {
            if (slot.item == item && slot.equipment != null)
            {
                equipment = slot.equipment;
                break;
            }
        }

        if (equipment != null)
        {
            source.RemoveEquipmentItem(item);
            equipment.SetCarried(false, null);
            var equipmentTransform = (equipment as Component)?.transform;
            if (equipmentTransform != null)
            {
                equipmentTransform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
                var despawn = equipmentTransform.gameObject.AddComponent<Despawn>();
                despawn.delay = equipmentDespawnDelay;
            }

            // Real bug found live (2026-08-13): this is the actual path
            // the Inventory screen's Drop button uses (DrawItemDropPopup),
            // which PlayerBelt.Drop's own equivalent fix never covers —
            // a Canteen clipped to a worn Belt is a pure data relationship
            // (registered in belt.Inventory), not a Transform-hierarchy
            // one, so it doesn't automatically follow the Belt into the
            // world when only the Belt's own SetCarried(false, ...) runs
            // above. Generalized beyond Belt/Canteen: any IInventoryHolder
            // equippable (a worn Backpack holding another equipped item,
            // etc.) gets the same cascade.
            if (equipment is IInventoryHolder holder && holder.Inventory != null)
                DropNestedEquipment(holder.Inventory);

            return;
        }

        // FIXED (2026-08-30, found live: dropping a plain item like a Stick
        // removed it from the caller's own LOCAL Inventory only, never told
        // the server -- so PlayerEquipment's per-frame client-side
        // reconciliation (added the same session, see its own header
        // comment) kept snapping the "removed" item right back the very
        // next frame, since the server's broadcast truth never changed.
        // Same shape as Pickup/ChopTree's own Request.../Cmd... pattern:
        // resolve locally which slot this Inventory actually is (a
        // Command can't serialize an arbitrary object reference), then let
        // the server do the real removal + spawn.
        int amount = Mathf.Min(quantity, source.GetCount(item));
        if (amount <= 0) return;

        // Three possible sources, resolved locally (a Command can't
        // serialize an arbitrary Inventory reference): the main
        // PlayerInventory (slotName/containerNetId both empty/0), one of
        // PlayerEquipment's own named slots (a hand, mainly), or a worn
        // Backpack's own nested Inventory -- identified by its
        // NetworkIdentity, same as every other live-object reference this
        // project's Commands already resolve things by (Pickup, tree, ...).
        var equippedBackpack = GetComponent<PlayerBackpack>()?.Equipped;
        uint containerNetId = 0;
        if (equippedBackpack != null && ReferenceEquals(equippedBackpack.Inventory, source)
            && equippedBackpack.TryGetComponent(out NetworkIdentity backpackIdentity))
        {
            containerNetId = backpackIdentity.netId;
        }

        string slotName = containerNetId == 0 ? GetComponent<PlayerEquipment>()?.SlotNameFor(source) : null;
        CmdDropItem(slotName ?? "", containerNetId, item.name, amount);
    }

    [Command]
    private void CmdDropItem(string slotName, uint containerNetId, string itemId, int quantity)
    {
        var item = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(itemId) : null;
        if (item == null) return;

        Inventory source;
        if (containerNetId != 0)
        {
            source = NetworkServer.spawned.TryGetValue(containerNetId, out var identity)
                ? (identity.GetComponent(typeof(IInventoryHolder)) as IInventoryHolder)?.Inventory
                : null;
        }
        else
        {
            source = string.IsNullOrEmpty(slotName)
                ? playerInventory.Inventory
                : GetComponent<PlayerEquipment>()?.GetSlot(slotName);
        }
        if (source == null) return;

        int amount = Mathf.Min(quantity, source.GetCount(item));
        if (amount <= 0 || !source.RemoveItem(item, amount)) return;

        Vector3 position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        SpawnPickupServerSide(item, amount, position);
    }

    // Detaches and drops every physical equipment item still registered
    // in inventory's own slots, scattered slightly so they don't all land
    // exactly on top of whatever container just got dropped.
    private void DropNestedEquipment(Inventory inventory)
    {
        var nested = new System.Collections.Generic.List<(ItemDefinition item, IEquippable equipment)>();
        foreach (var slot in inventory.Slots)
            if (slot.equipment != null)
                nested.Add((slot.item, slot.equipment));

        foreach (var (item, nestedEquipment) in nested)
        {
            inventory.RemoveEquipmentItem(item);
            nestedEquipment.SetCarried(false, null);

            var nestedTransform = (nestedEquipment as Component)?.transform;
            if (nestedTransform == null) continue;

            Vector3 scatter = Random.insideUnitSphere * 0.3f;
            nestedTransform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight + scatter;
            var despawn = nestedTransform.gameObject.AddComponent<Despawn>();
            despawn.delay = equipmentDespawnDelay;
        }
    }

    // Instantiates item's world pickup prefab (or the generic fallback) at
    // dropDistance/dropHeight in front of the player and configures it.
    // Shared by DropFrom above (removing from an inventory first) and
    // AdminSpawnScreen's dev/test spawn tool (no inventory involved at
    // all — conjures the item from nothing).
    //
    // FIXED (2026-08-26, found live during the real two-machine test with
    // traskmi): this used to Instantiate directly on whichever machine
    // called it. NetworkSpawnHelper.SpawnIfNetworked only actually spawns
    // on the network when NetworkServer.active is true, which is only
    // ever true on the HOST's own machine -- so a drop/admin-spawn
    // performed by the CLIENT silently stayed local-only, invisible to
    // everyone else (see BUGS_AND_ENHANCEMENTS.md's full writeup). Fix:
    // route the actual spawn through a Command so it always runs
    // server-side regardless of who triggered it. Item is passed by its
    // stable string id (ItemDatabase.Find), not the ScriptableObject
    // reference itself -- Mirror can't serialize an arbitrary asset
    // reference across a Command.
    public void SpawnPickup(ItemDefinition item, int count = 1)
    {
        if (item == null) return;
        Vector3 position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        CmdSpawnPickup(item.name, count, position);
    }

    // Server-side entry point (2026-08-30, found live: traskmi's console
    // showed "CmdSpawnPickup ... called ... without authority" whenever a
    // creature was skinned). SkinnableCreature.ServerComplete already runs
    // server-side (invoked via its own Command) and correctly resolves the
    // actual killer's PlayerDropping -- but it was calling SpawnPickup()
    // above, which dispatches through ANOTHER Command. A [Command] is a
    // client-to-server call; invoking one directly from code that's
    // already running on the server re-triggers Mirror's generated
    // client-side ownership check wherever it happens to execute, which
    // fails because the code isn't actually running on the loot-owning
    // player's own client. Fix: server-side callers use this directly
    // (the same real spawn logic, just not wrapped in a Command) instead
    // of going through the client-facing SpawnPickup()/CmdSpawnPickup()
    // pair, which stays as the entry point for genuinely client-triggered
    // actions (AdminSpawnScreen's own button, clicked by the local player).
    public void ServerSpawnPickup(ItemDefinition item, int count = 1)
    {
        if (item == null) return;
        Vector3 position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        SpawnPickupServerSide(item, count, position);
    }

    [Command]
    private void CmdSpawnPickup(string itemId, int count, Vector3 position)
    {
        var item = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(itemId) : null;
        if (item == null) return;
        SpawnPickupServerSide(item, count, position);
    }

    private void SpawnPickupServerSide(ItemDefinition item, int count, Vector3 position)
    {
        var prefab = item.worldPickupPrefab != null ? item.worldPickupPrefab : droppedItemPrefab;
        if (prefab == null) return;

        var spawned = Instantiate(prefab, position, Quaternion.identity);
        NetworkSpawnHelper.SpawnIfNetworked(spawned);

        if (spawned.TryGetComponent(out Pickup pickup))
        {
            pickup.Configure(item, count);
            return;
        }

        // The prefab isn't a stack-representable Pickup (e.g. Log's real
        // choppable ResourceNode, since v0.3.150-dev) -- one instance can't
        // represent "count of these" the way a Pickup's own count field
        // can, so a dropped stack of 5 used to silently collapse to a
        // single choppable node no matter how many were actually dropped.
        // Found live, 2026-08-19: breaking 5 dropped Logs only ever
        // yielded 2 Planks total (one node's worth), not 5 nodes' worth.
        // Fixed by spawning the remaining count-1 as separate instances,
        // scattered the same way ChoppableTree.Complete() already scatters
        // a felled tree's own logs, instead of discarding the quantity.
        for (int i = 1; i < count; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.5f;
            offset.y = Mathf.Abs(offset.y);
            var extra = Instantiate(prefab, position + offset, Random.rotation);
            NetworkSpawnHelper.SpawnIfNetworked(extra);
        }
    }
}
