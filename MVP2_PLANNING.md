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

1. **Expand Stats** — use Strength as the method/template.
2. **Expand NPC hiring** beyond stonework (Mining is the only job family today).
3. **Finish basic starting gear** for a new player.
4. **Player and NPC animation.**
5. **Sky and weather.**
6. **Save/load persistence.**
7. **Skill books.**
8. **Expand hunting** — diverse animals, and the ability to use a weapon against them.
9. **Cooking** — supplements healing (better foods give health/healing); teas and drinks.
10. **"Prefab" buildings** — drop a full premade building into a scene, rather than piece-by-piece.

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

**Open question, not decided:** an Intelligence-driven XP multiplier would
make Int's own growth compound into every other stat's growth rate — could
tie the stat block together nicely, or could snowball into Int mattering
more than the other three. Needs a conscious call either way.

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
Likely the single biggest, riskiest item on this list — there's an existing
unresolved ideation thread on this from 2026-08-11
(`BUGS_AND_ENHANCEMENTS.md`'s "Next Session: NPC Model, Animation &
Equipment Visuals"), three model-source options being compared, nothing
decided yet. Nearly everything else on this list depends on it visually:
item 2 (NPCs actually look like they're working), item 3 (gear shows on a
body instead of being pure bookkeeping), item 8 (weapon swings are
readable). Worth treating as its own mini-project, and worth sequencing
relatively early since so much else is blocked *cosmetically* even where
it isn't blocked *functionally*.

### 5 — Sky and weather
The procedural sky texture already exists but has a known unresolved bug
(the `Mathf.SmoothStep` vs. GLSL `smoothstep` mismatch documented in
`CLAUDE.md`, affecting cloud coverage). Weather (temperature swings, rain)
would be new. Strong natural tie to item 1 (Constitution resisting
cold/heat) and item 9 (warm food/tea countering cold) — these three could
become one coherent survival mini-system instead of three unrelated
features.

### 6 — Save/load persistence
Also has an existing narrow-scope v1 draft in `BUGS_AND_ENHANCEMENTS.md`,
never built. Several other systems already work around its absence today —
e.g. hireable NPCs' "5-minute stand-in for 5 real days" job-shift length
exists only because there's no persistence layer to make the real duration
meaningful. Building this earlier could let items 1 and 2's pacing be
designed properly instead of around a limitation.

### 7 — Skill books
Directly the unlock vehicle for Intelligence's proposed training trigger
(item 1) and for teaching NPCs. Also unlocks the second magic lineage and
Scrolls per `docs/design-brief.md`'s Magic System section. A good hub item
— small on its own, but several other items lean on it existing.

### 8 — Expand hunting
Today: one huntable animal (Wolf, via `HostileCreature`), and only
"Bare-handed" of five originally-named combat skills actually exists. This
item is really three bundled together: new animal types, real ranged/melee
weapon skills, and the animation to make weapon use readable (ties back to
item 4). Worth deciding whether MVP 2 wants all three or just animal
variety first.

### 9 — Cooking
Natural extension of the existing `EdibleItem`/skill-tied-quality pattern
(a "Cooked" tier, same five-tier convention crafting already uses) rather
than a new system from scratch. Supplementing healing via food quality
closes a nice loop with Phase 1's basic first aid. Teas/drinks would reuse
the Canteen liquid mechanic. Strong tie to item 5 (weather-driven demand
for warm food/drink) and item 1 (Constitution).

### 10 — "Prefab" buildings
**Needs a scope decision before further ideation:** is this a
**dev-facing level-design tool** (drop a finished structure into the scene
fast, to populate the world for testing/design purposes) or a
**player-facing feature** (players save/stamp their own building layout as
a reusable blueprint, in-game)? Very different builds — one's an Editor
script, the other's real gameplay UI plus a save format (and would lean on
item 6). Not yet decided which Ben meant.

## How the list clusters

- **Foundation tier** (de-risks/unlocks others): item 4 (animation) and
  item 6 (persistence) — both are infrastructure other items either need
  directly or are currently working around.
- **Stat/world-sim cluster**: items 1 + 5 + 9 reinforce each other
  (Constitution, weather, food/warmth) into one coherent survival loop
  instead of three separate features.
- **NPC/labor cluster**: item 2 leans on item 4 for presentation and item 3
  for what a hired NPC starts equipped with.
- **Combat/hunting cluster**: item 8 leans on item 1 (Dexterity) and item 4
  (weapon animation).
- **Standalone-ish**: item 7 (skill books) is small and mostly just needs
  deciding; item 10 needs its scope question answered first.

Build order not yet decided.
