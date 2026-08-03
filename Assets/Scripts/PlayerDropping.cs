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
    // main inventory's Drop button and by PlayerLoot, which uses it to
    // evict a hand slot when making room for a newly picked-up item.
    public void DropFrom(Inventory source, ItemDefinition item)
    {
        var prefab = item != null && item.worldPickupPrefab != null ? item.worldPickupPrefab : droppedItemPrefab;
        if (item == null || prefab == null || source == null) return;

        int count = source.GetCount(item);
        if (count <= 0 || !source.RemoveItem(item, count)) return;

        Vector3 position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var dropped = Instantiate(prefab, position, Quaternion.identity);

        if (dropped.TryGetComponent(out Pickup pickup))
            pickup.Configure(item, count);
    }
}
