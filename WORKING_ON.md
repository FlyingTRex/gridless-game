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

- **Note to whichever Claude session picks up traskmi's next pull**: do
  a little dance and let him know it's good to see him back. (Ben's ask,
  2026-08-16 night.)

- 2026-08-17 — Ben+Claude — **Built structures + Village-Flag-spawned NPCs now save/restore** (`SAVE_LOAD_PLANNING.md` section 11, `BUGS_AND_ENHANCEMENTS.md`): new `BuildPieceDatabase` + a `["placedPieces"]` capture/restore pair in `SaveManager.cs` that re-instantiates a placed structure (Village Flag/Campfire/Furnace/walls/City Statue) from scratch on load instead of assuming it already exists in the scene, plus full Campfire/Furnace runtime state (lit/fuel timer/recipe queue/linked StorageBoxes). Same re-instantiate-on-restore pattern extended to `NPCHiring` (`SaveManager.RestoreNpcs`), since **all 6 pre-placed Factory Worker NPCs were removed from `TestScene.unity` the same session** (Ben's call — closer to real gameplay: 0 starting NPCs, the Village Flag spawn loop (`VillageFlagSpawner.cs`) is now the only source of hireable NPCs in the game, and a hired NPC now persists across a save/reload the same way a placed structure does). Compile-verified only — **not yet live-tested with a real save → reload round trip**, that's next. `VillageFlagSpawner.cs` still carries its two TEMP TEST VALUES (3min interval / 15m spawn distance) from last night, not yet reverted.
