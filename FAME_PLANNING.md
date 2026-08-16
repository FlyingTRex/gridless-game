# Fame Planning

Deferred/optional-layer system from `docs/design-brief.md`'s Phase 2 list,
was just a placeholder `Fame: 0` tile on the Player tab with no backing
system at all (added 2026-08-10 purely so the full tab layout could be
judged together). Designed and built same session (2026-08-14).

**Built**: the real `PlayerFame` component, every input with something to
hook (Hire/Fire/unpaid wages, guild Join/Leave, skill/stat tier-unlock),
the NPC-flee output effect, and a real Player-tab tile with a band-name
sub-line. Everything else in this doc — Kill NPC, Player death, Start/
Close a guild, business-reach Fame, and the Traveling Trader — is
designed but blocked on a system that doesn't exist yet, each logged as
its own `BUGS_AND_ENHANCEMENTS.md` follow-up rather than built blind.

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
- **Factions removed from the design entirely, 2026-08-14.** Was going to
  be a separate behavior-driven trust/fear standing system
  (`docs/design-brief.md`'s Factions, Guilds & Warbands section) — never
  built, and duplicated what Fame already does. Fame now absorbs its role
  everywhere it was referenced (Warband conduct, Settlement Warfare
  outcomes) — see `docs/design-brief.md`'s now-renamed "Guilds & Warbands"
  section.
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

## Input side

### NPC treatment

Ben's framing: "if I hire an npc, I should get some fame. If I fire one,
I should lose fame. if I kill an npc... I lose a lot more fame."

- **Hire an NPC**: **+1** — ✅ **Built.** Hooks `NPCHiring.TryHire` via
  `NPCHiringScreen`'s Hire button.
- **Fire an NPC**: **-0.5** — ✅ **Built.** Hooks `NPCHiring.Fire` via the
  Fire button. Deliberately asymmetric with Hire — Ben's call: leaving is
  a lighter mark than the positive weight of having hired at all, not a
  straight reversal.
- **Leave a hired NPC unpaid**: **-0.5 per missed pay cycle** — ✅
  **Built.** New `unpaidTimer` on `NPCHiring`, separate from `workTimer`
  (which stops advancing once `isWaitingForPayment` is true) — ticks
  every `workDurationSeconds` spent unpaid, resets on `TryPay`/`Fire`.
  Not a one-time hit — a chronically-neglected NPC keeps costing Fame.
- **Kill any humanoid NPC**: **-10** — 🚧 **Blocked, not built.**
  Confirmed applies broadly ("any humanoid NPC, hired or not," not
  scoped to a betrayal-specific penalty for only your own hires),
  roughly 10x Hire/Fire's weight. Explicitly **not** the same as killing
  a `HostileCreature` (Wolf) — ordinary survival combat carries no Fame
  penalty, only killing a person does.
  **Real prerequisite gap**: hired NPCs (`NPCFactoryWorker` and friends)
  don't implement `IDamageable` at all — only `HostileCreature` does.
  `PlayerCombat`'s attack raycast literally cannot detect a hired NPC as
  a valid target right now. Logged as its own `BUGS_AND_ENHANCEMENTS.md`
  follow-up rather than built blind.

### Player death

- **-2** — 🚧 **Blocked, not built.** A public failure costs some
  standing — double Fire/Unpaid's weight, a fifth of Kill's.
  **Real prerequisite gap, found while building this pass**: there is no
  player-death detection anywhere in the codebase at all —
  `PlayerVitals.health` just clamps at 0 via `Mathf.Max`, nothing ever
  fires a "player died" event, no respawn/game-over exists. This wasn't
  caught during the original design conversation; logged as its own
  `BUGS_AND_ENHANCEMENTS.md` follow-up now.

### Skill/stat mastery — tier-unlock events

Ben's framing, using the Hulk as the reference point: "everyone knows
who the hulk is for his strength... as your stats change... the fame
gets adjusted as well." Resolved to reuse the *exact* mechanism already
used for discipline skills, not a new live-tracked component — see
"Considered and rejected" below for why. — ✅ **Built.**

- **Any skill, any category, crossing into a new `CraftTier` — including
  core stats.** Rudimentary +1, Normal +2, Fine +3, Masterwork +5, in
  `CraftTierScale.FameOnTierUnlock(tier)` — its own dedicated table, same
  pattern as `WeaponDamageBonus`/`WeightModifier`.
- **Mechanism**: `PlayerSkills` gained a new `event Action<CraftTier>
  TierUnlocked`, invoked from `GainExperience` right where
  `TierJustUnlocked` already fires the "skill increased" banner — for
  *every* skill regardless of `SkillCategory` (Gathering/
  CraftingDiscipline/Combat/Magic/Attribute). Core stats already flowed
  through this exact path (that's how Strength's own tier-unlock banner
  already worked), so the Hulk case needed **no new detection logic**,
  just `PlayerFame` subscribing to the new event in `OnEnable`.

**Considered and rejected**: a live, continuously-recalculated Fame term
tracking current stat value (rising *and* falling as stats change) —
closer to the literal "improves or drops" phrasing and the Hulk framing
(famous for a trait you currently have), but architecturally heavier
(Fame would need to be a stored accumulator *plus* a separately
recomputed live term, not a pure accumulator) and moot today since
nothing in this project currently has any stat-decay mechanism at all.
Ben's explicit call: use the one-time tier-unlock credit instead, same
mechanism as every other skill.

### Guild membership

Ben's framing: "let's have a flat and equal gain and loss for joining a
guild. it would stand to logic that being part of a guild makes you
known to more people. starting a guild would give you a bigger addition,
and closing the guild would cause you to lose double the join."

- **Join a guild**: **+1** — ✅ **Built.** Hooks `PlayerGuilds.Join`
  (the only current entry point is admin-only per that script's own
  comment, but the Fame hook attaches to the method itself, so it fires
  the same way once a real in-world join UI ships too).
- **Leave a guild**: **-1** — ✅ **Built.** Hooks `PlayerGuilds.Leave`,
  flat and equal to Join, deliberately symmetric (unlike Hire/Fire's
  asymmetric +1/-0.5).
- **Start (found) a guild**: **+3** — 🚧 **Blocked, not built.** Bigger
  than joining an existing one, first-pass number.
  **Real prerequisite gap**: `GuildDefinition` is a plain pre-authored
  `ScriptableObject` asset (`[CreateAssetMenu]`, hand-built in the Editor
  like `SkillDefinition`) — there's no player-driven guild-creation
  mechanic at all. A player "starting a guild" doesn't exist as a
  concept yet, only joining/leaving a developer-authored one does.
  Logged as its own `BUGS_AND_ENHANCEMENTS.md` follow-up.
- **Close a guild you started**: **-6** — 🚧 **Blocked, not built.**
  Landed at 2x the Start amount (3), not 2x Join as the original framing
  suggested — Ben's final given number (-6) is what's recorded here.
  Same blocker as Start.
  Same blocker as Start — needs the same not-yet-existing guild-creation
  mechanic.

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
  🚧 **Blocked, not built — bigger prerequisite than Kill's**: neither an
  Inn nor a Trader concept exists anywhere in this codebase or
  `docs/design-brief.md` today, and there's no vendor/customer/
  transaction system at all (confirmed via grep — only
  `PlayerCurrency`/`Coin`/banking exist, no selling). This isn't a
  missing hook on an existing system, it's an entire unbuilt commerce
  system — the biggest prerequisite gap of anything in this doc. Logged
  as its own `BUGS_AND_ENHANCEMENTS.md` follow-up.

### Explicitly deferred

- **Notable-outcome Fame** (a `BrilliantSuccess` craft, a rare/notable
  kill) — real candidate, matches the design brief's "the world
  recognizing your competence" framing better than tier-unlocks alone
  (which are bounded/one-time; notable outcomes could keep trickling
  Fame across a whole playthrough even after every discipline is
  mastered). Ben's call: keep v1 to tier-unlocks + NPC-treatment +
  business-reach, add this as a real follow-up rather than designing it
  blind alongside everything else.

## Output side

The design brief's three original examples (hunter → rarer/better game
+ higher-quality meat/hides; blacksmith → better customers/prices;
miner → better luck striking rich veins) were written for a **per-trade**
Fame structure and don't cleanly carry over now that Fame is a single
overall number — a renowned blacksmith's fame currently can't be
distinguished from a renowned hunter's under this structure. Two real
effects got fully designed this session anyway (below); the rest is
still open.

### Negative Fame — NPCs flee — ✅ Built

Ben's framing: "if you have negative fame, npc's will run away from you.
you are a potential threat."

- **Trigger**: simple threshold for v1 — any Fame < 0, at a fixed
  detection range (**~10m**, wider than `NPCWander`'s own 6m wander
  radius so there's room to actually flee before the player is on top of
  them). Scaling detection range/urgency by *how* negative Fame is was
  considered and explicitly deferred to a later refinement.
- **Scope**: applies to **every** NPC, including ones the player has
  already hired — confirmed deliberately harsher than scoping it to
  strangers only. A hired NPC fleeing **pauses their current job**
  (mining, gathering, ...) for the duration, same shape as the existing
  dialogue-pause mechanism (`NPCWander.SetPaused`), and resumes normal
  behavior once the player leaves detection range. Does **not** auto-fire
  or un-hire anyone — that stays a deliberate player action.
- **Movement**: reuses `NPCWander`'s existing move/ground-sample/face
  plumbing, just picking a target *away* from the player instead of a
  random one, at roughly **2x** normal wander speed (~2.4 vs. the
  default 1.2) so it reads as panic, not casual repositioning.
- **Built**: `NPCFlee.cs`, added to `NPCFactoryWorker.prefab`. Checks
  distance to `PlayerFame`'s transform each frame; while fleeing, calls
  `SetPaused(true)` on both `NPCWander` and (if present) `NPCGathering`
  and drives movement directly via a small local copy of `NPCWander`'s
  MoveTowards/ground-sample/face pattern (aimed away from the player
  instead of at a random point), then un-pauses both once the player
  leaves range.

### Fame bands, and the Traveling Trader

Ben's framing: "negative fame would end up reducing how often the
travelling trader showed up, and a positive fame would make the trader
show up more. if the player fame was over 500, the quality of items the
trader has available would increase. NPC traders/vendors (including
food) could also alter their price based on fame."

**A new concept, distinct from the input-side "run a Trader" business**:
this is a wandering vendor NPC that periodically visits the player,
not something the player operates. Neither this nor the input-side
Trader/Inn exist yet — no vendor/customer/transaction system exists
anywhere in this codebase at all (confirmed via grep, same finding as
the input side's "business/commerce reach" section above).

**Five discrete Fame bands** (Ben confirmed these edges), mirroring
`CraftTier`'s own 5-tier shape:

| Band | Fame range | Visit frequency | Pricing |
|---|---|---|---|
| Infamous | ≤ -500 | 0.5x | +50% |
| Notorious | -499 to -100 | 0.75x | +20% |
| Neutral | -99 to 99 | 1.0x (baseline) | baseline |
| Known | 100 to 499 | 1.25x | -10% |
| Renowned | ≥ 500 | 1.5x | -20% |

- **Visit frequency** multiplier applies to however the Traveling
  Trader's spawn/visit interval ends up being built. **The mechanism
  is now designed** (2026-08-16, see `VILLAGE_FLAG_PLANNING.md`) — a
  craftable Village Flag beacon with its own spawn-interval timer that
  reuses this exact band table, originally designed for settler-NPC
  population growth (a separate, sooner-buildable half) but explicitly
  built to be reusable for the Trader once the wider commerce
  prerequisite below exists.
- **Pricing** is symmetric (confirmed) — Renowned gets a real discount,
  not just "Infamous pays a markup, everyone else is flat." Applies
  broadly to "NPC traders/vendors (including food)," not just this one
  Traveling Trader specifically.
- **Item quality** only kicks in at the top band (Renowned, ≥ 500) — the
  original single threshold Ben gave, now folded into the same band
  table rather than a separate special case.
- First-pass numbers (frequency/price multipliers), same "tune by
  playtesting" spirit as everything else in this project — band *edges*
  are confirmed, the multiplier magnitudes are a starting point.

### Still open

- The `bonusChunkChance` hook (`ResourceNode` already has this field,
  0-1, e.g. a chopped Log's chance of also yielding a Stick) — Fame
  scaling it directly for a "better luck" gathering effect (the miner
  example from the design brief) is a real candidate, not yet designed
  in detail or confirmed.
- Hunting-quality scaling (blocked doubly — neither a real
  hunting-diversity system nor a meat/hide-quality mechanic exists;
  `HostileCreature`/Wolf is still the only huntable target).
- Whether the Flee/Trader band effects are the *only* output effects, or
  more get added later (e.g. a per-trade-style effect once/if Fame ever
  gets split back into per-discipline values).

## Built this session (2026-08-14)

- `PlayerFame.cs` — the real component, `-1000` to `1000`, clamped.
- `PlayerSkills.TierUnlocked` event + `CraftTierScale.FameOnTierUnlock`.
- Hire/Fire hooks in `NPCHiringScreen`; unpaid-cycle tracking in
  `NPCHiring`; Join/Leave hooks in `PlayerGuilds`.
- `NPCFlee.cs`, added to `NPCFactoryWorker.prefab`.
- `PlayerMenuScreen.DrawFameTile` — real value + band-name sub-line,
  replacing the old `DrawPlaceholderTile("Fame", "0")` call.

## Real prerequisite gaps — logged as `BUGS_AND_ENHANCEMENTS.md` follow-ups

- **Kill NPC Fame** — needs hired NPCs to implement `IDamageable`/a
  health-death system.
- **Player death Fame** — needs player-death detection to exist at all;
  found while building this pass, wasn't caught during design.
- **Start/Close guild Fame** — needs a player-driven guild-creation
  mechanic; `GuildDefinition` is currently a pre-authored asset only.
- **Business-reach Fame + the Traveling Trader** (both input and output
  sides) — needs an entire vendor/customer/transaction system that
  doesn't exist in any form. The single biggest prerequisite in this doc.

## Still open, not blocked on anything — just not decided

- Exact real-world pacing check: has anyone sanity-checked how fast Fame
  would actually move given realistic hire/fire/tier-unlock frequency
  across a playthrough? Not yet simulated the way Strength's capacity
  curve was.
- Everything in "Still open" above (the `bonusChunkChance` hook,
  hunting-quality scaling, whether Flee/Trader are the only output
  effects) — real candidates, none confirmed in detail yet.
