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

Nothing in progress right now — everything through v0.3.181-dev is merged
to origin/main. Multiplayer Phase 3 sub-phases 1 (Bootstrap), 2
(Inventory + Equipment), and 3 (Crafting + Building) are fully done - see
MULTIPLAYER_PLANNING.md for the full list of what shipped. Sub-phase 4
(Magic + Combat): melee and ranged are both done. Melee: PlayerCombat is
a NetworkBehaviour with a real RequestPunch/CmdPunch Command; live-
confirmed punching a Wolf to death. Ranged: PlayerRangedCombat is a
NetworkBehaviour with a real RequestFireArrow/CmdFireArrow Command, same
client-resolves-aim/server-resolves-damage split as melee. Live testing
of Ranged found a real bug (not in the new Command) - Deer_001 and
Chicken_001 both live outside Assets/Prefabs/ (third-party ithappy
folder) so the earlier creature NetworkIdentity sweep missed them
entirely; fixed, and audited all 13 real creature instances in the scene
to rule out a third gap. spawnPrefabs now 164. Not yet retested live
against Deer with the fix in. Not yet started: PlayerMagic/wishes
(TryWish needs the same server-authority treatment as StartCraft, but
HandleWish lives in PlayerInteraction, a large central script not yet
touched by this conversion at all). See `CHANGELOG.md`'s v0.3.181-dev
entry and `MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 4 for
full detail — pick up there next time rather than re-deriving the state.

Reminder for whoever picks this back up: the overall Multiplayer
conversion was always scoped as multi-week (48 PlayerXXX.cs scripts
across Inventory/Equipment, Crafting/Building, Magic/Combat, everything
else, then NPCs server-side, then a persistence restructure) - don't
treat "not finished yet" as behind schedule.
