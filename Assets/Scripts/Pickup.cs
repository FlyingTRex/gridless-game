using UnityEngine;

public class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int quantity = 1;
    [SerializeField] private SkillDefinition trainedSkill;
    [SerializeField] private float skillGain = 0.05f;

    public string Prompt => item != null ? $"Pick up {item.itemName}" : "Pick up";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    public void Configure(ItemDefinition newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;
    }

    public void Complete(GameObject player)
    {
        var inventory = player.GetComponent<PlayerInventory>();
        int leftover = inventory != null ? inventory.AddItem(item, quantity) : quantity;
        if (leftover > 0)
        {
            // Inventory is full — leave the remainder on the ground instead of deleting it.
            quantity = leftover;
            return;
        }

        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);
        Destroy(gameObject);
    }
}
