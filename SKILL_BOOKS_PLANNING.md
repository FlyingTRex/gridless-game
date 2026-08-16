# Skill Books Planning

MVP2 item 7 (`MVP2_PLANNING.md`). Full design worked out in one session
(2026-08-13) — every major question resolved; what remains is either
number-tuning or explicitly deferred to a later MVP (rare magic-teaching
NPCs, NPCs writing their own books). Planning only, nothing built yet.

Rendered summary artifact: https://claude.ai/code/artifact/2af217f7-450e-4e4b-9b09-6411a8b72115

## Why this, why now

Ben's MVP2 list flags skill books as small on its own but a real hub
item — three other pieces already lean on it existing rather than
inventing their own unlock mechanic:

- **Intelligence (MVP2 item 1)** — one of the candidate training triggers
  raised for Intelligence is "reading skill books," directly tying this
  item to that one.
- **Magic lineages** (`docs/design-brief.md`'s Magic System section) —
  every player starts with one free lineage; the other three are
  explicitly gated behind "apprentice under an NPC or read a
  lineage-specific tome/scroll" — this is Phase 2 scope specifically
  *because* it depends on the skill-books mechanic not existing yet.
- **Scrolls** — found/scribed Scrolls (an "Unidentified Scroll" whose
  lineage+wish rolls at read time) are called out as riding "this same
  mechanic, not a separate system" in both `design-brief.md` and
  `BUGS_AND_ENHANCEMENTS.md`.
- **Teaching NPCs** — `MVP2_PLANNING.md` item 7 also names this as a
  target use case, distinct from the player's own training.

## What's already decided elsewhere (constraints, not open questions)

- **Skill-via-use stays the primary path.** `design-brief.md` is explicit
  that this is "an alternate path alongside learn-by-doing," not a
  replacement — every skill in the game (7 crafting disciplines, 5
  weapon-usage skills, 4 magic lineages, core stats) already grows by
  doing the thing. Skill books add to that, they don't gate it.
- **Magic lineage unlock is a real, separate consumer of this mechanic**,
  already specced at the design-doc level: reading a lineage-specific
  tome/scroll (or apprenticing under an NPC — a *non-book* path to the
  same unlock, worth remembering this isn't 100% book-exclusive) opens
  that lineage's skill track at 0, same starting point the free starting
  lineage had.
- **Scrolls are a distinct item from skill books**, even though both are
  "read" items and both ride the same underlying unlock mechanic —
  Scrolls roll a random lineage+wish at read time and are consumed for an
  immediate effect; a skill book is deliberately chosen (not random) and
  trains a skill over time/on read, not "casts something once."

## Decided so far (2026-08-13)

- **Reading a book grants a specific recipe, not a skill-level boost.**
  Refined mid-session — see "What a crafting/weapon book actually grants"
  below for the current shape, which supersedes the original "unlocks a
  skill at 0" framing for crafting/weapon disciplines specifically. The
  "unlocks a currently-untrainable skill at 0" framing still holds
  as-is for magic lineages, which don't have recipes to grant.
- **Scope: crafting + weapon + magic skills**, not core stats. Core stats
  (Strength/Dexterity/Constitution/Intelligence) stay pure skill-via-use,
  no book variant.
- **Consumed on read** — same convention as Scrolls and `EdibleItem`'s
  eat-and-consume pattern, not a keepable reference item.
- **Resolved**: crafting/weapon skills do *not* get retroactively locked.
  They stay open from level 0 exactly as today — a book for one of these
  disciplines grants one specific recipe as a standing exception to the
  normal skill-gate check, it doesn't touch the discipline's actual skill
  level or lock anything that's currently unlocked.

## Superseded idea: a separate Reading/Writing sub-skill

First framing (2026-08-13, Ben): a new standalone Reading/Writing skill
under Intelligence, trained via skill-via-use, gating the ability to read
books. **Replaced same session by the simpler version below** — kept here
for history, not the current direction.

## Current direction (2026-08-13, Ben): reading/writing is a trigger on Intelligence itself

Simpler than a separate sub-skill: reading (and later writing/scribing)
is just one more trigger on Intelligence directly, same single-stat
pattern `PlayerEncumbrance` already established for Strength (a real
trigger + a real effect, no intermediate skill needed). Joins the other
candidate Intelligence triggers already raised in `MVP2_PLANNING.md` item
1 (crafting Fine/Masterwork items, casting wishes, teaching NPCs) rather
than replacing them.

**Resolved by the writing mechanic below**: the "does reading need its
own gate" question turns out to be the wrong question. The real risk/
skill-check moment is at **writing** time, not reading time — a finished
book's quality is baked in when it's written; reading it just applies
whatever quality it already has. No separate reading-side gate needed.

## Writing a book (2026-08-13, Ben): reuses `PlayerCrafting`'s outcome-roll pattern directly

Ben's proposal: writing a skill book compares the author's *subject-matter*
skill against their Intelligence — a Masterwork Sewer with Intelligence 2
writing a book about Sewing risks a bad result specifically *because* the
gap between "how good my hands are" and "how well I can teach it" is
large. Outcome tiers, from Ben's own framing: a horrible result makes the
book unusable; a minor failure only raises the reader's skill a little; a
success/super success raises it more, with better outcomes also boosting
the *author's* own Intelligence gain from writing it.

This maps directly onto `PlayerCrafting.cs`'s existing `CraftOutcome`
roll (`SpectacularFailure`/`BadFailure`/`BarelyFail`/`Success`/
`BrilliantSuccess`, odds interpolated by `Mathf.Lerp` off a skill margin,
via `RollOutcome`) — no new formula needed, just different inputs:

- **Margin = author's Intelligence level − `CraftTierScale.SkillRequirement`
  of the subject skill's tier.** Exactly the same margin shape crafting
  already uses (`actual skill − required skill for the tier`), just with
  Intelligence standing in for the subject skill and the *book's* tier
  standing in for the crafted item's tier. A high-Sewing/low-Int author
  writing about a high tier gets a deeply negative margin — same "risky"
  end of the existing odds curve, no separate curve to design.
- **Outcome → pass/fail only** (refined 2026-08-13, after the recipe-grant
  shape below replaced the variable-strength-unlock idea — there's no
  "how far above 0" to grade anymore, just whether the book works):
  - `SpectacularFailure`/`BadFailure` → book unusable, matches Ben's
    "horrible result."
  - `BarelyFail`/`Success`/`BrilliantSuccess` → book works, grants
    whatever it grants (see below) in full — no partial/degraded version.
- **Author's own Intelligence gain still scales with outcome**, per Ben's
  explicit call — a `BrilliantSuccess` grants more Int XP for having
  written it than a `BarelyFail` does, even though the book's own effect
  on the reader doesn't vary by outcome anymore.

**Resolved (2026-08-13, Ben)**: `SpectacularFailure` does carry an extra
penalty, same spirit as crafting's — not just "book unusable," it harms
the character too. **Damage: random 2–10** (crafting's own
`SpectacularFailureDamage` is a flat `10f` — writing gets its own rolled
range instead of reusing that flat number).

## What a crafting/weapon book actually grants (2026-08-13, Ben)

Ben's example: a Masterwork Metalworker (gold jewelry) writes a book
about it. A reader who's never touched Metalworking reads it — they get
**the one specific recipe the book was written about, nothing else at
that tier.** If they then craft with it, that trains their Metalworking
level the normal skill-via-use way; once that real level naturally
crosses the tier's threshold, every recipe at or below that tier opens up
normally and the book's exception stops mattering.

This is a materially different (and better-bounded) reward than a raw
skill-level/XP grant — it's precise, and it creates the training loop for
free: the one granted recipe *is* the on-ramp to the real unlock, not a
shortcut around it.

**Implementation shape this implies**: a book targets one specific
`CraftingRecipe` asset (not just a `SkillDefinition` + tier), and reading
it adds that recipe to a per-player tracked exception set. `PlayerCrafting
.HasRequiredSkill(recipe)` — which currently just checks
`skills.GetLevel(recipe.trainedSkill) >= CraftTierScale.SkillRequirement
(recipe.outputItem.tier)` — would need an `|| bookGrantedRecipes.Contains
(recipe)` added, rather than any change to the skill-level math itself.

## Applying the same logic to magic (2026-08-13, Ben): a Fireball book

Ben's example: a Restoration-only character reads a book on "Fireball"
(an Elemental wish) — it should grant both the Elemental lineage itself
*and* the specific Fireball wish. Checked directly against
`PlayerMagic.cs`, and this turns out to be **one unified mechanic after
all**, not two separate book types as the previous section guessed —
`WishRecipe` is already structured as `CraftingRecipe`'s exact sibling
(`lineage` instead of `trainedSkill`, `unlockTier` instead of an output
item's tier), and `PlayerMagic.CanAttempt` already gates on exactly two
things: `IsLineageKnown(wish.lineage) && skills.GetLevel(wish.lineage) >=
CraftTierScale.SkillRequirement(wish.unlockTier)`.

A wish book satisfies both halves of that same gate at once:

1. **Lineage known** — if the reader doesn't have Elemental yet, reading
   grants it (the "unlocks the skill track at 0" framing, unchanged).
2. **Wish-tier exception** — same recipe-exception mechanism as a
   crafting book, just applied to a `WishRecipe` (Fireball) instead of a
   `CraftingRecipe`, bypassing the `unlockTier` check for that one wish
   regardless of current lineage level.

**Confirmed (2026-08-13, Ben): the wish exception is scoped to that one
named wish only, not the whole lineage's wishlist.** After reading the
Fireball book, the reader knows Elemental (so any Crude-tier Elemental
wish is open the normal free-Crude way) and can specifically attempt
Fireball — but a separate wish like Spark, if its tier sits above Crude,
stays gated exactly like any other wish: reachable only by training
Elemental up through ordinary casting (skill-via-use, likely via Fireball
itself), or by finding/being given a *separate* book or Scroll that
specifically targets Spark. One book, one wish exception — never a
blanket unlock of everything in the lineage.

A plain "lineage tome" with no specific wish targeted is just the
degenerate case of the same mechanic: unlock the lineage at 0, grant no
wish exception, and whatever Crude-tier wish(es) exist in that lineage
become castable for free anyway (Crude's threshold is 0, same as Crude
crafting — "everyone starts able to" the base tier).

**Real code gap this surfaces, not just a data-shape question**:
`PlayerMagic.IsLineageKnown(lineage)` today is literally
`lineage == StartingLineage` — a single field, not a collection. **A
player cannot know more than one lineage in code at all right now.**
Building any of this needs `StartingLineage` to become a real
`knownLineages`-style set first — a genuine prerequisite piece, not
just "add a line to a list" once book-reading exists.

**Confirmed (2026-08-13, Ben): no cap.** A player should be able to
eventually know all 4 lineages if they put in the time/opportunity cost
to do so — matches `design-brief.md`'s existing "a character can train
one lineage deeply or spread across all four... not a gated choice"
line, now confirmed as applying to the book-unlock path specifically too
(not just a theoretical ceiling in the design doc). `knownLineages` needs
to hold up to all 4, not cap out at 2 or 3.

**Sourcing implication**: a wish book *within* an already-known lineage
can be self-written by a sufficiently skilled/Intelligent author (same as
a crafting book). But the *first* time a given character ever gains a
second lineage, it can't be self-authored — nobody can write about a
lineage they don't have — so that first copy has to come from outside.
See "Where skill books come from" below for the three resolved sources.

## Where skill books come from (2026-08-13, Ben) — resolved, three sources

1. **Random world drops.** Any skill or magic book can turn up as loot in
   game-spawned chests — not tied to anyone having written it. The "first
   copy of a lineage nobody around has yet" problem gets solved for free
   by loot existing independently of the player-authorship loop.
2. **Player-to-player trade, riding the existing writing mechanic.** If
   you know a magic lineage (or crafting/weapon skill), you can write it
   up and sell the book — someone else with a *different* lineage can do
   the same and sell to you. This is how players fill in lineages they
   didn't randomly start with and haven't found in loot, once someone
   else in the (eventually multiplayer) world already knows it.
3. **Rare magic-teaching NPCs.** A new NPC archetype, distinct from the
   existing generic Hireable NPCs — explicitly **rare**, not a common
   vendor. This is the concrete shape of `design-brief.md`'s
   "apprentice under an NPC" path, now confirmed as its own special NPC
   type rather than something any hired worker could do. **Explicitly
   deferred to a later MVP** — see "NPCs and magic" below.

## Lineage tome starting level (2026-08-13, Ben) — resolved

Only a **`BrilliantSuccess`** ("spectacular success," the best outcome
tier — note the asymmetric naming: the roll's worst outcome is
`SpectacularFailure` but its best is `BrilliantSuccess`, not
"SpectacularSuccess") grants a head start above 0. `Success` and
`BarelyFail` both still unlock the lineage — outcome stays pass/fail for
*whether* the book works — but land at a plain 0 start; only the top
outcome tier adds a bonus above that. This is the one place magnitude
still matters post-refinement — crafting/wish recipe grants stay a flat
"you get the recipe, full stop" with no such bonus, since there's no
"above 0" concept for a fixed recipe grant to begin with.

**Bonus amount resolved: random 1–10** (raw skill-level points, same
0–100 scale every skill already uses) — a `BrilliantSuccess` lineage tome
lands the reader somewhere in that range instead of exactly 0.

## NPCs and magic (2026-08-13, Ben) — resolved

**NPCs cannot be trained in magic at all** — not via a book, not any
other way. Magic stays player-only. This resolves the magic half of the
"NPC training" question outright; whether hired NPCs can be
book-trained in *crafting/weapon* skills (and whether that reads a book
itself vs. the player reading it "at" the NPC, per `TryGiveTool`'s
hand-over-an-item pattern) is still open — see below.

**Rare magic-teaching NPCs: explicitly deferred to a later MVP.** Not
being designed further this pass — noted as a real future item, not
dropped.

## NPC training for crafting/weapon skill books (2026-08-13, Ben) — resolved

**The NPC has to be handed the book and reads it itself** — a new NPC
action, not the player reading it "at" or "for" the NPC. Same "hand the
NPC an item" shape `TryGiveTool` already established for equipping tools,
just triggering a read instead of an equip once the item's in the NPC's
hands.

**Whether an NPC can *write* books itself (using its own `NPCSkills`) is
explicitly deferred to a later MVP** — not decided this pass, same
deferral as the rare magic-teaching NPC archetype above.

## Everything currently open

Nothing design-level remains open in this pass. What's left are real
future-MVP items, not open forks in *this* design:

- **Rare magic-teaching NPCs** — full design (spawn rules, cost/
  condition, lineage-specific or general) deferred to a later MVP.
- **NPCs writing their own books** — deferred to a later MVP.
- Small number-tuning only, not a design question: whether
  `SpectacularFailureDamage` for writing (2–10, resolved above) needs
  further balancing once actually played, same as any other tuned number
  in this project.

## Build order (2026-08-13)

Audited directly against the current code before writing this — three
real gaps found that aren't design questions, just things that don't
exist yet and need building first or scoped around:

- `PlayerCrafting`'s `CraftOutcome` enum and `RollOutcome`/`RiskMarginCap`
  are all `private`, nested inside that one class. Writing needs the
  identical formula (that's the whole point — "no new formula needed"),
  so this needs extracting to a small shared static utility both
  `PlayerCrafting` and the new writing code can call, not copy-pasted.
- **NPCs have no crafting/bench-work system at all yet** — confirmed via
  grep, and matches `NPC_JOB_GENERALIZATION_PLANNING.md`'s own "bench-
  crafting explicitly deferred" note. NPC book-training (Phase 4 below)
  would grant a recipe exception with nothing yet able to use it — inert
  until NPC bench-crafting itself ships. Worth sequencing Phase 4 after
  that piece, not before, or there's nothing to test it against.
- **No loot-chest/loot-table system exists in this codebase at all** —
  confirmed via grep. "Random world drops" (one of the three resolved
  book sources) has nothing to hang off yet; scoped into Phase 5 below as
  its own small stopgap rather than assumed to already have a home.

### Phase 0 — Prerequisites

1. ✅ **`PlayerMagic`: `StartingLineage` → `knownLineages`.** Done —
   `StartingLineage` kept as-is (the originally free lineage, still read
   by `MagicScreen`), new `knownLineages` `HashSet<SkillDefinition>`
   added alongside it, seeded with `StartingLineage` in `Awake`.
   `IsLineageKnown` now checks the set. New `LearnLineage(lineage,
   bonusLevel)` — the magic book read action's entry point (Phase 3).
2. ✅ **Extracted `CraftOutcome`/`RollOutcome`/`RiskMarginCap`** into a new
   `CraftOutcomeRoll.cs` (public `CraftOutcome` enum + `CraftOutcomeRoll
   .Roll(margin)` + `RiskMarginCap`). `PlayerCrafting.ResolveOutcome`
   calls it exactly as it called the old private method — no behavior
   change, confirmed via compile + the existing crafting flow being
   untouched otherwise.
3. **Material cost for writing: resolved — Paper + Ink.** ✅ Both now
   exist as real `ItemDefinition`s (Phase 1 item 5). Obtaining them
   (a gather node or cheap recipe) is still open — not blocking, since
   `AdminSpawnScreen` already makes any `ItemDefinition` spawnable for
   testing the moment it exists.

### Phase 1 — Data model

4. ✅ **`SkillBook : IEquippable`** (new `SkillBook.cs`) — carries
   `TargetRecipe`/`TargetWish`/`BonusLevel` as per-instance state,
   `CanEquipToSlot` always false (held/read only, never worn),
   `SetTargetRecipe`/`SetTargetWish` written by `PlayerWriting` (Phase 2,
   not built yet). Mirrors `Canteen`'s `Stash`/`SetCarried`/pickup shape
   exactly.
5. ✅ **Paper + Ink `ItemDefinition`s** — `Assets/Data/Paper.asset`
   (maxStack 20, weight 0.05) and `Assets/Data/Ink.asset` (maxStack 20,
   weight 0.1), each with a real pickup prefab (`PaperPickup.prefab`/
   `InkPickup.prefab`, generic `Pickup` component) and a baked icon.
   **Still open**: an actual gather/craft source beyond Admin Spawn.
6. ✅ **One shared `ItemDefinition`**: `Assets/Data/SkillBookItem.asset`
   ("Skill Book," maxStack 1, weight 1.2), backed by `SkillBookPickup
   .prefab` (the Book model + `SkillBook` component) — used for both
   crafting/weapon and magic targets, per the "one shared, simpler"
   option. The Scroll model stays unused for now, reserved for the
   separate future Scroll item (random-roll on read), not a
   deliberately-written magic book.
7. **Models: a Book, a Scroll, Paper, and Ink.** ✅ **Generated and
   verified (2026-08-13)** — Ben's call: Blender (`Tools/Blender/
   GenerateSkillBookModels.py`, a new permanent generator script, not
   throwaway) instead of the Tripo3D pipeline other models in this
   project have used. Book cover reads "Skill Book" (embossed text,
   dark red cover); Scroll has a tied ribbon + "Magic" tag; Paper is a
   plain white sheet; Ink is a dark bottle with a lighter cap (a real
   glass-transparency-plus-visible-liquid look needed more EEVEE
   transmission/refraction setup than a placeholder prop warranted, so
   the bottle body itself is ink-dark instead). All four imported into
   `Assets/Models/` and measured via a batch-mode script per `CLAUDE.md`'s
   mandatory new-model checklist — final sizes match intent exactly
   (Book 0.22×0.16×0.037m, Scroll 0.26m long, Paper 0.297×0.21×0.003m,
   Ink 0.05m diameter × 0.078m tall) and all four are correctly grounded
   (`min.y = 0`). Two real bugs hit and fixed along the way: `Material.
   diffuse_color` alone doesn't drive the actual EEVEE render (needs
   wiring into the Principled BSDF's Base Color explicitly, or
   everything renders flat gray), and `primitive_cube_add(size=1)`
   already produces a full 1-unit cube, so Book/Paper's first pass
   (scaled by `length/2`) came out exactly half their intended size —
   caught by the same batch-mode measurement, not assumed correct.
   A Scroll's own gameplay (the random lineage+wish roll on read) is
   **not** part of this build's scope — only its `worldPickupPrefab`
   exists now; Scroll mechanics stay a future item. Not yet wired into
   `ItemDefinition`s or prefabs at the time this was written — ✅ now
   wired, see items 5/6 above (Book → `SkillBookItem`, Paper → `Paper`,
   Ink → `Ink`). Scroll stays unused/unwired, reserved for the future.
8. ✅ **Exception-tracking sets**: `PlayerCrafting` gained
   `bookGrantedRecipes` + `GrantRecipe(recipe)`, `PlayerMagic` gained
   `bookGrantedWishes` + `GrantWish(wish)`. `HasRequiredSkill`/
   `CanAttempt` both gained the `|| grantedSet.Contains(...)` bypass —
   the skill-level math itself is untouched.

### Phase 2 — Writing

9. ✅ **`PlayerWriting` + `WritingScreen` built.** New "Writing" tab in
   `PlayerMenuScreen` (Tab key, alongside Crafting/Magic/Build — no new
   keybinding, rides the existing menu). Lists every recipe the author
   currently passes `HasRequiredSkill` for and every wish in a known
   lineage (`PlayerMagic.KnownWishes`), each with a Write button.
   Consumes 1 Paper + 1 Ink per attempt regardless of outcome. Margin =
   author's Intelligence level − `CraftTierScale.SkillRequirement` of
   the subject's tier, rolled via the shared `CraftOutcomeRoll`.
   `BadFailure`/`SpectacularFailure` → no book (`SpectacularFailure` also
   → 2–10 damage); anything else → a real `SkillBook` instance spawned
   and added to the author's inventory, target baked in via
   `SetTargetRecipe`/`SetTargetWish`, with a 1–10 bonus level rolled only
   for a `BrilliantSuccess` wish book. Author's Intelligence XP scales by
   outcome tier (0.5/1.5/3 for BarelyFail/Success/BrilliantSuccess, 0 on
   either failure — first-pass numbers, same "tune by playtesting" status
   as every other balance value in this project).

### Phase 3 — Reading

10. ✅ **`PlayerReading` built.** Not quite the originally-sketched
    `PlayerEating.TryEatFrom`-style dispatch — that shape only works for
    plain `ItemDefinition`-only consumables looked up by item reference.
    A `SkillBook` is equipment-backed (a real physical instance with its
    own `TargetRecipe`/`TargetWish`/`BonusLevel`), so it goes through
    `InventoryScreen`'s `pendingActionEquipment` branch instead — the
    same shape `Canteen`'s Drink/Fill buttons already established there.
    A `SkillBook` skips the generic Equip/Unequip block entirely (it's
    never worn) and shows only Read + Drop.
    - **Crafting/weapon target**: `PlayerCrafting.GrantRecipe(recipe)`.
    - **Magic target**: `PlayerMagic.LearnLineage(wish.lineage,
      bonusLevel)` first (a no-op if already known), then
      `PlayerMagic.GrantWish(wish)` — `CanAttempt` still separately
      requires `IsLineageKnown` even with the wish exception granted, so
      the order matters.
    - Either way: reader's Intelligence gets a small XP tick (0.25 —
      smaller than any of writing's own gains, since reading is the
      passive half of the loop), then the book is permanently destroyed
      (removed from its `Inventory` slot + `Destroy(gameObject)` — a
      Scroll-style one-time consumable, not stashed/returned).

### Phase 4 — NPC training

**Superseded 2026-08-16, built the same day — see `NPC_TRAINING_PLANNING.md`
for the real design (v0.3.102-dev, see `CHANGELOG.md`).** The sketch below
(item 11) was written 2026-08-13, before a full conversational design pass
with Ben. It's kept here for history but is **no longer accurate**: the
real design is a Desk/Bookshelf ritual (2 real minutes, not instant), reads
from both a Bookshelf *and* the player's inventory (not just a direct
hand-over), and **includes magic books** (banked inertly on the NPC for a
future NPC-magic system, explicitly not excluded the way this stub
originally said). NPC bench-crafting (`NPC_JOB_GENERALIZATION_PLANNING.md`
section 7) shipped first, same session (v0.3.101-dev), giving the
crafting/weapon-book half something real to attach to.

<details>
<summary>Original sketch (2026-08-13), kept for history, superseded above</summary>

11. New "give NPC a book" interaction, mirroring `TryGiveTool`'s hand-
    over-an-item shape — the NPC reads it itself (its own small granted-
    recipes set, parallel to the player's). Crafting/weapon books only —
    magic is fully excluded per the design. Sequence this phase *after*
    NPC bench-crafting exists (a separate, already-deferred piece) —
    before that, this grant has nothing to attach to.

</details>

### Phase 5 — Sourcing

12. ✅ **Player-to-player trade** confirmed needing no new code — works via
    the existing pickup/drop/`StorageBox` flow now that `SkillBook` is a
    real item.
13. ✅ **"Random world drops" placed, via a revised stopgap.** A
    `StorageBox` turned out unable to be pre-filled at scene-authoring
    time at all (its `Inventory` is created fresh in `Awake`, never
    serialized) — the actually-simplest stopgap is a bare `SkillBook`
    sitting in the world, exactly like any other `Pickup`, no box needed.
    Two placed in `TestScene.unity`: one targeting `MasterworkKnifeRecipe`
    (a real crafting/weapon book, well beyond starting reach), one
    targeting `SparkWish` (a real magic book). Revisit with a proper
    loot-table system later if/when one gets built for other reasons.
    Also gave Paper and Ink a real source, closing the gap flagged since
    Phase 0/1: `PaperRecipe` (1 Plank → 4 Paper) and `InkRecipe` (2 Berry
    → 1 Ink), both Crude/no-skill-gate gadget recipes, registered on
    `PlayerCrafting.recipes`.
14. **Rare magic-teaching NPCs** — skipped, explicitly deferred.

**Real bug caught and fixed while placing the found books**: `SkillBook.
TargetRecipe`/`TargetWish`/`BonusLevel` were plain C# auto-properties, not
`[SerializeField]` — invisible to Unity's scene serializer. A book
written and read within one Play session was never affected (it lives
entirely in memory the whole time), but a book placed directly in a
*saved scene* at edit-time silently lost its target on reload — caught
directly by verifying the saved YAML rather than trusting "the script
logged success." Converted to real `[SerializeField]` backing fields
with read-only accessor properties. A second, related trap on the way to
the fix: even after that change, a plain C# field assignment on a
prefab-instance's component still didn't register as a serializable
override on its own — needed an explicit
`PrefabUtility.RecordPrefabInstancePropertyModifications(book)` call
after `SetTargetRecipe`/`SetTargetWish` for the change to actually make
it into the saved scene file. Both are real, generalizable gotchas for
any future batch-mode script that sets a prefab-instance's fields via
plain C# rather than through `SerializedObject`.

### Phase 6 — Verification

15. ✅ **Batch-mode compile + YAML grep, comprehensive final pass done.**
    0 CS errors across every round of this build (10+ separate batch-mode
    runs over Phases 0–5). Final sweep directly confirmed: all 3
    `ItemDefinition`s have their `worldPickupPrefab`/`icon` wired; both
    new recipes have the right ingredients/output/count; all 3 pickup
    prefabs carry the right script; `PlayerWriting`/`PlayerReading`/
    `WritingScreen` are all present exactly once on the Player object in
    `TestScene.unity`; both found books' `targetRecipe`/`targetWish`
    modifications are present in the saved scene YAML.
16. ✅ **New `TEST_FEATURE_PLAN.md` section 31 written** — covers the
    basic crafting and magic write→read loops, the one-recipe-only
    scoping check, both pre-placed found books (no writing required to
    test reading), `SpectacularFailure` damage, `BrilliantSuccess`'s
    lineage bonus range, Intelligence actually training, and a UI
    regression check for the Writing tab's empty/warning states. NPC
    training explicitly called out as not testable yet (blocked on
    bench-crafting). **Not yet walked through live** — this build has
    been verified structurally (compile + YAML) at every step but has
    zero real Play-mode confirmation so far, same status save/load
    carried until Ben's live round-trip test confirmed it for real.

## Cross-references against MVP2_PLANNING.md (2026-08-13)

- **Advances item 1 (Expand Stats) for free.** Phases 2–3's Intelligence
  training (reading/writing as a trigger) is the first real progress on
  item 1 beyond Strength/`PlayerEncumbrance` — building skill books gives
  Intelligence its own concrete trigger+effect pair, which item 1 had
  only ever raised as a candidate idea.
- **Phase 4 is blocked on item 2 (Expand NPC hiring).** NPC training has
  nothing to attach to until NPC bench-crafting exists — that's item 2's
  own already-deferred sub-scope
  (`NPC_JOB_GENERALIZATION_PLANNING.md` section 7), not something this
  build finishes on its own.
- **Creates follow-up work for item 6 (Save/load persistence).** New
  player state this build introduces — `knownLineages`,
  `bookGrantedRecipes`, `bookGrantedWishes`, and `SkillBook` instances
  sitting in inventory — isn't covered by the v1 save system yet.
  Because `SkillBook` is deliberately an `IEquippable` (Phase 1, item 4
  above), it composes almost for free with the recursive
  `EquipmentSaveUtility`/`InventorySaveUtility` capture already built and
  live-tested — needs one more type-specific branch (mirroring the
  existing `Canteen` case) for `targetRecipe`/`targetWish`/`bonusLevel`,
  plus three new fields on `SaveManager.CapturePlayer`/`RestorePlayer`.
  Small, but real — flagged in `SAVE_LOAD_PLANNING.md` too so it isn't
  discovered later as a "why didn't my skill books survive a reload" bug.
- **No interaction** with items 3, 4, 5, 8, 9, or 10.
