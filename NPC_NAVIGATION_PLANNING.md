# NPC Navigation — a real system, not another patch

Planning only, 2026-08-21. Prompted by a live-testing failure: NPCs
assigned to deposit into a StorageBox inside a walled building got stuck
against a wall, hard-teleported clear (tonight's own stuck-bump fix), then
walked straight back into the same wall and repeated — an infinite
ping-pong. Ben's own framing, correctly refusing a third patch on top of
two already stacked tonight: **"we're patching the patches... let's think
of a new solution that addresses all items — detecting things in the
world, avoiding them instead of colliding, and be able to detect and open
doors."**

## Be-mean pass — why patching further is the wrong move

**The straight-line-plus-local-deflection model has a real structural
ceiling, and tonight is the first time it got hit for real.**
`NPCMovement.FindClearDirection` (every job-driven NPC mover) was designed
for open-terrain obstacles — a Boulder or Tree an NPC can route around
within a ~15-90° search cone at a few meters' range. A wall with a door
several meters to the side is completely outside what that search can ever
discover; the NPC isn't "having trouble," it is structurally incapable of
finding the route. No amount of tuning the deflection angle or bump
distance fixes that — it's the wrong tool for the problem, and tonight's
own stuck-bump/raycast-validation fixes, while individually correct fixes
for what they targeted, are stacked on top of that same wrong tool.

**A target-cooldown stopgap (the option raised and declined) would make
this actively worse, not better, in one specific way**: it trades a
visible, obviously-broken symptom (ping-ponging) for an invisible one (the
NPC goes quietly idle near the wall forever, having silently given up on a
task the player assigned and has no way to know failed). Visible breakage
is more honest than silent breakage.

**This is going to keep recurring, not stay a one-off.** Ben is already
building multi-room walled structures live tonight, and the project's own
direction (City Statue, growing settlements) points toward more complex
layouts, not less. Every one of the 5 job-driven movers
(`NPCGathering`/`NPCCrafting`/`NPCGuarding`/`NPCSeekFlag`/`NPCTraining`)
walks to a fixed target that could plausibly end up inside or behind a
player-built structure. Roaming wildlife (`HostileCreature`/`PreyWander`)
doesn't hit this — no fixed distant target — so the real scope is those 5
movers specifically, not every moving thing in the game.

**Real risk of building a full replacement blind**: this project has zero
NavMesh infrastructure today (checked `Packages/manifest.json` directly —
`com.unity.ai.navigation` isn't installed; the only `NavMesh.asset` files
in the repo belong to unrelated Mirror demo scenes). A conversion touching
5 core movers is genuinely foundational, and Claude sessions can't run
Play mode to verify between steps. This needs a phased build order with a
real checkpoint after the first phase, not one giant untested change —
same discipline `MULTIPLAYER_PLANNING.md` already established with its own
Phase 0 spike.

## Recommended approach: Unity NavMesh + NavMeshObstacle carving for doors

Unity's built-in navigation system (via the `com.unity.ai.navigation`
package) is the standard, well-supported answer to "detect the world,
avoid obstacles, route around them" — not something to hand-roll. Three
real pieces, each solving a distinct part of Ben's ask:

1. **Routing/avoidance**: `NavMeshSurface` baked over the terrain +
   `NavMeshAgent` on each of the 5 job-driven movers, replacing their
   current `MoveToward`/`FindClearDirection` calls with
   `agent.SetDestination(target)`. This is genuinely "detect things in the
   world and avoid them" for free — NavMeshAgent's own local-avoidance
   system supersedes the hand-rolled widening-angle deflection entirely
   for these 5 movers.
2. **Dynamic re-baking**: since walls/doors are placed and destroyed live
   by players, the navmesh needs re-baking on construction changes —
   Unity supports this natively (`NavMeshSurface.BuildNavMesh()` /
   `UpdateNavMesh()`), triggered from `PlayerBuilding.Confirm()` and
   `PlayerPieceUpgrade.DestroyPiece()`/`Upgrade()`. Real perf
   consideration flagged, not yet solved: baking should be scoped to a
   local region around the change, not the whole 200×200 terrain every
   time — `NavMeshSurface` supports bounds-limited baking for exactly
   this.
3. **Doors**: `NavMeshObstacle` with `carving = true` on the `Door` leaf is
   the Unity-native way to make a closed door block pathing and an open
   one not — carving updates the navmesh hole automatically as the door
   swings, no manual re-bake needed for open/close specifically. What
   NavMesh does *not* do on its own: make an NPC *want* to open a closed
   door blocking its route. That still needs real new logic — detect a
   nearby closed `Door` obstructing the agent's path and trigger it open,
   which needs the same player-only-today gap closed
   (`Door.Open(Vector3)` takes the player's own position directly; an
   `OpenForNPC()`-shaped generalization is a small, well-scoped addition
   on top of the NavMesh foundation, not the whole solution — NavMesh
   handles routing generically, this only handles the door-specific
   interaction).

**A real, non-obvious upside worth naming**: `NavMeshAgent`-driven NPC
movement is also the standard pattern for server-authoritative NPC
movement in Mirror-based multiplayer (agent runs server-side, position
synced to clients via `NetworkTransform`) — this doesn't compete with
`MULTIPLAYER_PLANNING.md`'s eventual direction, it's a good fit for it.

**What becomes obsolete vs. what stays**: `NPCMovement.FindClearDirection`
and tonight's `StuckTracker` hard-bump/raycast-validation fixes become
unnecessary for the 5 converted movers once they're NavMeshAgent-driven —
not wasted work (they fixed real problems for the interim, and this
conversion wasn't going to happen blind tonight regardless), just scoped
down. `HostileCreature`/`PreyWander`/`NPCWander` don't need converting —
they have no fixed distant target, open-terrain wandering doesn't hit this
ceiling — so `NPCMovement.cs` stays alive for those.

## Build vs. buy — Asset Store evaluation (2026-08-21, Ben's ask)

Checked real alternatives before committing to build-in-house, same
discipline as `WEATHER_MAKER_PLANNING.md`'s own build-vs-buy pass.

**A* Pathfinding Project (Pro, $140 one-time)** — the clear standout,
industry-standard third-party option, 500+ reviews, 5-star. Real
advantages over Unity's built-in system for *this specific project's*
use case:
- **Navmesh cutting** — designed specifically for frequent incremental
  graph updates without a full rebake, which matters a lot for a game
  where players are constantly building/destroying walls. This is a
  real, meaningful edge over hand-rolling bounds-limited rebakes on top
  of Unity's own system.
- Built-in **RVO local avoidance** (proper crowd simulation, "hundreds of
  agents at once") — more sophisticated than `NavMeshAgent`'s default
  avoidance, useful if NPCs ever congregate densely (a City-scale
  settlement, a Guild hall).
- Free version exists too, but automatic navmesh generation (the
  Recast-graph auto-bake-from-geometry feature) is **Pro-only** — the
  free tier's Grid/Point graphs could still work but represent obstacles
  more crudely, a real capability gap for this project's actual need.
- Real cost: $140 one-time, a genuine new third-party dependency
  (same category of decision as Mirror/Weather Maker, but this one costs
  real money up front rather than being free-to-import).

**Unity's own built-in NavMesh (`com.unity.ai.navigation`, free)** — one
claim found in initial research ("baked in advance, can't change at
runtime") is **outdated/inaccurate and worth correcting**: `NavMeshSurface
.BuildNavMesh()`/`UpdateNavMesh()` genuinely do support runtime rebaking,
and `NavMeshObstacle` with `carving = true` genuinely does handle moving/
dynamic obstacles (doors) natively — this is a well-established, common
pattern in other Unity base-building games, not a naive workaround.
Real, honest downside vs. A*: a Recast-based full/regional rebake is
plausibly more expensive per-update than A*'s purpose-built incremental
graph cutting, if the project ends up with frequent, large-scale
construction changes (a growing City). Not yet known whether that
actually matters at this project's real scale — no data yet either way.

**Recommendation: start with the free built-in system, not a $140 spend
up front.** The proposed Phase 0 spike (bake once, convert one mover,
prove the concept) costs nothing to try either way, and the scoped
bounds-limited-rebake approach already in this plan is a real, reasonable
mitigation for the update-frequency concern. If live testing at real
settlement scale shows rebake cost or crowd-avoidance quality is
genuinely a problem, A* Pathfinding Pro is a well-understood, known-good
upgrade path with a real track record — but paying for it *before*
confirming the free path can't handle this project's actual scale isn't
justified yet. Same "prove it's needed before spending" discipline this
project already applies elsewhere.

## Proposed phased build order (not committed, needs Ben's sign-off)

1. **Phase 0 — infra spike, isolated. Built 2026-08-21, not yet
   live-tested.** `com.unity.ai.navigation` (2.0.7) added and resolved
   cleanly. `NavMeshSurface` baked over `TestScene.unity`'s terrain —
   verified with real sample-position checks (`NavMesh.SamplePosition`
   at 4 points including right at the test building), not just "no
   exception thrown"; 4/4 found real walkable navmesh. `NPCGathering`
   converted to use a `NavMeshAgent` as a pathfinding *oracle* only —
   `updatePosition`/`updateRotation` both off, so the agent never writes
   to `transform` itself; `MoveToward` reads `agent.desiredVelocity` for
   its direction and still owns `transform.position` exactly as before,
   meaning harvest-lock/`GroundHeight` sampling/`wander.FaceToward`/
   `NPCWander`'s own idle movement all keep working completely
   unchanged — deliberately the safest possible integration, chosen to
   avoid the agent fighting any of those systems for transform control.
   Falls back to the old `NPCMovement.FindClearDirection` system
   automatically if no agent is present or it's off the baked mesh, so
   this is additive/backward-compatible, not a hard cutover.
   `NavMeshAgent` added to `NPCFactoryWorker.prefab` (the only prefab
   using `NPCGathering`), sized to match its existing `CapsuleCollider`
   (radius 0.3, height 1.4 — smaller than the navmesh's bake-time
   default agent radius of 0.5, a conservative/safe fit). Compile-
   verified only. **Real live-test checkpoint still needed before Phase
   1** — this is the single riskiest unknown (does NavMeshAgent movement
   look/feel right next to this project's existing animation-driven
   `NPCAnimatorDriver`, and does an NPC actually walk around the test
   building's wall to reach its target now?).
2. **Phase 1 — dynamic re-baking. Built 2026-08-21 (after Phase 2 below,
   once Ben decided the ordering caveat wasn't worth leaving open for
   the incoming playtest), not yet live-tested.** New
   `NavMeshRebaker.RequestRebake()`/`RequestRebakeDelayed(MonoBehaviour)`
   — deliberately the simple version (Ben's own call): a full-surface
   rebake on every change, not a perf-optimized bounded-region one; worth
   revisiting if it hitches noticeably at real settlement scale, not
   before that's confirmed to matter. Hooked into `PlayerBuilding
   .Confirm()` (covers both a fresh build and a re-placed existing
   StorageBox — neither destroys anything, so an immediate rebake is
   correct) and `PlayerPieceUpgrade`'s `Upgrade()`/`DestroyPiece()`
   (both call `Destroy()` first, which only marks a GameObject for
   removal — it's still fully present, collider included, until the end
   of the current frame — so these use the delayed variant, a one-frame-
   deferred coroutine, or the rebake would still see the about-to-be-
   removed object's geometry). This closes the ordering caveat Phase 2
   below was built with — new construction should now be reflected in
   routing without needing a fresh Phase 0-style full rebake.
3. **Phase 2 — doors. Built 2026-08-21, ahead of Phase 1 per Ben's ask,
   not yet live-tested.** `NavMeshObstacle` (Box shape, sized to match
   each door leaf's real `BoxCollider`, `carving = true`,
   `carveOnlyStationary = false` since the leaf actively swings) added
   to both `PlankDoorPiece.prefab`/`TwigDoorPiece.prefab` — carving
   tracks the leaf's live transform, so the navmesh hole moves with it:
   closed blocks the doorway gap, open moves the hole into the wall
   next to it, no extra logic needed for that half. `Door.cs` gained
   `IsOpen` (public) and `OpenForNPC(Vector3)` — an NPC-safe
   generalization of the existing player-only `Open()`, same class of
   gap as skinning/StorageBox pickup earlier this session. `NPCGathering
   .CheckForBlockingDoor` (called from `MoveToward`, throttled to twice
   a second) raycasts toward the agent's current steering target; if a
   closed `Door` is in the way, it opens it. Live-test: the exact repro
   that started this — an NPC depositing into a box inside a walled room
   with a door. The "only a door already standing at the Phase 0 bake"
   caveat this was originally built with no longer applies now that
   Phase 1 (above) also landed the same session.
4. **Phase 3 — convert the remaining 4 movers. Built 2026-08-21, same
   session as the live-testing pass below that motivated it.**
   Live-testing Phase 0-2 surfaced real gaps beyond plain doors/walls
   (see below), and once those were fixed on `NPCGathering`, the same
   treatment was applied to `NPCCrafting`, `NPCGuarding`, `NPCTraining`,
   and `NPCSeekFlag` — all 4 gained a `NavMeshAgent` (routing direction
   only, `updatePosition`/`updateRotation` off, same as `NPCGathering`)
   and the physics-sweep safety net described below. `NPCGuarding`'s
   circular patrol movement is a separate, deliberately non-pathfinding
   system and was left untouched — only its chase-a-threat `MoveToward`
   was converted. Compile-verified only for the last 3; `NPCSeekFlag`'s
   conversion was prompted by, and is described in full alongside, a
   live repro ("Cora is stuck").

**Live-testing Phase 0-2 found three real gaps beyond "does an NPC route
around a wall/door," all fixed the same session:**

- **Wall geometry can be too thin for a static bake to reliably catch,
  even with `NavMeshObstacle` on doors.** Live-confirmed via screenshot:
  an NPC clipped straight through a wall corner. Root cause: Unity's
  navmesh voxelization/erosion can drop geometry thinner than roughly
  2x the voxel size, and a plank wall panel measured only ~0.14m thick.
  Fixed the same way doors already were — every wall panel prefab
  (`TwigWallPiece`/`TwigHalfWallPiece`/`TwigDoorFrameWallPiece`/
  `PlankWallPiece`/`PlankHalfWallPiece`/`PlankDoorFrameWallPiece`) got
  an explicit `NavMeshObstacle` sized to its real `BoxCollider`, instead
  of depending on thin-geometry static baking.
- **Obstacles alone aren't a complete guarantee — added a genuine
  physics safety net.** Even with correct obstacle geometry, a corner
  where two adjacent obstacles' carve regions meet can still report a
  technically-valid navmesh route that clips the corner (found live,
  same screenshot). `MoveToward` (all 5 movers now) sweeps a chest-
  height, agent-radius capsule against real colliders before committing
  each step, rejecting the step outright if it would cross anything —
  the same guarantee a `CharacterController` gives the player for free,
  which NPCs (raw `transform.position` writers) never had. Uses
  `Physics.SphereCastNonAlloc` with a shared static buffer, not
  `SphereCastAll` — the allocating version, combined with a dense
  cluster of colliders (a storage-box overflow scattering a pile of
  loose items nearby), is the likely cause of a real live multi-second
  freeze affecting every NPC at once (large per-frame GC allocations).
- **A safety net alone can freeze an NPC forever on a genuinely
  unreachable target.** Blocking a bad step is necessary but not
  sufficient — without a way to give up, an NPC just stands at the
  obstruction permanently instead of clipping through it. `NPCGathering`
  gained a distance-to-target progress watchdog (not a simple "was
  this frame blocked" counter, which a stuck-jittering-in-place NPC can
  dodge by making tiny, directionless successful steps): after 5
  seconds with no real progress, a harvest/search target gets a 30s
  "avoid" cooldown so `FindTarget` picks something else, and a blocked
  deposit-return backs off 10s before retrying. `NPCSeekFlag` reuses the
  same physics sweep but not the retarget logic (there's only ever one
  destination — the Flag), so instead its own bug (the despawn timer
  only ticking while inside the "already arrived or already moving"
  code path, so a stuck-before-arriving spawn never started its own
  countdown and could wait forever unhired) was fixed directly.

Also worth remembering: `SaveManager.Load()` didn't rebake the navmesh
at all after restoring placed pieces, so any wall built in an earlier
session and reloaded from a save was invisible to routing even after
Phase 0-2 shipped (confirmed live: smooth `PathComplete` walk-through,
no obstacle-avoidance anomaly, because the pathfinder genuinely didn't
know the wall existed). Fixed with a `NavMeshRebaker.RequestRebake()`
call at the end of `Load()`. Separately, that rebake was found to flood
the Console with `RuntimeNavMeshBuilder ... does not allow read access`
warnings for every non-readable decorative mesh in the scene (grass/
bush/groundcover, useGeometry was `RenderMeshes`) — likely the real
cause of the freeze above, not just the GC theory. Fixed by switching
`NavMeshSurface.useGeometry` to `PhysicsColliders`, which also means
the bake now reflects the same collider geometry gameplay already
collides against.
