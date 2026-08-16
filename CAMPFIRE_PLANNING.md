# Campfire Planning

Planning doc for turning Campfire from a single magic-only scene prop into
a real craftable, fuel-burning, cooking structure (2026-08-12). Decisions
below are locked in; open items are flagged as such.

**Status: built (v0.3.26-dev through v0.3.30-dev).** Placement, fuel,
warmth, the Blender model rebuild, and the dedicated E-key popup UI have
all shipped — see `CHANGELOG.md` for the full writeup and
`TEST_FEATURE_PLAN.md` section 21 for the manual verification checklist
(not yet walked through, and the cooking-rework steps below need writing
into it). **Cooking rebuilt again (v0.3.30-dev)** — the 4 accessory
slots are now built (see section 4 below, updated in place); still
open/deferred: Wood Stove and the water-safety mechanic.

**Accessory models/icons/recipes — built (v0.3.90-dev, 2026-08-15).**
The one open gap from the v0.3.30-dev rebuild is closed: all 4 accessory
`ItemDefinition`s now have a real Blender model (`Tools/Blender/
GenerateCookwareModels.py`), a baked icon+preview, and a Forging-skill
crafting recipe (Grill 2x Iron Ingot, Cooking Pot 3x Iron Ingot, Kettle
2x Copper Ingot, Frying Pan 2x Iron Ingot — all `requiresAnvilSurface`,
same pattern as `NailRecipe`/`RudimentaryShovelRecipe`). Forging was an
existing `SkillDefinition`/`CraftingScreen` discipline tab with zero
recipes using it until now — the natural home for these, not a new
skill. See `CHANGELOG.md` for the full build writeup.

**Grilled Meat — the first Grill-accessory-gated `CookableItem`
(v0.3.91-dev, 2026-08-15), Ben's direct ask.** Herb x1 + Raw Meat x1,
40s cook time, `requiredAccessory` = Grill — the first recipe to
actually exercise the accessory-gating path
(`RawMeatToCookedMeatCookable`, the only other `CookableItem`, is
open-flame/no-accessory). `GrilledMeat.asset`/`GrilledMeatEdible.asset`
reuse Cooked Meat's model/icon as a placeholder, same convention
`TEST_FEATURE_PLAN.md` already documents for Cooked Meat itself — one
step up on `FoodTier` (HeartyMeal, 60 Hunger, vs. Cooked Meat's
Meal/40). Appended directly to `Campfire.prefab`'s `cookableItems`
array (the field lives on the prefab asset, not a per-instance scene
override, so every placed Campfire — current and future — picks it up
automatically) and to `PlayerEating.edibles` (a separate manually-
curated array, `EFFICIENCY_AUDIT.md` item 1, not yet covered by
`DatabaseRepopulator` — skipping this step would have left Grilled
Meat silently uneatable).

**Steak and Potatoes (Frying Pan) + Herbal Tea (Kettle) — built
(v0.3.92-dev, 2026-08-15).** Both Ben's direct asks, both with their own
real merged model rather than a reused placeholder (Steak and Potatoes:
`Tools/Blender/GenerateSteakAndPotatoesModel.py`, a pan + seared steak +
potato; Herbal Tea: `Tools/Blender/GenerateHerbalTeaModel.py`, a copy of
the Kettle geometry with the existing Herb model leaned against its
base). **Herbal Tea is also the first recipe to actually need water** —
the "Water is explicitly out of scope" decision below still holds for a
dirty/unsafe-water *mechanic*, but plain canteen water as a cooking
*ingredient* didn't exist as a mechanism at all until this recipe
needed it. `CookableItem` gained `requiresCanteenWater`/
`canteenWaterAmount`, mirroring `CraftingRecipe`'s existing fields
(already used by Healing Paste); `Campfire.cs` gained its own
`HasCanteenWater()`/`FindPlayerCanteen()`, wired into both
`GetAvailableRecipes()` and `StartCooking()` the same way
`PlayerCrafting` already gates/consumes it for ordinary crafting.

**UI redesign — built (v0.3.28-dev, then substantially reworked
v0.3.30-dev, both 2026-08-13).** Loading fuel/food used to only work via
a "Campfire (nearby)" section auto-appended to the bottom of the main
Inventory tab's scroll view (same pattern as nearby StorageBox) — a real
UX problem found live: on an already-busy screen, a small unlabeled row
at the very bottom wasn't discoverable (Ben's live report read verbatim
as "there's no mechanism to transfer fuel," even though the mechanism
was technically present and functional). **v0.3.28-dev** replaced it
with `CampfireScreen`, a focused popup (same visual family as
`LockboxScreen`) with simple Add-1/Take buttons. **v0.3.30-dev**
upgraded that same popup to real drag-and-drop once the cooking system
grew utensil/ingredient/output slots — Fuel, 4 Utensil boxes, 4
Ingredient boxes, 4 Output boxes, a Recipe picker, and a Transfer
section scoped to exactly Backpack contents + Left/Right Hand (Ben's
explicit scope, live feedback while looking at the built popup). The
drag-and-drop mechanics are a self-contained implementation inside
`CampfireScreen.cs`, not a literal extraction from `InventoryScreen.cs`
— mirrors that screen's proven interaction model without touching its
heavily-tested equip-dispatch code, which this screen has no need for
(every box here is a plain, unequippable `Inventory`). **Open question,
still not decided:** whether this same popup pattern should also replace
StorageBox's identical nearby-section approach — raised as a natural
follow-on, not committed either way.

## 1. Current state (audit, 2026-08-12)

Confirmed directly against the codebase before designing anything new:

- **`Campfire.cs`** is a single pre-placed object in `TestScene.unity`
  (`(-4, 0.3, -2)`) — no `ItemDefinition`, no recipe, not
  craftable/placeable by the player at all.
- **Only lit via the Elemental "Spark" magic wish** (hold R), gated by
  lineage/skill/Will through `IWishTarget`/`OnWishComplete`. There is no
  ordinary interaction to light it by hand.
- State is a **binary `isLit` bool only** — no timer, no fuel
  consumption, no re-extinguishing. It does drive a real `Light`
  component (so it genuinely illuminates) and swaps lit/unlit materials.
- **No warmth or cooking connection of any kind.**
- **Body Temperature is 100% decorative.** `PlayerVitals.bodyTemperature`
  only drifts back toward neutral (50) every frame — nothing in the game
  pushes it away from that. It's not even on the real HUD
  (`VitalsBarHUD`), only a debug-overlay label.
- **No cooking mechanic exists anywhere.** Raw Meat
  (`Assets/Data/RawMeat.asset`) is a real pickupable item but has **no
  `EdibleItem` registered at all** — it can't be eaten raw today, it just
  sits inert in inventory.
- **The design brief itself flags this as an intentional gap**: Spark
  lighting the Campfire was documented as a known simplification —
  *"Campfire.Complete() just lights unconditionally... no fuel-tier input
  exists to cap it against"* — meaning a fuel-driven Campfire was part of
  the original vision, never built.
- **The current model is a pre-Blender placeholder.** Built before this
  project had a working from-scratch Blender pipeline (see
  `Tools/Tripo3D/README.md`'s Blender notes — the 5 Trimmed Stick craft
  tiers are the proof this works well now).

## 2. Decisions (2026-08-12)

- **Becomes a real craftable/placeable item.** New recipe + Build-tab
  piece, replacing "the one hardcoded scene object" status quo — the
  player can place as many as they want. **Spark becomes an alternate,
  tool-free way to light an already-placed Campfire**, not the only way
  one can exist or be lit. **Campfire's own recipe should be an early,
  low-skill unlock** ("a tier one building recipe" — Ben's framing; see
  section 5's Wood Stove note for why that's a low `SkillRequirement`
  number, not a formal tier enum). Exact skill(s)/number not decided here.
- **Fuel: reuses the exact `FuelTier`/`FuelItem` system built for the
  Furnace** (see `WOOD_AND_FUEL_PLANNING.md`) — any registered `FuelItem`
  (Stick, Trimmed Stick tiers, Plank) works, tier controls burn duration.
  No new fuel logic needed. **1 fuel slot** — simpler/smaller-scale than
  the Furnace's planned 2, fitting a primitive campfire.
- **Cooking — rebuilt v0.3.30-dev, superseding the original decision
  below.** Was: 1 cooking slot, auto-cooks over time, no manual action
  (matching the Furnace's smelting model). **Now:** a real recipe system
  — a 4-slot ingredient input pool, a 4-slot output bank, and a manual
  Recipe button (Ben's live call while looking at the built popup: "once
  the raw ingredients are loaded, a 'recipe' button should show only
  items that can be cooked with the utensils and ingredients loaded").
  `CookableItem` now mirrors `CraftingRecipe`'s own `ingredients[]`/
  `outputItem` shape instead of a single raw/cooked pair, supporting
  multi-ingredient recipes. Still works with no accessory for the
  baseline (Raw Meat → Cooked Meat needs none). See section 4.
- **Accessory slots — built v0.3.30-dev, 4 slots, one per accessory
  type.** Grill, Cooking Pot (renamed from the original "Soup Pot"
  during the build — Ben's actual wording when specced), Kettle, Frying
  Pan — all usable simultaneously, each a capacity-1 `Inventory`
  restricted to exactly that item. **Accessories gate which recipes are
  possible** — `CookableItem.requiredAccessory`, checked via
  `Campfire.GetAvailableRecipes()`, same `CraftingRecipe.requiredTools`-
  style gating shape as planned. Each accessory is a real, plain (non-
  equippable) `ItemDefinition`. **Models/icons/recipes built v0.3.90-dev**
  (see the status note above) — no longer a blank placeholder or
  admin-spawn-only.
- **Water is explicitly out of scope for now.** There's no dirty/unsafe-
  water mechanic anywhere in the game today — `Canteen`/`WaterSource`
  don't distinguish water quality at all, and the only water-related
  vital risk is *overdrinking* (too much at once), not water safety.
  **Decided:** boiling a filled Canteen at the Campfire is allowed as a
  convenience interaction with **no mechanical effect yet** — seeds the
  interaction without deciding a real water-safety system now. A fuller
  version of that idea was raised and explicitly deferred (see section 5).
  **Still true as of v0.3.92-dev** — Herbal Tea consumes plain Canteen
  water as an ordinary ingredient (see the status note above), no water-
  quality/safety concept involved.
- **Warmth: a lit Campfire raises Body Temperature while the player is
  nearby** — the first real use of a vital that's been 100% decorative
  until now. **Body Temperature also gets added to the real HUD**
  (`VitalsBarHUD`), not left debug-overlay-only.

## 3. New model (Blender) — built v0.3.27-dev, 2026-08-13

Built as designed: a ring of 8 irregular low-poly rocks around a shallow-
teepee pile of 6 charred sticks, replacing the pre-Blender placeholder.

- **Rocks:** reuse `Assets/Data/RockChunk.mat` directly, as planned — no
  new rock material authored.
- **Wood:** rather than reusing `TreeBark.mat`/`PlankFoundation.mat`
  as-is, two new materials were generated (`CampfireWoodUnlit.mat`,
  `CampfireWoodLit.mat`) from a procedurally-built 256x256 charred-wood
  albedo texture + matching ember-glow emission texture — needed a
  charred look distinct from plain unburned wood, and a lit variant with
  real emission for the ember glow.
- **Char effect:** built exactly as specced — `SmoothThreshold(x, edge0,
  edge1)`, not `Mathf.SmoothStep`. On the cone-shaped stick UVs this
  reads as dark streaks running along the grain (not the blotchier look
  the flat 2D texture swatch shows in isolation) — inspected directly via
  a rendered close-up before accepting it, per the project's "check
  actual pixel output" convention; reads well as charred wood either way.
- **Two separate meshes/renderers on purpose** (`Rocks`, `Wood`) — lets
  `Campfire.SetLit()` swap material on the wood only, rocks always stay
  on `RockChunk.mat`.
- Script: `Tools/Blender/GenerateCampfireModel.py`, kept in the repo
  (unlike the lost Trimmed Stick script) so it can be re-run/tweaked
  later — rock count, stick count, ring radius, lean angle are all
  parameters near the top.
- Scaled to a 0.95m footprint against measured bounds (not assumed),
  grounded and verified per CLAUDE.md's imported-model rules — see
  `CHANGELOG.md`'s v0.3.27-dev entry for the full verification trail and
  the two prefab-editing bugs hit and fixed along the way.

## 4. Data shape — built (v0.3.26-dev through v0.3.30-dev)

- `Campfire` holds: `isLit` + a `FuelTier`-driven burn timer (1 fuel
  slot), **4 accessory slots** (`grillSlot`/`cookingPotSlot`/`kettleSlot`/
  `fryingPanSlot`, each a capacity-1 `Inventory` restricted to its own
  `ItemDefinition`), a 4-slot ingredient input pool (`inputInventory`),
  and a 4-slot output bank (`outputInventory`) — all plain `Inventory`
  instances using the existing `restrictedTo` mechanism, not a bespoke
  named-slot structure.
- **`CookableItem` ScriptableObject**, restructured v0.3.30-dev to
  mirror `CraftingRecipe`'s own shape instead of
  `EdibleItem`/`MedicineItem`/`FuelItem`'s single-field pattern:
  `ingredients[]` (item+count array), `outputItem`, `outputCount`,
  `cookDurationSeconds`, and `requiredAccessory` (`ItemDefinition`,
  nullable — null means open-flame, no accessory needed; set means that
  exact item must be seated in one of the 4 accessory slots).
  `Campfire.GetAvailableRecipes()` filters the registered set down to
  what's currently satisfiable; `StartCooking()` commits to one (consumes
  ingredients immediately, one recipe cooking at a time, real-time timer
  pausing while unlit/player away, landing in the output bank on
  completion — manual trigger via CampfireScreen's Recipe button, not
  the originally-planned auto-cook).
- **`CookedMeat`'s own `ItemDefinition` + `EdibleItem` registration** —
  Raw Meat itself deliberately stays un-eatable (no `EdibleItem`), so
  cooking is required, not optional. Meal-tier `FoodTier` (40 hunger).
  `RawMeatToCookedMeatCookable.asset` is still the only recipe that
  actually exists — anything accessory-gated (Soup, boiled water, etc.)
  remains unscoped beyond "the accessory exists and gates it."
- **Warmth:** a proximity check nudging `bodyTemperature` upward while
  the player is within `warmthRange` of a lit Campfire, toward
  `warmthTarget` (80) at `warmthRatePerSecond` (5/s).
- **`VitalsBarHUD` has a real Body Temperature bar**, same treatment as
  the other 5 vitals.

## 5. Deferred ideas (not decided, logged for later)

- **A real water-safety mechanic** (untreated water risks sickness,
  boiling at the Campfire purifies it) was raised and explicitly
  deferred — Ben's call: meaningfully bigger scope than this round,
  revisit once there's a concrete reason to build it (e.g. a recipe or
  survival-pressure need that actually depends on it).
- **Wood Stove, as a future upgrade using the Campfire as a template**
  (2026-08-12): Ben's idea — this design (fuel slot, cooking slot,
  accessory slots) is meant to be reusable for a genuinely better cooking/
  heating structure later, not a one-off. **Explicitly decided: no new
  "structure tier" concept** — a Wood Stove is a different *structure*
  from a Campfire (different capability, not a quality-tier of the same
  object), so it does **not** reuse `CraftTier` the way a Backpack's 5
  quality tiers do (same mistake CLAUDE.md's tier-scaling gotcha already
  warns against — Crude/Masterwork means skill level, not
  technology/building era). Instead: **Wood Stove is simply its own
  recipe with a higher `SkillRequirement` than Campfire's**, the same
  ordinary skill-gate every other recipe already uses — no new enum, no
  new scale. Not scoped beyond that today: which skill(s) gate it, its
  exact fuel/cooking/accessory-slot counts (presumably more than
  Campfire's 1/1/4, but unconfirmed), and whether it's Woodworking-only
  or also needs Metalworking (a wood-and-metal appliance) are all open.

## Cross-references

- `WOOD_AND_FUEL_PLANNING.md` — the `FuelTier`/`FuelItem` system this
  reuses directly, and the Furnace design this deliberately mirrors in
  shape (fuel inventory, burns while lit regardless of active use).
- `CLAUDE.md`'s `Mathf.SmoothStep` gotcha — applies directly to the
  charred-wood texture work.
- `Tools/Tripo3D/README.md`'s Blender from-scratch modeling notes (the
  Trimmed Stick tiers) — the precedent for building this model in Blender
  rather than Tripo3D or a bespoke procedural C# script.
