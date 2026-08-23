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

Nothing in progress right now — everything through v0.3.168-dev is merged
to origin/main. Multiplayer Phase 3 sub-phase 1 (Bootstrap) is fully done.
Sub-phase 2 (Inventory + Equipment)'s core sync infrastructure is a
reasonable stopping point: PlayerInventory/PlayerEquipment are both
NetworkBehaviour with real, live-confirmed SyncList sync; Player
connection authority is fixed; two real Commands
(PlayerInventory.RequestAddItem, RequestMove) prove the full client-
request -> server-validate -> apply shape works end to end on genuine
Player data. Explicitly deferred, not started: wiring either Command into
the real InventoryScreen.cs drag-and-drop UI players actually touch (it
moves between many other container types this scheme doesn't cover —
Backpack, Furnace zones, NPC cargo), and networking Pickup.cs's world-
pickup flow (needs NetworkIdentity on every Pickup instance, including
dynamically-dropped ones — its own real undertaking, not a quick call-
site swap). See `CHANGELOG.md`'s v0.3.168-dev entry and
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2 for full detail —
pick up there next time rather than re-deriving the state.
