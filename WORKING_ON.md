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

**Everything through v0.3.195-dev is merged to origin/main.** Multiplayer
Phase 3 is fully complete (all 5 sub-phases). "NPCs move server-side" is
substantially done: the 5 job-driven NPC scripts, roaming wildlife (Wolf/
Rabbit/Pig), and a follow-up audit that found and fixed 4 more missed
Update() loops (NPCWander — the actual foundational idle movement,
NPCFlee, NPCVitals, NPCHiring's work timer) are all server-guarded and
live-confirmed. See MULTIPLAYER_PLANNING.md section 3 item 4 for the full
detail.

**2026-08-23 — uncommitted local work, not yet tested or pushed — pick
this up first next session:**
- `NPCHiringScreen.cs` — added `RequestTalk`/`RequestSetFrozen` Commands,
  closing the last flagged low-priority gap (Talk/Freeze were still
  calling `NPCDialogue`/`NPCFreeze` directly instead of through a
  Command). Compiles clean, **not yet live-tested** — verify Talk still
  opens dialogue and the Frozen checkbox still holds an NPC in place
  before committing.
- `PlayerInteraction.cs` / `PlayerRangedCombat.cs` — **temporary debug
  logging only, not meant to ship as-is.** Added to chase the stuck
  empty hold-progress-bar bug (`BUGS_AND_ENHANCEMENTS.md`) — Ben's
  latest report ties it to Bow/arrow usage specifically ("something I'm
  trying to do and then click the mouse"), seen a few times since the
  original Heal Self report. `PlayerRangedCombat` logs every `isDrawing`
  state transition; `PlayerInteraction.OnGUI` logs (throttled, once/sec)
  whenever the E-hold bar is actually drawing, printing which
  `IInteractable` and its progress/duration. Next session: reproduce
  with the bow (draw/fire, including odd timing like clicking right as
  something else happens), read the log for `[RangedDebug]`/
  `[HoldBarDebug]` lines, root-cause it, then **remove this logging**
  before committing the real fix — don't ship the instrumentation.

The persistence restructure for a real dedicated server is now underway
(chunked, 2026-08-23, see `MULTIPLAYER_PLANNING.md` section 3 item 5 for
the full 7-chunk breakdown: data-shape split -> real player identity ->
per-player load/save -> new-vs-returning logic -> real save triggers ->
real connectivity -> live multi-connection test). **Chunks 1-3 done,
2026-08-23 (v0.3.193-dev through v0.3.195-dev, all pushed).** Chunk 1
split `"player"` vs. `"world"`. Chunk 2 gave `PlayerIdentity` a real
stable `PlayerId` (`NetworkBehaviour`, generated via `PlayerPrefs`,
handed to the server via `CmdSetPlayerId`). Chunk 3 is the real
architectural change: `"player"` became `"characters"`, a dictionary
keyed by `PlayerId`, with `Save()` doing a read-modify-write so one
player's save doesn't clobber another's, and a real timing-hazard fix
(`SaveManager` now waits for `PlayerIdentity`'s new `PlayerIdReady`
event instead of assuming `Start()` ordering guarantees `PlayerId` is
already set). Live-confirmed with a real fresh-session round trip:
empty start -> crafted a Village Flag -> saved -> reloaded -> character
and world both restored correctly, zero errors. Next up: chunk 4
(new-vs-returning player logic — `SaveExists` is still a global bool,
not yet per-player).

One real decision already made during planning: single JSON file with
a per-player dictionary, not real separate files (confirmed by Ben
before chunk 3 started). One real, concrete prerequisite surfaced during
planning: Ben and traskmi test from genuinely different physical
locations (both confirmed real public IPs, no CGNAT, both have router
access) — so chunk 6 (a real Connect screen + port forwarding on
whichever side hosts) is a genuine blocker for the final live test, not
later polish. Confirmed directly (not assumed): zero Connect UI exists
anywhere today, not even Mirror's own stock `NetworkManagerHUD` wired
into `TestScene.unity`.

Still to do after the persistence restructure: whether the design-brief's
other Phase 2/3 items need anything.

Also cleaned up this session: an unrelated Discord bot project (`pom.xml`/
`src/`/`config.json`, using JDA) had somehow been scaffolded directly into
this repo, clobbering `.gitignore` with a generic Maven template in the
process. `.gitignore` restored to the Unity-specific version via `git
checkout`; the Discord bot files were removed entirely at Ben's request —
not part of Gridless, shouldn't reappear here.

Reminder for whoever picks this back up: the overall Multiplayer
conversion was always scoped as multi-week (48 PlayerXXX.cs scripts
across Inventory/Equipment, Crafting/Building, Magic/Combat, everything
else, then NPCs server-side, then a persistence restructure) - don't
treat "not finished yet" as behind schedule.
