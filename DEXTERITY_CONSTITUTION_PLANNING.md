# Dexterity & Constitution Planning

Follow-up to `BUGS_AND_ENHANCEMENTS.md`'s "Dexterity / Constitution /
Intelligence — display-only, no growth hooks or mechanical effects yet"
entry (2026-08-10). Intelligence shipped since then (`SKILL_BOOKS_PLANNING.md`,
v0.3.53-dev) — this doc works out the last two, plus one small refinement to
Intelligence discovered while designing them. Designed and built same
session (2026-08-14, v0.3.55-dev, see `CHANGELOG.md`). Verified via
batch-mode compile + YAML grep only so far — **no live Play-mode
confirmation yet**, see `TEST_FEATURE_PLAN.md` section 33.

## Framework: two established growth patterns, reused

Two real mechanisms already exist in the code, and every new hook below is
built from one or the other (or, for Constitution, both):

- **Continuous, load/state-driven** (Strength's shape, `PlayerEncumbrance.cs`)
  — ticks every frame based on a live ratio or boolean state, banked in a
  local accumulator (small per-frame amounts round-trip through float32
  precision loss otherwise — see `CLAUDE.md`'s Strength-adjacent gotcha
  neighborhood), calibrated to a real-world pacing target rather than a
  felt-right raw number.
- **Discrete, event-driven** (Intelligence's shape, `PlayerWriting.cs`/
  `PlayerReading.cs`) — a flat `GainExperience` call fired once at the
  moment a specific action resolves. No per-frame tracking.

Both existing stats also earned a custom Player-tab tile with a sub-line
showing their mechanical effect (`DrawStrengthTile`/`DrawIntelligenceTile` in
`PlayerMenuScreen.cs`, replacing the generic `DrawStatTile` Dexterity/
Constitution still use today) — that's part of "done," not just the growth
hook.

## Intelligence refinement (small addition to the already-shipped system)

Decided while designing the other two, not part of the original ship:
**Intelligence grants a small global XP multiplier on every *other* skill's
gains** — smarter characters learn faster across the board.

- Formula: `xpGained *= 1 + intLevel/2000`, where `intLevel` is the raw
  0-100 skill level (not the 0.25-10 displayed value). Caps at **+5%** at
  Intelligence 100.
- Does **not** apply to Intelligence's own gains — no self-reinforcing loop,
  no double-counting.
- Natural implementation point: inside `PlayerSkills.GainExperience` itself,
  since every stat funnels through there — needs a parameter or an internal
  check to exclude Intelligence's own calls from the multiplier.
- An earlier, much bigger version of this idea already existed in
  `BUGS_AND_ENHANCEMENTS.md`'s Intelligence sketch (`intLevel/200`, capping
  at +50%) — **superseded**, that number was explicitly "not vetted," and
  +50% doesn't match "very small" (Ben, 2026-08-14).

## Constitution

### Output: Max Health and Max Stamina, both growable

Both vitals are hardcoded `Mathf.Min(100f, ...)` throughout `PlayerVitals.cs`
today — no growable cap exists for either. The pattern to copy is
`GrowMaxWill` (Will's ceiling already isn't fixed — it grows via a direct
`maxWill += amount` call), applied to Health and Stamina instead of Will.

A pure power-law formula (`Max = C × Constitution^n`, the same shape as
`PlayerEncumbrance.Capacity`) turned out **not** to work here: solving for an
exponent that passes through both a sensible low anchor (Constitution 2.00,
today's baseline) and a sensible high anchor (Constitution 10.00, the cap)
gives `n ≈ 0.43` — a *concave* curve, the opposite of Strength's front-loaded
`n = 1.5`. A front-loaded curve mathematically requires the output to grow by
*more* than the input ratio, which a modest 2x-at-cap target can't satisfy.
Anchoring only the high end and using Strength's real `n = 1.5` unanchored
(`PlayerEncumbrance`'s own approach) crashes the low-end value to ~18, a
stealth nerf to every fresh character.

**Resolved with an additive model instead**: `Max = 100 + k ×
(Constitution - 2)^1.5`. This guarantees the baseline stays exactly 100 at
starting Constitution (no regression), while the *bonus* above that baseline
follows a genuinely front-loaded curve (barely any gain in the first couple
points, most of the growth comes late), same spirit as Strength without the
math conflict.

- **Max Health**: `100 + 4.42 × (Constitution - 2)^1.5` — reaches 200 at
  the cap. (`k` solved from `100 = k × 8^1.5`.)
- **Max Stamina**: `100 + 8.84 × (Constitution - 2)^1.5` — reaches 300 at
  the cap. (`k` solved from `200 = k × 8^1.5`.)
- Both use Constitution's `.25-10` displayed value (`PlayerSkills
  .GetAttributeValue`), same convention as Strength's capacity formula.
- Max Stamina deliberately stays with Constitution, not Dexterity — decided
  directly (Ben, 2026-08-14): "stamina should be constitution." Health and
  Stamina read as the same underlying trait (endurance/toughness), same
  reasoning most RPG conventions already use.

### Input: exercise, not adversity

Original `BUGS_AND_ENHANCEMENTS.md` sketch had Constitution training from
"surviving damage, repeatedly hitting 0 Stamina, environmental exposure" —
an adversity-based framing. **Explicitly replaced** (Ben, 2026-08-14): use
the exercise angle instead — "if you exercise, it is supposed to help your
health." Constitution grows from doing physical activity, not from getting
hurt.

- **Sprinting** — continuous (Strength's shape). Live signal already exists:
  `vitals.IsSprinting`, set every frame in `FirstPersonController.cs`.
  Pacing target: **~4 real days** for +0.25 at Constitution 2.00 (vs.
  Strength's 2 days) — deliberately slower, Ben's call ("cardio conditioning
  should take noticeably longer than load-bearing strength gains"). Same
  ODE-solved-rate approach as `PlayerEncumbrance.SolveMostGainRate()`.
- **Soccer kicks** — discrete (Intelligence's shape), and secret — not shown
  anywhere in UI/tooltips, a game-within-a-game easter egg (Ben's framing:
  "that could introduce a game within the game"). Hooks `SoccerBall
  .TryKick()`, which already exists and already varies by sprint state
  (normal kicks 3-7m, sprint-kicks 5-12m/higher angle). Grant scales with
  kick distance — a hard sprint-kick gives meaningfully more than a light
  tap, so it can't be cheesed by tapping the ball in place.
- **Explicitly deferred, needs new systems first**: swimming, biking, horse
  riding — none of these exist in the codebase at all today (no water
  traversal, no mount, no vehicle). Not a Constitution-hook gap, three
  entire unbuilt gameplay systems. Add as Constitution inputs once (if) they
  ship, same shape as Dexterity being blocked on ranged/melee combat below.

## Dexterity

### Output: movement speed

Confirmed directly (Ben, 2026-08-14): "dexterity will drive speed" — not the
movement-under-load efficiency the original backlog sketch floated, which
was already explicitly closed on the Encumbrance side back on 2026-08-10
("the relative amounts apply nicely, no change to that").

Movement speed in `FirstPersonController.cs` is already a multiplier chain:
```
speed = baseSpeed * staminaMultiplier * encumbranceMultiplier
```
(`baseSpeed` itself derived from `moveSpeed`/`sprintSpeed` and the current
`MovementStance`). Dexterity slots in as one more multiplicative term
without touching any existing constant:
```
speed = baseSpeed * dexterityMultiplier * staminaMultiplier * encumbranceMultiplier
```

- `dexterityMultiplier` reaches **+30%** at Dexterity 10.00 (Ben's pick — "a
  meaningful but not game-breaking boost"). **Curve shape: linear**
  (2026-08-14) — speed doesn't have the same "actively managed resource"
  feel Encumbrance/Health do, so `dexterityMultiplier = 1 + 0.30 *
  (dexValue - 0.25) / (10 - 0.25)` (0% at the display floor, +30% at the
  cap), not a front-loaded power curve like Strength/Constitution.
- Cross-stat interaction worth remembering, not a conflict: Dexterity
  governs how fast you *can* sprint; Constitution grows *because* you
  sprint. Same action, opposite sides of two different stats.

### Input: sprinting, sneaking, jumping, and hands-on crafting

- **Sprinting** — continuous, **shared with Constitution** (Ben's call,
  2026-08-14: "sprinting trains both" — same real action, two payoffs,
  matches how cardio training works in life).
- **Sneaking** — continuous, while moving in Kneeling/Crawling/Prone stance
  (`MovementStance`, already exists). Pacing target: **~3 real days** for
  +0.25 at Dexterity 2.00 — faster than sprinting's shared 4-day target,
  since sneaking is Dexterity-exclusive and has to pull its own weight
  alone (first-pass number, tunable like every other rate in this doc).
- **Jumping** — discrete, per jump (spacebar in `FirstPersonController.cs`,
  already exists — currently only costs Stamina, no XP hook yet). Flat
  **0.1** (raw level) per jump — no extra anti-spam cooldown needed, the
  existing 10-Stamina cost per jump already self-limits it.
- **Completing a `CraftingRecipe`** — discrete, **small flat amount
  regardless of what was crafted** (Ben, 2026-08-14) — not scaled by the
  recipe's own `skillGain` or output tier. Flat **0.1** (raw level), same
  magnitude as the jump grant.

### The manual-vs-machine distinction, and why it needs no new field

Ben's framing: "manual crafting skills would have an input to dexterity
improvement — sewing pouches could give you a dex boost, but using a sewing
machine wouldn't," then confirmed with a real in-game example: "you could
make a nail on the anvil — bang metal with a hammer. or you could put metal
in the forge and crank it out that way."

That distinction **already exists structurally in the data model** — no new
per-recipe flag needed:

- `CraftingRecipe` (Anvil-hammered Nail, a hand-sewn Pouch) is inherently
  the "player actively performed a skilled action" type in this codebase:
  player-triggered, skill-gated, subject to the chance-of-creation roll.
- `SmeltableItem` (the Furnace's automated smelting queue, v0.3.31-dev) is a
  deliberately *separate* type — unattended, deterministic, no skill
  involved, ticks every frame even with the player not present (that's the
  entire point of the Furnace automation work).
- `CookableItem` (Campfire cooking) is the same story — its own type,
  separate from `CraftingRecipe`, for the same reason.

**Rule**: completing any `CraftingRecipe` craft grants Dexterity XP;
`SmeltableItem`/`CookableItem` outputs never do. The existing type boundary
in the code already *is* the manual/machine boundary — a future Sewing
Machine would naturally live as its own third automated type (mirroring
`SmeltableItem`), keeping the rule intact without touching it.

### Explicitly deferred, needs Combat first

"Ranged combat" was in the original backlog sketch as a Dexterity input.
Not viable yet — only Bare-handed melee exists today (per
`BUGS_AND_ENHANCEMENTS.md`'s "Only Bare-handed exists of the five
weapon-usage skills" entry); no Spear/Bow/Gun actually deals damage. Add
once Combat ships real weapon types.

## Resolved 2026-08-14 — ready to build

Every open question above is now settled:

- **Dexterity's speed-multiplier curve is linear**, not front-loaded — see
  the Output section above.
- **All four Dexterity input numbers now have real first-pass values** —
  sneak (~3 real days for +0.25), jump (flat 0.1), per-craft (flat 0.1) —
  same "shipped as tunable" status as every other rate in this doc.
- **`PlayerSkills.GainExperience` excludes Intelligence's own gains via an
  internal check**, not a call-site parameter — compares the skill argument
  against its own cached Intelligence reference and skips the multiplier on
  a match, so none of `GainExperience`'s dozens of existing call sites need
  to change.

Still needed, but implementation work rather than open design questions:
new `DrawDexterityTile`/`DrawConstitutionTile` methods in
`PlayerMenuScreen.cs` (matching `DrawStrengthTile`/`DrawIntelligenceTile`'s
custom sub-line pattern), replacing the generic `DrawStatTile` both
currently use.
