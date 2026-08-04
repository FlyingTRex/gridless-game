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

        var prefab = item.worldPickupPrefab != null ? item.worldPickupPrefab : droppedItemPrefab;
        if (prefab == null) return;

        int count = source.GetCount(item);
        if (count <= 0 || !source.RemoveItem(item, count)) return;

        Vector3 position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var dropped = Instantiate(prefab, position, Quaternion.identity);

        if (dropped.TryGetComponent(out Pickup pickup))
            pickup.Configure(item, count);
    }
}
