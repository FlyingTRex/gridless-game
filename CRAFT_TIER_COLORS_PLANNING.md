# Craft Tier Colors + Crafting Screen Sort/Filter

Planning doc for a visual + UX pass on `CraftTier` (2026-08-12): color-code
items by tier so quality is legible at a glance, and let the Crafting
screen sort/filter by tier instead of the current fixed family order.
Decisions below are locked in; see `WORKING_ON.md`/`CHANGELOG.md` for
build status.

**Mockup:** https://claude.ai/code/artifact/5ba4d56b-8223-4e3e-a093-8c3ba309a7c6
— static HTML preview built with real item families (Knife/Pickaxe/Hammer)
showing both pieces together. Ben's sign-off: "I like it."

## Decisions

### 1. Color palette — full 5-color ramp, including Normal

| Tier | Color | Hex |
|---|---|---|
| Crude | Gray | `#9a9a9e` |
| Rudimentary | White | `#f4f5f7` |
| Normal | Green | `#6fbf6a` |
| Fine | Blue | `#5b9fe0` |
| Masterwork | Gold | `#e3a53e` |

Considered and rejected: leaving `Normal` uncolored to preserve its
"unremarkable no-prefix baseline" status (matching `CraftTierNames`'
existing no-prefix convention). Ben's call: every tier gets a color,
including Normal — simple and consistent beats preserving that asymmetry
into the visual language too.

### 2. Technical approach — border + text color, not a full tint

Research before deciding (`Assets/Scripts/DebugGUI.cs`,
`InventoryScreen.cs`, `CraftingScreen.cs`, all read directly): this is
greenfield, no existing rarity/tier color-coding or `GUI.color` usage
anywhere in the codebase.

**Rejected: wrapping draws in `GUI.color`.** It multiplies *everything*
under it — including the icon's own art (a red apple would get re-tinted
toward the tier color) and `DebugGUI.Slot`'s already-dark background
(multiplying two dark colors together reads as mud, not the intended
tier color). Neither is what "visually apparent tier" should mean.

**Decided:** two additions instead, both leaving icon art untouched —
- A thin colored **border** around each occupied slot/tile (new per-tier
  border texture, same technique `DebugGUI.Slot`'s solid fill already
  uses, just an outline instead of a full fill).
- A per-tier **text color** for the item name specifically (new small
  `GUIStyle` variant per tier, cloned from the existing `Label`/`Header`
  base with `normal.textColor` overridden — `DebugGUI`'s styles are
  single cached shared instances today, so this means caching 5 variants
  instead of 1, not restructuring the existing pattern).

Applied everywhere an item currently renders: `InventoryScreen.DrawSlotBox`
(main grid, equipment slots, container contents — this screen already
unified all three into one draw path this session, so the color logic only
needs to live in one place), `CraftingScreen.DrawTile`/`DrawIcon`, and the
shared tooltip (`InventoryScreen.DrawTooltip`).

**New shared lookup:** `CraftTierColors` in `Assets/Scripts/CraftTier.cs`,
mirroring the existing `CraftTierNames`/`CraftTierScale` pattern exactly —
a static table other systems already know how to find and extend.

### 3. Crafting screen: sort + filter by tier

- **Recipes within the current discipline tab (or search results) sort by
  tier ascending by default** — this replaces today's implicit order
  (whatever order `PlayerCrafting.recipes` lists them in, which in
  practice is family-grouped: all 5 Knife tiers together, then all 5 Axe
  tiers, etc.).
  
  **Real tradeoff, decided anyway:** tier-sort scatters families apart
  (Crude Knife sits next to Crude Pickaxe next to Crude Hammer, not next
  to Rudimentary Knife). Considered keeping family-grouped as the default
  with tier-sort as an opt-in toggle instead. **Ben's call: tier-sort
  becomes the new default** — matches "finding recipes would be easier"
  directly, family-grouped browsing isn't preserved as a separate mode.
- **A sort-direction toggle button** ("Tier 1 → 5" / "Tier 5 → 1") flips
  ascending/descending.
- **A tier filter row** (All + 5 tier chips, colored per the palette above
  — reusing the same tier colors reinforces the visual language instead of
  inventing a second one) narrows the grid to one tier, ANDed with the
  existing discipline-tab/search filter, not replacing it.

## Build plan

1. `CraftTier.cs` — add `CraftTierColors` static table (5 `Color` values,
   hex above converted to 0-1 floats).
2. `DebugGUI.cs` — add a per-tier border-texture cache and a per-tier
   `GUIStyle` (or a `GetTierLabelStyle(CraftTier)`-style accessor) built
   the same lazy-cached way `Label`/`Header`/`Slot` already are, just
   keyed by tier instead of a single instance.
3. `InventoryScreen.DrawSlotBox` — draw the tier border around occupied
   slots, use the tier-colored text style for the item name.
4. `CraftingScreen.cs`:
   - `DrawIcon`/`DrawTile` — same border + tier-colored name treatment.
   - New tier-filter chip row + sort-direction toggle in `DrawContent`,
     state fields for current filter/sort direction (session-local UI
     state, same as existing `searchQuery`/`desiredQuantity`, not saved).
   - Materialize the filtered recipe list, sort by `outputItem.tier`
     (respecting the toggle direction) before the existing tile-grid
     layout loop, instead of iterating `crafting.Recipes` directly.
5. Batch-mode compile check, then a manual Play-mode pass (add a
   `TEST_FEATURE_PLAN.md` entry): confirm colors read correctly against
   the dark panel background, confirm the tier filter/sort actually
   reorders the grid, confirm existing availability warnings (red
   ingredient text) aren't visually confused with tier colors.
