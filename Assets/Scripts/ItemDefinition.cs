using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    public string itemName = "New Item";
    public int maxStack = 20;

    // Optional — the visual used when this item is dropped/placed in the
    // world. Falls back to PlayerDropping's generic dropped-item prefab if
    // unset, so most items don't need one.
    public GameObject worldPickupPrefab;
}
