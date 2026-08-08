using UnityEngine;

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
    public float willCost = 10f;
    public float skillGain = 1f;
}
