# Wood & Fuel Planning

Planning doc for the wood-item audit and the Furnace fuel system worked out
in conversation (2026-08-12), prompted by a simple observation: the Furnace
needs fuel to work, and wood is the obvious first fuel type. Decisions below
are locked in; open items are flagged as such. See `WORKING_ON.md`/
`CHANGELOG.md` for build status once this moves from planning into code.

## 1. Current wood system (audit, 2026-08-12)

Confirmed directly against the codebase before designing anything new:

- **Tree** (`ChoppableTree.cs`) — chop with a 5-tier **Axe** → drops 3×
  **Log** as world objects, tree becomes a stump (regrows in 180s).
- **Log** — **not a player-pickupable item.** No `Log` `ItemDefinition`
  exists at all. `Log.prefab` is a stationary `ResourceNode` (same category
  as a boulder/ore node), not something that ever enters inventory.
- Chopping a Log node, also with an **Axe**, yields 2× **Plank**
  (guaranteed) + a 30% chance of a bonus raw **Stick**.
- **Stick** (`Assets/Data/Stick.asset`, `CraftTier.Normal`) — pickupable,
  dropped by chopping a Log node. Also the base item for a 5-tier
  **Trimmed Stick** ladder (Crude→Masterwork), each requiring its own
  recipe (1 Stick + a Knife in hand + skill), tier-chained for upgrades.
- **Plank** (`Assets/Data/Plank.asset`) — pickupable, produced only by
  chopping a Log node (no separate crafting-bench recipe of its own).
  Also feeds a number of building pieces (Wall/Half-Wall/Door) as
  further downstream items.
- No other wood items exist (no Bark, Sawdust, Charcoal, Firewood).

So "Sticks and Planks are the only wood items you can pick up" is correct,
but not because anything was filtered out — Log was designed from the
start as a stationary choppable node, never an inventory item.

## 2. Fuel tier system (decided 2026-08-12)

| Fuel Tier | Items | Burn duration (per item) |
|---|---|---|
| 1 | Stick + all 5 Trimmed Stick craft-tiers (craft quality does **not** affect fuel efficiency — a Masterwork Trimmed Stick burns exactly as long as a plain Stick) | 5 min |
| 2 | Plank | 10 min |
| 3–5 | Reserved for future fuel types — Coal, Gas, Electricity, in that rough progression | TBD when those items get designed |

**Tier is efficiency (burn duration) only — it never gates which recipes
can be smelted.** A Tier-1 fuel can smelt anything a Tier-5 fuel can;
higher tiers just burn longer per item, matching Ben's explicit call.

**Technical shape:** a new `FuelTier` enum + a `FuelItem` companion
`ScriptableObject` (mirrors the existing `EdibleItem`/`MedicineItem`
pattern — a small registered-list asset type, not a field bolted onto
`ItemDefinition` itself). This keeps the base item class untouched and
matches the project's established convention for optional per-item
behaviors (see `CraftTier.cs` for the same enum-plus-static-scale shape,
though `FuelTier` is a deliberately separate axis from `CraftTier` — food
substantiality and fuel efficiency are unrelated quantities to craft
quality, same reasoning `FoodTier.cs` already established for Hunger).

## 3. Furnace behavior (decided 2026-08-12)

- **Burns continuously once lit**, in real time, independent of whether a
  craft is actually running — realistic ("light it and it burns until it's
  out or shut off"), not "only ticks fuel while actively smelting."
- This means the Furnace needs an explicit **on/off toggle**, or a player
  who lights it and walks away burns through fuel for nothing.
  **Built (v0.3.31-dev)** as `FurnaceScreen`'s Auto-Run toggle.
- **Built (v0.3.31-dev).** `Furnace.cs` now holds the real state described
  below — lit/unlit flag, a fuel inventory, a remaining-burn-time timer
  ticking down over real time. `FurnaceSurface.cs` stays a bare marker,
  unchanged — it's still the sole gate for `PlayerCrafting.
  HasNearbyFurnace`/`CraftingRecipe.requiresFurnace`, a separate concern
  from `Furnace.cs`'s own unattended production line.

## 4. Loading fuel & ore (decided direction, 2026-08-12)

- **Near-term: manual.** The Furnace gets its own small fuel inventory (2
  slots), filled by dragging from the player's inventory the same way any
  other container transfer works today. The same slots (or a similarly
  simple manual mechanism) also accept **ore** — Ben's addition: the
  Furnace isn't just fuel-in, it also needs the actual smelting input
  loaded somehow, and manual loading is the simplest version of that.
- **Near/mid-term: Storage Crate auto-feed.** Ben's idea — designate a
  nearby `StorageBox` (**already a built system**, unlike the Woodshed
  below) to auto-feed the Furnace (fuel and/or ore) within some range.
  Meaningfully shorter lift than the Woodshed idea since the container
  type already exists; worth building before Woodshed for that reason.
- **Future, not scoped: a dedicated Woodshed structure** (doesn't exist)
  auto-feeding fuel specifically within 15m. An alternative/additional
  path layered on top of manual loading and Storage Crate auto-feed
  later, not a replacement for either.

## 5. Longer-term vision: autonomous production chain (forward-looking, not scoped)

Ben's fuller vision, floated in the same conversation: assign a
Woodcutting NPC to gather wood into a storage container, assign a Mining
NPC (already exists) to gather ore into a storage container, and have the
Furnace auto-pull both from nearby/linked storage and continuously produce
finished products (Ingots, eventually more) with no player nearby at all —
a real automated production line.

**This is a genuinely new subsystem, not a data/config extension of the
fuel-tier work above.** Two real gaps stand between here and there:

- **Woodcutting doesn't exist as an NPC job family.** Mining is the only
  job family hireable NPCs support today (see `MVP2_PLANNING.md` item 2,
  "Expand NPC hiring beyond stonework") — a Woodcutting job would need to
  be built the same way Mining was (`NPCJob`/`NPCJobDefinition`).
- **The Furnace has no independent process loop.** Every craft today,
  including the planned Iron Ingot recipe, only runs because the player is
  physically present and clicked Craft — `PlayerCrafting`'s batch timer is
  driven entirely by the player's own component. For the Furnace to keep
  smelting unattended while an NPC feeds it, it needs its own loop ticking
  on its own timeline — closer in spirit to how a Hireable NPC's mining
  job runs whether or not the player is watching, but nothing like that
  exists for a *building* today.

**Decision: scope this as its own future chunk, not part of the near-term
fuel-tier build.** Logged here and in `BUGS_AND_ENHANCEMENTS.md` so it
isn't lost, not committed to an order yet.

## Suggested build order (once this moves from planning to code)

1. ✅ **Done (v0.3.25-dev, 2026-08-12):** `FuelTier` + `FuelItem` data
   layer (Stick + all 5 Trimmed Stick tiers = Tier 1, Plank = Tier 2). Also
   picked up along the way (not originally numbered here, but a
   prerequisite for Log to participate in any of this): **Log is now a
   real pickupable item** (`ResourceNode` gained an optional secondary "F"
   pick-up action alongside its chop action; new `Log` `ItemDefinition`,
   15 lbs, reuses the existing placeholder cylinder mesh), and **Stick/
   Trimmed Stick (0.5 lbs)/Plank (3 lbs) got real weights** instead of the
   untuned default `1f` every wood item silently had before. **Log itself
   is not yet wired as a `FuelItem`** — its tier/burn-duration wasn't
   decided during planning, only its weight was.
2. ✅ **Done (v0.3.31-dev, 2026-08-13):** Real Furnace state: lit/unlit
   flag, a 2-slot fuel inventory, a burn timer ticking in real time while
   lit (`Furnace.cs`, on the existing scene `Furnace` GameObject alongside
   the untouched `FurnaceSurface` marker).
3. ✅ **Done (v0.3.31-dev):** Furnace on/off toggle — `FurnaceScreen`'s
   Auto-Run toggle. Lighting itself is automatic (not a player-clicked
   button like Campfire's) — with Auto-Run on, fuel on hand, and a
   non-empty recipe queue, the Furnace lights itself, matching the point
   of building it as an unattended structure in the first place.
4. ✅ **Done (v0.3.31-dev), reshaped from "manual ore loading":** ore/
   materials loading is drag-and-drop via `FurnaceScreen` (same as fuel),
   plus a new `SmeltableItem` recipe type (deliberately separate from
   `CraftingRecipe` — see `CHANGELOG.md`) driving an up-to-4 sequential
   smelting queue, distinct from the existing player-driven, skill-gated
   `IronIngotRecipe` bench craft.
5. ✅ **Done (v0.3.31-dev), pulled forward ahead of schedule:** StorageBox
   auto-feed/auto-drain — the player designates a nearby StorageBox each
   for Fuel Source, Materials Source, and Output via `FurnaceScreen`'s
   picker; the Furnace's own `Update()` loop pulls/pushes from them every
   frame regardless of whether the player is nearby, independent of the
   popup being open — genuine unattended automation (Ben's call, pulling
   forward part of section 5 below rather than waiting for the full NPC
   chain).
6. *(Future phase, unscoped)* Woodcutting NPC job family to keep the
   linked boxes stocked without a player doing it by hand, plus the
   Woodshed structure. The Furnace's own process loop (the other half of
   section 5's vision) is no longer a gap — see step 5 above.

## Cross-references

- `BUGS_AND_ENHANCEMENTS.md`'s "Furnace Fuel System" section — the on/off
  toggle gap and the autonomous-production-chain vision, both logged there
  too so they survive independently of this doc.
- `docs/design-brief.md`'s Metal pipeline (`Ore →(Furnace + fuel,
  Metalworking)→ Ingot`) and its Furnace open-questions list — both
  updated to point here.
- `CraftTier.cs` / `FoodTier.cs` — the established enum-plus-static-scale
  pattern `FuelTier` will follow, and the precedent for keeping tier axes
  for unrelated quantities (craft quality vs. food substantiality vs. fuel
  efficiency) separate rather than reusing one scale for all of them.
