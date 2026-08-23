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

Nothing in progress right now — everything through v0.3.170-dev is merged
to origin/main. Multiplayer Phase 3 sub-phase 1 (Bootstrap) is fully done.
Sub-phase 2 (Inventory + Equipment) has made major progress: real SyncList
sync on PlayerInventory/PlayerEquipment, four working Commands (add-item,
move-a-plain-item, equip/unequip-a-real-instance, complete-a-world-pickup),
and world-pickup networking is DONE IN FULL (not just a pilot) - 78 of 127
worldPickupPrefab prefabs converted, Pickup.Complete() routes through a
real server-authoritative Command. Two real bugs found and fixed live
during that rollout (non-prefab scene pickups, scene-resave-for-sceneId
gotcha) - see MULTIPLAYER_PLANNING.md for both, worth knowing before doing
the equivalent for equippables. Explicitly deferred, remaining work:
giving every other equippable prefab (~10 types, Backpack alone has 10
tier/material variants) the same NetworkIdentity treatment as the one
Backpack pilot, and wiring any of these Commands into the real
InventoryScreen.cs drag-and-drop UI players actually touch. See
`CHANGELOG.md`'s v0.3.170-dev entry and `MULTIPLAYER_PLANNING.md` section
3 item 3 sub-phase 2 for full detail — pick up there next time rather than
re-deriving the state.
