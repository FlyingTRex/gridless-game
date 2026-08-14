using System.Collections.Generic;
using UnityEngine;

// Writing half of the skill-books mechanic (SKILL_BOOKS_PLANNING.md
// Phase 2) — reuses PlayerCrafting/PlayerMagic's exact CraftOutcomeRoll
// formula, just with the author's Intelligence standing in for the
// subject skill and the book's own subject tier standing in for the
// crafted item's tier. Consumes Paper + Ink on every attempt regardless
// of outcome; only a BadFailure/SpectacularFailure produces no book.
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerCrafting))]
[RequireComponent(typeof(PlayerMagic))]
public class PlayerWriting : MonoBehaviour
{
    [SerializeField] private SkillDefinition intelligenceSkill;
    [SerializeField] private ItemDefinition paperItem;
    [SerializeField] private ItemDefinition inkItem;
    [SerializeField] private ItemDefinition skillBookItem;

    // Intelligence XP granted to the author on a write, by outcome tier —
    // failures grant none (SKILL_BOOKS_PLANNING.md's writing-outcome
    // table: "—" for both failure tiers). Same small-number scale as
    // other skillGain values in this project (ResourceNode 0.5,
    // WishRecipe 1).
    private const float BarelyFailIntelligenceGain = 0.5f;
    private const float SuccessIntelligenceGain = 1.5f;
    private const float BrilliantSuccessIntelligenceGain = 3f;

    private const float SpectacularFailureDamageMin = 2f;
    private const float SpectacularFailureDamageMax = 10f;
    private const float LineageBonusLevelMin = 1f;
    private const float LineageBonusLevelMax = 10f;

    private const float MessageDuration = 4f;

    private PlayerSkills skills;
    private PlayerVitals vitals;
    private PlayerInventory playerInventory;
    private PlayerCrafting crafting;
    private PlayerMagic magic;

    private string message;
    private float messageExpireTime;

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        vitals = GetComponent<PlayerVitals>();
        playerInventory = GetComponent<PlayerInventory>();
        crafting = GetComponent<PlayerCrafting>();
        magic = GetComponent<PlayerMagic>();
    }

    public bool HasMaterials =>
        playerInventory.Inventory.GetCount(paperItem) > 0 && playerInventory.Inventory.GetCount(inkItem) > 0;

    // Every recipe the author currently knows well enough to write a
    // book about — same HasRequiredSkill gate crafting itself uses, so
    // writing about something you can't actually make yet isn't offered.
    public IEnumerable<CraftingRecipe> WritableRecipes
    {
        get
        {
            foreach (var recipe in crafting.Recipes)
                if (recipe != null && recipe.outputItem != null && crafting.HasRequiredSkill(recipe))
                    yield return recipe;
        }
    }

    // Every wish in a lineage the author currently knows.
    public IEnumerable<WishRecipe> WritableWishes => magic.KnownWishes;

    public bool TryWriteRecipeBook(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.outputItem == null || !HasMaterials) return false;

        float margin = skills.GetLevel(intelligenceSkill) - CraftTierScale.SkillRequirement(recipe.outputItem.tier);
        var outcome = ConsumeAndRoll(margin);
        if (outcome == CraftOutcome.BadFailure || outcome == CraftOutcome.SpectacularFailure) return true;

        var book = SpawnBook();
        if (book != null) book.SetTargetRecipe(recipe);
        else ShowMessage("The book was written, but you had nowhere to put it.");
        return true;
    }

    public bool TryWriteWishBook(WishRecipe wish)
    {
        if (wish == null || !HasMaterials) return false;

        float margin = skills.GetLevel(intelligenceSkill) - CraftTierScale.SkillRequirement(wish.unlockTier);
        var outcome = ConsumeAndRoll(margin);
        if (outcome == CraftOutcome.BadFailure || outcome == CraftOutcome.SpectacularFailure) return true;

        float bonus = outcome == CraftOutcome.BrilliantSuccess
            ? Random.Range(LineageBonusLevelMin, LineageBonusLevelMax)
            : 0f;

        var book = SpawnBook();
        if (book != null) book.SetTargetWish(wish, bonus);
        else ShowMessage("The book was written, but you had nowhere to put it.");
        return true;
    }

    // Consumes materials, rolls the outcome, and applies its Intelligence-
    // gain/damage/message side effects — shared by both entry points
    // above. Doesn't decide what (if anything) to spawn; callers branch
    // on the returned outcome for that, since a recipe book and a wish
    // book need different SkillBook.SetTarget* calls.
    private CraftOutcome ConsumeAndRoll(float rawMargin)
    {
        playerInventory.Inventory.RemoveItem(paperItem, 1);
        playerInventory.Inventory.RemoveItem(inkItem, 1);

        float margin = Mathf.Max(0f, rawMargin);
        var outcome = CraftOutcomeRoll.Roll(margin);

        switch (outcome)
        {
            case CraftOutcome.SpectacularFailure:
                vitals.Damage(Random.Range(SpectacularFailureDamageMin, SpectacularFailureDamageMax));
                ShowMessage("Disaster — the writing attempt failed and hurt you in the process.");
                break;
            case CraftOutcome.BadFailure:
                ShowMessage("The writing attempt failed — the result was unusable.");
                break;
            case CraftOutcome.BarelyFail:
                skills.GainExperience(intelligenceSkill, BarelyFailIntelligenceGain);
                ShowMessage("The book turned out rough, but usable.");
                break;
            case CraftOutcome.Success:
                skills.GainExperience(intelligenceSkill, SuccessIntelligenceGain);
                ShowMessage("You wrote a solid book.");
                break;
            case CraftOutcome.BrilliantSuccess:
                skills.GainExperience(intelligenceSkill, BrilliantSuccessIntelligenceGain);
                ShowMessage("A brilliant piece of writing!");
                break;
        }

        return outcome;
    }

    private SkillBook SpawnBook()
    {
        var instance = Instantiate(skillBookItem.worldPickupPrefab);
        var book = instance.GetComponent<SkillBook>();

        if (book != null && playerInventory.Inventory.AddEquipmentItem(skillBookItem, book))
        {
            book.Stash();
            return book;
        }

        Destroy(instance);
        return null;
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpireTime = Time.time + MessageDuration;
    }

    // y=190 — below PlayerSkills (70), PlayerCrafting (110), PlayerMagic
    // (150), same stacking convention so none of these overlap.
    private void OnGUI()
    {
        if (message == null || Time.time >= messageExpireTime) return;

        const float width = 420f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 190f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }
}
