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
            return;
        }

        int amount = Mathf.Min(quantity, source.GetCount(item));
        if (amount <= 0 || !source.RemoveItem(item, amount)) return;

        SpawnPickup(item, amount);
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
