using UnityEngine;

public class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int quantity = 1;

    public string Prompt => item != null ? $"Pick up {item.itemName}" : "Pick up";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    public void Complete(PlayerInventory inventory)
    {
        inventory.AddItem(item, quantity);
        Destroy(gameObject);
    }
}
