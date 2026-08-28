# Working On

What's actively in progress right now, one line per active session. Check this
before starting new feature work — if something here overlaps what you're about
to build, coordinate before duplicating effort (see the Waterskin/Canteen
collision in `CHANGELOG.md`, 2026-08-02, for what happens when this doesn't get
checked).

Add a line when you start a non-trivial feature; remove it once merged to
`origin/main`. Stale entries are worse than none — if you're not sure whether an
entry is still active, ask before trusting it.

Note: "merged to origin/main" means the code is in — it doesn't require a live
Play-mode pass first. Manual test status for a shipped feature belongs in
`TEST_FEATURE_PLAN.md`, not here; don't keep an entry alive just to track that a
live test is still pending.

Format: `- YYYY-MM-DD — who — one-sentence description`

**Everything through v0.3.204-dev is merged to origin/main.**

## Multiplayer roadmap — mostly closed out, one real gap found 2026-08-26

`MULTIPLAYER_PLANNING.md`'s entire phased plan (Phase 3 player-
authoritative conversion, NPCs server-side, the persistence restructure,
and real per-connection spawning) is done and live-confirmed. Full
chunk-by-chunk detail lives in that doc and in `CHANGELOG.md`'s
2026-08-25 entries — summary:

- Persistence restructure (chunks 1-6): per-player save keying, a real
  `RequestSave` Command, autosave/disconnect/shutdown saves, and a
  Host/Join Connect screen — all live-confirmed.
- Real per-connection Player spawning: a second connection now gets a
  genuine second character instead of being refused. Surfaced (and
  fixed) that none of the 48 `PlayerXXX.cs` scripts gated local input
  by `isLocalPlayer` — invisible until a second Player object could
  ever exist.
- The first real live two-connection test (Editor host + standalone
  exe, localhost) found and fixed two rounds of the same bug live: a
  remote player's body, then their worn equipment, both rendering
  invisible due to `WornEquipmentLayer`'s camera-cullingMask exclusion
  being global-per-camera rather than "hide my own stuff." Both
  confirmed working from the host's side (the standalone exe itself
  hasn't been rebuilt with the fix — a known, non-urgent asymmetry, not
  a bug).
- NPC Talk/Freeze now route through real Commands, with `NPCDialogue
  .isTalking`/`NPCFreeze.IsFrozen` converted to this project's first
  `[SyncVar]`s so the fix actually displays correctly on the calling
  client, not just the server. Live-confirmed.

**Chunk 7 — the real live test with traskmi, from two separate physical
locations — actually ran, 2026-08-26.** Connecting itself took real
troubleshooting (a stray-text bug in the Connect screen's IP field, then
a literal comma-vs-period typo in the address — neither one a networking
problem at all, just worth remembering to check the exact error string
before chasing router/firewall/CGNAT theories). Once connected, the test
did exactly what it was for: **found a real gap Phase 3 had missed,
same night fixed** (v0.3.205-dev). Admin-spawning, dropping, and
renaming all ran as plain local method calls with no `[Command]`/
`[SyncVar]` — full root-cause detail and the fix that shipped are in
`CHANGELOG.md`'s v0.3.205-dev entry. Compile+weave verified via batch
mode only — **still needs a real live re-test with traskmi** to
confirm the client side now actually replicates correctly, the same
two-machine scenario that found the bug in the first place.

## Open backlog, not currently being worked

See `BUGS_AND_ENHANCEMENTS.md` for full detail on each. Highlights as
of 2026-08-25: `ReachableInventories`' Belt/Shirt/Jeans gap (4 systems),
Nail's icon rendering as a Hammer, an unconfirmed Nail/Copper Nail
stack-cap-at-10 report, a stuck hold-progress-bar bug from 2026-08-23
(still unreproduced), tool tiers giving no functional benefit within
their own class, and Piece Destroy's hold-X control never actually
having been wired up despite its own code comment claiming otherwise.
