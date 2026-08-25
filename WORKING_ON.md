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

**Everything through v0.3.198-dev is merged to origin/main.** Multiplayer
Phase 3 is fully complete (all 5 sub-phases). "NPCs move server-side" is
substantially done (see `MULTIPLAYER_PLANNING.md` section 3 item 4).

**Persistence restructure — chunks 1-4 done + chunk 5a done, 2026-08-23/24**
(see `MULTIPLAYER_PLANNING.md` section 3 item 5 for full chunk-by-chunk
detail). Chunk 5's remaining sub-chunk (5b: autosave/save-on-disconnect/
save-on-shutdown, all server-guarded) not started —
`PlayerAutosave.cs` still calls `Save()` directly, unguarded by
`isServer`. Chunk 6 (a real Connect screen + port forwarding) is a
confirmed prerequisite for the eventual live test with traskmi (both
have real public IPs, no CGNAT, router access — port forwarding will
work) — zero Connect UI exists anywhere today, not even Mirror's stock
`NetworkManagerHUD`.

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
