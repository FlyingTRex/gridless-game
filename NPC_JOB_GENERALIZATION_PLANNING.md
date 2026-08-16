# NPC Job Generalization Planning

**Status: built, v0.3.32-dev (2026-08-13)** — sections 1-6 below are now
shipped (see `CHANGELOG.md`'s v0.3.32-dev entry for the built shape).
Section 7 (bench-crafting families) remains planning-only, not scoped in.
Left as-written below rather than rewritten past-tense — it's still the
accurate design record of what got built and why.

Planning doc for generalizing the Hireable NPC system beyond Mining
(2026-08-13). Ben's ask: the player should be able to assign an NPC to
Mining *or* Woodworking (or, eventually, any craft family except Building),
and the NPC should carry out that job based on equipped tools and skill —
the same way Mining already works today. **Scope for this pass, per Ben's
call:** gathering families only — Mining, Woodworking (including felling
standing Trees), and Berry/Herb foraging (section 3a, added after Ben's
follow-up ask: the NPC actually searches a bush and then walks over and
collects whatever scattered on the ground, skipping BerryBush's separate
chop-for-Trimmed-Stick action entirely). Bench-crafting families
(Metalworking, Sewing, Stonework, Carpentry, Forging, Minting) are a real
second phase, sketched at the end of this doc but not scoped into this
build.

## 1. What's already generic (audited directly against the code, not assumed)

The Hireable NPC system (`NPCJob.cs`/`NPCJobDefinition.cs`/`NPCJobScreen.cs`,
2026-08-10) is far less mining-specific than its class names suggest:

- **`NPCJobDefinition`** is pure data — `family` is any `SkillDefinition`,
  `toolRequirements` is any set of tool categories. Nothing about it
  assumes Mining.
- **`NPCJobScreen`** already renders family-tabs → job-tiles generically
  (same shape as `CraftingScreen`'s discipline tabs), reading from
  hand-maintained `families[]`/`jobs[]` arrays — adding a new family/job
  today is a data change (new `NPCJobDefinition` asset + appending to those
  arrays), not a code change.
- **`NPCMining.cs`'s actual loop**, despite the name, targets *any*
  `ResourceNode` in the world (`FindObjectsByType<ResourceNode>`), filters
  by whatever tools the assigned job's `toolRequirements` gave the NPC
  (`job.HasAnyTool`), and trains whichever skill the assigned job names
  (`assignedJob.family`) — not a hardcoded Mining skill. **Fallen Log nodes
  are already a `ResourceNode`** (Axe-gated, yields Plank + a Stick chance
  — see `WOOD_AND_FUEL_PLANNING.md`), so an NPC "Chop Wood" job targeting
  them would work **today, with zero code changes** — just a new
  `NPCJobDefinition` data asset.

So the gathering generalization is mostly already done. The one real gap:
**standing Trees are a different component with a different interaction
shape**, covered next.

## 2. The real gap: standing Trees (`ChoppableTree.cs`) aren't `ResourceNode`s

`ChoppableTree` predates the NPC system and was never touched by it —
`Complete()` is hard-wired to `PlayerEquipment`/`PlayerSkills`, and instead
of yielding an item+count directly (`ResourceNode.PeekYield`/
`TryMineForNPC`), it spawns `logCount` separate `Log` GameObjects as
scattered physics objects into the world. An NPC has no "walk over and
collect what I just knocked loose" step today, so it can't consume that
model as-is.

**Good news: `ResourceNode` itself already establishes the exact pattern
needed**, and it's not hypothetical — it's shipped, working code
(`ResourceNode.cs`):

- `Complete(player)` (the **player** path) spawns physical chunk objects
  via `SpawnChunk` — scatter, physics impulse, walk-over-and-pick-up.
- `TryMineForNPC(out item, out count)` / `PeekYield(...)` (the **NPC**
  path) walk the same `chunkPrefab` chain and resolve straight to an
  item+count, skipping the scatter-and-collect step entirely — "for an NPC,
  the yield is just a number, it doesn't need a physical pickup step" (see
  that method's own comment, 2026-08-10).

`ChoppableTree` needs the identical split: keep `Complete()`'s scatter
behavior for the player exactly as-is, and add a parallel
`TryChopForNPC(out item, out count)` / `PeekYield(...)` pair that instead
resolves directly to `logPrefab`'s item, `logCount` times, and puts the
tree into stump state — no new gameplay behavior, just the same
already-proven pattern applied to a second component.

**What item does an NPC-felled tree actually yield?** `logPrefab` is
itself a `Log` `ResourceNode` (pickupable directly since v0.3.25-dev, or
further choppable into 2× Plank + a Stick chance). Simplest, most
consistent choice: `ChoppableTree.TryChopForNPC` yields raw **Log** items
directly into NPC cargo (same as the player's existing F-key "pick up Log"
secondary action, which also grants no tool/skill involvement beyond the
tree-felling itself) — it does *not* auto-continue into Plank/Stick.
An NPC that's also willing to process Log nodes (which the same "Chop
Wood" job's Axe requirement already qualifies it for, per section 1) will
naturally pick those up as a *separate* target the next time it searches —
no special-casing needed, the generic search loop just finds whichever
`ResourceNode`/`ChoppableTree` target is nearest and carriable next. Its
cargo ends up a natural mix of Log/Plank/Stick depending on what it
actually harvested, all deposited to the same box regardless.

## 3. Proposed shape: a shared `INPCHarvestable` interface

To let one gathering loop target both `ResourceNode` and `ChoppableTree`
(and any future gatherable type — Fiber/Berry nodes, if those are ever
built as their own component) without hardcoding a second parallel search
path, extract the small slice of API the loop actually needs into an
interface both implement:

```csharp
public interface INPCHarvestable
{
    bool IsAvailable { get; }
    ItemDefinition[] RequiredTools { get; }
    float SkillGain { get; }
    Transform transform { get; } // implicit on any MonoBehaviour
    bool PeekYield(out ItemDefinition item, out int count);
    bool TryHarvestForNPC(out ItemDefinition item, out int count);
}
```

- `ResourceNode` already exposes every one of these members today (just
  under the name `TryMineForNPC` instead of `TryHarvestForNPC` — a rename,
  not new behavior) — implementing the interface is a signature-only
  change.
- `ChoppableTree` gains `IsAvailable => !IsStump`, a public `RequiredTools`
  accessor (currently private), a public `SkillGain` accessor (currently
  private, needed since `MineCurrentTarget`\-equivalent reads
  `currentTarget.SkillGain` directly rather than the node's own
  `trainedSkill` — the *job's* family skill trains, not the node's, same
  as today), plus the new `PeekYield`/`TryHarvestForNPC` pair from section 2.
- The gathering loop's `FindTarget()` searches `FindObjectsByType<ResourceNode>()`
  and `FindObjectsByType<ChoppableTree>()` (two type-specific calls merged
  into one "best so far" comparison — cheaper and simpler than reflecting
  over all `INPCHarvestable` implementors in the scene) instead of just the
  first.

No `NPCJobDefinition` schema change needed — the existing `toolRequirements`
already discriminates families in practice (an Axe-only NPC won't qualify
for a Pickaxe-gated ore node, a Pickaxe-only NPC won't qualify for an
Axe-gated Tree/Log), the same implicit filter the system already relies on
today.

## 3a. Berry/Herb foraging: a third, genuinely different gathering shape

Ben's follow-up ask (2026-08-13): add Berry/Herb gathering too, with the
NPC actually searching a bush and then walking over to pick up whatever
lands on the ground — not a direct-yield shortcut like sections 2-3 above.
**Skip `BerryBush`'s separate chop-for-Trimmed-Stick action** (E, requires
Knife/Axe) — Ben's explicit call; only the F-search half is in scope.

Audited `BerryBush.cs`/`HerbBush.cs`/`Pickup.cs` directly:

- **Both bushes' search action (`CompleteSecondary`) never yields an item
  directly at all** — unlike `ResourceNode`, there's no `PeekYield`/
  `TryXForNPC` pair to add. It always scatters `Pickup` prefabs onto the
  ground (`SpawnScattered`, a fixed-ring offset chosen specifically so a
  scattered pickup can't land inside the bush's own always-enabled
  collider — see that method's comment), on an independent respawn
  cooldown, exactly like the player experiences it. So this genuinely is a
  two-step action for an NPC: trigger the search, then separately walk to
  and collect whatever it produced — Ben's framing is exactly right, not
  a simplification opportunity like Tree-felling had.
- **`Pickup.cs` has the same "hard-wired to the player" problem
  `ResourceNode`/`ChoppableTree` had before their NPC paths existed** —
  `Complete(player)` reaches for `PlayerLoot`/`PlayerInventory` directly.
  Needs the same treatment: a `TryPickupForNPC(out item, out count)`
  mirroring `ResourceNode.TryMineForNPC`'s shape (no player dependency,
  respects `canRespawn`/`respawnDelay` the same way, no skill-gain call
  inside it — same reasoning as `ResourceNode`: the *job's* family skill
  trains, not the pickup's own `trainedSkill` field, so a public
  `SkillGain` accessor is enough, mirroring `ResourceNode.SkillGain`).
  Bush-spawned pickups never call `Configure()`, so they have no despawn
  timer — an NPC has no urgency to rush a scattered berry before it
  vanishes, unlike a player-dropped item.
- **Doesn't fit the `INPCHarvestable` interface from section 3 cleanly** —
  that interface's contract is "yields an item directly into cargo on
  success." A bush search yields nothing into cargo; it just seeds the
  world with new `Pickup` objects. Forcing it into the same interface
  would make `TryHarvestForNPC` lie about what it does. Cleaner as its own
  small interface:

  ```csharp
  public interface INPCSearchable
  {
      bool IsAvailable { get; }
      void TriggerSearchForNPC();
  }
  ```

  `BerryBush`/`HerbBush` implement only this (their F-search half — the
  chop half stays player-only, untouched, same "new NPC path, old player
  path unchanged" pattern as every other node this doc touches).

- **`NPCGathering`'s target search generalizes to three candidate pools**,
  not two: `INPCHarvestable` (walk to it, harvest, item lands in cargo
  immediately — ore/rock/Tree/Log), `INPCSearchable` (walk to it, trigger
  the search, nothing lands in cargo yet), and **loose `Pickup` objects
  already sitting in the world** (walk to it, collect, item lands in
  cargo) — the last pool is what actually closes the loop after an
  `INPCSearchable` trigger: on its *next* target search, the NPC finds the
  `Pickup`s its own search just created (or any other loose `Pickup`
  already nearby) and walks over to collect them, the same "nearest thing
  I can currently do something useful with" comparison already used for
  the other two pools, just executed differently once reached. No new
  state machine required — this falls out of the existing "keep finding
  and doing the nearest available thing" loop for free once `Pickup` is a
  third scanned type.

  **One side effect worth flagging, not silently deciding:** scanning all
  loose `Pickup` objects generically means a foraging NPC will also
  collect *any* nearby dropped item, not just ones its own bush search
  produced — e.g. a player-dropped item sitting nearby, or a scattered
  chunk from a Rock a Mining NPC broke that nobody came back for. This
  reads as a reasonable, even useful, side effect (a NPC quietly tidying
  loose items into its deposit box) rather than a bug, but it's a real
  behavior change worth Ben's explicit sign-off before shipping, not an
  assumption baked in silently.

- **Job data (decided, Ben via `AskUserQuestion`, 2026-08-13): one
  combined `ForageJob.asset`**, not separate Gather-Berries/Gather-Herbs
  jobs — `family = Gathering` (existing `Gathering.asset`, unused by NPCs
  today), no tool requirement (matches the F-search action needing none),
  covers both `BerryBush` and `HerbBush` as fair-game `INPCSearchable`
  targets. Matches how `MVP2_PLANNING.md` item 2 already bundled these as
  one line ("Gathering (Berry/Herb bushes)"), not two.

## 4. Rename `NPCMining` → a generic name

Now that the loop is provably generic (not just in theory — it's about to
target Trees too), the class name should stop implying it's mining-only.
Proposed: `NPCMining.cs` → `NPCGathering.cs`, same file otherwise (a
`MonoBehaviour` rename doesn't break existing scene/prefab component
references — Unity resolves a serialized component by its script's GUID,
not the class name string). `NPCFactoryWorker.prefab` is the only prefab
referencing it today (grep-confirmed) — its `RequireComponent`
attributes (`NPCWander`/`NPCJob`/`NPCSkills`/`NPCEncumbrance`/`NPCCargo`)
carry over unchanged since none of those are mining-specific either.

Internal renames for clarity, same rationale: `MineCurrentTarget` →
`HarvestCurrentTarget`, `mineRange`/`mineDuration`/`mineTimer` → 
`harvestRange`/`harvestDuration`/`harvestTimer` (comments throughout
already describe the loop generically — the identifiers should match).

## 5. New data needed

- **`ChopWoodJob.asset`** (`NPCJobDefinition`): `jobName = "Chop Wood"`,
  `family = Woodworking`, `toolRequirements = [Axe, Backpack]` — **built
  with the Backpack gate included**, matching Mine Ore's pattern of
  requiring carrying capacity up front (resolved rather than left open,
  given Log/Plank are heavier items than ore chunks).
- **`ForageJob.asset`** (`NPCJobDefinition`): `jobName = "Forage"`,
  `family = Gathering`, `toolRequirements = [Backpack]` — same resolution,
  no tool needed for either bush's search action beyond carrying capacity.
- Add `Woodworking` and `Gathering` to `NPCJobScreen.families[]`, and
  `ChopWoodJob`/`ForageJob` to `NPCJobScreen.jobs[]` (both hand-maintained
  arrays wired on the Player prefab/scene object, same as every other
  array of this shape in the project).

No new `SkillDefinition`s needed — `Woodworking.asset` and
`Gathering.asset` both already exist (confirmed in `Assets/Data/`), just
not yet used by anything NPC-facing.

## 6. Build order (once this moves from planning to code)

1. `INPCHarvestable` interface; `ResourceNode` implements it (rename
   `TryMineForNPC` → `TryHarvestForNPC`, everything else already matches).
2. `ChoppableTree` implements it: new public accessors, new
   `PeekYield`/`TryHarvestForNPC` pair mirroring `ResourceNode`'s exact
   split (scatter for player, direct-yield for NPC) — `Complete()` (the
   player path) stays completely untouched.
3. `Pickup.cs` gains `TryPickupForNPC(out item, out count)` + a public
   `SkillGain` accessor (section 3a) — mirrors step 2's split exactly,
   `Complete()` (the player path) untouched.
4. `BerryBush`/`HerbBush` implement `INPCSearchable` (section 3a) — F-search
   half only, chop half untouched.
5. Rename `NPCMining.cs` → `NPCGathering.cs`; generalize `FindTarget()` to
   search all three candidate pools (`INPCHarvestable`, `INPCSearchable`,
   loose `Pickup`s); rename mining-specific identifiers per section 4.
   Update `NPCFactoryWorker.prefab`'s `RequireComponent` set only if the
   class name change affects it (it shouldn't — component references
   survive by script GUID).
6. `ChopWoodJob.asset` + `ForageJob.asset`; wire both into
   `NPCJobScreen.families`/`jobs`.
7. Batch-mode compile check + YAML grep verification, same convention as
   every build this session.
8. Manual Play-mode pass: hire an NPC, assign Chop Wood, equip an Axe,
   confirm it fells a standing Tree (stump appears, no scattered Log
   objects spawn for the NPC path), confirm it also processes a fallen Log
   node it wanders past, confirm mixed Log/Plank/Stick cargo deposits
   correctly into its assigned `StorageBox`. Separately, hire/assign a
   second NPC to Forage — confirm it walks to a Berry or Herb bush,
   triggers the search (no tool needed), then walks over and collects the
   scattered pickups that produced, depositing them the same way. Confirm
   it also sweeps up any other loose `Pickup` nearby (the flagged side
   effect from section 3a) — decide with Ben whether that's acceptable
   live rather than after the fact. Confirm a Mining-assigned NPC run at
   the same time is unaffected (regression check on the shared loop).

## 7. Bench-crafting families (Metalworking, Sewing, Stonework, Carpentry,
   Forging, Minting)

**Status: planned in full 2026-08-16, not yet built.** Was deferred at
the end of section 6's build; this is the follow-up pass Ben asked for
("let's do some planning on bench training" → "I meant benchcrafting").
Design below is decision-locked (confirmed via `AskUserQuestion`) and
ready to build — nothing here is still open except what's explicitly
flagged as such.

### 7.1 Shape: a new sibling component, not a `NPCGathering` extension

Bench crafting has no "target in the world to walk up to and hit
repeatedly" the way gathering does — the action is "run a
`CraftingRecipe`," not "harvest a node." But it *does* share gathering's
"walk to a required surface first" shape for any recipe with
`requiresAnvilSurface`/`requiresFurnace` set (`AnvilSurface`/
`FurnaceSurface` are exactly the right targets to walk to). New
`NPCCrafting.cs`, sibling to `NPCGathering.cs` — both `RequireComponent`'d
on the hireable NPC prefab (matching the existing pattern where every
`NPCJob`-driven component is always present, gated by whether it's
actually the assigned job's shape), each bailing out early in `Update()`
if the assigned job isn't its own kind.

**`NPCJobDefinition` gains `public enum JobKind { Gathering, Crafting }`
+ `public JobKind kind = JobKind.Gathering;`** — default keeps
`MineOreJob`/`ChopWoodJob`/`ForageJob` behaving exactly as today with no
data migration needed (they're all implicitly `Gathering` already).
`NPCGathering.Update()` gains one more early-out:
`if (job.AssignedJob != null && job.AssignedJob.kind != JobKind.Gathering) return;`
— `NPCCrafting.Update()` does the mirror check for `Crafting`.

### 7.2 Recipe selection: a per-NPC queue, not "craft anything in family"

**Decided (Ben, `AskUserQuestion`, 2026-08-16): the player queues
specific recipes per NPC**, reusing `Furnace.recipeQueue`/`ToggleQueue`'s
exact shape rather than letting the NPC freely auto-craft any
family-matching recipe it has materials for — same reasoning Furnace's
own queue already established (predictability: the player decides what
gets made, not a background process burning through materials on its
own judgment). `NPCCrafting` gets its own `List<CraftingRecipe>
recipeQueue` + `ToggleQueue(CraftingRecipe)`, mirroring `Furnace`'s
member-for-member.

**Discovery is still family-scoped, just for what's *offerable* to
queue, not what's *auto-crafted*** — the crafting-job UI (see 7.5) only
lists `CraftingRecipe`s whose `trainedSkill` matches the assigned job's
`family`, the same grouping `CraftingScreen`/`NPCJobScreen` already use.
This gets both halves of the originally-flagged open question for free:
family scoping keeps the offered list relevant with no new per-job
recipe-list data asset, and the queue itself is the "explicit selection"
half.

### 7.3 The loop

Each `Update()` tick (while `job.IsReady` and not
`hiring.IsWaitingForPayment`, same readiness gate `NPCGathering` uses):

1. Walk the queue in order; the first entry that's currently
   satisfiable (see 7.4) is `activeRecipe`. None satisfiable → idle
   (resume wandering), same "nothing useful right now" fallback
   `NPCGathering.FindTarget` returning null already has.
2. If `activeRecipe.requiresAnvilSurface` or `.requiresFurnace`, and the
   NPC isn't currently within range of a qualifying surface, walk to the
   nearest one (`FindObjectsByType<AnvilSurface>`/`<FurnaceSurface>`,
   same nearest-in-range scan `NPCGathering.FindTarget` already does for
   harvestables) before proceeding. No surface requirement → craft in
   place, no walk needed (this is Sewing/Woodworking's case — see 7.6).
3. Once in position: a `craftDuration` timer (new field, mirrors
   `harvestDuration`), then commit — consume ingredients from
   `materialsSourceBox` (7.4), deterministic success (**no
   `CraftOutcomeRoll`, confirmed already decided** — `lowerTierItem`/
   `higherTierItem`/the whole chance-of-creation ladder are simply
   unused on this path, same as `SmeltableItem`'s existing precedent),
   output (+ `bonusItem` if set) lands in `outputBox`, `NPCSkills.
   GainExperience(job.AssignedJob.family, recipe.skillGain)`.
4. Repeat from 1.

### 7.4 Satisfiability check (gates both queue-walking and the UI)

A queued recipe is currently craftable if, **all** of:
- Every `ingredients[]` entry's count is present in `materialsSourceBox`.
- `job.HasAnyTool(recipe.requiredTools)` — same check `NPCGathering`
  already uses for harvest targets, reusing whatever tools the player
  handed the NPC via this job's own `toolRequirements`.
- `NPCSkills.GetLevel(recipe.trainedSkill) >=
  CraftTierScale.SkillRequirement(recipe.outputItem.tier)` — the exact
  same threshold check `PlayerCrafting.HasRequiredSkill` uses. An
  under-leveled NPC simply can't queue-execute a recipe yet (not a
  "lower quality" outcome — there's no quality ladder on this
  deterministic path at all, see 7.3).
- **`!recipe.requiresCanteenWater`** — explicitly excluded, not silently
  broken. NPCs have no `Canteen`/liquid concept at all today. A
  water-requiring recipe (Healing Paste) should be filtered out of the
  offerable-to-queue list entirely for now, flagged here rather than
  discovered as a live bug later. Revisit if NPCs ever gain their own
  Canteen equipment.
- `outputBox.HasSpaceFor(...)` — same upfront space check
  `PlayerCrafting.StartCraft`/`Furnace.StartSmelting` both already do.

### 7.5 Data/UI needed

- **`MetalworkingJob.asset`** (`NPCJobDefinition`): `jobName =
  "Metalworking"`, `family = Metalworking`, `kind = Crafting`,
  `toolRequirements = [Backpack]` (matches every other job's carrying-
  capacity-up-front resolution — no held tool needed for the pilot
  recipes, see 7.6). Add to `NPCJobScreen.jobs[]`/`families[]` if
  `Metalworking` isn't already listed there.
- **`NPCCrafting` gets `materialsSourceBox`/`outputBox` fields** (no fuel
  box — `requiresFurnace` just needs the NPC standing near a placed
  `Furnace`'s `FurnaceSurface` marker, not its own fuel supply; the
  Furnace structure manages its own fuel independently). Set via the
  same point-and-confirm flow `PlayerNPCDeposit` already provides for
  `NPCJob.DepositContainer` — either extend `PlayerNPCDeposit` to target
  a second/third box kind, or a small sibling targeting helper; exact
  shape TBD at build time, not a design blocker.
- **New crafting-queue UI** — `NPCJobScreen` (or a new dedicated screen,
  same "Campfire got its own popup instead of overloading Inventory"
  precedent) needs: a family-filtered recipe list with per-recipe
  queue-toggle buttons (mirrors `FurnaceScreen`'s own queue UI almost
  exactly), the two box-assignment buttons, and a live satisfiability
  indicator per queued recipe (materials/tool/skill/space, same
  four-way check as 7.4) so the player can see *why* an NPC is idle
  instead of guessing.

### 7.6 Pilot recipe, once built

`IronIngotRecipe` (existing asset, already `trainedSkill = Metalworking`,
`requiresFurnace = true`, no tool requirement) is the natural first live
test — proves the hardest case (walk-to-a-surface) with zero new recipe
data needed. Assign a hired NPC to `MetalworkingJob`, give it a Backpack,
point its materials box at a box stocked with Iron Ore and its output box
at an empty one, queue `IronIngotRecipe`, confirm it walks to the
Furnace, waits `craftDuration`, and Iron Ingots land in the output box
with Metalworking XP gained — no risk roll, always succeeds once
satisfiable.

### 7.7 Explicitly out of scope for this pass

- **Sewing/Woodworking/Stonework/Carpentry/Forging/Minting** as actually
  *assignable* jobs — the design above is family-agnostic (any
  `SkillDefinition` works as `family`), so adding one is a data-only
  follow-up (`NPCJobDefinition` asset + `NPCJobScreen` array entries)
  once `NPCCrafting` itself is proven on Metalworking. Not built
  speculatively ahead of that proof.
- **Multiple recipes cooking in parallel per NPC** — one `activeRecipe`
  at a time, same single-slot precedent `Campfire`/`PlayerCrafting`'s
  non-batch path already set; a real batch/queue-depth concept (like
  `PlayerCrafting.StartCraft`'s quantity parameter) isn't scoped here.
- **NPC Canteen/liquid-ingredient support** — see 7.4's explicit
  exclusion; would need giving NPCs a `Canteen` equipment concept
  first, a bigger separate piece.

## Cross-references

- `BUGS_AND_ENHANCEMENTS.md`'s "Furnace Fuel System" section, item on
  Woodcutting not existing as an NPC job family yet — this doc is that
  gap's actual design.
- `MVP2_PLANNING.md` item 2, "Expand NPC hiring beyond stonework" — this
  is the concrete plan for that item's Woodworking half.
- `CHANGELOG.md`'s v0.3.31-dev entry (Furnace automation) — the precedent
  this doc's section 7 leans on for "NPC/structure automation is
  deterministic, player-facing crafting keeps the risk roll."
