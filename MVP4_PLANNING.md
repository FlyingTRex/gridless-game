# MVP 4 Planning

Decided conversationally with Ben (2026-08-25), the same night as a long side
session building a rigged SL clothing pipeline (see
`D:\Ben\SL\LESSONS_LEARNED.md`, outside this repo). `MVP3_PLANNING.md` had
already flagged "Teams specifically should be in scope for MVP4 'at least'"
earlier that same day, while per-connection spawning was being built — this
doc is that tier finally getting its own curated planning surface, plus a
second half Ben added once the SL session's clothing pipeline proved out:
**MVP4 = Team + a custom player model/clothing pipeline, running
concurrently, not sequenced against each other** (they share no real
dependency — see below).

## Status check before committing scope (verify, don't assume)

`TEAMS_AND_GUILDS_PLANNING.md` names two hard prerequisites: multiplayer's
player-authoritative-gameplay conversion, and a real player identity/naming
system. Checked directly against the actual code before writing this
(**corrected once already** — the first pass trusted the planning docs'
framing that player naming was "still unbuilt" and was wrong; Ben caught it
live via the in-game Player tab's existing "Set Name (free)" control):

- **Player-authoritative gameplay is DONE.** `MULTIPLAYER_PLANNING.md`
  states outright: "Phase 3 is fully complete as of 2026-08-23 — all 5
  sub-phases above [...] is now fully complete."
- **Player identity/naming is ALSO already fully built** —
  `PlayerIdentity.cs` (a real `NetworkBehaviour`, built 2026-08-22/23) has
  `DisplayName`, a stable `PlayerId`, `TryRename` (free first rename, then
  costs Gold + a Fame penalty on later renames, exactly the design detail
  `MULTIPLAYER_PLANNING.md` called for), sanitization, and save/restore
  hooks. `PlayerMenuScreen.cs` is the entry point (the Player tab's "Set
  Name" button). This was a real doc-vs-code staleness gap, not a design
  gap — `TEAMS_AND_GUILDS_PLANNING.md`/`MULTIPLAYER_PLANNING.md` simply
  hadn't been updated after this shipped.

**Net effect: Team has NO remaining blocker.** Both stated prerequisites
are done; `TEAMS_AND_GUILDS_PLANNING.md`'s own design is ready to implement
directly, today.

## Half 1 — Team

Scope is **Team only**, not Guild — Ben's own framing tonight was specific:
"implement teams, that would allow traskmi and I to work collaboratively."
Guild (cap 64, player-founded, Guild Bank, revocable specialty grants) stays
fully designed in `TEAMS_AND_GUILDS_PLANNING.md` and available to pull into
a later tier, but isn't committed to this one — same "leave room, don't
guess ahead" discipline as every other MVP scoping pass in this project.

**Full implementation-level scope now resolved, 2026-08-26** — see
`TEAMS_AND_GUILDS_PLANNING.md`'s "Team — resolved for MVP4" section for the
complete, confirmed detail. Short version: a codebase survey found there's
no ownership/permission plumbing anywhere in the game today (`StorageBox`'s
single crude flag is the closest thing that exists; `Furnace` has none at
all), so real per-object access control was deliberately dropped from this
tier's scope — Team is roster + cosmetic territory + vendor-split only,
matching what Ben actually asked for (two trusted collaborators, not a
public-server security model). Team creation is a pure UI action (no
physical object), the Owner must hand off before leaving (no auto-
succession), and Officer permissions are invite + kick-Members-only.

Build order — no remaining prerequisite, both are already shipped
(`PlayerIdentity.cs`/`PlayerMenuScreen.cs` for naming, Multiplayer's Phase 3
for player-authoritative gameplay):

- [x] **Team**, per `TEAMS_AND_GUILDS_PLANNING.md`'s now-fully-resolved
  design: cap 6, Owner/Officer/Member roles, a new Team tab (create/invite/
  kick/promote/leave/disband), territory as the federated union of the
  Owner's and any Officer's own Village Flags (reusing
  `CraftTierScale.VillageFlagRevealRadius` as-is, live-recomputed off each
  member's current rank) drawn as a cosmetic map shape only — no shared
  Bank, no object-level access control of any kind. **Built 2026-08-28,
  v0.3.206-dev** — `PlayerTeam.cs` (roster/lifecycle Commands) +
  `TeamScreen.cs` (the `T`-key UI), wired into the Player prefab/scene.
  Confirmed working live the same night ("teams work" — Ben). One real
  live-play bug found and fixed the same session (v0.3.207-dev): a
  renamed player showed as "Traveler" in the roster, since
  `PlayerIdentity.playerName` wasn't a `[SyncVar]` and rename never went
  through a `[Command]` — see `CHANGELOG.md`'s v0.3.207-dev entry.
- [ ] **Team-mate map markers** — distinct shape from NPC markers, per the
  same doc's Map presentation section. `MapScreen.cs` currently draws Flag
  markers only (checked directly — no `PlayerIdentity` reference in it at
  all), so this marker-drawing itself is genuinely still to build; the
  naming half it labels off of (`PlayerIdentity.DisplayName`) is the part
  that's already done. Not started.
- [ ] **Crosshair name display** — Ben's ask (2026-08-25): show a player's
  name when your crosshair is aimed at them, not just on the map.
  `PlayerInteraction.cs` already has exactly this shape for world objects
  — `RaycastCrosshair` + `IInteractable.Prompt`, drawn as text below the
  crosshair in `DrawCrosshair()`. A remote player either implementing
  `IInteractable` (returning `PlayerIdentity.DisplayName` as its `Prompt`,
  `IsInstant` presumably irrelevant since there's nothing to hold-interact
  with) or a lighter parallel raycast check reuses this existing prompt
  rendering directly — not a new UI element. Not started.
- Team Vendor (the `VendorStall` driver `MVP3_PLANNING.md` deferred out of
  its own Commerce build) is a natural next step now that Team exists and
  has a real second player (traskmi) to test against, but still not
  committed to this tier's initial scope.

**Real-money side effect of Team shipping, found live 2026-08-27/28, not
part of the original design:** the same live two-machine session that
confirmed Team working also surfaced a much broader class of bug —
several `PlayerXXX.cs` components hold real per-player state (inventory,
skill levels, Magic lineage, worn equipment, ...) that server-authoritative
logic updates correctly but never actually syncs back to the owning
client's own copy. 11 instances found and fixed same night (v0.3.207-dev,
full detail in `CHANGELOG.md`/`BUGS_AND_ENHANCEMENTS.md`) — not part of
Team's own scope, but directly surfaced by Team's live two-player test
being the first real sustained multi-hour two-machine session this
project has had. `PlayerCurrency` (not networked at all, all 16 mutation
sites un-Command-routed) and a bigger open finding (most direct gameplay
actions, e.g. wish-casting, never reach the server for a real remote
client at all) are both still open, logged in `BUGS_AND_ENHANCEMENTS.md`.

## Half 2 — Custom player model + clothing pipeline

`CUSTOM_AVATAR_PLANNING.md` already exists (2026-08-20) but is planning-only
— nothing modeled or rigged yet. Its own locked-in decision: the human
replacement is **mesh-only** (new body matching the existing Kevin Iglesias
rig's exact bone names/bind pose, so the current Animator Controller and
every existing animation clip keep working unmodified — no new rig, no new
animations for humans).

Tonight's SL session (`D:\Ben\SL\LESSONS_LEARNED.md`) proves out the
*clothing* half of that plan concretely — a real, working, portable Blender
pipeline, not just a technique in the abstract:

1. Duplicate a region of the body mesh, trim to shape with
   `bmesh.ops.bisect_plane` (clean edges regardless of underlying topology
   — a raw vertex-range delete leaves a visible jagged edge instead).
2. Push outward along each vertex's own normal for clearance, Solidify for
   fabric thickness.
3. Vertex-group weights are inherited automatically at duplication time —
   no separate reskinning/Data Transfer step needed for anything built this
   way.

This is not SL-specific — the only SL-specific parts of that session were
the Collada export step and the Linden Skeleton's bone names. Pointed at
Gridless's own custom body mesh instead (once it exists, with real vertex
groups matching the current Animator rig), the exact same script pattern
builds correctly-fitted clothing/armor with **zero licensing exposure**,
since nothing derives from the Kevin Iglesias pack — the actual reason this
whole tangent got started ("our goal is to replace kevin's model
eventually").

Build order:

- [ ] **The custom human body mesh itself** — `CUSTOM_AVATAR_PLANNING.md`'s
  own scope, not yet started. Real open questions that doc already flagged
  and still hasn't resolved: whether Chicken is in scope now, whether
  animals need wearable equipment, how the human vs. animal tracks should
  be sequenced against each other. Worth a fresh look now that there's a
  concrete reason (clothing) to want the human body specifically, sooner.
- [ ] **First replacement clothing item**, built with tonight's proven
  pipeline, once the body mesh exists — smallest reasonable test (a shirt
  or shorts, matching tonight's SL proof-of-concept) before committing to
  a full wardrobe.
- [ ] **Icon baking / `ItemDatabase` wiring** for whatever clothing gets
  built, via the existing `IconBaker.cs` tool — same convention every other
  wearable in this project already follows.

This half has no real dependency on Half 1 (Team) — they can run
concurrently. It also has no dependency on Multiplayer at all; the custom
avatar body and its clothing are exactly as buildable single-player today
as the Kevin Iglesias-based pipeline already is.

## Cross-references

- `MULTIPLAYER_PLANNING.md` — confirms player-authoritative gameplay (one
  of Team's two stated blockers) is complete as of 2026-08-23.
- `TEAMS_AND_GUILDS_PLANNING.md` — full Team (and Guild, not in this tier's
  scope) design; nothing here re-decides any of it, just sequences the one
  remaining prerequisite (player naming) in front of it.
- `CUSTOM_AVATAR_PLANNING.md` — the body-mesh half of Half 2's scope,
  planning only as of 2026-08-20, real open questions not yet resolved.
- `D:\Ben\SL\LESSONS_LEARNED.md` (outside this repo) — the proven clothing
  pipeline Half 2 points at the custom body mesh once it exists.
- `MVP3_PLANNING.md` — the prior tier; its own "MVP4" section is now
  superseded by this doc, same "curated planning surface" convention as
  every prior MVP tier transition in this project.
