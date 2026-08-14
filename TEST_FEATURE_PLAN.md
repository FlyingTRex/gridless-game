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
- [ ] **Encumbrance-gated speed (2026-08-10):** carried weight vs. Strength-scaled
  capacity (`PlayerEncumbrance.LoadRatio`, shown on the Player tab — see §6c) caps
  speed on top of the stance/stamina multipliers above, using the same 50/80/90/95%
  breakpoints as Strength's own load-based skill gain (see §6c): ≤50% load = full
  speed, sprint allowed; 50–80% = 0.85x, sprint still allowed; 80–90% = 0.65x,
  sprint disabled; 90–95% = 0.45x, sprint disabled; >95% = 0.25x, sprint disabled
  plus an extra 5/s Stamina drain while moving. Confirm speed in the bottom-left
  debug panel drops as you load up past each threshold, and that Left Shift does
  nothing once load exceeds 80%.

## 2. Vitals & Stamina Decay

- [ ] `VitalsBarHUD` (always-on, bottom-center, Health/Stamina top row,
  Hunger/Thirst middle row) is visible with no equipment required. Bar fill
  reflects value/150 (so a stat at 100 fills two-thirds, not the whole bar —
  this is intentional headroom, not a bug).
- [ ] **Will (v0.1.148-dev):** a third row, one full-width bar, shows
  underneath Hunger/Thirst. Starts at 100/100. Unlike the other four bars,
  Will's fill fraction is against its own live max (not the fixed 150%
  scale) — confirm the bar still reads sensibly after Will's max grows (see
  §6a) rather than looking permanently near-full.
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

### Drag-and-drop interaction model (v0.3.11-dev — supersedes the button-based
steps below)

The button-per-item interaction (Equip/Unequip/Drop buttons, the "To X"
destination popup) was replaced entirely by drag-and-drop. Every slot box on
this screen — main inventory grid, equipment slots, backpack/boot/storage
contents — is both a drag source and a drop target. The bullets further down
in this section that reference specific buttons ("To L Hand", the move
popup's destination list, per-type Equip/Unequip/Drop) describe the *old*
interaction and are kept for their regression history, not as current steps —
re-verify the same underlying behaviors using drag instead.

- [ ] **Basic drag-move.** Press and hold a main-inventory item (e.g. Small
  Rock), drag it onto a hand slot in the Equipment section, release — it
  moves there. Drag it back to an empty main-inventory box the same way.
- [ ] **Snap-back on an invalid drop.** Drag an item onto a slot it can't go
  (e.g. a Wolf Pelt onto the Head equipment slot) — nothing happens, the
  item stays exactly where it was, no error.
- [ ] **Right-click for actions (v0.3.11-dev, added after live testing found
  left-click too twitchy).** Right-click any occupied slot — the action menu
  opens immediately and reliably (Drop always; Eat/Apply/Drink/Fill/Equip/
  Unequip as applicable), with no chance of it being misread as a drag.
  This is the recommended way to open the menu.
- [ ] **Click-for-actions (left-click).** A left click (press+release with
  under ~12px of mouse movement) on an occupied slot should also open the
  same menu. Confirm Drop still opens the existing quantity popup
  (`-10`/`-1`/`+1`/`+10`/All steppers). **Regression check:** confirm an
  ordinary, unhurried click reliably opens the menu rather than picking the
  item up — if it still frequently misfires into a drag, prefer right-click
  and flag it in `BUGS_AND_ENHANCEMENTS.md`.
- [ ] **Canteen equip-from-Backpack (the bug this whole rewrite grew out
  of).** Pick up a Canteen while wearing a Backpack so it lands in the
  Backpack's contents (not the main inventory). Drag it directly from the
  Backpack's contents grid onto a hand slot, and separately onto the worn
  Belt's own contents grid — confirm both equip correctly and the Backpack
  doesn't end up with a stale leftover entry.
- [ ] **Boot equip-from-Backpack.** Same test as above with a Boot dragged
  from the Backpack onto the Feet slot.
- [ ] **Partial-stack drag.** With a stack of 10+ of a stackable item (Small
  Rock, Stick), drag with no modifier (whole stack moves), then Shift held
  (half moves, rounded down, minimum 1 stays possible), then Ctrl held
  (exactly 1 moves) — confirm the source slot's remaining count is correct
  each time.
- [ ] **Apply from every location.** Click a Healing Paste or Bandage sitting
  in a hand, and separately inside a Backpack — confirm "Apply" appears and
  works in both places.
- [ ] **Drop from every location.** Click and Drop a Wolf Pelt from the main
  inventory, and separately from inside a Backpack — confirm it spawns as a
  world pickup both times.
- [ ] Spot-check the remaining equip types (Belt, NavComputer,
  HealthMonitor, Sunglasses, MiningFaceShield) drag-equip correctly from
  both the main inventory and a Backpack.

### Tools are equippable (v0.3.12-dev)

- [ ] Pick up a world-placed tool (or admin-spawn one, e.g. a Crude
  Pickaxe) and equip it to a hand — confirm a real 3D model appears in the
  player's hand, not just a text/icon entry in the inventory grid.
- [ ] Craft a brand-new tool from scratch (e.g. a Crude Hammer at an
  Anvil) — confirm the crafted instance *also* shows a physical model once
  moved to a hand. This is the main risk case: crafting goes through a
  different code path (`PlayerCrafting.AddCraftedOutput`) than picking one
  up in the world, and the two need to agree.
  **Regression check:** if the crafted tool sits invisible/text-only while
  a world-found one of the same type shows a model, the two systems have
  drifted apart — flag it.
- [ ] Drag a tool between backpack ↔ hand ↔ main inventory — confirm the
  model shows/hides correctly at each step (`SetCarried`/`Stash`), same as
  an existing equippable like Canteen.
- [ ] Mine a Copper Ore Node (or chop a tree, skin a creature) with the
  matching tool equipped in a hand — confirm the tool-gated action still
  works exactly as before. `PlayerEquipment.HasInHand` didn't change, but
  this is the real end-to-end proof it still reads the hand slot correctly
  now that the slot's entry is equipment-backed.
- [ ] Trigger (or force, by temporarily lowering the odds) a
  spectacular-failure craft while holding a required tool — confirm the
  tool actually breaks: it should disappear completely (both the inventory
  entry and the physical hand model), not sit there still visibly held
  with no inventory entry (an orphaned object — the exact bug
  `PlayerCrafting.BreakHeldTool`'s equipment-aware fix targets).

### Settler's Shirt (v0.3.13-dev)

- [ ] Enter Play mode fresh — confirm the player spawns already wearing the
  Settler's Shirt on Chest (check the Equipment section's Chest row), with
  no manual pickup/equip action taken.
- [ ] Right-click (or click) the worn shirt and confirm "Unequip"/"Drop"
  are the offered actions, matching a worn equippable like a Backpack.
- [ ] Confirm the shirt's 4-slot contents grid renders in the "Inventory"
  side panel while worn, same as a worn Backpack's does.
- [ ] Put an item in one of its 4 slots, unequip, re-equip — confirm
  contents persist.
- [ ] Drop the shirt, confirm it becomes a normal world pickup (falls to
  the ground, collider re-enabled), then pick it back up and re-equip —
  confirms the auto-equip-at-spawn mechanism doesn't interfere with the
  ordinary equip/unequip/drop flow afterward.
- [ ] **Dropped pose (added after Ben's live report that it looked
  oversized and stayed upright in its worn shape).** Confirm the dropped
  shirt actually lies flat on the ground — not floating upright still
  torso-shaped — and reads as a reasonably-sized discarded garment, not
  oversized relative to the player/environment.
- [ ] Confirm Admin Spawn's item search also finds "Settler's Shirt" (a
  second one, independent of the auto-equipped one) with no extra wiring.
- [ ] Look closely at the worn/dropped shirt in the 3D world — confirm
  "GRIDLESS" reads correctly (not mirrored, not on the back) across the
  chest.

### Settler's Belt + starting Canteen (v0.3.19-dev, Canteen v0.3.20-dev)

- [ ] Enter Play mode fresh — confirm the player spawns already wearing
  Settler's Belt on Waist, with no manual pickup/equip action taken.
- [ ] **Confirm a Canteen is already clipped into the belt's one
  attachment point at spawn** — not just the belt itself worn empty. Check
  the "Inventory" side panel shows the Canteen occupying that slot
  immediately, no manual equip needed.
- [ ] Confirm Drink/Fill work on the starting Canteen right away (proves
  it's a real, functional `Canteen` instance, not just a placeholder).
- [ ] Confirm the belt shows exactly one attachment point in the
  "Inventory" side panel (not the usual 6 generic points other belts have).
- [ ] Unequip/drop the starting Canteen, then drag a different Canteen onto
  that same point — confirm it's still accepted afterward (the starting
  attachment doesn't leave the slot stuck).
- [ ] Try dragging something else (e.g. a plain item, or a different
  equippable) onto that same point — confirm it's rejected (the drop
  should silently fail/snap back, same as any other restricted-slot
  mismatch).
- [ ] Drop the belt, confirm it becomes a normal world pickup lying
  correctly on the ground, then pick back up and re-equip.
- [ ] Confirm Admin Spawn's item search finds "Settler's Belt" (a second
  one, independent of the auto-equipped one).
- [ ] **Scene cleanup regression check:** confirm the Military Boots, Grass
  Belt, Backpack, and (as of v0.3.20-dev) the standalone ground Canteen
  that used to be pre-placed in `TestScene.unity` are all gone — starting
  gear should now be the only source of a new player's initial equipment.

### Sneakers + Settler's Sneakers (v0.3.18-dev, auto-equip v0.3.21-dev)

- [ ] Enter Play mode fresh — confirm the player spawns already wearing
  Settler's Sneakers on Feet, with no manual pickup/equip action taken —
  fourth starting-gear item after Shirt/Jeans/Belt.
- [ ] Admin-spawn plain "Sneakers" (separate item from Settler's Sneakers)
  and equip to Feet — confirm the model shows, correctly grounded (not
  floating/sunk), reads as a believable size next to the player.
- [ ] Confirm no named sub-slots appear for either (no Knife Sheath/Pistol
  Holster row) — both are slot-less like Civilian Boots, on purpose.
- [ ] Drop each — confirm they land and look correct lying in the world
  (collider fit to actual bounds).
- [ ] Open the Inventory tab with both Sneakers and Settler's Sneakers in
  the main grid — confirm both show a real icon, not text-only, and read
  as visually identical (same underlying model).

### MRE Ration — starting food + 5-tier Hunger restoration (v0.3.23-dev, Hunger + lying-flat fix v0.3.24-dev)

- [ ] Enter Play mode fresh — confirm the player spawns wearing the
  Settler's Shirt with 2 MRE Rations already sitting in its pocket
  storage (open Inventory, check the Shirt's own contents grid), no
  manual pickup/craft action taken.
- [ ] Right-click an MRE Ration in the shirt's contents (or the main
  grid, after moving one there) — confirm an **Eat** option appears in
  the action menu, same as Berry.
- [ ] Eat one from partial Hunger and partial Health (not already full,
  so both climbs are actually visible) — confirm: Hunger jumps up by 40
  immediately (Meal tier); Health jumps up by 25 immediately, then
  continues ticking upward over the next ~60 seconds for the remaining
  15 (check the debug vitals panel or just watch the bar), stopping once
  the full +40 Health total is applied, not before or past it. Confirm
  the item is consumed (count drops from 2 to 1, or the slot empties if
  it was the last one).
- [ ] Eat a Berry — confirm it now restores 15 Hunger (Snack tier, down
  from the old flat 20) and has no Health effect at all (no instant
  jump, no heal-over-time tick).
- [ ] Confirm eating an MRE while a Medicine/Heal Self heal-over-time is
  already in progress doesn't stack — the MRE's own heal-over-time
  should simply take over (same known behavior as Medicine/Heal Self
  overwriting each other today, not a new bug).
- [ ] Admin-spawn an extra MRE Ration directly — confirm the icon shows
  (not text-only), reads as a small tan ration pouch, and its tier
  border reads Normal-green (craft tier colors, v0.3.22-dev) since it
  has no `CraftTier` ladder (unrelated to its new `FoodTier`).
- [ ] Drop an MRE Ration (or look at one sitting in the world/its Admin
  Spawn preview) — confirm it now lies **flat** on the ground, thin
  side up, not standing on edge like a little box (regression check —
  the first shipped version of this model stood upright, fixed
  v0.3.24-dev by rotating the model 90° and re-grounding it). Confirm
  it's not floating or sunk into the ground either.

### Jeans — Settler's Jeans + plain Jeans (v0.3.17-dev)

- [ ] Enter Play mode fresh — confirm the player spawns already wearing
  Settler's Jeans on Leg (check the Equipment section's Leg row), with no
  manual pickup/equip action taken — same auto-equip mechanism as the
  Settler's Shirt, second caller of it.
- [ ] Right-click (or click) the worn jeans and confirm "Unequip"/"Drop"
  are offered, matching a worn equippable like a Backpack/Shirt.
- [ ] Confirm the jeans' 4-pocket contents grid renders in the "Inventory"
  side panel while worn, same as a worn Backpack's/Shirt's does.
- [ ] Put an item in one of the 4 pockets, unequip, re-equip — confirm
  contents persist.
- [ ] Drop the jeans, confirm they become a normal world pickup (fall to
  the ground, collider re-enabled, lying flat rather than standing upright
  in the worn pose), then pick back up and re-equip.
- [ ] Confirm the dropped jeans read as a believable size next to the
  player/environment (not oversized) — this was checked via a diagnostic
  render before shipping, but worth confirming in the live 3D view too.
- [ ] Confirm Admin Spawn's item search finds **both** "Settler's Jeans"
  and plain "Jeans" (two separate entries, only the Settler's variant
  auto-equips) — neither has a `CraftingRecipe` yet, both should still be
  obtainable via Admin Spawn.

### Combat Boots model + Boots icons (v0.3.14-dev, model fixed v0.3.15-dev, scaled to player v0.3.16-dev)

- [ ] Equip (or admin-spawn and equip) each of Civilian/Hiking/Military
  Boots — confirm all three now show the new combat boot model on the Feet
  slot, correctly grounded (not floating/sunk), not the old placeholder.
  **Regression check:** confirm exactly 2 boots are visible, not 3 (the
  first regeneration came back with an extra boot baked into the mesh).
  **Regression check:** confirm the boots read as a believable size next to
  the player — roughly ankle-to-mid-calf height, not the "boot the size of
  a washing machine" the raw import first produced.
- [ ] Drop each of the three — confirm the model still looks correct lying
  in the world (collider re-fit to the new model's actual bounds).
- [ ] Open the Inventory tab with one of each Boots type in the main grid —
  confirm all three now show a real icon instead of text-only (previously
  `icon: {fileID: 0}` on all three).

### Drop-zone hover highlight + coordinate-space fix (v0.3.15-dev, real bug
fixed v0.3.16-dev)

- [ ] Wear Military (or Hiking) Boots, hold a Knife anywhere reachable
  (hand/backpack/main inventory), and start dragging it — confirm a yellow
  outline appears **directly around the actual slot box** the cursor is
  over (not offset onto a caption label or elsewhere), updating live as the
  cursor moves. **Regression check:** v0.3.15-dev's first version of this
  highlight was itself visibly mispositioned (landed on the row's caption
  text instead of the box) due to a coordinate-space bug in
  `RegisterDropZone` — confirm that's now fixed.
- [ ] Drag a Knife onto the worn Boot's Knife Sheath specifically — confirm
  the highlight clearly marks the Knife Sheath box (not the adjacent Pistol
  Holster), and that releasing there actually moves the knife into the
  sheath. **This is the real regression test for the original bug report**
  (2026-08-12, "wouldn't move") — the root cause turned out to be the same
  coordinate-space bug affecting the highlight, not target-precision as
  first suspected, so this specific repro is the one that actually proves
  the fix.
- [ ] While scrolled partway down the Inventory tab's scroll view (not at
  the very top), repeat the drag-and-drop test on any slot box — confirm
  drops still land correctly. This is the scenario that would have broken
  hardest under the coordinate bug (unconverted rects were offset by
  `scrollPos`, so the mismatch grows the further you've scrolled).
- [ ] Confirm the highlight disappears immediately once the drag ends
  (drop or release-as-click) and doesn't linger.

### Historical button-based steps (pre-v0.3.11-dev — see note above)

- [ ] Clicking the **Inventory** tab: main inventory list (4 slots) shows carried items with Eat/Drink (if
  edible/drinkable), Craft (if a known recipe), Drop, **To L Hand, To R Hand**,
  To Pack, To Storage buttons as applicable.
- [ ] **To L Hand / To R Hand from the main inventory (v0.1.158-dev).**
  Craft a Pickaxe (or any tool) with no backpack equipped — it lands in
  the main inventory. Click "To L Hand" (or "To R Hand") and confirm it
  actually moves there, and that holding it in that hand now satisfies a
  tool-gated `ResourceNode` (e.g. a Copper Ore Node). **Regression:**
  before this fix, a plain item in the main inventory had no path to a
  hand at all — this option only existed for items already inside a
  Backpack/Belt/Storage Box's contents grid or already equipped
  somewhere. Caught during the first full system-test pass.
- [ ] **Drop quantity picker (v0.1.162-dev).** Clicking "Drop" (main
  inventory list, or the move popup for a hand slot/Backpack/Storage
  item) now opens a quantity popup — `-10`/`-1`/`+1`/`+10` steppers plus
  "All", defaulting to the full count already held. Confirm dropping a
  partial amount (e.g. 5 of 20 Stick) leaves the rest behind and spawns
  exactly 5 in the world. **Regression this specifically fixes:** with 2
  of the same Hammer tier (Hammer doesn't stack, `maxStack: 1`, so two
  separate slots), Drop used to dump both — confirm the popup now lets
  you drop just 1 and keep the other.
- [ ] **Move-as-many-as-fit still applies at the popup's default.**
  Since the quantity popup defaults to the full held count, clicking
  Drop immediately (no adjustment) should behave exactly like the old
  one-click Drop for a normal stackable item — confirm dropping a full
  Rope stack still works in the same two clicks (Drop, then Drop again
  in the popup) as before.
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
  priority below. **Fixed v0.1.164-dev:** `StickPickup.prefab`'s
  `Pickup.item` was never actually wired to the Stick `ItemDefinition`
  (`{fileID: 0}`, silently null, same bug class as Berry) — picking one
  up did nothing at all despite looking and behaving normally
  otherwise (real collider, real Rigidbody, real model). Very likely
  the actual explanation for repeated "Stick count doesn't change"
  reports earlier in this same testing pass. Confirm picking up a
  ground Stick now actually adds one to inventory. Swept every other
  `*Pickup.prefab` for the same pattern and also fixed
  `RopeCoilPickup.prefab` and `RockKnifePickup.prefab` (Crude Knife's
  world pickup) — confirm dropping and re-picking-up a Rope or a Crude
  Knife both work too.
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
  Woodworking rises). **Sized up v0.1.159-dev:** Plank's visual model and
  pickup collider are both 1.5x their original size (was too small to
  read clearly on the ground per Ben's live-testing feedback) — confirm
  a dropped Plank pile is clearly visible/clickable, not a tiny sliver.
  Roughly 3 in 10 Log chops should also drop a
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
    dropped — should now all show a real knapped-stone-knife model and
    icon.
  - **All 5 tiers get distinct real Blender models (v0.1.175-dev),
    superseding the "pixel-identical across tiers" note above.** No
    longer the same shared placeholder at different stretched scales —
    confirm a real visual progression: Crude/Rudimentary have a rough
    chipped-flint blade edge, Normal is a clean plain blade, Fine and
    Masterwork are smooth with a ribbed handle-wrap detail and a
    visibly darker (near-black by Masterwork) blade. All 5 should be
    the same overall size (~0.28m).
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
    (Tripo3D-generated, no credits needed).
  - **All 5 tiers get distinct real Blender models (v0.1.177-dev,
    head shape corrected v0.1.178-dev), superseding "identical across
    tiers" above.** The shaft is a plain wooden shaft, unchanged across
    all 5 (it's a Trimmed Stick — a separate ingredient with its own
    tiers, shouldn't visually improve with the Hammer's own tier). The
    head sits crosswise on the handle (a real sledgehammer/maul
    silhouette — the first attempt extended the head along the same
    axis as the handle instead and read as a lollipop, corrected same
    day) and is where tier shows: Crude/Rudimentary are noticeably
    bigger and organically lumpy; Normal is smoother and more rounded;
    Fine/Masterwork shrink toward a compact, precise head with a
    visible lashing/cord-binding detail where it meets the handle and a
    progressively darker (near-black by Masterwork) stone color.
    Confirm all 5 read as an actual hammer at a glance, not an
    abstract blob on a stick.
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
- [ ] **Berry Bush (real model v0.1.139-dev, redesigned v0.1.166-dev,
  visual swapped v0.1.171-dev)** at `(-1.5, 0.2, 1.5)` — a real
  Tripo3D-generated leafy bush model (`GeneratedBerryBush.glb`, reused
  from an earlier decorative comparison prop that's now removed from
  the scene). **Deliberately not the Strawberries model anymore** —
  that's reserved for the loose dropped Berry pickup only, so the
  standing bush and a scattered berry are never visually confused for
  each other again (they used to share the exact same model). No
  longer an instant E-pickup — two independent gather actions instead:
  - **E, chop:** hold to chop, prompt reads "Hold to chop (requires
    Knife or Axe)" — requires any tier of either actually in hand (not
    just carried) to actually work; without one, holding does nothing.
    On success, 2 loose Trimmed Stick (Crude) pickups scatter on the
    ground near the bush and Woodworking gains experience. Goes on a
    ~180s cooldown afterward (prompt reads "Bush (branches regrowing)"),
    independent of search's own cooldown below.
  - **F, search:** no tool needed, prompt reads "Search for berries"
    (hidden entirely while on cooldown, not shown as blocked). Rolls 0
    to 3 and scatters that many loose Berry pickups on the ground —
    confirm a 0-roll is possible (nothing drops, still goes on
    cooldown) and that dropped Berries can be individually picked up
    (E), stacked in inventory, and eaten. Own independent ~180s
    cooldown from chop.
  - **"Super success" Berry Seed chance (v0.1.179-dev):** every search
    also rolls an independent 2% chance (`berrySeedChance`) for a bonus
    Berry Seed, regardless of the normal berry roll's own outcome — a
    0-berry search can still find a seed, a 3-berry search can too. At
    2% this will take many searches to see live; if verifying quickly,
    consider temporarily raising `berrySeedChance` in the Inspector
    rather than brute-force searching. Confirm a found Berry Seed is a
    real pickup (own icon, distinct small dark-brown teardrop model,
    pickable/stackable) — no use for it yet beyond holding it.
  - Confirm the bush itself never disappears or hides — only whichever
    prompt (chop or search) is on cooldown changes, the model stays
    visible and both raycast targets remain hittable throughout.
  - **Fixed v0.1.173-dev:** the scattered Trimmed Stick pickup used to
    reuse the plain Stick's branch model as a placeholder visual
    (indistinguishable from a regular dropped Stick). Now spawns the
    real Crude-tier Trimmed Stick model (Blender-generated — see
    `Tools/Tripo3D/README.md`). Confirm chopping the bush now drops a
    visually distinct, slightly gnarled trimmed stick, not a plain
    branch.
  - **Fixed v0.1.169-dev:** a scattered Berry (or Trimmed Stick) could
    land close enough to the bush's own collider to permanently block
    raycasts from ever reaching it — aiming at what looked like a loose
    berry showed the *bush's* chop/search prompt instead of "Pick up
    Berry," and E did nothing. Scatter now spawns on a fixed ring
    clearly outside the bush's collider instead of a fully random
    offset. Confirm every berry/stick from a chop or search is
    individually aimable and pickable, not just the ones that happened
    to scatter far enough away by luck.
  - **Berry resized v0.1.171-dev:** the loose Berry pickup shrank from
    0.35m to 0.18m bounds — was sized to look right next to itself as
    the old (identical-model) bush, read as oversized once the bush
    became the bigger, visually distinct leafy model. Confirm a
    dropped/scattered Berry now reads clearly as "a small handful,"
    not competing in size with the bush itself.
- [ ] **Eat from anywhere (fixed same day, v0.1.159-dev):** previously
  Eat only ever showed in the main inventory list — a Berry sitting in a
  hand slot, a Backpack, or a Storage Box had no Eat option at all,
  forcing a move back to the main inventory first just to eat it. The
  same "where should this go?" popup used for moving an item (click a
  hand slot, or an item inside a Backpack/Storage contents grid) now
  shows an **Eat** button first when the item is edible. Confirm eating
  a Berry directly from your Left/Right Hand works, and from inside a
  Backpack/Storage contents view too. **Fixed same day (v0.1.161-dev):**
  the button appeared but eating from anywhere other than the main
  inventory silently did nothing — `PlayerEating.TryEat` always removed
  from the main inventory specifically, regardless of where the Berry
  actually was. Confirm a Berry eaten from a hand/Backpack/Storage now
  actually decrements from wherever it was and restores Hunger.
- [ ] **Move-as-many-as-fit (fixed v0.1.161-dev):** every "To Left
  Hand"/"To Right Hand"/"To Backpack"/"To Inventory"/"To Storage" button
  used to pass the source's *entire* matched count as the move quantity
  — fine for a stacking item, but a non-stacking item (any Hammer tier,
  `maxStack: 1`, each instance its own slot) broke immediately: 2
  Hammers into an empty single-capacity hand slot failed completely
  instead of moving the 1 that fits. Confirm having 2 of the same Hammer
  tier (craft or admin-spawn a second) in a Backpack, with an empty
  hand, and clicking "To Left Hand" now moves exactly 1 — leaving 1
  behind in the Backpack — instead of doing nothing.
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
  *not* apply to world-placed pickups (Sticks) or `ResourceNode`/
  `ChoppableTree`/`BerryBush` scatter (Logs, Planks, ore chunks, Berry
  Bush's chopped Trimmed Sticks and found Berries, etc.) — those
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

## 4a. Combat (2026-08-10)

- [ ] **Wolf** (2 placed, `(14, 0, 6)` and `(-14, 0, -8)`): stands idle until
  you're within ~10m, then chases (5 m/s) and bites in range (~2m, ~8 dmg,
  1.5s cooldown). Walk away past ~20m mid-chase — confirm it gives up and
  returns to idle rather than following forever.
- [ ] **Punch (Left Mouse Button):** short-range, ~9 dmg, ~0.7s cooldown
  between swings whether or not it connects. Trains the new **Bare-handed**
  skill (Skills tab → Combat category — the first real entry there, all
  four other weapon-usage skills named back in the original Crafting
  planning still don't exist as real `SkillDefinition`s). **Regression:**
  confirm punching does nothing while a Build piece is armed — Building's
  own Left Click takes priority.
- [ ] **Death:** at 0 HP the Wolf flops onto its side (a static rotation —
  no animation system exists, don't expect a real death animation) and
  stops attacking/chasing.
- [ ] **Skinning:** aim at a dead Wolf with a **Knife** in hand, hold E
  (~2s) — "Hold to skin (requires Knife)". Confirm it correctly refuses
  without a Knife held. Yields **50% chance** of 1 Wolf Pelt, and
  **always** 1–2 Raw Meat (randomized — confirm both outcomes happen
  across a few kills, not just one). Trains Gathering. Corpse respawns
  ~3 minutes after skinning (not after death — an unskinned corpse should
  sit there indefinitely).
- [ ] **Health regen (v0.1.190-dev, slowed from the original §2 rate):**
  passive Health regen (needs Hunger/Thirst both > 50) is now 0.05/s —
  roughly 33 minutes for a full heal doing nothing. Confirm a Wolf bite's
  damage visibly persists rather than healing back within a minute or two
  — this was fast enough before this fix to make combat feel
  consequence-free.

## 4b. First Aid (2026-08-10)

- [ ] **Herb Bush** (2 placed alongside the Berry Bushes, same visual
  model reused): press **F** (not E — Herb Bush has no chop action to
  reserve E for, but reuses Berry Bush's exact look, so F matches Berry
  Bush's own search key rather than the "every single-action gatherable
  uses E" convention, deliberately, per Ben's call after live confusion
  with a first E-bound pass). "Search for herbs" — rolls 1-3 Herb
  (leaf-shaped), scattered on the ground. "Herbs (regrowing)" while on its
  ~3 minute cooldown.
- [ ] **Healing Paste recipe** (Crafting tab → new **Medicine** discipline
  tab): 3 Herb + a **Canteen holding ≥20 Water equipped in a hand**.
  Confirm it's blocked without enough Canteen water, and that crafting it
  actually drains the Canteen's `Amount` (check via the Inventory tab's
  Drink/Fill row) — same mechanic as drinking, just feeding the recipe
  instead of Thirst. Trains Medicine. **Known gap:** the water check only
  looks in your hands, not a Belt-attached Canteen.
- [ ] **Bandage recipe** (same Medicine tab): 1 Cloth + 1 Healing Paste →
  1 Bandage. Trains Medicine.
- [ ] **Apply button** — shows next to Healing Paste/Bandage anywhere Eat
  shows for a Berry (main inventory list, and the move popup for a hand/
  backpack/storage item). Healing Paste heals 10 HP over 10s; Bandage
  heals 15 HP over 10s — both via the same heal-over-time mechanism
  Restoration's Heal Self wish uses (`PlayerVitals.StartHealOverTime`),
  confirm the Health bar visibly climbs gradually, not instantly.

## 4c. NPC Placeholder (2026-08-10)

- [ ] **SD Macross Factory Worker** (1 placed in `TestScene.unity`): idle-wanders
  within ~6m of its spawn point, walking speed 1.2 m/s, pausing 2-5s between
  legs. Confirm it visibly faces the direction it's walking (not
  sideways/crab-walking — this was a real bug, fixed via a
  `modelForwardOffsetY` correction).
- [ ] **Menu interaction** (2026-08-10, Chunk 1 of Hireable NPCs): look at
  the NPC, confirm "[E] Talk to Factory Worker" prompt appears. Press E —
  confirm a popup menu opens (cursor unlocks) instead of dialogue firing
  directly.
  - [ ] **Talk** button: confirm the placeholder dialogue line appears on
    screen, the NPC stops wandering (holds still even mid-walk) for ~4
    seconds, then automatically resumes wandering from wherever it
    stopped, and the menu itself closes (cursor re-locks).
  - [ ] **Hire** button (not yet hired): shows "Hire cost: 10 Copper" and
    your current balance. Confirm it's greyed out/unusable if you can't
    afford it, and that clicking it while affordable deducts 10 Copper
    from your wallet (check via the Inventory tab or `PlayerCurrency`)
    and the menu now shows "Hired" + a Fire button instead of Hire.
  - [ ] **Fire** button (hired): confirm it immediately drops back to
    showing the Hire option, no confirmation prompt.
  - [ ] **Escape** while the menu is open: confirm it closes the menu and
    re-locks the cursor, same as every other screen (Bank/Lockbox/
    Crafting/etc.).

## 4d. NPC Job Assignment (2026-08-10, Chunk 2 of Hireable NPCs)

- [ ] **Assign Job button**: hire the Factory Worker, open its menu again —
  confirm it now shows "Hired — no job assigned" and an "Assign Job"
  button. Click it — confirm `NPCHiringScreen` closes and `NPCJobScreen`
  opens in its place (no double-modal, cursor stays unlocked throughout).
- [ ] **Family tabs**: confirm a "Mining" tab shows, with a "Mine Ore" job
  underneath it.
- [ ] **Assign an unassigned job**: click "Assign" on Mine Ore — confirm the
  tile switches to showing 3 tool requirements (Pickaxe, Mining Face
  Shield, Backpack), each showing "—" and a "Give" button.
- [ ] **Give a tool you don't have**: confirm the Give button is greyed out
  and shows "(none in inventory)".
- [ ] **Give a tool you have** (use the Admin tab to spawn a Crude Pickaxe/
  Mining Face Shield/Backpack if needed): confirm clicking Give removes
  one from your inventory and the row updates to show the item name
  instead of "—".
- [ ] **Give all 3**: confirm the NPC now shows all requirements filled.
  Close and reopen the menu from `NPCHiringScreen` — confirm "Hired — job:
  Mine Ore" shows, and the given tools are still equipped (not reset).
- [ ] **Reassignment loses tools**: with Mine Ore assigned and tools given,
  if a second job family/job existed you'd confirm assigning it wipes the
  Mine Ore tools for good — not testable yet with only one job in the
  game, but flag if `NPCJob.Assign`'s reassignment-wipe behavior seems
  needed sooner than expected.
- [ ] **Fire clears the job**: Fire the NPC, hire it again, open Assign Job
  — confirm no job is assigned and no tools are equipped (both wiped by
  Fire, not carried over to the new hiring).

## 4e. NPC Stats & Job Gating (2026-08-10, Chunk 3 of Hireable NPCs)

- [ ] **Stats section**: hire the Factory Worker, open its menu — confirm a
  "Stats" section shows Strength/Dexterity/Constitution/Intelligence all
  reading **3.00**, Mining reading **0.0**, and an "Encumbrance: 0/90 lbs"
  line (90 is Strength 3's capacity — confirm the number, not just that a
  line shows).
- [ ] **Mine Ore still available**: open Assign Job — confirm Mine Ore still
  shows under the Mining tab (its tier-1 requirement is Mining skill 0, so
  it should never be hidden at a fresh NPC's starting skill).
- [ ] **Locked-job message** (not concretely testable with only one job in
  the game yet): if a future higher-tier job is added, confirm it's hidden
  until Mining skill reaches its threshold, and the panel shows "No jobs
  unlocked at this NPC's current skill yet." instead of "No jobs in this
  family yet." while family Mining has jobs that just aren't earned yet.

## 4f. NPC Autonomous Mining (2026-08-10, Chunk 4 of Hireable NPCs)

- [ ] **Starts working once ready**: assign Mine Ore and give all 3 tools
  (Chunk 2/3's flow) — confirm the NPC stops idle-wandering and instead
  walks toward a real ore/rock node somewhere in the world (needs one
  within ~50m of wherever it's standing — walk it closer to the mining
  area near spawn if it doesn't find one).
- [ ] **Actually mines**: confirm it stops at the node, pauses briefly
  (mining), and the node visually disappears/goes on cooldown afterward
  — same as if you'd broken it yourself.
- [ ] **Cargo updates**: open the hire menu — confirm a "Carrying" section
  now shows the mined item and count, Encumbrance's carried number went
  up to match, and the Mining stat grew above 0.0. Once it's mined
  several different item types (multiple ore types), confirm the
  Stats/Carrying area scrolls instead of overflowing the panel, and the
  Talk/Hire/Assign Job/Fire buttons above it stay put.
- [ ] **Repeats automatically**: confirm it moves on to another node and
  keeps mining without any further input, until —
- [ ] **Stops when full**: confirm it stops moving/working once carrying
  ~80% of its Encumbrance capacity (not 100%) rather than continuing to
  try. It should neither wander nor mine at that point (Chunk 5 is what
  teaches it to walk back and deposit).
- [ ] **Obstacle avoidance**: if a wall/foundation piece sits between the
  NPC and its target node, confirm it slides around rather than getting
  permanently stuck pushing into the obstacle. Not expected to be perfect
  pathfinding — flag if it gets fully stuck rather than just taking an
  inefficient route.
- [ ] **Talk still freezes it mid-mining**: press E and Talk while it's
  actively walking toward or mining a node — confirm it stops completely
  (not just paused-wandering) and resumes exactly where it left off once
  the dialogue ends.

## 4g. NPC Deposit & Return-to-Mining (2026-08-10, Chunk 5 of Hireable NPCs)

- [ ] **Set Deposit Container**: with Mine Ore assigned, open Assign Job
  and click "Set Deposit Container" — confirm the menu closes and the
  cursor re-locks (normal aiming). Look around — a prompt should say
  "Look at a Storage Box to set it as the deposit point" while nothing's
  in the crosshair, and switch to "[E] Set \<name\> as deposit point"
  once one is.
- [ ] **Confirm doesn't also pick up the box**: aim at an *empty* Storage
  Box (normally pickupable via E) and press E to confirm it as the
  deposit target — confirm the box stays placed in the world (not picked
  up into your inventory) and the targeting prompt disappears.
- [ ] **Escape cancels targeting**: start targeting, press Escape before
  confirming — confirm it cancels cleanly (prompt disappears, cursor
  stays locked/normal gameplay, doesn't leave you stuck unable to
  interact with anything).
- [ ] **Deposit point shows in the menu**: reopen Assign Job — confirm
  "Deposit point: \<box name\>" now shows instead of "not set".
- [ ] **Returns and deposits automatically**: once the NPC can't find any
  more carriable ore nearby, confirm it walks to the deposit box instead
  of just stopping, and its cargo empties into the box (check the box's
  own contents via the Inventory tab, and the NPC's "Carrying" list
  should shrink/clear).
- [ ] **Resumes mining after depositing**: confirm it goes right back to
  searching for ore afterward rather than idling.
- [ ] **No deposit point set still works** (Chunk 4's original fallback):
  an NPC assigned Mine Ore with no deposit container set should still
  mine and just stop once full, not get stuck in a broken state.

## 4h. NPC Work Timer & Payment (2026-08-10, Chunk 6 of Hireable NPCs)

- [ ] **Countdown shows while working**: hire, assign Mine Ore, and give
  it all 3 tools — confirm the menu shows "Working — payment due in Ns"
  and the number counts down over real time (this is a genuine 5-minute
  = 300s real-world wait to see it fully elapse; the countdown updating
  at all confirms the timer is live without waiting the full 5 minutes).
- [ ] **Stops when unpaid**: once the timer hits 0, confirm the NPC stops
  mining/moving (holds in place, doesn't just idle-wander either) and the
  menu now shows "Waiting for payment" + a Pay button instead of the
  countdown.
- [ ] **Timer doesn't run while not working**: an NPC that's hired but
  has no job assigned (or is missing a tool) shouldn't show a countdown
  at all, and shouldn't need paying just for existing unassigned.
- [ ] **Pay resumes it**: click Pay (needs 10 Copper again) — confirm it
  goes right back to mining/depositing where it left off, and the
  countdown restarts from a fresh 300s.
- [ ] **Fire resets it too**: fire an NPC mid-countdown, hire it again —
  confirm it starts a fresh countdown rather than picking up where the
  old one left off.

## 5. Player Menu (Tab) — Crafting Tab

- [ ] **Tile grid, not a list (redesigned v0.1.167-dev).** Clicking the
  **Crafting** tab shows every known recipe (not just ones you currently
  have materials for) as a grid of tiles, not a text list — each tile: a
  big icon (blank spacer, not a placeholder glyph, for the handful of
  items without one baked yet — currently Sunglasses/Nav Computer/Health
  Monitor; all 5 Trimmed Stick tiers got real Blender-generated models
  and icons in v0.1.173-dev. Masterwork got a real Tripo3D-generated
  wood texture in v0.1.174-dev — confirm its icon shows visible wood
  grain/warm tones, distinctly nicer than the other 4 tiers, which
  still use a flat-color material, no image texture, for now), the
  item name, materials with
  live "have N" counts (red when short), tool/skill/Anvil-surface
  requirement lines when applicable, a quantity stepper, a **Craft**
  button, and a **Max** button. **Icon framing improved v0.1.169-dev:**
  every icon (Crafting tiles, Build tiles, inventory/equipment icons —
  53 total, re-baked in one pass) now fills its box tightly instead of
  floating with visible padding, especially noticeable on non-cubic
  shapes like Nail or Foundation which used to look small relative to
  roughly-cube-shaped items baked with the same settings.
- [ ] **Search bar (v0.1.167-dev), above the grid.** Typing filters every
  discipline's recipes by name (case-insensitive substring), ignoring
  the discipline-tab selection entirely while active — confirm typing
  "ax" shows every unlocked-or-not Axe tier across all 5 tiers in one
  list regardless of which tab was selected before searching, and that
  clicking **Clear** (or emptying the box) reverts to the normal
  per-discipline tab view. Empty search + no recipes in a discipline
  still shows "No recipes yet."; a search with no matches shows
  `No recipes match "<query>".` instead.
- [ ] **Quantity + Max, not a single instant craft.** The stepper
  defaults to 1; `-`/`+` adjust it, clamped between 1 and however many
  the current materials actually support. **Max** jumps straight to
  that ceiling. **Craft** starts a batch for whatever quantity is
  currently selected — greyed out if materials/tool/skill/Anvil-surface/
  output space don't support it, same "— inventory full" label as
  before when that's specifically the blocking reason.
- [ ] **Real batch crafting with a per-item timer (v0.1.167-dev).**
  Starting a batch removes ingredients for the *whole* batch immediately
  (not per item), then the tile's Craft/Max row is replaced by a
  progress bar reading `Crafting 2 / 5  (1.4s)` — the per-item duration
  is `CraftTierScale.HoldDuration`, the same skill-scaled ladder
  gathering already uses (higher skill = faster). Confirm each
  completed item lands in inventory as it finishes, not all at once at
  the end, and that skill experience gains once per completed item, not
  once per batch.
- [ ] **Keeps running in the background.** Start a batch, then close the
  Crafting tab entirely (or switch to a different PlayerMenuScreen tab,
  or close the menu and walk around) — confirm the batch keeps
  progressing and items keep completing while the tab isn't even open,
  unlike every hold-and-release interaction elsewhere in the game which
  cancels the moment you look away. Reopening the Crafting tab mid-batch
  should show the progress bar picking up exactly where it actually is.
- [ ] **Cancel refunds only the unfinished remainder.** With a batch of
  5 running, cancel after 2 have completed — confirm the 2 already-
  crafted items stay in inventory (nothing clawed back) and the
  materials for the remaining 3 are refunded. The tile returns to the
  normal stepper/Craft/Max view afterward.
- [ ] **A broken tool stops the batch.** Queue a large batch of a
  tool-gated recipe (Trimmed Stick, Knife required) at low skill (higher
  spectacular-failure odds) and keep going until a "your Knife broke"
  message appears — confirm the batch stops immediately afterward
  (refunding the unfinished remainder) rather than continuing to
  silently fail every remaining item with no tool in hand.
- [ ] **One batch at a time, globally.** While any recipe's batch is
  running, every *other* tile's Craft/Max should grey out with "Crafting
  queue busy" — confirm you can't start a second batch on a different
  recipe until the first finishes or is cancelled.
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
- [ ] **Grid scrolls:** confirm the Crafting tab scrolls to reach tiles
  that run off the bottom of a long discipline instead of clipping, and
  that the tab bar/search bar/Close button stay fixed above/below the
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
  consumed. Without a Knife in hand, the tile reads `— requires Knife in
  hand` and Craft is greyed out even with a Stick available. Equip a Knife
  to a hand and it should read `[Knife in hand]` instead and Craft should
  enable (assuming a Stick is also available). Confirm the Knife is still
  in your hand — not consumed — after crafting. Crafting any tier trains
  **Woodworking** (check the Skills tab, Crafting Disciplines category).
- [ ] **Ingredient substitution (v0.1.159-dev):** recipes/pieces that ask
  for a raw material now also accept anything refined from it
  (`ItemDefinition.baseItem`, checked via the new `IngredientMatching`
  helper) — e.g. Crude Axe (needs 2x Stick) should now craft fine holding
  only Trimmed Stick (any tier, Crude through Masterwork), and Crude
  Fiber Backpack/Belt (need raw Fiber) should craft fine holding only
  Woven Grass Cloth. Confirm "have N" in the Crafting tab counts
  substitutes too, and that removal spends your raw/exact stock first
  before touching the refined substitute (hold both Stick and Trimmed
  Stick, craft an Axe, confirm the plain Stick disappears first).
- [ ] **Nail (v0.1.160-dev, Metalworking tab):** `Nail  (needs 1x Iron)`
  should craft into 5 Nails, require any tier of Hammer in hand (not
  consumed), and train Metalworking. **New: requires a nearby Boulder or
  Anvil.** With no Boulder/Anvil within 2m, the label should read
  `— requires a Boulder or Anvil nearby` and Craft should stay disabled
  even with Iron and a Hammer both available. Walk within 2m of either
  the Boulder or the newly-placed Anvil (near the Boulder in
  `TestScene`) and it should become craftable. Confirm this same gate
  also blocks **Twig Foundation** if you strip 2m range away from both
  (shouldn't — Foundation has no `requiresAnvilSurface`, only Nail does;
  this is a per-recipe opt-in, not global). **Fixed same day (v0.1.161-dev):**
  the recipe originally showed `— requires Metalworking 25` with no way
  to reach it — `Nail.asset` defaulted to `tier: Normal` when created
  (the skill gate reads the output item's own tier directly), instead of
  `tier: Crude` like every other no-ladder item (Rope, Cloth). Confirm
  Nail is craftable from skill 0 now, same as Rope/Cloth.
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

- [x] **Craft tier colors + tier sort/filter (v0.3.22-dev).** Every
  Crafting tile's icon has a thin colored border matching its output
  item's `CraftTier` (Crude gray, Rudimentary white, Normal green, Fine
  blue, Masterwork gold), and its item name is drawn in that same color
  instead of plain white — confirm all 5 colors are visually distinct
  against the dark panel background, and that the existing red "have N"
  ingredient-shortage warnings still read clearly as a separate, distinct
  color from any tier (don't get visually confused with the palette).
  Below the search bar, a **"Tier:" filter row** (All + one colored chip
  per tier) narrows the grid to a single tier, ANDed with whatever the
  current discipline tab/search already shows (not replacing it) —
  confirm selecting a tier chip while a discipline tab is active shows
  only that tab's recipes at that tier, and typing a search query while
  a tier chip is selected further narrows within it. A **sort-direction
  button** ("Tier 1 → 5" / "Tier 5 → 1") toggles the grid's order — confirm
  the default (on first opening the tab) is ascending (Crude first), and
  that toggling actually re-orders the grid (families now scatter apart,
  e.g. Crude Knife sits next to Crude Pickaxe, not next to Rudimentary
  Knife — this is the intended tradeoff, not a bug). Also confirm the same
  tier border + colored name appears on inventory slot boxes
  (`InventoryScreen` — main grid, equipment slots, worn-container
  contents) for any item with a `tier` set.

## 6. Player Menu (Tab) — Skills Tab

- [ ] Clicking the **Skills** tab shows four category tabs — **Gathering**,
  **Crafting Disciplines**, **Combat**, **Magic** (added v0.1.148-dev) —
  each listing the skills in that category with their current level
  (0–100). `Crafting` no longer exists as a skill (retired v0.1.70-dev,
  see `CHANGELOG.md`) — don't expect to see it anywhere.
- [ ] **Magic** tab: shows only your randomly-assigned starting lineage
  (Elemental/Illusion/Kinetic/Restoration) once you've completed at least
  one wish (see §6a) — the other three lineages should never appear here,
  since only the starting one is currently attemptable. Before your first
  successful wish, this tab should show "No skills trained yet."
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

## 6a. Player Menu (Tab) — Magic Tab (v0.1.148-dev; all magic unified onto R in v0.1.151-dev; default-skill selection in v0.1.152-dev)

- [ ] **No on-screen hint for magic at all, by design (v0.1.155-dev).**
  Holding R never shows a prompt, a key label, or a progress bar,
  regardless of lineage or target — this is deliberate ("something people
  play with in order to explore it," not a labeled button), not a bug.
  The Controls tab (`` ` `` menu) doesn't mention R either. Testing magic
  now means holding R and watching for a *world* reaction (a campfire
  lighting, an object sliding, Health climbing) or checking the Skills/
  Magic tabs afterward, not reading a crosshair prompt — every check
  below has been rewritten around that.
- [ ] Clicking the **Magic** tab shows your randomly-assigned starting
  lineage (one of Elemental/Illusion/Kinetic/Restoration — reroll a fresh
  character a few times via a new save to confirm it actually varies, not
  always the same one), current Will (starts 100/100), and a list of known
  wishes for that lineage, showing both costs (e.g. "60 Will on success /
  40 on failure"). If your starting lineage is Illusion, this list should
  show nothing yet — it's the only lineage with no wish so far.
- [ ] **Select button (v0.1.152-dev):** each known wish row has a button
  reading "Active" for whichever wish is currently selected (disabled,
  can't re-click it) and "Select" for any others. Since every lineage has
  at most one wish today, there's nothing to actually choose between yet
  — just confirm the currently-known wish shows "Active" automatically
  from the moment you spawn, with no menu trip required (auto-defaults in
  `PlayerMagic.Awake`). Groundwork for when a lineage gets a second wish.
- [ ] **All magic activates with R**, no prompt (unified v0.1.151-dev,
  hints removed v0.1.155-dev). **E is no longer involved in magic at
  all** — confirm holding E at the Campfire does nothing now.
- [ ] **Campfire + Spark** — one unlit Campfire sits at `(-4, 0.3, -2)` in
  `TestScene`. With an Elemental character, stand near it and hold R for
  a few seconds (duration scales with Elemental skill tier — several
  seconds at Crude) — no prompt will appear, just hold roughly that long
  and release, then check whether it lit.
- [ ] **Success/failure roll (v0.1.149-dev)** — completing the hold no
  longer guarantees the campfire lights. Odds start at 50% right at
  Elemental's unlock threshold, rising to 90% once you're roughly 20
  skill points past it — craft/gather other things first if you want to
  push Elemental up via repeated attempts and watch the odds improve.
  **On success:** campfire visibly lights (orange emissive material, a
  point light turns on), Will drops by **60** (check the Magic tab, not a
  prompt), Elemental gains experience (check the Skills tab's Magic
  category), and Will's **max** ticks up slightly. **On failure:**
  campfire stays unlit, Will drops by **40** (not 60), Elemental still
  gains experience, and a message reading roughly "The wish didn't take
  — Spark fizzled." still appears top-center (this message is not part of
  the removed UI hints — it's feedback about an *attempt that already
  happened*, not a hint about what R does).
- [ ] **Will regen:** confirm Will climbs back up at 1 point every 5
  seconds while doing nothing (check via the Magic tab — slow, a full
  recovery from a failed attempt takes a couple minutes).
- [ ] **Gating, confirm each fails silently (no roll, no cost, no
  message)** rather than attempting anyway: (1) a non-Elemental character
  holding R at the unlit Campfire — nothing happens at all, not even a
  fizzle message (a hard gate failure, distinct from a failed roll); (2)
  an Elemental character with less than 60 Will (the success cost —
  gated even with enough for a mere failure) — same silent no-op; (3)
  holding R at an *already-lit* Campfire does nothing (one-shot, no
  re-lighting mechanic).
- [ ] **Known simplification, not a bug:** the roll only checks skill
  margin — there's no fuel/tinder quality input to weight it, unlike
  crafting's ingredients. A Masterwork Elemental caster and a
  freshly-unlocked Crude one (once both clear the unlock gate) roll
  against the same campfire, just at different odds and hold speeds.
- [ ] **Push (Kinetic) — the generic fallback wish, same R as Spark, no
  prompt.** With a Kinetic character, aim at *any* loose Rigidbody object
  (a dropped Small Rock, an ore chunk, a dropped Pickup — not a static
  resource node like Rock Node/Boulder itself, which has no Rigidbody)
  and hold R for a few seconds. Same success/failure roll and 60/40 Will
  split as Spark. On success, the object gets a real physics shove away
  from you (`Rigidbody.AddForce`, impulse) — confirm it actually slides/
  rolls, not just teleports. On failure, same fizzle message, no
  movement, Will still drops 40 and Kinetic still gains experience.
  **Fixed v0.1.164-dev:** the hold used to require the raycast to
  resolve the *exact same* GameObject on every single frame — any
  one-frame aim jitter (more likely on a bigger/multi-part model, e.g.
  a dropped Backpack) silently reset progress to 0 with no feedback at
  all, since wishes show no progress bar by design. Confirmed live: a
  full multi-second hold on a Backpack produced nothing, no message.
  Relaxed to match E's hold (no frame-to-frame identity check) —
  confirm holding R on a Backpack now reliably completes the hold and
  rolls, even with natural mouse movement during the hold.
- [ ] **E and R are independent** — confirm holding E on an IInteractable
  (e.g. a Rock Node) and R on a separate loose Rigidbody chunk don't
  interfere with each other's progress if you were to somehow trigger
  both (edge case, low priority, just confirm nothing crashes/soft-locks).
- [ ] **Heal Self (Restoration) — the first Unconditional wish, no aiming
  and no prompt.** With a Restoration character, hold R anywhere, looking
  at anything (or nothing) — duration is purely off the Restoration
  skill tier. Take some damage first (e.g. let hunger/thirst hit 0
  briefly, or a Spectacular Failure craft) so healing is visible. **On
  success:** Health climbs toward +10 total, spread smoothly over the
  next 30 seconds (not an instant jump) — watch the Health bar in
  `VitalsBarHUD` tick up gradually. Will drops 60, Restoration gains
  experience. **On failure:** no healing at all, Will drops 40, same
  fizzle message, Restoration still gains experience. **Re-casting
  mid-heal:** trigger a second successful Heal Self before the first
  one's 30s finishes — confirm the new heal replaces the old one (fresh
  10-over-30s from that point), not stacks on top of it or extends the
  total duration.
- [ ] **Illusion still has no wish** — confirm an Illusion character's
  Magic tab shows "No wishes known yet" and R does nothing for them at
  all (silent, same as any other gate failure).

## 6b. Player Menu (Tab) — Build Tab (v0.1.156-dev, Foundation only)

- [ ] Clicking the **Build** tab lists **Twig Foundation** (needs 6x
  Stick, 3x Rope), always unlocked (Crude tier, no Woodworking
  requirement to start). An "Arm" button arms it; once armed the button
  reads "Armed (click to cancel)" and clicking it again un-arms.
  **Unlike Magic, this tab is meant to be fully informative** — costs and
  requirements should be plainly visible, not hidden.
- [ ] **Tile grid + search (v0.1.168-dev, same treatment as Crafting).**
  Pieces show as tiles (icon, name, live materials have/need, skill
  requirement if locked, Arm/Armed) instead of a text list — confirm
  Twig Foundation and Storage Box both show a real baked icon, not a
  blank spacer. A search bar above the grid filters by name (e.g.
  "found" matches "Twig Foundation"); clearing it shows every piece
  again. **Deliberately no batch/quantity/Max/timer here** — Arm still
  works exactly as before, one piece placed per walk-and-aim act; this
  redesign only touched the browsing/visual layer, not placement itself.
- [ ] **Ingredient substitution (v0.1.159-dev):** Twig Foundation's 6x
  Stick requirement should also accept Trimmed Stick (any tier) as a
  substitute, same `IngredientMatching` mechanism as Crafting — confirm
  placing one with only Trimmed Stick on hand (no plain Stick) works.
- [ ] **Materials from Backpack/Storage (fixed same day, v0.1.159-dev):**
  previously `PlayerBuilding` only ever checked the main 4-slot
  inventory — 6 Stick + 3 Rope sitting in an equipped Backpack (or a
  nearby Storage Box) didn't count at all, showing "Not enough
  materials" even when you genuinely had enough. Now reaches the same
  three sources Crafting does (main inventory, equipped Backpack, nearby
  Storage Box). Confirm placing a Foundation works with all 6 Stick + 3
  Rope sitting entirely in a Backpack and none in the main inventory.
- [ ] **Free placement (first Foundation, nothing to snap to):** with
  Twig Foundation armed, close the menu, aim at open ground, and confirm
  a translucent cyan ghost preview follows your crosshair in real time
  (this is a real visible preview, distinct from Magic's zero-UI
  approach). **Left Mouse Button** — the ghost should lock in place
  (stop following the crosshair). **Scroll wheel** — the ghost should
  rotate in 90° steps. **Left Mouse Button again** — the piece should
  actually spawn at that position/rotation, 6 Stick and 3 Rope should
  leave your inventory, and Woodworking should gain experience (check
  the Skills tab's Crafting Disciplines category).
- [ ] **Not enough materials:** try placing without 6 Stick + 3 Rope on
  hand — confirm a message reading "Not enough materials." appears
  top-center (below the Magic/skill-up messages, same stacking
  convention) and nothing is spent, nothing spawns.
- [ ] **Cancel out of build mode (fixed v0.1.159-dev, key changed same
  day):** previously, once a piece was armed there was no way back out
  at all, so a failed "Not enough materials" placement left you stuck
  following a ghost forever. First attempt bound this to Escape, but
  Escape is also read the same frame by `FirstPersonController` to
  unlock the cursor — the two firing together left the cursor unlocked
  with nothing actually open, and Tab's own guard (deliberately) refuses
  to reopen the menu while the cursor's already unlocked, so Tab
  appeared to do nothing. **Fixed for real with Right Mouse Button**
  instead. Confirm **Right Mouse Button** while the ghost is following
  the crosshair fully cancels (ghost disappears, nothing armed), while
  Right Mouse Button during the rotate/confirm sub-phase just steps back
  to following (doesn't fully cancel in one press). Also confirm the
  Build tab's **"Armed (click to cancel)"** button un-arms the piece the
  same way, and that Tab reopens the menu normally afterward either way.
- [ ] **Edge-snapped placement (second Foundation):** with one Foundation
  already placed, arm Twig Foundation again and aim near one of its
  edges — the ghost should snap immediately to that edge (position *and*
  rotation both automatic, no rotate step) as soon as you're close
  enough. A single **Left Mouse Button** press should confirm it
  immediately — no lock/rotate phase, unlike the free-placement case.
  Confirm the two panels sit flush with no gap or overlap, and the
  second panel's top surface is exactly level with the first's — even if
  you deliberately aimed slightly off the exact same height, since
  height is inherited from the neighbor, not read from your aim.
- [ ] **Sockets can't be double-claimed:** after two Foundations are
  snapped together, try arming a third and aiming at the *already-used*
  edge between the first two — it should not offer a snap there (falls
  back to free placement rooted wherever you're aiming instead).
- [ ] **Known gap, not a bug:** Foundation is currently a flat slab only
  — there's no visible support column/pedestal reaching down to uneven
  terrain yet, even though the mechanical 5m reach check is real (it
  only actually gates the edge-snapped case in this build, since free
  placement always matches the raycast hit exactly).
- [ ] **1m thick, mostly buried (v0.1.163-dev, supersedes an earlier
  same-day "fully raised" pass).** The slab is now 1m thick (was 0.3m)
  and sits mostly below the raycast hit point — reading as a real
  foundation wall rather than a thin flush slab or a raised deck.
  Applies to both Twig and Plank Foundation, and to edge-snapped
  placement too (neighbors should still align exactly, just all at the
  new height together). **Lip raised 0.2m → 0.4m, v0.1.182-dev** — Ben's
  call after seeing the (by-then correctly visible, see the entry
  below) platform in game: "needs to be slightly higher." Confirm a
  freshly placed Twig or Plank Foundation now shows roughly a 0.4m lip
  above the grass, not 0.2m.
- [ ] **Real Twig Foundation model (v0.1.169-dev).** The plain grey Cube
  slab is now a real Tripo3D-generated lashed-twig-and-rope platform on
  short legs — confirm it looks like a crude bundled-stick platform, not
  a primitive shape, and that the collider/socket footprint is
  unchanged (edge-snapping a second Foundation should still align
  exactly flush, same as before this visual swap). **Plank Foundation
  still uses the plain Cube** — only the Twig tier got a real model this
  pass. **Real alignment bug fixed v0.1.181-dev, not caught until now:**
  from this model swap up through v0.1.180-dev, the visible mesh never
  actually matched the `BoxCollider` — the mesh sat over a meter lower
  than the collider, with even its top under the visible ground plane.
  Completely invisible in every icon/preview taken during that whole
  stretch (`IconBaker` frames from the mesh's own bounds, independent of
  the collider), only surfaced once someone tried to actually look at a
  live-placed one. Confirm a freshly placed/spawned Twig Foundation now
  shows a visible platform with a real lip above the grass (~0.4m as of
  v0.1.182-dev, see the entry above), not a buried/invisible collider
  with nothing to see.
- [ ] **The Build tab should now show Twig Foundation, Storage Box,
  Twig Wall (added v0.1.180-dev), Twig Roof Panel (added
  v0.1.183-dev), Twig Half-Wall, Twig Door-Frame Wall, Twig Door (all
  three added v0.1.184-dev), Twig Gable Panel (added v0.1.186-dev),
  and Twig Pole (added v0.1.187-dev, see below) — 9 pieces total,
  nothing else.
- [ ] **Twig Wall (v0.1.180-dev) — first piece that isn't Foundation.**
  Modeled and textured entirely in Blender this time (no Tripo3D) — 15
  individually irregular vertical branches lashed with 2 horizontal
  rope bars, real baked wood-grain texture, not a flat color. Costs 8
  Stick + 4 Rope, trains Woodworking, no skill requirement to start
  (Crude tier). **Doesn't free-place meaningfully** — arm it with no
  Foundation nearby and it'll follow your raycast like Foundation does,
  but there's no reason to place one that way; the real test is the
  snap case below. **Edge-snap onto a placed Foundation:** arm Twig
  Wall and aim near one of a placed Foundation's 4 edge sockets — the
  ghost should snap to stand vertically right at that edge (not lie
  flat the way a second Foundation panel would), confirm with a single
  Left Mouse Button press. Confirm the wall's base reads as sitting
  right at (very slightly embedded into) the Foundation's top surface,
  not floating above it or leaving a visible gap. **Known, flagged
  deviation from the design-brief's spec:** shipped at ~2.6m tall, not
  the documented 3m — see that doc's Building System section. **Confirmed
  live, v0.1.182-dev** — Ben tested the actual Build-tab arm/aim/snap
  flow (not Admin Spawn, which has no socket awareness at all — see the
  entry below) and confirmed the ghost snaps correctly to a Foundation
  edge and stands vertically as designed. **Starting materials for
  testing (v0.1.182-dev):** a fresh game now grants 24 Stick + 12 Rope
  automatically (`AdminSpawnScreen.Awake`, Editor-only) — exactly enough
  for 3 Twig Walls (8 Stick + 4 Rope each) without gathering first.
- [ ] **Twig Roof Panel (v0.1.183-dev) — first piece that snaps onto
  another piece's socket instead of Foundation.** Same Blender build as
  Twig Wall (15 branches + 2 rope lashing bars, real baked wood-grain
  texture), but built along the slope with a 35° pitch baked directly
  into the mesh. Costs 10 Stick + 5 Rope, trains Woodworking, no skill
  requirement (Crude tier). **Requires a Wall already placed** — arm
  Twig Roof Panel and aim near the *top* of a placed Twig Wall (not the
  Foundation edge below it); the ghost should snap standing at the
  wall's own pitch, eave flush with the wall top, ridge end reaching up
  and inward over the building's interior. Confirm with a single Left
  Mouse Button press. **Two-panel ridge test:** place Walls on two
  *opposite* Foundation edges (e.g. North and South), then a Roof Panel
  on each — the two panels' ridge ends should meet close together at
  the same height near the building's center line, forming a real
  ridge peak, not overlapping past each other or leaving a visible gap.
  **Known gap, not a bug:** no ridge-socket lock between the two
  panels — they only meet correctly if both walls sit on opposite
  Foundation edges of the same building; non-opposite or
  mismatched-size placements won't line up. **Confirmed live,
  v0.1.183-dev** — Ben built a full four-wall structure with both Roof
  Panels through the real Build-tab arm/aim/snap flow; the two panels
  meet cleanly at a real ridge peak with the correct pitch and no gap,
  matching the throwaway batch-mode measurement taken before this was
  tested live in-game (see `CHANGELOG.md` v0.1.183-dev).
- [ ] **Twig Half-Wall (v0.1.184-dev).** Same visual style as Twig Wall,
  half the width (2.5m vs 5m), same height. Costs 4 Stick + 2 Rope, no
  skill requirement. Snaps to a Foundation edge exactly like Twig Wall
  (arm it, aim near an open edge socket, single Left Mouse Button press)
  — no new placement behavior to test here, it reuses Wall's own snap
  math untouched. **Known, not a bug:** placing one only covers half a
  5m edge; there's no side-by-side snapping between two Half-Walls (or
  a Half-Wall and a Door-Frame Wall) to jointly fill one full edge —
  each piece only snaps directly to a Foundation edge socket.
- [ ] **Twig Door-Frame Wall (v0.1.184-dev).** Same 5m×2.69m footprint
  as Twig Wall, with a 1.5m-wide × 2.4m-tall doorway cut into it —
  thicker jamb posts on both sides of the opening, a wood header beam
  across the top. Costs 10 Stick + 4 Rope. Snaps to a Foundation edge
  exactly like Twig Wall. Confirm the doorway is actually walkable (no
  invisible collider blocking it) and that a placed Twig Door (see
  below) visually sits inside the opening, not offset into a jamb post
  or floating outside the wall. **Real bug found live and fixed
  (v0.1.184-dev):** the collider was originally one `BoxCollider` sized
  from the whole mesh's bounds, which can't carve out a hole — the
  doorway was completely solid, both unwalkable and silently blocking
  any interaction raycast aimed through it (this is what first looked
  like "F doesn't open the door," see the Twig Door entry below). Fixed
  by splitting it into 3 separate boxes (two flanks + the header) that
  leave the doorway's own space genuinely open. **Second real bug,
  found right after fixing the first one:** even with the doorway open,
  Ben still couldn't walk through — "the player is too fat and tall."
  The doorway was originally sized 1.2m×2.0m, but Foundation's own edge
  socket sits 0.4m below Foundation's actual walkable top surface (a
  "mostly buried" offset every wall inherits), so the *effective*
  clearance above the real floor was only 2.0-0.4=1.6m against the
  CharacterController's 1.8m height and 1.2m against its ~0.96m
  effective diameter — both genuinely too tight, not just visually
  cramped. Resized to 1.5m×2.4m (2.0m effective clearance, 0.2m margin
  over the CharacterController's height), confirmed via a batch-mode
  capsule-overlap check sized to the exact CharacterController
  dimensions (radius 0.4, height 1.8) standing at the doorway's center.
  **Still broken live, per Ben 2026-08-09 — the resize did NOT actually
  fix it.** The batch-mode capsule check reported zero overlaps, so
  whatever's actually blocking movement in the real running game isn't
  something that check captured — see the open bug entry in
  `BUGS_AND_ENHANCEMENTS.md` for the investigation notes/next steps.
  Do not assume this is fixed just because the doorway math checks out.
- [ ] **Twig Door (v0.1.184-dev) — first piece that snaps onto a
  Door-Frame Wall's own socket, and the first placed piece with any
  runtime behavior at all.** Costs 4 Stick + 2 Rope. **Requires a
  Door-Frame Wall already placed** — arm Twig Door and aim at the
  doorway opening; the ghost should snap standing in the frame, hinge
  aligned with one side of the opening. Confirm with a single Left
  Mouse Button press. **Open/close:** look at a placed Door and press
  **F** (not E — see below) — it should swing open (away from wherever
  you're standing, never toward you) over about half a second, and the
  prompt should change to "Close Door". Press F again to close it
  manually before the timer, or walk away and wait — it should
  **auto-close after 60 seconds** if left open. **Swing-away test:**
  open the door standing on one side, then close it, walk to the
  *other* side, and open it again — it should swing the opposite
  direction both times, confirming it always opens away from your
  current position rather than a single fixed direction. **Bound to F,
  not E (fixed live, v0.1.184-dev):** Door originally used E like every
  other interaction, but E is also `PlayerPieceUpgrade`'s own
  click-to-upgrade/hold-to-destroy key on any placed piece — with a
  Hammer equipped (the normal state while building), E never reached
  the door's own open/close at all. Confirm E does nothing to the door
  now (no accidental destroy), and that a Hammer equipped at the same
  time doesn't block F from opening/closing it. **Two real bugs found
  live in sequence, not one** — after the F-key fix, Ben reported F
  *still* didn't open the door; the actual remaining cause was the
  Door-Frame Wall's own collider silently blocking the doorway (see its
  entry above), not the keybind at all. Confirm F now works reliably
  once you're actually standing where a raycast can reach through the
  (now genuinely open) doorway. Verified via throwaway batch-mode
  checks before each was ever re-tested live (placement-gap
  measurement, open/close swing-direction math via reflection, the
  F-key interface swap, and a doorway raycast check — see
  `CHANGELOG.md` v0.1.184-dev) — still needs a real Play-mode
  arm/aim/snap/open/close/auto-close pass to confirm, same as every
  other piece got before being marked "Confirmed live."
- [ ] **Twig Gable Panel (v0.1.186-dev) — fills the triangular gap above
  a Wall, up to the Roof Panel's ridge, on the two Foundation edges
  that *don't* carry a Roof Panel.** Costs 6 Stick + 3 Rope. **Requires
  a Wall already placed** (Twig Wall, Half-Wall, or Door-Frame Wall) —
  arm it and aim near the top of that wall, same `WallTop` socket Roof
  Panel uses; the ghost should snap standing flush against the wall's
  own vertical plane (not tilted/sloped like Roof Panel — this piece
  stands straight up). Confirm with a single Left Mouse Button press.
  **Full building test:** Foundation with Walls on all 4 edges, Roof
  Panels on the North/South pair, Gable Panels on the East/West pair —
  the gable's sloped edges should sit flush against the roof panels'
  underside on both sides, with the gable's own apex reaching right up
  to the ridge line (no visible gap, no interpenetration). **Known
  gap, not a bug in the piece itself:** the Build tab icon renders
  tiny and off-center — see the open bug in `BUGS_AND_ENHANCEMENTS.md`.
  Confirmed via throwaway batch-mode checks before this was tested
  live (placement-sign check — same-sign and negated gave identical
  results, since this piece is symmetric unlike Door; apex-height
  measurement against the computed ridge height) — still needs a real
  Play-mode arm/aim/snap pass to confirm, same as every other piece
  got before being marked "Confirmed live."
- [ ] **Twig Pole (v0.1.187-dev) — Foundation's own footprint as an open
  stilt frame, 4 corner posts + top/mid beam frames, no floor.** Costs
  12 Stick, no Rope (plain post-and-beam, not lashed). **Ground
  tiling:** arm Pole and aim near an existing Foundation's edge socket
  — should snap beside it exactly like a second Foundation would,
  standing on the ground. **Stacking (the new part):** with a Pole
  already placed, arm Twig Foundation and aim at the *top* of the
  Pole — the ghost should snap sitting on top of the frame, elevated
  (~2.4m up), not at ground level. Confirm the elevated Foundation
  reads as "sitting in" the pole's top frame the same slightly-buried
  way a ground Foundation sits in the dirt, not floating above it or
  clipped weirdly into it. Confirm the space *underneath* the Pole's
  frame is actually walkable — no invisible collider filling the
  hollow middle (this piece deliberately used per-element colliders
  from the start, not the single-bounding-box mistake Door-Frame Wall
  made). Verified via throwaway batch-mode checks before this was
  tested live (zero-gap checks on both the tiling and stacking cases,
  elevated-height math) — still needs a real Play-mode arm/aim/snap
  pass on both placement modes to confirm, same as every other piece
  got before being marked "Confirmed live."
- [ ] **Plank tier for the whole Building System (v0.1.188-dev) — Wall,
  Half-Wall, Door-Frame Wall, Door, Roof Panel, Gable Panel, Pole, plus
  a real visual for Plank Foundation (which never had one before —
  it was a plain grey cube).** All 8 are directly buildable from the
  Build tab now (`unlockTier` Rudimentary, skill level 10), not just
  reachable via the Twig→Plank Hammer upgrade. Confirm each one: shows
  up in the Build tab once your Woodworking skill is 10+ (and is
  correctly *hidden*/greyed below that); costs the right amount of
  Plank (Wall 10, Half-Wall 5, Door-Frame Wall 12, Door 5, Roof Panel
  12, Gable Panel 7, Pole 10, Foundation 8); snaps exactly like its
  Twig counterpart (same sockets, same placement math — Roof Panel's
  pitch, Door's hinge side, Door-Frame Wall's doorway, Pole's tiling
  and stacking all reuse the exact same code paths, no new placement
  bugs expected but worth a real look at each one). **Known gap:** all
  8 icons read pale/washed-out in the Build tab (see
  `BUGS_AND_ENHANCEMENTS.md`) — a real, understood-but-unfixed lighting
  issue, not a sign anything is actually broken. **Upgrade path:** with
  a Twig piece placed, Hammer equipped, tap E — should replace it in
  place with the matching Plank piece and consume the right Plank
  amount, exactly like the existing Twig Foundation → Plank Foundation
  upgrade already does. Ben found a real testing-friction gap doing
  this: Admin Spawn only grants 1 item per click, so getting enough
  Plank (69 total for one of everything) meant either dozens of clicks
  or grinding a tree — fixed by adding an 80-Plank starting grant next
  to the existing Stick/Rope one in `AdminSpawnScreen.Awake()` (Editor-
  only testing convenience, same as always; needs a fresh Play session
  to take effect). **A second, real bug surfaced right after**: even
  with 20 Plank on hand, upgrading Twig Door-Frame Wall (and Door —
  "same as the door") failed with "Not enough materials." Root cause:
  `PlayerPieceUpgrade` only ever checked the player's own main-
  inventory list, never an equipped Backpack or nearby StorageBox
  (unlike `PlayerBuilding`, which already reaches all three for fresh
  builds) — same class of bug as the original "can't eat a Berry" fix.
  Fixed by porting `PlayerBuilding`'s own reach logic into
  `PlayerPieceUpgrade`. Confirm broadly: an upgrade should now succeed
  with materials in *any* reachable location — main inventory,
  equipped Backpack, or a nearby StorageBox — not just the main list.
- [ ] **Doorway walkability, actually fixed this time (v0.1.188-dev).**
  The real cause of the doorway bug logged 2026-08-09 (resize passed
  every batch-mode check but still failed live) had nothing to do with
  the doorway itself: Foundation's own exposed lip (0.4m, raised from
  0.2m the same day) exceeded the Player's `CharacterController.
  stepOffset` (0.3m), so walking onto *any* Foundation edge from
  ground level was blocked project-wide — Ben's own diagnostic catch
  ("walking blocked, jump/run-through clears it") is what actually
  cracked it. Fixed by raising `stepOffset` to 0.45 rather than
  lowering the lip back down, keeping Ben's original "needs to be
  slightly higher" call intact. Confirm broadly, not just at
  doorways: walking up onto a bare Foundation edge (no Wall) from
  open ground should now also work smoothly, without needing a jump.
  Confirmed live by Ben: "the door works much better just walking
  through it now."
- [ ] **Upgrade/destroy on a placed piece (v0.1.157-dev).** Equip any
  tier of Hammer in a hand, look at a placed Twig Foundation — a prompt
  should read "Click to upgrade to Plank Foundation — hold 5s to
  destroy". **Click (tap, quick press+release):** the Foundation should
  be replaced in place by a Plank Foundation (lighter tan color) at the
  exact same position/rotation, 8 Plank should leave your inventory, and
  Woodworking should gain experience. **Hold for 5 full seconds:** the
  piece should be destroyed outright (disappears, no item/material
  returned to inventory) — confirm nothing is refunded. **Without a
  Hammer equipped:** no prompt should appear at all, neither action
  should do anything.
- [ ] **Upgrade preserves snap connections:** place two Foundations
  snapped edge-to-edge, then upgrade one of them to Plank — confirm the
  two pieces are still connected afterward (try snapping a third
  Foundation to the *other* panel's far edge — should still work
  normally) rather than the upgrade silently breaking the connection.
- [ ] **Destroy frees the connection:** with two Foundations snapped
  together, destroy one — confirm the *other* panel's edge is available
  to snap a new piece to again afterward, not permanently stuck
  "occupied" by the piece that no longer exists.
- [ ] **Already at the highest tier:** since Rock/Metal don't exist yet,
  upgrading a Plank Foundation should show "Already highest tier — hold
  5s to destroy" and clicking it should do nothing (no infinite ladder,
  no error).
- [ ] **Not enough materials:** try upgrading without 8 Plank on hand —
  confirm a "Not enough materials." message appears and nothing is
  destroyed or replaced (the original Twig Foundation should remain
  exactly as it was).
- [ ] **Known gap, not a bug:** the 5-second destroy hold shows a text
  countdown only, no graphical progress bar.
- [ ] **Storage Box, built (v0.1.160-dev).** Build tab lists
  `Storage Box  (needs 4x Plank, 6x Nail)`, Crude tier (no skill
  requirement to start). Arm it and place it same as Foundation (no
  socket to snap to — it's always free placement: LMB drops the ghost,
  scroll rotates, LMB again confirms). Confirm 4 Plank and 6 Nail leave
  inventory, Woodworking gains experience, and the placed box registers
  as a real `StorageBox` — walk up and open the Inventory tab, it should
  show up in the nearby-storage section same as any other box.
- [ ] **Storage Box, pickupable (v0.1.160-dev).** Look at any *empty*
  placed Storage Box (the newly-built one, or the original "Small
  Storage Box" already sitting in the scene) — prompt should read
  "Pick up Storage Box" (or "Pick up Small Storage Box"), **E** picks it
  up instantly (no hold, no tool needed), removes the placed box, and
  adds a "Storage Box" item to your inventory. **Must be empty to pick
  up:** put at least one item in a box first — prompt should instead
  read "Storage Box (must be empty to pick up)" and E should do nothing.
  **Place it again:** with a "Storage Box" item in inventory, Drop it —
  confirm it spawns as a real, working, empty Storage Box (not an inert
  prop) that you can store items in and pick back up again, for free (no
  material cost the second time).

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

## 6c. Player Menu (Tab) — Player Tab (2026-08-10)

- [ ] **Core stat tiles:** Strength, Dexterity, Constitution, Intelligence show as
  a 3-tile-per-row grid (Fame/Faction fill out the second row), each reading
  `Name: 2.00` on a fresh character — the **.25–10 display scale**
  (`PlayerSkills.GetAttributeValue`), not the raw 0–100 skill level every craft
  skill uses. All four start above the .25 floor (raw level 20, per
  `PlayerSkills.startingLevels`), not untrained-from-zero.
- [ ] **Growth bar:** each of the 4 stat tiles has a labeled bar ("Growth") at the
  bottom, same visual style as `VitalsBarHUD`'s vital bars. Fills 0→1 as the stat
  progresses toward its *next .25 point* (not the 0–100 cap) — on a fresh
  character this reads as empty (no gold fill) since no progress has accumulated
  yet; that's expected, not a bug. Fame/Faction/Guild tiles do **not** get a
  Growth bar — none of those have a `GainExperience`-backed track.
- [ ] **Tile grid fills the screen edge-to-edge** (`PlayerMenuScreen.TileWidth` is
  computed from `Screen.width`, not a fixed pixel size) — confirm at a couple of
  window sizes that tiles stretch to fill available width rather than leaving a
  large empty gap on the right.
- [ ] **Encumbrance (Strength tile only):** a second line, `Encumbrance: X/Y lbs`,
  where X = `PlayerEncumbrance.CarriedWeight` (main inventory + every
  `PlayerEquipment` slot, including a worn Backpack's own weight, + that
  Backpack's contents — **not** nearby Storage Boxes) and Y = `Capacity`
  (`17.3925 × Strength^1.5`, anchored so Strength 10.00 caps at exactly 550 lbs).
  At the default starting Strength (2.00), Y should read ~49 lbs.
  - [ ] **Strength-from-load tiers:** carrying ≤50% of capacity grants no Strength
    XP; 50–80% grants a marginal rate; 80–90% better; 90–95% the best rate;
    >95% ("Overloaded") the rate drops back down *and* Health drains at 2/s while
    sustained. Real-time-calibrated (2026-08-10): at Strength 2.00, the 90–95%
    tier takes ~2 real days to gain +0.25 — **not observable in a short test
    session**; don't mistake a motionless Growth bar over a few minutes for a
    bug, that's the intended pacing. A batch-mode simulation, not live play,
    is the practical way to re-verify the math if this is ever touched again.
  - [ ] **Pickup blocked at/over capacity (`PlayerLoot`):** once `LoadRatio >= 1.0`,
    every pickup (plain items via `Receive`, equippables via `ReceiveEquipment`)
    fails outright and the item stays on the ground — confirm by loading up to
    exactly capacity and trying to pick up one more item. No on-screen message
    for this yet (same as every other pickup-failure case, e.g. full
    inventory — a pre-existing gap, not new).
- [ ] **Fame / Faction:** placeholder tiles (`Fame: 0`, `Faction: None`), always
  visible, no backing system yet.
- [ ] **Guild tiles:** one full-width tile per joined guild (same total width as
  the 3-tile stat row above), stacked one per row below Fame/Faction — **no row
  at all while zero guilds are joined.** Join/leave via the Admin tab (see §15)
  since there's no in-world way to join yet; confirm a tile appears/disappears
  live the moment you click Join/Leave, capped at 3 simultaneous guilds
  (`PlayerGuilds.MaxGuilds`).

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
- [ ] **Admin — Spawn Build Piece (v0.1.170-dev, same Admin tab).** A
  second list below the item one — every `BuildPiece` alphabetically
  (currently Twig Foundation, Storage Box), each with its own Spawn
  button. Clicking Spawn places the piece directly on the ground under
  the player — free (no materials removed, no skill-tier check) — via a
  straight-down raycast from just above the player, same as a real
  placement would land on flat ground. Confirm the spawned piece is
  tagged as a real `PlacedPiece`: aim a Hammer at it afterward and
  confirm the normal click-to-upgrade/hold-to-destroy prompt works on
  it exactly like a normally-built one. Testing aid only, same
  Editor-only scoping as the item spawn list above. **Fixed same day
  (v0.1.171-dev):** spawning used to push the player underground —
  the piece's collider materialized wrapped around the player's own
  feet (raycast originates from the player's position) and
  `CharacterController` depenetration resolved downward instead of up.
  Confirm spawning Twig Foundation now lands the player standing on
  top of the freshly-placed platform instead. **Fixed same day
  (v0.1.172-dev):** the ground raycast was hitting the player's own
  `CharacterController` capsule before it ever reached real terrain,
  spawning the piece floating ~1.8m up (legs dangling in open air)
  instead of flush with the ground. Confirm a freshly-spawned Twig
  Foundation now sits with just its intended small lip above the
  grass, not floating at head height — and that you can reach/climb
  onto a *normally-built* (not admin-spawned) Foundation with an
  ordinary jump, now that the admin tool's own bug isn't muddying
  whether climbing itself has a real problem.
  **Simplified v0.1.181-dev, superseding both fixes above.** Found live-
  testing the Twig Wall: standing directly on top of a large flat piece
  right after it spawns doesn't visually read as "a piece exists" at
  all (see the Foundation alignment bug entry elsewhere in this doc for
  the bug this first looked like). Root-cause fix instead of another
  stand-on-it workaround: pieces now spawn a few meters in front of the
  player instead of at their own position, so there's never any burial
  risk to rescue from in the first place — the player is no longer
  auto-teleported onto the new piece at all. Confirm a spawned piece
  now lands visibly on the ground a short walk in front of you, and
  that the "reach it, aim a Hammer at it" upgrade/destroy check above
  still works the same way, just from a few steps away instead of
  standing on it immediately.
- [ ] **Admin — Guilds (2026-08-10, same Admin tab).** A third list, below
  the item and build-piece ones, showing every `GuildDefinition` asset
  (Masonry, Carpentry, Smithing) with a Join or Leave button matching
  current membership. Join is disabled once already a member of 3
  guilds (`PlayerGuilds.MaxGuilds`); clicking Leave frees a slot
  immediately. Confirm this list drives the Player tab's guild tiles
  live (see §6c) — join one here, switch to the Player tab, confirm the
  tile appeared with no re-open of the menu needed.

## 16. Scattered World Content (2026-08-11)

The 200x200 hilly Terrain (v0.2.4-dev) now has real content spread across it
via a one-time seeded batch pass, not just the original hand-placed objects
near spawn. This is a spot-check, not an exhaustive walk of all ~120 new
objects.

- [ ] **29 Trees** scattered across the map, each choppable exactly like the
  original hand-placed Tree — hold E, trains Gathering, drops Logs. Walk to a
  few far from spawn and confirm none are floating above or sunk into the
  terrain (each was placed via `GroundHeight` sampling at its own (x,z), so
  should sit flush with the hillside under it, not a flat assumption).
- [ ] **71 ore Boulders** scattered across the map, visually identical
  (`Boulder.prefab`) regardless of what's actually inside — confirm a handful
  in different areas: most break into plain Rock, but breaking enough should
  turn up Copper and Iron (visibly tagged as ore before breaking, same as the
  original Copper/Iron Ore Nodes) and, more rarely, a disguised Silver/Gold/
  Platinum node that only reveals its true material once cracked open or
  checked with a Mining Face Shield. Confirm tool-gating still applies
  per-tier same as the original named Ore Nodes. Also confirm each scattered
  Boulder still works as an `AnvilSurface` crafting proximity point.
- [ ] **10 Berry Bush + 10 Herb Bush** scattered across the map, each
  gatherable exactly like the original hand-placed one.
- [ ] **Spacing feels natural, not crowded or robotic** — scattered objects
  keep at least a few meters from each other and from hand-placed important
  objects (spawn point, Water Puddle, storage boxes, Campfire); flag any spot
  where two objects visually overlap or crowd a walking path.
- [ ] **Copper/Iron non-disguised (confirmed 2026-08-11)** — these two
  always show their true ore color/material immediately, same as before
  scattering; Silver/Gold/Platinum stay hidden until revealed. Confirmed
  intentional by Ben, not an open question (see `CHANGELOG.md` v0.2.6-dev).
- [ ] **Terrain renders real grass, not magenta (fixed v0.2.7-dev).** If the
  ground ever shows solid magenta again after a Terrain-related change,
  it's the same class of bug — check `Terrain.materialTemplate` isn't null.
- [ ] **Trees/Boulders actually visible, not buried (fixed v0.2.8-dev).** If
  a future batch-scattered prefab type shows up missing/sunk into the
  ground, check whether it needs the same pivot-offset treatment (compare
  a hand-placed instance's Y against a fresh `GroundHeight.Sample` at its
  position — a nonzero diff is that prefab's own pivot-to-base offset).
- [ ] **5 Wolves scattered (v0.2.9-dev)** — each should behave exactly like
  the two original hand-placed Wolves (chase/attack the player within
  range, drop a Wolf Pelt on death). Confirm none spawned suspiciously
  close to the player's start position — all 5 should be well out
  (verified 56.9 units minimum at scatter time, but worth an eyeball
  check that none feel like an unfair ambush right at spawn).
- [ ] **5 NPCs scattered (v0.2.9-dev)** — each is an independent, unhired
  `NPCFactoryWorker` findable across the map, same Talk/Hire flow as the
  original. Confirm hiring more than one and assigning them to Mining works
  without one NPC's state (job, tools, cargo, skill growth) bleeding into
  another's — this is the actual point of scattering multiple, not just
  more content. Try assigning two different NPCs to two different
  deposit `StorageBox`es (not just the same one) to confirm per-NPC
  deposit targeting really works as advertised.

## 17. Combat Boots — Civilian/Hiking/Military (v0.3.0-dev)

Spawnable via the Admin tab's item list (Editor Play Mode only) — no
recipe exists yet. All three use the same `CombatBoot.glb` visual
(deliberately, per Ben's ask); the difference is purely in equippable
behavior.

- [ ] **A set of Military Boots sits near spawn as real starting gear
  (v0.3.2-dev)** — "Military Boots (Starting Gear)", ~1.6 units from the
  Player, same pickup flow as the Stick/Canteen/Backpack cluster already
  there. Confirm it's visible and reachable right at game start, sitting
  flush on the ground (not floating or sunk), and picks up/equips exactly
  like an Admin-spawned one.

- [ ] **All three equip into the Feet slot** (Tab → Inventory) exactly
  like any other equippable — drag from inventory onto the Feet row.
  **Known gap, not a bug:** nothing renders on the player when worn (no
  visible player body exists yet — see the NPC Model/Animation plan in
  `BUGS_AND_ENHANCEMENTS.md`), so "equipped" is bookkeeping-only for now,
  same as every other worn item today.
- [ ] **Civilian Boots** have no special slots — equip/unequip only.
- [ ] **Hiking Boots** have one restricted slot, "Knife Sheath" — confirm
  any tier of Knife (Crude through Masterwork) can go in it, and that a
  non-Knife item (a Rock, a Stick, anything else) is refused. This is a
  brand-new mechanism (`Inventory.restrictedTo`) — worth confirming it
  actually blocks the wrong item type rather than silently accepting it.
- [ ] **Military Boots** have two slots — "Knife Sheath" (same as Hiking)
  plus a "Pistol Holster" that should refuse *everything* right now — no
  Pistol item exists yet, this is intentional, not a bug to report.
- [ ] **Knife Sheath/Pistol Holster now have real UI (v0.3.1-dev).** Once
  a Hiking or Military Boot is equipped (Feet slot), its slot(s) appear in
  the "Inventory" side panel next to Backpack/Belt contents, each with its
  own contents grid. To fill one: click a Knife anywhere (main inventory,
  a hand, a container) to open the move popup, then look for a "To Knife
  Sheath" button — same pattern as "To Backpack". Confirm a non-Knife item
  (Rock, Stick, anything else) offered the same button either doesn't show
  it or silently fails to land — should never actually occupy the slot.
  Confirm the Pistol Holster's button appears too but genuinely can't be
  filled by anything (no Pistol item exists yet — intentional).
- [ ] **Unequipping/dropping a worn Boot** should behave exactly like
  Backpack/Belt — Unequip returns it to a free inventory/hand slot (or
  drops it if nothing fits), Drop places it in the world. Whatever's still
  sitting in its Knife Sheath at that point stays with the boot (not
  spilled into the player's inventory) — confirm that's actually true,
  not assumed.

## 18. NPC Visual (Human Character Dummy, v0.3.4-dev/v0.3.5-dev)

- [ ] **Idle pose and facing — both confirmed working (v0.3.5-dev).** NPCs
  stand with arms down (not T-pose) and correctly face their direction of
  travel while wandering.
- [ ] **NPCs should stand flush with the ground, not partway sunk in.**
  Started once the idle animation was wired in (v0.3.5-dev). First fix
  attempt (`NPCVisualGroundFix.cs`'s one-shot `LateUpdate`) didn't hold up
  under live testing — **Regression (v0.3.5-dev → still broken as of
  that version):** sinking persisted after the one-shot fix.
  - **v0.3.7-dev retry, needs a fresh live check:** script now corrects
    every `LateUpdate` instead of once (working theory: the one-shot
    version measured bounds before the Animator had evaluated its first
    real pose, computed ~zero correction, then permanently disabled
    itself). To verify: watch an NPC in Play mode for a full idle cycle,
    confirm feet stay flush with the ground continuously, not just at
    the moment it spawns. If still sinking, pause Play mode, select an
    NPC, and check what `Visual`'s local Y actually is relative to the
    measured `feetOffset` (the old `corrected` flag no longer exists in
    this version) rather than more screenshot round-trips.

## 19. Iron Ingot & the Furnace crafting gate (v0.3.8-dev)

- [ ] **Furnace is visible and positioned reasonably in `TestScene.unity`**
  — ~2.5m from the Anvil, sitting on the ground (not floating/sunk).
  Built from `Assets/Models/CrudeFurnace.glb`, not yet live-viewed in Play
  mode (only verified via saved-scene YAML this session).
- [ ] **Iron Ingot recipe appears in the Crafting tab** under the
  Metalworking discipline (or via Search — "Iron Ingot"), showing "10x
  Iron" as its ingredient.
- [ ] **Crafting is blocked far from the Furnace, even with 10+ Iron in
  inventory** — Craft button disabled, "— requires a Furnace nearby"
  warning shown (same shape as the existing Anvil warning).
- [ ] **Crafting succeeds within ~2m of the Furnace** with 10+ Iron on
  hand — consumes 10 Iron, produces 1 Iron Ingot, trains Metalworking.
- [ ] **Iron Ingot's icon and world-drop model both look like a metal bar**,
  not the generic rock placeholder Iron itself still uses — confirm by
  dropping a crafted Ingot and looking at it directly, not just the
  inventory icon.
- [ ] **Admin Spawn Screen search box** — type "iron" and confirm both
  "Iron" and "Iron Ingot" show up filtered from the full list; confirm
  Clear restores the full list. Spawning "Iron Ingot" from here should
  place a real ingot-shaped pickup, not a rock.

## 20. Pickupable Log + wood-item weights + FuelItem data layer (v0.3.25-dev)

- [ ] **Chop a Tree** with an Axe — confirm it still drops 3 fallen Log
  nodes exactly as before (regression check — Tree/chop behavior is
  unchanged by this work).
- [ ] **Approach a fallen Log node and look at the interaction prompt** —
  confirm it now shows both the primary "[E] Hold to break (requires
  Axe)" prompt **and** a secondary "[F] Pick up Log" prompt at the same
  time.
- [ ] **Press F on a Log node** (no Axe needed) — confirm a Log item
  (weight 15 lbs) lands in your inventory and the node disappears from
  the world with no respawn. Confirm this works with an empty hand (no
  tool requirement).
- [ ] **With your inventory full, press F on a Log node** — confirm the
  Log item is *not* added, and the node stays in the world (not silently
  destroyed/lost).
- [ ] **Chop a Log node with an Axe (E, hold) instead of picking it up**
  — confirm this still yields 2x Plank + the existing 30% chance of a
  bonus Stick, unchanged from before (regression check — the two
  interactions are independent alternatives, not a replacement of one by
  the other).
- [ ] **Open Inventory and check weights**: Stick and each of the 5
  Trimmed Stick craft-tiers should show 0.5 lbs; Plank should show 3 lbs;
  Log should show 15 lbs (check via `PlayerEncumbrance`'s total or by
  comparing capacity used before/after picking one up).
- [ ] **Drop a picked-up Log** — confirm it lands in the world looking
  identical to a naturally fallen Log node (same cylinder mesh/material),
  and can be picked back up again via the same F-key interaction.
- [ ] **Admin-spawn a Log directly** — confirm it appears in the search
  results and spawns a real Log pickup, not a placeholder cube.
- [x] **Furnace fuel/lit/burn-timer UI now exists** — see section 22
  (v0.3.31-dev). This bullet originally flagged fuel as data-only; no
  longer accurate, kept here as a pointer forward rather than deleted.

## 21. Campfire rebuilt — craftable, fuel, cooking, warmth, model, popup (v0.3.26-dev — v0.3.30-dev)

Rewritten 2026-08-13 (twice — v0.3.28-dev's E-key popup, then
v0.3.30-dev's drag-and-drop/recipe rework) and for the Blender model
rebuild (v0.3.27-dev). The old "Campfire (nearby)" Inventory-tab steps
are long gone (removed entirely in favor of `CampfireScreen`), and so is
v0.3.28-dev's simple Add-1/Take button flow — cooking is now drag-and-
drop with utensils, multi-slot ingredients/output, and a manual Recipe
button. **Not yet walked through even once — this pass matters more
than usual given how much changed.**

- [ ] **Open the Build tab and find "Campfire"** in the piece list —
  confirm it shows an icon (not blank), and its cost reads 4 Rock + 3
  Stick.
- [ ] **Place a Campfire** via the normal Build flow (free placement, no
  socket snapping expected — it should behave exactly like placing a
  Storage Box: ghost follows the raycast, scroll rotates, click locks,
  click again confirms) — confirm materials are actually consumed and a
  real Campfire appears in the world, unlit. **Confirm the model itself**
  is the new Blender build (a ring of rocks around a pile of charred
  sticks), not the old scaled-cylinder placeholder.
- [ ] **Look at the unlit placed Campfire** — confirm the prompt reads
  "Open Campfire" (E).
- [ ] **Press E** — confirm a popup opens (larger now — scrollable
  content area), cursor unlocks, showing "Unlit", a Fuel box, a Cooking
  Utensils row (4 boxes: Grill/Cooking Pot/Kettle/Frying Pan), an
  Ingredients row (4 boxes), a Cooked Items row (4 boxes), a Recipe
  section, a Transfer section (Backpack + hands), and a Light button
  (disabled since there's no fuel yet).
- [ ] **Drag fuel in**: with a Stick in your Backpack or a hand, drag it
  from the Transfer section onto the Fuel box — confirm it lands there
  and leaves the source. Try dragging a non-wood/non-fuel item onto the
  Fuel box — confirm it's rejected (snaps back, nothing moves).
- [ ] **Drag-and-drop basics**: press-and-hold an occupied box, confirm a
  ghost icon/label follows the cursor once you've moved far enough
  (small clicks shouldn't start a drag), confirm the box under the
  cursor highlights yellow while dragging, and releasing over an invalid
  target (or empty space outside any box) leaves the item exactly where
  it started.
- [ ] **Light it**: click the Light button — confirm it's now enabled,
  clicking it lights the fire (visual/light turns on, wood renderer swaps
  to the ember-glow lit material — rocks should NOT change), and the
  popup's status line updates to "Lit — Ns of fuel left" counting down.
- [ ] **Close via the Close button**, and separately via Escape — confirm
  both re-lock the cursor and return to normal gameplay, and confirm a
  drag in progress doesn't get stuck if you close mid-drag.
- [ ] **Let fuel run out** (or load just 1 Stick and wait ~5 min) —
  confirm the Campfire extinguishes on its own (wood renderer reverts to
  the unlit charred material, rocks unaffected), fire light turns off,
  with no player action needed.
- [ ] **Re-light via Spark** instead of the popup's Light button — with an
  Elemental-lineage character, load fuel via the popup, close it, then
  aim at the Campfire and **hold R** (not tap — no on-screen feedback by
  design) for a few seconds. Confirm it lights as an alternate path.
  Confirm Spark offers nothing (R does nothing) on a Campfire with no
  fuel loaded.
- [ ] **Cooking Utensils**: drag a Grill (Admin Spawn it — no recipe
  exists yet) into the Grill box — confirm it only accepts a Grill,
  rejecting anything else. Confirm all 4 utensil boxes work
  independently and can all hold an item simultaneously.
- [ ] **Ingredients + Recipe list**: with the Campfire lit, drag 1 Raw
  Meat into an Ingredients box — confirm the Recipe section now shows a
  "Cook Cooked Meat x1" button (Raw Meat → Cooked Meat needs no
  accessory). Confirm the Recipe section reads "No recipes available..."
  when the ingredients/utensils don't satisfy anything.
- [ ] **Start cooking**: click the Recipe button — confirm the Raw Meat
  is immediately consumed from the Ingredients box, the Recipe section
  now shows "Cooking Cooked Meat — N%" counting up, and after ~30
  seconds the finished Cooked Meat appears in a Cooked Items box. Walk
  away mid-cook, confirm progress pauses (doesn't restart from 0). Let
  the fire go out mid-cook — confirm cooking pauses too, then resumes
  once relit.
- [ ] **Take cooked items out**: drag a Cooked Meat from a Cooked Items
  box to your Backpack or a hand — confirm it moves normally. Confirm
  you can NOT drag anything INTO a Cooked Items box (system-populated
  only).
- [ ] **Eat the Cooked Meat** — confirm a right-click Eat option appears
  and restores Hunger (Meal tier, ~40) with no Health effect. Confirm Raw
  Meat itself has **no** Eat option at all (still not directly edible —
  cooking is required).
- [ ] **Cooked Meat's icon/model** — expected to look identical to Raw
  Meat (a known placeholder simplification, not a bug) — confirm it's at
  least not blank/missing. Same for the 4 utensil items (Grill/Cooking
  Pot/Kettle/Frying Pan) — expect a blank/generic box, not broken.
  Confirm all 4 are findable via Admin Spawn's item search.
  Also try dragging a Left/Right Hand-held item into the Transfer
  section's boxes and back — confirm nothing gets orphaned (the
  equipment-reference gotcha this project has hit before).
- [ ] **Warmth:** check the debug vitals panel or the new HUD bar (bottom
  of screen, 4th row under Will) for Body Temperature. Stand within ~4m of
  a lit Campfire — confirm the value climbs toward ~80 over a few seconds.
  Walk away — confirm it drifts back down toward the neutral baseline
  (50) over time, same passive drift as before this feature existed.
- [ ] **Multiple Campfires**: place a second Campfire, confirm pressing E
  on each opens *that specific* one's popup (fuel/utensils/ingredients/
  output/lit-state are genuinely per-instance, not shared).
- [ ] **Regression:** confirm ordinary crafting/other Build pieces still
  work normally, and confirm StorageBox's own nearby-Inventory mechanism
  (still using the old pattern, untouched by this change) still works —
  this change touched shared code (`FirstPersonController.cs`) alongside
  Campfire-specific additions. Confirm `InventoryScreen`'s own drag-and-
  drop still works exactly as before (CampfireScreen's is a separate,
  self-contained implementation, but worth confirming nothing regressed).

## 22. Furnace real state + unattended automation (v0.3.31-dev)

New — not yet walked through in Play mode at all. `Furnace.cs`/
`FurnaceScreen.cs` are entirely new; the scene's existing `Furnace`
GameObject (`FurnaceSurface.cs`, untouched) gained the new component.

- [ ] **Look at the placed Furnace in `TestScene.unity`** — confirm the
  prompt reads "Open Furnace" (E), same convention as Campfire's popup.
- [ ] **Press E** — confirm a popup opens (responsive sizing, same as
  Campfire's), cursor unlocks, showing "Unlit", an Output row (4 boxes),
  a Smelting Queue section (0/4), a Transfer section (Backpack + Hands),
  a Materials row (4 boxes), a Fuel row (2 boxes), and an Automation
  section (Auto-Run toggle + Fuel Source/Materials Source/Output Box
  pickers, each currently "(none)").
- [ ] **Regression check — `IronIngotRecipe` still works as before**:
  craft an Iron Ingot from the Crafting tab near the Furnace (skill-gated,
  10 Iron → 1 Iron Ingot) exactly as it did pre-v0.3.31-dev — this new
  system shouldn't have touched that path (`FurnaceSurface`/
  `PlayerCrafting.HasNearbyFurnace` are unrelated to the new `Furnace.cs`).
- [ ] **Drag fuel and materials in manually**: with Sticks/Planks and 10+
  Iron in your Backpack or hands, drag them from the Transfer section onto
  the Fuel and Materials rows — confirm they land and leave the source,
  and confirm a non-fuel item is rejected by the Fuel row, same rejection
  behavior as Campfire's Fuel box.
- [ ] **Queue a recipe**: click "Iron Ingot x1 (60s)" in the Smelting
  Queue section — confirm it shows `[Queued]` and the counter reads 1/4.
  Click it again — confirm it un-queues (counter back to 0/4).
- [ ] **Turn Auto-Run on with fuel + a queued recipe + 10 Iron already
  loaded** — confirm the Furnace lights itself within a frame or two (no
  Light button anywhere in this UI, unlike Campfire) and the Smelting
  Queue section shows live progress toward "Iron Ingot — X%". After 60s,
  confirm 1 Iron Ingot lands in the Output row and materials drop by 10
  Iron.
- [ ] **Turn Auto-Run off** — confirm the Furnace doesn't auto-light again
  once its current fuel burns out, and doesn't auto-refill from any linked
  box, but anything already lit/mid-smelt keeps running to completion.
- [ ] **StorageBox links**: place a StorageBox within the Furnace's link
  range (~10m) with some Sticks in it, open the Furnace popup, confirm it
  appears as a button under "Fuel Source" — click it, confirm the label
  updates to that box's name and a "Clear" button appears. With Auto-Run
  on and the Furnace's on-board Fuel row empty, confirm Sticks migrate
  from the box into the Fuel row on their own within a few seconds, with
  no player interaction. Repeat for Materials Source (Iron) and Output Box
  (confirm smelted Ingots migrate out of the Output row into the assigned
  box automatically).
- [ ] **Move a linked StorageBox out of range** (or its contents added
  elsewhere) — confirm auto-feed/drain simply stops for that link
  (no error, no crash) until it's back in range.
- [ ] **Multiple recipes in the queue**: register a second `SmeltableItem`
  (or use Admin Spawn to get materials for one, if a second exists by the
  time this is tested) and queue both — confirm the Furnace round-robins
  between them rather than only ever running the first one repeatedly.
- [ ] **True unattended check**: queue a recipe, turn Auto-Run on, load
  fuel + materials, close the popup, and walk far enough away that the
  Furnace would be off-screen/unloaded-feeling — confirm smelting keeps
  progressing in real time (check by walking back later) even though
  nothing was watching it, since `Update()` ticks regardless of player
  proximity or popup state.
- [ ] **Regression:** confirm Campfire's popup, fuel, and cooking still
  work exactly as before (shared code touched: `FirstPersonController.cs`
  gained a `furnaceScreen` field alongside the existing `campfireScreen`
  one) and StorageBox's own nearby-Inventory mechanism is unaffected.

## 23. NPC job generalization: Woodworking + Berry/Herb foraging (v0.3.32-dev)

New — not yet walked through in Play mode at all. `NPCMining.cs` renamed
to `NPCGathering.cs`; `ChoppableTree`/`BerryBush`/`HerbBush`/`Pickup` all
gained new NPC-facing code paths.

- [ ] **Regression — Mining still works exactly as before**: hire an NPC,
  assign Mine Ore (equip Pickaxe/Mining Face Shield/Backpack), confirm it
  mines ore nodes and deposits into its assigned `StorageBox` exactly as
  it did pre-v0.3.32-dev — the rename/generalization shouldn't have
  changed this job's behavior at all.
- [ ] **Chop Wood — fallen Log nodes**: hire a second NPC, assign Chop
  Wood, give it an Axe + Backpack. Confirm it walks to and chops a fallen
  Log node, yielding Plank (+ occasional Stick) into cargo, same as the
  player's own Axe-chop on a Log node.
- [ ] **Chop Wood — standing Trees**: with the same NPC, confirm it also
  walks to and fells a standing Tree — the tree should become a stump
  (same visual as a player-chopped one), and **no scattered Log objects
  should spawn** for this NPC-driven chop (the direct-yield path skips the
  physical scatter entirely) — confirm Log items appear directly in the
  NPC's cargo instead. Confirm the stump regrows on the same timer as a
  player-chopped one.
- [ ] **Mixed Woodworking cargo**: let the Chop Wood NPC keep working
  until its cargo fills or nothing's left in range — confirm it deposits
  a natural mix of Log/Plank/Stick into its `StorageBox` (whatever it
  actually happened to harvest), not just one item type.
- [ ] **Forage — Berry Bush**: hire a third NPC, assign Forage (Backpack
  only, no other tool). Confirm it walks to a Berry Bush, triggers the
  search (bush's cooldown starts, same as a player's F action), then
  **separately walks over to and collects the scattered Berry pickups**
  that search produced — this should visibly read as two steps, not an
  instant grant. Confirm it does *not* use the bush's E-chop action (no
  Trimmed Sticks should ever come from this NPC).
- [ ] **Forage — Herb Bush**: same NPC, confirm it also searches and
  collects from a nearby Herb Bush.
- [ ] **Regression — Mining/Woodworking don't chase loose pickups
  (v0.3.37-dev fix)**: with ore and a stray Stick/Herb/other loose item
  both in range of a Mine Ore or Chop Wood NPC, confirm it goes straight
  for its own job's targets and never detours for the unrelated loose
  item — this was a real live bug (a Mining NPC got "stuck gathering
  sticks"), not just the originally-flagged side effect. Only a Forage NPC
  should ever pick up loose items (`NPCJobDefinition.collectLoosePickups`,
  Forage-only).
- [ ] **Forage still collects loose items near it**: drop an unrelated
  item near a Forage NPC — confirm it still picks it up on its way past
  (this part of the original side effect is intentional and unchanged —
  only Mining/Woodworking got the behavior turned off).
- [ ] **All three jobs running at once**: confirm a Mining NPC, a Chop
  Wood NPC, and a Forage NPC operating simultaneously don't interfere with
  each other (each only pursues targets its own tools/job allow).
- [ ] **`NPCJobScreen` shows all three families**: open the Assign Job
  screen for any hired NPC — confirm Mining, Woodworking, and Gathering
  all appear as tabs, each showing its one job.
- [ ] **Regression — NPCDialogue**: talk to a working NPC (any job) —
  confirm it still pauses correctly (this file's `mining` field became
  `gathering`, verify nothing broke in that rename) and resumes its job
  after the dialogue line ends.

## 24. NPC animation — locomotion + per-job work actions (v0.3.33-dev)

New — not yet walked through in Play mode at all. Per `CLAUDE.md`'s
Humanoid-retargeting gotcha, batch mode can't verify any of this visually,
so every item below needs a real look, not just a compile/YAML check.

- [ ] **Idle → Walk**: watch an unassigned NPC wander (`NPCWander`). Confirm
  it plays the Idle pose while stationary and transitions smoothly into the
  Walk cycle as soon as it starts moving toward a wander target, back to
  Idle once it arrives and pauses. No foot sliding during Walk.
- [ ] **Ground contact still holds under the new states**: this is the
  specific regression risk flagged when building this — `NPCVisualGroundFix`
  was tuned against the old placeholder Idle pose only. Watch closely for
  sinking/floating on Walk and on each Work pose below, not just Idle.
- [ ] **Mining work animation**: hire an NPC, assign Mine Ore, watch it
  approach an ore node. Confirm it switches into the mining animation once
  in range (roughly when it stops moving) and holds it for the ~3s harvest
  window, then returns to Idle/Walk once it moves to the next target.
- [ ] **Chopping work animation**: same check with a Chop Wood NPC on a
  Log node or standing Tree — confirm the chopping animation plays during
  the harvest window.
- [ ] **Gathering work animation**: same check with a Forage NPC at a
  Berry/Herb bush — confirm the gathering animation plays during the
  search's dwell window.
- [ ] **Correct animation per job, not a generic one**: with two NPCs
  assigned to different jobs (e.g. one Mining, one Chopping) working near
  each other, confirm each plays its *own* job's animation, not either
  defaulting to the other or to Idle while "working."
- [ ] **Return-to-deposit still animates as Walk**: once an NPC's cargo
  fills and it heads back to its `StorageBox` (`NPCGathering`'s
  `isReturning` state), confirm it plays Walk during that trip, not Idle
  or a stuck Work pose.
- [ ] **Male vs Female prefab both work**: repeat at least the Idle/Walk/
  one Work-state check on both `NPCFactoryWorkerMale` and
  `NPCFactoryWorkerFemale` — they use separate Animator Controllers
  (`NPCAnimatorMale.controller`/`NPCAnimatorFemale.controller`), built by
  the same script but not literally the same asset, so a mistake in one
  wouldn't necessarily show up in the other.

## 25. Player visible body + first/third-person camera toggle (v0.3.34-dev)

New — not yet walked through in Play mode at all. Per `CLAUDE.md`, Humanoid
retargeting can't be verified headlessly, so every item below needs a real
look.

- [ ] **First-person view unchanged (regression)**: before touching V at
  all, confirm the game looks and plays exactly as before this feature —
  no visible body parts in view, no camera position/FOV change, normal
  look/move feel. This is the most important check: it's easy for a body
  attachment mistake to leak into the first-person view.
- [ ] **V toggles to third person**: press V, confirm the camera pulls back
  to a behind-the-shoulder view and the player's own body becomes visible
  (KI dummy, standing pose matching current movement).
- [ ] **V toggles back to first person**: press V again, confirm the camera
  snaps back to the normal eye-height first-person view with no lingering
  offset or wrong pitch.
- [ ] **Camera collision**: in third person, walk backward into a wall or
  large object — confirm the camera pulls in smoothly rather than clipping
  through the obstruction, and pulls back out once clear.
- [ ] **Locomotion animates correctly in third person, Standing stance**:
  confirm Idle/Walk/Sprint all play the right clip as you start moving,
  walk, and hold Shift to sprint.
- [ ] **All 4 stances animate correctly**: toggle through Kneel (X)/Crawl
  (C)/Prone (Z) while in third person — confirm each stance shows its own
  idle and walk animation (not stuck on Standing's), and that switching
  stances doesn't get the Animator stuck mid-transition or replaying the
  wrong pose repeatedly (the `StanceChanged`-trigger gating exists
  specifically to prevent this — worth watching closely).
- [ ] **Ground contact**: watch for foot sliding on Walk and any
  sinking/floating across all 4 stances in third person — same
  Humanoid-retargeting risk class already hit once on the NPC pass
  (`NPCVisualGroundFix`, reused as-is here).
- [ ] **Worn equipment becomes visible in third person**: equip something
  (e.g. a Backpack), toggle to third person — confirm it's visible on the
  body's layer alongside the body itself (this is a side effect of the
  `cullingMask` toggle, not dedicated equipment-attachment code — the item
  won't be *positioned* on the body via a bone attachment, just no longer
  camera-culled, so don't expect it to look anchored correctly).
- [ ] **Toggle while stationary and while moving** — confirm no popping or
  wrong-pose flash at the instant of the switch either way.

## 26. Player body Male/Female toggle (v0.3.35-dev)

New — not yet walked through in Play mode at all. Depends on section 25
(third-person view) to actually see the body.

- [ ] **Default is Male**: fresh Play-mode start, press ` then check the
  Player tab — confirm "Male" reads as selected (highlighted tab style)
  and toggle to third person (V) — confirm the visible body is the same
  Male model section 25 already tested, unchanged.
- [ ] **Switch to Female**: with the ` menu open on the Player tab, click
  "Female" — confirm the tab highlight swaps, and (in third person) the
  visible body switches to the Female model instantly, no popping/T-pose
  flash.
- [ ] **Female animates correctly**: same checklist as section 25's
  locomotion/stance items, but for the Female model — Idle/Walk/Sprint,
  all 4 stances, ground contact (no sliding/sinking) — confirm
  `NPCVisualGroundFix`'s correction re-measures properly for the new
  model's own bind pose rather than carrying over any Male-specific
  offset.
- [ ] **Switch back to Male**: toggle Female → Male — confirm it reverts
  cleanly, no leftover Female mesh visible, animations correct again.
- [ ] **Toggle while in first person**: switch gender while still in
  first-person view (before pressing V) — confirm nothing looks wrong
  once you do switch to third person afterward (the swap shouldn't
  require being in third person to take effect correctly).
- [ ] **Toggle while moving**: switch gender mid-walk — confirm the new
  model picks up the current Speed/Stance state immediately (walking, not
  reset to Idle).
- [ ] **Regression**: confirm equipment visibility in third person
  (section 25's last-but-one item) still works after a gender switch, and
  that switching gender repeatedly back and forth doesn't leak a second
  active instance or leave both/neither model visible.

## 27. NPC equipment visual attachment (v0.3.38-dev)

New — not yet walked through in Play mode at all. First real look at
whether the attach-point numbers (section on `ToolRequirement.attachBone`/
offsets) actually look right — expect these need tuning.

- [ ] **Hire an NPC, assign Mine Ore, give it a Pickaxe** — confirm a
  Pickaxe model appears in its right hand (previously nothing appeared no
  matter what was given). Don't expect a perfect grip — this is the first
  unverified pass; note how far off it looks for a follow-up tuning pass.
- [ ] **Give it a Mining Face Shield** — confirm a shield model appears
  near its head/face, roughly forward-facing.
- [ ] **Give it a Backpack (any tier)** — confirm a backpack model appears
  on its back, not floating in front of the chest or sideways (the 180°
  turn is meant to flip a front-facing dropped-pickup model to face
  backward — confirm that actually worked instead of pointing it the
  wrong way).
- [ ] **Assign Chop Wood, give it an Axe** — confirm an Axe model appears
  in the right hand, same slot the Pickaxe used (fire/reassign first,
  since a single NPC only holds one job).
- [ ] **Fire the NPC** — confirm all attached equipment models disappear
  along with the job/tools being cleared (no leftover Pickaxe/Backpack
  still visible on an unassigned NPC).
- [ ] **Re-hire and re-assign** — confirm equipment models reappear
  correctly once tools are re-given, not stuck permanently empty after a
  Fire.
- [ ] **Both genders**: confirm equipment attaches correctly on both
  `NPCFactoryWorkerMale` and `NPCFactoryWorkerFemale` instances (separate
  Animator per prefab — confirm neither one silently has a null
  `animator` reference doing nothing).
- [ ] **Regression — Backpack/Pickaxe fixes (v0.3.36-dev)**: confirm a
  Fine (or any other tier) Backpack and any Pickaxe tier can now actually
  be given via the Assign Job screen's Give button — this was the live
  bug that started this whole chain.
- [ ] **Regression — Mining doesn't chase Sticks (v0.3.37-dev)**: confirm
  a Mining NPC with a full requirement set actually walks to and mines ore
  nodes, plays its Mining work animation, and doesn't detour for unrelated
  loose items.

## 28. Player equipment visual attachment (v0.3.40-dev)

New — not yet walked through in Play mode at all. This is the one Ben can
test directly (third person, V key) rather than only watching an NPC.

- [ ] **Equip a tool, look in third person**: equip any tool (Knife/
  Pickaxe/Hammer/Axe) to a hand via the Inventory screen, press V for
  third person — confirm it appears roughly in the right hand, following
  along as the character moves/turns (not floating at a fixed world
  position).
- [ ] **Equip a Backpack, look in third person**: confirm it appears on
  the back, not floating in front of the chest — this is the exact
  placement mistake just fixed for NPCs (root-relative offset math), so
  it should already be correct, not need the same fix twice.
- [ ] **Switch gender while holding a tool/wearing a Backpack**: open the
  ` menu's Player tab, toggle Male ↔ Female — confirm the held/worn items
  re-anchor onto the new model's own hands/chest instead of staying
  attached to the now-invisible previous model (or disappearing
  entirely).
- [ ] **Unequip/drop, re-equip**: confirm nothing about the visual
  attachment breaks the underlying pickup/drop/inventory-move flow —
  this only changes *where* the physical object sits while equipped, not
  how it's carried logically.
- [ ] **First-person view unchanged**: confirm equipped items still don't
  appear in first-person view (no first-person view-model exists —
  this change shouldn't have altered that).
- [ ] **Compare against the NPC pass**: since both use the same
  `EquipmentAttach` math and the same starting Backpack offset numbers,
  confirm the player's Backpack placement looks similar to whatever the
  NPC's ends up looking like once that's checked too (section 27) — if
  one looks right and the other doesn't, that's a real discrepancy worth
  tracking down, not just "needs more tuning."

## 29. Full equipment-visual sweep (v0.3.41-dev)

New — not yet walked through in Play mode at all. Covers the real bug fix
plus all 9 newly bone-attached types.

- [ ] **Pickaxe bug fix, the actual repro that started this chain**: walk
  up to a Pickaxe lying in the world and pick it up normally (E) with a
  free hand — **not** via the Inventory screen's Equip button — confirm
  it now appears correctly in the hand in third person. This is the exact
  case that was broken (`PlayerLoot.ReceiveEquipment`'s hand-fill path).
- [ ] **Canteen, same bug class**: pick a Canteen up off the ground into a
  free hand — confirm it's correctly hand-positioned, not floating at the
  player's root.
- [ ] **Boots**: equip Boots/Sneakers — confirm they sit roughly at the
  feet, not floating elsewhere. Walk around — a single Hips-anchor won't
  perfectly track each foot during the walk cycle (documented limitation,
  not a bug) — note how bad it actually looks live; if it reads as
  clearly wrong rather than "a little loose," that's worth a follow-up
  per-foot-mesh redesign, not just an offset tweak.
- [ ] **Belt**: equip a Belt — confirm it sits at the waist/hips.
- [ ] **Canteen clipped to Belt**: with a Belt worn, clip a Canteen to it
  (not held in a hand) — confirm it sits to the side of the belt, not
  overlapping/inside it.
- [ ] **Sunglasses / Mining Face Shield**: equip either — confirm it sits
  on the face, forward of the head, not centered inside the skull.
- [ ] **Personal Health Monitor / Navigation Computer, both wrists**:
  equip one on Left Wrist and (a second, or switch) one on Right Wrist —
  confirm each actually lands on the *correct* wrist (not both collapsing
  onto the same arm the way Tool's hand attachment deliberately does).
- [ ] **Shirt / Jeans**: confirm both still look reasonable worn (body-
  conforming items with a zero offset — should look closest to "already
  correct" of everything in this pass, since they're not floating props).
- [ ] **Gender switch with everything equipped at once**: equip as many
  of the 11 types simultaneously as possible (both hands, backpack, belt,
  canteen clipped to belt, both wrists, face slot, shirt, jeans, boots),
  then toggle Male ↔ Female in the ` menu — confirm every single item
  re-anchors onto the new model instead of some subset staying attached
  to the now-invisible previous body.
- [ ] **Regression — inventory-screen equip still works for everything**:
  confirm equipping each type via the Inventory screen's Equip button
  (not just world pickup) still results in correct placement — this
  path already worked for Tool/Backpack pre-sweep; shouldn't have
  regressed for anything.

## 30. Save/load persistence, v1 (v0.3.51-dev)

Live-tested 2026-08-13 (Ben, real Editor-restart round trip, not just
re-entering Play mode). First pass confirmed:

- [x] **Worn equipment, plain**: Backpack equipped, saved, exited and
  relaunched — still worn and correctly bone-attached (visible on the
  back, not floating/detached).
- [x] **Nested equipment state, the hard case — partial**: a worn Settler's
  Belt with a Canteen clipped to it also survived the same round trip
  unprompted (it was already equipped from starting gear). Then
  specifically tested: 11 Sticks placed in the worn Backpack's contents
  grid, saved, exited, relaunched — stack (and exact quantity) intact in
  the same slot. **Still not directly confirmed**: the Canteen's liquid
  type/amount specifically (it just happened to still be clipped to the
  Belt; wasn't checked whether it was filled or what happened to the fill
  state).
- [ ] **Basic round-trip, full**: change a few vitals (take damage,
  eat/drink), confirm health/hunger/thirst/stamina/position all come back
  exactly, not just inventory contents (inventory contents alone already
  confirmed above).
- [ ] **Canteen liquid specifically**: fill a Canteen with Water, clip it
  to a worn Belt (or hold it), save/reload — confirm it's still full of
  Water, not reset to empty.
- [ ] **StorageBox**: store some items in a box, rename it, save/reload —
  confirm the name and contents both persist.
- [ ] **ResourceNode mid-respawn**: break an ore node so it's mid-respawn-
  timer, save/reload — confirm it's still unavailable and comes back at
  roughly the right time (not instantly available, not stuck forever).
- [ ] **Hireable NPC**: hire an NPC, assign a job, give it tools, let it
  gather a bit (cargo, skill gain), save/reload — confirm hired state,
  job, tools, cargo, and skill levels all persist. If a deposit container
  was set, confirm the cross-reference still points at the same box.
- [ ] **No save file yet**: delete/rename the save file (or test on a
  fresh install) — confirm the game starts normally with default
  starting-gear behavior, no error.
- [ ] **Regression — starting gear**: on a *fresh* save (no file), confirm
  the Settler's Belt/Canteen/Shirt/Jeans auto-equip sequence still runs
  normally (save/load shouldn't interfere with the no-save-file path).

## 31. Skill books (writing + reading), v1

New — not yet walked through in Play mode at all (verified so far only
via batch-mode compile + direct YAML grep of every new asset/prefab/scene
reference). Full design in `SKILL_BOOKS_PLANNING.md`. Two found
`SkillBook`s are already placed in `TestScene.unity` for convenience:
one near (4, 0, 4) targeting `MasterworkKnifeRecipe`, one near (-4, 0, 6)
targeting `SparkWish` — a quick way to test *reading* before ever
writing anything.

- [ ] **Basic crafting/weapon write → read loop**: get 1 Paper + 1 Ink
  (Admin Spawn, or gather a Plank/2 Berries and craft them via the new
  Paper/Ink recipes in the Crafting tab), open the Writing tab (Tab
  menu), pick a recipe you already know, click Write — confirm Paper +
  Ink are both consumed and a message shows the outcome. If it wasn't a
  failure, confirm a new Skill Book appears in your inventory. Open its
  action popup and click Read — confirm the book disappears and you can
  now craft the targeted recipe even *without* the normal skill level
  (test by writing/reading a recipe above your current tier).
- [ ] **Scope check — one recipe only**: after reading a book for one
  specific recipe (e.g. a Fine-tier item), confirm you still can't craft
  *other* recipes at that same tier you haven't separately unlocked —
  the grant should be scoped to exactly the one recipe, not the whole
  tier.
- [ ] **Basic magic write → read loop, unknown lineage**: as a character
  who only knows their starting lineage, write a book targeting a wish
  in a *different* lineage (only available if you already know that
  lineage — if not, use the pre-placed `SparkWish` found book instead
  to test the read half specifically). Reading it should grant both the
  lineage itself (check the Magic tab — the new lineage's wishes should
  now be listed under `KnownWishes`) *and* the specific wish, while a
  different wish in that same lineage (if one exists) should still be
  unusable.
- [ ] **Found books (no writing required)**: walk to each of the two
  pre-placed found books, pick them up, and Read each — confirm the
  Masterwork Knife recipe becomes craftable and the Spark wish becomes
  castable, with no Paper/Ink spent (found books skip writing entirely).
- [ ] **`SpectacularFailure` damage**: with low Intelligence and a
  high-tier subject (a deeply negative margin), write repeatedly until a
  `SpectacularFailure` lands — confirm it deals 2–10 damage and produces
  no book, and that Paper/Ink were still consumed despite the failure.
- [ ] **`BrilliantSuccess` lineage bonus**: with high Intelligence
  relative to the subject's tier, write a wish book repeatedly until a
  `BrilliantSuccess` lands — confirm the resulting book's read grants a
  starting lineage level somewhere in the 1–10 range instead of exactly
  0 (check the Skills tab or `SkillsScreen`'s lineage level display right
  after reading).
- [ ] **Intelligence actually trains**: note your Intelligence level
  before a writing/reading session, confirm it visibly increases after a
  few successful writes and reads (Player tab tile).
- [ ] **UI regression — Writing tab**: confirm the Writing tab shows
  "Nothing you currently know how to craft yet" / "You don't know any
  wishes yet" correctly on a fresh character with nothing craftable yet,
  and that the Paper/Ink count line turns into a warning color when
  either is at 0.
- [ ] **Not testable yet, by design**: NPC training (Phase 4) is blocked
  on NPC bench-crafting, which doesn't exist — skip until that ships.
