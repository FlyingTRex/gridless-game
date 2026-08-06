using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerDropping : MonoBehaviour
{
    [SerializeField] private GameObject droppedItemPrefab;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;

    private PlayerInventory playerInventory;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
    }

    public void Drop(ItemDefinition item) => DropFrom(playerInventory.Inventory, item);

    // Removes all of `item` from the given inventory and spawns it as a
    // physical pickup in the world in front of the player. Shared by the
    // main inventory's Drop button, the inventory screen's move-popup Drop
    // option, and PlayerLoot, which uses it to evict a hand slot when
    // making room for a newly picked-up item.
    public void DropFrom(Inventory source, ItemDefinition item)
    {
        if (item == null || source == null) return;

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
                equipmentTransform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
            return;
        }

        int count = source.GetCount(item);
        if (count <= 0 || !source.RemoveItem(item, count)) return;

        SpawnPickup(item, count);
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

        if (spawned.TryGetComponent(out Pickup pickup))
            pickup.Configure(item, count);
    }
}
