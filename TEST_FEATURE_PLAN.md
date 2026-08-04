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
- [ ] Bottom-left debug panel shows `Gridless <version>` matching `CHANGELOG.md`'s
  "Current version" line and `FirstPersonController.GameVersion`.
- [ ] **Ground texture (v0.1.53-dev):** the ground reads as a mottled green grass
  texture (dark/mid/light patches with fine blade-like detail), not a flat solid
  green. Walk across a wide stretch of it and check for an obvious visible
  repeating grid pattern or a hard seam line — some faint repetition is a known,
  accepted limitation of this first procedural pass (see `CHANGELOG.md`), but call
  it out if it's distracting rather than subtle.

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

## 3. Inventory Screen (I)

- [ ] **I** opens/closes the Inventory screen; **Escape** also closes it (and
  re-locks the cursor) — the two never disagree about open/closed state.
- [ ] I only opens while the cursor is already locked (can't stack on top of
  another open screen — try pressing I while Crafting/Skills/Bank/Lockbox/rename
  is open; nothing should happen).
- [ ] Main inventory list (4 slots) shows carried items with Eat/Drink (if
  edible/drinkable), Craft (if a known recipe), Drop, To Pack, To Storage buttons
  as applicable.
- [ ] Equipment section lists all 14 slots (Head, Face ×2, Neck, Chest, Back, Left/
  Right Arm, Left/Right Wrist, Left/Right Hand, Waist, Leg, Feet) — empty ones show
  "Empty", occupied ones show the item name plus Equip/Unequip/Drop as applicable.
- [ ] Equipping a container (Backpack) into Back shows a nested contents grid
  underneath that row; clicking an item there opens the move popup (Drop / To Left
  Hand / To Right Hand / To Inventory / To Storage — options only show if not
  already the source).
- [ ] Currency row (5 boxes: Copper/Iron/Silver/Gold/Platinum) shows live wallet
  balances; clicking a box opens a quantity popup (±1/±10/All + Drop) — dropping
  spawns physical coins in front of the player that scatter and don't fall through
  the ground.
- [ ] When within `storageRange` (10m) of one or more Storage Boxes, a third
  section auto-appears showing the nearest box's contents.
- [ ] **Window scale (v0.1.50-dev):** the whole Inventory window — panel, scroll
  view content, and both popups (move destination, coin drop) — renders 50%
  larger than the base layout and stays centered on screen, same technique as
  the Bank window (§11). Confirm it still fits on screen without clipping at a
  smaller window/resolution (the panel's height cap was adjusted to account for
  the scale — this is the one thing worth specifically re-checking here that
  the Bank window didn't need, since Bank's panel size was fixed, not
  screen-responsive). Buttons should still be clickable at the enlarged size.

## 4. Gathering & World Interaction

- [ ] **Sticks** (E, instant pickup) go straight to inventory/hands per the loot
  priority below.
- [ ] **Rock Node** (left-click/punch, `IPunchable`) breaks into 3 physical Small
  Rock chunks that scatter and can be picked up individually — doesn't take one
  punch; confirm it takes the expected number of hits.
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

## 5. Crafting (O)

- [ ] **O** opens/closes the Crafting screen (same open/close/Escape rules as
  Inventory).
- [ ] Lists every known recipe (not just ones you currently have materials for),
  each showing every ingredient with "have N" counts.
- [ ] Craft button greys out when short on materials or when the main inventory
  has no room for the output — label appends "— inventory full" specifically when
  that's the blocking reason (not just insufficient materials).
- [ ] Crafting draws materials from the main inventory first, then an equipped
  Backpack, then nearby Storage Boxes (within range) in distance order — confirm
  a recipe reads "have N" correctly when materials are split across all three.
- [ ] Crafted output always lands in the main inventory, never the backpack, even
  if the inputs came from there (this is intentional, not a bug).
- [ ] Spot-check at least one multi-ingredient recipe (Rock Hammer: 1 Stick + 1
  Small Rock) and one single-ingredient recipe (Rock Knife: Small Rock).

## 6. Skills (U)

- [ ] **U** opens/closes the Skills screen (same rules as Inventory/Crafting).
- [ ] Lists each skill (Gathering, Mining, Crafting, ...) with its current level
  (0–100).
- [ ] Levels rise from relevant actions (gathering Sticks, breaking the Rock Node,
  crafting) with visibly diminishing gains as level rises — a handful of early
  actions shouldn't jump a skill anywhere near 100.

## 7. Equippable Gadgets

- [ ] **Backpack** (world pickup near spawn): Equip puts it on Back and exposes its
  8-slot contents grid; Unequip falls back to main inventory → a hand → world-drop
  if everything else is full (never no-ops); Drop removes it and its contents move
  with the physical object. Worn Backpack is invisible from the player's own
  camera (no first-person clipping) but visible to an external view.
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

- [ ] Only one of Inventory/Crafting/Skills/Bank/Lockbox/rename can be open at a
  time — opening one while another is open (via its hotkey or an E/right-click
  interaction) should not stack or corrupt state.
- [ ] Escape always closes whichever screen is open and re-locks the cursor,
  regardless of which screen it is or how it was opened.
- [ ] While any screen is open (cursor unlocked), WASD/Space/mouse-look do
  nothing to the player — including while typing in the rename text box (no
  accidental jump from typing a space).
