# Working On

What's actively in progress right now, one line per active session. Check this
before starting new feature work — if something here overlaps what you're about
to build, coordinate before duplicating effort (see the Waterskin/Canteen
collision in `CHANGELOG.md`, 2026-08-02, for what happens when this doesn't get
checked).

Add a line when you start a non-trivial feature; remove it once merged to
`origin/main`. Stale entries are worse than none — if you're not sure whether an
entry is still active, ask before trusting it.

Format: `- YYYY-MM-DD — who — one-sentence description`

- 2026-08-13 — Ben — Player body Male/Female toggle in the ` menu's
  Player tab (MVP2 item 4, direct follow-up to the player-visible-body
  entry below). v0.3.35-dev, **implementation complete, not yet
  committed/pushed.** Deliberately held off starting this until the
  player-visible-body entry below was actually committed/pushed, since
  both would otherwise batch-mode-save the same `TestScene.unity` —
  exactly the collision this file exists to catch. New `PlayerBodyModel.cs`
  (both gendered `Visual` instances pre-instantiated, `SetActive`-toggled,
  not created/destroyed at toggle time), small setter additions to
  `PlayerAnimatorDriver`/`NPCVisualGroundFix`, two tab-style buttons in
  `GameMenuScreen.DrawPlayerTab()`. Verified via compile + YAML grep only
  so far — **no live Play-mode test yet**, see `TEST_FEATURE_PLAN.md`
  section 26. Full detail in `CHANGELOG.md`'s v0.3.35-dev entry.
- 2026-08-13 — Ben — NPC animation, MVP2 item 4 (scope narrowed to NPC-only).
  v0.3.33-dev, **committed and pushed to origin/main** (confirmed —
  fetch shows origin/main at the same commit; an earlier push report of
  "remote rejected" turned out to be a transient ref-lock race, not a real
  collision — all LFS objects had already uploaded and the ref updated
  moments later). **No live Play-mode test yet**, see
  `TEST_FEATURE_PLAN.md` section 24.
- 2026-08-13 — Ben — Player visible body + first/third-person camera
  toggle (MVP2 item 4, player half). v0.3.34-dev, **committed and pushed
  to origin/main** (confirmed — local `HEAD` and `origin/main` both at
  `fb158df`). Full detail in `CHANGELOG.md`'s v0.3.34-dev entry. **No live
  Play-mode test yet**, see `TEST_FEATURE_PLAN.md` section 25 — remove
  this line (and the NPC animation line above it, same status) once a
  Play-mode pass covers both.
- 2026-08-13 — Ben — NPC job generalization: Woodworking + Berry/Herb
  foraging (v0.3.32-dev, **committed and pushed**). Full design in
  `NPC_JOB_GENERALIZATION_PLANNING.md`,
  built same day: `NPCMining.cs` renamed to `NPCGathering.cs` (functional
  rename-only, script GUID preserved so existing prefab references
  survived), a new `INPCHarvestable` interface lets `ChoppableTree`
  (standing Trees) join `ResourceNode` as a valid gathering target
  (direct-yield for NPCs, scatter behavior unchanged for players), a new
  `INPCSearchable` interface lets `BerryBush`/`HerbBush`'s search action
  (chop-for-stick skipped, player-only) trigger for NPCs too, and
  `Pickup.cs` gained an NPC-safe collection path so a foraging NPC can
  actually walk over and pick up whatever a search scattered — closes the
  loop with no new state machine, just a third scanned target pool.
  `ChopWoodJob.asset`/`ForageJob.asset` wired into `NPCJobScreen`.
  Deliberately deferred: bench-crafting families (Metalworking, Sewing,
  etc. — section 7 of the planning doc). Verified via compile + YAML grep
  only so far — **no live Play-mode test yet**, see `TEST_FEATURE_PLAN.md`.
- 2026-08-13 — Ben — Furnace real state + unattended automation
  (v0.3.31-dev, **committed and pushed**). Applied the Campfire
  treatment (E-key popup, `FurnaceScreen`, real Fuel/Materials/Output
  inventories) plus three new asks: an up-to-4 sequential smelting queue
  (new `SmeltableItem` type, deliberately separate from `CraftingRecipe`
  — see `CHANGELOG.md`), a player-selectable Output StorageBox, and
  player-designated nearby StorageBoxes for Fuel/Materials that the
  Furnace auto-pulls from. Confirmed via 3 `AskUserQuestion` items before
  building: sequential (not parallel) queue, **true unattended
  automation** (Furnace ticks fuel/queue/auto-feed every frame regardless
  of player presence — pulls forward part of `WOOD_AND_FUEL_PLANNING.md`
  section 5's "autonomous production chain" vision), on-board slots +
  auto-refill/drain (not a raw passthrough to the linked boxes). Verified
  via batch-mode compile (0 CS errors) + YAML grep of the saved scene/
  asset. **Not yet tested in Play mode** — `TEST_FEATURE_PLAN.md` needs a
  new section for this.
- 2026-08-12 — Ben — Campfire rebuild shipped (v0.3.26-dev). Full design
  in `CAMPFIRE_PLANNING.md`, built in 4 approved chunks: real craftable/
  placeable `BuildPiece` (Crude unlock, 4 Rock + 3 Stick), two ways to
  light it (E, tool-free, or the original Spark wish — now an alternate,
  not the only way), a real fuel system (1 slot, reuses `FuelTier`/
  `FuelItem` from the Furnace work, burns down and auto-extinguishes), a
  real cooking system (new `CookableItem` type, Raw Meat → Cooked Meat,
  auto-cooks while lit and the player's nearby), and Body Temperature's
  first real gameplay effect (warmth) plus a spot on the real HUD.
  `InventoryScreen` gained a "Campfire (nearby)" section for loading
  fuel/food. Deliberately deferred, per the plan: the Blender model
  rebuild (still the pre-Blender placeholder, not started), the 4
  accessory items + slots (Grill/Soup Pot/Kettle/Frying Pan — need
  models/icons before they're usable, noted in `CAMPFIRE_PLANNING.md`),
  Wood Stove, and the water-safety mechanic. Implementation complete:
  verified via YAML grep throughout, one real bug caught and fixed
  mid-build (a stale `ItemDefinition` reference across a
  `PrefabUtility.LoadPrefabContents` cycle — the exact CLAUDE.md-documented
  gotcha), throwaway scripts deleted, batch-mode compile check passed
  (0 CS errors) after every chunk.
  **Live-testing finding, same day:** the "Campfire (nearby)" fuel-loading
  UI is a real discoverability failure (an unlabeled row at the bottom of
  an already-busy Inventory scroll) — decided replacement is a focused
  popup opened by E (see `CAMPFIRE_PLANNING.md` and
  `BUGS_AND_ENHANCEMENTS.md`'s new "Campfire dedicated popup UI" section),
  **design decided, explicitly not built yet** (Ben's call: "let's wait on
  doing this further" for now) — current embedded mechanism stays in place
  as a working stopgap. **Still needed:** manual Play-mode pass — see
  `TEST_FEATURE_PLAN.md` section 21 (written against the current
  mechanism; will need a follow-up pass once the popup replaces it).
  **2026-08-13 update:** the Blender model rebuild (previously deferred)
  is now also done — v0.3.27-dev, ring of 8 rocks + 6 charred sticks,
  reused `RockChunk.mat` for rocks, generated new charred/ember-glow
  materials for the wood via the `SmoothThreshold` technique, script kept
  at `Tools/Blender/GenerateCampfireModel.py`. Full writeup in
  `CHANGELOG.md`. Verified via YAML grep + rendered preview screenshots,
  not yet checked live in Play mode.
  **2026-08-13 update 2:** the dedicated E-key popup UI (previously
  deferred) is now also done — v0.3.28-dev, new `CampfireScreen.cs`
  (same family as `LockboxScreen`), old "Campfire (nearby)" Inventory-tab
  section removed entirely (fully superseded, along with the now-unused
  `Campfire.Active`/`FindNearby`). This closes out everything logged
  above except the 4 accessory items, Wood Stove, and the water-safety
  mechanic. **Still needed:** manual Play-mode pass for both the model
  and the new popup — `TEST_FEATURE_PLAN.md` section 21 needs a rewrite
  against the new E-key-opens-popup flow, not just a re-check.
- 2026-08-12 — Ben — Set up a second texture/model API alongside
  `Tools/Tripo3D/`: `Tools/TextureAPI/` (3D AI Studio). Confirmed it's
  genuinely Tripo's own texturing tech exposed through a different
  endpoint (`/v1/3d-models/tripo/texture-model/`), not an unrelated
  vendor. Scaffolding + `Generate-Texture.ps1` built and documented
  (`Tools/TextureAPI/README.md`), API key stored in a gitignored `.env`
  (verified via `git check-ignore`, never committed). **Not yet tested
  against a real API call** — response field names inside the `results[]`
  array are a best-effort guess from the docs, script dumps raw JSON for
  diagnosis on first real use.
- 2026-08-13 — Ben — Campfire cooking system reworked again (v0.3.30-dev,
  **committed and pushed**). Grew out
  of Ben looking at the just-shipped v0.3.28-dev popup live and asking
  for more, in sequence: 4 Cooking Utensil boxes (Grill/Cooking Pot/
  Kettle/Frying Pan, one item each), a wood-only Fuel box, a Transfer
  section scoped to exactly Backpack + Hands, then — once he saw the
  utensil boxes — a bigger ask: 4 output boxes for cooked items, a
  multi-slot ingredient input area, and a Recipe button that only shows
  what's currently cookable given the loaded utensils/ingredients.
  Confirmed via `AskUserQuestion` before building: 4 input slots, single
  cook at a time (4 output boxes are just banked storage, not parallel
  cooking), and Recipe is a manual trigger (real philosophy change from
  the old always-auto-cook design). `CookableItem` restructured to
  mirror `CraftingRecipe`'s `ingredients[]`/`outputItem` shape;
  `RawMeatToCookedMeatCookable.asset` migrated in place. 4 new plain
  `ItemDefinition`s created (Grill/Cooking Pot/Kettle/Frying Pan — no
  model/icon yet, admin-spawnable, same known-placeholder gap
  `CAMPFIRE_PLANNING.md` already flagged). `CampfireScreen.cs` rewritten
  from button-based to real drag-and-drop — a self-contained
  implementation mirroring `InventoryScreen.cs`'s interaction pattern,
  not a literal shared-code extraction (avoided touching that heavily-
  tested file for a screen that needs none of its 11-equippable-type
  dispatch logic). One infra hiccup: a batch-mode Unity process hung
  after colliding with a leftover `bee_backend` process from an earlier
  interrupted run; confirmed with Ben before killing the stuck PID, then
  a fresh invocation compiled clean. Verified via compile checks + YAML
  grep only so far — **no live Play-mode test yet**, and this is a much
  bigger surface area than the earlier single-chunk changes, so that
  pass matters more than usual before calling it done. Full writeup in
  `CHANGELOG.md`'s v0.3.30-dev entry; `TEST_FEATURE_PLAN.md` section 21
  still needs a rewrite for this flow (currently written against
  v0.3.28-dev's simpler button UI).
  **Two live-feedback fixes, same day, before any test pass completed:**
  (1) the Ingredients/Cooked Items grid had zero `GUILayout.Space`
  between boxes, so 4 adjacent empty slots visually merged into one
  solid rectangle instead of reading as 4 separate boxes — fixed with a
  consistent `BoxGap` (8px) applied between every box in the grid, the
  Utensils row, and the Hands row. (2) The popup's fixed 520x640 panel
  didn't fit Ben's screen, and his touchpad has no working scroll
  gesture in this window, so the overflow content (and the Close button)
  was genuinely unreachable, not just inconvenient — panel width/height
  are now responsive (`Mathf.Min(max, Screen.dimension * 0.92f)`, same
  pattern `PlayerMenuScreen.DrawScrollable` already uses), so it shrinks
  to fit rather than relying on scrolling to cover the gap. Neither fix
  has been visually confirmed yet (I can't screenshot a live IMGUI popup
  the way I could the 3D Blender model — this needs Ben's own eyes).
