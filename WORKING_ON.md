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

Nothing in progress right now — everything through v0.3.179-dev is merged
to origin/main. Multiplayer Phase 3 sub-phases 1 (Bootstrap) and 2
(Inventory + Equipment) are fully done - see MULTIPLAYER_PLANNING.md for
the full list of what shipped. Sub-phase 3 (Crafting + Building) is now
functionally complete for the core loop: Crafting has a real working
Command (RequestStartCraft/CmdStartCraft reuses StartCraft unchanged
server-side, output rides PlayerInventory.syncedSlots for free) and
Building has a real working Command (RequestConfirmPlacement/
CmdConfirmPlacement — the server re-derives the BuildSocket from the
placement position via FindNearbySocket instead of networking a live
reference; all 32 BuildPiece prefabs got NetworkIdentity, spawnPrefabs
127->158). Both live-confirmed: multi-item craft batch, plus free
placement and socket-snapped placement. One known deferred gap:
crafting progress display isn't synced to a remote client yet - logged,
not attempted. Next up in the multiplayer conversion: sub-phase 4
(Magic + Combat), not started. See `CHANGELOG.md`'s v0.3.179-dev entry
and `MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 3 for full
detail — pick up there next time rather than re-deriving the state.

Reminder for whoever picks this back up: the overall Multiplayer
conversion was always scoped as multi-week (48 PlayerXXX.cs scripts
across Inventory/Equipment, Crafting/Building, Magic/Combat, everything
else, then NPCs server-side, then a persistence restructure) - don't
treat "not finished yet" as behind schedule.
