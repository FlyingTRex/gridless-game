# Village Flag Planning

Planning doc for a craftable beacon that draws new hireable NPCs (and,
later, a Traveling Trader) toward the player's settlement, with Fame and
the Flag's own crafted quality both speeding it up (2026-08-16). Designed
conversationally with Ben, decision-locked, not yet built.

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

## 5. Explicitly out of scope for this pass

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

Planning only, not yet built.
