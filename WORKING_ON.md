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
