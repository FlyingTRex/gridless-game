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

Nothing in progress right now — everything through v0.3.191-dev is merged
to origin/main. **Multiplayer Phase 3 is fully complete** — all 5
sub-phases (Bootstrap; Inventory + Equipment; Crafting + Building; Magic
+ Combat; everything else) shipped and live-confirmed. See
MULTIPLAYER_PLANNING.md for the full list of what shipped across the
whole phase.

**NPCs move server-side (the next phase) is well underway.**
NPCGathering/NPCCrafting/NPCGuarding/NPCSeekFlag/NPCTraining (the 5
job-driven NPC scripts) are all NetworkBehaviours with isServer guards.
Roaming wildlife also done, per Ben's explicit call: SkinnableCreature
(shared base for HostileCreature/PreyCreature) converted to
NetworkBehaviour in one move, HostileCreature.Update()/PreyWander
.Update() both isServer-guarded. All relevant prefabs (3
NPCFactoryWorker* variants, Wolf, Rabbit, Pig) got a real
NetworkTransformReliable for position replication - caught and fixed
the SAME real wrong-default twice: this Mirror version's own default
syncDirection for a fresh NetworkTransformReliable is ClientToServer,
which would silently sync nothing for a server-driven creature with no
owning client; set explicitly to ServerToClient both times. Deer/
Chicken checked and correctly left alone - no movement code exists for
them at all (pre-existing gap, unrelated to networking). Live-confirmed
both slices: hired NPCs and Wolf/Rabbit/Pig all moving around normally,
zero errors.

Still not audited: whether any NPC-initiated gameplay interaction (not
directly triggered by a player Command) breaks under the new
server-only-simulation model - worth a look before calling this phase
fully done. Still to do after that: the persistence restructure for a
real dedicated server, then the design-brief's remaining Phase 2/3
items - neither started yet.

One real UI bug found live and logged to BUGS_AND_ENHANCEMENTS.md rather
than chased same-session: a stuck empty hold-progress bar after casting
Heal Self, cause not yet confirmed. See `CHANGELOG.md`'s v0.3.191-dev
entry and `MULTIPLAYER_PLANNING.md` section 3 item 4 for full detail —
pick up there next time rather than re-deriving the state.

Reminder for whoever picks this back up: the overall Multiplayer
conversion was always scoped as multi-week (48 PlayerXXX.cs scripts
across Inventory/Equipment, Crafting/Building, Magic/Combat, everything
else, then NPCs server-side, then a persistence restructure) - don't
treat "not finished yet" as behind schedule.
