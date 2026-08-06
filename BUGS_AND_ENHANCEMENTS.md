# Bugs & Enhancements

Known issues and requested features not being worked right now. Not a replacement
for `WORKING_ON.md` (that's for active work) or `CHANGELOG.md` (that's for shipped
work) — this is the backlog between the two. Check off and move the entry to
`CHANGELOG.md` once it's actually fixed/built.

## Bugs

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

## Enhancements

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
    scaffolding, no real progression gate yet. See the weakest-link item
    below for what's still needed to make tiers actually mean something.
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
  **Still not built:** the Mining skill itself as an actual `SkillDefinition`
  (nodes currently still train `Gathering`, per what already existed, not the
  newly-decided `Mining` split — that decision hasn't been wired into code yet),
  four of the six discipline skills (Metalworking/Forging/Minting/Sewing —
  Woodworking and Stonework both now have real actions training them),
  the weakest-link `CraftTier` determination itself, the full material web beyond
  wood/stone (metal, textiles), the randomized-size-on-spawn/yield-scaling/duration-scaling design
  for Boulder/Rock, Rock → Small Rock refinement, and the new click-and-locked
  interaction model (everything still uses the old instant-hold-E/punch
  mechanics). Don't start implementing any further piece of this without
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

  *(Reported by Ben. The 15-minute despawn timer on dropped items that was
  originally part of this same request shipped separately in `v0.1.48-dev` —
  see `CHANGELOG.md` — and doesn't yet cover the equipped-item unequip-fallback
  drop path described above, since that path isn't built yet either.)*
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
