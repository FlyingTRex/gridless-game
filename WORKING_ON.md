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

Nothing in progress right now — everything through v0.3.189-dev is merged
to origin/main. **Multiplayer Phase 3 is fully complete** — all 5
sub-phases (Bootstrap; Inventory + Equipment; Crafting + Building; Magic
+ Combat; everything else) shipped and live-confirmed. See
MULTIPLAYER_PLANNING.md for the full list of what shipped across the
whole phase. Sub-phase 5's last pieces: PlayerVitals converted to
NetworkBehaviour with an isServer guard on its passive-drain Update()
loop (no Command needed - every mutating method already runs server-side
via an existing Command or at load); "admin tools" dropped from scope
(AdminSpawnScreen is #if UNITY_EDITOR-only, never ships in a real build,
so no multiplayer-correctness concern exists there), same correction as
"skill/attribute point spending" earlier. Live-confirmed: played
normally, hunger/thirst ticked down correctly, zero errors.

Next up, per MULTIPLAYER_PLANNING.md's own roadmap: NPCs move
server-side (the 5 Update()-driven NPC scripts stop running client-side
entirely), then the persistence restructure for a real dedicated server,
then the design-brief's remaining Phase 2/3 items. None of these are
started yet.

One real UI bug found live and logged to BUGS_AND_ENHANCEMENTS.md rather
than chased same-session: a stuck empty hold-progress bar after casting
Heal Self, cause not yet confirmed. See `CHANGELOG.md`'s v0.3.189-dev
entry and `MULTIPLAYER_PLANNING.md` section 3's Phase-3-complete note
for full detail — pick up NPCs-server-side next time rather than
re-deriving the state.

Reminder for whoever picks this back up: the overall Multiplayer
conversion was always scoped as multi-week (48 PlayerXXX.cs scripts
across Inventory/Equipment, Crafting/Building, Magic/Combat, everything
else, then NPCs server-side, then a persistence restructure) - don't
treat "not finished yet" as behind schedule.
