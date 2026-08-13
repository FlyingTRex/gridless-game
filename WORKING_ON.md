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

- 2026-08-12 — Ben — Building the Campfire. Full design in
  `CAMPFIRE_PLANNING.md`: becomes a real craftable/placeable item (Spark
  becomes an alternate lighting method, not the only one), reuses the
  Furnace's `FuelTier`/`FuelItem` system (1 fuel slot), adds cooking (new
  `CookableItem` type, Raw Meat → Cooked Meat, 1 cooking slot, no
  accessory required) plus 4 accessory slots (Grill/Soup Pot/Kettle/Frying
  Pan) that gate additional recipes, and gives Body Temperature its first
  real gameplay effect + a spot on the real HUD. Also specs a Blender
  model rebuild (ring of rocks + charred wood). Planning done, moving into
  implementation now.
