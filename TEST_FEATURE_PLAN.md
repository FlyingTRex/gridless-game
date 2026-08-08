# Test Feature Plan

A manual playtest checklist covering every shipped feature, meant to be run inside
a live Play-mode session (or a build) — not a substitute for the batch-mode compile
check, which only proves the code builds, not that it plays correctly.

**Convention — keep this file current:**
- Any commit that ships new player-facing behavior adds a checklist entry here in
  the matching (or a new) section.
- Any commit that changes existing behavior updates the matching entry's Steps/
  Expected so it still describes reality — a stale step is worse than none, same
  principle as `WORKING_ON.md`.
- Any commit that fixes a bug found via this checklist should add a short
  **Regression** line under the relevant entry so the next full pass re-checks it
  specifically, not just the happy path.
- This is the *manual* test plan — it doesn't replace `BUGS_AND_ENHANCEMENTS.md`
  (backlog of known/open issues) or `CHANGELOG.md` (what shipped and why).

**How to run a full pass:** open `TestScene`, press Play, and work top to bottom —
later sections assume nothing from earlier ones is left in a broken state. Each
section can also be run standalone when only one feature changed. Player spawns at
`(0, 1.05, 0)`; landmark coordinates below are approximate — if something's moved,
fix the coordinate in this file rather than assuming the step is wrong.

---

## 0. Setup

- [ ] Project opens directly into `TestScene` on a fresh clone (no blank `Untitled`
  scene). **Expected:** ground plane, player, and world objects visible immediately
  on Play.
- **Startup scene trimmed 2026-08-06** (Ben's call, to declutter): the
  5 Coins, Navigation Computer, Personal Health Monitor, Sunglasses,
  and the larger Storage Box no longer spawn by default — removed from
  `TestScene.unity`, not disabled. Sections below that test these will
  fail at their stated coordinates; use the **Admin** tab (`` ` `` menu)
  to spawn any `ItemDefinition`-based one instead.
  (Mining Face Shield and the Silver/Gold/Platinum Ore Nodes were also
  trimmed here, but got rebuilt and re-added to the scene in v0.1.120-dev
  — see that section below, no longer affected by this trim.)
- [ ] Bottom-left debug panel shows `Gridless <version>` matching `CHANGELOG.md`'s
  "Current version" line and `FirstPersonController.GameVersion`.
- [ ] **Ground texture (v0.1.53-dev, tiling fixed in v0.1.54-dev):** the ground
  reads as a mottled green grass texture (dark/mid/light patches with fine
  blade-like detail), not a flat solid green. Walk/look across a wide stretch,
  ideally from an elevated or angled view (that's what made the original bug
  obvious). **Regression:** v0.1.53-dev's first pass showed a clearly visible
  repeating checkerboard/waffle grid — should now read as continuous mottled
  ground with no obvious seam lines or identically-repeating blob shapes. Some
  amount of large-scale pattern repetition is inherent to any tiled texture at
  a fixed tile size; flag it specifically if it's still distracting rather than
  subtle.
- [ ] **Sky (v0.1.55-dev; visibility fixed in v0.1.56-dev; gradient direction
  fixed + clouds sharpened in v0.1.57-dev):** looking toward the horizon at a
  normal level pitch should show a *pale, lightly saturated* blue near the
  horizon getting *deeper* blue as you look up toward the zenith — real
  atmospheric-haze direction, pale near the ground, deep overhead. Clouds
  should read as distinct white blobs with reasonably crisp edges, not a soft
  blurry brightening, and should be clearly present at a normal level-pitched
  view (not just near the zenith). Turn a full circle checking for a visible
  vertical seam line clouds don't cross smoothly.
  **Regression history on this one entry — check all three:**
  1. v0.1.55-dev: no clouds visible at all from a level view, no visible
     gradient — just a flat pale wash.
  2. v0.1.56-dev: clouds still not reading as shapes (one soft blurry glow),
     and the gradient direction was backwards — deep blue *at* the horizon,
     fading pale going up, opposite of realistic haze.
  3. v0.1.57-dev (current): both should now be corrected — confirm the
     gradient direction specifically, since that one was actually inverted
     rather than just weak.
  Also reconfirm no pink rendering (the original shader-compatibility risk
  from v0.1.55-dev).
## 1. Movement & Stances

- [ ] WASD moves the player relative to camera facing; mouse look rotates
  camera/body (pitch clamped so you can't flip past vertical).
- [ ] **Sprint:** hold Left Shift while moving with Stamina ≥ 85% — speed
  increases, Stamina drains at 10/s. Below 85%, Shift gives no speed bonus (drains
  at the 2/s walk rate instead, not the sprint rate).
- [ ] **Jump:** Space while grounded and Standing — costs a flat Stamina amount
  (`jumpStaminaCost`, default 10) per jump, not per-second.
- [ ] **Stance keys** (Standing is default): **X** toggles Kneel (0.4× speed), **C**
  toggles Crawl (0.2× speed), **Z** toggles Prone (0.1× speed, slower than Crawl).
  Pressing the active stance's key again returns to Standing; pressing a different
  stance's key switches directly to it (mutually exclusive). Sprint and Jump are
  disabled in all three non-Standing stances.
- [ ] Current stance shows in the bottom-left debug panel and updates immediately.
- [ ] **Stamina-gated speed** (Standing only): 10%–85% stamina = normal speed, no
  sprint bonus; <10% = half speed; 0% = 10% speed (very slow limp).
- [ ] **Stamina regen** only climbs while stopped, Kneeling, Crawling, or Prone —
  confirm it holds flat (doesn't regen) while walking Standing, even without Shift.

## 2. Vitals & Stamina Decay

- [ ] `VitalsBarHUD` (always-on, bottom-center 2×2 grid: Health/Stamina top,
  Hunger/Thirst bottom) is visible with no equipment required. Bar fill reflects
  value/150 (so a stat at 100 fills two-thirds, not the whole bar — this is
  intentional headroom, not a bug).
- [ ] Hunger and Thirst tick down over real time while doing nothing.
- [ ] Health drains when either Hunger or Thirst hits 0; regenerates when both are
  reasonably fed.
- [ ] Eating a Berry (see §4) restores Hunger; drinking (see §7 Canteen, §8 Water
  Source) restores Thirst.
- [ ] **Overdrinking:** fill Thirst above 100% via repeated Drink actions — allowed
  up to 125%. Above 125%, "SICK: Overdrank water!" warning appears (bold red, via
  `PlayerHealthMonitor`, requires the Personal Health Monitor equipped — see §7) and
  Health drains at 5/s. Thirst itself drains fast (10/s) while sick, so sickness is
  self-limiting — confirm it actually clears once Thirst drops back to 50% and
  Health stops draining.

## 3. Player Menu (Tab) — Inventory Tab

- [ ] **Tab** opens/closes the full-screen Player Menu (Player/Inventory/Skills/
  Crafting tabs across the top); **Escape** also closes it (and re-locks the
  cursor) — the two never disagree about open/closed state.
- [ ] Tab only opens while the cursor is already locked (can't stack on top of
  another open screen — try pressing Tab while Bank/Lockbox/rename/Game Menu is
  open; nothing should happen).
- [ ] **Player tab** is intentionally blank right now (just a header) — same
  placeholder treatment as the `` ` `` menu's Player tab, not a bug.
- [ ] Clicking the **Inventory** tab: main inventory list (4 slots) shows carried items with Eat/Drink (if
  edible/drinkable), Craft (if a known recipe), Drop, To Pack, To Storage buttons
  as applicable.
- [ ] Equipment section lists all 14 slots (Head, Face ×2, Neck, Chest, Back, Left/
  Right Arm, Left/Right Wrist, Left/Right Hand, Waist, Leg, Feet) — empty ones show
  "Empty", occupied ones show the item name plus Equip/Unequip/Drop as applicable.
- [ ] **Worn container side column (v0.1.67-dev):** equipping a container
  (Backpack) into Back shows its box reading **"Equipped"** (not the item
  name) and its contents grid appears in a separate column to the right of
  the equipment list, not inline underneath the Back row. **Regression
  check:** every other equipment slot row (Left Arm, Right Arm, etc.) stays
  at a fixed, uniform height regardless of whether a Backpack is worn —
  confirm nothing shifts position when you equip/unequip it. Clicking an
  item in the side column still opens the move popup (Drop / To Left Hand /
  To Right Hand / To Inventory / To Storage — options only show if not
  already the source).
- [ ] **"QTY: N" label + icon overlay in contents grid slots (v0.1.108-dev,
  icon rendering fixed v0.1.109-dev, empty-slot text dropped
  v0.1.110-dev):** in a worn container's contents grid or a nearby
  StorageBox's, each occupied slot shows a small "QTY: N" label
  directly below it, matching that slot's actual count (Small Rock
  stacks are the easiest test — put several in a backpack). Should be
  **blank** (no "QTY:" text, not "QTY: 1") for a non-stackable item,
  still reserving the same row height either way. Confirmed working
  by Ben: the Small Rock icon renders correctly in its slot (fixed in
  v0.1.109-dev after an earlier version silently dropped the icon and
  truncated the text instead — see CHANGELOG.md). **Empty slots in
  this grid are a plain gray box with no "Empty" text** — this is
  scoped to the contents grid only; the equipment slot list's own
  "Empty" labels (Head/Face/Neck/...) are unchanged, still show the
  word. The main Inventory list and move popup are also unaffected.
  **Regression caught by Ben immediately after v0.1.110-dev:** the
  first version of the empty-box change rendered as literally
  nothing — no visible box at all, capacity impossible to gauge —
  because `GUI.skin.box`'s default runtime look had too little
  contrast to show up without text inside it. Fixed in v0.1.111-dev
  with an explicit `DebugGUI.Slot` background color, now used for
  both empty and occupied slots — confirm every slot in the grid
  (empty or occupied) reads as a clearly visible gray box against the
  panel behind it.
- [ ] **Hover tooltip on icon-only slots (v0.1.112-dev):** hovering the
  mouse over a contents-grid slot that shows only an icon (Small
  Rock, once it has any) should pop a small floating label near the
  cursor with the item's name — confirm it appears/disappears as the
  mouse enters/leaves the slot, and that it's positioned near the
  actual cursor (not clipped or stuck somewhere else). Slots without
  an icon (still showing text) should NOT show a tooltip — redundant
  there, name's already visible.
- [ ] Currency row (5 boxes: Copper/Iron/Silver/Gold/Platinum) shows live wallet
  balances; clicking a box opens a quantity popup (±1/±10/All + Drop) — dropping
  spawns physical coins in front of the player that scatter and don't fall through
  the ground.
- [ ] When within `storageRange` (10m) of one or more Storage Boxes, a third
  section auto-appears showing the nearest box's contents.
- [ ] **Full-screen layout (2026-08-04):** Inventory content (and its move/
  coin-drop popups) now renders inside the Player Menu's full-screen area
  instead of its own floating window — the previous 50%-scale (`GUI.matrix`)
  boost from v0.1.50-dev was dropped since the menu itself is already much
  larger than the old floating panel. Flag if text/buttons read as too small
  now that it shares space with the tab bar — the scale can be reintroduced
  for this tab specifically if so.

## 4. Gathering & World Interaction

- [ ] **Sticks** (E, instant pickup) go straight to inventory/hands per the loot
  priority below.
- [ ] **Stick visual (v0.1.73-dev):** both pre-placed "Stick Pickup" world
  objects and any freshly-dropped Stick should render as the imported
  branch model (real bark texture, natural shape), not the old plain box.
  Check it sits at a reasonable size/orientation lying roughly along the
  ground — flag if it looks stretched, floating, or rotated oddly, since
  this was a hand-computed scale/rotation fit, not an exact art pass.
- [ ] **Hold-and-release gathering/chopping (v0.1.147-dev, replaces
  punch-to-break entirely — `IPunchable` deleted):** every entry below that
  says "punch"/"hits to break" is superseded by this. Hold E on a Rock
  Node/Boulder/Ore Node/Tree/Log/ore chunk — a green progress bar fills
  under the crosshair prompt; releasing E (or looking away) resets progress
  to 0 with no effect. Duration is skill-tiered, not fixed: check the
  Skills tab for your current Gathering (or the relevant discipline)
  level, expect roughly 3s at Crude tier down to 0.5s at Masterwork (exact
  thresholds in `CraftTierScale.SkillRequirement`/`HoldDuration`). Tool
  presence still gates whether it works at all where required, but does
  **not** currently speed up the hold — that's a known, not-yet-built gap.
- [ ] **Rock Node** breaks into 3 physical Small
  Rock chunks that scatter and can be picked up individually — hold E until
  the bar fills, doesn't complete instantly.
- [ ] **Rock Node visual (v0.1.86-dev):** should render as the Poly Pizza
  "Stone" model (CC-BY), not the old plain grey sphere — check it sits
  on the ground without floating or sinking, and that its footprint is
  roughly where the old sphere's collider was (same hold/interact range
  as before, collider itself unchanged).
- [ ] **Rock Node position (moved v0.1.88-dev):** now at `(-2, 0.35, 8)`,
  not `(-2, 0.35, 3)` — moved further from Boulder (~4.48 units apart
  now vs. ~2.24 before) after a playtest report that Boulder/Rock Node/
  Big Tree were crowding each other at game start.
- [ ] **Rock Chunk visual (v0.1.87-dev):** the Small Rock pieces that
  scatter when Rock Node breaks should render as a smaller, distinctly
  different-proportioned version of the Stone model (not a plain grey
  sphere, and not just a miniature clone of Rock Node's shape — check it
  looks like a genuinely different chunk, not identical geometry at a
  different size). Should still physically collide/be pickupable at
  roughly the same size as before.
- [ ] **Rock texture (v0.1.59-dev, superseded by the Stone model swap
  above for Rock Node/Rock Chunk specifically):** this entry originally
  covered a shared procedural "RockTexture" material — Rock Node and
  Rock Chunk now use the imported Stone model's own material instead, so
  judge them by the new model's look, not this texture. Boulder/Rock
  (the bigger tier) may still be relevant here if untouched — check
  what's actually using `RockTexture.png` before assuming this entry
  still applies as originally written.
- [ ] **Small Rock chunk shape (v0.1.62-dev):** broken/dropped Small Rock pieces
  should read as a rounded sphere shape, not an obvious cube. **Regression:**
  `RockChunk.prefab` was a plain scaled Cube from the very start of the
  project — should now look and collide (roll, not slide/skid on flat faces)
  like a sphere.
- [ ] **Stick icon (v0.1.114-dev):** wherever a Stick appears in
  inventory UI it should show a small rendered picture (a thin
  branch/twig, matching the actual `StickPickup.prefab` model) next
  to its name/count instead of text-only. First item baked via the
  new `IconBaker` tool rather than a bespoke script — same visual
  quality expected as the earlier hand-scripted icons.
- [ ] **Small Rock icon (v0.1.107-dev):** wherever a Small Rock appears
  in inventory UI (main inventory list, an equipment slot if somehow
  equipped, a container's contents grid, the move popup header) it
  should show a small rendered picture (pale rock/pebble shape,
  matching the actual `RockChunk.prefab` model) next to its name/count
  instead of text-only. Second item in the game with an icon, after
  the Backpack — confirm every other item without one still looks
  unchanged (text-only).
- [ ] **Boulder (v0.1.62-dev, visual replaced v0.1.87-dev)** at
  `(-4, 0.6, 4)`: now renders as the "Rock by Quaternius" model (public
  domain), not the old procedural displaced-mesh-plus-pebbles shape —
  confirm it's visible from every angle, sits at roughly the same spot/
  depth as before (grounded to match the old visual's exact footprint,
  not just eyeballed), and reads as one cohesive rock, not disconnected
  pieces. Bare-handed holding works (no tool required, same as Rock
  Node) — hold E until the bar fills, scatters 3 Rock chunks nearby (see
  next entry — as of v0.1.90-dev these are hold-interactable, not a direct
  pickup).
  **Confirmed working (v0.1.88-dev):** an earlier playtest reported
  being unable to break the Boulder at all, traced to it standing too
  close to Rock Node (~2.24 units); moving Rock Node to `(-2, 0.35, 8)`
  in v0.1.88-dev resolved it — Ben broke the Boulder successfully on
  retest.
- [ ] **Rock chunk (`MediumRockChunk.prefab`, hold-interactable as of
  v0.1.90-dev):**
  the pieces that scatter when Boulder breaks render as the Stone model
  (CC-BY, fixed v0.1.89-dev after a regression where they still showed
  the old grey fused-pebble cluster) and are **not directly pickupable**
  — walking up to one should NOT offer a "Pick up" prompt. Holding E on one
  (bare-handed) breaks it into 2 **Small Rock**, same as Rock
  Node's own break. Confirm the chunk physically launches/settles when
  Boulder first breaks (still has its `Rigidbody`), and that holding on a
  settled chunk doesn't require any tool.
- [ ] **Chunk scatter distance (v0.1.63-dev):** breaking a Boulder or Rock Node
  should scatter chunks with a visible initial burst that settles down
  quickly nearby, not chunks that keep rolling/bouncing far away from the
  break point. **Regression:** v0.1.62-dev's Small Rock/Rock chunks (right
  after the Cube→Sphere shape swap) rolled much farther than intended —
  `MediumRockChunk.prefab`'s `Rigidbody` damping had never actually been set
  (near-zero), and `RockChunk.prefab`'s existing damping was tuned for its
  old Cube shape, insufficient for a freely-rolling Sphere.
- [ ] **"Rock" item (`MediumRock.asset`, orphaned as of v0.1.90-dev):**
  no longer spawned or consumed anywhere — Boulder's chunk now breaks
  straight into Small Rock instead of granting a Rock item first (Ben's
  call). Should NOT appear anywhere in inventory, crafting, or Admin's
  item-spawn list behaving as if it still has a purpose; if you spot it
  referenced anywhere, that's stale. See `BUGS_AND_ENHANCEMENTS.md` for
  the open question of whether to delete or repurpose it.
- [ ] **Big Tree by 3Donimus is choppable (v0.1.91-dev — now the game's
  only tree, after the procedural Tree was removed entirely in
  v0.1.126-dev)** at `(10, 3.99, 10)`: requires an Axe in hand (prompt
  reads "Hold to chop (requires Axe)", holding bare-handed does nothing —
  same tool-gating as ore nodes). A completed hold drops 3 `Log` instances
  scattered
  nearby with physics (should tumble briefly then settle, not roll away
  indefinitely). Chopping trains **Gathering**. The tree fully
  disappears when chopped (no stump visual) and reappears after ~180s
  (long wait — consider temporarily shortening `ChoppableTree.
  regrowDelay` to verify without the full wait).
  **Confirmed working (v0.1.92-dev):** the first version's
  `CapsuleCollider` had a math error placing it ~3.6 units above the
  actual tree (floating in the canopy/above it), which Ben caught by
  testing — the hold never registered. Fixed by matching the
  collider's world-space Y range directly against the tree's measured
  renderer bounds. Confirm holds now register normally when aimed at the
  visible trunk.
- [ ] **Log chopping (v0.1.83-dev; Plank gets a real model v0.1.137-dev):**
  each dropped Log also requires an Axe (same tool-gating). One completed
  hold
  should destroy the Log outright (not hide-and-respawn like other
  `ResourceNode`s — a Log is a one-off spawn, there's nothing to
  respawn) and drop 2 **Plank**. As of v0.1.137-dev, Plank should show
  a real wood-planks model (Quaternius, public domain) instead of the
  old grey Cube, plus an icon. There's also one sitting at `(6, 0.3, 2)`
  for a direct look without chopping anything first. Chopping a Log
  trains **Woodworking**, not Gathering — confirm the Skills tab
  reflects the split correctly (Tree chop → Gathering rises, Log chop →
  Woodworking rises). Roughly 3 in 10 Log chops should also drop a
  **Stick** (reusing the existing branch-model item, not a separate
  "Branch") — chop several Logs across a full playtest and confirm this
  is a real, visible chance, not always/never happening.
- [ ] **Copper Ore Node (v0.1.59-dev, real shape v0.1.117-dev, resized
  bigger v0.1.119-dev)** at `(2, 0.4, -4)`: holding E *without* a
  Pickaxe held in a hand does nothing (`Complete` no-ops, prompt reads
  "Hold to break (requires Pickaxe)"). With a Pickaxe in either hand,
  holding completes after the skill-tiered duration and breaks into 3
  Copper Ore chunks.
  This is a real irregular rock shape (`Rock_Quaternius.glb`, the same
  mesh Boulder uses), not the old plain sphere — texture should read
  as grey rock with scattered copper-orange flecks and occasional
  green patina spots distributed evenly across the surface (not one
  big smear or streak — that was a real bug caught and fixed before
  this ever shipped, see CHANGELOG.md v0.1.117-dev for the UV/tiling
  explanation). As of v0.1.119-dev it's noticeably bigger
  (`1.15x1.06x1.30`, up from matching the old sphere's `0.8` size) —
  confirm holding still works reliably at the new size (the collider
  was resized to match at the same time, fixing a separate gap where
  it had been left too small for the visual).
- [ ] **Iron Ore Node (v0.1.119-dev, same treatment as Copper):** at
  `(4, 0.4, -4)`, same real-mesh/texture treatment as Copper Ore Node,
  but deliberately **flatter and wider** (`1.50x0.85x1.60`) instead of
  matching Copper's proportions — should read as a visibly different,
  squatter silhouette standing next to Copper Ore Node, not a same-
  shape recolor. Same tool-gating (Pickaxe, skill-tiered hold) and texture
  check
  (dark rock with scattered rust-orange flecks, evenly distributed —
  same UV-tiling fix as Copper was applied here too).
- [ ] **Iron Ore chunk is now hold-interactable too, breaks into Iron
  (v0.1.119-dev):** same treatment as Copper Ore chunk → Copper —
  holding E on a scattered Iron Ore chunk (bare-handed) breaks it
  into 2 of a new **Iron** item. Built with the Copper pickup-bug
  lesson already applied, so this one should work correctly the first
  time — confirm the Iron pieces can actually be picked up. Both
  `IronOre` and `Iron` should show small icons wherever they appear in
  inventory UI. **Note:** Iron has no crafting recipe using it yet
  (see `BUGS_AND_ENHANCEMENTS.md`) — built ahead of the crafting need,
  not a bug.
- [ ] **Copper Ore chunk is now hold-interactable too, breaks into Copper
  (v0.1.117-dev, pickup bug fixed v0.1.118-dev):** the "Copper Ore"
  pieces that scatter when Copper Ore Node breaks are no longer
  directly pickupable — same treatment Boulder's Rock chunk got in
  v0.1.90-dev. Holding E on one (bare-handed) breaks it into 2 of a
  brand-new **Copper** item — confirm these Copper pieces can actually
  be picked up (E to interact) once they land; the first version
  spawned them permanently un-pickupable (`Pickup.item` left null,
  same bug class as the Stick-bonus-chunk issue in
  `BUGS_AND_ENHANCEMENTS.md`). Both Copper Ore Node's chunk and the
  new Copper chunk should render as the same rock-shape-plus-copper-
  texture family, each visibly smaller/differently proportioned than
  the tier above it (Ore Node > Copper Ore chunk > Copper). Both
  `CopperOre` and `Copper` should show small icons wherever they
  appear in inventory UI. **Note:** Copper has no crafting recipe
  using it yet (see `BUGS_AND_ENHANCEMENTS.md`) — built ahead of the
  crafting need, not a bug.
- [ ] **Knife/Hammer/Axe/Pickaxe now come in all 5 CraftTiers
  (v0.1.69-dev):** craftable from the Crafting tab as `Crude Knife` through
  `Masterwork Knife` (and same for Hammer/Axe/Pickaxe) — 20 recipes total.
  What used to be the single `Rock Knife`/`Rock Hammer`/`Axe`/`Pickaxe` are
  now specifically the **Crude** tier (renamed in place, same recipe as
  before: 2 Small Rock + 1 Stick for Pickaxe, 1 Small Rock + 2 Stick for
  Axe, etc.). **Known, expected placeholder behavior — not a bug:** every
  tier of a given tool currently costs the *exact same* ingredients (no
  weakest-link ingredient-quality rule yet) — only the skill side is
  gated now (see the Skill-gated crafting entry below), not material
  quality. Carrying any of them in a backpack/main inventory (not a
  hand) should **not** satisfy a tool requirement below — still has to be
  held in a hand (`PlayerEquipment.HasInHand`).
- [ ] **Crude Knife real visual + icon (v0.1.115-dev):** craft or find a
  Crude Knife — its world/held model should be a real AI-generated
  knapped stone knife (dark, textured blade with a handle/crossguard),
  not the old plain grey capsule placeholder. Should also show a small
  icon (a dark diagonal blade shape) wherever it appears in inventory
  UI. Note: it has a full handle despite the "Crude Knife" recipe using
  only Small Rock (no wood) — a known, accepted mismatch between the
  generated model and the recipe's implied bare-blade design, not a
  bug to fix.
  - **Other 4 Knife tiers matched up (v0.1.138-dev):** Rudimentary/
    Knife (Normal)/Fine/Masterwork Knife already had real recipes since
    v0.1.69-dev but showed nothing/a generic grey cube when crafted or
    dropped — should now all show the exact same knapped-stone-knife
    model and icon as Crude Knife (deliberately identical across tiers,
    same as every other tool — only the name/skill-gate differs, not
    the visual). Confirm all 5 tiers look pixel-identical, not subtly
    different sizes.
  - **All 5 Pickaxe tiers get a real model (v0.1.141-dev):** Crude/
    Rudimentary/Pickaxe (Normal)/Fine/Masterwork Pickaxe all had real
    recipes but no model/icon at all before this — craft or spawn any
    tier via Admin and confirm it shows a real pickaxe model (public
    domain, CreativeTrio), identical across all 5 tiers. Confirm
    holding one still satisfies every Pickaxe-gated `ResourceNode`
    (ore nodes, Boulder, Rock Node) exactly as before — only the visual
    changed, not the `ItemDefinition` guids those check against.
  - **All 5 Axe tiers get a real model (v0.1.142-dev):** same
    treatment — Crude/Rudimentary/Axe (Normal)/Fine/Masterwork Axe all
    show a real wood-handled axe model now (CC-BY, suerozcelik),
    identical across tiers. Confirm holding one still satisfies every
    Axe-gated `ResourceNode` (Tree, Log) exactly as before.
  - **All 5 Hammer tiers get a real model (v0.1.143-dev):** same
    treatment — Crude/Rudimentary/Hammer (Normal)/Fine/Masterwork
    Hammer all show a real stone-headed hammer model now
    (Tripo3D-generated, no credits needed), identical across tiers.
    Trimmed Stick still has no model — last one on the list.
- [ ] **Skill-gated crafting tiers (v0.1.80-dev):** on a fresh character
  (Stonework 0), only Crude Knife/Hammer/Axe/Pickaxe and Crude Trimmed
  Stick should be craftable — Rudimentary/Normal/Fine/Masterwork should
  show greyed out with a `— requires Stonework 10` (or 25/50/100, or
  `Woodworking`/`Sewing` for the other disciplines) label and Craft
  disabled, even with enough ingredients and the right tool in hand. Craft
  enough Crude items to push Stonework past 10 and confirm Rudimentary
  unlocks (Skills tab should show the rising level). Rope/Cloth and Crude
  Fiber Belt/Backpack should stay craftable from Sewing 0 — they're
  single-tier items with no real ladder, not actually gated despite
  defaulting to/being tagged with a `CraftTier` value. The 5 gadget
  recipes (Canteen/Sunglasses/Nav Computer/Health Monitor/Mining Face
  Shield) have no `trainedSkill` and should be completely unaffected —
  always craftable regardless of any skill level.
- [ ] **Skill-up messages (v0.1.81-dev):** every successful craft that
  actually raises a skill's level should show a brief (~3s) positive
  message top-center (e.g. "Congratulations! You have increased your
  Stonework skill to 4.0!") — wording should vary across repeated crafts
  (6 possible ordinary-gain lines), not always the same sentence. Craft
  enough Crude items to cross Stonework 10 (Rudimentary) and confirm the
  message changes to a distinct, more celebratory line mentioning
  "Rudimentary tier unlocked" — same for Normal (25), Fine (50), and
  Masterwork (100) if you push a skill that far. No special message
  should ever appear for "unlocking Crude" (threshold is 0 — nothing to
  cross). Message should sit just below the compass when a Navigation
  Computer is worn, never overlapping it. Craft twice in quick succession
  (before the 3s expires) and confirm the second message replaces the
  first rather than both showing at once. At MaxLevel (100, essentially
  unreachable in a normal playtest but worth noting) no message should
  appear since there's no real gain left to report.
- [ ] **Chance-of-creation crafting (v0.1.82-dev):** craft a batch of
  Crude tools/Trimmed Stick (low skill margin, riskiest odds — roughly
  63% Success / 20% Barely Fail / 12% Bad Failure / 3% Spectacular / 2%
  Brilliant) and confirm all 5 outcomes are actually reachable, not just
  Success: some crafts should silently succeed as normal (no message),
  some should show Bad/Spectacular Failure messages. **Crude specifically
  has nowhere lower to downgrade to**, so confirm it never shows a
  "Close, but not quite" downgrade message — only ever a plain Success or
  one of the other outcomes, even on a Barely Fail roll. Push a skill well
  past a tier's threshold (margin 20+, e.g. Stonework 30+ crafting
  Rudimentary) and confirm failures/downgrades get noticeably rarer and
  a "Incredible! You crafted a [higher tier item]" brilliant-success
  message becomes reachable, producing the next tier up in your
  inventory. **Bad Failure:** confirm ingredients are gone and nothing
  is added. **Spectacular Failure:** confirm ingredients are gone, you
  take visible health damage (check the vitals HUD if a Health Monitor
  is worn), and — only when crafting Trimmed Stick specifically (the
  only recipe with a required tool today) — the Knife held in your hand
  actually disappears from that hand slot. Crafting a tool (Knife/Hammer/
  Axe/Pickaxe) on Spectacular Failure should NOT break anything (no tool
  required for those recipes) — just materials lost + damage. Confirm
  Masterwork tools never show a "brilliant success" upgrade message
  (nowhere higher to go) even though the roll can still land there
  internally. Message should appear just below the skill-up message
  (`y=110` vs `y=70`) — craft something that both raises a skill's level
  AND has a notable chance outcome, and confirm both messages show at
  once without overlapping.
- [ ] **Tool gating now accepts any tier (v0.1.69-dev):** the Copper Ore
  check above, and every other Pickaxe/Axe-gated node below, should accept
  **any** of the 5 Pickaxe/Axe tiers held in a hand, not just one specific
  one — spot-check at least two different tiers (e.g. Crude Pickaxe and
  Masterwork Pickaxe) against the same node to confirm both work.
- [ ] **Silver/Gold/Platinum Ore Nodes (rebuilt v0.1.120-dev, mid-tier
  added v0.1.121-dev — the originals shipped v0.1.60-dev but were
  removed in the 2026-08-06 startup-scene trim; this replaces that whole
  section)** at `Silver Ore Node (6, 0.4, -4)`, `Gold Ore Node (8, 0.4,
  -4)`, `Platinum Ore Node (10, 0.4, -4)`: **without** a Mining Face
  Shield equipped, each should look and behave exactly like a plain
  Rock/Boulder node — same generic `Rock_Quaternius` grey rock texture,
  no visual hint anything's special. Sizes are deliberately distinct per
  metal (confirm the three read as different silhouettes, not
  same-shape recolors): Gold smallest (`0.70x0.65x0.72`), Silver medium
  (`1.00x0.95x1.05`), Platinum largest (`1.80x1.15x1.35`). **Equip the
  Mining Face Shield** (Face slot; craft it — 2 Small Rock + 1 Stick —
  or pick up the one sitting at `(6, 0.5, -6)`) and look at each node
  again: it should visibly change to that metal's `*OreRevealed`
  texture. **This is the important check:** mine one of these nodes
  *without* the shield equipped (Pickaxe in hand, shield off) — holding E
  should complete and yield **Small Rock** (via `RockChunk.prefab`
  as the `hiddenChunkPrefab`), not the real ore — the ore goes
  undetected. Put the shield on and mine a *different* instance of the
  same ore type (or wait for the 180s respawn) — this time it should
  yield 3 of the metal's hold-interactable ore chunk (`SilverOreChunk`/
  `GoldOreChunk`/`PlatinumOreChunk` — now the **mid tier**, matching
  Copper/Iron's structure, same `Rock_Quaternius`+texture treatment).
  If a hidden node ever yields real ore *without* the shield equipped,
  or plain rock *with* it equipped, that's the core mechanic broken,
  not a minor issue.
- [ ] **Silver/Gold/Platinum Ore chunk is now hold-interactable too, breaks
  into the actual Ore item (v0.1.121-dev)** — same treatment as Copper/
  Iron's mid tier: holding E on a scattered `SilverOreChunk`/`GoldOreChunk`/
  `PlatinumOreChunk` (bare-handed) breaks it into 2 of a new
  smaller final piece (`SilverOrePiece`/`GoldOrePiece`/
  `PlatinumOrePiece.prefab`), which is what actually grants the
  `SilverOre`/`GoldOre`/`PlatinumOre` item on pickup — confirm these
  pieces can actually be picked up (E to interact), same "built with the
  Copper pickup-bug lesson already applied" expectation as Iron.
  `SilverOre`/`GoldOre`/`PlatinumOre` should show small icons wherever
  they appear in inventory UI (previously text-only, then re-baked
  against this new final piece specifically). **Regression check —
  scatter/bounce:** v0.1.120-dev's first pass had pieces flying off
  before they could be reached (near-zero `Rigidbody` damping on the
  original pre-existing prefabs); fixed to match `CopperChunk`'s
  damping in v0.1.121-dev, **confirmed by Ben's playtest** — pieces
  now settle close to where the chunk broke instead of rolling/
  bouncing away. **Note:** unlike Copper/Iron, there's still no
  separate refined "Silver"/"Gold"/"Platinum" bar item beyond this — the
  final piece grants the same `SilverOre`/`GoldOre`/`PlatinumOre` item
  the mid-tier chunk used to grant directly in v0.1.120-dev, just gated
  behind an extra hold now.
- [ ] **Mining Face Shield now has a world pickup (v0.1.120-dev —
  previously craft-only)**: one sits at `(6, 0.5, -6)` in `TestScene`, a
  flattened dark disc/visor shape (placeholder primitive, no custom
  model generated for it yet). Picking it up and equipping/unequipping/
  dropping uses the same three-button pattern as every other Face-slot
  equippable (Sunglasses). Worn shield should be invisible from the
  player's own camera (same `WornEquipment` layer fix every other
  equippable already has) but visible on an external view. Should show
  a small icon in inventory UI.
- [ ] **Berry Bush (real model v0.1.139-dev)** at `(-1.5, 0.2, 1.5)` —
  should show a real strawberries model (CC-BY, Jarlan Perez), not the
  old grey Sphere placeholder, plus an icon wherever Berry appears in
  inventory UI. Picking a Berry gives a real inventory item (not an
  instant-eat-on-touch); Eat button only appears in the main inventory
  list, never in a backpack/storage contents view.
- [ ] **Loot priority:** with a Backpack equipped, new pickups go straight into
  it. With no backpack, pickups try Left Hand, then Right Hand; if both hands are
  full with non-stacking items, the new pickup evicts (physically drops, not
  deletes) whatever's in Left Hand.
- [ ] **Respawn:** Stick Pickup / Stick Pickup 2 and the Rock Node reappear ~3
  minutes after being fully taken/broken, at their original position plus a small
  random offset. (Long wait — spot-check the timer logic instead of waiting the
  full 3 minutes every pass, e.g. temporarily shorten `respawnDelay` if verifying
  the exact timing matters.)
- [ ] **Despawn (v0.1.48-dev, retimed + expanded v0.1.85-dev):** an item
  dropped via the inventory's Drop button, or via the hand-eviction
  fallback (both hands full with non-stacking items, no backpack
  equipped, picking up something new), disappears from the world after
  **2 minutes** (down from 15) if nobody picks it up. Confirm it does
  *not* apply to world-placed pickups (Sticks, Berry Bush) or
  `ResourceNode` chunk scatter (Logs, Planks, ore chunks, etc.) — those
  should sit indefinitely (or respawn per the item above), never
  silently vanish. Also confirm a *partial* pickup (leftover after your
  inventory fills up) keeps counting down from the original drop time,
  not reset by the partial pickup.
- [ ] **Despawn now also covers equipment and coins (v0.1.85-dev):** drop
  a worn Backpack/Belt/Canteen/Sunglasses/Nav Computer/Health Monitor/
  Mining Face Shield (via the Equipment section's Drop button) and
  confirm it also disappears after 2 minutes if left unpicked — this
  previously never despawned at all. Drop some coins and confirm the
  same. **Critical regression check:** drop a Backpack, pick it back up
  well within 2 minutes, then **equip it and wait past the 2-minute
  mark** — confirm it does NOT get destroyed while worn. This is the
  specific bug the fix was designed around (an already-expired timer
  firing the instant a re-equipped item reactivates) — if a worn
  Backpack vanishes mid-playthrough, this is the first place to look.
  Same check for an item that gets dropped, picked up, and left sitting
  in a hand (not re-equipped) past 2 minutes — should also survive.
  (Long waits — temporarily shorten the relevant `despawnDelay` field to
  verify exact timing rather than waiting the full 2 minutes every
  pass.)

## 5. Player Menu (Tab) — Crafting Tab

- [ ] Clicking the **Crafting** tab lists every known recipe (not just ones you currently have materials for),
  each showing every ingredient with "have N" counts.
- [ ] Craft button greys out when short on materials or when the main inventory
  has no room for the output — label appends "— inventory full" specifically when
  that's the blocking reason (not just insufficient materials).
- [ ] Crafting draws materials from the main inventory first, then an equipped
  Backpack, then nearby Storage Boxes (within range) in distance order — confirm
  a recipe reads "have N" correctly when materials are split across all three.
- [ ] Crafted output currently always lands in the main inventory, never the
  backpack or a free hand, even if the inputs came from there. **No longer
  considered correct as of 2026-08-05** — logged as a bug in
  `BUGS_AND_ENHANCEMENTS.md` (should route through the same equip-or-store
  priority as pickup once that's built), just not fixed yet.
- [ ] Spot-check at least one multi-ingredient recipe (Crude Hammer: 1 Stick + 1
  Small Rock) and one single-ingredient recipe (Crude Knife: Small Rock).
- [ ] **List now scrolls (v0.1.69-dev):** confirm the Crafting tab scrolls
  to reach the bottom entries of a long discipline instead of running off
  the screen, and that the tab bar/Close button stay fixed above/below the
  scroll area.
- [ ] **Discipline sub-tabs (v0.1.70-dev):** a second row of tabs —
  Woodworking, Stonework, Metalworking, Forging, Minting, Sewing, Other —
  sits below the "Crafting" header. All 20 Knife/Hammer/Axe/Pickaxe recipes
  (every tier) should appear under **Stonework** specifically, not spread
  across tabs or left in a default list. **Other** should hold the 5 gadget
  recipes (Sunglasses, Nav Computer, Health Monitor, Mining Face Shield,
  Canteen) — these no longer train any skill when crafted (confirm no
  skill level changes in the Skills tab after crafting one). **Woodworking**
  now has 5 recipes too (Trimmed Stick, v0.1.71-dev — see below); Metalworking/
  Forging/Minting/Sewing should still be empty and show "No recipes yet."
  rather than a blank panel or an error.
- [ ] **Tool-in-hand requirement (v0.1.71-dev):** the 5 Trimmed Stick
  recipes (Crude through Masterwork, under **Woodworking**) each need 1
  Stick *and* any tier of Knife held in a hand — the Knife is **not**
  consumed. Without a Knife in hand, the label reads `— requires Knife in
  hand` and Craft is greyed out even with a Stick available. Equip a Knife
  to a hand and it should read `[Knife in hand]` instead and Craft should
  enable (assuming a Stick is also available). Confirm the Knife is still
  in your hand — not consumed — after crafting. Crafting any tier trains
  **Woodworking** (check the Skills tab, Crafting Disciplines category).
- [ ] **Fiber byproduct (v0.1.77-dev):** crafting any tier of Trimmed
  Stick should show `Trimmed Stick + 1x Fiber  (needs ...)` in the recipe
  list, and produce 1 Fiber in the main inventory alongside the Trimmed
  Stick every time — guaranteed, not a chance. If the main inventory is
  full enough that the Fiber wouldn't fit (even if the Trimmed Stick
  alone would), the recipe should show "— inventory full" and Craft
  should stay disabled, same as any other space-check failure.
- [ ] **Rope/Cloth (v0.1.78-dev, Sewing tab):** with 5+ Fiber, `Rope`
  should show as `Rope  (needs 5x Fiber (have N))` and craft into 1 Rope;
  with 10+ Fiber, `Cloth` similarly craft into 1 Cloth. Both should be
  greyed out/uncraftable below their Fiber threshold, and both should
  train **Sewing** — check the Skills tab afterward to confirm it now
  appears with a nonzero level (see the Skills tab section below, this is
  the first thing that ever trains it).
  - **Cloth real visual + icon (v0.1.144-dev):** craft a Cloth and drop
    it — should appear as a real folded pale-cloth model (Tripo3D-
    generated, visible fold creases), not the old generic grey cube. It
    had no `worldPickupPrefab` at all before this. Should show a small
    icon wherever it appears in inventory UI.
  - **Fiber real visual + icon (v0.1.146-dev):** trim a Stick to get
    Fiber, then drop it — should appear as a real wispy grass-tuft
    model (public domain, Quaternius), not the old generic grey cube.
    Should show a small icon wherever it appears in inventory UI.
  - **"Woven Grass Cloth" (new item, v0.1.145-dev):** not craftable yet
    (no recipe) — spawn via Admin to check. Green-tinted variant of the
    same Cloth model/mesh, a separate standalone item for a future
    clothing material line. Confirm it shows its own green-tinted icon,
    distinct from plain Cloth's pale one.
- [ ] **Rope real visual + icon (v0.1.116-dev):** craft a Rope (5+
  Fiber, Sewing tab) and drop it — should appear in the world as a
  real AI-generated tan coiled bundle, not invisible/using some
  generic fallback shape (it had no visual at all before this). Should
  also show a small icon (a coiled tan shape) wherever it appears in
  inventory UI.
- [ ] **Crude Fiber Belt / Crude Fiber Backpack (v0.1.79-dev, Sewing
  tab; Crude Fiber Belt gets a real model v0.1.122-dev; placed in the
  scene v0.1.128-dev):** a `Crude Fiber Belt` now also sits at
  `(4, 0.3, 1.5)` as a world pickup, or craft one with 8+ Fiber — this
  should be the **first-ever crafted equippable that actually works**:
  check it lands in the main inventory as a real equippable (Equip
  button available, not a dead stackable entry), and equipping it puts
  it on Waist with 2 attachment points. As of v0.1.122-dev it should
  show a real green woven-grass ring model (Tripo3D-generated) both as
  a world pickup and worn, not the old flat grey box, plus a small icon
  wherever it appears in inventory UI. With 15+ Fiber, craft a `Crude
  Fiber Backpack` the same way — Equip should put it on Back with 4
  inventory slots; as of v0.1.133-dev it should show a real woven
  straw/grass basket model (Tripo3D-generated, brown leather straps +
  buckle) both as a world pickup and worn, not the old flat grey box,
  plus updated icon/previewIcon. The pre-placed `Backpack` should still
  work exactly as before too. **Note:** the old found "Fiber Belt" (the
  Normal-tier `BeltItem.asset`, a separate standalone placeholder near
  `(-2, 0.3, 1.5)`, never given its own model) was removed outright in
  v0.1.140-dev as redundant with `Crude Fiber Belt` — don't expect to
  find it anymore.
- [ ] **Backpack icons (v0.1.93-dev, corrected v0.1.94-dev):** there are
  two separate Backpack items, each with its own icon now — don't
  confuse them:
  - **`Backpack`** (`BackpackItem.asset`, the plain pre-placed one near
    `(0, 0.3, 2.5)`, visual is `Backpack.prefab`/`CrudeLeatherBackpack.glb`):
    icon shows visible straps, more detailed than the Fiber one.
  - **`Crude Fiber Backpack`** (`CrudeFiberBackpackItem.asset`, the
    Sewing-craftable one): icon is a simpler brown angular shape,
    matching its lower-detail model.
  Either way, confirm the icon appears next to the item wherever it
  renders — as an unequipped stack in the main Inventory list, in the
  Equipment section's "Back" slot box once equipped, inside a
  container's contents grid, and in the move popup's header. Every
  other item should look unchanged (still text-only, no icon). Confirm
  the icon is small and proportional (32x32 baked size) and doesn't
  distort any row/box/button size or overlap the Equip/Drop/Unequip
  buttons next to it.
- [ ] **Icon-only in every equipment slot (v0.1.95/96-dev):** any item
  with an icon shows icon-only (no text) everywhere in the Equipment
  section — a hand-held Backpack shows just its picture (not
  "Backpack"), and a worn Back/Waist container shows just its picture
  (not "Equipped"). Items without an icon (Belt, everything else)
  still show their old text exactly as before.
- [ ] **Equipment slot list + Back preview/contents panels, side by side,
  each with their own header (v0.1.95-dev through v0.1.106-dev — see
  CHANGELOG.md for the full iteration history):** two visibly bordered
  dark panels (`DebugGUI.Panel`) sit **side by side in one row** — no
  header above the row as a whole anymore. The left panel (equipment
  slot list: Head/Face/.../Back/..., always present) has its own
  **"Equipment"** header drawn inside it. The right panel (96x96
  preview icon — crisp/detailed, straps/buckle visible on the
  Backpack, not blurry — plus that container's own "___ contents"
  grid) has its own **"Inventory"** header drawn inside it, and only
  appears at all when something's worn on Back or Waist (a worn Belt's
  contents still show, just without a preview picture, since Belt has
  no icon yet). The **main inventory list above these two panels now
  has no header at all** — "Inventory" moved down onto the right
  panel instead, per Ben's call. Confirm there's a visible gap between
  the two panels, each sizes to fit only its own content, and the
  right panel disappears entirely (not just going blank) when nothing's
  worn on Back/Waist.
- [ ] **Folder-tab look (v0.1.70-dev):** the selected discipline tab should
  read as visually connected to the recipe list below it (matching
  background, no seam), while unselected tabs look visibly separate/
  receded behind it. Same visual language should now also appear on the
  top-level Player Menu tabs, the ` Game Menu tabs, and the Skills tab's
  category tabs below — check all four for consistency, not just this one.

## 6. Player Menu (Tab) — Skills Tab

- [ ] Clicking the **Skills** tab shows three category tabs — **Gathering**,
  **Crafting Disciplines**, **Combat** — each listing the skills in that
  category with their current level (0–100). `Crafting` no longer exists
  as a skill (retired v0.1.70-dev, see `CHANGELOG.md`) — don't expect to
  see it anywhere.
- [ ] **Gathering** tab: shows `Gathering` (and `Mining`, once that split is
  actually built — not yet, still just `Gathering` today).
- [ ] **Crafting Disciplines** tab: shows `Stonework` once you've crafted at
  least one tool (Knife/Hammer/Axe/Pickaxe, any tier), `Woodworking` once
  you've carved at least one Trimmed Stick (v0.1.71-dev), and `Sewing`
  once you've crafted a Rope or Cloth (v0.1.78-dev) — the remaining three
  disciplines (Metalworking, Forging, Minting) still won't appear at all
  until something actually trains them, which nothing does yet. If you
  haven't crafted anything, this tab should show "No skills trained yet."
  rather than an empty blank panel.
- [ ] **Combat** tab: always shows "No skills yet — combat/hunting isn't
  built." — there's no combat system and no weapon skills exist, so this
  tab can never have content today. Not a bug.
- [ ] Levels rise from relevant actions (gathering Sticks, breaking the Rock Node,
  crafting) with visibly diminishing gains as level rises — a handful of early
  actions shouldn't jump a skill anywhere near 100.

## 7. Equippable Gadgets

- [ ] **Backpack** (world pickup near spawn): Equip puts it on Back and exposes its
  8-slot contents grid; Unequip falls back to main inventory → a hand → world-drop
  if everything else is full (never no-ops); Drop removes it and its contents move
  with the physical object. Worn Backpack is invisible from the player's own
  camera (no first-person clipping) but visible to an external view.
- [ ] **Backpack visual (v0.1.74-dev; model switched leather → grass
  v0.1.134-dev):** the world-placed Backpack (and any dropped copy)
  should render as the woven grass basket model (Tripo3D-generated,
  brown leather straps + buckle), **not** the original crude-leather
  model anymore — that model moved to the new standalone `Leather
  Backpack` item instead (see below). Check it sits at a reasonable
  size/orientation and doesn't look stretched or float above the
  ground.
  - **`worldPickupPrefab` actually wired up (v0.1.132-dev):**
    `BackpackItem.asset` never referenced `Backpack.prefab` at all until
    now — confirm a *dropped* Backpack (not just the pre-placed one)
    also shows the real model, not a generic grey cube. Also replaced
    the pre-placed scene instance with a real `PrefabInstance` (it was
    a standalone copy before, same bug class as the Canteen fix in
    v0.1.128-dev) — its visual should be unchanged, just now wired
    correctly for future prefab edits.
- [ ] **Backpack retiered (v0.1.75-dev); all 5 tiers get a real
  model/icon/world pickup (v0.1.134-dev):** the world-placed Backpack
  reads as plain "Backpack" (not "Rough Backpack") — Normal tier,
  capacity 8. Crude (4)/Rudimentary (6)/Fine (12)/Masterwork (16)
  Backpack now all have a real grass-basket model, icon, and
  `worldPickupPrefab` too (spawn any of them via the Admin tab to
  check) — but **still no crafting recipe for any of the 4**, only
  reachable via Admin. Confirm equipping any tier puts it on Back with
  the right capacity for that tier.
- [ ] **Leather Backpack (new, v0.1.134-dev, craftable — 6x Cloth + 4x
  Rope, trains Sewing; own 5-tier ladder v0.1.135-dev):** uses the
  original crude-leather model that the Normal-tier grass `Backpack`
  used to have, in its own CraftTier ladder now (Crude 4 / Rudimentary
  6 / Normal 8 / Fine 12 / Masterwork 16, same curve as every other
  tiered container). Only Normal tier ("Leather Backpack") has a
  recipe — craft it, confirm it equips to Back correctly and shows the
  leather model + icon. The other 4 tiers have a real model/icon/
  `worldPickupPrefab` but no recipe yet — spawn via Admin to check.
  **Note:** the Normal tier's recipe ingredients are an explicit
  placeholder (no real Leather/hide material exists yet) — don't read
  them as a balance decision, just confirm the recipe itself actually
  works end-to-end.
- [ ] **Belt (new, v0.1.75-dev, world pickup near `(-2, 0.3, 1.5)`):**
  Equip puts it on Waist; Unequip/Drop follow the same fallback rules as
  Backpack. Placeholder flat-box visual only, no dedicated art yet. With a
  Belt worn, equip a Canteen — it should now be able to clip onto one of
  the Belt's 6 attachment points once both hands are already occupied
  (previously it would have gone to the body's Waist slot directly; that
  path no longer exists once a Belt is worn — see the Canteen entry
  below). **Display gap fixed v0.1.124-dev, merged into one panel
  v0.1.125-dev:** a Backpack (Back) and Belt (Waist) worn at the same
  time now both show their contents as two stacked rows inside the
  single "Inventory" panel (each with its own preview icon + "X
  contents" label), instead of only one winning and hiding the other.
  Confirm: equip both, drop an item into each, and check both rows stay
  visible and correctly labeled ("Backpack contents" / "Crude Fiber Belt
  contents" or similar) at the same time.
- [ ] **Canteen (v0.1.75-dev, no longer clips to bare Waist):** Equip to
  Left/Right Hand, or — only if a Belt is currently worn — one of its
  attachment points. Without a Belt worn, a Canteen with both hands full
  should fail to equip (previously it would have fallen back to Waist
  directly).
- [ ] **Canteen real model + world pickup (v0.1.127-dev; scene instance
  fixed v0.1.128-dev)** at `(-1, 0.3, 1.5)`: should be a real
  Tripo3D-generated metal canteen (cylindrical body, dark screw cap),
  not the old two-piece grey Cylinder placeholder. **Regression:** the
  pre-existing scene object here was a standalone embedded copy, not a
  real `Canteen.prefab` instance, so it kept showing the old placeholder
  even after v0.1.127-dev's model swap — confirm this specific instance
  now shows the real model, not just newly-crafted ones. Craft one too
  (3x Stick, see `CanteenRecipe.asset`) to confirm both paths match.
  Previously had no `worldPickupPrefab` at all (craft-only, couldn't be
  dropped and picked back up or spawned via Admin) — confirm it can now
  be dropped and picked back up correctly, and appears in the Admin
  spawn list. Should show a small icon wherever it appears in inventory
  UI. The empty/filled tint (see the Fill/Drink entry below) should
  still work correctly against the new single-mesh model — confirm the
  whole canteen tints blue when filled, gray when empty, not just part
  of it.
- [ ] **Canteen/Belt carry anchors (v0.1.123-dev — previously invisible
  when worn):** `PlayerCanteen`'s hand/belt anchors and `PlayerBelt`'s
  own carry anchor were never wired up — a worn Belt and anything
  equipped to a hand or the Belt's attachment points were parented at
  the player's exact pivot point instead of a real carry position,
  effectively invisible even though fully functional in the Equipment/
  contents UI. Now wired to the existing `HandAnchor`/`BeltAnchor`
  transforms on the Player. Confirm: a worn Belt is visible at roughly
  waist height on the body (not the player's feet/center); a Canteen
  equipped to Left Hand, Right Hand, or a worn Belt's attachment point
  is now actually visible from an external view in each case, not just
  logically equipped. **Partially confirmed:** Ben's retest showed the
  Belt equip working functionally (Waist row, Unequip/Drop present) but
  surfaced a *different* pre-existing bug — the Canteen didn't appear in
  the Inventory tab's contents panel because a Backpack was also worn,
  which hid the Belt's own contents panel entirely (fixed separately,
  v0.1.124-dev, see the entry above). Still worth a final pass
  confirming the Canteen is now visible both in-world (this entry) and
  in the contents panel (the entry above) at the same time.
- [ ] **Equip destination picker (v0.1.76-dev):** with both hands free
  *and* a Belt worn, clicking Equip on a Canteen sitting in the main
  inventory should pop up a small "Equip Canteen to:" list (Left Hand /
  Right Hand / Belt) instead of silently picking one — click one to
  commit, or Cancel to back out with nothing equipped. Same check for
  NavigationComputer/PersonalHealthMonitor with both wrists free (list
  shows Left Wrist / Right Wrist). With only one destination free (e.g.
  one hand already occupied), Equip should commit immediately with no
  popup — confirm that path still works too. Backpack/Belt/Sunglasses/
  Mining Face Shield are unaffected (each has only one destination) —
  their Equip buttons should still equip immediately, no popup.
  **Fill** only works within `fillRange` (2m) of a `WaterSource` (Water
  Puddle) — walking away and trying to Fill should fail/not appear. Filled Canteen
  visibly tints blue; empty is gray/neutral — check this both in the inventory
  panel and on the physical dropped object. **Drink** restores Thirst (see §2 for
  the overdrink ceiling).
  - **Regression (v0.1.46):** confirm the tint actually renders (was silently
    broken — `GetComponent<Renderer>()` on the root missed the child mesh
    renderers, and `Material.color` alone doesn't drive URP/Lit's `_BaseColor`).
  - **Blue glow, root cause fixed v0.1.131-dev:** the real metal canteen
    model uses glTFast's `Shader Graphs/glTF-pbrMetallicRoughness`
    shader, which uses `baseColorFactor`/`emissiveFactor` instead of
    Unity's usual `_BaseColor`/`_EmissionColor` — every tint/emission
    call had been silently no-op'ing against this specific model since
    v0.1.127-dev's model swap. **Confirmed working** — filled reads as
    a clear blue-navy tint against empty's neutral dark brown/black.
  - **Fill status in the contents grid (v0.1.129-dev):** with a Canteen
    clipped to a worn Belt's attachment point, its slot in the "X
    contents" grid should show `Water 100/100` (or `Empty`) in the same
    spot a stackable item's `QTY: N` normally sits — not blank.
  - **Lands upright when dropped (v0.1.129-dev):** drop a Canteen (empty
    or filled) from a height or onto uneven ground — it should always
    settle standing up, never tipped onto its side (rotation is frozen
    on X/Z, only free to spin around its own vertical axis).
- [ ] **Sunglasses** (near spawn, ~`(-3.5, 0.3, 1.5)`, or craftable from 1 Rock
  Knife): Equip to Face. While worn, a light silver screen tint overlay is visible;
  unequip/drop removes it immediately.
- [ ] **Navigation Computer** (near spawn, ~`(-1.5, 0.3, 0.5)`, or craftable):
  Equip to either wrist — a scrolling compass strip appears top-center with
  current horizontal speed underneath; unequip stops drawing it.
- [ ] **Personal Health Monitor** (near spawn, ~`(-3, 0.3, 1)`, or craftable):
  Equip to either wrist — shows the detailed Vitals panel (Health/Hunger/Thirst/
  Stamina/Body Temp), including the "SICK" overdrink warning from §2. No vitals
  detail panel should be visible without this equipped (the always-on
  `VitalsBarHUD` from §2 is the only baseline readout).
- [ ] **Moving an equipped item's slot** (e.g. backpack contents → main inventory
  via the move popup) never orphans it — Fill/Drink/Equip buttons should still
  work immediately after the move, not go dead.
  - **Regression (v0.1.45/46):** this exact bug shipped twice (Canteen, then
    Sunglasses) from `InventoryTransfer.Move` stripping the `equipment` reference
    — specifically re-test moving a Canteen and Sunglasses through every move
    path (hand ↔ inventory ↔ backpack ↔ storage), not just one.

## 8. Water Source Direct Interaction

- [ ] Standing near the Water Puddle with no water carrier equipped: prompt shows
  `[E] Drink` only.
- [ ] With an unfilled Canteen equipped: prompt shows both `[E] Drink` and
  `[F] Fill Canteen`.
- [ ] With a full Canteen equipped: `[F] Fill Canteen` disappears (nothing left to
  fill), `[E] Drink` still present.
- [ ] E drinks directly (restores Thirst) with no Canteen needed at all.

## 9. Storage Boxes

- [ ] Two boxes exist: **Storage Box** (20 slots, `(3, 0.25, 0)`) and **Small
  Storage Box** (10 slots, `(0, 0.2, -20)`).
- [ ] Looking at either box (no need to be in interact range) shows its
  `DisplayName` above the crosshair via `StorageBoxHover` (longer range than
  interact, ~20m).
- [ ] Within 10m, Inventory screen's third section shows the *nearest* box's
  contents; clicking "To Storage" (from the main list or a move popup) opens a
  picker listing every box in range by name, with Back/Cancel — not just the
  single nearest one.
- [ ] **Right-click** either box opens a rename text box (Enter/Save commits,
  Cancel/Escape discards); the new name shows immediately in the hover label and
  the "To Storage" picker.

## 10. Currency & Coins

- [ ] One world coin per type (Copper/Iron/Silver/Gold/Platinum) is placed near
  spawn. Picking one up (E) deposits straight into the wallet — no inventory slot
  involved — and destroys the world object.
- [ ] Wallet balance caps at 250 per coin type — picking up a coin at/above the
  cap leaves the (partial) remainder sitting in the world instead of destroying
  value for nothing.
- [ ] New character starts with 20 Copper, 5 Silver, 1 Gold already in the wallet
  (visible in the Inventory screen's currency row from the very first frame).
- [ ] Dropping coins from the currency row (§3) scatters physical coins on the
  ground that don't tunnel through it.

## 11. Banking

- [x] **Bank Box** (`(5, 0.3, -20)`, 5m from the Small Storage Box): E opens the
  Bank screen. *(Confirmed 2026-08-03 by Ben.)*
- [ ] **Window scale (v0.1.49-dev):** the whole Bank window — panel, text,
  buttons, and both the Deposit/Withdraw and Exchange popups — renders 50%
  larger than the base layout (480×620 scaled by 1.5x via `GUI.matrix`) and
  stays centered on screen. Buttons should still be clickable at the enlarged
  size (mouse hit-testing should track the scale, not the original small
  coordinates). `LockboxScreen` is unaffected — this only applies to the Bank
  window.
- [ ] Lists wallet vs. bank balance per coin type with Deposit/Withdraw buttons.
  New character starts with 25 Gold already in the bank (separate from the wallet
  purse).
- [x] **Deposit/Withdraw popup:** stepper buttons (±1/±10/Max), live fee preview
  (`max(1, ceil(3% of amount))` charged as an *extra* cost on the source side —
  depositing 100 costs 103 from the wallet, the bank receives exactly 100).
  Confirm/Cancel resolves the popup. *(Withdraw confirmed working with Gold,
  2026-08-03 by Ben; Deposit and other coin types not yet explicitly re-tested.)*
- [x] **Exchange section:** 8 buttons (both directions × 4 adjacent pairs:
  Copper↔Iron↔Silver↔Gold↔Platinum), clean 10:1 ratio, same fee model, rounds an
  upgrade's input down to the nearest multiple of 10 rather than ever producing a
  fractional coin. *(Gold→Silver confirmed working, 2026-08-03 by Ben; other
  seven direction/pair combinations not yet explicitly re-tested.)*
- [ ] **Modal popup guard — regression (v0.1.47):** with a Deposit/Withdraw/
  Exchange popup open, click a *different* row's Deposit/Withdraw button (or an
  Exchange button, or Buy on a Lockbox, or the panel's Close button) underneath
  it. **Expected:** nothing happens — the background panel is fully disabled while
  a popup is open. (Previously this silently reassigned the popup to a different
  coin type and reset the pending amount to 0.)

## 12. Lockboxes

- [ ] Bank screen's Lockbox shop section lists all 5 tiers with capacity and
  price, Buy button greyed out below the Gold cost:
  - Crude: 500/type, 2 Gold
  - Rudimentary: 1,250/type, 5 Gold
  - Normal: 2,500/type, 10 Gold
  - Fine: 5,000/type, 20 Gold
  - Masterwork: 12,500/type, 50 Gold
- [ ] Buying a Lockbox spawns it 2m in front of the *specific* Bank Box used to
  buy it (test from both Bank Boxes if more than one is ever placed) — not at the
  player's position.
- [ ] Buying two Lockboxes of the same tier creates two independent objects with
  separate balances (capacity doesn't pool).
- [ ] E opens a Lockbox's own screen: wallet vs. that box's balance per coin type,
  Deposit/Withdraw only (no Exchange, no fee on either direction).
- [ ] **Capacity/wallet-room clamping:** depositing more than the box's remaining
  space only adds up to that remaining space (e.g. 250 into a box with 200 space
  left adds exactly 200, refunds nothing lost). Withdrawing is capped by both what
  the box holds *and* what the wallet has room for under its 250 cap — e.g.
  withdrawing 1000 Gold should be impossible regardless of the box's balance if
  the wallet can't hold that much.
- [ ] Right-click renames a Lockbox same as a Storage Box; a renamed box's title
  shows correctly on the Lockbox screen and its prompt.
- [ ] **Modal popup guard — regression (v0.1.47):** same check as §11's last item,
  applied to the Lockbox screen's own Deposit/Withdraw popup.

## 13. Secret Message Wall (Easter Egg)

- [ ] Wall exists at `(0, 2.5, 8)`, blocks movement like any solid object.
- [ ] Looking at it *without* Sunglasses equipped: plain wall, no text.
- [ ] Looking at it *with* Sunglasses equipped and actually facing it (not just
  nearby): "Hell Yeah Brother!" appears in bold black text at the wall's
  screen-projected position.

## 14. Screen Management

- [ ] Only one of Player Menu/Bank/Lockbox/rename/Game Menu can be open at a
  time — opening one while another is open (via its hotkey or an E/right-click
  interaction) should not stack or corrupt state.
- [ ] Escape always closes whichever screen is open and re-locks the cursor,
  regardless of which screen it is or how it was opened.
- [ ] While any screen is open (cursor unlocked), WASD/Space/mouse-look do
  nothing to the player — including while typing in the rename text box (no
  accidental jump from typing a space).

## 15. Game Menu (` key)

- [ ] **`` ` `` (backtick/grave)** opens/closes a full-screen menu with 6 tabs
  across the top: Player, Audio, Graphics, Controls, Credits, Admin. Same
  rules as every other screen — Escape also closes it, and it only opens
  while the cursor is already locked (can't stack on another open screen).
- [ ] **Player tab** is intentionally blank right now (just a header) — not a
  bug, a deliberate placeholder pending a future decision on content.
- [ ] **Audio** and **Graphics tabs** each show a plain "nothing to configure
  yet" message — also intentional, since neither system exists in the game.
- [ ] **Controls tab** lists every real key binding, alphabetized by key name:
  `` ` ``, C, E, Escape, F, Left Mouse Button, Left Shift, Mouse Movement,
  Right Mouse Button, Space, Tab, WASD, X, Z — each with a plain-language
  description of what it does. Cross-check this against what's actually bound
  in the game right now; flag anything missing or stale (see `CLAUDE.md`'s
  standing rule to keep this list updated whenever a new key mapping ships).
- [ ] **Credits tab (updated v0.1.89-dev):** shows the credits image
  (`tekim_trex.png`) centered horizontally at the top, sized to 90% of
  screen width and also capped to 50% of screen height (whichever
  binds first, proportionally), then a single centered line reading
  "Tekim & The T-Rex" below it, then a "Third-Party Assets" section
  listing "Tree branch by Poly by Google [CC-BY] via Poly Pizza",
  "Stone by Poly by Google [CC-BY] via Poly Pizza", (as of
  v0.1.91-dev, once it became choppable and stopped being
  comparison-only) "Big Tree by 3Donimus [CC-BY] via Poly Pizza", (as
  of v0.1.137-dev) "Wood Planks by Quaternius [Public Domain] via Poly
  Pizza", (as of v0.1.139-dev) "Strawberries by Jarlan Perez [CC-BY]
  via Poly Pizza", (as of v0.1.141-dev) "Pickaxe by CreativeTrio
  [Public Domain] via Poly Pizza", (as of v0.1.142-dev) "Low Poly
  Axe by suerozcelik [CC-BY] via Poly Pizza", and (as of v0.1.146-dev)
  "Grass Wispy by Quaternius [Public Domain] via Poly Pizza" — exact
  text, not paraphrased (compare against
  `Assets/Models/THIRD_PARTY_CREDITS.md`).
  **Regression caught by
  Ben:** the v0.1.88-dev
  width-only sizing let the image grow tall enough to push the name
  line/attribution list/Close button off-screen with no way to scroll
  back — confirm the whole tab (image through Close button) stays
  visible at once.
- [ ] **Admin tab (v0.1.68-dev, Editor Play Mode only):** lists every
  `ItemDefinition` in the project alphabetically, each with a Spawn button.
  Clicking Spawn drops one directly in front of the player (same physical
  behavior as a manual inventory Drop) — pick a plain stackable item (e.g.
  Stick, Rock, Pickaxe) and confirm it appears ~1m ahead and can be picked
  up normally. **Refresh List** re-scans for newly-created items without
  needing to re-enter Play Mode. **Known gap:** spawning an equippable
  gadget (Backpack/Canteen/Sunglasses/Nav Computer/Health Monitor/Mining
  Face Shield) produces a plain inventory stack that can't actually be
  equipped — not fixed yet, see `BUGS_AND_ENHANCEMENTS.md`.
