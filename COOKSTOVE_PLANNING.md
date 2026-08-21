# Cookstove — Planning

Planning only, 2026-08-20 — not built. First of the three automation
structures raised in `BUGS_AND_ENHANCEMENTS.md`'s "automation of tasks to
get to the end game" entry. Ben's brief: fuel is the existing FuelTier/
FuelItem system as-is (no new fuel type), and match `Furnace.cs`'s shape as
closely as possible.

## Be-mean pass

**Fork 1 — is this really a new structure, or just "Campfire + Auto-Run"?**
`Furnace` itself went through exactly this evolution (bare marker →
fuel+smelt state → automation bolted on). `Campfire` already has a fuel
system (`FuelTier`/`FuelItem`) and a cooking system (`CookableItem`) —
building a whole separate Cookstove duplicates both. **Real collision
found**: `Campfire.fuelInventory` capacity is **hardcoded to 1 slot in
code**, and that's not an oversight — `Furnace.cs`'s own header comment
says it explicitly: "a Furnace is meant to run longer unattended" (2 slots)
vs. Campfire's 1. Retrofitting Auto-Run onto Campfire either needs a
capacity bump (undoing a deliberate earlier design choice) or ships an
automation feature that needs restocking every few minutes, undermining
the whole point.
**Resolved, Ben's call**: build Cookstove as its own dedicated structure
(new model/prefab/`BuildPiece`), same relationship Furnace already has to
the Anvil — two physically distinct structures serving overlapping
domains. Campfire keeps its 1-slot, hand-tended, cozy identity untouched.

**Fork 2 — does automating cooking gut the Cooking skill/quality system
that was just built (`COOKING_SKILL_PLANNING.md`)?** This is the sharpest
risk in the whole plan. Campfire's real skill-gated recipes (Grilled Meat/
Herbal Tea at Cooking 5, Steak and Potatoes at Cooking 15) carry a genuine
`CraftOutcomeRoll` failure chance and train the skill — if a Cookstove
could queue those same recipes with guaranteed success and zero skill
requirement, there'd be no reason to ever cook at a Campfire again. Same
failure shape `CLAUDE.md`'s Magic section already flagged once (an
Elemental ore-smelting wish undercutting the real Furnace economy) — don't
repeat it here.
**Resolved — Ben's own fix, and it's better than my original one**:
gate the *structure itself*, not each recipe individually. `BuildPiece`
already has exactly this mechanism — `trainedSkill`/`unlockTier`, the
same fields every other `BuildPiece` (including the Anvil/Furnace built
this session) already uses to require a real skill level before the
piece can even be placed. Setting `Cookstove.trainedSkill = Cooking`,
`unlockTier = CraftTier.Normal` (skill 25 — see the Threshold note below)
means a Cookstove **cannot be built at all** until the player has hand-
cooked enough at a Campfire to earn it — Cooking is *only* trainable that
way, so this genuinely mandates real Campfire use first, exactly Ben's
ask, for free (zero new code, reuses the existing gate every `BuildPiece`
already has).
This changes what "the new recipes" means too, per Ben's follow-up call
(see Fork 2b) — since the structure itself is now the gate, individual
`CookstoveRecipe` entries no longer need their own skill field the way
`CookableItem` does; once built, everything on the list is available.
`Herbal Tea`/`Meat Stew`'s `requiresCanteenWater` remains a separate, real
blocker regardless of skill — an unattended structure has no live
player's equipped Canteen to draw from, so those two stay permanently
Campfire-only no matter how this gate is designed.

**Fork 2b — should the Cookstove eventually host Campfire's fancier
recipes (Grilled Meat, Steak and Potatoes, Fried Egg), not just the free
baseline?** Ben's call: **yes** — once the structure is skill-unlocked,
those should become real Cookstove automation targets too, not
permanently excluded. Real complication found checking the actual data:
**every one of those recipes requires a specific accessory in 100% of
existing `CookableItem` assets**, not just skill — Grilled Meat needs a
Grill, Steak and Potatoes and Fried Egg need a Frying Pan, Meat Stew a
Soup Pot. Skill and accessory travel together in every recipe that has
either. So hosting them means Cookstove needs **its own accessory-slot
system**, mirroring Campfire's 4 slots (Grill/Soup Pot/Kettle/Frying Pan)
— a real, meaningful scope increase over "clone Furnace," not a free
add-on. Scoped into the design below; `Herbal Tea`/`Meat Stew` still can't
join the list even with the right accessory equipped, because of the
canteen-water blocker above.

**Threshold — Ben's call: push well past every existing recipe's
requirement (25+), not match Steak and Potatoes' existing 15.** Landed on
**`unlockTier = CraftTier.Normal`** — this project already has a
established skill-level-per-tier vocabulary (Crude=1, Rudimentary=10,
Normal=25, Fine=50, Masterwork=100 — same table every other `BuildPiece`
already reads via `CraftTierScale.SkillRequirement`), so Normal (skill 25)
reuses an existing meaningful number rather than inventing a bespoke one,
and it's already past every shipped Cooking recipe's own requirement (the
highest, Steak and Potatoes, needs only 15). If 25 ends up feeling too
easy once live-tested, `unlockTier = CraftTier.Fine` (skill 50) is the
next rung on the same existing ladder — no new number-inventing needed
either way.

**Fork 3 — new recipe type vs. reusing `CookableItem` directly.**
Considered reusing `CookableItem` verbatim now that skill lives on the
structure, not the recipe. **Still rejected, same reasoning as before**:
`CookableItem` carries fields that don't apply here at all
(`trainedSkill`/`skillGain`/`requiredSkillLevel` are now meaningless once
gated at the structure level; `requiresCanteenWater` recipes must be
excluded outright, not just "always false"). A dedicated
**`CookstoveRecipe`** type — `ingredients[]`, `outputItem`, `outputCount`,
`cookDurationSeconds`, `requiredAccessory` (nullable, same meaning as
Campfire's) — makes automatability an explicit, per-recipe authoring
choice (does this recipe even have a `CookstoveRecipe` entry) rather than
a runtime filter over fields that no longer mean what they used to.

**Fork 4 — fuel.** Ben's own framing, confirmed against the code: reuse
`FuelItem`/`FuelTier` exactly as-is, same assets Furnace already uses
(Stick, the 5 Trimmed Stick tiers, Plank). No new fuel type, no
Cookstove-specific fuel item. `FuelItem` is already a generic registered-
item-type asset, not Furnace-specific in any way — a `[SerializeField]
FuelItem[] fuelItems` field on Cookstove pointing at the same assets is
the entire fuel side of this build.

**Risk flagged, not resolved — output competes with the same finite
prey pool `HUNTER_PLANNING.md` already flagged for the planned Hunter
NPC.** A Cookstove auto-consuming Raw Meat as fast as it's supplied
doesn't change the underlying wildlife-density pacing question — worth
watching together with the Hunter build, not two separate concerns.

## Design — deliberately copy `Furnace.cs`'s shape field-for-field

| Furnace | Cookstove | Notes |
|---|---|---|
| `SmeltableItem` | **`CookstoveRecipe`** (new) | `Ingredient[] ingredients`, `outputItem`, `outputCount`, `cookDurationSeconds`, plus a nullable `requiredAccessory` (see the new accessory-slot row below). No skill/canteen fields — skill now lives on the `BuildPiece` itself (Fork 2), canteen-gated recipes are excluded outright, not represented. |
| *(none)* | **`accessorySlots` (new, 4)** | Mirrors Campfire's Grill/Soup Pot/Kettle/Frying Pan accessory-slot inventory, added specifically so skill-unlocked recipes like Grilled Meat/Steak and Potatoes/Fried Egg can actually run (Fork 2b) — real new surface area beyond a pure Furnace clone, copy Campfire's own accessory-slot code rather than Furnace's (Furnace has none). |
| `FuelItem[] fuelItems` | `FuelItem[] fuelItems` | Identical, same asset references. |
| `fuelInventory` (2 slots) | `fuelInventory` (2 slots) | Same `FuelCapacity = 2` — no reason to differ from Furnace, Cookstove is equally meant to run unattended. |
| `materialsInventory`/`outputInventory` (4 slots each) | same | Unchanged capacities. |
| `recipeQueue` (max 4, round-robin `StartNextQueuedRecipe`) | same | Copy verbatim — this logic has nothing smelting-specific in it. |
| `fuelSourceBox`/`materialsSourceBox`/`outputBox` + `AutoRefill`/`AutoDrain` | same | Copy verbatim, including `storageLinkRange`. |
| `TryAutoLight`/`isLit`/`fuelSecondsRemaining` | same | Copy verbatim. |
| `RestoreState(...)` / `SaveId` | same | Same top-level `SaveManager` category shape Furnace uses (always pre-exists or is placed via `BuildPiece`, found-and-restored, not tied to `PlacedPiece`... **actually diverges here, see below**). |
| No `BuildPiece` (fixed scene fixture) | **Has a `BuildPiece`** | Real divergence: Furnace is a single fixed pre-placed fixture; Cookstove should be player-**buildable**, matching the Anvil/Furnace `BuildPiece` work from this session (real ingredients, `groundOffset` grounding check, baked icons). This changes its save-restore path to match `RestorePlacedPieces`, not `Furnace`'s own top-level category — same ordering rule `CLAUDE.md`'s `SaveManager.Load()` gotcha already documents (must restore after `RestorePlacedPieces`, same as `StorageBox`). |
| `FurnaceScreen` | **`CookstoveScreen`** (new) | Copy `FurnaceScreen`'s layout — recipe list with add/remove-from-queue, Fuel/Materials/Output box pickers, Auto-Run toggle. |

### Recipe seed list (proposed, not final)

Checked against every shipped `CookableItem` (Fork 2b) — since skill now
gates the whole structure rather than individual recipes, everything
*except* the two canteen-water recipes gets a `CookstoveRecipe` mirror:

| Cookstove recipe | Mirrors | Accessory needed |
|---|---|---|
| Raw Meat → Cooked Meat | `RawMeatToCookedMeatCookable` | none |
| Grilled Meat | `GrilledMeatCookable` | Grill |
| Fried Egg | `FriedEggCookable` | Frying Pan |
| Steak and Potatoes | `SteakAndPotatoesCookable` | Frying Pan |
| ~~Herbal Tea~~ | — | **excluded** — `requiresCanteenWater`, no live player Canteen to draw from unattended |
| ~~Meat Stew~~ | — | **excluded** — same canteen-water blocker |

All four included recipes are only reachable at all once the Cookstove
itself is built (Cooking 25+, Fork 2), and the accessory-gated three also
need the matching accessory slotted in — same double-gate Campfire's own
recipes already have today, just with skill checked once at construction
instead of per-craft.

## Build order (proposed, not committed)

1. `CookstoveRecipe.cs` (new `ScriptableObject`: `ingredients[]`,
   `outputItem`, `outputCount`, `cookDurationSeconds`, `requiredAccessory`)
   + the 4 seed recipes from the table above.
2. `Cookstove.cs` — copy `Furnace.cs`, rename types
   (`SmeltableItem`→`CookstoveRecipe`, `smeltDurationSeconds`→
   `cookDurationSeconds`, `TickSmelting`→`TickCooking`), add an
   `accessorySlots` inventory copied from `Campfire.cs`'s own accessory-
   slot handling, and gate `StartNextQueuedRecipe`-equivalent selection on
   `recipe.requiredAccessory == null || accessorySlots.Contains(...)` the
   same way Campfire's own cooking-start check already does.
3. `CookstoveScreen.cs` — copy `FurnaceScreen.cs`'s layout, plus
   Campfire's accessory-slot UI section.
4. `CookstoveBuildPiece.asset` — `trainedSkill = Cooking`,
   `unlockTier = CraftTier.Normal` (skill 25, see Threshold above) — the
   entire "must use the Campfire first" gate, via fields every other
   `BuildPiece` already has.
5. Model + `BuildPiece` prefab — same pipeline as this session's Anvil/
   Furnace build: Tripo3D generation (or reuse an existing kitchen/oven-
   shaped asset if one exists), bounds-check against the player before
   placement, `groundOffset` grounding check (mandatory per `CLAUDE.md`'s
   pivot gotcha — hit twice already this session on Furnace/Anvil, don't
   skip it a third time), baked icons via `IconBaker`.
6. Real ingredient cost for the `BuildPiece` recipe — Ben's own framing
   from the automation-strategy discussion: should genuinely require
   smelted Iron (Iron Ingot/Nail), so building a Cookstove also means
   having already built a working Furnace + Metalworking chain, not just
   Cooking skill — a second, independent progression gate stacked on top
   of the skill one, not a substitute for it.
7. Save/load: register as a `PlacedPiece`-restorable type, ordered after
   `RestorePlacedPieces` in `SaveManager.Load()` per the established
   StorageBox/GardenPlot ordering rule.
8. Live-test: confirm the `BuildPiece` genuinely can't be placed below
   Cooking 25, confirm the auto-cook loop works end-to-end for the
   no-accessory recipe, confirm an accessory-gated recipe only starts once
   the matching accessory is slotted, confirm Herbal Tea/Meat Stew were
   never offered at all.

No code has been written for any of this yet.
