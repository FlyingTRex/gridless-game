using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    public string itemName = "New Item";
    public int maxStack = 20;

    // Normal (the default) means "no tier concept" for items that don't
    // have one (Stick, Small Rock, ore, ...), matching CraftTierNames'
    // existing "no prefix" convention for the baseline tier. Meaningful
    // for items that come in a 5-tier ladder (tools, Lockboxes) — read by
    // the eventual weakest-link crafting rule to cap a recipe's output
    // tier by its ingredients', not just by skill.
    public CraftTier tier = CraftTier.Normal;

    // Optional — the visual used when this item is dropped/placed in the
    // world. Falls back to PlayerDropping's generic dropped-item prefab if
    // unset, so most items don't need one.
    public GameObject worldPickupPrefab;
}
