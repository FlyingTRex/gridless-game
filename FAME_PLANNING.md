# Fame Planning

Deferred/optional-layer system from `docs/design-brief.md`'s Phase 2 list,
currently just a placeholder `Fame: 0` tile on the Player tab with no
backing system at all (added 2026-08-10 purely so the full tab layout
could be judged together). Worked out in one session (2026-08-14).
**Planning only, nothing built yet** — input side is fully designed,
output side is still open.

## Why this, why now

`docs/design-brief.md` itself flags Fame's timing as unresolved: Ben
separately floated it as a possible *later* phase, never confirmed
against the brief's existing Phase 2 placement. Decided explicitly this
session (2026-08-14): keep designing regardless of when it actually gets
built — a clear design is useful whenever it lands, and working through
it makes the timing call easier later, not harder.

## What's already decided elsewhere (constraints, not open questions)

- **Distinct from the shipped skill-tied quality mechanic.** Quality (the
  `CraftOutcome` roll) is about *your* competence; Fame is *the world
  recognizing* it. Two different things, not a reskin of one system.
- **Distinct from Factions.** Factions are a separate, behavior-driven
  trust/fear standing system (`docs/design-brief.md`'s Factions, Guilds &
  Warbands section) — not built yet either, but a different axis entirely
  from Fame.
- **Has a Phase 3 PvP dimension already sketched**, not designed further
  here: Settlement Warfare (winning/losing a city fight) moves Fame
  directly, per the design brief. Out of scope until multiplayer exists.

## Structure

**A single overall Fame number** (Ben's call, 2026-08-14) — not
per-trade, despite the design brief's original "fame in that trade line"
framing. Simpler to build and display; the world-recognizes-you payoff
applies broadly rather than needing a per-discipline lookup.

- **Type: float** (not int) — several input amounts are fractional
  (Fire's -0.5).
- **Range: -1000 to 1000**, zero as neutral. Negative reads as
  infamous/disliked, positive as renowned. All input magnitudes below are
  deliberately small relative to this range — Ben's explicit call after
  an initial pass felt too large ("we need a much smaller value scale")
  — reputation is meant to build slowly across a whole playthrough
  (hundreds of interactions), not swing dramatically from a handful of
  actions.

## Input side — fully designed

### NPC treatment

Ben's framing: "if I hire an npc, I should get some fame. If I fire one,
I should lose fame. if I kill an npc... I lose a lot more fame."

- **Hire an NPC**: **+1**. Hooks `NPCHiring.TryHire` (already exists).
- **Fire an NPC**: **-0.5**. Hooks `NPCHiring.Fire` (already exists).
  Deliberately asymmetric with Hire — Ben's call: leaving is a lighter
  mark than the positive weight of having hired at all, not a straight
  reversal.
- **Leave a hired NPC unpaid**: **-0.5 per missed pay cycle** (not a
  one-time hit — a chronically-neglected NPC keeps costing Fame). Hooks
  `NPCHiring`'s existing `IsWaitingForPayment`/`TryPay`. Same theme as
  Fire ("you're not taking care of your people"), same magnitude.
- **Kill any humanoid NPC**: **-10** (confirmed applies broadly — "any
  humanoid NPC, hired or not," not scoped to a betrayal-specific penalty
  for only your own hires). Roughly 10x Hire/Fire's weight, matching
  Ben's "a lot more" framing. Explicitly **not** the same as killing a
  `HostileCreature` (Wolf) — ordinary survival combat carries no Fame
  penalty, only killing a person does.
  **Blocked — real prerequisite gap**: hired NPCs (`NPCFactoryWorker` and
  friends) don't implement `IDamageable` at all today — only
  `HostileCreature` does (confirmed via grep). `PlayerCombat`'s attack
  raycast literally cannot detect a hired NPC as a valid target right
  now. This Fame hook needs a hired-NPC health/death system to exist
  first, same shape as digging needing the Shovel before dig sites could
  ship.

### Player death

- **-2** (Ben's adjustment from an initial -1 proposal). A public
  failure costs some standing — roughly double Fire/Unpaid's weight, but
  a fifth of Kill's — a real but minor stumble, not a moral failing.

### Skill/stat mastery — tier-unlock events

Ben's framing, using the Hulk as the reference point: "everyone knows
who the hulk is for his strength... as your stats change... the fame
gets adjusted as well." Resolved to reuse the *exact* mechanism already
used for discipline skills, not a new live-tracked component — see
"Considered and rejected" below for why.

- **Any skill, any category, crossing into a new `CraftTier` — including
  core stats.** Rudimentary +1, Normal +2, Fine +3, Masterwork +5
  (scaling with how meaningful the milestone is, same spirit as
  `CraftTierScale`'s other per-tier tables).
- **Mechanism**: reuses `PlayerSkills.GainExperience`'s existing
  `TierJustUnlocked` detection — the exact code path that already
  triggers the "skill increased" banner message, for *every* skill
  regardless of `SkillCategory` (Gathering/CraftingDiscipline/Combat/
  Magic/Attribute). Core stats (Strength/Dexterity/Constitution/
  Intelligence) already flow through this identical path today (that's
  how Strength's own tier-unlock banner already works), so **no new
  component is needed for the Hulk case specifically** — only
  confirmation that Fame's hook isn't scoped to exclude the `Attribute`
  category. Real implementation note for later: `GainExperience` doesn't
  currently expose this event externally; `PlayerFame` will need either
  a new event/callback on `PlayerSkills`, or to duplicate the tier-check
  logic itself.

**Considered and rejected**: a live, continuously-recalculated Fame term
tracking current stat value (rising *and* falling as stats change) —
closer to the literal "improves or drops" phrasing and the Hulk framing
(famous for a trait you currently have), but architecturally heavier
(Fame would need to be a stored accumulator *plus* a separately
recomputed live term, not a pure accumulator) and moot today since
nothing in this project currently has any stat-decay mechanism at all.
Ben's explicit call: use the one-time tier-unlock credit instead, same
mechanism as every other skill.

### Business/commerce reach (Inn, Trader)

Ben's framing: "if you run an inn... or you run a trader, it stands to
logic that people would learn about you as well."

- **Scales with activity** (Ben's call over a flat ownership bonus) —
  "people would learn about you" means more customers/reach, not just
  having one built. A busy Inn should out-fame an empty one.
- **Placeholder magnitude: +0.1 per customer served/trade completed** —
  deliberately rough, not a real number yet. Needs to be small since this
  could fire far more often than any other input once real, matching the
  "tune by playtesting once the real system exists" spirit of every
  other first-pass number in this project.
  **Blocked — bigger prerequisite than Kill's**: neither an Inn nor a
  Trader concept exists anywhere in this codebase or `docs/design-brief
  .md` today, and there's no vendor/customer/transaction system at all
  (confirmed via grep — only `PlayerCurrency`/`Coin`/banking exist, no
  selling). This isn't a missing hook on an existing system, it's an
  entire unbuilt commerce system — the biggest prerequisite gap of
  anything in this doc.

### Explicitly deferred

- **Notable-outcome Fame** (a `BrilliantSuccess` craft, a rare/notable
  kill) — real candidate, matches the design brief's "the world
  recognizing your competence" framing better than tier-unlocks alone
  (which are bounded/one-time; notable outcomes could keep trickling
  Fame across a whole playthrough even after every discipline is
  mastered). Ben's call: keep v1 to tier-unlocks + NPC-treatment +
  business-reach, add this as a real follow-up rather than designing it
  blind alongside everything else.

## Output side — not yet designed

The design brief's three original examples (hunter → rarer/better game
+ higher-quality meat/hides; blacksmith → better customers/prices;
miner → better luck striking rich veins) were written for a **per-trade**
Fame structure and don't cleanly carry over now that Fame is a single
overall number — a renowned blacksmith's fame currently can't be
distinguished from a renowned hunter's under this structure. Needs its
own design pass.

**One real hook already identified, not yet built**: `ResourceNode`
already has a `bonusChunkChance` field (0-1, currently used for e.g. a
chopped Log's chance of also yielding a Stick) — Fame could scale this
directly for a "better luck" effect, no new field needed on `ResourceNode`
itself.

**Blocked, same as the input-side note above**: "better prices" has
nothing to hook into — no vendor/pricing system exists.

**Not addressed at all yet**: hunting-quality scaling (blocked doubly —
neither a real hunting-diversity system nor a meat/hide-quality mechanic
exists; `HostileCreature`/Wolf is still the only huntable target), and
whether output effects should require a *minimum* Fame threshold (only
kicking in once you're actually renowned, not from Fame's first point)
or scale continuously from zero.

## Open questions before this is buildable

- The full output/effects design (see above).
- How `PlayerFame` actually observes tier-unlock events — `PlayerSkills
  .GainExperience` has no external event today.
- Exact real-world pacing check: has anyone sanity-checked how fast Fame
  would actually move given realistic hire/fire/tier-unlock frequency
  across a playthrough? Not yet simulated the way Strength's capacity
  curve was.
- New Player-tab tile treatment — does Fame get a custom sub-line like
  `DrawStrengthTile`/`DrawIntelligenceTile`/`DrawDexterityTile`/
  `DrawConstitutionTile`, replacing the current `DrawPlaceholderTile`
  call? Not discussed yet.
