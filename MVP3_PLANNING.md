# MVP 3 Planning

Decided conversationally with Ben (2026-08-19), grown directly out of the same
session that produced `TEAMS_AND_GUILDS_PLANNING.md` and the Commerce driver
updates. Started as "should MVP3 be Multiplayer + Teams/Guilds + Commerce, all
together" and got a real "be mean" pass before locking in scope — the honest
finding was that those three items aren't comparable in size or dependency
shape, and bundling them would have hidden that. Resolved into two tiers:

- **MVP3 = Multiplayer conversion (through player-authoritative gameplay) +
  Commerce, running concurrently, not sequenced.**
- **MVP4 = Teams & Guilds + Team/Guild Vendor + whatever else gets picked at
  that point** — deliberately not scoped further right now, since it's
  entirely downstream of MVP3's multiplayer half actually landing.

Ideation/scope-setting only as of this entry — nothing new built by writing
this doc, it's organizing work that was already either built (Commerce
planning, the Phase 0 spike) or already decided (`MULTIPLAYER_PLANNING.md`'s
phased proposal, `TEAMS_AND_GUILDS_PLANNING.md`'s full design).

## Why not one bundled MVP3

Three real problems with treating Multiplayer + Teams/Guilds + Commerce as one
tier, found during the "be mean" pass:

1. **Teams & Guilds cannot be built before Multiplayer's player-authoritative
   phase lands — not a scheduling preference, a hard dependency.** Every
   mechanic in that design (invite, kick, split a sale, promote an Officer) is
   a relationship *between real players*. There's no smaller single-player
   version to build toward first. Listing it as co-equal MVP3 scope would
   have set the doc up to be wrong the moment anyone checked it against
   reality — the same kind of staleness `CLAUDE.md`'s Category C re-audits
   have already caught and fixed elsewhere in this project.
2. **"Multiplayer" isn't a single MVP-shaped item.** Every prior MVP tier in
   this project has been bounded and closeable — build X, test X live, move
   on. Multiplayer's own roadmap (`MULTIPLAYER_PLANNING.md` section 3) is an
   infra spike → a pilot networked object → **"the single largest phase by
   raw effort"** (converting all 48 `PlayerXXX.cs` scripts to Command/
   validate/replicate) → NPCs server-side → persistence restructuring → the
   macro layer. That's a whole separate roadmap, not a checklist line —
   folding it into "MVP3" without naming a real finish line risks it becoming
   the item that never closes (it already sat completely untouched for 6 days
   after Mirror was imported, before Phase 0 finally started).
3. **Commerce has no real dependency on Multiplayer** — only 2 of its 5
   `VendorStall` drivers (Team Vendor, Guild Vendor) need a second real
   player; `VendorStall`'s core, Village Vendor, Traveling Trader, and the
   NPC-staffed Vending job are all buildable and playable in single-player
   today. Gating all of Commerce behind an open-ended multiplayer conversion
   would delay real, ready playability for no structural reason — the same
   critique, just one MVP tier later, if it had been folded into MVP4
   instead.

## MVP3 — Multiplayer (through player-authoritative gameplay) + Commerce

**A real defined finish line for the multiplayer half**, not an open-ended
"multiplayer" bullet: MVP3 covers `MULTIPLAYER_PLANNING.md`'s phases through
**player-authoritative gameplay** — the point where a second real player can
actually exist, move, and act in a shared world. NPCs-server-side,
persistence restructuring, and the macro layer (geolocated spawn, settlement
growth, Warfare/PvP) are explicitly *not* MVP3 scope — they're what unlocks
once this lands, tracked against the planning doc's own later phases, not
squeezed into this tier just because they're "also multiplayer."

- [x] **Phase 0 — infra spike.** Built 2026-08-19, v0.3.145-dev.
  `NetworkSpike.unity` (isolated, not in `EditorBuildSettings`) +
  `NetworkSpikePlayer.prefab`, client-authoritative as a first data point on
  the movement-authority question. Compile/YAML-verified. **Still needs the
  real two-process live test** — steps written out in `WORKING_ON.md`, not
  yet run.
- [ ] **Phase 1 — one pilot networked world object.** `StorageBox`, per the
  planning doc's own recommendation (simplest existing interactable, already
  has the `Active`/`FindNearby` registry pattern). Not started.
- [ ] **Phase 2 — player-authoritative gameplay.** The actual MVP3 finish
  line — systematically converting the 32→48 `PlayerXXX.cs` scripts' local-
  mutation call sites to Command/validate/replicate. Not started, not yet
  broken into sub-phases (the planning doc suggests inventory/equipment
  first, then crafting/building, then magic/combat — a real candidate order,
  not committed).

Real open prerequisite surfaced along the way, worth tracking here since it
blocks real player trade (from `TEAMS_AND_GUILDS_PLANNING.md`) as well as
this tier: **player identity/naming doesn't exist yet.** `NPCDialogue`'s
existing auto-naming + `IRenameable` rename flow is the proven pattern to
port, not a new design question — just not built for the player yet.

**Commerce**, running concurrently, not gated behind the above — see
`COMMERCE_PLANNING.md` for the full design (fully updated 2026-08-19: five
drivers, the Traveling Trader spec fleshed out, the NPC-staffed Vending job
designed). Recommended build order from that doc, unchanged:

- [ ] `VendorStall` core + Village Vendor driver (lowest risk, proves the
  whole mechanic solo).
- [ ] Traveling Trader driver.
- [ ] Player Stall driver (needs the Lockbox-assignment/Bank-locality
  plumbing from that doc's section 6 — or the NPC-staffed Vending job's
  auto-bank automation, which sidesteps needing that plumbing built
  abstractly at all).
- [ ] NPC-staffed Vending job (`NPCVending.cs`, new `JobKind`) — an
  automation upgrade on top of Player Stall, never a requirement for it.
- Team Vendor / Guild Vendor are **not** MVP3 scope — both need Team/Guild to
  exist first, which needs Multiplayer's player-authoritative phase to land.
  They're MVP4 items (see below), tracked here only so the driver list stays
  complete.

## MVP4 — Teams & Guilds + Team/Guild Vendor + TBD

**Ben confirmed, 2026-08-25 (while per-connection spawning was being
built): Teams specifically should be in scope for MVP4 "at least"** —
not left as pure "whatever else." Still blocked on the same
prerequisite as everything else here (MVP3's multiplayer half, real
player identity), so this doesn't pull Teams forward into MVP3 — it's
a confirmation of priority *within* MVP4's own already-open scope, not
a re-sequencing.

Deliberately not scoped further yet — genuinely can't be, since it's
downstream of MVP3's multiplayer half actually landing, and there's no value
in pre-ordering work against a foundation that doesn't exist. What's already
designed and ready to pick up once MVP3 clears:

- **Team** (cap 6, Owner/Officer/Member, shared physical access, federated
  Village-Flag territory) and **Guild** (cap 64, player-founded via Guild
  Sign, Owner/Officer/Member, Guild Bank as a pure Perk-funding sink,
  revocable specialty recipe/wish grants, 15m Sign/Marker territory,
  disband/succession lifecycle) — both fully designed in
  `TEAMS_AND_GUILDS_PLANNING.md`, nothing built.
- **Team Vendor / Guild Vendor** — the two `VendorStall` drivers deferred out
  of MVP3 above.
- General player-to-player trade (atomic two-sided window, reset-on-offer-
  change anti-scam behavior) — also blocked on the same player-identity
  prerequisite as MVP3's Phase 2.
- Map presentation for both (distinct player-marker shape, Team-color-over-
  Guild-color priority, Guild Sign/Marker visibility toggle, a corner
  legend) — designed, not built.
- **"Whatever else to add at that point"** — Ben's own framing; deliberately
  left open rather than guessed at now.

## Cross-references

- `MULTIPLAYER_PLANNING.md` — the phased roadmap MVP3's multiplayer half is
  scoped against; this doc names a finish line (through player-authoritative
  gameplay) rather than treating the whole roadmap as one MVP tier.
- `COMMERCE_PLANNING.md` — full driver design and build order for MVP3's
  Commerce half.
- `TEAMS_AND_GUILDS_PLANNING.md` — full design for MVP4, not buildable before
  MVP3's multiplayer prerequisite lands.
- `MVP2_PLANNING.md` — the prior MVP tier this one follows; same "curated,
  live planning surface" convention, not a replacement for
  `BUGS_AND_ENHANCEMENTS.md`'s broader backlog.
