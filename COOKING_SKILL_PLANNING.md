# Cooking Skill / Quality-Tier System Planning

**Status: built (v0.3.93-dev, 2026-08-15).** Everything below shipped as
designed — `CookableItem.trainedSkill`/`skillGain`/`requiredSkillLevel`,
`Campfire.HasRequiredCookingSkill`/`ResolveCookingOutcome`/
`ShowCookMessage`, `CampfireScreen`'s toast line. Grilled Meat and Herbal
Tea both gated at Cooking 5/skillGain 1.0; Steak and Potatoes at Cooking
15/skillGain 1.5 — exactly section 6's proposed numbers, confirmed as-is.
`RawMeatToCookedMeatCookable` deliberately left untouched (no
`trainedSkill`, stays free/risk-free). Verified via compile + direct
YAML grep of all 3 tuned recipes' `trainedSkill`/`requiredSkillLevel`/
`skillGain` fields — not yet live-tested in Play mode.

Planning doc for giving Cooking real stakes (2026-08-15) — right now the
`Cooking` skill (`Assets/Data/Cooking.asset`) exists as a `SkillDefinition`
but is used by nothing: `CookableItem` has no `trainedSkill`/`skillGain`
field at all, and `Campfire.TickCooking()` is 100% deterministic — load
ingredients, wait `cookDurationSeconds`, always get the output. This is the
last open piece of MVP2 item 9 (Cooking) per `MVP2_PLANNING.md`.

## 1. Decisions (2026-08-15, via AskUserQuestion)

- **Binary success/fail, no quality-tier ladder.** Considered mirroring
  `CraftingRecipe`'s `lowerTierItem`/`higherTierItem` cross-linking (a
  brilliant success gives a better dish, a barely-fail gives a worse one
  like "Burnt Grilled Meat") — rejected as too much new content for now
  (2-3 new items per dish, each needing its own model/icon). **Instead:**
  reuses the same 5-outcome `CraftOutcomeRoll` crafting already has, but
  collapses it to two buckets — you get the dish, or you don't.
- **A real failure has a real cost.** A `SpectacularFailure` deals a mild
  Health hit (food poisoning/burns), same shape as
  `PlayerCrafting.SpectacularFailureDamage` (10) but smaller —
  **`CookingFailureDamage = 5`**, reflecting "mild" per Ben's framing
  rather than crafting's full disaster.

## 2. Data shape

`CookableItem` gains two fields, mirroring `CraftingRecipe`'s shape:

```csharp
public SkillDefinition trainedSkill;   // null = no skill gate, always
                                        // succeeds (see below)
public float skillGain = 1f;
public int requiredSkillLevel;         // flat Cooking level needed —
                                        // NOT routed through
                                        // CraftTierScale.SkillRequirement,
                                        // since food items don't use the
                                        // CraftTier ladder for this
```

`requiredSkillLevel` is a flat int rather than reusing
`CraftTierScale.SkillRequirement(outputItem.tier)` the way `CraftingRecipe`
does — food items are all `CraftTier.Normal` today (no tier ladder, see
decision above), so there's nothing meaningful to key off. A flat
per-recipe number is simpler and matches CLAUDE.md's own tier-scaling
gotcha: a scale tuned for one quantity (crafting-quality tiers) doesn't
automatically transfer to an unrelated one (cooking difficulty).

**`trainedSkill == null` skips the roll entirely and always succeeds** —
same convention `PlayerCrafting`'s 5 skill-less gadget recipes already
use. This is how `RawMeatToCookedMeatCookable` (the original, already-
shipped baseline recipe) stays exactly as it is today: free, no risk, no
skill gate. The new system only applies to recipes that opt in.

## 3. Gating — `Campfire.cs`

Needs a `PlayerSkills` reference alongside the `player` Transform it
already caches in `Start()`. New `HasRequiredCookingSkill(CookableItem
recipe)`, mirroring `PlayerCrafting.HasRequiredSkill`:

```csharp
private bool HasRequiredCookingSkill(CookableItem recipe)
{
    if (recipe.trainedSkill == null) return true;
    return playerSkills != null && playerSkills.GetLevel(recipe.trainedSkill) >= recipe.requiredSkillLevel;
}
```

Wired into both `GetAvailableRecipes()` (under-leveled recipes don't even
show as an option — same "not satisfiable yet" treatment as a missing
accessory/ingredient/water) and `StartCooking()` (can't be started via
any other path either).

## 4. Outcome roll — at completion, not at start

Ingredients are already consumed upfront in `StartCooking()` (existing
behavior, matches `PlayerCrafting.StartCraft`'s own upfront-consume
convention) — the roll happens when `TickCooking()`'s timer finishes,
mirroring `PlayerCrafting.ResolveOutcome`:

```csharp
float margin = playerSkills != null
    ? playerSkills.GetLevel(recipe.trainedSkill) - recipe.requiredSkillLevel
    : 0f;
var outcome = CraftOutcomeRoll.Roll(Mathf.Max(0f, margin));

switch (outcome)
{
    case CraftOutcome.BrilliantSuccess:
    case CraftOutcome.Success:
        outputInventory.AddItem(recipe.outputItem, recipe.outputCount);
        playerSkills?.GainExperience(recipe.trainedSkill, recipe.skillGain);
        LastCookMessage = $"{recipe.outputItem.itemName} turned out great!"; // BrilliantSuccess only
        break;

    case CraftOutcome.BarelyFail:
    case CraftOutcome.BadFailure:
        LastCookMessage = "It didn't turn out — the ingredients were wasted.";
        break;

    case CraftOutcome.SpectacularFailure:
        playerVitals?.Damage(CookingFailureDamage);
        LastCookMessage = "Disaster! Burnt beyond saving, and it made you feel sick.";
        break;
}
```

No skill XP on any failure outcome (same as crafting — only a real
success trains the skill). `BrilliantSuccess` and `Success` are
deliberately treated identically for output (binary, per decision 1) —
`BrilliantSuccess` only gets a nicer message, not a better item.

## 5. UI — a small message toast on `Campfire`, read by `CampfireScreen`

`Campfire` has no `OnGUI` of its own (unlike `PlayerCrafting`, which
draws its own toast) — `CampfireScreen` does all the drawing. Plan:
`Campfire` gets a `public string LastCookMessage` + an internal expiry
timestamp (same `ShowMessage`/`messageExpireTime` shape as
`PlayerCrafting`, just exposed as a property `CampfireScreen` can read),
rendered under the existing "Cooking {item} — N%" line in
`DrawRecipeSection()` once cooking completes.

## 6. Proposed per-recipe tuning (first pass — confirm before building)

| Recipe | trainedSkill | requiredSkillLevel | skillGain |
|---|---|---|---|
| Raw Meat → Cooked Meat | *(none — unchanged)* | — | — |
| Grilled Meat (Grill) | Cooking | 5 | 1.0 |
| Herbal Tea (Kettle) | Cooking | 5 | 1.0 |
| Steak and Potatoes (Frying Pan) | Cooking | 15 | 1.5 |
| *(future Cooking Pot recipe)* | Cooking | — | — |

Reasoning: the baseline recipe stays free (it's the tutorial-level
"first thing you can cook"); Grilled Meat/Herbal Tea are simple
single-accessory combinations, low gate; Steak and Potatoes is a more
involved two-ingredient sear, higher gate. At `requiredSkillLevel 5`
with exactly 5 Cooking, margin is 0 — riskiest odds
(`CraftOutcomeRoll`'s low end: ~2% brilliant, 63% success, ~20%
barely-fail, ~12% bad-failure, ~3% spectacular). Risk bottoms out at
margin ≥ `CraftOutcomeRoll.RiskMarginCap` (20) — Cooking 25 for the
Level-5 recipes, Cooking 35 for Steak and Potatoes.

## Cross-references

- `CraftOutcomeRoll.cs` — the shared 5-outcome roll this reuses
  directly, already shared between `PlayerCrafting` and `PlayerWriting`.
- `PlayerCrafting.cs`'s `HasRequiredSkill`/`ResolveOutcome`/
  `SpectacularFailureDamage` — the direct precedent this mirrors.
- `CAMPFIRE_PLANNING.md` section 4 — `CookableItem`'s current shape and
  `Campfire.cs`'s cooking-completion flow this extends.

Built 2026-08-15, v0.3.93-dev — see the status note at the top of this
doc and `CHANGELOG.md`.
