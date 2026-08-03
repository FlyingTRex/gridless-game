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

    public void Complete(GameObject player)
    {
        player.GetComponent<PlayerInventory>()?.AddItem(item, quantity);
        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);
        Destroy(gameObject);
    }
}
