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

Nothing in progress right now — everything through v0.3.177-dev is merged
to origin/main. Multiplayer Phase 3 sub-phases 1 (Bootstrap) and 2
(Inventory + Equipment) are fully done - see MULTIPLAYER_PLANNING.md for
the full list of what shipped. Sub-phase 3 (Crafting + Building): the
Crafting half has a real working Command now.
RequestStartCraft/CmdStartCraft reuses StartCraft entirely unchanged
server-side, resolving the CraftingRecipe asset by name against this
player's own recipes array (validates availability for free).
Update()'s batch-progression gained an isServer guard. Output/ingredient
sync needed NO new code - rides entirely on PlayerInventory.syncedSlots
from sub-phase 2. Live-confirmed with a real multi-item batch through
the actual Craft button. One known deferred gap: crafting progress
display isn't synced to a remote client yet (invisible in solo testing).
Building (the other half of this sub-phase) hasn't been started at all.
See `CHANGELOG.md`'s v0.3.177-dev entry and `MULTIPLAYER_PLANNING.md`
section 3 item 3 sub-phase 3 for full detail — pick up there next time
rather than re-deriving the state.

Reminder for whoever picks this back up: the overall Multiplayer
conversion was always scoped as multi-week (48 PlayerXXX.cs scripts
across Inventory/Equipment, Crafting/Building, Magic/Combat, everything
else, then NPCs server-side, then a persistence restructure) - don't
treat "not finished yet" as behind schedule.
