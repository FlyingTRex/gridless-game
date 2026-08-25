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

**Everything through v0.3.203-dev is merged to origin/main and
LIVE-CONFIRMED — both visibility fixes hold up, from the host's side.**
Multiplayer Phase 3 is fully complete (all 5 sub-phases). "NPCs move
server-side" is substantially done (see `MULTIPLAYER_PLANNING.md`
section 3 item 4).

**2026-08-25 — THE FIRST REAL LIVE TWO-CONNECTION TEST HAPPENED AND
WORKED, iterated three times live.** Editor host + standalone exe
client, localhost. Per-connection spawning (v0.3.200-dev) and the
Connect screen (v0.3.201-dev) are both genuinely live-confirmed —
`Player(Clone)` spawned correctly, independent character state, correct
non-local ownership. Two rounds of the same underlying bug found, fixed,
and now both confirmed working from the host's side: **round 1
(v0.3.202-dev)** — neither side could see the other's BODY at all,
root-caused via the V-key third-person toggle (no code needed to
diagnose it) to `WornEquipmentLayer`'s camera-cullingMask exclusion
being global-per-camera rather than "hide my own body." **Round 2
(v0.3.203-dev)** — the very next test found worn EQUIPMENT had the
identical problem (each of 11 equippables independently manages the
same layer via its own `SetCarried`), fixed by generalizing into one
full-hierarchy sweep. **Confirmed from the host's side: body AND
equipment both render correctly in first person and third person.**
The exe's own view still shows the old broken behavior — a real but
harmless asymmetry, not a code bug: the standalone `.exe` was built
once early in the session, before any of these fixes existed, and
hasn't been rebuilt since. Rebuild whenever a true bidirectional
confirmation is wanted (not urgent — the fix itself is proven).
(`FirstPersonController.ApplyRemoteVisibilityLayer`) instead of the
narrower body-only fix. Also found and explained (unrelated to any
code): the exe crashed once mid-test from a native D3D12 GPU driver
error — worked around with `-force-d3d11` on relaunch, nothing to fix
in the codebase.

**2026-08-25 — chunk 6, real Host/Join Connect screen, v0.3.201-dev,
NOT yet live-tested.** `NetworkAutoHost.cs` repurposed in place (silent
auto-host-on-Play → a real Host/Join OnGUI screen, Ben's explicit
call — the old behavior meant there was never an actual path to join
someone else's game, every Play session just started its own server).
`GridlessNetworkManager` gained `OnClientError`/`OnClientDisconnect`
feeding real failure messages into it. Confirmed transport config via
an independent verification process: `KcpTransport`, port `7777`.
**Real trade-off Ben chose knowingly**: every Play session, including
solo Editor testing, now needs one Host click first — no more instant
auto-host. Port forwarding itself is still a manual step for whoever
hosts. Chunk 7 (the actual live two-player test) is next, but needs
this AND chunk 5's per-connection spawning both live-verified first —
neither has had a real second connection yet, only compile checks.

**Persistence restructure — chunks 1-5 done, 2026-08-23/25** (see
`MULTIPLAYER_PLANNING.md` section 3 item 5 for full chunk-by-chunk
detail). Chunk 5 (server-authoritative saving) is fully closed out and
live-confirmed (hired a Woodworking NPC, autosave picked it up on
schedule, a shutdown-save afterward kept the same data intact).

**2026-08-25 — real per-connection Player spawning built, v0.3.200-dev,
NOT yet live-tested.** Found while building chunk 5b:
`GridlessNetworkManager.OnServerReady` only ever handed the one
pre-existing scene Player to whichever connection asked first, refusing
a second connection anything at all. Fixed: `playerPrefab` wired in the
scene, second-and-later connections get a fresh `Instantiate`d Player.
That surfaced a much bigger gap on inspection — none of the 48
`PlayerXXX.cs` scripts gated local input/`OnGUI()` by `isLocalPlayer`,
invisible until a second real Player object could exist at all. Fixed
across `FirstPersonController` (the root input pump every `*Screen.cs`
sibling routes through) plus the 12 other scripts that read local input
directly, and `FirstPersonController` now disables Camera/AudioListener
on non-local instances. Also built: a nearby-player-joined toast
(1000m radius, fog of war still hides the Map marker). **Still needs a
real live test** — per this project's "Compiled Game" testing
convention, build a standalone Player exe, host in the Editor's Play
mode, connect the standalone as a second client to localhost. One
real open question flagged, not checked: whether a remote player's
animation actually looks right off network-synced position now that
local `CharacterController.velocity` isn't locally driven for them
anymore. Full detail in `BUGS_AND_ENHANCEMENTS.md` and
`MULTIPLAYER_PLANNING.md` section 3 item 6.

**Real gap found while building 5b, not yet solved — likely blocks
chunk 6/7**: `GridlessNetworkManager.OnServerReady` only ever hands out
the one pre-existing scene Player object; a second connection gets
none. Logged in `BUGS_AND_ENHANCEMENTS.md`. Chunk 6 (a real Connect
screen + port forwarding) is still a confirmed prerequisite for the
eventual live test with traskmi (both have real public IPs, no CGNAT,
router access — port forwarding will work) — zero Connect UI exists
anywhere today, not even Mirror's stock `NetworkManagerHUD` — but this
per-connection-spawning gap probably needs solving first, since a
working Connect flow that hands the second player nothing to control
isn't useful to test with.

**2026-08-24 — a genuinely functional playtest (Ben deliberately
avoiding Admin Spawn) found and fixed a real early-game bootstrap
deadlock**, live-confirmed end-to-end (crafted Copper Nail from raw
Copper, gathered Plank, built a Storage Box):
- Added `CopperNail`/`CopperNailRecipe` (1 raw Copper → 5 Copper Nail,
  no Furnace needed), swapped `StorageBoxPiece` from iron Nail to
  Copper Nail, registered in `Player.prefab`'s recipe list, databases
  repopulated. Iron Nail recipe itself untouched.
- Confirmed every scattered Boulder having `AnvilSurface` is
  *intentional* ("Hammer + Boulder → Nail" was the original design,
  already correctly implemented) — not a bug, don't "fix" it.
- Fixed a real NPC bug: `MineOreJob.asset` had the Mining Face Shield
  as a *mandatory* tool, so a Mining NPC could never start working at
  all. Removed — Shield is now correctly just a situational benefit.
- Removed the dev-convenience Anvil, Furnace, and a placed Mining Face
  Shield pickup from `TestScene.unity` (near spawn) for a genuine
  from-zero test.
- Designed (not built) a full **Metal Detecting** system with Ben —
  a trained attribute-style skill (grown by mining, same pattern as
  Str/Dex/Con/Int) gating Silver/Gold/Platinum visibility specifically
  (Rock/Copper/Iron stay baseline-visible regardless). The Mining Face
  Shield's own reveal power needs scoping down to common ore only, or
  it undercuts the whole gate — logged in `BUGS_AND_ENHANCEMENTS.md`.

**Real bugs found live tonight, logged but not fixed — pick these up
next:**
- `ReachableInventories` (the "how much of X do I have" check) only
  looks at main inventory + the equipped Backpack specifically —
  independently duplicated with the identical gap across 4 systems
  (`PlayerBuilding`, `PlayerCrafting`, `PlayerPieceUpgrade`,
  `VendorStallScreen`), none of them checking Belt/Shirt/Jeans. Found
  live: 9 Plank in a worn Shirt didn't count toward a Storage Box
  build. `PlayerCarriedItems.cs` already has the correct generalized
  pattern (`ContainerSlots = { "Back", "Waist", "Chest", "Leg" }`) from
  fixing this identical bug once before for NPC tool-giving — the real
  fix is one shared helper all 4 call sites use, not 4 separate patches.
- Nail's icon renders as a Hammer — confirmed pre-existing (not
  introduced by tonight's Copper Nail work, which faithfully copied
  Nail's own already-wrong icon reference).
- Nail/Copper Nail appeared capped at 10 in a stack (both in main
  inventory and moving into a Backpack), but the code says the cap
  should be 20 (`Inventory.MaxStackCap = 20`, `Mathf.Min(20, 20) = 20`,
  no duplicate asset, no hardcoded 10 anywhere found). Best guess is a
  stale Play session, **not confirmed** — needs a real retest from a
  fresh domain reload before assuming anything.

**2026-08-23 — uncommitted local work from earlier, still not tested or
pushed — pick this up too:**
- `NPCHiringScreen.cs` — added `RequestTalk`/`RequestSetFrozen`
  Commands, closing the last flagged low-priority gap. Compiles clean,
  **not yet live-tested** — verify Talk still opens dialogue and the
  Frozen checkbox still holds an NPC in place before committing.
- `PlayerInteraction.cs` / `PlayerRangedCombat.cs` — **temporary debug
  logging only, not meant to ship as-is.** Added to chase the stuck
  empty hold-progress-bar bug — Ben's report ties it to Bow/arrow usage
  specifically. `PlayerRangedCombat` logs every `isDrawing` state
  transition; `PlayerInteraction.OnGUI` logs (throttled, once/sec)
  whenever the E-hold bar is drawing. Reproduce, read the
  `[RangedDebug]`/`[HoldBarDebug]` log lines, root-cause it, then
  **remove this logging** before committing the real fix.

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
