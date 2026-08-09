using System.Collections.Generic;
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
    // Every wish in the game, regardless of lineage — the single source of
    // truth MagicScreen and PlayerInteraction both read from (2026-08-08
    // refactor; previously MagicScreen and PlayerInteraction each held
    // their own separate lists/refs, which only worked because there were
    // exactly two wishes total). KnownWishes filters this down per-player.
    [SerializeField] private WishRecipe[] allWishes;
    // Placeholder, same "tune by playtesting" status as every other
    // balance value introduced this session (CraftTierScale.HoldDuration,
    // etc.) — how much a completed wish grows Will's ceiling by.
    [SerializeField] private float maxWillGrowthPerWish = 0.5f;

    // Skill points above a wish's unlock threshold at which success-chance
    // risk bottoms out — same shape as PlayerCrafting's RiskMarginCap/
    // RollOutcome, kept as an independent constant rather than shared,
    // since crafting and wishes are separate systems that happen to use
    // the same interpolation idea.
    private const float RiskMarginCap = 20f;
    private const float BaseSuccessChance = 0.5f;
    private const float MasteredSuccessChance = 0.9f;
    private const float MessageDuration = 3f;

    private PlayerSkills skills;
    private PlayerVitals vitals;

    private string message;
    private float messageExpireTime;

    public SkillDefinition StartingLineage { get; private set; }

    // The wish R currently attempts, player-chosen (2026-08-08, Ben's
    // "default skill" idea) rather than implicitly decided by whatever the
    // crosshair happens to be over — see PlayerInteraction's dispatch off
    // this wish's own `targeting` mode. Auto-defaults to the first known
    // wish in Awake so single-wish gameplay keeps working with zero menu
    // trips; only matters as a real choice once a lineage has more than one.
    public WishRecipe SelectedWish { get; private set; }

    public IEnumerable<WishRecipe> KnownWishes
    {
        get
        {
            if (allWishes == null) yield break;
            foreach (var wish in allWishes)
                if (wish != null && IsLineageKnown(wish.lineage))
                    yield return wish;
        }
    }

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        vitals = GetComponent<PlayerVitals>();

        if (allLineages != null && allLineages.Length > 0)
            StartingLineage = allLineages[Random.Range(0, allLineages.Length)];

        foreach (var wish in KnownWishes)
        {
            SelectedWish = wish;
            break;
        }
    }

    public bool IsLineageKnown(SkillDefinition lineage) =>
        lineage != null && lineage == StartingLineage;

    // Called by MagicScreen's Select button. Refuses (returns false, no
    // change) for a wish outside the player's known lineage — the UI
    // shouldn't offer that button at all, but this is the real gate, not
    // just UI discipline.
    public bool SelectWish(WishRecipe wish)
    {
        if (wish == null || !IsLineageKnown(wish.lineage)) return false;
        SelectedWish = wish;
        return true;
    }

    // Gated on successWillCost, not failureWillCost, even though success
    // isn't guaranteed — success costs more than failure, so requiring
    // only the cheaper amount could let an attempt succeed and then be
    // unable to actually pay for it (ConsumeWill would refuse and silently
    // leave Will unchanged, a confusing "it worked but nothing was spent"
    // state). Requiring the more expensive amount up front avoids that.
    public bool CanAttempt(WishRecipe wish) =>
        wish != null
        && IsLineageKnown(wish.lineage)
        && skills.GetLevel(wish.lineage) >= CraftTierScale.SkillRequirement(wish.unlockTier)
        && vitals.Will >= wish.successWillCost;

    // Called by a wish target (e.g. Campfire) from its own Complete() once
    // a hold finishes — mirrors ResourceNode/CraftingScreen's "check gates,
    // spend a cost, train the skill" shape, plus a binary success/failure
    // roll (Ben's call, 2026-08-08) — same interpolated-by-skill-margin
    // idea as PlayerCrafting's chance-of-creation, just two outcomes
    // instead of five. Returns false either if the gates fail outright (no
    // side effects at all, same silent-no-op convention as a tool-gated
    // ResourceNode) or if the roll itself fails (Will is still spent and
    // the skill still trains — a failed attempt isn't a non-attempt).
    public bool TryWish(WishRecipe wish)
    {
        if (!CanAttempt(wish)) return false;

        float margin = Mathf.Max(0f, skills.GetLevel(wish.lineage) - CraftTierScale.SkillRequirement(wish.unlockTier));
        float successChance = Mathf.Lerp(BaseSuccessChance, MasteredSuccessChance, Mathf.Clamp01(margin / RiskMarginCap));
        bool succeeded = Random.value < successChance;

        vitals.ConsumeWill(succeeded ? wish.successWillCost : wish.failureWillCost);
        if (succeeded) vitals.GrowMaxWill(maxWillGrowthPerWish);
        skills.GainExperience(wish.lineage, wish.skillGain);

        if (!succeeded)
            ShowMessage($"The wish didn't take — {wish.wishName} fizzled.");

        return succeeded;
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpireTime = Time.time + MessageDuration;
    }

    // Top-center, below PlayerSkills' skill-up message (y=70) and
    // PlayerCrafting's chance-of-creation message (y=110) — same stacking
    // convention, so a wish that both trains a skill and fizzles can show
    // both without overlapping.
    private void OnGUI()
    {
        if (message == null || Time.time >= messageExpireTime) return;

        const float width = 420f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 150f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }
}
