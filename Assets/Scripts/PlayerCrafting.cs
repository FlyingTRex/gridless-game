using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerCrafting : MonoBehaviour
{
    [SerializeField] private CraftingRecipe[] recipes;
    [SerializeField] private float storageRange = 10f;

    private static readonly string[] HandSlotNames = { "Left Hand", "Right Hand" };

    // Skill points above a tier's threshold at which chance-of-creation
    // risk bottoms out (see RollOutcome) — a flat cap rather than scaling
    // to the next tier's own threshold, since the gaps (10/25/50/100) are
    // unevenly spaced and a single, consistent "meaningfully practiced
    // past the minimum" number is simpler to reason about and tune.
    private const float RiskMarginCap = 20f;
    private const float SpectacularFailureDamage = 10f;

    // How long a chance-of-creation outcome message stays on screen.
    private const float MessageDuration = 3f;

    private PlayerInventory inventory;
    private PlayerSkills skills;
    private PlayerBackpack backpackCarrier;
    private PlayerEquipment equipment;
    private PlayerVitals vitals;
    private readonly List<StorageBox> nearbyStorages = new List<StorageBox>();

    private string message;
    private float messageExpireTime;

    private enum CraftOutcome
    {
        BrilliantSuccess,
        Success,
        BarelyFail,
        BadFailure,
        SpectacularFailure,
    }

    // Read by CraftingScreen (the Crafting tab of PlayerMenuScreen, Tab key)
    // to render the recipe list.
    public IReadOnlyList<CraftingRecipe> Recipes => recipes;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        skills = GetComponent<PlayerSkills>();
        backpackCarrier = GetComponent<PlayerBackpack>();
        equipment = GetComponent<PlayerEquipment>();
        vitals = GetComponent<PlayerVitals>();
    }

    // Every Inventory a recipe is allowed to draw materials from: the main
    // inventory, an equipped backpack's contents, and any StorageBox
    // within storageRange — not just what's directly in your hands/main
    // slots. Crafted output still only ever goes to the main inventory.
    private IEnumerable<Inventory> ReachableInventories()
    {
        yield return inventory.Inventory;

        var backpack = backpackCarrier != null ? backpackCarrier.Equipped : null;
        if (backpack != null)
            yield return backpack.Inventory;

        StorageBox.FindNearby(transform.position, storageRange, nearbyStorages);
        foreach (var box in nearbyStorages)
            yield return box.Inventory;
    }

    // Read by CraftingScreen's DrawContent() to show how much of an ingredient you
    // actually have access to, matching what HasIngredients/TryCraft use —
    // not just what's in the main inventory.
    public int GetAvailableCount(ItemDefinition item)
    {
        int total = 0;
        foreach (var inv in ReachableInventories())
            total += inv.GetCount(item);
        return total;
    }

    // True if every ingredient's required count is currently reachable.
    public bool HasIngredients(CraftingRecipe recipe)
    {
        if (recipe?.ingredients == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (GetAvailableCount(ingredient.item) < ingredient.count) return false;
        }

        return true;
    }

    // True if recipe has no tool requirement, or any one of its
    // requiredTools is currently held in a hand (not consumed — same "any
    // tier counts" check ResourceNode uses for Pickaxe/Axe gating).
    public bool HasRequiredTool(CraftingRecipe recipe)
    {
        if (recipe?.requiredTools == null || recipe.requiredTools.Length == 0) return true;
        if (equipment == null) return false;

        foreach (var tool in recipe.requiredTools)
        {
            if (tool != null && equipment.HasInHand(tool)) return true;
        }

        return false;
    }

    // True if the recipe's output tier has no real skill gate (Crude —
    // see CraftTierScale.SkillRequirement), or trainedSkill is currently
    // at or above that tier's threshold (Rudimentary 10, Normal 25,
    // Fine 50, Masterwork 100).
    public bool HasRequiredSkill(CraftingRecipe recipe)
    {
        if (recipe?.outputItem == null || recipe.trainedSkill == null) return true;

        int required = CraftTierScale.SkillRequirement(recipe.outputItem.tier);
        if (required <= 0) return true;

        return skills != null && skills.GetLevel(recipe.trainedSkill) >= required;
    }

    public bool TryCraft(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.outputItem == null || recipe.ingredients == null) return false;
        if (!HasRequiredTool(recipe)) return false;
        if (!HasRequiredSkill(recipe)) return false;

        // Checked before removing any ingredient so a full inventory can't
        // consume materials without being able to hold the output(s).
        // Every tier sibling of the same base item shares outputItem's
        // maxStack, so this check stays valid even when ResolveOutcome
        // below ends up producing lowerTierItem/higherTierItem instead.
        if (!inventory.Inventory.HasSpaceFor(recipe.outputItem, recipe.outputCount)) return false;
        if (recipe.bonusItem != null && !inventory.Inventory.HasSpaceFor(recipe.bonusItem, recipe.bonusCount)) return false;
        if (!HasIngredients(recipe)) return false;

        // Ingredients are gone regardless of outcome from here on — a bad
        // or spectacular failure is specifically "the materials were
        // wasted," not "the attempt silently didn't happen."
        foreach (var ingredient in recipe.ingredients)
            RemoveAcrossReachable(ingredient.item, ingredient.count);

        ResolveOutcome(recipe);

        skills?.GainExperience(recipe.trainedSkill, recipe.skillGain);
        return true;
    }

    // Chance-of-creation roll (Ben's call, v0.1.82-dev): recipes with no
    // trainedSkill (the 5 gadgets) skip the roll entirely and always
    // succeed plainly, same as before this system existed — there's no
    // skill/tier concept to drive risk for them. Everything else rolls
    // between five outcomes based on how far the player's skill is above
    // this tier's threshold (see RollOutcome) and applies the result:
    // a better/worse tier of the same item, wasted materials, a broken
    // tool, or player damage.
    private void ResolveOutcome(CraftingRecipe recipe)
    {
        if (recipe.trainedSkill == null)
        {
            GiveOutput(recipe, recipe.outputItem);
            return;
        }

        float margin = skills != null
            ? skills.GetLevel(recipe.trainedSkill) - CraftTierScale.SkillRequirement(recipe.outputItem.tier)
            : 0f;
        var outcome = RollOutcome(Mathf.Max(0f, margin));

        switch (outcome)
        {
            case CraftOutcome.BrilliantSuccess:
                GiveOutput(recipe, recipe.higherTierItem != null ? recipe.higherTierItem : recipe.outputItem);
                if (recipe.higherTierItem != null)
                    ShowMessage($"Incredible! You crafted a {recipe.higherTierItem.itemName} — far better than intended!");
                break;

            case CraftOutcome.Success:
                GiveOutput(recipe, recipe.outputItem);
                break;

            case CraftOutcome.BarelyFail:
                GiveOutput(recipe, recipe.lowerTierItem != null ? recipe.lowerTierItem : recipe.outputItem);
                if (recipe.lowerTierItem != null)
                    ShowMessage($"Close, but not quite — you ended up with a {recipe.lowerTierItem.itemName} instead.");
                break;

            case CraftOutcome.BadFailure:
                ShowMessage("The attempt failed and the materials were ruined.");
                break;

            case CraftOutcome.SpectacularFailure:
                string toolClause = BreakHeldTool(recipe);
                vitals?.Damage(SpectacularFailureDamage);
                ShowMessage($"Disaster! The attempt failed, the materials were destroyed{toolClause}, and you were hurt in the process.");
                break;
        }
    }

    // Interpolates each outcome's odds between "just barely qualified for
    // this tier" (margin 0 — riskiest) and "meaningfully practiced past
    // it" (margin >= RiskMarginCap — safest). SpectacularFailure isn't
    // its own Lerp — it gets whatever probability mass the other four
    // don't use, so they don't need to be hand-tuned to sum to exactly 1.
    private static CraftOutcome RollOutcome(float skillMargin)
    {
        float t = Mathf.Clamp01(skillMargin / RiskMarginCap);

        float brilliant = Mathf.Lerp(0.02f, 0.10f, t);
        float success = Mathf.Lerp(0.63f, 0.85f, t);
        float barelyFail = Mathf.Lerp(0.20f, 0.04f, t);
        float badFailure = Mathf.Lerp(0.12f, 0.01f, t);

        float roll = Random.value;
        if (roll < brilliant) return CraftOutcome.BrilliantSuccess;
        roll -= brilliant;
        if (roll < success) return CraftOutcome.Success;
        roll -= success;
        if (roll < barelyFail) return CraftOutcome.BarelyFail;
        roll -= barelyFail;
        if (roll < badFailure) return CraftOutcome.BadFailure;
        return CraftOutcome.SpectacularFailure;
    }

    // Destroys one instance of whichever requiredTools item is actually
    // held (checks both hands), for a spectacular failure. No-op — no
    // tool breaks — for recipes with no tool requirement (everything
    // except Trimmed Stick today). Returns a clause to fold into the
    // failure message, or "" if nothing broke.
    private string BreakHeldTool(CraftingRecipe recipe)
    {
        if (recipe.requiredTools == null || recipe.requiredTools.Length == 0 || equipment == null)
            return "";

        foreach (var handSlotName in HandSlotNames)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null) continue;

            foreach (var tool in recipe.requiredTools)
            {
                if (tool != null && hand.GetCount(tool) > 0)
                {
                    hand.RemoveItem(tool, 1);
                    return $", your {tool.itemName} broke";
                }
            }
        }

        return "";
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpireTime = Time.time + MessageDuration;
    }

    // Top-center, just below PlayerSkills' own skill-up message (y=70 to
    // y=100) so a craft that both raises a skill's level AND has a
    // notable chance-of-creation outcome can show both without
    // overlapping.
    private void OnGUI()
    {
        if (message == null || Time.time >= messageExpireTime) return;

        const float width = 420f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 110f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }

    // Adds outputItem plus bonusItem (if any) via the equippable-aware
    // path (see AddCraftedOutput) — bonusItem only fires here, i.e. only
    // when the craft actually produced something, never on a failure
    // that destroyed the materials outright.
    private void GiveOutput(CraftingRecipe recipe, ItemDefinition actualOutput)
    {
        AddCraftedOutput(actualOutput, recipe.outputCount);
        if (recipe.bonusItem != null)
            AddCraftedOutput(recipe.bonusItem, recipe.bonusCount);
    }

    // Plain stackable output goes straight into the main inventory as a
    // count, same as always. An equippable output (Backpack/Belt/etc. —
    // always outputCount 1 in practice, since equippables never stack)
    // instead gets a real, physical instance spawned from its
    // worldPickupPrefab and added via AddEquipmentItem. Previously this
    // always used the plain-stack path even for equippables, producing an
    // inert, non-wearable stack with no physical object behind it — same
    // root cause as the Admin spawn tab's matching gap (see
    // BUGS_AND_ENHANCEMENTS.md).
    private void AddCraftedOutput(ItemDefinition item, int count)
    {
        var equippablePrefab = item.worldPickupPrefab != null
            ? item.worldPickupPrefab.GetComponent<IEquippable>()
            : null;

        if (equippablePrefab == null)
        {
            inventory.AddItem(item, count);
            return;
        }

        var instance = Object.Instantiate(item.worldPickupPrefab);
        var equippable = instance.GetComponent<IEquippable>();
        if (!inventory.Inventory.AddEquipmentItem(item, equippable))
        {
            Object.Destroy(instance);
            return;
        }

        equippable.Stash();
    }

    // Takes from the main inventory first, then the backpack, then each
    // nearby box in distance order, until amount is fully removed. Safe to
    // call only after HasIngredients confirmed enough exists in total.
    private void RemoveAcrossReachable(ItemDefinition item, int amount)
    {
        foreach (var inv in ReachableInventories())
        {
            if (amount <= 0) return;

            int have = inv.GetCount(item);
            if (have <= 0) continue;

            int take = Mathf.Min(have, amount);
            inv.RemoveItem(item, take);
            amount -= take;
        }
    }
}
