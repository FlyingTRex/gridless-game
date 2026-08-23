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

Nothing in progress right now — everything through v0.3.190-dev is merged
to origin/main. **Multiplayer Phase 3 is fully complete** — all 5
sub-phases (Bootstrap; Inventory + Equipment; Crafting + Building; Magic
+ Combat; everything else) shipped and live-confirmed. See
MULTIPLAYER_PLANNING.md for the full list of what shipped across the
whole phase.

**NPCs move server-side (the next phase) has started, first slice
done.** NPCGathering/NPCCrafting/NPCGuarding/NPCSeekFlag/NPCTraining
(the 5 job-driven NPC scripts) are all NetworkBehaviours with isServer
guards on their own Update() loops. The 3 NPCFactoryWorker* prefabs got
a real NetworkTransformReliable for position replication - caught and
fixed a real wrong-default along the way (this Mirror version's own
default syncDirection for a fresh NetworkTransformReliable is
ClientToServer, which would silently sync nothing for a server-driven
NPC; set explicitly to ServerToClient). Live-confirmed: hired NPCs
moving around normally, zero errors. Roaming wildlife (Wolf/Rabbit/Pig/
Deer/Chicken, NPCWander/NPCFlee) deliberately excluded from this slice -
not part of the named "5," open question for later whether it needs the
same treatment. Also not yet audited: whether any NPC-initiated
gameplay interaction breaks under the new server-only-simulation model.

Still to do after that: the persistence restructure for a real
dedicated server, then the design-brief's remaining Phase 2/3 items -
neither started yet.

One real UI bug found live and logged to BUGS_AND_ENHANCEMENTS.md rather
than chased same-session: a stuck empty hold-progress bar after casting
Heal Self, cause not yet confirmed. See `CHANGELOG.md`'s v0.3.190-dev
entry and `MULTIPLAYER_PLANNING.md` section 3 item 4 for full detail —
pick up there next time rather than re-deriving the state.

Reminder for whoever picks this back up: the overall Multiplayer
conversion was always scoped as multi-week (48 PlayerXXX.cs scripts
across Inventory/Equipment, Crafting/Building, Magic/Combat, everything
else, then NPCs server-side, then a persistence restructure) - don't
treat "not finished yet" as behind schedule.
