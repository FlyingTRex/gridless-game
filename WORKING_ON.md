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
  MVP2 item 3 ("Finish basic starting gear"), plus a new 5-tier Hunger
  restoration system. Model via Tripo3D (clean first attempt),
  scaled/grounded against the player (0.20 x 0.15 x 0.06m), no
  `CraftingRecipe`, 0.3 lbs. Spawns 2 into the starting Settler's Shirt's
  own pocket storage at game start
  (`PlayerShirt.startingRationItem`/`startingRationCount`). Eaten via the
  existing right-click Eat action — `EdibleItem` gained an optional
  heal-over-time component (25 Health instant + 15 more over 60s, reusing
  `PlayerVitals.StartHealOverTime`) alongside its existing instant
  restore. **Follow-up (same day, v0.3.24-dev) from Ben's live-test
  report:** the model stood upright instead of lying flat when dropped —
  fixed by rotating it 90° and re-grounding (caught and corrected a math
  bug in the fix itself — first attempt sank it ~0.1m into the ground —
  before saving, via reviewing the numbers rather than a second live
  report). Also added `FoodTier` (`Assets/Scripts/FoodTier.cs`, mirrors
  `CraftTier.cs`'s pattern but is a deliberately separate axis — food
  substantiality, not crafting quality): Snack(15)/Light Meal(25)/
  Meal(40)/Hearty Meal(60)/Feast(90) Hunger restored. Every `EdibleItem`
  now restores Hunger via this tier unconditionally; MRE Ration = Meal,
  Berry retuned to Snack (was a flat 20 Hunger, now 15, with its old
  Health-effect fields zeroed since it has none). Implementation
  complete: verified via YAML grep throughout (ItemDefinition, EdibleItem,
  prefab, scene wiring all checked directly), throwaway scripts deleted,
  batch-mode compile check passed (0 CS errors). Version bumped to
  v0.3.24-dev, changelog written. **Still needed:** manual Play-mode pass
  — see TEST_FEATURE_PLAN.md's updated "MRE Ration — starting food +
  5-tier Hunger restoration" checklist.
