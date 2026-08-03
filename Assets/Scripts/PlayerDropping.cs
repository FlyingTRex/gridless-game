using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerDropping : MonoBehaviour
{
    [SerializeField] private GameObject droppedItemPrefab;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;

    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    public void Drop(ItemDefinition item)
    {
        if (item == null || droppedItemPrefab == null) return;

        int count = inventory.GetCount(item);
        if (count <= 0 || !inventory.RemoveItem(item, count)) return;

        Vector3 position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var dropped = Instantiate(droppedItemPrefab, position, Quaternion.identity);

        if (dropped.TryGetComponent(out Pickup pickup))
            pickup.Configure(item, count);
    }
}
