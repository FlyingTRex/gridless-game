# Endgame Planning — Leaving the Planet

**Status: planning only, nothing built (2026-08-16).** Ben's ask: a
critical, honest big-picture map from today's basic-survival state to
the already-designed endgame (`docs/skill-path-space.md` /
`design-brief.md`'s "Endgame: Leaving the Planet"), to shape near-term
development priority rather than let every session keep building
outward (survival/crafting/NPC systems) with nothing pointed upward
(the actual path to space). This doc is the first pass at making that
convergence layer mechanically real, not just design prose.

## 1. The honest audit this doc is built on

Cross-checked every one of the endgame's 8 "Keystone" disciplines
against what actually exists in code (2026-08-16):

| Discipline | Design-brief Keystone | Real skill(s) today | Status |
|---|---|---|---|
| Survival | Master Forager | `Gathering` | Real |
| Medical | Master Physician | `Medicine` | Real |
| Combat | Master Warlord | `Melee`/`Archery`/`Guarding`/`Bare-handed` | Real but fragmented — no unifying "Combat" skill |
| Trade/Guilds | Master Financier | none | **Doesn't exist** — `PlayerGuilds` only supports join/leave a dev-authored guild; no player-driven economy at all (see `COMMERCE_PLANNING.md`, itself only just planned) |
| Blacksmithing | Master Smith | `Metalworking`/`Forging` | Real, mature 5-tier ladder |
| Engineering | Master Engineer | none | **Doesn't exist at all** — zero skill, zero items, zero code, confirmed via a full-repo grep |
| Carpentry | Master Builder | `Woodworking` | Real, mature 5-tier ladder |
| Magic | Master of the Lineage | `Elemental`/`Kinetic`/`Illusion`/`Restoration` | Real — 4 lineages, real wish-casting, real skill-via-use XP |

**The structural problem underneath all of this**: every skill in the
game caps at level 100, and 100 already has a meaning —
`CraftTierScale.SkillRequirement(Masterwork)`. There is no headroom
above Masterwork for a distinct "Keystone" tier to live in. Today,
"Master Smith" is design-doc language with no corresponding game state
— reaching Metalworking 100 currently does nothing except let you craft
the best gear. No Gateway trigger, no Ruins, no route-check exists
anywhere. This doc's first real job is deciding what "Keystone" *means*
mechanically, not just scoping content.

## 2. Decision: what "Keystone" means (proposed, not yet Ben-confirmed)

**Proposed: Keystone = skill level 100 (Masterwork) in the discipline's
named skill(s), nothing new above the existing cap.** Reasoning: this
needs zero changes to `PlayerSkills`' leveling math, reuses the exact
number every skill already climbs toward, and treats "you're the best
in the world at X" as a legitimate narrative Keystone rather than
inventing a 6th `CraftTier`-style rung nothing else in the game has.
The cost is real, though — Masterwork today is purely a *crafting
quality* gate (can you make Masterwork gear), and reusing it as an
*endgame narrative* gate conflates two different claims ("your gear is
top-tier" vs. "you personally are a master of this discipline"). For
skills with no crafting output at all (`Guarding`, the 4 Magic
lineages, `Gathering`), this distinction doesn't bite — level 100 there
already only means "you're extremely good at this," so the reuse is
clean specifically for those. It's murkier for `Metalworking`/
`Woodworking`, where 100 today just measures your best possible
*output*, not some broader mastery. Flagging this rather than
pretending it's a clean fit — **needs Ben's confirmation**, not
resolved unilaterally here.

**Multi-skill disciplines** (Combat → 4 skills, no unifying skill;
Blacksmithing → 2 skills) need an explicit rule. Proposed: **any one**
of a discipline's component skills hitting 100 satisfies that
discipline's Keystone — same "any-one-of-eight" spirit the Gateway
itself already uses one level up. A player who maxed Archery but never
touched Melee still counts as Keystone-Combat. Simpler than requiring
all of a discipline's skills, and consistent with the design's overall
philosophy of rewarding depth in *a* thing over breadth across
everything.

## 3. The Gateway — Ruins of the Old Engineers

**Mechanically**: a new `PlayerEndgame` (or similar) component,
polling (or event-driven off `PlayerSkills.TierUnlocked`, which already
fires on every tier crossing including Masterwork) for whether *any*
tracked discipline-Keystone condition above is met. First time it's
true, reveal the Ruins — a new placeable/discoverable location (not
designed here: does it spawn at a fixed distant point, get revealed on
the Player Map like a Village Flag, or need to be physically found?
**Open question, not decided**).

Reuses real, already-built infrastructure either way: `PlayerSkills
.TierUnlocked` (Fame system already subscribes to this exact event),
`PlayerMapExploration.RevealCircle` (if map-based reveal is chosen),
and the Fame band/reveal-radius pattern `CraftTierScale
.VillageFlagRevealRadius` already established.

## 4. Route scoping and recommended build order

**Recommendation, reinforced by the audit above: build Arcane
Propulsion first, and only that one, before touching the other three.**
It's the only route requiring zero new disciplines or systems — Magic
already has everything a route needs (a real skill, real XP gain, real
distinct lineages). Building it first proves the entire pipeline
(Keystone check → Gateway reveal → route requirement check → Escape
Velocity → Ascend to the Stars) end-to-end with the least new work,
and every other route reuses that same pipeline once it exists.

### Arcane Propulsion (mystic) — build first
**Requires** (design-brief): Master of the Lineage + a mid-tier Kinetic
node. Translated to real state: any one Magic lineage skill at 100,
plus `Kinetic` at some mid threshold (proposed: 50, matching
`CraftTierScale.SkillRequirement(Fine)` — "clearly skilled, not yet
mastered," consistent with every other mid-tier gate in this project).
Requires `Kinetic` specifically per design-brief text even if the
player's *maxed* lineage is a different one (e.g., maxed Elemental,
Kinetic at 50) — worth confirming this reading with Ben rather than
assuming, since the alternative ("any lineage at 100 AND that same
lineage at 50" is nonsensical) needs the docs' intent double-checked.
No new content needed beyond the route-check itself and whatever
"Ascend to the Stars" actually does (section 6).

**Stops along the way** (Ben's ask, 2026-08-16 — a real staged
progression per route, not just a start/end pair):

1. Lineage assigned at character start — **built**.
2. First wish cast (Crude, level 0) — **built**, this is just playing
   the game today.
3. Rudimentary (10) → Normal (25) → Fine (50) — **built**, ordinary
   skill-via-use progression, no new content needed at any of these
   steps. Fine (50) is worth calling out specifically: it's the exact
   number this route's own "mid-tier Kinetic node" requirement already
   maps to (section 4 above).
4. Masterwork (100) in any one lineage — **built** (the skill can
   reach 100 today), but nothing currently *does* anything when it
   happens. This is the Keystone condition itself — needs the Gateway
   trigger from section 3 wired up before crossing this line means
   anything beyond a number.
5. `Kinetic` specifically at 50, independent of which lineage got
   maxed in step 4 — **built mechanically** (just another skill level
   check), not yet wired into any route-check.
6. Escape Velocity → Ascend to the Stars — **not built at all**, see
   section 5.

Steps 1-5 are all things a character can do in the game *today* — the
entire gap for this route is steps 4/5's route-check and step 6's
destination, not any new grindable content. This is the strongest
argument for building this route first: everything upstream of the
Gateway already exists.

### Orbital Engineering (purist) — real content gap, not just scoping
**Requires:** Master Smith + Master Engineer + Master Builder. Smith
and Builder are real; **Engineering is not a system that exists in any
form.** Before this route can be scoped at all, Engineering needs: a
`SkillDefinition`, a reason to exist distinct from Metalworking/
Forging/Woodworking (what does it actually craft? components for what?
automation, per the design brief's own Phase 3 "Automation/logic
system" wishlist item?), and enough recipes to reach level 100 through
normal play. This is a genuine multi-session content build, not a
quick follow-up — logged here as the real next-biggest lift after
Arcane Propulsion ships, not attempted in this pass.

**Stops along the way:**

1. First Crude tool via Metalworking or Woodworking — **built**, day
   one of the game.
2. Full 5-metal ore pipeline (Copper→Iron→Silver→Gold→Platinum,
   Furnace + fuel, Ingots) — **built**.
3. Fine tier (50) in both Metalworking and Woodworking — **built**,
   ordinary progression.
4. A real settlement-scale Builder milestone already exists and fits
   naturally here even though it wasn't designed for this route: a
   placed Masterwork Village Flag + 10 hired NPCs unlocks the **City
   Statue** (v0.3.104-dev) — **built**. Worth treating as an informal
   "you're operating at Builder-Keystone scale" checkpoint even before
   Woodworking itself hits 100.
5. Masterwork (100) in both Metalworking *and* Woodworking — **built
   mechanically**, reachable today, just ordinary grinding.
6. Engineering invented as a real discipline — **does not exist**, the
   actual blocker. No stops are possible past this point until it's
   built. Once it exists, a plausible internal ladder (not designed in
   detail here): basic mechanisms/components → automation systems (the
   design brief's own Phase 3 "Automation/logic system" wishlist item
   is a natural on-ramp) → propulsion-specific systems as the
   Masterwork-tier content.
7. Masterwork (100) Engineering — **gap**, blocked on step 6.
8. All three disciplines at Masterwork simultaneously → Keystone trio
   complete → Gateway reveal (section 3).
9. A physical "build the launch vehicle" late-game project/structure —
   **not designed at all**. Likely needs its own Build-tab-scale
   mega-project, bigger than anything built so far (City Statue is the
   closest precedent in shape, not scale).

The honest read: steps 1-5 are just ordinary crafting-game progression
this project already does well. Steps 6-9 are where this route
actually lives, and none of it exists yet — this route's real cost is
entirely in the part past the Ruins gate, not before it.

### Chartered Expedition (merchant) — blocked on Commerce
**Requires:** Master Financier + a Guild node. Blocked on the same gap
`COMMERCE_PLANNING.md` already covers (no player-driven economy exists)
plus a missing Financier skill and player-founded guilds (`PlayerGuilds`
today is join/leave a dev-authored `GuildDefinition` only). Sequencing:
this route can't be meaningfully scoped until Commerce's `VendorStall`
work has shipped and proven out — revisit this route's requirements
once that's real, not before.

**Stops along the way** (heavily gated — most of this ladder doesn't
exist yet, listed as the plausible shape once Commerce ships, not a
commitment):

1. First trade with a prespawned Village Vendor (`COMMERCE_PLANNING.md`'s
   simplest driver) — **planned, not built**.
2. Operate a player-built Vendor Stall — **planned, not built**.
3. Join an existing (dev-authored) Guild — **built** (`PlayerGuilds`),
   the one real step on this entire route.
4. Found a player-driven Guild — **does not exist**, explicitly
   blocked per `FAME_PLANNING.md` ("a player 'starting a guild' doesn't
   exist as a concept, only joining/leaving a developer-authored one
   does"). No amount of Commerce work fixes this on its own — it's a
   separate gap.
5. Guild reaches an internal "Apprenticeship" mid-tier node — **there
   is no internal rank/tier concept on a Guild at all today**, checked
   directly against `PlayerGuilds.cs`. This isn't just unbuilt, it's a
   concept the current data model has no room for yet (`GuildDefinition`
   is a flat pre-authored asset, no XP/rank field anywhere).
6. A Financier skill reaching Masterwork (100) — **the skill doesn't
   exist**, on top of everything above.
7. Fund/contract an NPC-crewed ship, or otherwise "win" this route
   economically — **entirely new mechanic, not designed anywhere.**

Of the four routes, this one has the least existing scaffolding beneath
it — even step 3 (the only genuinely built stop) is closer to a
side-effect of the Hireable NPC system than real progress toward
"Financier." Confirms the section 4 sequencing call: don't invest here
before Commerce is real.

### Conquered Launch Site (warlord) — last, and possibly out of scope for a while
**Requires:** Master Warlord + Siege Tactics/Warbands. Settlement
Warfare is explicitly described in `design-brief.md` as *never having
been built*, and meaningfully touches PvP/multiplayer territory this
project has no infrastructure for at all (`MULTIPLAYER_PLANNING.md` is
exploration-only). Of the four routes, this is the one most likely to
stay purely aspirational for the longest time — not worth scoping
further until Settlement Warfare gets its own real design pass, which
itself is gated on questions bigger than this document.

**Stops along the way** (also heavily gated, but with one genuine
surprise — real combat-NPC infrastructure already exists closer to
"Warband" than expected):

1. Personal combat mastery — Masterwork (100) in any one of
   Melee/Archery/Guarding — **built mechanically**, reachable today
   through ordinary play, same as any other skill.
2. Command an NPC combatant — **built**, and closer to this route than
   it looks: `NPCGuarding.cs` (v0.3.106-dev) is already a hired NPC
   that fights autonomously, patrols, and can die permanently. It's not
   framed as a "Warband" anywhere in the code, but the raw ingredient
   (a player-directed combat NPC) already exists.
3. A formal "Warband" concept — multiple combat NPCs organized/
   commanded as a real unit, distinct from individually assigning
   several Guards — **does not exist**. Step 2's ingredient hasn't been
   generalized into this yet.
4. Settlement Warfare's actual capture/destroy mechanic against a real
   target (NPC-held or player-held) — **does not exist at all**,
   `design-brief.md`'s own words: "never got built."
5. A discoverable/besiegeable Launch Site as a real location — **not
   designed**, new content on top of everything above.
6. Taking the Launch Site by force → this route's Keystone-adjacent
   condition satisfied.

Step 2 is the one genuinely encouraging finding in this whole route —
Guarding wasn't built with this endgame route in mind, but it's real
proof that "an NPC that fights on the player's behalf" isn't a
from-scratch problem anymore. Steps 3-5 are still a lot of new systemic
work, but less than it would have been a week ago.

## 5. Escape Velocity / Ascend to the Stars — still genuinely undecided

Per `skill-path-space.md`'s own open questions, unchanged here:
- Are the four routes mutually exclusive per-character, or freely
  chosen among whichever a player qualifies for? **Proposed default:
  freely chosen** — simpler to implement (no need to "lock in" a route
  choice anywhere), and rewards a player who happens to qualify for
  multiple without punishing them for it.
- What does Ascend to the Stars actually *do*? Ends the character's
  arc? Unlocks new content? A repeatable milestone? **Not proposed
  here** — this is a real game-design decision (not an engineering
  one) that needs Ben's call before any "liftoff" content gets built,
  since it changes what liftoff even needs to contain.
- Conquered Launch Site's Fame consequences — blocked on Settlement
  Warfare's own capture/destroy Fame ruling, itself unresolved.
- Does Chartered Expedition require personally traveling, or can a
  purely economic playstyle "win"? Blocked on Commerce existing at all
  first.

## 6. Explicitly out of scope for this pass

- Building the Engineering discipline from scratch.
- Settlement Warfare / Warbands in any form.
- Any actual "Ruins of the Old Engineers" content/location/model.
- What Ascend to the Stars does mechanically.
- The Gateway reveal's exact trigger UX (map ping vs. physical
  discovery vs. something else).
- Re-litigating whether Masterwork-as-Keystone is the right call —
  proposed in section 2, needs a real decision, not built either way
  yet.

## Cross-references

- `docs/skill-path-space.md` / `design-brief.md`'s "Endgame: Leaving
  the Planet" — the source design this doc translates into buildable
  steps.
- `COMMERCE_PLANNING.md` — the real prerequisite for Chartered
  Expedition; also the most recent example of this project's "one
  shared mechanic, thin drivers" approach, same instinct this doc tries
  to apply to the Gateway/route-check layer.
- `FAME_PLANNING.md` — `PlayerSkills.TierUnlocked` (already fires on
  every Masterwork crossing) and the Fame-band reveal-radius pattern
  the Gateway reveal would reuse.
- `MULTIPLAYER_PLANNING.md` — why Conquered Launch Site is the
  farthest-out route, not just least-built.
- `CraftTierScale.SkillRequirement` — the existing 0/10/25/50/100
  ladder this doc's mid-tier and Keystone thresholds are built against.

Planning only — nothing built yet.
