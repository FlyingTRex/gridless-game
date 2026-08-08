# Bugs & Enhancements

Known issues and requested features not being worked right now. Not a replacement
for `WORKING_ON.md` (that's for active work) or `CHANGELOG.md` (that's for shipped
work) — this is the backlog between the two. Check off and move the entry to
`CHANGELOG.md` once it's actually fixed/built.

## Bugs

- [ ] **Silver/Gold/Platinum still don't have a refined "bar" item the
  way Copper/Iron do** (v0.1.121-dev gave them the missing punchable
  mid-tier, matching Copper/Iron structurally, but the final tier still
  yields the existing `SilverOre`/`GoldOre`/`PlatinumOre` item directly,
  not a further-refined material). Worth deciding whether these three
  should eventually get a true refined tier too (a smelting recipe
  consuming the raw Ore?), or whether raw Ore is the intended final form
  for these three specifically (more currency/jewelry-flavored than
  Copper/Iron's tool-material role) — not an oversight, just an open
  design question same as the ones below.
- [ ] **New "Iron" item (`Iron.asset`, v0.1.119-dev) has no crafting
  recipe consuming it yet either.** Same situation as Copper below —
  built ahead of the crafting need when the Boulder→chunk→refined-
  material tier structure was extended to Iron Ore too. Nothing turns
  Iron into anything, and no recipe consumes it as an ingredient.
- [ ] **New "Copper" item (`Copper.asset`, v0.1.117-dev) has no crafting
  recipe consuming it yet.** Built ahead of the crafting need, same
  situation as Rock below and the pre-existing "Copper Ore" item —
  Ben's explicit call when extending the Boulder→chunk→refined-material
  tier structure to copper, not an oversight. Nothing currently turns
  Copper into anything (no smelting/refining recipe exists), and no
  recipe consumes it as an ingredient either. Worth deciding what
  Copper is actually *for* (a Bronze-tier alloy ingredient? a currency
  material distinct from Copper Coin?) before it reads as forgotten
  dead content the way Rock/Wood below already do.
- [ ] **"Rock" item (`MediumRock.asset`) is now completely orphaned.** Side
  effect of the v0.1.90-dev change making Boulder's chunk punchable
  instead of directly pickupable (per Ben's request): `MediumRockChunk.
  prefab` used to be a `Pickup` granting 1 Rock, now it's a
  `ResourceNode` that breaks into 2 Small Rock instead and never grants
  Rock at all. Confirmed via guid search — nothing in the project
  references `MediumRock.asset` anymore (no recipe ever did, per the
  entry below this one). Same situation the Wood item used to be in
  before it was removed outright (v0.1.136-dev, Ben's call — the
  Stick/Plank material line covers that role, Wood was redundant) —
  worth deciding whether to delete `MediumRock.asset` outright too, or
  give "Rock" a real purpose (a crafting ingredient? a coarser material
  than Small Rock for some recipe?) before it reads as forgotten dead
  content.
- [ ] **Can't eat a Berry.** Reported by Ben during playtest, 2026-08-07.
  Root cause confirmed via investigation: the data wiring is actually
  correct (`Berry.asset`/`BerryEdible.asset` match, and
  `PlayerEating.edibles` has `BerryEdible` wired in) — the bug is that
  `InventoryScreen.DrawInventorySection`'s "Eat" button
  (`InventoryScreen.cs`) is only drawn for items sitting in the **main
  inventory list**, which iterates `playerInventory.Inventory.Slots`
  specifically. A freshly picked-up Berry never lands there — `Pickup.
  Complete()` routes it through `PlayerLoot.Receive()`, which stashes
  plain items into a **hand** slot first. Hand/backpack slots are drawn
  by `DrawEquipmentSection`/`DrawContainerContents`, and both only offer
  the generic "where should this go?" move popup — no Eat option exists
  there at all. A player has to know to manually move the Berry "To
  Inventory" via that popup before an Eat button ever appears, which
  isn't discoverable and just reads as "can't eat it." Same underlying
  gap as the already-logged "Eat directly from a container" item below —
  this is really that bug, just hit for the first time via a real edible
  pickup rather than found in code review.
- [ ] **Chunks/bonus-chunks spawned by `ResourceNode.SpawnChunk` can be
  un-pickupable if their prefab expects `Pickup.Configure()`.** Reported
  by Ben during playtest, 2026-08-07, as "when I chop the tree, if it
  spawns a branch, I can't pick it up" (the new 30% bonus-Stick chance on
  chopping a Log, v0.1.83-dev). Root cause confirmed: `Stick.asset`'s
  `worldPickupPrefab` is `StickPickup.prefab`, whose `Pickup` component
  has `item: {fileID: 0}` baked in — by design, it's meant to be filled
  in at runtime via `Pickup.Configure(item, quantity)`, which today is
  **only** ever called from `PlayerDropping.SpawnPickup()`. `ResourceNode.
  SpawnChunk()` (used for both the guaranteed `chunkPrefab` and the new
  `bonusChunkPrefab`) just does a plain `Instantiate(prefab, position,
  Random.rotation)` — it never calls `Configure()`. With `item` left
  null, `Pickup.Complete()` calls `PlayerLoot.Receive(null, ...)`, which
  immediately no-ops, so the spawned object can never be picked up.
  **This is a latent bug for any future `ResourceNode.chunkPrefab`/
  `bonusChunkPrefab` that (like `StickPickup`) relies on runtime
  `Configure()` rather than a hardcoded `item` field** — it happened not
  to matter before now because every existing chunk prefab
  (`WoodChunk`/`RockChunk`/`PlankChunk`/etc.) hardcodes its `item`
  directly in the asset instead. Fix is likely either: give
  `ResourceNode.SpawnChunk` an item-aware overload that calls `Configure`
  when the spawned prefab has a `Pickup` with a null `item`, or simply
  avoid pointing `bonusChunkPrefab`/`chunkPrefab` at `Configure()`-style
  prefabs and use hardcoded-item prefabs (like `WoodChunk`) instead.
  **Hit again, 2026-08-07 (v0.1.117-dev):** built `CopperChunk.prefab`
  (the refined-Copper chunk spawned when a Copper Ore chunk breaks)
  following `StickPickup`'s empty-`item`/`Configure()` convention
  instead of `RockChunk`'s hardcoded-`item` one — same bug, same
  symptom ("can't pick up the smaller blocks", reported live during
  playtest). Confirms this isn't a one-off risk; it's the default
  failure mode any time a new chunk prefab is built by copying the
  *wrong* one of these two established patterns. Fixed locally by
  hardcoding `item` directly on `CopperChunk.prefab` (option 2 above),
  but the underlying systemic gap — `ResourceNode.SpawnChunk()` still
  never calls `Configure()` — remains unfixed, and `StickPickup` as a
  Log's `bonusChunkPrefab` is still affected by it.
- [ ] **The two `TreeBranch_PolyByGoogle` instances in the scene are
  still non-interactive decoration.** Follow-up to the "only the
  procedural Tree is choppable" report from Ben's 2026-08-07 playtest —
  Big Tree by 3Donimus got `ChoppableTree` in v0.1.91-dev (see
  `CHANGELOG.md`), but the two `TreeBranch_PolyByGoogle` instances
  placed for visual comparison during art exploration
  (`THIRD_PARTY_CREDITS.md`) still have no script component at all.
  Not necessarily a bug — a Tree branch is a much smaller prop than a
  full tree, chopping it may not make sense — but worth an explicit
  decision either way so it doesn't read as an oversight.
- [ ] **Berry Bush searching — random 0-4 berry yield, plus a rare "super
  success" chance of a Berry Seed.** Ben's idea, 2026-08-07: "search the
  berry function... random chance of finding up to 4 berries.
  additionally, a super success chance of finding a berry seed." Not
  investigated or scoped yet — open questions for whenever this gets
  picked up: is this a new "Search" interaction distinct from however
  Berries are gathered today, does the 0-4 yield replace or sit alongside
  the existing gather path, and does a Berry Seed imply Berry Bushes
  becoming plantable/farmable eventually (a real new system) or just a
  rare collectible for now. Same "chance of a bonus item" shape as the
  Log's Stick chance (`ResourceNode.bonusChunkPrefab`/`bonusChunkChance`,
  v0.1.83-dev) might be directly reusable here, if Berry Bush searching
  turns out to fit the same punch-based `ResourceNode` model — worth
  checking before building something new. *(Reported by Ben.)*
- [ ] **Procedural tree (v0.1.58-dev) doesn't read as a tree yet.** Confirmed
  via screenshot: `GenerateTree.cs`'s branching mesh renders and is visible
  (the untested backface-culling safety net wasn't even needed, or at least
  didn't hide anything), but the result looks wrong in three specific ways:
  - **Proportions read as a pole with a ball stuck on top**, not a tree. The
    trunk barely tapers and stays near-vertical for most of its height —
    lateral branch spread (32° max deviation per split, `RandomConeDirection`
    in `GenerateTree.cs`) only becomes visually obvious in the last couple of
    generations right below the canopy, because each generation's segments
    are shorter than the last (0.62–0.8× length falloff per level) — spread
    needs to happen gradually up the whole tree, not compress into the top.
  - **Foliage reads as a cluster of grapes/balloons**, not a canopy — the
    sphere clusters at each branch tip are too separated; they need to
    overlap into one rounded mass (larger radius and/or tighter placement
    per cluster, or bigger spheres with more overlap between adjacent tips).
  - **Bark color renders pale grey-tan instead of the brown actually set**
    (`TreeBark.mat`'s `_BaseColor` is `(0.32, 0.20, 0.11)`). Suspect but
    unconfirmed: the new procedural sky (v0.1.55/57-dev) may be contributing
    more ambient light than the old default skybox did, washing out
    unrelated materials — worth checking `RenderSettings` ambient source/
    intensity before assuming the material itself is wrong.

  *(Reported by Ben, deferred rather than iterated on immediately —
  "we will have to work on the trees.")*
- [ ] **Crafted items land in the plain main inventory instead of a free hand
  or an equipped container's slot.** Surfaced 2026-08-05 when Ben crafted a
  Pickaxe and couldn't find it — it wasn't missing, `PlayerCrafting.TryCraft`
  had correctly placed it in the main inventory's "uncategorized" list
  (verified: recipe/item data all wired correctly, this is a real behavior,
  not a data bug). Ben's expectation was that it should've gone to a free
  hand or an equipped container's inventory slot instead, matching the
  intended end state of "Simplify item-holding to two states" below — that
  item is about the *pickup* path specifically (Backpack → free hand →
  inventory slot → drop), and *crafting* output was never actually brought
  in line with it; `TryCraft` has unconditionally targeted the main
  inventory since before this session, documented as intentional at the
  time (see the Crafting-tab test-plan section). Logging as a bug now since
  that's no longer the wanted behavior — fix should route crafted output
  through the same equip-or-store priority once "Simplify item-holding to
  two states" is built, rather than hardcoding straight to main inventory.
  *(Reported by Ben.)*
- [ ] **No way to move an equipped item (e.g. Canteen) into a backpack.**
  `InventoryTransfer.Move`/`Inventory.AddEquipmentItem` already support carrying an
  equipment reference into any `Inventory`, backpack included, but no UI path ever
  calls it for an equipment-backed slot: `DrawEquipmentSection` draws an
  `entry.equipment != null` slot as a plain `GUILayout.Box` (not a `Button`, so
  it's not clickable at all), and `DrawInventorySection`'s equipment branches
  (Backpack/Canteen/NavigationComputer/PersonalHealthMonitor/Sunglasses) only ever
  offer Equip/Drop — unlike the plain-item branch, there's no "To Backpack"/"To
  Storage". Affects every equippable, not just the Canteen. *(Reported by Ben.)*
- [x] **Only one worn container's contents show in the Inventory tab's side
  column at a time — fixed v0.1.124-dev, refined v0.1.125-dev.** Surfaced
  2026-08-06 building Belt (`CHANGELOG.md` v0.1.75-dev), confirmed via
  playtest 2026-08-07 ("when you equip the belt, the backpack
  disappears"), and hit again 2026-08-08 testing the Crude Fiber Belt's
  new attachment points (a Canteen equipped to the Belt was invisible
  because the Backpack, also worn, was winning). `InventoryScreen.
  GetWornContainer()` returned only the first worn `IInventoryHolder`
  found (Back beat Waist); replaced with `GetWornContainers()` returning
  all of them. `DrawContent()` first rendered one bordered panel per
  worn container side by side (v0.1.124-dev), then merged into a single
  "Inventory" panel with one preview+contents row per container stacked
  inside it (v0.1.125-dev, Ben's call after seeing the two-panel look).

- [x] **Backpack — folded into the 5-tier CraftTier ladder, capacity scales
  by tier — shipped 2026-08-06, see `CHANGELOG.md` v0.1.75-dev.** Grew out
  of the Belt discussion just below: same "container capacity scales with
  crafted tier" idea, applied to Backpack.
  - **Renamed**, not just cosmetic: `"Rough Backpack"` → plain `Backpack`
    (Normal, no prefix, per `CraftTierNames`' convention), alongside new
    `Crude Backpack`/`Rudimentary Backpack`/`Fine Backpack`/`Masterwork
    Backpack` `ItemDefinition`s.
  - **Capacity curve, shipped as designed:**

    | Tier | Capacity |
    |---|---|
    | Crude | 4 |
    | Rudimentary | 6 |
    | Normal | 8 |
    | Fine | 12 |
    | Masterwork | 16 |

  - **Update, v0.1.134-dev:** all 5 tiers now have a real world pickup
    (grass-basket model, `IconBaker`-baked icons) — Ben's call to go
    ahead and wire the models even though real per-tier recipes still
    don't exist. **Recipes for Crude/Rudimentary/Fine/Masterwork
    Backpack specifically are still NOT built** — only reachable via
    Admin spawn or a future recipe. The Normal tier is craft-adjacent
    only via the separate `Leather Backpack` (new item, not this
    ladder) and `Crude Fiber Backpack` (also not this ladder). Still
    holding off on real Backpack-ladder recipes until there's an actual
    Fiber → Cloth / Leather material progression to gate tiers on,
    rather than 4 recipes that all cost the same placeholder materials
    with nothing but a name distinguishing them.
  *(Reported by Ben.)*
- [ ] **`LeatherBackpackRecipe.asset` (new, v0.1.134-dev) uses placeholder
  ingredients (6x Cloth + 4x Rope) — explicitly temporary.** Ben's
  direct call: build the recipe shape now, swap in real
  Leather/hide-tanning materials once that chain exists (no raw
  "Leather"/"Hide" material exists in the game yet — no
  hunting/skinning system built). Don't read the current ingredient
  list as a design decision; it's a placeholder standing in until a
  real material exists to replace it.
- [x] **Belt — new equippable, worn at Waist, holds generic attachment
  points instead of a normal inventory — shipped 2026-08-06 (Normal tier
  only), see `CHANGELOG.md` v0.1.75-dev.**
  - Equipping a Belt occupies the `Waist` slot in `PlayerEquipment`, which
    replaces Canteen's old direct-to-Waist fallback — a bare Canteen's
    carry locations are now Left Hand → Right Hand → the equipped Belt's
    attachment points, not the body's Waist slot directly.
  - Attachment points are **generic**, not typed — any attachment
    (Canteen, Knife Scabbard, Pouch, Holster) consumes exactly 1 point
    regardless of kind.
  - Point count scales with the Belt's own `CraftTier`, hand-picked (like
    Lockbox) rather than fit to the existing `CraftTierScale.Modifier()`
    ratio, since 2→12 doesn't match that curve: Crude 2, Rudimentary 4,
    Normal 6, Fine 9, Masterwork 12. **Normal tier renamed to `Fiber
    Belt` 2026-08-07 (v0.1.79-dev)** — establishes "Fiber Belt" as the
    ladder's actual base name (not just "Belt"), and **`Crude Fiber Belt`
    shipped the same day** — first tier with a real recipe (8x Fiber, 2
    points, trains Sewing), and the first-ever crafted equippable that
    actually works (see the equippable-crafting-output fix in the
    Textiles/Leather item below). Rudimentary/Fine/Masterwork Fiber Belt
    still don't exist. **Update, v0.1.140-dev:** the Normal-tier `Fiber
    Belt` item itself (`BeltItem.asset`, the standalone placeholder
    behind this rename, never given its own real model) was removed
    outright — Ben's call, redundant with `Crude Fiber Belt` which
    already has real content. This ladder's remaining open question
    (Rudimentary/Fine/Masterwork) is now moot unless the ladder concept
    gets revived under a different base tier.
  - **Attachments, in the order they'd likely get built:** Canteen (built
    — can now carry on a belt point as an alternative to a hand), Knife
    Scabbard (holds exactly 1 Knife, any tier, nothing else), Pouch (grants
    1-3 general-item storage slots — sized independently of the Belt's own
    tier, so a Crude Belt can carry a 3-pocket Pouch), Holster (deferred —
    no ranged/melee weapon exists yet to holster).
  - **Explicitly open, not decided:** whether attachments themselves get
    quality tiers that change their function, not just belt-slot
    occupancy — Ben's example: a higher-tier Canteen could hold more
    water than a Crude one. Same question would presumably apply to
    Scabbard/Pouch/Holster once those exist (does a Masterwork Scabbard do
    anything a Crude one doesn't?). Left as a question for whenever
    attachments actually get built, not resolved now.
  - **Ties into Encumbrance (design-brief.md Phase 1, not built —
    `ItemDefinition` has no weight field yet):** once carried weight
    affects movement/stamina, belt capacity stops being a free number —
    a bigger Belt is presumably heavier to wear, and a full Canteen/loaded
    Pouch weighs more than an empty one. Gives a real capacity-vs-mobility
    trade instead of just "more slots is strictly better." Same logic
    applies to Backpack's flat 8 slots once weight exists. Third lever
    already named in design-brief.md's Phase 1 encumbrance item, not new
    here: carry capacity/movement efficiency also improve as
    Strength/Athletics grows through use (Pillar 2's skill-via-use model)
    — so a heavy belt+pouch loadout is viable for a character who's
    trained for it, not just a flat gear tax on everyone equally.
  *(Reported by Ben.)*
- [x] **Equip destination picker for multi-slot equippables — shipped
  2026-08-06, see `CHANGELOG.md` v0.1.76-dev.** Ben's follow-up right
  after Belt landed: Canteen can now go to Left Hand, Right Hand, or a
  worn Belt's points, and clicking Equip silently picking the first match
  isn't good enough. `PlayerCanteen`/`PlayerNavComputer`/
  `PlayerHealthMonitor` (the only 3 equippables with more than one
  possible destination) gained `AvailableDestinations`/`EquipTo`; a new
  popup in `InventoryScreen.cs` shows the real options when there are 2+,
  and still equips immediately with 0 or 1 (no needless click for
  Backpack/Belt/Sunglasses/Mining Face Shield, which only ever have one
  destination each). **Related but NOT the same fix as** "No way to move
  an equipped item into a backpack" and "Equip directly from a container"
  (both under Bugs, above) — those are about different actions (moving an
  already-equipped item elsewhere, and equipping straight from a
  container's contents) and are both still open. *(Reported by Ben.)*
- [ ] **Fiber → Cloth textile chain, and a way to source Leather — needed
  before Backpack/Belt (or any future Sewing-discipline item) can get real
  recipes.** Ben's call, 2026-08-06, made mid-build on the Backpack/Belt
  retier: rather than faking their recipes with placeholder ingredients
  (Stick/Wood, the way the tool tiers did as pure scaffolding), hold off
  until there's an actual textile/leather material web. Ties directly into
  the still-open "full material web beyond wood/stone (metal, textiles)"
  gap already logged under "Full crafting/gathering/skills redesign"
  below, and gives the empty `Sewing` skill (exists as a `SkillDefinition`,
  zero recipes train it today) its first real reason to exist.
  **"Where Fiber comes from" answered 2026-08-07 (`CHANGELOG.md`
  v0.1.77-dev):** all 5 `TrimmedStick` recipes now also yield 1 Fiber
  (guaranteed, flat across tiers) — trimming a branch with a Knife leaves
  you with usable fiber alongside the Trimmed Stick. Ben's framing: "if we
  use the rock knife on the tree branch... outcome would be maybe some
  fiber and the trimmed stick." **Rope/Cloth recipes shipped 2026-08-07
  (`CHANGELOG.md` v0.1.78-dev):** `RopeRecipe` (5x Fiber → 1 Rope) and
  `ClothRecipe` (10x Fiber → 1 Cloth), both training `Sewing` directly
  (skillGain 2, no intermediate step) — the first two recipes to ever
  populate that skill. **First real starter gear shipped 2026-08-07
  (`CHANGELOG.md` v0.1.79-dev):** `Crude Fiber Belt` (8x Fiber, 2 points)
  and a new, distinct `Crude Fiber Backpack` (15x Fiber, capacity 4) —
  see the Belt and Backpack entries below for the full detail. Also
  required fixing `PlayerCrafting.TryCraft` so a crafted equippable
  actually works (see the Admin-spawn-tab entry above — same root cause,
  only the crafting side is fixed). **Still open:** where Leather comes
  from (implies hunting/animals, which don't exist at all yet — or some
  other source?), and Rudimentary/Fine/Masterwork tiers of either new
  Fiber item.
  *(Reported by Ben.)*
- [x] **Skill-gated crafting tiers — shipped 2026-08-07, see
  `CHANGELOG.md` v0.1.80-dev.** Ben's call: use skill level 1/10/25/50/100
  to denote the 5 `CraftTier`s. Real bootstrap deadlock caught before
  building: skills start at 0, and the only way to gain most disciplines
  (Stonework/Woodworking/Sewing) is crafting the exact items this gate
  would restrict — requiring Crude ≥ 1 would make a fresh character
  unable to ever craft a first item in that discipline at all. **Resolved:
  Crude requires 0** (no real gate, same as today), curve applies from
  Rudimentary up: Rudimentary 10, Normal 25, Fine 50, Masterwork 100.
  - New `CraftTierScale.SkillRequirement(tier)`, alongside the existing
    `Modifier(tier)`. `PlayerCrafting.HasRequiredSkill(recipe)` checks
    `recipe.trainedSkill`'s current level against it (recipes with no
    `trainedSkill`, e.g. the 5 gadgets, are unaffected — same as
    `HasRequiredTool`'s pattern). Wired into `TryCraft` and
    `CraftingScreen`'s enabled/label logic (`— requires Stonework 25`,
    same style as the tool-requirement label).
  - **Real bug caught before it shipped:** `Rope`/`Cloth` never had an
    explicit `tier` set, silently defaulting to `CraftTier.Normal` —
    would have required Sewing ≥ 25 just to make basic Rope, breaking the
    very recipes meant to build up Sewing in the first place. Fixed by
    setting both to `tier: 0` explicitly (they're single-tier items with
    no real ladder, so Crude/0 — meaning "no gate" — is the correct
    value, not a real tier claim).
  - Verified via a scripted read-back of all 34 recipes confirming every
    tier's required level resolved correctly, not just that individual
    values parsed.
  - **Immediate effect:** the previously-documented "known, expected
    placeholder behavior" of all 5 tool tiers being craftable side by
    side with nothing gating the player (see the Knife/Hammer/Axe/Pickaxe
    entry below) is now real gating, not a placeholder — a fresh
    character can only craft Crude tools until Stonework reaches 10.
  *(Reported by Ben.)*
- [x] **Knife/Hammer/Axe/Pickaxe across all 5 CraftTiers — shipped
  2026-08-05, see `CHANGELOG.md` v0.1.69-dev.** Originally scoped 2026-08-04
  as "six base tools" (including Spear and Bow); a planning pass the next
  day resolved several open forks before building:
  - **Spear and Bow deferred entirely**, not part of this batch — neither
    has a function yet (no combat/damage/projectile system exists
    anywhere), and Bow's design-brief recipe (Stick + Rope) needs the
    unbuilt Textiles chain (Fiber/Fabric/Rope, Sewing skill). Revisit once
    combat exists and there's a real reason to give them stats.
  - **Consolidated, not duplicated:** the existing `Rock Knife`/
    `Rock Hammer`/`Axe`/`Pickaxe` became the Crude tier in place (renamed,
    same GUIDs) rather than sitting alongside 30 brand-new parallel items.
  - **Recipes are identical across all 5 tiers of a tool for now** — pure
    scaffolding. **Skill-side gating shipped 2026-08-07 (`CHANGELOG.md`
    v0.1.80-dev):** crafting a given tier now requires trainedSkill at or
    above `CraftTierScale.SkillRequirement(tier)` (Crude 0, Rudimentary
    10, Normal 25, Fine 50, Masterwork 100) — a real progression gate now
    exists. Ingredient-quality-side of weakest-link (below) still doesn't
    — every tier still costs identical ingredients, so skill is the only
    thing gating tier today, not material quality too.
  - **Skill wiring deferred, not guessed at:** all 20 recipes train the
    existing `Crafting` skill rather than inventing Woodworking/Stonework/
    Forging assignments now — raised during planning that a Hammer alone
    plausibly touches at least 3 different future skills, with no way to
    know today which is right. Revisit once the refining pipeline (which
    is what would actually exercise those skills) is built.
  - `Admin spawn tab — shipped 2026-08-05` (`AdminSpawnScreen.cs`, Admin
    tab on the `` ` `` menu) landed first specifically to make testing this
    batch easier. See the follow-up item just below for its one known gap.
    *(Reported by Ben.)*
- [ ] **Admin spawn tab can't spawn a working equippable gadget.**
  `AdminSpawnScreen` (shipped v0.1.68-dev) spawns any `ItemDefinition` via
  `PlayerDropping.SpawnPickup`, which instantiates `worldPickupPrefab` (or a
  generic fallback) and calls `Pickup.Configure`. That's correct for plain
  stackable items, but Backpack/Canteen/Sunglasses/Nav Computer/Health
  Monitor/Mining Face Shield are `IEquippable` carriers whose real physical
  form is a dedicated prefab, not the generic `Pickup` path — spawning one
  from the Admin tab today produces a plain, non-equippable inventory stack
  instead. Not urgent (those already have pre-placed world pickups near
  spawn per `TEST_FEATURE_PLAN.md` §7, and tomorrow's tool batch doesn't need
  it), but worth fixing if the Admin tab needs to cover gadgets too — likely
  means giving each one a real `worldPickupPrefab` pointing at its own
  carrier prefab instead of relying on the generic fallback.
  **Same root cause hit again from the crafting side, 2026-08-06 —
  crafting-side FIXED 2026-08-07 (`CHANGELOG.md` v0.1.79-dev), Admin-spawn
  side still open.** `PlayerCrafting.TryCraft` used to always call
  `inventory.AddItem(...)` too — also a plain stackable add with no
  `.equipment` reference, surfaced while scoping Backpack/Belt recipes
  (v0.1.75-dev). Fixed via a new `AddCraftedOutput` helper: when
  `recipe.outputItem.worldPickupPrefab` has an `IEquippable`, instantiate
  it stashed and add via `AddEquipmentItem` instead of `AddItem` — exactly
  the mechanism this entry originally speculated about, and what made
  `Crude Fiber Belt`/`Crude Fiber Backpack` (v0.1.79-dev) the first
  working crafted equippables. **`AdminSpawnScreen`/`PlayerDropping.
  SpawnPickup` is a separate code path and was NOT touched** — spawning a
  gadget from the Admin tab still produces a non-equippable stack today.
  Same underlying idea would fix it (check for `IEquippable` on
  `worldPickupPrefab` there too) but wasn't done as part of this pass.
- [ ] **Apply the Boulder/Rock hybrid shape technique to the ore nodes too,
  once the rock/boulder look itself is finalized.** Ben's explicit intent
  (2026-08-04) — the ore nodes (Copper/Iron/Silver/Gold/Platinum) are still
  plain Sphere primitives. Deliberately not done yet: waiting until the
  rock/boulder shape (displaced-mesh body + clustered pebbles, `CHANGELOG.md`
  v0.1.62/63-dev) is confirmed good, since ore would reuse the exact same
  `GenerateDisplacedSphere`/`BuildClusteredRock`-style technique rather than
  reinventing it. Note the hidden-ore nodes (Silver/Gold/Platinum) would need
  this applied to *both* their hidden and revealed materials/meshes.
- [ ] **Spawn a starting Pickaxe and Axe in the world for now.**
  Ben hit this directly playtesting the ore work: `Crude Pickaxe`/`Crude Axe`
  (renamed from `Pickaxe`/`Axe` in v0.1.69-dev, same recipe) are craft-only
  today (2 Small Rock + 1 Stick / 1 Small Rock + 2 Stick), with no world pickup
  instance anywhere — unlike Backpack/Canteen/Sunglasses/Nav Computer/Health
  Monitor, which all have at least one pre-placed near spawn so a fresh
  playthrough doesn't have to craft everything from zero before it can do
  anything. Bare-handed Rock Node mining still works without a Pickaxe, so
  gathering the materials to craft one isn't actually blocked — but the friction
  of needing to know that and craft one first before ever touching ore is real.
  A stopgap for now ("for now" — Ben's words), not necessarily a permanent
  design call once the fuller skills/tools pipeline lands: place one Pickaxe
  **and** one Axe (confirmed — both, same bootstrapping situation) as world
  pickups near the other starter gear. *(Reported by Ben.)*
- [ ] **Full crafting/gathering/skills redesign — partially built.** See
  `docs/design-brief.md`'s **Crafting, Gathering & Skills Pipeline (2026-08-04,
  amended 2026-08-05)** section for the complete plan: 7 new refining skills
  (Mining, Woodworking, Stonework, Metalworking, Forging, Minting, Sewing),
  alongside existing Gathering — **8 total**, `Crafting` having retired as a
  distinct skill on 2026-08-05 (see next) — a weakest-link tier rule
  (skill vs. material quality), a full gather→refine→assemble material web
  (wood, stone, metal, textiles), tool-quality effects (yield/quality/speed),
  and a new click-once-and-locked interaction model that replaces the current
  punch-to-break mechanic entirely. Large, cross-cutting, and *decided in shape
  but not in exact numbers* — several sub-questions are explicitly still open
  (see that section's own "Still open" list).
  **New 2026-08-05:** a planning conversation following the tool-tier work
  above resolved three more open questions — every finished item now sorts
  into exactly one discipline skill by its *defining* material (not every
  ingredient); crafting an item trains that broad discipline *and* a narrow
  per-item proficiency together, with the broad skill also gating recipe
  unlocks, not just `CraftTier`; and a new, separate weapon-usage skill tier
  (Archery/Spear/Sword/Gun/Bare-handed) was named for whenever combat/hunting
  eventually exists. See the design-brief section for the full reasoning.
  **The discipline-sort half shipped same-day, v0.1.70-dev:** `Crafting`
  retired, 6 new discipline `SkillDefinition`s created, all 25 recipes
  repointed (20 tools → Stonework, 5 gadgets → no skill), and both
  `CraftingScreen`/`SkillsScreen` got sub-tabs to make the now much-longer
  lists navigable. **Still purely design, nothing built:** the narrow
  per-item-proficiency track (no data structure exists for it yet) and the
  weapon-usage skill tier (needs a combat system that doesn't exist).
  **First real material-web step shipped v0.1.71-dev:** Stick + Knife (held,
  not consumed) → Trimmed Stick, trains Woodworking — the first thing to
  ever populate that tab. `CraftingRecipe` gained a `requiredTools[]`/
  `requiredToolLabel` pair for this (a tool held but not consumed, distinct
  from `ingredients`), same "any tier counts" convention as
  `ResourceNode.requiredTools`.
  **Shipped so far:** the full ore ladder (Iron/Silver/Gold/Platinum Ore Nodes)
  and the Mining Face Shield hidden-ore detection mechanic (visual reveal +
  yield gating both, not just the visual half) — v0.1.60/61-dev. Also
  **Boulder + Rock** (v0.1.62-dev) — the new stone size tier (Boulder → Rock →
  Small Rock) got its shapes built (a hybrid displaced-mesh-body-plus-pebbles
  look) and Boulder → Rock wired through the *existing* punch mechanic, but
  **Rock → Small Rock is still not built** — that specific refinement step (a
  recipe? a separate mineable object? never decided) remains exactly as open
  as it was when the tier was first discussed. See `CHANGELOG.md`.
  **Interaction model shipped v0.1.147-dev** — `IPunchable` is deleted;
  `ResourceNode`/`ChoppableTree` now use hold-and-release `IInteractable`
  (Ben's call over the design-brief's original tap-once-and-locked version —
  simpler, and the hold-progress plumbing already existed), with duration
  read from the player's live skill tier via `CraftTierScale.HoldDuration`/
  `TierForSkillLevel`. **Two real gaps left even within this piece:** tool
  tier doesn't speed up the hold on top of skill tier yet (the design-brief's
  own "Tool-quality effects" promise, not implemented), and the Crafting
  screen's own instant "Craft" button is still untimed — a different UI
  surface (menu-driven, not world-raycast) that needs its own progress/cancel
  affordance, deliberately deferred rather than folded into the same pass.
  **Still not built:** the Mining skill itself as an actual `SkillDefinition`
  (nodes currently still train `Gathering`, per what already existed, not the
  newly-decided `Mining` split — that decision hasn't been wired into code yet,
  confirmed again during the v0.1.147-dev work — every `ResourceNode` still
  points `trainedSkill` at `Gathering`),
  three of the six discipline skills (Metalworking/Forging/Minting —
  Woodworking, Stonework, and Sewing all now have real actions training them
  as of v0.1.78/79-dev), the **ingredient-quality half** of the weakest-link
  `CraftTier` determination (the **skill half** shipped 2026-08-07,
  v0.1.80-dev — see the Knife/Hammer/Axe/Pickaxe entry above), the full
  material web beyond wood/stone (metal, textiles — though Fiber/Rope/Cloth
  are now a real start, v0.1.77/78-dev), and the randomized-size-on-spawn/
  yield-scaling design for Boulder/Rock and Rock → Small Rock refinement.
  Don't start implementing any further piece of this without
  re-reading the full design-brief section first — it's too
  interlocking to build from memory of this one-line summary.
- [ ] **Sky texture could use another pass.** Procedural cloudy skybox shipped
  v0.1.55-dev through v0.1.57-dev (`GenerateSkyTexture.cs`, throwaway —
  `Assets/Data/Sky.mat` + `Assets/Textures/SkyTexture.png` are the persistent
  result) — gradient direction and basic visibility are now correct per Ben's
  screenshots, but clouds are still sparse/barely present in a normal view
  ("this is fine for now," not "this is done"). Worth another round of tuning
  cloud coverage/density and possibly the cloud shape/softness if it comes up
  again — same tileable-noise technique as the grass texture, see
  `CHANGELOG.md`'s v0.1.55/56/57-dev entries for the full history of what was
  tried (including the inverted-gradient bug already fixed) before changing
  anything.
  **Likely root cause identified 2026-08-04, not yet applied here:** while
  fixing the ore textures, found that `Mathf.SmoothStep(low, high, rawValue)`
  doesn't threshold anything the way GLSL's `smoothstep` does — see `CLAUDE.md`'s
  new gotcha on this exact bug. The sky's cloud-coverage code used the identical
  pattern, so the persistently-faint clouds across three tuning rounds were very
  likely this, not a frequency/contrast problem. Try the corrected
  `SmoothThreshold` helper from that gotcha before anything else. *(Reported by
  Ben.)*
- [ ] **Simplify item-holding to two states: equipped or inventory-stored — no
  ad-hoc "held in a hand" third state.** Today `PlayerLoot`'s pickup priority is
  Backpack → Left Hand → Right Hand → evict-into-world (`CHANGELOG.md`
  v0.1.10-dev/v0.1.15-dev), and a plain picked-up item can sit directly in a hand
  slot as an in-between state: not equipped (no Equip button was ever pressed)
  and not really "inventory" either. Requested target design:
  - Every object is always either **equipped** into a named equipment slot, or
    **stored** in an inventory slot (main inventory / backpack / storage box) —
    eliminate that third, ad-hoc "just sitting in a hand" holding state.
  - **Pickup — decided:** `PlayerLoot`'s existing Backpack-first priority stays
    unchanged. This rule fills the specific gap in the *current* fallback
    instead — today, when no backpack is equipped and both hands are already
    occupied by non-stacking items, the pickup evicts (physically drops)
    whatever's in Left Hand to make room. Replace that eviction with: route the
    new item into an inventory slot instead. Full resulting order: Backpack (if
    equipped) → a free hand (Left, then Right) → an inventory slot → drop to the
    ground if the inventory is also full (picking something up should never
    silently fail or destroy value).
  - **Unequip:** the item goes to an inventory slot; if every inventory slot is
    full, drop it to the ground instead of failing. (`PlayerBackpack.Unequip`
    already has this exact fallback chain — extend the same guarantee to every
    equippable: Canteen, Sunglasses, NavigationComputer, PersonalHealthMonitor.)
  - **Manual drop from inventory:** unchanged — goes straight to the ground.

  *(Reported by Ben. The despawn timer on dropped items that was originally
  part of this same request shipped separately in `v0.1.48-dev` (15 min),
  shortened to 2 min and extended to cover equipment/coins too in
  `v0.1.85-dev` — see `CHANGELOG.md` for both. Still doesn't cover the
  equipped-item unequip-fallback drop path described above, since that
  path isn't built yet either — despawn now covers every *existing* drop
  action, not this still-hypothetical one.)*
- [ ] **Equip directly from a container.** Same underlying gap as "Eat/Drink
  directly from a container" below — `DrawContainerContents` (backpack contents
  and storage boxes alike) treats every item as a generic move-popup button
  regardless of `entry.equipment`, so an equippable item sitting in a backpack
  (Sunglasses, a spare Canteen, Navigation Computer, Personal Health Monitor) has
  no direct Equip button; it has to be moved out to a hand or the main inventory
  first. *(Reported by Ben.)*
- [ ] **Eat directly from a container.** Food items sitting in a backpack (or other
  container) can't be eaten in place today — `DrawInventorySection` in
  `InventoryScreen.cs` gives main-inventory items a direct "Eat" button via
  `PlayerEating.FindEdible`/`TryEat`, but `DrawContainerContents` (used for a worn
  backpack's contents and nearby storage boxes) only offers the generic "where
  should this go?" move popup for every item, edible or not. Player has to move food
  out to the main inventory first.
- [ ] **Drink/fill directly from a container.** Same gap for a Canteen sitting in a
  backpack/container — no direct Drink/Fill buttons there, only the generic move
  popup (which, as of 2026-08-03, correctly preserves the equipment reference when
  used, but still requires moving the canteen out before it can be used).
