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
- 2026-08-13 — Ben — Imported the "Stylized Nature - Megapack" (Rystek
  Software, Unity Asset Store) into `Assets/LJPackages/` — turns out to be
  a bundle of 6 separate biome packs (AutumnForest, CommonForest,
  DesertEnvironment, SpringEnvironment, Wetlands, WinterEnvironment),
  190 prefabs total, URP Shader Graph wind/water shaders, 28 terrain
  layers. Confirmed compiles clean with zero conflicts against the
  project's own scripts. Used 4 of the 6 packs (Common/Autumn/Desert/
  Winter) to scatter `TestScene.unity`'s terrain into 4 biome quadrants
  as a first-look world-dressing test — trees/rocks/groundcover/bushes
  placed with per-instance bounds-measured grounding (never assumed a
  pivot-at-base convention), plus a matching terrain splat-paint pass
  (sand under Desert, snow under Winter, etc.) with a smooth bilinear
  blend at the quadrant cross and a soft blend back to the original grass
  near the existing Anvil/Furnace/Campfire base (kept as a 20-unit
  keep-out radius, untouched). Verified via rendered preview screenshots.
  **Found and not yet resolved:** a couple of specific CommonForest tree/
  bush prefabs render solid black in preview renders — isolated to a
  couple of assets, not a pack-wide shader problem (most trees/bushes
  across all 4 quadrants render correctly), but the exact cause wasn't
  pinned down from outside the Editor (no shader compile errors in the
  batch-mode log). Worth a direct look in the Editor's Scene/Game view
  (real baked lighting + Console) before trusting it either way — my
  preview renders use a deliberately minimal unbaked lighting setup that
  may itself be part of the problem, not necessarily the asset.
