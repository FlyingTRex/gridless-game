using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    public string itemName = "New Item";
    public int maxStack = 20;

    // Pounds, read by PlayerEncumbrance to total up what the player is
    // carrying (see design-brief.md's Encumbrance section, 2026-08-10).
    // Defaults to 1 so every pre-existing item gets a sane starting value
    // for free instead of silently weighing nothing — first-pass numbers,
    // trivially rebalanced later since it's just a serialized float.
    public float weight = 1f;

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

    // Optional — marks this item as a refined/upgraded form of a raw
    // material (every Trimmed Stick tier points baseItem at Stick, Woven
    // Grass Cloth points at Fiber). Read by IngredientMatching so a recipe
    // or build piece asking for the raw material also accepts anything
    // refined from it, instead of needing a second recipe per variant.
    // Null (the default) means "not a refined form of anything."
    public ItemDefinition baseItem;

    // Optional — marks this item as a melee weapon for PlayerCombat.
    // False (default) for everything else, including plain crafting Tools
    // (Pickaxe/Axe/Hammer) that aren't meant to double as weapons. When
    // true, holding this item adds CraftTierScale.WeaponDamageBonus(tier)
    // on top of the base bare-handed punch and trains the shared Melee
    // skill instead of Bare-handed — first applied to the Knife
    // (2026-08-14), deliberately reusable by any future melee weapon
    // (Sword, Spear, ...) without touching PlayerCombat again.
    public bool isMeleeWeapon;

    // Marks this item as a bow for PlayerRangedCombat — mirrors
    // isMeleeWeapon's shape exactly (2026-08-15, Hunting Expansion
    // design). Held in one hand slot like any other tool (no two-handed
    // equip system exists or is needed — see the design doc's "what if
    // we were lazy" pivot: the OTHER hand holds whichever Arrow tier the
    // player wants to fire, which doubles as ammo selection with no
    // separate Quiver item required).
    public bool isRangedWeapon;

    // Marks this item as arrow ammo for PlayerRangedCombat — checked
    // when scanning the off-hand slot so a plain Stick or other non-ammo
    // item can't accidentally get fired/consumed as if it were an arrow.
    public bool isArrow;

    // Optional per-item override of CraftTierScale.ArrowDamageBonus(tier)
    // — sentinel -1 (default) means "use the shared tier table," same as
    // every other arrow. Exists so a different arrow *material* (Iron vs.
    // Stone, both spanning the same 5-tier CraftTier ladder) can deal
    // different damage at the same nominal tier without a second
    // material-keyed table bolted onto CraftTierScale — the "a scale
    // tuned for one thing doesn't transfer to another" gotcha (see
    // CLAUDE.md) applies just as much to a hypothetical second axis on an
    // existing scale as it does to reusing one scale for a new quantity.
    // Iron Arrow (2026-08-18) is the first and, as of writing, only user.
    public float arrowDamageBonus = -1f;

    public float EffectiveArrowDamageBonus =>
        arrowDamageBonus >= 0f ? arrowDamageBonus : CraftTierScale.ArrowDamageBonus(tier);
}
