using UnityEngine;

// Lineage/wish gatekeeper — see design-brief.md's Magic System section
// (2026-08-08). Every character is randomly assigned one of the four
// lineage SkillDefinitions for free at spawn (Pillar 7's "no lineage-less
// players"). Learning an additional lineage later rides the Phase 2
// skill-books mechanic, which doesn't exist yet — until that's built, a
// character only ever knows their one starting lineage.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerMagic : MonoBehaviour
{
    [SerializeField] private SkillDefinition[] allLineages;
    // Placeholder, same "tune by playtesting" status as every other
    // balance value introduced this session (CraftTierScale.HoldDuration,
    // etc.) — how much a completed wish grows Will's ceiling by.
    [SerializeField] private float maxWillGrowthPerWish = 0.5f;

    private PlayerSkills skills;
    private PlayerVitals vitals;

    public SkillDefinition StartingLineage { get; private set; }

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        vitals = GetComponent<PlayerVitals>();

        if (allLineages != null && allLineages.Length > 0)
            StartingLineage = allLineages[Random.Range(0, allLineages.Length)];
    }

    public bool IsLineageKnown(SkillDefinition lineage) =>
        lineage != null && lineage == StartingLineage;

    public bool CanAttempt(WishRecipe wish) =>
        wish != null
        && IsLineageKnown(wish.lineage)
        && skills.GetLevel(wish.lineage) >= CraftTierScale.SkillRequirement(wish.unlockTier)
        && vitals.Will >= wish.willCost;

    // Called by a wish target (e.g. Campfire) from its own Complete() once
    // a hold finishes — mirrors ResourceNode/CraftingScreen's "check gates,
    // spend the cost, train the skill" shape. Returns false (no side
    // effects at all) if any gate fails, same silent-no-op convention as a
    // tool-gated ResourceNode.
    public bool TryWish(WishRecipe wish)
    {
        if (!CanAttempt(wish)) return false;

        vitals.ConsumeWill(wish.willCost);
        vitals.GrowMaxWill(maxWillGrowthPerWish);
        skills.GainExperience(wish.lineage, wish.skillGain);
        return true;
    }
}
