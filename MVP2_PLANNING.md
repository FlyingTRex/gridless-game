# MVP 2 Planning

Ben's curated list for MVP 2 (2026-08-12), pulled out of the broader Phase 2
ideation in `BUGS_AND_ENHANCEMENTS.md` and `docs/design-brief.md` — those
documents still hold the full backlog, but **this list is the actual MVP 2
scope**, not everything in them. Treat this file as the live planning
surface for MVP 2 specifically; the other two stay as the longer-term
backlog/history they've always been.

Ideation only as of this entry — nothing here is built, ordered, or fully
specced yet.

## The list

1. **Expand Stats** — use Strength as the method/template. — ✅ Done (Intelligence via item 7, v0.3.53-dev; Dexterity/Constitution v0.3.55-dev, `DEXTERITY_CONSTITUTION_PLANNING.md`), committed and pushed; verified via compile + YAML only, not yet live-tested in Play mode.
2. **Expand NPC hiring** beyond stonework (Mining is the only job family today). — 🟡 Woodcutting + Gathering shipped (v0.3.32-dev). Bench-crafting (Metalworking pilot) planned in full (2026-08-16, `NPC_JOB_GENERALIZATION_PLANNING.md` section 7) — new `NPCCrafting.cs` sibling component, `Furnace`-style recipe queue, deterministic (no risk roll), not yet built. Guarding still unstarted/unplanned.
3. **Finish basic starting gear** for a new player. — ✅ Done (2026-08-12).
4. **Player and NPC animation.** — ✅ Shipped (v0.3.33-dev/v0.3.34-dev, 2026-08-13); not yet live-tested in Play mode.
5. **Sky and weather.** — ✅ Built and live-tested (Weather Maker, `WEATHER_MAKER_PLANNING.md`, 2026-08-13).
6. **Save/load persistence.** — ✅ Built and live-tested (v0.3.51-dev, 2026-08-13).
7. **Skill books.** — 🟡 Built (v0.3.53-dev, `SKILL_BOOKS_PLANNING.md` — [summary](https://claude.ai/code/artifact/2af217f7-450e-4e4b-9b09-6411a8b72115)), committed and pushed; verified via compile + YAML only, not yet live-tested in Play mode. Phase 4 (NPC training) correctly blocked on item 2's bench-crafting.
8. **Expand hunting** — diverse animals, and the ability to use a weapon against them. — 🟡 Melee weapon damage framework built (v0.3.61-dev). Ranged combat built (v0.3.86-dev) and polished (v0.3.88-dev, 2026-08-15) — Bow (5-tier) + Stone Arrow (5-tier, gated by Trimmed Stick tier not skill) + Stone Arrowhead, `PlayerRangedCombat.cs` (draw/fire, Strength/Dexterity-scaled), new Archery skill, icons, a visible flying-arrow effect, draw-progress UI, aim zoom, and a real full-body draw/hold/release animation (both player Animator Controllers, using the Human Animations pack's own `BowShot01` clips). Gun, Iron Arrowhead, sound (no audio system exists in this project at all), and NPC archery still open. Animal diversity in progress — 4 new animals designed (Chicken/Pig/Deer/Rabbit, see `HUNTING_EXPANSION` design artifact); 2 of 4 now live via the same generic `PreyCreature.cs`: Chicken (v0.3.87-dev, Feather ×1-3 + Egg ×1) and **Deer (v0.3.95-dev, 2026-08-15, Raw Meat ×2-4 + a new Leather item ×1-2 — the first real source of Leather in the game, closing a long-open BUGS_AND_ENHANCEMENTS.md question)**, both skinned with Knife, trains Gathering, still standing still with no wander/flee AI (PreyCreature's movement half unbuilt). Pig/Rabbit not built. Taming explicitly pinned for a later MVP.
9. **Cooking** — ✅ Done (2026-08-16). Gardening's 16-cell grid built (v0.3.79-dev), 7 crops with real seed-packet models (v0.3.80-dev), 6 of 7 crops grow through real Wild Harvest growth-stage art (v0.3.81-dev), and real harvested-pickup visuals too (v0.3.83-dev) — only Corn's visuals stay placeholder throughout (not in the pack). The 4 Campfire cooking accessories (Grill/Cooking Pot/Kettle/Frying Pan) have real models, icons, and Forging-skill recipes (v0.3.90-dev), each with at least one recipe (v0.3.91-dev through v0.3.94-dev): Grilled Meat, Steak and Potatoes, Herbal Tea (the first recipe needing water — added `CookableItem.requiresCanteenWater`/`Campfire.HasCanteenWater`), Meat Stew — each with its own real merged model. Cooking's skill/quality-tier system is built (v0.3.93-dev, `COOKING_SKILL_PLANNING.md`) — a real success/fail outcome via `CraftOutcomeRoll`, gated by Cooking level, mild Health hit on the worst roll. **Seed sourcing closed v0.3.96-dev (2026-08-16)**: `CropDefinition.seedDropChance` (30%) gives harvesting a chance to return a seed, and 7 `GardenPlot4x4` instances scattered around `TestScene.unity` (pre-seeded, one per crop, 7 Ready cells each) give a fresh game a real in-world first-seed source — not the originally-envisioned wild-forage nodes, but closes the same gap (Ben's call). Verified via compile + YAML only — not yet live-tested in Play mode.
10. **"Prefab" buildings** — ✅ Built and placed in `TestScene.unity` (v0.3.69-dev/v0.3.70-dev) — a dev-facing Editor menu tool, 4 composite buildings (Small Hut/Rectangular House × Twig/Plank). Rectangular House has a known gable-end roof gap, logged.

## First ideation pass (2026-08-12)

### 1 — Expand Stats
`PlayerEncumbrance.cs` is the template: Strength grows via skill-via-use
(tiered gain rate off carried-load ratio, calibrated to a real-time pacing
target) and drives a derived gameplay effect (carry capacity via
`Capacity = 17.3925 x Strength^1.5`). Dexterity/Constitution/Intelligence
need the same two halves each — a trigger and an effect. Candidates raised
so far:
- **Dexterity** — effects: movement/sprint speed, attack speed or ranged
  accuracy, dodge/evasion, crafting speed on fine work. Triggers: sustained
  sprinting, landing hits, dodging damage, crafting Fine/Masterwork items.
- **Constitution** — effects: max health/stamina, stamina regen, resistance
  to cold/heat/poison, reduced damage taken, faster wound recovery.
  Triggers: sustained high exertion (mirrors Strength's near-capacity
  pattern), surviving damage, enduring environmental extremes.
- **Intelligence** — effects: skill XP gain multiplier, crafting
  quality/recipe discovery, magic power/Will pool, NPC management
  efficiency. Triggers: crafting, casting wishes, reading skill books
  (direct tie to item 7), teaching/mentoring NPCs.

**Open question above — resolved 2026-08-14, deliberately small.** An
Intelligence-driven XP multiplier shipped (`xpGained *= 1 + intLevel/2000`,
+5% at cap, never applies to Intelligence's own gains) — small enough that
it doesn't snowball Int into mattering more than the other three, per
Ben's explicit call ("very small").

**Intelligence's trigger+effect resolved via item 7** — see
`SKILL_BOOKS_PLANNING.md`. Reading/writing became the concrete trigger,
mirroring `PlayerEncumbrance`'s Strength pattern exactly.

**Dexterity/Constitution built 2026-08-14** — full design and build in
`DEXTERITY_CONSTITUTION_PLANNING.md` (v0.3.55-dev). Landed differently
from the candidates sketched above: Constitution's trigger ended up
exercise-based (sprinting + a secret soccer-kick bonus), not the
adversity-based "surviving damage" candidate originally floated; its
effect is Max Health/Max Stamina via an additive front-loaded curve
(a pure power law couldn't hit both a sane low anchor and a front-loaded
shape at once). Dexterity's effect narrowed to just movement speed (not
also attack speed/dodge/crafting speed as originally brainstormed); its
triggers are sprinting (shared with Constitution), sneaking, jumping, and
completing any `CraftingRecipe` — the manual-vs-machine distinction
(hand-crafting trains it, Furnace/Campfire automation doesn't) needed no
new field, since `CraftingRecipe` vs. `SmeltableItem`/`CookableItem`
already is that exact boundary in the data model. This closes out item 1
entirely — all four core stats now have a real trigger and effect.

### 2 — Expand NPC hiring beyond stonework
**Woodcutting and Gathering (Berry/Herb) shipped 2026-08-13 (v0.3.32-dev)**
— full design in `NPC_JOB_GENERALIZATION_PLANNING.md`, summarized in
`CHANGELOG.md`'s v0.3.32-dev entry. `NPCMining.cs` renamed to
`NPCGathering.cs` (the mechanism really was mostly content, as guessed
below — a new `INPCHarvestable`/`INPCSearchable` interface pair let
`ChoppableTree`/`BerryBush`/`HerbBush` plug into the *same* loop instead
of needing an `NPC<Job>` behavior each). Still open: **Guarding** (not
started — a materially different job shape, defense/patrol rather than
gather-and-deposit) and the full **bench-crafting** generalization
(Metalworking, Sewing, etc. — explicitly deferred, see that planning
doc's section 7). Still blocked *cosmetically*, not functionally, by item
4 — a Woodcutting/Foraging NPC with no chop/search animation reads exactly
as "bleh" as Mining does today; nothing in this build changed that.

**Also now a blocker for item 7's Phase 4** (`SKILL_BOOKS_PLANNING.md`)
— NPC book-training has nothing to attach to until this item's deferred
bench-crafting sub-scope ships.

<details>
<summary>Original ideation (2026-08-12), kept for history</summary>

Only one job family exists today (`Mining` -> `Mine Ore`, via
`NPCJobDefinition`/`NPCJob`/`NPCMining`). The mechanism is proven — a new
family is mostly content (a new `NPCJobDefinition` + an `NPC<Job>` behavior
mirroring `NPCMining`). Natural next families: Woodcutting (`ChoppableTree`
already exists), Gathering (Berry/Herb bushes), Guarding. Blocked
*cosmetically*, not functionally, by item 4 — a woodcutting NPC with no
swing animation reads exactly as "bleh" as mining does today.

</details>

### 3 — Finish basic starting gear
**Done as of 2026-08-12.** Auto-equip-at-spawn now covers Settler's
Shirt (v0.3.12-dev), Settler's Jeans, Settler's Belt with a Canteen clipped
to it, and Settler's Sneakers (all same session) — the clothing+canteen
side of "Boots? Canteen?" from the original open question is fully closed.

**Decided out of scope (2026-08-12, Ben's call): no starting Knife.**
`docs/game-overview.md`'s backstory text mentions "a military-grade
survival knife" as part of the crash-landing gear, but this project's
actual starting-gear implementation won't include one — dropped from the
requirements. (Open question, not yet asked: whether `game-overview.md`'s
prose should be updated to match, since it still names the knife
explicitly — flagging rather than silently leaving the doc inconsistent
with the real decision.)

**Closed 2026-08-12 (v0.3.23-dev): starting food rations.** New "MRE
Ration" item — 0.3 lbs, no recipe, spawns 2 into the starting Settler's
Shirt's own pocket storage at game start (`PlayerShirt.startingRationItem`/
`startingRationCount`). Eaten via the same right-click Eat action every
other `EdibleItem` uses — 25 Health instantly, plus 15 more over 60s
(`EdibleItem` gained an optional heal-over-time component for this,
reusing `PlayerVitals.StartHealOverTime`). Closes the last gap against
`docs/game-overview.md`'s "a small cache of survival rations" line — item 3
is now **fully done**, both the clothing/canteen side and the food side.

### 4 — Player and NPC animation
**Shipped 2026-08-13**, both halves, same day: NPC animation (v0.3.33-dev)
and the player half — visible body + first/third-person camera toggle
(v0.3.34-dev). Both committed and pushed to `origin/main`. Direct
follow-ups landed the same day too: Male/Female body toggle
(v0.3.35-dev), then the full equipment-visual bone-attach sweep
(v0.3.38-dev through v0.3.50-dev) that put items 2 and 3's gear onto an
actual body instead of pure bookkeeping — the dependency this item was
unblocking.

**Not yet live-tested in Play mode** — see `TEST_FEATURE_PLAN.md`
sections 24 (NPC animation) and 25 (player visible body/camera toggle).
Worth a real pass before calling this fully closed, same caveat save/load
carried until today's confirmation.

### 5 — Sky and weather
**Built and live-tested 2026-08-13** — see `WEATHER_MAKER_PLANNING.md`.
Weather Maker (Digital Ruby, v8.1.0) fully replaced the old procedural
sky texture (deleted, along with the `Mathf.SmoothStep` cloud bug it
carried) with a real sky/cloud/day-night/precipitation system. `Player
WeatherEffects.cs` bridges live precipitation intensity into
`PlayerVitals.bodyTemperature` via the existing `WarmNear`, delivering
the actual item 1 (Constitution)/item 9 (warm food/tea) tie-in, not just
visuals. Ben watched a complete day/night cycle live end to end (day →
dusk → a genuinely striking sunset → full night with a textured moon) —
real confirmation, not a clean-compile assumption. Two project-wide
changes (URP Render Pipeline Asset replacement, Gamma → Linear color
space) were each explicitly confirmed with Ben before running. Three real
bugs hit and fixed along the way: two missing built-in Unity modules, a
Mirror API version mismatch in an out-of-scope optional script, and a
shipped day/night profile with `Speed`/`NightSpeed` both frozen at `0`
(found live when asked how long until night, fixed by tuning to a ~3
real-minute day for testing pace).

### 6 — Save/load persistence
**Built (v0.3.51-dev, 2026-08-13).** Full implementation plan in
`SAVE_LOAD_PLANNING.md`, build detail in `CHANGELOG.md`'s v0.3.51-dev
entry. Manual Save button (` menu, Player tab), loads automatically on
game start if a save file exists. Live-tested by Ben with a real
Editor-restart round trip (not just re-entering Play mode): worn
equipment (Backpack, Belt with a clipped Canteen) and nested equipment
contents (11 Sticks inside the worn Backpack) both survived exactly.
Remaining untested per `TEST_FEATURE_PLAN.md` section 30: full vitals
round-trip, Canteen liquid specifically, StorageBox, ResourceNode respawn
timing, Hireable NPC state — architecturally covered, just not yet
walked through live.

Several other systems already worked around its absence — e.g. hireable
NPCs' "5-minute stand-in for 5 real days" job-shift length exists only
because there was no persistence layer to make the real duration
meaningful. Revisiting that stand-in with a real multi-day timer is a
natural follow-up now that persistence exists, but is its own separate
piece of work, not bundled into this build.

**Known future follow-up from item 7** (`SAVE_LOAD_PLANNING.md` section
10): once skill books are built, this system needs a small increment to
capture `knownLineages`/`bookGrantedRecipes`/`bookGrantedWishes` plus
`SkillBook` item instances — composes almost for free since `SkillBook`
is designed as an `IEquippable`, but isn't automatic.

### 7 — Skill books
**Design worked out and built same day (2026-08-13)** — see
`SKILL_BOOKS_PLANNING.md` ([rendered summary](https://claude.ai/code/artifact/2af217f7-450e-4e4b-9b09-6411a8b72115)).
Reading/writing became a direct trigger on Intelligence (item 1's
proposed training trigger, mirroring `PlayerEncumbrance`'s Strength
pattern) — `PlayerMagic.StartingLineage` became a real `knownLineages`
set with no cap, the real code prerequisite the design phase surfaced.
A crafting/weapon skill book grants one specific `CraftingRecipe` as a
standing exception to the normal skill gate, not a level/XP boost; a
magic wish book (e.g. "Fireball") does the same for a `WishRecipe` *and*
unlocks its lineage if not already known — one unified mechanic, not two
separate systems. Writing reuses `PlayerCrafting`'s `CraftOutcome` roll
directly via a newly-extracted shared `CraftOutcomeRoll.cs` — no new
formula needed. New "Writing" tab in `PlayerMenuScreen`; reading hooks
into `InventoryScreen`'s equipment-action popup (same shape Canteen's
Drink/Fill use — a real mid-build correction from the originally-sketched
`PlayerEating`-style dispatch, which only fits plain stackable items).
Two "found" skill books placed in `TestScene.unity` for easy testing.
**Phase 4 (NPC training) correctly left unbuilt** — blocked on item 2's
bench-crafting, which doesn't exist yet. Rare magic-teaching NPCs and
NPCs writing their own books both explicitly deferred to a later MVP.
Verified via 10+ rounds of batch-mode compile + direct YAML grep —
**not yet live-tested in Play mode.**

### 8 — Expand hunting
Melee weapon skills got a real framework first (v0.3.61-dev) — a Knife
deals tier-scaled bonus damage on top of the base Bare-handed punch, and
any future melee weapon plugs in via one `ItemDefinition` flag.

**Ranged combat built (v0.3.86-dev, 2026-08-14/15)** — designed live
with Ben across a long propose-confirm-adjust session (see the published
"Hunting Expansion" design artifact), then built the same session. New
`PlayerRangedCombat.cs`: hold-left-click to draw, release to fire, a
sibling script to `PlayerCombat` (charge-and-release is a different
shape from melee's instant tap) — `PlayerCombat` itself now bails out of
punching whenever a Bow is held so the two never fight over one click.
Bow is a full 5-tier ladder mirroring Knife's exact shape (2 Stick + 1
Rope every tier, `CraftOutcomeRoll`-linked), held in one hand slot — no
two-handed equip system needed, since the *other* hand holds whichever
Arrow tier the player wants to fire, doubling as ammo selection with
zero new plumbing (Ben's "what if we were lazy" pivot). Stone Arrow is
also 5-tier, but gated by which Trimmed Stick tier feeds the recipe
(deterministic, no roll) rather than by skill — "the quality of the
stick determines the result." Damage stacks Arrow's tier bonus
(primary, +0 to +6) with Bow's (secondary, +0 to +1.5) on a small
`Random(2,4)` base, all scaled by how far the shot was drawn; accuracy
is a random spread cone driven by Arrow tier and tightened further by
Dexterity, which also speeds up the reload cooldown — filling a gap
flagged when Dexterity was originally built ("ranged accuracy/attack
speed" was one of its cut candidate effects). New Archery skill, Combat
category, trained per shot. 3 new from-scratch Blender models (Bow,
Stone Arrow, Stone Arrowhead), all player-scale-checked.

**Explicitly pinned out of this pass**: a Gun (second ranged weapon
type, different sourcing), and the whole Taming half of the original
"tame, hunt, harvest, skin" backlog vision (a real companion-AI system,
wholly separate) — both Ben's call.

**Animal diversity — Chicken's "hunt" half built, movement still
open.** Same design session picked 4 new animals (Chicken, Pig, Deer,
Rabbit) to round the roster to 5 with Wolf. Real asset win found
mid-session: `ithappy Animals_FREE` (a second new pack Ben added)
includes fully animated Chicken and Deer prefabs with walk/run/idle
clips — a real upgrade over Wolf's own zero-animation
`transform.MoveTowards` movement.

**Chicken built (v0.3.87-dev, 2026-08-15)** — new generic
`PreyCreature.cs` (killable/lootable, same tool-gated hold-to-skin/
respawn shape `HostileCreature` proved out for Wolf, deliberately no
attack/chase state machine) plus two new Blender models, Feather and
Egg. Killing and skinning a Chicken now drops 1-3 Feather + 1 Egg
(both guaranteed), trains Gathering. Built generic/reusable, not
Chicken-specific, so Pig/Deer/Rabbit can reuse the same component. What
it's still missing is movement — no wander/flee AI exists yet, so the
Chicken just stands still until killed (`CreatureMover`, the pack's
own movement/animation engine, is untouched and ready to be driven by
a future AI script). Pig and Rabbit still need their own model source.
Deer's Hide closes a real, separately long-flagged gap: no raw
Leather/Hide material has existed in this project at all, and the
Leather Backpack recipe has been sitting on a placeholder Rock
ingredient waiting for exactly this.

The animation gap that used to block weapon-use readability (item 4) is
now resolved for animation *in general* (item 4 shipped), but neither
the Bow's own draw animation nor the new animals' AI-driven movement
actually exist yet — both still read as instant/static in-game.

### 9 — Cooking
**Planned in full, 2026-08-14 — see `COOKING_AND_GARDENING_PLANNING.md`.**
The Campfire's cooking mechanism already shipped (`CAMPFIRE_PLANNING.md`,
v0.3.26-dev through v0.3.30-dev) but has no skill/quality concept at all.
Design now covers: a new Cooking skill + a real 5-tier Cooked Meat ladder
reusing crafting's existing `CraftOutcomeRoll`/`lowerTierItem`/
`higherTierItem` quality mechanic (near-zero new code, just new data),
quality scaling `EdibleItem`'s secondary Health-boost field (not
`FoodTier`/hunger — that axis is deliberately protected from crafting-
quality scaling, see `FoodTier.cs`). Also grew into a full new Gardening
system (Carrot/Potato/Corn via a 4×4 `GardenPlot`, wild-forage seed
sourcing) once seed availability came up mid-conversation. New
recipes (Soup, Tea, fried dishes) deliberately left unenumerated — Ben's
call, add them incrementally once the mechanism is built. Strong tie to
item 5 (weather-driven demand for warm food/drink) and item 1
(Constitution).

**Single-plant proof of concept built (v0.3.71-dev, 2026-08-14)** —
see `COOKING_AND_GARDENING_PLANNING.md` section 5. A scoped-down single
`GardenPlot` (Berry Bush only, reusing the existing model, a real small
Blender-built raised bed, 3-stage growth, Cooking-gated `BuildPiece`)
proves the "plant a seed stack, harvest auto-replants until exhausted"
mechanic before building the full 16-cell grid.

**Full 4x4 (16-cell) grid built (v0.3.79-dev, 2026-08-15)** — see
`COOKING_AND_GARDENING_PLANNING.md` section 3/6. `GardenPlot4x4`
generalizes the single-plot mechanic to 16 independent cells and any
number of `CropDefinition`s (Carrot/Potato/Corn shipped). Deliberately
deviates from the plain-`Inventory`-per-cell idea in the original
planning (index instability risk) and skips a second drag-and-drop UI
(a click-based context panel covers the same mechanic). Visuals are
placeholder primitives — the Asset-Store-pack-vs-Blender question is
still open. Seed sourcing is Admin-Spawn-only; wild forage nodes aren't
built. The Cooking skill/quality-tier system itself (`CookableItem`
gaining a real tier ladder) is still unbuilt — see section 2.

### 10 — "Prefab" buildings
**Scope resolved (2026-08-14): dev-facing level-design tool**, not the
bigger player-facing blueprint feature (players saving their own builds
as reusable stamps) — that stays a distinct, undesigned future item if
ever picked up, since it needs real gameplay UI plus a save format
(leaning on item 6) rather than just an Editor script.

**Built (v0.3.69-dev).** Four composite building prefabs (Small Hut /
Rectangular House, each Twig and Plank) hand-assembled via a batch-mode
Editor script that replicates `PlayerBuilding`'s own socket-snap math,
plus a permanent `PrefabBuildingPlacer.cs` with four
`Gridless/Place Prefab Building/...` Editor menu items (drops at the
Scene view's pivot, samples real terrain height, full Undo support).
Small Hut's roof (a true square footprint) came out correct on the
first pass, confirmed via a render screenshot. Rectangular House ships
with a known, logged issue — its two gable ends have broken roof
geometry, since the existing `RoofPanel` piece is built for a
point-meeting pyramid roof, not a ridge-line gable roof; no gable-end
piece exists yet.

**All 4 placed into `TestScene.unity` (v0.3.70-dev)** — a loose square
around the player's spawn point, so the world is actually populated with
them now, not just sitting as unused prefab assets. Placed via a
throwaway batch script (not the live Editor menu, since a headless
session can't drive `SceneView.lastActiveSceneView`) — hit and fixed a
real `EditorSceneManager` silent-save gotcha along the way (see
`CLAUDE.md`). See `BUGS_AND_ENHANCEMENTS.md` for the Rectangular House
gable-end roof issue.

## How the list clusters

- **Foundation tier** (de-risks/unlocks others): item 4 (animation) and
  item 6 (persistence) — both are infrastructure other items either need
  directly or are currently working around.
- **Stat/world-sim cluster**: items 1 + 5 + 9 reinforce each other
  (Constitution, weather, food/warmth) into one coherent survival loop
  instead of three separate features. **Items 1 and 5 are now both
  built** — `PlayerWeatherEffects.cs` delivers real weather→body-
  temperature cooling, and Constitution now grows Max Health/Max Stamina
  (though it doesn't yet grant cold/heat resistance the way the original
  item-1 brainstorm floated — that never made it into the actual build,
  see the item 1 section above). Item 9 (warm food/tea countering cold)
  is the one leg still missing to complete the loop this cluster was
  always describing.
- **NPC/labor cluster**: item 2 leans on item 4 for presentation and item 3
  for what a hired NPC starts equipped with.
- **Combat/hunting cluster**: item 8 leans on item 1 (Dexterity, now built
  — though item 1's effect narrowed to movement speed only, so item 8
  would need its own attack-speed/accuracy hook rather than assuming
  Dexterity already provides one) and item 4 (weapon animation).
- **No longer standalone: item 7 (skill books)**, now built (v0.3.53-dev,
  `SKILL_BOOKS_PLANNING.md`) and cross-linked three ways — it advanced
  item 1 (Intelligence's trigger) for free, its NPC-training phase is
  blocked on item 2 (bench-crafting), and it creates a real follow-up
  increment for item 6 (new save-state surface). Item 10 still needs its
  scope question answered before any of this clustering applies to it.

Build order not yet decided.
