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
  5 Coins, Secret Wall, Navigation Computer, Personal Health Monitor,
  Sunglasses, Mining Face Shield, Silver/Gold/Platinum Ore Nodes, the
  larger Storage Box, and 3 of the 4 Trees no longer spawn by default —
  removed from `TestScene.unity`, not disabled. Sections below that test
  these will fail at their stated coordinates; use the **Admin** tab
  (`` ` `` menu) to spawn any `ItemDefinition`-based one instead (gadgets,
  ore chunks aren't items so this doesn't cover the Ore Nodes themselves —
  those still need re-adding to the scene to test again). **Secret Wall
  specifically can't be spawned via Admin at all** — it's a structural
  world object, not an `ItemDefinition`/`Pickup` — testing it requires
  manually re-adding it to the scene.
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
- [ ] **Trees (v0.1.58-dev):** originally 4 `Tree.prefab` instances around
  `(6,0,6)`, `(-6,0,8)`, `(9,0,-3)`, `(-8,0,-6)` — **3 removed 2026-08-06**
  to declutter the startup scene, 1 remains (exact position may not match
  the list above — check the scene rather than assuming). Walk to it and
  confirm: the trunk/branches render as a real branching shape (not a
  cylinder or a blob), visible and correctly lit **from every angle walked
  around it** (this specifically checks whether the untested triangle-winding
  safety net — `_Cull: Off` on `TreeBark.mat` — was actually needed; it
  should look normal regardless, just possibly rendering both mesh faces
  instead of one). Foliage clusters (small green spheres) sit at branch tips
  without floating detached from the branch. Walking into the trunk should
  block movement (`MeshCollider`); walking through where foliage-only spheres
  are shouldn't (their colliders were deliberately removed). Flag if the
  silhouette reads as clearly tree-like or more like an abstract branching
  blob — this is a first pass with no prior visual check.

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
- [ ] **Rock Node** (left-click/punch, `IPunchable`) breaks into 3 physical Small
  Rock chunks that scatter and can be picked up individually — doesn't take one
  punch; confirm it takes the expected number of hits.
- [ ] **Rock texture (v0.1.59-dev):** Rock Node and every Small Rock/chunk read as
  mottled grey stone (same tileable-noise technique as the grass texture), not
  flat solid grey — check both the whole node and its broken-off chunks look
  consistent with each other (they now share one material).
- [ ] **Small Rock chunk shape (v0.1.62-dev):** broken/dropped Small Rock pieces
  should read as a rounded sphere shape, not an obvious cube. **Regression:**
  `RockChunk.prefab` was a plain scaled Cube from the very start of the
  project — should now look and collide (roll, not slide/skid on flat faces)
  like a sphere.
- [ ] **Boulder (v0.1.62-dev)** at `(-4, 0.6, 4)`: should read as one irregular,
  lumpy rock shape (not a smooth sphere) with several small pebble bumps
  scattered on its surface, not several separate balls obviously glued
  together. Bare-handed punching works (no tool required, same as Rock Node)
  — takes 2 hits, yields 3 **Rock** (a new item, distinct from Small Rock).
  Confirm it renders and is visible from every angle walked around it (checks
  whether the untested triangle-winding safety net — `_Cull: Off` on
  `RockChunk.mat` — was actually needed).
- [ ] **Chunk scatter distance (v0.1.63-dev):** breaking a Boulder or Rock Node
  should scatter chunks with a visible initial burst that settles down
  quickly nearby, not chunks that keep rolling/bouncing far away from the
  break point. **Regression:** v0.1.62-dev's Small Rock/Rock chunks (right
  after the Cube→Sphere shape swap) rolled much farther than intended —
  `MediumRockChunk.prefab`'s `Rigidbody` damping had never actually been set
  (near-zero), and `RockChunk.prefab`'s existing damping was tuned for its
  old Cube shape, insufficient for a freely-rolling Sphere.
- [ ] **Rock (the new middle-tier item, v0.1.62-dev):** a pure intermediate
  material — no recipe currently uses it directly, and there's no way yet to
  turn it into Small Rock (not built — see `BUGS_AND_ENHANCEMENTS.md`/
  `CHANGELOG.md` for the scope boundary). Its dropped/world visual should be
  the same "lumpy body + small pebbles" hybrid shape as the Boulder, just
  smaller (4 pebbles vs. Boulder's 8).
- [ ] **Copper Ore Node (v0.1.59-dev)** at `(2, 0.4, -4)`: punching it *without* a
  Pickaxe held in a hand does nothing (no hit registers, prompt reads "Punch to
  break (requires Pickaxe)"). With a Pickaxe in either hand, punching works and
  takes 2 hits to break into 3 Copper Ore chunks — texture should read as
  grey rock with scattered copper-orange flecks and occasional green patina
  spots, not flat grey or flat orange.
- [ ] **Knife/Hammer/Axe/Pickaxe now come in all 5 CraftTiers
  (v0.1.69-dev):** craftable from the Crafting tab as `Crude Knife` through
  `Masterwork Knife` (and same for Hammer/Axe/Pickaxe) — 20 recipes total.
  What used to be the single `Rock Knife`/`Rock Hammer`/`Axe`/`Pickaxe` are
  now specifically the **Crude** tier (renamed in place, same recipe as
  before: 2 Small Rock + 1 Stick for Pickaxe, 1 Small Rock + 2 Stick for
  Axe, etc.). **Known, expected placeholder behavior — not a bug:** every
  tier of a given tool currently costs the *exact same* ingredients (no
  weakest-link rule enforcing real materials yet), so all 5 tiers of e.g.
  Knife are craftable side by side right away with nothing gating the
  higher ones. Carrying any of them in a backpack/main inventory (not a
  hand) should **not** satisfy a tool requirement below — still has to be
  held in a hand (`PlayerEquipment.HasInHand`).
- [ ] **Tool gating now accepts any tier (v0.1.69-dev):** the Copper Ore
  check above, and every other Pickaxe/Axe-gated node below, should accept
  **any** of the 5 Pickaxe/Axe tiers held in a hand, not just one specific
  one — spot-check at least two different tiers (e.g. Crude Pickaxe and
  Masterwork Pickaxe) against the same node to confirm both work.
- [ ] **Trees are now harvestable (v0.1.59-dev)** — previously purely decorative.
  Punching a tree *without* an Axe held does nothing, prompt reads "Punch to
  break (requires Axe)". With an Axe in hand, takes 4 hits to break into 3 Wood
  chunks; the tree (trunk + foliage) hides and respawns ~3 minutes later, same
  pattern as Rock Node.
- [ ] **Iron Ore Node (v0.1.60-dev; texture fixed in v0.1.61-dev)** at
  `(4, 0.4, -4)`: same tool-gating and texture check as Copper — visibly
  identifiable as ore from the start, no Mining Face Shield needed, 2 hits with
  a Pickaxe to break into 3 Iron Ore. **Regression:** v0.1.60-dev's texture
  rendered as a near-solid reddish-brown blob with no visible rock/fleck
  contrast — should now read as dark rock with distinct rust-colored flecks.
- [ ] **Copper Ore Node (texture fixed in v0.1.61-dev):** re-check this one too
  even though it shipped earlier — v0.1.60-dev's texture bug affected it too
  (rendered as a near-solid green blob). Should now read as dark rock with
  distinct copper-orange flecks and sparser green patina spots.
- [ ] **Silver/Gold/Platinum Ore Nodes (v0.1.60-dev; texture fixed in
  v0.1.61-dev)** at `(2, 0.4, -6)`,
  `(4, 0.4, -6)`, `(6, 0.4, -6)`: **without** a Mining Face Shield equipped, each
  should look and behave exactly like a plain Rock Node — indistinguishable, no
  visual hint anything's special. **Equip the Mining Face Shield** (Face slot;
  craft it first — 2 Small Rock + 1 Stick, or find/pick it up near the other
  wearable gadgets close to spawn) and look at each node again: it should
  visibly change to a metal-flecked ore texture (Silver = bright silvery-white
  flecks, Gold = yellow-gold flecks, Platinum = pale cool-white flecks —
  distinct from each other and from Silver's warmer white). **Regression:**
  v0.1.60-dev's Silver/Platinum textures were both near-solid pale blobs, nearly
  indistinguishable from the hidden/rock state even when correctly revealed —
  the reveal *logic* was actually working the whole time, but the visual result
  looked broken because the texture itself lacked contrast. Confirm the change
  now actually reads as obviously different, not just technically different.
  **Regression-style check, this is the important part:** mine one of these
  nodes *without* the shield equipped (Pickaxe in hand, shield off) — it should
  take 2 hits and yield **Small Rock, not the real ore** (the ore goes
  undetected). Unequip nothing else, just put the shield on, and mine a
  *different* instance of the same ore type (or wait for respawn) — this time
  it should yield the actual ore (Silver/Gold/Platinum). If a hidden node ever
  yields real ore *without* the shield equipped, or plain rock *with* it
  equipped, that's the core mechanic broken, not a minor issue.
- [ ] **Mining Face Shield equip/unequip/drop (v0.1.60-dev):** same three-button
  pattern (Equip/Unequip/Drop) as every other Face-slot equippable (Sunglasses),
  both from the main inventory list and the Equipment section. Worn shield
  should be invisible from the player's own camera (same `WornEquipment` layer
  fix every other equippable already has) but visible on an external view.
- [ ] **Berry Bush** — picking a Berry gives a real inventory item (not an
  instant-eat-on-touch); Eat button only appears in the main inventory list, never
  in a backpack/storage contents view.
- [ ] **Loot priority:** with a Backpack equipped, new pickups go straight into
  it. With no backpack, pickups try Left Hand, then Right Hand; if both hands are
  full with non-stacking items, the new pickup evicts (physically drops, not
  deletes) whatever's in Left Hand.
- [ ] **Respawn:** Stick Pickup / Stick Pickup 2 and the Rock Node reappear ~3
  minutes after being fully taken/broken, at their original position plus a small
  random offset. (Long wait — spot-check the timer logic instead of waiting the
  full 3 minutes every pass, e.g. temporarily shorten `respawnDelay` if verifying
  the exact timing matters.)
- [ ] **Despawn (v0.1.48-dev):** an item dropped via the inventory's Drop button,
  or via the hand-eviction fallback (both hands full with non-stacking items, no
  backpack equipped, picking up something new), disappears from the world after
  15 minutes if nobody picks it up. Confirm it does *not* apply to world-placed
  pickups (Sticks, Berry Bush) or `ResourceNode` chunk scatter — those should sit
  indefinitely (or respawn per the item above), never silently vanish. (Long
  wait — temporarily shorten `Pickup.DespawnDelay` to verify the exact timing
  rather than waiting the full 15 minutes every pass.) Also confirm a *partial*
  pickup (leftover after your inventory fills up) keeps counting down from the
  original drop time, not reset by the partial pickup.

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
  least one tool (Knife/Hammer/Axe/Pickaxe, any tier), and `Woodworking`
  once you've carved at least one Trimmed Stick (v0.1.71-dev) — the
  remaining four disciplines (Metalworking, Forging, Minting, Sewing) won't
  appear at all until something actually trains them, which nothing does
  yet. If you haven't crafted anything, this tab should show "No skills
  trained yet." rather than an empty blank panel.
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
- [ ] **Backpack visual (v0.1.74-dev):** the world-placed Backpack (and any
  dropped copy) should render as the Tripo3D-generated leather backpack
  model (metal buckle/studs, crude-leather look), not the old 5-cube
  placeholder. Check it sits at a reasonable size/orientation and doesn't
  look stretched or float above the ground — this was a hand-computed
  scale fit (0.53x), not an exact art pass.
- [ ] **Canteen** (near spawn, ~`(-1, 0.3, 1.5)`): Equip to Left/Right Hand or
  Waist. **Fill** only works within `fillRange` (2m) of a `WaterSource` (Water
  Puddle) — walking away and trying to Fill should fail/not appear. Filled Canteen
  visibly tints blue; empty is gray/neutral — check this both in the inventory
  panel and on the physical dropped object. **Drink** restores Thirst (see §2 for
  the overdrink ceiling).
  - **Regression (v0.1.46):** confirm the tint actually renders (was silently
    broken — `GetComponent<Renderer>()` on the root missed the child mesh
    renderers, and `Material.color` alone doesn't drive URP/Lit's `_BaseColor`).
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
- [ ] **Credits tab** shows "Tekim" and "the T-Rex."
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
