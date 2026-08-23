using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerDropping : MonoBehaviour
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

        int amount = Mathf.Min(quantity, source.GetCount(item));
        if (amount <= 0 || !source.RemoveItem(item, amount)) return;

        SpawnPickup(item, amount);
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
    public void SpawnPickup(ItemDefinition item, int count = 1)
    {
        var prefab = item.worldPickupPrefab != null ? item.worldPickupPrefab : droppedItemPrefab;
        if (prefab == null) return;

        Vector3 position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var spawned = Instantiate(prefab, position, Quaternion.identity);

        // Multiplayer Phase 3 sub-phase 2 pilot (2026-08-23) -- a prefab
        // carrying a NetworkIdentity (currently only
        // MasterworkLeatherBackpackPickup, proving the pattern before
        // it's applied to every other equippable/pickup prefab) needs to
        // be spawned through the network, not just locally Instantiate'd,
        // or it never gets a valid netId and can't be referenced by any
        // future Command. Guarded by NetworkServer.active since this
        // method can run from a context with no active server (shouldn't
        // happen given NetworkAutoHost, but a plain Instantiate is a
        // strictly safer fallback than throwing).
        if (spawned.TryGetComponent<NetworkIdentity>(out _) && NetworkServer.active)
            NetworkServer.Spawn(spawned);

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
            Instantiate(prefab, position + offset, Random.rotation);
        }
    }
}
