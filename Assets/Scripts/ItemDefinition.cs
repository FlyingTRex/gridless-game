using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    public string itemName = "New Item";
    public int maxStack = 20;
}
