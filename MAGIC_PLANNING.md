# Magic Planning

Planning only as of 2026-08-18 — nothing here is built. Prompted by a wait-time
planning session while watching an NPC hire timer, framed around one specific
question: does the current Magic backlog actually move the game toward its
stated endgame, or is it more horizontal survival content wearing a magic
costume? See `ENDGAME_PLANNING.md` for the full endgame audit this plan
answers to, and `CLAUDE.md`'s Magic System section (in `BUGS_AND_ENHANCEMENTS.md`)
for the current build's real state.

## The decision this whole plan hangs on

`ENDGAME_PLANNING.md` left one question explicitly unresolved: does reaching
Keystone (level 100, the proposed threshold — itself still unconfirmed) in
Magic require **one** lineage or **all four**?

**Decided 2026-08-18 (Ben's call): all four lineages.**

This isn't a detail — it changes the whole shape of the plan. If Keystone
needed just one lineage, each of the four could be designed independently and
imbalance between them would be fine (players just specialize into whichever
is strongest). Needing all four means **every lineage has to reach genuine
parity at every tier** — a player working toward Keystone has no way to avoid
a weak or under-designed lineage. That constraint drives the build-order
principle below, which is the single most important thing in this document.

## Build-order principle: lockstep tiers, not lineage races

Because all four lineages are mandatory, **no lineage should ever be more
than one tier ahead of the others.** Concretely: don't fully build out
Elemental's whole ladder while Restoration sits at one wish. Every build pass
should move all four lineages up one tier together, or explicitly close the
lineage that's furthest behind first.

**Current state (2026-08-18), audited directly against `PlayerMagic.cs`/
`WishRecipe` assets, not assumed:**

| Lineage | Tier reached | Wish |
|---|---|---|
| Elemental | 1 | Spark (lights a Campfire) |
| Kinetic | 1 | Push (shoves a loose Rigidbody) |
| Restoration | 1 | Heal Self (10 HP over 30s) |
| Illusion | **0** | — nothing |

Illusion is the only lineage below parity. **The very next Magic build pass,
whenever it happens, should be exactly one thing: give Illusion a Tier 1
wish, closing the gap to match the other three — not a scattershot of new
wishes across multiple lineages while Illusion stays at zero.**

Once all four sit at Tier 1, the next pass takes all four to Tier 2 together
(Fireball / Pull / a status-cure wish / a second Illusion wish), and so on.
This document lays out a full four-tier ladder per lineage so that future
passes have a real target, but **the tiers should ship in lockstep, one
row of the table at a time, not lineage-by-lineage.**

## Proposed ladder

Each entry below is designed against something that already exists in the
codebase where possible — reused mechanics, not invented from scratch — and
flagged where it isn't.

### Kinetic (Push exists)
| Tier | Wish | Notes |
|---|---|---|
| 1 (have) | Push | Shoves a loose Rigidbody |
| 2 | **Pull** | Same Rigidbody-force mechanic, opposite direction. Cheapest possible build in this whole plan — reuses 100% of Push's existing code path. |
| 3 | **Leap/Dash** | Kinetic force applied to the *player's own* rigidbody/`CharacterController` instead of a target — real mobility utility (escape, traversal, combat repositioning) in every phase of the game, not just late-game flavor. |
| Keystone | Large-scale Push/Pull | Move a Boulder, clear building rubble — ties into Building tiers and base-building. |

**Worth flagging to Ben directly:** if the endgame route is literally named
"Arcane Propulsion," Kinetic — force applied to matter — is the lineage that
thematically owns liftoff, not Elemental. Don't let "fire = rocket" become
the assumed answer by default just because Elemental is more visually
obvious; Kinetic's whole theme is more literally propulsion.

### Elemental (Spark exists)
| Tier | Wish | Notes |
|---|---|---|
| 1 (have) | Spark | Lights a Campfire |
| 2 | **Fireball** | Already re-audited this session as unblocked — melee and ranged combat both shipped since the original "needs a combat system" blocker was written. Real, obvious next step. |
| 3 | A burn/clear utility | Not yet designed in detail — deliberately **not** an ore-smelting shortcut, see the hard stop below. |
| Keystone | AoE burn / sustained flame field | Real late-game combat/area-denial payoff. |

**Hard stop, not a suggestion:** no Elemental wish should smelt ore or
substitute for Furnace fuel. This project has real invested economy here
(`FuelTier`, the Woodshed idea, the autonomous production chain) — a magic
shortcut around it isn't new content, it's a hole punched in an existing
system that took real design work to build.

### Restoration (Heal Self exists)
| Tier | Wish | Notes |
|---|---|---|
| 1 (have) | Heal Self | 10 HP over 30s |
| 2 | **Status-cure**, not Heal Other | Cures cold/heat/hunger/thirst rather than more raw HP — see the redundancy warning below for why this is the safer 2nd wish. |
| 3 | Heal Other | Only once the lineage has a wish that *isn't* competing with crafted Medicine — see below. |
| Keystone | Area/sustain effect | Possibly the real endgame answer to the still-unbuilt "Universal degradation" enhancement — Restoration magic staving off gear/structure decay would give this lineage genuine unique late-game relevance instead of "healing but bigger." |

**Redundancy warning:** Medicine is one of the real, already-built Keystone
disciplines (Healing Paste exists, trains a real skill). A Restoration wish
that just heals HP is doing what Healing Paste already does, priced in Will
instead of materials — that's not new content, it's the same content with a
different resource sink. The status-cure wish is proposed as Tier 2 instead
of Heal Other specifically to give this lineage a real differentiator before
it starts competing with an existing system.

**Do not build a Resurrection wish without a separate, explicit decision
first.** `NPCVitals.Die()`'s own comment states death is "permanent," and
recent work (the whole Guard-saga fix two sessions ago) directly depended on
a killed creature staying dead. A Resurrect wish would contradict an
established design invariant, not just add a cool ability — flag it to Ben
before it's anywhere near a build script, don't let it sneak in as "the
obvious Restoration finisher."

### Illusion (currently zero — the actual priority)
| Tier | Wish | Notes |
|---|---|---|
| 1 | **Decoy** | Spawns a fake target to redirect aggro. **Cheapest build in this entire document after Pull** — `HostileCreature.RedirectAggro(Transform)` already exists (built for the Guard-saga fix), so this wish is mostly "call the existing method with a decoy transform instead of a Guard." |
| 2 | Invisibility/camouflage | Reduced detection radius vs. hostiles — real early-mid survival value, not just an endgame gate. |
| 3 | Not yet designed | — |
| Keystone | Mass illusion | Camouflage a whole base, or fool ranged NPC attacks — real defensive endgame utility. |

**Cut, not deferred:** a "disguise as an NPC to deceive a rival faction"
idea came up and should be dropped, not just parked. This is a single-player
game with no rival faction to deceive — there's no concrete use case for it
today, and designing around a system that doesn't exist yet is exactly the
kind of speculative scope this project's own conventions warn against.

**Given Decoy is the cheapest wish in the whole plan to build (reuses
existing code) and Illusion is the only lineage below parity, Decoy is the
natural very-next Magic build item.**

## Real gap that has nothing to do with lineage design: the Will-cost table

All 3 existing wishes cost the same flat 60 Will (success) / 40 Will
(failure), explicitly noted in the code as "no reason given yet to differ."
That was fine when every wish was roughly the same power level. It stops
being fine the moment the ladder above ships — Push and "move a Boulder"
cannot reasonably cost the same Will, any more than a Crude and Masterwork
item should cost the same materials.

This needs its own real per-tier cost curve before Tier 2+ wishes ship —
same discipline as `CraftTierScale`'s per-tier tables, but **a genuinely new
table**, not a reused one. (`CLAUDE.md`'s own "a scale tuned for one
quantity doesn't transfer to another" gotcha applies here directly — don't
reach for `CraftTierScale.Modifier` or any existing curve out of
convenience.)

## Open questions, explicitly not decided here

- Is "Keystone = level 100" itself confirmed, or still the `ENDGAME_PLANNING.md`
  proposal awaiting a real answer? This plan assumes it holds, but doesn't
  re-litigate it.
- Does the literal Gateway/liftoff mechanic for Arcane Propulsion consume a
  specific Keystone wish from each lineage, or just require the skill level?
  Not designed here — that's `ENDGAME_PLANNING.md`'s scope, not this doc's.
- Exact Will-cost numbers per tier — flagged as needed above, not proposed
  yet.
- Tier 3 for Illusion — left undesigned pending Decoy/Invisibility landing
  first and seeing how the lineage actually plays.

## The Will-cost table, designed for real (2026-08-18)

First, a correction to this document's own earlier framing: the ladder
above talks about "Tier 1/2/3/Keystone" — four rungs. **That's inconsistent
with every other tiered system in this project**, which always uses the
same 5-rung `CraftTier` ladder (Crude/Rudimentary/Normal/Fine/Masterwork).
`WishRecipe` already has an `unlockTier: CraftTier` field, currently unused
beyond `Crude` by all 3 shipped wishes — no new gating mechanism is needed,
just actually using the field that's already there. Corrected mapping: the
3 existing wishes sit at **Crude**; the ladder tiers above map onto
**Rudimentary → Masterwork**.

**Also folding in Ben's addition (2026-08-18): Intelligence should raise
Max Will**, same as `BUGS_AND_ENHANCEMENTS.md`'s already-open "Max Will
should scale with Intelligence" item — this plan resolves that item's own
open question (does the per-wish `GrowMaxWill` increment stay, or does
Intelligence supersede it?) instead of leaving it unanswered a second time.

### Formula

Mirror Constitution's exact curve *shape* (`Baseline + k × (stat - floor)^1.5`,
`PlayerConstitution.cs`) — not its coefficient, a fresh one tuned for Will's
own range, per this doc's own earlier warning against reusing a scale tuned
for a different quantity. Constitution operates on the *displayed*
0.25–10 attribute value (`GetAttributeValue = level/10`), not the raw
0-100 skill level — easy to get this wrong by a factor of 10, catching it
here before it becomes a real implementation bug:

```
MaxWill = 100 + 4.42 × (Intelligence_displayed - 2)^1.5
```

At Intelligence_displayed = 10 (max), that's `100 + 4.42 × 8^1.5 ≈ 200` —
Max Will roughly doubles from baseline to a maxed-out Intelligence, the
same relative growth Constitution gives Max Health. The coefficient
landing on the same `4.42` as `HealthCoefficient` is a coincidence of
targeting the same 2x growth ratio, not a copy-paste — flagged explicitly
so nobody mistakes it for reuse. **This is a target, not gospel** — needs
real playtesting to confirm 2x is the right ceiling once wishes with real
cost variance actually exist to test against.

### Resolving the "does GrowMaxWill get superseded" question

**Keep both, layered additively, not one replacing the other:**

```
MaxWill = IntelligenceFormula(Intelligence) + wishMasteryBonus
```

`wishMasteryBonus` is exactly today's mechanism (`PlayerMagic` calling
`vitals.GrowMaxWill` on every successful wish), just re-scoped as a
permanent bonus layered on top of a now-live Intelligence-driven baseline
(`PlayerVitals.SetMaxWill`, called every frame the same way
`SetMaxHealth`/`SetMaxStamina` already are) instead of being the only
source. This preserves a real, distinct reward for *actually practicing
magic* — a high-Intelligence character who's never cast a wish shouldn't
have the same effective Will ceiling as one who's cast hundreds — while
giving Intelligence the direct effect Ben asked for. Two additive sources,
not a replacement, same shape Intelligence's own small XP multiplier
already coexists with every other skill's own gain rate elsewhere in this
project.

**Higher-tier successes should also grow Max Will by more than Crude
does** — pushing your limits should build capacity faster than repeating
the easy wish. Proposed `maxWillGrowthPerWish` by tier: Crude 0.5 (current,
unchanged) / Rudimentary 0.8 / Normal 1.2 / Fine 1.8 / Masterwork 2.5.

### The actual cost table

A discrete per-tier table (`CraftTierScale.WishWillCost`-shaped, same
`switch` convention as `ArrowDamageBonus`/`BowDamageBonus` — not a smooth
formula, matching how every other `CraftTier`-keyed value in this codebase
is expressed), covering both `successWillCost` and `failureWillCost`:

| Tier | Success | Failure | Failure as % of success |
|---|---|---|---|
| Crude (existing, unchanged) | 60 | 40 | 67% |
| Rudimentary | 90 | 65 | 72% |
| Normal | 130 | 95 | 73% |
| Fine | 180 | 135 | 75% |
| Masterwork | 260 | 200 | 77% |

**Deliberate design choice, not an accident: the failure/success ratio
climbs with tier instead of staying flat or narrowing.** The lazy version
of this table would let failure get relatively *cheaper* at high tiers
(more room between a big success cost and a small failure cost) — that
would make reckless high-tier spam relatively safe, exactly backwards for
what should be the game's biggest, riskiest casts. Instead, failing an
ambitious Masterwork attempt costs almost as much as succeeding — reaching
for something ambitious should be a real commitment either way, not a
cheap gamble once you've got the Will to spare.

**Real, deliberate secondary gate this creates:** at baseline Max Will
(100, before any Intelligence/practice growth), Normal-tier wishes (130
success cost) and above are **physically uncastable regardless of skill
level** — the player needs to have already grown their Will ceiling
through either Intelligence or prior practice before Fine/Masterwork
wishes are even attemptable once, not just skill-gated. Two independent
gates (skill level via `unlockTier`, capacity via grown Max Will)
reinforcing that top-tier wishes are genuinely late-game content, not a
single checkbox to clear.

## Recommended next build step

Not a full 16-wish push. **Ship Decoy (Illusion Tier 1)** — cheapest build
in the plan, closes the only lineage sitting below parity, and gives every
lineage a real foothold before any lineage gets a second wish. Everything
else in this document is a target for future lockstep passes, not a queue
to build straight through.
