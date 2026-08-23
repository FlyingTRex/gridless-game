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

Nothing in progress right now — everything through v0.3.171-dev is merged
to origin/main. Multiplayer Phase 3 sub-phase 1 (Bootstrap) is fully done.
Sub-phase 2 (Inventory + Equipment) has both major rollout pieces DONE IN
FULL, not pilots: world-pickup networking (78 of 127 prefabs) and now
every equippable type too (remaining 49 prefabs, 127 total networked).
The equippable rollout used a real design improvement over the original
Backpack pilot: one generic RequestEquipInstance/RequestUnequipInstance
Command pair on PlayerInventory (mirroring InventoryScreen.cs's own
dispatch switch) instead of converting all 10 carrier scripts to
NetworkBehaviour individually. Real SyncList sync on
PlayerInventory/PlayerEquipment, five working Commands total (add-item,
move-a-plain-item, equip/unequip-any-instance, complete-a-world-pickup).
Real bugs found and fixed live along the way (non-prefab scene pickups,
scene-resave-for-sceneId gotcha) - see MULTIPLAYER_PLANNING.md. What's
explicitly still not done: wiring any of these Commands into the real
InventoryScreen.cs drag-and-drop UI players actually touch, and the
broader mutation surface outside Inventory/Equipment (crafting, NPC
deposit, admin tools) is still local-only - both smaller in scope than
what's already shipped. See `CHANGELOG.md`'s v0.3.171-dev entry and
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2 for full detail —
pick up there next time rather than re-deriving the state.
