# Campfire Planning

Planning doc for turning Campfire from a single magic-only scene prop into
a real craftable, fuel-burning, cooking structure (2026-08-12). Decisions
below are locked in; open items are flagged as such.

**Status: built (v0.3.26-dev, 2026-08-12).** Placement, fuel, cooking, and
warmth all shipped in 4 approved chunks — see `CHANGELOG.md` for the full
writeup and `TEST_FEATURE_PLAN.md` section 21 for the manual verification
checklist (not yet walked through). Still open/deferred, unchanged from
this doc's original scope: the Blender model rebuild (still the pre-Blender
placeholder, not started), the 4 accessory items + their slots, Wood
Stove, and the water-safety mechanic.

**UI redesign decided, not yet built (2026-08-12, same day, from live
testing feedback).** Loading fuel/food currently only works via a
"Campfire (nearby)" section auto-appended to the bottom of the main
Inventory tab's scroll view (same pattern as nearby StorageBox) — found
live to be a real UX problem: on an already-busy screen, a small unlabeled
row at the very bottom isn't discoverable (Ben's live report reads
verbatim as "there's no mechanism to transfer fuel," even though the
mechanism is technically present and functional). **Decided
replacement:** pressing E on the Campfire opens a small, focused popup
(same visual family as the existing action-menu popups —
`InventoryScreen.DrawPendingActionMenu` — not a full Tab-style screen)
showing its fuel/cooking slots directly, closable the same way those
popups already are. This **replaces** the current E-key light/needs-fuel
prompt-and-tap flow, not adds to it — lighting becomes a button inside the
new popup instead of the world-crosshair tap. Not yet built; the current
embedded-in-Inventory mechanism is left in place as a working (if clunky)
stopgap until the popup exists, not removed. No decision yet on whether
this same popup pattern should also replace StorageBox's identical nearby-
section approach — raised as a natural follow-on question, not committed
either way.

## 1. Current state (audit, 2026-08-12)

Confirmed directly against the codebase before designing anything new:

- **`Campfire.cs`** is a single pre-placed object in `TestScene.unity`
  (`(-4, 0.3, -2)`) — no `ItemDefinition`, no recipe, not
  craftable/placeable by the player at all.
- **Only lit via the Elemental "Spark" magic wish** (hold R), gated by
  lineage/skill/Will through `IWishTarget`/`OnWishComplete`. There is no
  ordinary interaction to light it by hand.
- State is a **binary `isLit` bool only** — no timer, no fuel
  consumption, no re-extinguishing. It does drive a real `Light`
  component (so it genuinely illuminates) and swaps lit/unlit materials.
- **No warmth or cooking connection of any kind.**
- **Body Temperature is 100% decorative.** `PlayerVitals.bodyTemperature`
  only drifts back toward neutral (50) every frame — nothing in the game
  pushes it away from that. It's not even on the real HUD
  (`VitalsBarHUD`), only a debug-overlay label.
- **No cooking mechanic exists anywhere.** Raw Meat
  (`Assets/Data/RawMeat.asset`) is a real pickupable item but has **no
  `EdibleItem` registered at all** — it can't be eaten raw today, it just
  sits inert in inventory.
- **The design brief itself flags this as an intentional gap**: Spark
  lighting the Campfire was documented as a known simplification —
  *"Campfire.Complete() just lights unconditionally... no fuel-tier input
  exists to cap it against"* — meaning a fuel-driven Campfire was part of
  the original vision, never built.
- **The current model is a pre-Blender placeholder.** Built before this
  project had a working from-scratch Blender pipeline (see
  `Tools/Tripo3D/README.md`'s Blender notes — the 5 Trimmed Stick craft
  tiers are the proof this works well now).

## 2. Decisions (2026-08-12)

- **Becomes a real craftable/placeable item.** New recipe + Build-tab
  piece, replacing "the one hardcoded scene object" status quo — the
  player can place as many as they want. **Spark becomes an alternate,
  tool-free way to light an already-placed Campfire**, not the only way
  one can exist or be lit. **Campfire's own recipe should be an early,
  low-skill unlock** ("a tier one building recipe" — Ben's framing; see
  section 5's Wood Stove note for why that's a low `SkillRequirement`
  number, not a formal tier enum). Exact skill(s)/number not decided here.
- **Fuel: reuses the exact `FuelTier`/`FuelItem` system built for the
  Furnace** (see `WOOD_AND_FUEL_PLANNING.md`) — any registered `FuelItem`
  (Stick, Trimmed Stick tiers, Plank) works, tier controls burn duration.
  No new fuel logic needed. **1 fuel slot** — simpler/smaller-scale than
  the Furnace's planned 2, fitting a primitive campfire.
- **Cooking: 1 cooking slot.** Raw Meat → Cooked Meat while the player
  stands nearby and the Campfire is lit — **auto-cooks over time, not a
  manual action**, same "runs on its own once lit, independent of active
  player input" mental model already decided for the Furnace's smelting.
  **Works with no accessory equipped at all** — open-flame roasting is the
  baseline, not something accessories unlock from zero.
- **Accessory slots (2026-08-12 addition): 4 slots, one per accessory
  type** — Grill, Soup Pot, Kettle, Frying Pan can all be equipped
  simultaneously, not swapped one-at-a-time. **Accessories gate which
  recipes are possible, not just speed/quality** — e.g. Soup requires the
  Soup Pot equipped, boiled water requires the Kettle, mirroring how
  `CraftingRecipe.requiredTools` already gates ordinary crafting
  elsewhere. Raw Meat → Cooked Meat needs no accessory (open-flame
  default); accessories add recipes on top of that baseline rather than
  being required to cook at all. Each accessory is itself a real
  equippable item (its own `ItemDefinition`, eventually its own
  recipe/model) — none of the four are designed yet beyond their names
  and their gating role. **Concrete gap to close before any of the four
  are usable: each needs its own model + icon** (Grill, Soup Pot, Kettle,
  Frying Pan) — Ben's note, 2026-08-12. Not yet built; the accessory slot
  structure can ship without them (slots just stay empty/unused until
  these exist).
- **Water is explicitly out of scope for now.** There's no dirty/unsafe-
  water mechanic anywhere in the game today — `Canteen`/`WaterSource`
  don't distinguish water quality at all, and the only water-related
  vital risk is *overdrinking* (too much at once), not water safety.
  **Decided:** boiling a filled Canteen at the Campfire is allowed as a
  convenience interaction with **no mechanical effect yet** — seeds the
  interaction without deciding a real water-safety system now. A fuller
  version of that idea was raised and explicitly deferred (see section 5).
- **Warmth: a lit Campfire raises Body Temperature while the player is
  nearby** — the first real use of a vital that's been 100% decorative
  until now. **Body Temperature also gets added to the real HUD**
  (`VitalsBarHUD`), not left debug-overlay-only.

## 3. New model (Blender, not yet built)

Design: a ring of rocks around a pile of charred wood/sticks, replacing
the current pre-Blender placeholder.

- **Rocks:** reuse an existing rock material (`Assets/Data/RockChunk.mat`
  or `RockKnifePickup.mat`) rather than authoring a new one, for visual
  consistency with the rest of the game's rock-textured props.
- **Wood:** reuse an existing wood material (`Assets/Data/TreeBark.mat`
  or `PlankFoundation.mat`) as the stick/log texture base.
- **Char effect:** a red/dark noise pass over the wood texture to read as
  charred/burnt. **Directly apply CLAUDE.md's documented
  `Mathf.SmoothStep` gotcha here** — the ore-texture fleck lesson applies
  verbatim: use the hand-rolled `SmoothThreshold(x, edge0, edge1)` helper
  for a sparse, thresholded charred-fleck look, not `Mathf.SmoothStep`,
  which would produce a uniform wash instead of scattered char marks.
- Exact reused-vs-new-texture choice and precise geometry are
  implementation details for build time, not locked here.

## 4. Data shape (implementation sketch, not yet built)

- `Campfire` gains real state: `isLit`, a `FuelTier`-driven burn timer
  (same shape as the Furnace's planned fuel inventory, just 1 slot
  instead of 2), a 1-slot cooking inventory, and **4 accessory slots**
  (Grill/Soup Pot/Kettle/Frying Pan — an `Inventory`-like structure with
  named/typed slots, similar in spirit to `PlayerEquipment`'s named body
  slots rather than a generic stack grid).
- **New `CookableItem` ScriptableObject**, mirroring
  `EdibleItem`/`MedicineItem`/`FuelItem`'s exact established pattern:
  `rawItem`, `cookedItem`, `cookDurationSeconds`, plus a new
  **`requiredAccessory` field (`ItemDefinition`, nullable)** — null means
  cookable over the open flame with no accessory (Raw Meat → Cooked
  Meat's case); set means that specific accessory must be equipped in one
  of the 4 slots (Soup, boiled water, etc.), the same gating shape
  `CraftingRecipe.requiredTools` already uses elsewhere, just checking an
  accessory slot instead of a held tool.
- **`CookedMeat` needs its own `ItemDefinition` + `EdibleItem`
  registration** — Raw Meat itself deliberately stays un-eatable (no
  `EdibleItem`), so cooking is required, not optional. Exact `FoodTier`
  for Cooked Meat not decided here (Meal tier, matching MRE Ration's
  substantiality, is a reasonable starting point to evaluate at build
  time). This is the one `CookableItem` recipe actually specced —
  anything accessory-gated (Soup, boiled water, etc.) is unscoped beyond
  "the accessory exists and gates it," per section 2.
- **Warmth:** a proximity check (Campfire-side or `PlayerVitals`-side)
  nudging `bodyTemperature` upward while the player is within range of a
  lit Campfire. Exact range and rate not decided here.
- **`VitalsBarHUD` gains a real Body Temperature bar/readout**, the same
  treatment the other 5 vitals already get.

## 5. Deferred ideas (not decided, logged for later)

- **A real water-safety mechanic** (untreated water risks sickness,
  boiling at the Campfire purifies it) was raised and explicitly
  deferred — Ben's call: meaningfully bigger scope than this round,
  revisit once there's a concrete reason to build it (e.g. a recipe or
  survival-pressure need that actually depends on it).
- **Wood Stove, as a future upgrade using the Campfire as a template**
  (2026-08-12): Ben's idea — this design (fuel slot, cooking slot,
  accessory slots) is meant to be reusable for a genuinely better cooking/
  heating structure later, not a one-off. **Explicitly decided: no new
  "structure tier" concept** — a Wood Stove is a different *structure*
  from a Campfire (different capability, not a quality-tier of the same
  object), so it does **not** reuse `CraftTier` the way a Backpack's 5
  quality tiers do (same mistake CLAUDE.md's tier-scaling gotcha already
  warns against — Crude/Masterwork means skill level, not
  technology/building era). Instead: **Wood Stove is simply its own
  recipe with a higher `SkillRequirement` than Campfire's**, the same
  ordinary skill-gate every other recipe already uses — no new enum, no
  new scale. Not scoped beyond that today: which skill(s) gate it, its
  exact fuel/cooking/accessory-slot counts (presumably more than
  Campfire's 1/1/4, but unconfirmed), and whether it's Woodworking-only
  or also needs Metalworking (a wood-and-metal appliance) are all open.

## Cross-references

- `WOOD_AND_FUEL_PLANNING.md` — the `FuelTier`/`FuelItem` system this
  reuses directly, and the Furnace design this deliberately mirrors in
  shape (fuel inventory, burns while lit regardless of active use).
- `CLAUDE.md`'s `Mathf.SmoothStep` gotcha — applies directly to the
  charred-wood texture work.
- `Tools/Tripo3D/README.md`'s Blender from-scratch modeling notes (the
  Trimmed Stick tiers) — the precedent for building this model in Blender
  rather than Tripo3D or a bespoke procedural C# script.
