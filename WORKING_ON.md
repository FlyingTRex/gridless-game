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

- 2026-08-12 — Ben — New "MRE Ration" starting-food item, closing out
  MVP2 item 3 ("Finish basic starting gear"). Model via Tripo3D (clean
  first attempt), scaled/grounded against the player (0.20 x 0.15 x
  0.06m), no `CraftingRecipe`, 0.3 lbs. Spawns 2 into the starting
  Settler's Shirt's own pocket storage at game start
  (`PlayerShirt.startingRationItem`/`startingRationCount`). Eaten via the
  existing right-click Eat action — `EdibleItem` gained an optional
  heal-over-time component (25 Health instant + 15 more over 60s, reusing
  `PlayerVitals.StartHealOverTime`) alongside its existing instant
  restore. Implementation complete: verified via YAML grep (ItemDefinition,
  EdibleItem, prefab, scene wiring all checked directly), throwaway
  scripts deleted, batch-mode compile check passed (0 CS errors) after
  killing one stale hung batch process along the way (infra flake, not a
  code issue). Version bumped to v0.3.23-dev, changelog written.
  **Still needed:** manual Play-mode pass — see TEST_FEATURE_PLAN.md's new
  "MRE Ration — starting food" checklist.
