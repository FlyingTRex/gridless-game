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

Nothing in progress right now — everything through v0.3.175-dev is merged
to origin/main. Multiplayer Phase 3 sub-phase 1 (Bootstrap) is fully done.
Sub-phase 2 (Inventory + Equipment): both major rollout pieces (world
pickups, all equippables - 127 prefabs total) are DONE IN FULL, five
working Commands, real SyncList sync. UI wiring is DONE for every real
case InventoryScreen.cs supports from the main inventory (Unequip,
drag-to-slot Equip, single- and multi-destination click-equip) AND for
moving items into/out of a worn Backpack's nested inventory
(RequestMove's "worn:<slot>" container key + InventoryScreen's
ContainerKeyFor), all live-confirmed. Two real bugs found and fixed along
the way, neither caused by the networking work itself: a spawn-gap
(NetworkSpawnHelper.SpawnIfNetworked) and a dual-wield display bug
(IsCurrentlyWorn used to only detect ONE worn instance per type - see
CLAUDE.md's new gotcha, also flags Canteen/NavComputer/HealthMonitor need
the same test later). Explicitly still not done, and arguably belongs to
later phases rather than sub-phase 2 itself: Furnace zones and NPC cargo
(neither is Player state - Furnace isn't a NetworkBehaviour yet, NPCs are
a later phase), and the broader mutation surface outside
Inventory/Equipment (crafting, NPC deposit, admin tools) - still
local-only. See `CHANGELOG.md`'s v0.3.175-dev entry and
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2 for full detail —
pick up there next time rather than re-deriving the state.
