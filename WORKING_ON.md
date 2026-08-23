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

Nothing in progress right now — everything through v0.3.172-dev is merged
to origin/main. Multiplayer Phase 3 sub-phase 1 (Bootstrap) is fully done.
Sub-phase 2 (Inventory + Equipment): both major rollout pieces (world
pickups, all equippables - 127 prefabs total) are DONE IN FULL, five
working Commands, real SyncList sync. UI wiring has started: real
Unequip now routes through the network (InventoryScreen.UnequipDispatch),
live-confirmed, chosen first since it needs no "which source container"
disambiguation. A real spawn-gap bug was found and fixed along the way -
NetworkSpawnHelper.SpawnIfNetworked is now the shared, single place every
real Instantiate-an-item call site (Dropping, Crafting, save/load
restore, Skill Book writing) spawns a networked item correctly; missing
this on PlayerCrafting specifically caused a live "unspawned GameObject"
error when unequipping a crafted item. Explicitly still not done: Equip
wiring in InventoryScreen.cs (harder than Unequip - needs to know which
container the item is actually in before it's safe to route through the
Command), and the broader mutation surface outside Inventory/Equipment
(crafting, NPC deposit, admin tools) is still local-only. See
`CHANGELOG.md`'s v0.3.172-dev entry and `MULTIPLAYER_PLANNING.md` section
3 item 3 sub-phase 2 for full detail — pick up there next time rather
than re-deriving the state.
