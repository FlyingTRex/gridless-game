using UnityEngine;

// How PlayerInteraction's R-handling finds a valid target for this wish
// once it's the player's selected/default wish (2026-08-08) — replaces
// the old "try IWishTarget, fall back to Rigidbody" order-of-checks with
// an explicit per-wish mode, since the player now picks which wish R
// attempts rather than the world implicitly deciding.
public enum WishTargeting
{
    // Needs a world object implementing IWishTarget that specifically
    // offers this wish (e.g. Campfire offers Spark). Default — matches
    // every wish shipped before this enum existed.
    SpecificObject,
    // Works on any Rigidbody-bearing object the crosshair hits, no
    // specific interface required (Push).
    AnyRigidbody,
    // No physical target required at all — valid the moment it's
    // selected, gated purely on lineage/skill/Will. Not used by any
    // shipped wish yet (a future Fireball is the intended first case,
    // per Ben's own example) but the dispatch path exists so it's ready.
    Unconditional,
}

// Sibling to CraftingRecipe, for magic wishes — see design-brief.md's Magic
// System section (2026-08-08 ideation). Deliberately reuses CraftTierScale's
// existing tier thresholds rather than a separate numeric field: unlockTier
// is the tier `lineage`'s skill has to clear before this wish is even
// attemptable (recipe-unlock gate), same convention every CraftingRecipe's
// output tier already uses for its own skill gate. There is no material-tier
// "weakest-link" input here the way crafting has ingredients — a wish's
// quality ceiling instead comes from whatever it acts on in the world (e.g.
// Campfire's fuel), decided per wish target, not by this shared data class.
[CreateAssetMenu(menuName = "Gridless/Wish Recipe", fileName = "NewWish")]
public class WishRecipe : ScriptableObject
{
    public string wishName = "New Wish";
    public SkillDefinition lineage;
    public CraftTier unlockTier = CraftTier.Crude;
    public WishTargeting targeting = WishTargeting.SpecificObject;
    // Two different costs depending on outcome (see PlayerMagic.TryWish's
    // success/failure roll, added 2026-08-08 per Ben's call) — success
    // costs more than failure, matching "a strained effort that actually
    // works takes more out of you than one that fizzles."
    public float successWillCost = 60f;
    public float failureWillCost = 40f;
    public float skillGain = 1f;
}
