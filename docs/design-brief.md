# The Flying T-Rex — Design Brief

*Codename: The Flying T-Rex. In-fiction/working title: **Gridless** (see the repo's
`docs/game-overview.md` for Ben's original narrative pitch). Status: early concept,
now reconciled with Ben's game-overview doc per `docs/reconciliation-questions.md` —
still not a locked spec.*

## One-Line Pitch
A first-person survival-crafting game set on Gridless — an artificially constructed
replica of Earth built by a mysterious advanced civilization — where you forage and
craft by hand, discover the magical lineage that awakens in you, build up skills like
blacksmithing, and grow a settlement by hiring NPCs who work autonomously — blending
survival-crafting's tactile loop with SimCity/Warcraft-style territory growth.

## Core Fantasy
You're a survivor of a colonist transport ship that suffered catastrophic failure
during reentry, crash-landing alone in untouched wilderness with minimal gear (a
durable metal canteen, a survival knife, a small cache of rations). The world looks
and feels like Earth, but nothing is quite right — clockwork environmental patterns,
off-grid infrastructure hinting at hidden advanced tech, and scattered agrarian
villages populated by descendants of earlier colonists who've regressed to a
pre-industrial era. As you adapt, a magical lineage awakens in you. You forage, craft
tools and gear by hand, and slowly build competence in trades like blacksmithing. As
you establish a base, you can hire NPCs with their own skills to take over work for
you — turning a solo survival game into a small, then growing, settlement.

## Core Pillars

1. **First-person, embodied play.** The player is always a physical character in the
   world, not a top-down cursor. Crafting is a hands-on act (e.g., hammer + anvil + wood
   fuel + steel → sword), not a menu-only abstraction.

2. **Skill-based crafting.** Skills (blacksmithing, etc.) improve with use. Higher skill
   unlocks better recipes/quality, similar to Wurm Online's crafting model.

3. **Hireable, autonomous NPCs.** NPCs have their own skills. You assign them jobs
   (e.g., "work this forge," "haul ore from that stockpile") and they execute
   autonomously over time — Dwarf Fortress-style delegation, not Warcraft-style
   unit micromanagement. This is the mechanism by which a solo survival game scales
   into settlement management without breaking the first-person fantasy.

4. **Two-layer world.**
   - **Macro layer:** An artificially constructed replica of Earth — not the real
     thing, not satellite-accurate, built by a mysterious advanced civilization.
     Real-world geography is preserved (settlements sit at their correct real-world
     locations), but city names are invented rather than reused, which is what makes
     the "this isn't actually Earth" reveal land (see World Scope). Settlements start
     small and grow over time as they're developed — the SimCity/Warcraft-flavored
     layer.
   - **Micro layer:** Whenever the player is at a settlement or out foraging, they're
     in a detailed first-person instance where crafting, NPC direction, and (later)
     combat happen. This mirrors how Mount & Blade, Kenshi, and RimWorld reconcile a
     huge world with intimate, detailed play.

5. **Real-world starting location.** The player's real-world location is detected via
   **IP-based geolocation** and mapped to the nearest seeded city/location as their
   starting point. Because IP geolocation is coarse (city/region-level at best, and can
   be wrong for VPNs/mobile carriers/corporate networks), the detected location should
   be shown to the player for confirmation or manual correction rather than applied
   silently. A short in-game disclosure ("we use your approximate location to place
   your starting city") is recommended even without a formal permission prompt, since
   this is passive collection of real-world data.
   - Note: "zip code" is US-specific — since the world models the whole real Earth's
     geography, this should genericize to postal/location code (or fall back
     gracefully) so it works for non-US players.

6. **Multiplayer: dedicated servers, each a full replica-Earth copy.** Like
   Valheim/Rust/ARK — anyone can host or rent a server, each running its own
   persistent copy of Gridless. A handful to dozens of players join a given server, each spawning
   near their own real-world (IP-geolocated) location within that server's world.
   The server is the authoritative simulation for NPCs, jobs, and settlement growth —
   they keep working even while a given player is offline. This was chosen over a
   single global shared MMO (more meaningful at planetary scale, but needs real
   backend/hosting/moderation investment beyond a solo/small effort) and over
   session-based play with no persistence (conflicts with the city-growth and
   autonomous-NPC pillars above).

7. **Magic (core, universal) — decided.** Every player is randomly assigned one of
   four magical lineages at the start — **Elemental, Illusion, Kinetic, or
   Restoration** — not optional, not rare, no lineage-less players. Abilities start
   minute and require deliberate training to master, following the same skill-via-use
   model as crafting (Pillar 2). Restoration magic integrates directly with the
   medical system. This was a real fork between the two founding docs (magic wasn't
   in the design brief at all before reconciliation) — see the dedicated Magic System
   section below for what's in Phase 1 vs. deferred.

## Character Creation & Stats

New via Ben's `docs/game-overview.md` update — no traditional point-buy attribute
screen at character creation, staying consistent with the skill-via-use philosophy
(Pillar 2) and avoiding pre-world min-maxing before a player has even seen the game:

- **Survival vitals (start full, tracked live):** Health, Hunger, Thirst,
  Stamina/Fatigue, and Body Temperature — the only numbers a new character has at
  spawn. Fills in the full vital list for the Phase 1 food/water item in the Systems
  Wishlist below.
- **No point-buy core attributes:** no Strength/Endurance/Agility/Intelligence
  allocation screen. These emerge over time as skills that grow through play — the
  same skill-via-use model as crafting (Pillar 2) — confirming how the Phase 1
  encumbrance item's "strength/athletics" skill is meant to work: it's grown, not
  chosen.
- **Skills grow through use, not assignment:** combat, crafting trades, medical, and
  foraging/gathering all improve through active use.
- **Magic lineage** is the one randomized element at character creation — confirms
  Pillar 7 / Magic System above.
- **Deferred/optional layers:** Sanity/Morale and Reputation/Fame are candidates for
  later phases, not part of the initial character model.

**Still open — flagged, not resolved here:** Ben's note that Reputation/Fame is a
"later phase" candidate is worth squaring against this brief's existing Phase 2
placement of the Fame/reputation system and Factions (below) — confirm with Ben
whether Phase 2 still holds or these should push later, rather than assuming either
way.

## Tech Stack

- **Engine: Unity 6.3 LTS (6000.3.0f1) — confirmed.** Moved from the originally
  targeted 6.0 LTS after reconsidering with nothing yet built to migrate (see prior
  discussion) — 6.3 gets a longer support runway for the same low switching cost.
  Chosen over Unreal for genre precedent (Valheim, Rust, V Rising all use Unity for
  dedicated-server survival-crafting), C#'s faster iteration for a small team, mature
  dedicated-server netcode options (Fish-Networking, Mirror), and because the macro
  map staying abstracted (rather than a fully rendered 3D globe) means Unreal's
  biggest advantage — seamless large-world streaming — isn't needed.
  - Note: local dev installs may be on a newer patch within 6000.3.x (e.g.
    6000.3.21f1) rather than the exact `6000.3.0f1` pinned in `ProjectVersion.txt` —
    fine within the same LTS line (patch-level, non-breaking), just worth keeping an
    eye on so the whole team converges on one patch eventually.

## World Scope (MVP)

**Decided:** the first playable world is a four-city **upstate New York cluster —
at the real-world locations of Buffalo, Rochester, Syracuse, and Albany** — rather
than the full multi-city replica Earth described in Pillar 4. This is the "crawl"
version of the macro layer: a small, bounded region instead of the many real-world
major-city seeds implied by the full pillar. Matches the Phase 1 MVP philosophy —
prove the core loop and a working settlement before building out the rest of the
planet.

**Real Earth vs. replica — resolved (see `docs/reconciliation-questions.md`):**
Gridless preserves real-world geography (settlements sit at their correct real-world
locations) but uses **invented city names**, not the real ones — that's what makes
the "this isn't actually Earth" reveal land. So these four cities occupy the real
Buffalo/Rochester/Syracuse/Albany locations, but need their own in-fiction names.
**Still open:** the actual invented names — using the real names above as internal
dev-reference labels until the team names them.

This is a better MVP shape than a single city: it gives City Growth's trade-route
modifier and Settlement Warfare's capture/destroy mechanics real targets to interact
with even in the MVP, rather than sitting unused until a hypothetical second city
exists. The four cities also happen to trace the real I-90 corridor west to east —
a natural, literal trade route connecting them, worth considering as the actual
trade-route feature rather than an abstract bonus.

**Interaction with Pillar 5 (real-world starting location) — decided:** keep the
nearest-of-the-4 IP-geolocation logic as designed, even though it won't feel
meaningfully "local" for players outside the US Northeast. Explicit MVP tradeoff,
not an oversight: the feature has to start somewhere and work end-to-end, and it
gets more meaningful automatically as the world expands beyond this one region —
no rework needed later, just more cities to route to.

**Starting size — decided:** all four cities start small and equal, regardless of
their real-world size ordering (Buffalo, Rochester, Syracuse, Albany). No head start
for any city — each one's growth from there is purely a function of the City Growth
Mechanics below (population added, buildings/modifiers built), not its real-world
population. Ties directly into that section: buildings/modifiers and NPC/player
population are literally what makes a city expand, not a fixed multiplier.

**Initial city boundary size — decided:** each city starts with a small, fixed
footprint on the macro layer — roughly hamlet-sized, enough for a handful of starter
building plots, consistent with "all start small." Rather than invent a separate
size curve alongside City Growth's population curve, boundary size is tied to the
*same* population milestones that already drive NPC growth-rate acceleration (e.g.,
whatever threshold shortens the NPC interval — population 10 in the current example —
also unlocks the next boundary size tier). One unified growth curve driving both
population rate and physical footprint, instead of two untracked ones.

**Full-game scope confirmed:** this four-city cluster is the MVP only. The finished
game keeps the full real-world major-city roster described in Pillar 4 as the
destination — this cluster exists to prove the core loop, City Growth, and Settlement
Warfare mechanics on a small, manageable slice before expanding city-by-city toward
that full scope.

**Still open:** exact bounds/radius of each boundary size tier in concrete units, and
the process/criteria for adding the next real city toward the full roster (tooling
readiness? player demand? a fixed content-release cadence?).

## City Growth Mechanics

A settlement grows based on population inside its **city boundary** (the geographic
area associated with that settlement on the macro layer).

- **Trigger:** growth begins once a city boundary has at least 2 resident players.
- **Base rate:** +1 NPC per in-game year.
- **Acceleration:** as total population (players + NPCs) crosses thresholds, the
  interval between new NPCs shortens. Example given: at population 10, the interval
  drops from 12 months to 11 months. Implies a continuing curve at higher thresholds,
  not just one step.
- **Positive modifiers:** settlement features boost growth — trade route, inn,
  saloon, etc. (magnitude/stacking not yet defined).
- **Negative modifiers:** "bad things" reduce or reverse growth — not yet defined,
  but candidates include famine/food shortage, disease outbreak, raid/war damage,
  overcrowding without enough housing, or resource shortages. Ties naturally into
  the Phase 1 food/water system and Phase 2 building tiers (a city plausibly needs
  housing capacity and food surplus to keep growing, not just time).

**Still open before this is implementable:**
- Real-world length of one "game year" (i.e., the growth-rate tuning knob).
- The full acceleration curve beyond the single pop-10 example — is it stepped at
  fixed population milestones, or a smooth formula?
- Whether there's a population cap, or a point where growth plateaus.
- Concrete definitions and magnitudes for the negative modifiers.
- Whether Warcraft-style attacks (Phase 3, Systems Wishlist) reduce population
  directly, or damage the modifiers that drive growth (e.g. burning down the inn) —
  **now answered, see Settlement Warfare below.**

## Settlement Warfare (Capture vs. Destroy)

*Terminology updated per reconciliation: the attacking/defending combatant groups
below are **Warbands/Militias** — see Factions, Guilds & Warbands for how that term
relates to reputation Factions and Merchant Guilds, which are separate systems.*

An attacking Warband chooses one of two objectives against a target city ("City B"),
each with distinct mechanics and consequences:

- **Capture** — the objective is to take the city intact. The attacking Warband
  fights to remove City B's current population (defending players/NPCs) while
  preserving the buildings. Skill and fame consequences are tied directly to the
  outcome: the **losing side's** skill and fame decrease, and the **winning side's**
  skill and fame increase. This makes the Phase 2 fame/reputation system stakeable,
  not just earnable through peaceful practice — a city fight is a real risk to a
  player's standing, not only a risk to the settlement. A Warband's conduct in a
  capture fight also affects the Faction standing of the players in it (see below).
- **Destroy** — the objective is to raze the city rather than take it. Buildings lose
  durability under attack until destroyed, which compounds into City Growth's
  negative modifiers twice over: (1) the building stops providing its positive growth
  modifier (e.g. the inn's bonus is gone), and (2) the resulting rubble sits in the
  city as an active negative modifier of its own, actively dragging growth down
  rather than just removing a bonus — until presumably cleared/rebuilt.

**Still open before this is implementable:**
- On a successful **capture**, does the city's control fully transfer to the
  attacking Warband (including its NPCs, buildings, and accrued growth modifiers),
  or is it partial (tribute, shared control, etc.)?
- What happens to the removed population on capture — killed, driven off and able to
  return, or absorbed into the attacking side?
- Scope of the skill/fame swing: does it apply to every participant on both sides, or
  is it weighted by participation/role (e.g. defenders who fled vs. those who fought)?
  And is it combat-related skills/fame specifically, or all of a character's fame?
- Whether rubble can be cleared and the building rebuilt, and what that costs/takes.
- Whether the fighting is played out live by participating players in real time, or
  abstracted/simulated once a siege is triggered (relevant since dedicated servers
  won't always have both sides' players online at once).
- The **Conquered Launch Site** endgame route (see Endgame: Leaving the Planet,
  below) is specifically a capture of a launch site by a Warband, and needs its
  Faction/reputation fallout ruled on consistently with whatever gets decided here.

## Factions, Guilds & Warbands

Three separate systems, decided via reconciliation — easy to conflate, so kept
distinct here:

- **Factions** — reputation/perception, not territory. How trusted or feared a
  player or group is, driven by behavior: safe, productive settlements build trust;
  raiding erodes it. Purely a standing/reputation layer.
- **Merchant Guilds** — craft-skill bonuses and trade perks. Not territorial — guild
  benefits apply regardless of who controls the surrounding settlement. Per Ben's
  original pitch: structured apprenticeships for advanced crafting tiers, exclusive
  trade contracts, preferential exchange rates on volatile assets like gems, and
  guild-backed caravan protection.
- **Warbands/Militias** — the literal combatant groups in Settlement Warfare (above).
  Separate from reputation Factions, but a Warband's conduct can move the Faction
  standing of the players associated with it — raiding as a Warband erodes your
  personal/group Faction trust even though Factions and Warbands are different
  systems.

**Still open:** concrete mechanics and magnitudes for all three — how Faction
standing is actually measured and what it unlocks/restricts, Merchant Guild
apprenticeship/contract specifics, and how Warband membership is formed/managed.

## Magic System

New via reconciliation with `docs/game-overview.md` — magic wasn't in the design
brief at all before this merge. See Core Pillar 7 for the headline decision (core
and universal, not optional). Details:

- Four lineages, randomly assigned at character creation: **Elemental, Illusion,
  Kinetic, Restoration.** No player goes without one.
- Granular progression — abilities start minute and require deliberate training to
  grow, the same skill-via-use model as crafting (Pillar 2), not a spell-list you
  unlock all at once.
- **Restoration** integrates directly with the medical system (see the combat/
  medical items in the Systems Wishlist below).
- **Phase 1 scope (per reconciliation):** lineage assignment and early-tier ability
  use only. Deeper mastery and any systemic effects beyond that are deferred.

**Still open:** what early-tier abilities actually look like per lineage; whether
lineage assignment happens instantly at spawn or "awakens" over time as part of the
crash-landing narrative; and how (or whether) magic interacts with crafting, combat,
or Settlement Warfare beyond the Restoration/medical tie-in.

## Endgame: Leaving the Planet

Full spec in `docs/skill-path-space.md` (companion to Ben's Skill Atlas visualization)
— summarized here since it's the convergence point for every discipline in the
Systems Wishlist below. Deep late-game content, but establishing the shape now
clarifies what the eight discipline Keystones (Master Forager, Master Physician,
Master Warlord, Master Financier, Master Smith, Master Engineer, Master Builder,
Master of the Lineage) are actually building toward.

- **The Gateway:** reaching *any one* discipline's Keystone reveals the **Ruins of
  the Old Engineers** — the launch complex of whoever actually built this replica.
  Deliberately any-one-of-eight, not all-of-eight: deep investment in a single
  discipline is enough to discover it exists.
- **Four distinct routes from there, only one needed:**
  1. **Orbital Engineering** (purist) — Master Smith + Master Engineer + Master
     Builder. Build a launch vehicle from first principles.
  2. **Arcane Propulsion** (mystic) — Master of the Lineage + a mid-tier Kinetic
     node. Magic substitutes for a rocket engine.
  3. **Chartered Expedition** (merchant) — Master Financier + a mid-tier Guild
     node. Fund and contract someone else's ship rather than build one.
  4. **Conquered Launch Site** (warlord) — Master Warlord + a mid-tier
     Combat/Warbands node. Take an existing launch site by force — ties directly
     into Settlement Warfare and should carry real Faction consequences (see that
     section's still-open items).
- **Convergence:** any one route → **Escape Velocity** → **Ascend to the Stars**.
  One narrow endpoint regardless of which discipline got a player there.

**Still open** (from `docs/skill-path-space.md`, not resolved here): whether the four
routes are mutually exclusive per-character or freely chosen among qualifying ones;
what Ascend to the Stars actually does (ends the character's arc? unlocks a new
layer of play? repeatable/server-wide milestone?); the Faction/reputation ruling for
Conquered Launch Site (ties to the Settlement Warfare capture/destroy consequences
above); and whether Chartered Expedition requires personally traveling or allows a
purely economic "win."

## Systems Wishlist (Feature Inspirations)

A running list of specific mechanics from other games we want to draw from, pulled
together on 2026-07-18. This is intentionally larger than any first playable build —
grouped into phases so it works as a north star without implying everything is needed
on day one. **Phased approach confirmed** ("crawl then run") — Phase 1 is the target
for a first playable build; Phases 2–3 are deliberately deferred, not cut.

**Phase 1 — MVP core loop** (proves the central fantasy is fun before anything else is built)
- **Skill progression via use** (SCUM) — any stat/skill slowly improves with practice.
  Already the basis of Pillar 2.
- **Encumbrance & skill-based movement** — carried weight affects movement speed and
  stamina; carry capacity and movement efficiency improve as related skills (e.g.
  strength/athletics) increase with use, same skill-via-use model as Pillar 2. Tightly
  coupled to loot/gathering and storage below, so it belongs in the same MVP slice.
- **Food/water survival needs** (SCUM).
- **Loot & gathering** — see and pick up items; dig/mine for rocks, dirt, sand, gems,
  precious metal.
- **Skill-tied crafting quality — five named tiers, decided.** Every craftable item can
  be produced at one of five quality tiers: **Crude, Rudimentary, (no adjective — the
  plain/standard item name), Fine, Masterwork.** The middle tier deliberately carries no
  prefix — "Rock Knife," not "Standard Rock Knife" — so the plain name always reads as
  the baseline, with quality only called out when it deviates from it. A low-skill
  blacksmith makes a Crude or Rudimentary sword; higher skill unlocks Fine and
  Masterwork. Low-quality gear degrades faster and performs worse; top-quality stays
  sharp longest and hits hardest. Extends Pillar 2 directly.
  - This formalizes and supersedes `docs/game-overview.md`'s original, less-specific
    "Crude, Standard, and Mastery" three-tier mention (Universal Crafting Ladder
    section) — that pitch line was never actually carried into this brief's Phase 1
    wishlist or given a decision entry in `docs/reconciliation-questions.md`, so the
    five-tier scheme above is the first time concrete tier names are decided anywhere
    in the docs.
  - **Still open:** the exact skill thresholds that unlock each tier, and the concrete
    degradation-rate/performance numbers per tier. The general *shape* of the answer
    (skill vs. material-quality interaction, the full gather/refine/assemble
    pipeline, tool-quality effects) was worked out 2026-08-04 — see the dedicated
    **Crafting, Gathering & Skills Pipeline** section below — but exact numbers are
    still not decided.
- **Basic building** — start of the building module, shelter tier only. Incorporates
  Ben's **"Equip-to-Define" system**: empty architectural shells become functional
  (workshop, inn, clinic, etc.) based on what equipment is installed inside, rather
  than picking a building type up front.
- **Personal storage.**
- **Basic combat + basic first aid** — punching/melee and simple wound care, as the
  floor of the combat/healing module.
- **Character/skills UI** — a personal interface showing all skills and stats at a
  glance.
- **Magic lineage assignment + early-tier ability use** — see the dedicated Magic
  System section. Core and universal per reconciliation, so it belongs in the MVP
  even though deeper mastery is deferred.
- Hireable autonomous NPCs (Pillar 3) — already core to the MVP loop.

**Phase 2 — Settlement depth** (once the solo loop works, deepen the base-building/social layer)
- **Universal degradation** — nothing lasts forever; items and structures decay if
  left unmaintained. Applies across gear, buildings, and vehicles.
- **Skill books/magazines** (7 Days to Die) — readable items that grant basic training
  or boost an existing skill, as an alternate path alongside learn-by-doing.
- **Gardening** — harvest seeds, plant and grow crops.
- **Animal & hunting module** — tame, hunt, harvest, skin.
- **Fame/reputation system** — skill mastery earns fame in that trade line, and fame
  itself feeds back into the world rather than just your own stat sheet: a renowned
  hunter attracts rarer/better game and yields higher-quality meat and hides on a
  kill. Proposed to generalize across trades (e.g. a famous blacksmith draws better
  customers/prices, a renowned miner has better luck striking rich veins) rather than
  being hunting-only — flag if you intended it narrower. Distinct from the Phase 1
  skill-tied quality mechanic: quality is about *your* competence, fame is the world
  *recognizing* it. Also gains a PvP dimension in Phase 3 — see Settlement Warfare:
  winning or losing a city fight moves fame and skill directly, not just practice.
- **Basic transportation** — log raft/boat up through a cart; a tamed animal can pull
  a cart or carry loot.
- **Larger/settlement-level storage**, distinct from personal storage.
- **Building tiers beyond shelter** — progressing toward town-scale construction.
  Includes real estate acquisition options beyond building from scratch: rent, buy,
  or construct.
- **Combat/medical tiers deepen** — ranged weapons; first aid grows toward surgery.
  Includes equippable infirmaries within a player's compound, staffable with hired
  NPC medics (ties to Pillar 3).
- **Reverse engineering & manuals** — disassemble items to learn their schematics,
  then write instructional manuals/grimoires to mentor other players or NPCs. Ties
  into the Phase 1 skill-books idea as the inverse: instead of finding a pre-made
  book, you can author your own from what you've learned.
- **Factions (reputation)** — see the Factions, Guilds & Warbands section. Behavior-
  driven trust/fear standing, separate from Fame above and from combat Warbands.

**Phase 3 — Systemic & late-game depth** (biggest, most complex systems — tackle last)
- **Utility infrastructure** (Icarus) — power and water as real requirements, with
  pipes/cables that must be physically run to connect buildings/machines.
- **Automation/logic system** (Factorio) — circuits, train control, security systems.
  This is a large system on its own (arguably a full game's worth of design in
  Factorio's case) — worth scoping as its own mini-project when we get here.
- **Commerce system** — currency, trading, and banking, using a **5-tier, base-10
  denomination system** (merged via reconciliation from both docs' separate
  versions): Copper (base unit) → Iron (×10) → Silver (×10) → Gold (×10) → Platinum
  (×10). Purely in-game, fictional currency with **no real-money conversion and no
  studio-issued cryptocurrency**. This was an explicit decision, not a default: a
  Second Life-style cash-out system would make the studio a de facto money
  transmitter (FinCEN registration, potential per-state licensing, AML compliance,
  1099 tax reporting on payouts), and a studio-issued crypto token carries
  securities-law exposure (see the 2021–2022 "play-to-earn" wave, several of which
  faced enforcement actions, token collapses, or exploits like the ~$600M Ronin
  bridge hack) plus real reputational risk with this genre's audience. Both remain
  theoretically possible far in the future, but only with real legal counsel — they
  are not designed or planned features of this game. Also includes a **volatile gem
  market** (high-risk/high-reward, prices fluctuate by regional supply/demand) and
  **connected central banking** in larger cities (deposit, storage, digital
  withdrawals).
  - **Implemented ahead of schedule:** a global personal bank account (deposit,
    withdraw, exchange between adjacent currency tiers at a fixed 10:1 rate, a flat 3%
    transaction fee charged on top of the amount moved) and purchasable **Lockboxes**
    — personal coin-storage containers available in all five crafting-quality tiers
    (see the Skill-tied crafting quality item above), each tier scaling both storage
    capacity and purchase price off the Normal tier's baseline (2,500 coins/type,
    10 Gold). This is the personal deposit/withdraw/exchange/storage slice only —
    trading between players, the gem market, and city-scale central banking are still
    not built.
- **Merchant Guilds & Warbands** — see the Factions, Guilds & Warbands section.
  Guilds provide craft bonuses/trade perks and aren't territorial; Warbands are the
  literal combatant groups in Settlement Warfare, deferred here alongside it.
- **Warcraft-style warfare** — assemble and grow a Warband, attack another
  settlement/city. Ties directly into the macro-layer city growth pillar and into
  multiplayer server PvP. Mechanics now defined — see Settlement Warfare section.
- **Research & Patents** — inventing new items grants official patents and ongoing
  licensing royalties. Extends the crafting/reverse-engineering systems above into
  a player-driven IP economy.
- **Full transportation tiers** — steamships, cars, planes, beyond the Phase 2 basics.

## Crafting, Gathering & Skills Pipeline (2026-08-04)

Planning session working out the "still open" gap from the Skill-tied crafting
quality item above (skill thresholds, tool/material effects) — grew into a full
gather → refine → assemble pipeline covering wood, stone, metal, and textiles, plus
a new interaction model for every tool-driven action. **Decided in shape, not in
exact numbers** — see "Still open" at the end. Nothing here is built yet; this is
the plan to review before any of it becomes actual implementation work.

**Skills (8 total, plus a separate weapon-usage tier — see below):**
`Gathering` (existing — now scoped specifically to Sticks, Berries, and plain
Rock: general "stuff found on the ground/bushes"), `Mining` (new — ore
specifically: breaking any Ore Node trains Mining, not Gathering; also governs
the ore-detection ability below), `Woodworking`, `Stonework`, `Metalworking`,
`Forging`, `Minting`, `Sewing`. The `Mining` split from `Gathering` was raised
earlier and initially deferred, then decided in a later pass of the same
session — no longer open. `Crafting` (final assembly) was originally a 9th
skill here — **retired 2026-08-05**, see the discipline-sort rule immediately
below for why.

**Which discipline claims a finished item — resolved 2026-08-05.** Every
finished item is governed by exactly one of the six material-discipline skills
above (not `Crafting`, which no longer exists as a category), determined by
its *defining* material — not every ingredient, just the one that conceptually
makes the item what it is. A stone head/edge defines a Knife/Hammer/Axe/
Pickaxe → `Stonework`, even though all four also consume a Stick. A wood body/
stave defines a Bow → `Woodworking`, even once rope/fiber is involved. This is
why `Crafting` retired: once every item sorts cleanly by defining material,
there's no leftover catch-all category left for it to cover. **Shipped
same-day, v0.1.70-dev:** `Crafting.asset` deleted, the 6 discipline skills
created, all 20 tool recipes repointed to `Stonework`. The 5 items without a
clean defining material (Sunglasses, Nav Computer, Health Monitor, Mining
Face Shield, Canteen) didn't get force-fit into a discipline — they train no
skill at all for now ("just to test ideas up front," not designed with this
rule in mind).

**Dual skill tracks.** Crafting an item trains two things from the same
action: the broad discipline skill (e.g. `Woodworking`), and a narrow
per-item-family proficiency specific to that exact item (e.g. Bow-making,
distinct from Spear-making even though both are `Woodworking`). The broad
skill isn't just a `CraftTier` ceiling (per the weakest-link rule below) — it's
also a **recipe unlock gate**: some recipes aren't attemptable at all until the
discipline skill clears a threshold, not just capped to a lower tier if
attempted early. Per-item proficiency is new data, not yet designed —
`PlayerSkills` today is one float per `SkillDefinition`, which fits the broad
half; the narrow half needs a second dimension nothing currently tracks (keyed
per item-family, not per named skill, so not a `SkillDefinition` at all).

**Weapon usage skills — resolved 2026-08-05.** Using a weapon — combat or
hunting, no split between the two, decided explicitly (the physical act
doesn't meaningfully differ) — trains a skill determined by weapon *type*,
entirely separate from the discipline that *crafting* it trains. Granular, not
one umbrella "Weapon Skills": **Archery**, **Spear**, **Sword**, **Gun**,
**Bare-handed** — five independent skills. E.g. crafting a Bow trains
`Woodworking` (its defining material); shooting it, at anything, trains
`Archery`. Far downstream of anything buildable today — no combat or hunting
system exists yet (same reason Spear/Bow were deferred from the 2026-08-05
tool-tier batch, see `CHANGELOG.md` v0.1.69-dev) — captured here so the
eventual system has a settled shape to build toward rather than being designed
from scratch later.

**Core tier rule — weakest link.** A crafted item's `CraftTier` is the *lower* of
(a) what the relevant skill's current level allows, and (b) the tier of every
material ingredient that went into it — not an average, not skill alone, not
materials alone. Skill sets the ceiling on what you're capable of producing at all;
material quality caps any single result regardless of skill. Applies at every stage
of the pipeline (refining a material and assembling a final item alike), each stage
checking its own relevant skill.
- Floor case: no skill + baseline ("Crude") materials = Crude output.
- A material's *own* achievable tier, when refined, is likewise capped by the
  refining skill's current level at that moment — so repeating a refining action
  with unchanged Crude tools/materials can still climb in output tier over time as
  skill rises mid-grind (skill is checked per-attempt, not locked in at the start).

**Tool-quality effects.** For any tool used in a refining or gathering action,
higher tool tier directly improves three things on that action: **yield** (more
output per attempt), **quality** (higher achievable output tier), and **speed**
(the action itself completes faster). Applies uniformly across every refining line
(trim, shape, saw, and eventually smelt/forge/mint) rather than each tool having
its own bespoke formula. *(Whether tool tier separately boosts skill-gain rate too,
on top of these three, was raised and explicitly parked — not decided either way.)*

**Interaction model — replaces punch-to-break entirely.** Every tool-driven action
(gathering *and* refining) becomes: press E once to start (no need to hold it) →
player is movement-locked until the action completes → a green progress bar shows
how far along it is → Escape cancels and forfeits progress. This replaces the
`IPunchable`/left-click/`hitsToBreak` mechanic currently used by Rock Node, Copper
Ore Node, and the Tree — those would move to the same click-and-locked pattern as
the new refining actions. Reuses the existing (currently unused by anything)
`IInteractable.HoldDuration` concept as its foundation rather than inventing a new
interaction primitive.
- **Tool requirement is per-node, not universal.** Rock Node stays tool-optional —
  bare hands work, just slower and lower-yield (worked example: bare hands = 10s
  for 2 Small Rock; Pickaxe = 8s for 3). Copper Ore and Trees stay **hard-gated** —
  no tool, no interaction at all, consistent with what's already shipped
  (`ResourceNode.requiredTools` — generalized to accept any tier of a tool
  2026-08-05, see `CHANGELOG.md` v0.1.69-dev).
- Whether *final* Crafting (assembling refined materials into a finished item,
  currently the Crafting screen's instant "Craft" button) also becomes a timed
  click-and-locked action, or stays instant/menu-based, was raised and not yet
  resolved.

**Material web:**
- **Wood:** Stick →(Knife, Woodworking)→ Trimmed Stick. **Shipped v0.1.71-dev**
  — full 5-`CraftTier` treatment (Ben's call, not staged as a single item
  first), via the Crafting tab rather than the click-and-locked model below
  (that's still unbuilt) — a `CraftingRecipe` with a Knife (any tier) as a
  held-not-consumed `requiredTools` entry, consuming 1 Stick, training
  Woodworking. Tree →(Axe)→ Logs + Twigs (Twigs is a secondary yield
  alongside Logs) →(Saw, Woodworking)→ Planks — still unbuilt.
  (Renaming note: the chop-tree output shipped this session as an item literally
  named "Wood" — rename to **Logs** whenever this is implemented.)
- **Foraging:** Bush →(search)→ Berries, randomized rather than a guaranteed pickup
  (the existing Berry Bush, made richer — currently a deterministic E-to-pick).
  Same Bush →(Knife/Axe)→ Twigs as an alternative to searching it.
- **Textiles:** Twigs →(Woodworking)→ Fiber →(Sewing)→ Fabric →(Crafting)→ Clothing
  (Shirt, Hat, Pants, Gloves, Boots), Rope, Quiver. Clothing maps onto
  `PlayerEquipment` slots that exist today but have never been used by anything —
  Head, Chest, Leg, and Feet have sat empty since the equipment system was built.
  Gloves doesn't map cleanly to an existing slot (Left/Right Hand are for actively-
  held tools, not worn gloves) — would likely want Left Arm/Right Arm instead, both
  also unused so far.
- **Stone:** Small Rock →(Hammer/rock, Stonework)→ Shaped Rock.
- **Metal:** Ore Node →(Pickaxe, **Mining**)→ Ore + Small Rock (mining an ore vein
  realistically kicks loose waste rock too — every ore node yields *both* its
  primary ore type and Small Rock as a byproduct, not just pure ore) →(Furnace +
  fuel [Sticks, Logs, etc.], Metalworking)→ Ingot, which branches two ways:
  - →(Forging)→ Forged Component (a shaped tool/weapon part)
  - →(Press, Minting)→ Coins — **coins stay plain/fungible, no `CraftTier` on the
    coin itself**; higher Ingot quality and Press quality instead increase *yield*
    (more coins per operation), not a quality label on the coin.
  - Full ore ladder needed eventually: Copper, Iron, Silver, Gold, Platinum —
    mirrors the existing `CoinType` ladder. **Metal type and `CraftTier` are
    orthogonal** (a "Crude Iron Knife" and a "Masterwork Iron Knife" are both
    valid — metal is what it's made of, tier is how well it's made), not merged
    into one axis.
  - **Base ore yield scales down as the ladder climbs** — Copper is easy, Platinum
    yields little without a skilled Miner and good tools to compensate. Rarity and
    skill-gating pull the same direction on purpose, so late-game metal feels
    earned rather than just reskinned Copper.
  - **Silver/Gold/Platinum ore is hidden, not visible.** Nodes containing these
    look like an ordinary Rock Node at a glance — same reveal mechanism already
    built for Sunglasses + the Secret Message Wall, generalized into a real
    gameplay system: a new **Mining Face Shield** (Face-slot equippable) visually
    marks a hidden-ore node as different when worn, and mining it only actually
    yields the ore *with* the shield on — without it, the same node just gives
    Small Rock, ore undetected. **At Mining skill tier 4 (Fine), the shield
    becomes unnecessary** — enough expertise to recognize ore-bearing rock by eye
    alone. Copper (and presumably Iron) stay visibly identifiable as ore nodes,
    same as Copper Ore ships today — this hidden/detection mechanic is specifically
    for the harder, higher-value metals.
  - Furnace is a new placeable *structure*, not a held tool — outlined only
    (transfer ore + fuel in, get metal out), not designed in detail.
- **Hunting weapons** (final assembly recipes, not new systemic mechanics —
  Woodworking-discipline now that `Crafting` retired, see the 2026-08-05
  discipline-sort update above): Stick + Rope → Bow; Stick + Rock → Arrows;
  Fabric → Quiver; Knife + Stick + Rock → Spear. Crafting these trains
  Woodworking; *using* them trains the separate weapon-usage skills above
  (Archery for Bow, Spear for Spear). Purpose: enables basic hunting, ties
  into the design brief's combat pillar and presumably feeds an animals/meat/
  hide loop down the line (not designed here).

**Still open (explicitly not decided, don't assume defaults):**
- Actual skill-level thresholds that unlock each `CraftTier`, per skill — each of
  the 8 skills gets its own curve, not one shared table. (This now also covers the
  Mining-tier-4 shield-bypass threshold specifically, not just `CraftTier` output.)
- The recipe-unlock thresholds themselves (2026-08-05) — how much of a discipline
  skill, specifically, before a given item's recipe becomes attemptable at all.
- The data/UI shape for narrow per-item proficiency (2026-08-05) — not a
  `SkillDefinition`, keyed per item-family instead; nothing about how it's
  tracked, displayed, or how quickly it climbs has been designed.
- The weapon-usage skills (Archery/Spear/Sword/Gun/Bare-handed, 2026-08-05) don't
  mean anything without an actual combat/hunting system, which doesn't exist and
  isn't designed here — only the skill *shape* is settled, nothing mechanical.
- Whether tool tier also boosts skill-gain rate, separately from yield/quality/speed.
- Whether final Crafting (assembly) becomes a timed click-and-locked action or
  stays the current instant menu-based "Craft" button.
- Furnace/smelting mechanics beyond "transfer ore + fuel, get metal" — capacity,
  smelt time, fuel consumption rate, etc.
- Exact ore/Small-Rock byproduct ratio per mining action (fixed split vs. random),
  and the exact per-metal base-yield curve (Copper→Platinum).
- Concrete degradation-rate/performance numbers per `CraftTier` (this was already
  open before this session — see the original Skill-tied crafting quality item).

## Open Questions / Next Decisions

Reconciliation with `docs/game-overview.md` resolved the big cross-doc conflicts
(magic, Factions/Guilds vs. Settlement Warfare, currency ladder, Earth vs. replica —
see `docs/reconciliation-questions.md`). Most remaining open items now live as
"Still open" call-outs in their relevant section rather than here: invented city
names and boundary tier sizes (World Scope), growth-curve/negative-modifier
magnitudes (City Growth Mechanics), capture control-transfer and live-vs-simulated
combat (Settlement Warfare), Faction/Guild/Warband concrete mechanics (Factions,
Guilds & Warbands), and early-tier magic abilities (Magic System). The one item
without a natural home elsewhere:

- **Economy specifics:** currency denominations are now decided (5-tier, see
  Commerce system in Phase 3), but source (mined? earned? printed by settlements?)
  and inter-settlement trade/banking mechanics are still undefined.

## Non-Goals / Risks (for now)
- Not attempting satellite-accurate real-world terrain — a replica built by a
  fictional advanced civilization, not literal Earth (see Pillar 4, Core Fantasy).
- Not attempting to simulate the entire planet's cities/population at once — bounded
  starting scope is now decided as an upstate New York cluster (at the real-world
  locations of Buffalo, Rochester, Syracuse, Albany, pending invented names — see
  World Scope), that expands later.
- IP geolocation is a UX/accuracy risk, not a security or precision feature — treat it
  as a nice-to-have starting hint, not ground truth.
- Not attempting real-money currency conversion or a studio-issued cryptocurrency —
  explicitly rejected due to money-transmitter/securities-law exposure and
  reputational risk (see Commerce system, Phase 3). In-game currency stays entirely
  fictional (copper/iron/silver/gold/platinum).
- Not attempting a single global shared world — each server is its own Earth copy,
  so "real location" is meaningful per-server, not planet-wide.
