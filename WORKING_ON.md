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

Nothing in progress right now — everything through v0.3.187-dev is merged
to origin/main. Multiplayer Phase 3 sub-phases 1-4 (Bootstrap; Inventory
+ Equipment; Crafting + Building; Magic + Combat) are ALL fully done -
see MULTIPLAYER_PLANNING.md for the full list of what shipped. Sub-phase
5 (everything else) in progress: PlayerEating, PlayerMedicine,
PlayerCanteen, NPCHiringScreen, and NPCJobScreen are all
NetworkBehaviours with real Commands (RequestEatFrom/RequestApplyFrom -
container-key shape; RequestDrink/RequestFill - physical instance;
RequestHire/RequestFire/RequestPay and RequestAssignJob/RequestSwapTool
- Command runs server-side and calls methods directly on the
still-non-networked NPCHiring/NPCJob, no conversion needed there). All
live-confirmed: MRE eaten, Healing Paste applied, Canteen drunk past
overdrink + refilled, NPCs paid off, hired+assigned Guarding+gave a
Masterwork Knife - zero real errors. "Skill/attribute point spending"
dropped from the sub-phase 5 list - doesn't exist as a real system.
Real bug found and fixed same session: 11 total Instantiate call sites
project-wide were missing NetworkSpawnHelper.SpawnIfNetworked (not just
the one that surfaced as "unspawned GameObject" hiring an NPC) - fixed
all 11 (NPC spawn/restore, admin spawn, piece upgrade, drop-stack
scatter, all 5 starting-gear auto-equip prefabs, 4 gathered-resource
drops). spawnPrefabs now 166. Not yet started in sub-phase 5: NPC
deposit-container targeting, admin tools beyond the spawn-gap fix, and
whether PlayerVitals' passive drain needs to move server-side too. One
real UI bug found live and logged to BUGS_AND_ENHANCEMENTS.md rather
than chased same-session: a stuck empty hold-progress bar after casting
Heal Self, cause not yet confirmed. See `CHANGELOG.md`'s v0.3.187-dev
entry and `MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 5 for
full detail — pick up there next time rather than re-deriving the
state.

Reminder for whoever picks this back up: the overall Multiplayer
conversion was always scoped as multi-week (48 PlayerXXX.cs scripts
across Inventory/Equipment, Crafting/Building, Magic/Combat, everything
else, then NPCs server-side, then a persistence restructure) - don't
treat "not finished yet" as behind schedule.
