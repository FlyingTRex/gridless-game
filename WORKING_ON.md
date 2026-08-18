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

- **Note to whichever Claude session picks up traskmi's next pull**: do
  a little dance and let him know it's good to see him back. (Ben's ask,
  2026-08-16 night.)

- 2026-08-18 — Ben+Claude — **Cooking playtest fixes** (`BUGS_AND_ENHANCEMENTS.md`,
  `CHANGELOG.md` v0.3.126-dev): a live Cooking session confirmed the skill/quality
  system end-to-end, and surfaced 4 gaps. 3 fixed same night: Fried Egg's missing
  `PlayerEating.edibles` registration, `CampfireScreen`'s missing QTY count label
  on iconed items (Ingredients/Output/Fuel), and a new opt-in `Auto-Run` toggle on
  Campfire (auto-relight + auto-repeat cooking, mirrors `Furnace.AutoRunEnabled`).
  4th (Campfire utensil slots not surviving save/reload) got a real investigation
  — capture confirmed correct via the actual save file, restore path still
  unexplained — temporary `[CampfireSaveDiagnostic]` logging added to
  `SaveManager.RestorePlacedPieces`, still needs a live save→reload with the
  Console open to read. **The other 3 fixes are now live-confirmed** (same
  night, immediately after): Fried Egg eatable, QTY labels correct, Auto-Run
  genuinely relights + auto-repeats. A follow-up audit of all 6 cooked-item
  Edibles found everything else already correctly wired.

  **v0.3.127-dev, same night**: Auto-Run toggle moved up near the top of the
  panel (was buried at the bottom). Also fixed a second, unrelated live-found
  bug while at it: Leather Backpack (all 5 tiers) was silently rejected as an
  NPC tool — the 4 jobs needing a Backpack (`MineOreJob`/`ChopWoodJob`/
  `ForageJob`/`MetalworkingJob`) only ever listed the original plain Backpack
  ladder's guids, never backfilled for the newer Leather Backpack ladder.
  Compile-verified only (Editor closed for this pass).

  **v0.3.128-dev, same night**: Guard patrol radius no longer reuses
  `CraftTierScale.VillageFlagRevealRadius` (was giving a Masterwork-Flag
  Guard a 75m patrol circle) — now a player-set leash on `NPCGuarding`
  (`PatrolRadius`), same shape as `NPCGathering.MaxRangeFromDeposit`, with
  a matching UI row in `NPCHiringScreen`. Also fixed the already-logged
  bug that neither leash was actually persisted through save/reload —
  both now round-trip via `SaveManager`.

  **Live-tested same night, immediately after** — the Campfire utensil
  persistence bug and the "ore not breaking" question both turned out to
  be non-issues (confirmed via the temporary diagnostic logs, now
  removed). But the new patrol leash exposed a real bug of its own: set
  to 2m, the Guard never got any closer to the Flag. **Fixed, v0.3.129-dev**:
  `NPCGuarding.UpdatePatrol()` now splits into an approach phase (walk
  straight at the nearest point on the circle) and an orbit phase (only
  once already within range) — the orbiting target's speed had worked
  out to exactly the Guard's own top speed regardless of radius, so a
  small radius made it uncatchable from outside. Not yet live-tested.

  **Also found live the same night**: the hire payment timer really was
  running a ~300s cycle, not 3600s. **Fixed, v0.3.130-dev**: a stale
  `workDurationSeconds: 300` override baked into `NPCFactoryWorker.prefab`
  (both spawned Male/Female variants inherit from it) from before the C#
  default was bumped to 3600 — classic stale-prefab-override gotcha, code
  was always right. **Confirmed live same night** — a fresh hire read
  "payment due in 3230s," in the right ballpark. This explains the
  short-timer half of the 2026-08-17 "298s→5s + name reverted" report;
  the name-reversion half is still open, split into its own
  `BUGS_AND_ENHANCEMENTS.md` entry.

  **Guard patrol still broken after the v0.3.129-dev fix — found live the
  same session.** Confirmed via the Player Map there's only one Flag on
  the whole terrain (rules out "stuck on a different Flag"), and the
  Guard sat still for 5+ real minutes with the player standing right at
  it. Two static re-reads of `NPCGuarding.cs` found nothing wrong.
  **v0.3.131-dev**: added temporary `[GuardDiagnostic]` logging
  (once/second) covering the full decision path.

  **Real answer, found live the same session, fixed v0.3.132-dev — this
  fully closes out the Guard patrol saga.** The diagnostic log showed the
  Guard correctly `Attacking` a Wolf, and a screenshot confirmed the Wolf
  was already dead — the Guard was never broken, it had actually won the
  fight. The real bug: nothing told `NPCGuarding` to let go once its
  target died. `ThreatStillValid()` only checked distance, and a killed
  creature's `GameObject` is never destroyed (just hidden, with a much-
  later respawn scheduled), so `currentThreat` stayed non-null forever —
  the Guard stayed locked in `Attacking`, unable to re-damage a corpse,
  never returning to patrol. Confirmed live: manually skinning the Wolf
  didn't unstick it either, since skinning only hides the object. Fixed
  by checking `IsDead` in both `ThreatStillValid()` and
  `FindNearestThreat()`. `[GuardDiagnostic]` logging removed now that
  it's served its purpose. **Live-confirmed the same night** — the Guard
  killed a Wolf, walked to the Flag, killed a second Wolf, then settled
  into real patrolling. This closes out the whole Guard patrol saga (3
  real bugs across v0.3.128/129/132-dev).

  **Also confirmed live the same session**: permanent NPC death
  (`NPCVitals`) fired correctly for the first time ever — the Guard was
  eventually overwhelmed by 3 Wolves and died permanently (`Destroy
  (gameObject)`, gone from Roster and Map both). Equipment loss on death
  is intentional (Ben confirmed, matches `NPCHiring.Fire()`'s existing
  convention) — no change needed. Also live-confirmed: the Leather
  Backpack NPC-tool fix and the Auto-Run/QTY-label Campfire fixes from
  earlier tonight all work correctly.

  **v0.3.133-dev, same night**: re-added temporary diagnostic logging
  (`[MinerStuckDiagnostic]`, `NPCGathering.cs`) for a related but
  distinct report — a Miner cycling between move/mining animations near
  a Boulder, not fully frozen like earlier stuck-Miner reports. Theory:
  `IsActingOnTarget` flips on distance crossing `harvestRange`, and an
  obstacle between the NPC and its target could make `MoveToward`'s
  deflection oscillate across that boundary without ever routing around
  it. Logs every move↔harvest transition plus throttled obstacle-hit
  detail. Compile-verified; needs a live session near a stuck Miner to
  read the output. Not yet committed.

- 2026-08-17 — Ben+Claude — **Built structures + Village-Flag-spawned NPCs now save/restore** (`SAVE_LOAD_PLANNING.md` section 11, `BUGS_AND_ENHANCEMENTS.md`): new `BuildPieceDatabase` + a `["placedPieces"]` capture/restore pair in `SaveManager.cs` that re-instantiates a placed structure (Village Flag/Campfire/Furnace/walls/City Statue) from scratch on load instead of assuming it already exists in the scene, plus full Campfire/Furnace runtime state (lit/fuel timer/recipe queue/linked StorageBoxes). Same re-instantiate-on-restore pattern extended to `NPCHiring` (`SaveManager.RestoreNpcs`), since **all 6 pre-placed Factory Worker NPCs were removed from `TestScene.unity` the same session** (Ben's call — closer to real gameplay: 0 starting NPCs, the Village Flag spawn loop (`VillageFlagSpawner.cs`) is now the only source of hireable NPCs in the game, and a hired NPC now persists across a save/reload the same way a placed structure does). Compile-verified only — **not yet live-tested with a real save → reload round trip**, that's next. `VillageFlagSpawner.cs` still carries its two TEMP TEST VALUES (3min interval / 15m spawn distance) from last night, not yet reverted.
