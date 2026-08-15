# Cooking & Gardening Planning

Planning doc for MVP2 item 9 (Cooking), expanded to include a new Gardening
system once seed sourcing came up (2026-08-14). Decisions below are locked
in via the same propose→confirm→adjust conversation this project's other
planning docs used; open items are flagged as such.

**Single-plant proof of concept built same day (v0.3.71-dev).** Before
building the full 4×4/16-cell `GardenPlot` design in section 3, Ben asked
for a scoped-down single-plot version to prove the core mechanic out —
one small raised bed, one plant (Berry Bush, since Berry/`BerrySeed`
already exist and are sourced), a real Blender model, placeable via the
Build tab. See section 5 for the full build writeup; sections 2-4 below
are still the target design for the eventual scaled-up version, not yet
built.

## 1. Current state (audit, 2026-08-14)

- **Cooking's mechanism already exists** (`Campfire`/`CookableItem`,
  shipped v0.3.26-dev through v0.3.30-dev, see `CAMPFIRE_PLANNING.md`): a
  1-fuel-slot, 4-accessory-slot (Grill/Cooking Pot/Kettle/Frying Pan),
  4-ingredient/4-output Campfire with a manual Recipe button. Exactly one
  recipe is registered today (`RawMeatToCookedMeatCookable`, Raw Meat →
  Cooked Meat, no accessory needed).
- **`CookableItem` has no skill or quality concept at all** — no
  `trainedSkill`, no tier-roll, unlike `CraftingRecipe`. This is the gap
  MVP2's original item-9 brainstorm called for ("skill-tied-quality
  pattern... a Cooked tier") but was never built.
- **`FoodTier` (hunger restore) is deliberately a separate axis from
  `CraftTier` (skill quality)** — there's an explicit comment in
  `FoodTier.cs` warning against conflating them, the same "a ratio tuned
  for one quantity doesn't transfer to another" mistake `CLAUDE.md`
  already flags for Encumbrance vs. capacity. Cooking skill/quality must
  NOT scale hunger restore.
- **`EdibleItem` already has a secondary Health-boost field**
  (`vital`/`restoreAmount`, plus optional heal-over-time) — MRE Ration
  already uses it. This is the natural target for "quality affects
  healing," not `FoodTier`.
- **`CraftingRecipe` already has `lowerTierItem`/`higherTierItem` + a
  shared `CraftOutcomeRoll`** margin-based quality mechanic (skill vs.
  tier requirement rolls BadFailure/BarelyFail/Success/BrilliantSuccess,
  shifting output up/down a tier) — extracted to `CraftOutcomeRoll.cs`
  during Skill Books (`SKILL_BOOKS_PLANNING.md`). Reusing it for cooking
  is near-zero new mechanics, just new data.
- **Raw ingredients today**: Berry, Herb, Raw Meat — all sourced from
  systems that already exist (`BerryBush`/`HerbBush` foraging, Wolf
  hunting). No Carrot/Potato/Corn/Egg/Bacon exist.
- **`BerrySeed.asset` already exists and is already sourced** —
  `BerryBush.TriggerSearchForNPC()` has a 2% "super success" bonus-seed
  chance on a berry search, independent of the normal Berry yield. Never
  consumed by anything, since no Gardening system exists yet. This is the
  precedent Gardening's own seed-sourcing design (section 3) reuses
  directly.
- Carrot/Potato/Corn would need Gardening (this doc); Egg/Bacon would need
  actual livestock (a separate, bigger, still-fully-unbuilt "Animal &
  hunting module" backlog item) — explicitly out of scope here.

## 2. Cooking — skill & quality

- **New `Cooking` skill** (`SkillCategory.CraftingDiscipline`, same
  family as Woodworking/Metalworking/Sewing).
- **`CookableItem` gains `trainedSkill`/`skillGain`** (mirrors
  `CraftingRecipe`), gated the same way `PlayerCrafting.HasRequiredSkill`
  gates a recipe: `CraftTierScale.SkillRequirement(outputItem.tier)`
  against the player's Cooking level (or a book-granted exception, same
  shape as crafting/wish books already support).
- **`CookableItem` gains `lowerTierItem`/`higherTierItem`** and cooking
  now rolls a `CraftOutcomeRoll` outcome (margin = Cooking level − the
  recipe's tier requirement) on completion, same as ordinary crafting.
  Cooked Meat becomes a real 5-tier ladder (Crude → Masterwork, same
  shape as Backpack/Pickaxe), each tier its own `ItemDefinition` +
  `EdibleItem`.
- **Quality scales the `EdibleItem` Health-boost (`vital`/
  `restoreAmount`), NOT `FoodTier`/hunger** — every Cooked Meat tier
  keeps the same `FoodTier.Meal` (40 hunger) `RawMeatToCookedMeatCookable`
  already uses; only the secondary Health effect grows with tier,
  closing the "healing via food quality" loop from MVP2's original
  brainstorm without touching the hunger axis `FoodTier.cs` explicitly
  protects.
- **New recipes (Soup via Cooking Pot, Tea/boiled water via Kettle, a
  Frying Pan dish) are explicitly NOT enumerated in this doc.** Ben's
  call (2026-08-14): "we can introduce new food items and recipes as we
  go along" — the mechanism (skill/quality/accessory-gating) is what
  needs to be right up front; specific dishes are free to add
  incrementally later without touching any of the systems above. Tea/
  boiled water in particular would need `LiquidType` expanded beyond
  today's `Water`-only enum and a Canteen-drink-effect hookup — real
  scope, deliberately deferred rather than designed blind here.

## 3. Gardening — new system

- **`GardenPlot`**: a 4×4 (16-cell) placeable structure. Each cell is a
  plain, stackable `Inventory.Slot` (item + count) inside one 16-capacity
  `Inventory`, restricted to registered seed items — deliberately reuses
  `CampfireScreen`'s proven drag-and-drop ingredient-grid pattern instead
  of inventing new UI, just sized up from 4 cells to 16. **No
  Campfire-style recipe-matching needed** — each cell is independent,
  just "this seed type, this timer, ready or not," not a multi-ingredient
  combination.
- **Per-cell state** (parallel array, indexed 0–15, alongside the seed
  `Inventory`): `Empty` / `Growing` (with a `readyAt` real-time deadline,
  same `Time.time`-deadline pattern `Despawn.cs` already uses) /
  `ReadyToHarvest`.
- **Planting**: drop a stack of seeds into an empty cell → immediately
  consumes 1 seed from the stack and starts growing. Topping up an
  already-growing cell's stack doesn't interrupt the current plant.
- **Growth is passive, always ticking** — no proximity requirement (grows
  whether or not the player is nearby), no watering/tending mechanic for
  v1. Matches how the project's other real-time systems (Furnace fuel
  burn, `RandomWeatherController`) already run unattended in the
  background.
- **Harvesting is a manual click per ready cell** (same feel as
  Campfire's Take button), not an auto-collect output bank. **A
  ready-but-uncollected plant blocks that cell** — it will not silently
  keep consuming the stack while an unharvested plant sits there; exactly
  one active plant per cell, ever.
- **Auto-replant on harvest**: collecting a ready plant immediately starts
  growing the next one from the same stack if `count > 0` after the
  harvest; an empty stack leaves the cell `Empty`, waiting for a fresh
  drop.
- **Grow durations — different per crop** (Ben's call): Carrot 5 real
  minutes, Potato 10 real minutes, Corn 15 real minutes.
- **Visuals — distinct per crop** (Ben's call, overriding the initially-
  proposed generic-sprout simplification): each of Carrot/Potato/Corn
  gets its own simple growing-plant shape in the cell, not one shared
  placeholder sprout. Empty = flat tilled soil; growing = the crop's own
  small low-poly plant shape, scaling up toward `readyAt`; ready = a
  brighter/highlighted material variant, same "reads as ready from a few
  steps away" precedent as Campfire's ember-glow-means-lit treatment.
  Base structure itself: a raised wooden-frame bed (Plank/Stick
  materials), reusing the established Blender-from-scratch pipeline
  (`Tools/Blender/`) rather than Tripo3D, same as Campfire/Trimmed
  Stick/Shovel.
- **Open, pinned (2026-08-14): whether to use a paid Asset Store pack for
  the crop plant models instead of custom Blender work.** [Wild Harvest:
  Root Vegetables](https://assetstore.unity.com/packages/3d/vegetation/plants/wild-harvest-root-vegetables-295553)
  ($24.99, NV3D) covers Potato/Carrot/Turnip/Sweet Potato/Onion with
  growth-stage variants and URP support — Corn isn't included and would
  still need a custom model either way. Visually reviewed (screenshots,
  not just the spec sheet): the pack's plants are noticeably leafier/more
  detailed/"painterly-shaded" than this project's established low-poly
  from-scratch look (Campfire, Trimmed Stick, the Ingots, the Shovel) —
  a real, visible style gap, not a subtle one, though arguably less
  jarring on a small contained bed than it would be on a larger prop.
  Not decided either way — revisit before building the Garden Plot's
  visuals.
- **Recipe/skill gate — ties to the new Cooking skill** (Ben's call,
  2026-08-14: "maybe we tie this to a 'cooking' tier?"), not Woodworking
  despite the wood-frame material — same "early, low-`SkillRequirement`
  building recipe" framing Campfire got (Rudimentary tier). A cook
  growing their own ingredients is the thematic hook.

## 4. Seed sourcing

- **Primary, real, buildable now: wild forage nodes.** One per crop
  (`WildCarrotPatch`/`WildPotatoPatch`/`WildCornStalk` or similar), same
  shape/interaction as `BerryBush` — primarily yields the raw crop item on
  harvest, plus a small bonus-seed chance reusing `BerrySeed`'s exact 2%
  "super success" precedent (`berrySeedChance` on
  `BerryBush.TriggerSearchForNPC()`). Foraging becomes both immediate food
  and the on-ramp into growing your own — no admin-spawn-only stopgap
  needed for v1.
- **Secondary, future, explicitly blocked: Traveling Trader stock**
  (Ben's addition, 2026-08-14). Seeds becoming purchasable ties into the
  5-band Traveling Trader system `FAME_PLANNING.md` already designed (not
  built) — blocked on the exact same missing prerequisite already flagged
  there: no vendor/commerce system exists in this codebase in any form.
  Not a new blocker, just another consumer of the existing one. Revisit
  once that system gets built.

## 5. Single-plant proof of concept — built (v0.3.71-dev, 2026-08-14)

Deliberately simplified from sections 2-4's full design to prove the core
mechanic first, on one plant, before investing in the full 16-cell grid:

- **New `GardenPlot.cs`** (single-slot, not the eventual per-cell-of-16
  version) — `IInteractable`, one growth cycle at a time. Press E on an
  empty plot while carrying Berry Seed plants your *entire current stack*
  at once (the mechanic actually being proven, not a UI test) and starts
  growing the first one; press E on a ready plot to harvest one Berry
  Bush's worth of Berries, which immediately starts the next seed growing
  if any remain in the stack, or returns to empty once exhausted.
  Deliberately skips the full design's drag-and-drop popup screen — a
  real `Inventory`-slot-per-cell only makes sense once there's an actual
  grid UI to drag into; this tracks the seed count as a plain int instead.
- **Growth — 3 discrete stages** (Ben's call: not smooth continuous
  scaling), thresholds at 1/3 and 2/3 of a 5-real-minute grow duration
  (matching the Carrot number floated in section 3), scale multipliers
  0.35× → 0.65× → 1.0×.
- **Visual reuses the actual `BerryBush` model directly** (Ben's "we have
  a berry bush" idea) — instantiated as a child, its own `BerryBush`
  component and colliders stripped at runtime (purely decorative reuse,
  not a second independently-searchable bush living inside the plot). No
  new plant modeling work needed at all.
- **New small raised-bed model** (`Tools/Blender/GenerateGardenPlotModel.py`,
  kept in the repo like every other from-scratch Blender script) — a
  simple ~0.8m wood-frame box with a soil interior, genuinely small-scale
  (single-plant), distinct from the eventual 4×4/5m version sections 2-4
  describe.
- **New `Cooking` SkillDefinition** (`SkillCategory.CraftingDiscipline`) —
  the skill itself now exists, though nothing trains it yet (that's
  sections 2's quality-roll work, not built this pass).
- **New `GardenPlotPiece` `BuildPiece`** (2 Plank + 2 Stick, Crude tier,
  trains Cooking) — placed via the existing Build tab/`PlayerBuilding`
  socket-free free-placement flow, same pattern `Campfire` already uses
  (`groundReach: 0`, no `BuildSocket` children).
  One instance placed directly into `TestScene.unity` near (4, -4) for
  immediate testing, alongside registering the piece into the scene's
  `PlayerBuilding.allPieces` array (the Build tab's manually-curated
  list) so more can be crafted normally.
- **Not built this pass, deliberately deferred**: icon/preview icon for
  `GardenPlotPiece` (blank tile in the Build tab for now, same "null
  means blank spacer" convention every other icon-less piece already
  uses); the ready-state brightness/highlight material swap sections 2-4
  describe (the plant reaching full scale is today's only "it's ready"
  signal); Carrot/Potato/Corn and the full 16-cell grid itself.

## Cross-references

- `CAMPFIRE_PLANNING.md` — the `Campfire`/`CookableItem` mechanism this
  extends, and `CampfireScreen`'s drag-and-drop grid pattern the Garden
  Plot's UI reuses directly.
- `SKILL_BOOKS_PLANNING.md` — origin of the shared `CraftOutcomeRoll.cs`
  utility this reuses for cooking's quality roll.
- `FAME_PLANNING.md` — the Traveling Trader design the future seed-selling
  channel would hook into, and its own commerce-system blocker.
- `BUGS_AND_ENHANCEMENTS.md`'s Phase 2 backlog — "Animal & hunting
  module" (blocks Egg/Bacon, explicitly out of scope here) and
  "Gardening" (this doc supersedes that bullet's one-line placeholder).
- `FoodTier.cs` — the hunger-restore axis cooking quality must NOT touch;
  see its own comment for why.
