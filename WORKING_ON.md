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

- 2026-08-12 — Ben — Wood & Fuel system. Full design in
  `WOOD_AND_FUEL_PLANNING.md`. **First build chunk shipped (v0.3.25-dev):**
  Log is now a real pickupable item (`ResourceNode` gained a secondary "F"
  pick-up action alongside its existing Axe-chop action; new `Log`
  `ItemDefinition`, 15 lbs, reuses the existing placeholder cylinder mesh
  — no Tripo3D needed), Stick/Trimmed-Stick-tiers (0.5 lbs)/Plank (3 lbs)
  got real weights instead of the untuned default `1f`, and the
  `FuelTier`/`FuelItem` data layer exists (Stick + 5 Trimmed Stick tiers =
  Tier 1, Plank = Tier 2) — mirrors `EdibleItem`/`MedicineItem`'s pattern.
  Log itself isn't wired as fuel yet (tier/duration undecided).
  Implementation complete: verified via YAML grep throughout (weights,
  Log item/prefab, ResourceNode pickup fields, all 7 FuelItem assets),
  throwaway script deleted, batch-mode compile check passed (0 CS errors)
  at every stage. Version bumped to v0.3.25-dev, changelog written.
  **Still needed:** manual Play-mode pass — see `TEST_FEATURE_PLAN.md`'s
  new "Pickupable Log + wood-item weights + FuelItem data layer" checklist.
  **Not yet built** (see `WOOD_AND_FUEL_PLANNING.md`'s build order): real
  Furnace state (lit/unlit, fuel inventory, burn timer), the on/off
  toggle, ore loading, Storage Crate auto-feed, and the longer-term
  autonomous-production-chain vision (already logged separately in
  `BUGS_AND_ENHANCEMENTS.md`).
