# Village Flag Planning

**Status: fully built.** Sections 2-4 (the Flag itself + spawn loop)
shipped v0.3.103-dev; section 6 (City Statue gate) shipped v0.3.104-dev
(both 2026-08-16) — see `CHANGELOG.md`'s entries for the built shape.
Left in present/future tense below rather than rewritten
past-tense, matching the convention `NPC_JOB_GENERALIZATION_PLANNING.md`
uses for its own built sections. Two of this doc's own flagged-open
numbers were resolved at build time: `baseStickAround` used the doc's own
proposed 10-minute anchor (still not Ben-confirmed as final), and "what
happens to an NPC that wanders off unhired" resolved to despawn (the
simpler of the two undecided options, needing no new system).

Planning doc for a craftable beacon that draws new hireable NPCs (and,
later, a Traveling Trader) toward the player's settlement, with Fame and
the Flag's own crafted quality both speeding it up (2026-08-16). Designed
conversationally with Ben, decision-locked.

## 1. Why this matters — it answers two separate open questions at once

1. **This conversation's own open question**: what should higher Fame
   unlock for finding/hiring more NPCs? Today NPCs "just are there" —
   pre-placed instances in the world, no dynamic population system at
   all.
2. **A long-blocked `FAME_PLANNING.md` question**: the Traveling
   Trader's 5-band visit-frequency table (Infamous 0.5x through
   Renowned 1.5x) has been fully designed and Ben-confirmed since
   2026-08-14, but explicitly flagged as unusable — "however the
   Traveling Trader's spawn/visit interval ends up being built (**not
   designed yet**)." The Village Flag *is* that missing mechanism.

The settler-NPC half is buildable now. The Trader half reuses the exact
same spawn-and-seek-the-flag mechanism later, once the wider commerce
system it depends on (`BUGS_AND_ENHANCEMENTS.md`'s "Fame: business-reach
input... blocked on an entire commerce system that doesn't exist") gets
built — not attempted together with this pass.

## 2. The Flag itself — a new Build piece, Sewing-trained

**Recipes built (2026-08-16).** 5 `BuildPiece` assets
(`Assets/Data/{Tier}VillageFlagPiece.asset`), each 1 tier-matched Trimmed
Stick + 1 Cloth, `trainedSkill = Sewing`, `unlockTier` set to the matching
`CraftTier` (gates visibility the same way `CraftTierScale.SkillRequirement`
gates every other tiered recipe — this *is* still a skill-level gate, just
not a random `CraftOutcomeRoll`, matching what Stone Arrow's own recipes
actually do). All 5 registered on the Player's `PlayerBuilding.allPieces`
in `TestScene.unity`, so they show up in the Build tab now. Each has its
own placeholder prefab (a primitive pole + banner, banner tinted via
`CraftTierColors` and both pole/banner sized up per tier) so the ladder is
placeable and testable today — **not the real Blender pass** this doc
already flagged as separate future work; swap `BuildPiece.prefab` on each
asset once real models exist, no other change needed. Verified via
batch-mode compile + direct scene/asset YAML grep only — not yet
live-tested in Play mode.


- **Recipe**: 1 Stick + 1 Cloth, trains Sewing — Sewing's first real
  crafted-item purpose beyond Rope/Cloth themselves (both of which just
  feed *into* other things; the Flag is an actual standalone structure).
- **5-tier ladder, deterministic by ingredient quality — same shape
  Stone Arrow already established** (`PlayerRangedCombat`/Hunting
  Expansion design, 2026-08-15): which **Stick tier** feeds the recipe
  determines the Flag's own tier, no skill roll involved (Crude Stick →
  Crude Flag, up through Masterwork Stick → Masterwork Flag). Higher
  Flag tiers shorten the spawn interval further, on top of whatever Fame
  is already doing (section 4).
- **Higher tiers also look bigger** (Ben, 2026-08-16) — a visual tell
  that reads at a glance, not just a stat difference the player has to
  check a screen to see. Same "5 real, differently-shaped/sized stages"
  precedent `CropDefinition.growthStagePrefabs`/`ChoppableTree`'s growth
  states already use in this project, rather than one mesh non-uniformly
  scaled up — a genuinely bigger/more elaborate flag per tier (taller
  pole, larger banner, maybe more ornamentation at Fine/Masterwork), not
  a naive `transform.localScale` multiply. Exact per-tier size/model
  detail not designed here — a Blender modeling pass at build time, same
  process every other tiered item in this project already went through.
- Player-placed, same free-placement Build flow every other structure
  (Campfire, Garden Plot, StorageBox) already uses.
- **Nameable, built v0.3.105-dev** (Ben's follow-up ask, same day) — a
  placed Flag can be renamed exactly like a Storage Box (`VillageFlag`
  implements `IRenameable`, so `PlayerRenaming`'s existing right-click
  flow covers it for free, no new interaction code). The chosen name
  shows as a labeled marker on the Player Map (`MapScreen`), shown
  unconditionally rather than gated by fog reveal.

## 3. Spawn loop

Every `spawnIntervalMinutes` (real time, section 4 for the formula), if
at least one Village Flag exists in the world: spawn a new hireable NPC
(same `NPCFactoryWorker`-shaped prefab existing pre-placed hires already
use) somewhere out in the world, then have it walk toward the nearest
placed Flag — reusing `NPCWander`'s move/ground-sample/face plumbing
aimed at a fixed destination instead of a random wander point (the same
reuse `NPCFlee.cs` already did for its own "move away from the player"
behavior, just aimed at a beacon instead of away from a threat).

Once the NPC reaches the Flag, it behaves exactly like any other
pre-placed hireable NPC standing in the world — same `NPCHiring`
interact-to-hire flow, nothing new needed there. If not hired within
**stickAroundMinutes** (section 4), it wanders off and is lost — the
window shrinks/grows with the same inputs that shrink/grow the spawn
interval, just inverted (section 4).

## 4. The interval formula

**Base interval: 30 real minutes** (Ben's number, confirmed 2026-08-16),
reduced by two independent multipliers:

- **Fame band** — reuses `FAME_PLANNING.md`'s existing 5-band table
  as-is, no new numbers needed:

  | Band | Fame range | Frequency multiplier |
  |---|---|---|
  | Infamous | ≤ -500 | 0.5x |
  | Notorious | -499 to -100 | 0.75x |
  | Neutral | -99 to 99 | 1.0x |
  | Known | 100 to 499 | 1.25x |
  | Renowned | ≥ 500 | 1.5x |

- **Flag tier** — a **new, dedicated scale**, not a reuse of an existing
  one. Per CLAUDE.md's own tier-scaling gotcha ("a ratio tuned for one
  quantity doesn't transfer to another"), Arrow/Bow's damage-bonus
  tables or `CraftTierScale`'s capacity/price modifiers aren't the right
  numbers for spawn-timing — needs its own small table, not designed in
  numeric detail here (first pass, tune-by-playtesting like everything
  else: something like Crude 1.0x down to Masterwork ~0.6x, mirroring
  the *shape* of `CraftTierScale.WeightModifier`'s "better tier is a
  believable amount better, not a 25x swing" restraint — exact numbers
  TBD at build time).

```
currentInterval = baseInterval(30 min) × fameBandMultiplier⁻¹ × flagTierMultiplier
```

(Multipliers above are framed as "frequency" — i.e., higher = more
often = shorter interval — so the interval itself divides by them, not
multiplies. Restated plainly: Renowned Fame's 1.5x *frequency* means
the interval is `30 / 1.5 = 20` minutes, not 45.)

**Stick-around time — the inverse, proposed formula (Ben's framing:
"the npc wanders away in the inverse of the spawn time... as fame
increases, npc shows up sooner and sticks around longer"):**

```
stickAroundMinutes = (baseInterval × baseStickAround) / currentInterval
```

At the 30-minute baseline, stick-around time equals `baseStickAround`
(proposed starting anchor: **10 minutes**, not yet Ben-confirmed —
flagged as the one remaining open number). If Fame + Flag tier together
halve the interval (15 min), stick-around time doubles (20 min); if they
cut it to a third (10 min), stick-around triples (30 min). Clean
proportional inverse, one anchor number to tune later once live-tested.

## 6. City Statue — the Village → City progression gate

**Decided (Ben, 2026-08-16): locked in as a gate mechanism only** — this
section defines the unlock condition and what it gates, not the
Statue's own recipe or any specific City-tier building beyond
illustrative examples.

- **Unlock condition**: a **Masterwork-tier Village Flag** placed, and
  **at least 10 currently-hired NPCs** — both checked at the moment the
  player attempts to build the Statue (a live precondition, not a
  lifetime/cumulative hire counter — if you've fired people back below
  10, the gate simply isn't satisfied yet, same as any other
  not-currently-satisfiable recipe in this project).
- **City Statue** — a new `BuildPiece` (built v0.3.104-dev). **Actual
  built behavior differs slightly from "hidden until satisfiable"**: it
  always shows in the Build tab like every other piece, locked with a
  reason label (`PlayerBuilding.LockReason`) when conditions aren't met
  — matches `BuildScreen`'s existing always-shown-with-a-lock convention
  for skill-gated pieces rather than introducing true hiding just for
  this one piece. Material cost: 20 Rock + 10 Iron Ingot + 5 Gold Ingot,
  a real, substantial civic cost matching its weight as a milestone (a
  build-time decision, not designed in more detail than that here).
- **Permanent once built** (Ben, explicit) — City status doesn't revert
  if the player later fires NPCs below 10 or the Flag is somehow lost.
  The Statue standing in the world *is* the proof; no separate
  "maintain city status" upkeep exists.
- **Grants Fame on completion** — proposed **+50**, a real milestone-
  sized jump, deliberately well above any repeatable action (Hire +1,
  Training's small per-session gain, even a Masterwork skill-tier
  mastery's +5) since reaching this requires 10 hires *and* a Masterwork
  Flag *and* the Statue's own real cost. Not yet Ben-confirmed as a
  final number.
- **The gate itself, reusable**: a new `requiresCityStatus` bool on
  `BuildPiece`, mirroring `CraftingRecipe.requiresAnvilSurface`/
  `requiresFurnace`'s exact shape — a flag any future advanced structure
  checks rather than something bespoke per building. **Research
  Facility and Spaceport are illustrative examples of what this could
  eventually gate, not designed here.** Genuinely promising real
  convergence point, though: `docs/design-brief.md`'s existing
  "Endgame: Leaving the Planet" section already specs an **Orbital
  Engineering** route (Master Smith + Master Engineer + Master Builder
  Keystones, "build a launch vehicle from first principles") converging
  on Escape Velocity — a City-tier Spaceport reads like a natural
  earlier stepping stone toward that already-designed endgame arc, not
  a disconnected new idea. Worth keeping in mind when City-tier
  buildings actually get designed, not committed to yet.

## 7. Explicitly out of scope for this pass

- **Any specific City-tier building** gated behind `requiresCityStatus`
  (Research Facility, Spaceport, or otherwise) — the gate mechanism is
  built and reusable, nothing yet actually uses it. (The Statue's own
  recipe/materials are no longer out of scope — see section 6, decided
  at build time: 20 Rock + 10 Iron Ingot + 5 Gold Ingot.)
- **The +50 Fame number** — proposed and shipped as the working number,
  not yet Ben-confirmed as final.
- **The Traveling Trader itself** — this doc designs the reusable
  spawn-and-seek mechanism; the Trader is a second future consumer of
  it, blocked on the same "no commerce system exists at all" prerequisite
  `BUGS_AND_ENHANCEMENTS.md` already flags. Not attempted here.
- **Exact Flag-tier multiplier numbers** — shape agreed (a small,
  restrained scale, not a 25x-style swing), exact values not locked.
- **`baseStickAround`'s exact value** — 10 minutes proposed, not yet
  confirmed.
- **What happens to an NPC that wanders off unhired** — despawns
  outright, or wanders back into the general world population as an
  ordinary pre-placed-style hire elsewhere? Not decided.
- **Multiple Village Flags at once** — "nearest Flag" is the assumed
  target once more than one exists, but multi-flag balance/behavior
  (do they compete for the same spawn timer, or does each Flag run its
  own?) isn't designed.

## Cross-references

- `FAME_PLANNING.md`'s "Fame bands, and the Traveling Trader" section —
  the 5-band table this reuses directly, and the "not designed yet"
  spawn mechanism this doc is the answer to.
- `BUGS_AND_ENHANCEMENTS.md`'s "Fame: business-reach input... blocked on
  an entire commerce system" entry — still true for the Trader half.
- Hunting Expansion / Stone Arrow design (`CHANGELOG.md` v0.3.86-dev) —
  the deterministic ingredient-tier-determines-output-tier precedent the
  Flag's own 5-tier ladder copies.
- `NPCFlee.cs` — the existing "move toward/away from a fixed point"
  reuse candidate for the spawned NPC's walk-to-Flag behavior.
- `NPC_TRAINING_PLANNING.md` — designed the same session, unrelated
  mechanically but part of the same conversation's larger "give Fame and
  NPC population real teeth" push.
- `docs/design-brief.md`'s "Endgame: Leaving the Planet" section — the
  Orbital Engineering route the City Statue gate's illustrative
  Spaceport example would eventually feed toward.

Sections 2-4 built (v0.3.103-dev). Section 6 (City Statue) built
(v0.3.104-dev).
