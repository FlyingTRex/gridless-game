using UnityEngine;

// A refillable water source (e.g. the Water Puddle) — fills an empty
// container in the player's inventory rather than being drunk directly.
public class WaterSource : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition emptyContainer;
    [SerializeField] private ItemDefinition filledContainer;

    public string Prompt => $"Fill {emptyContainer?.itemName}";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    public void Complete(GameObject player)
    {
        var inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null) return;
        if (!inventory.RemoveItem(emptyContainer, 1)) return;

        inventory.AddItem(filledContainer, 1);
    }
}
