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

    // Optional — a 2D icon shown next to this item's name in inventory UI.
    // Null (the default, and still the case for most items) means
    // text-only, same as every item looked before this field existed.
    // Small (baked ~32x32) — rendering it any larger than that looks
    // blurry, since GUIContent draws images at native pixel size with no
    // fit-to-control scaling. Use previewIcon below for anything bigger.
    public Sprite icon;

    // Optional — a bigger, separately-baked version of icon for large
    // preview UI (e.g. InventoryScreen's Back-slot preview box). Falls
    // back to icon (blurrily upscaled) if unset — only worth baking a
    // dedicated one for items that actually get a large preview treatment.
    public Sprite previewIcon;
}
