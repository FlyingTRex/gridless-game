# Changelog

Notable changes to the Gridless project, newest first. Written for whoever (human or
Claude session) picks this repo up next — includes the *why* behind non-obvious
decisions, not just the *what*. Full detail is always in `git log`; this is the
skimmable version.

**Current version:** `0.1.40-dev` — must always match `GameVersion` in
`Assets/Scripts/FirstPersonController.cs` (shown on-screen in the bottom-left debug
panel). Bump both together in the same commit whenever gameplay code/scenes/prefabs
change; see `CLAUDE.md` for the exact rule.

## 2026-08-03

### v0.1.40-dev — Prone is its own stance, and the keybinds moved
Follow-up to the previous version: "Crawling" and "Prone" turned out to be
two different things the user wanted, not one stance under two names.
Added `MovementStance.Prone` (0.1× speed — slower than Crawl's 0.2×,
lying flat being more restrictive than moving on hands and knees) and
rebound all three: **X** = Kneel (was Left Ctrl), **C** = Crawl (was Z),
**Z** = Prone (new). Still mutually exclusive — pressing a different
stance's key switches directly to it — and Prone gets the same
sprint/jump-disabled, stamina-regenerates treatment the other two already
had, since it's just as much "not standing" as they are.

### v0.1.39-dev — Stamina-gated movement speed, plus Kneeling/Crawling stances
Reworked stamina's effect on movement into three tiers (checked in
`FirstPersonController.HandleMove`, independent of stance):
- **Stamina ≥ 85%** (`PlayerVitals.SprintStaminaThreshold`) — sprint gives
  its full speed bonus, same as before.
- **10% ≤ stamina < 85%** — sprint no longer gives any bonus; holding
  Shift just moves at normal speed. `PlayerVitals.CanSprint` now checks
  this threshold directly instead of the old hysteresis-based
  `isExhausted`/`staminaExhaustionRecoveryThreshold`, which are gone.
- **0% < stamina < 10%** — movement speed halved.
- **Stamina = 0%** — movement speed cut to 10%.

Also reworked stamina regen: it used to climb back up any time the player
wasn't sprinting, including while just walking. Per this request it now
only regenerates while stopped, kneeling, or crawling — walking normally
holds it flat. `PlayerVitals` gained a `CanRegenStamina` flag (set every
frame by `FirstPersonController`, same pattern as `IsSprinting`) instead
of inferring it from `IsSprinting` alone.

Kneeling and crawling didn't exist as player states before this — added
both as new `MovementStance` values (`Standing`/`Kneeling`/`Crawling`),
toggled with Left Ctrl (kneel) and Z (crawl), mutually exclusive, each
applying its own speed multiplier (kneel 0.4×, crawl 0.2×, both stacking
with the stamina tiers above) and disabling sprint and jump while active.
Current stance now shows in the bottom-left debug panel alongside
speed/sprinting.

### v0.1.38-dev — Starting purse: 20 Copper, 5 Silver, 1 Gold
`PlayerCurrency.Awake` now seeds the wallet via the same `Add` path a Coin
pickup uses (so it still respects `MaxBalance`, though nowhere near it)
instead of starting every character at zero across the board.

### v0.1.37-dev — Coin pickups deposit straight into the wallet, capped at 250
`PlayerCurrency.Add` now clamps each balance at a new `MaxBalance` (250)
and returns the leftover that didn't fit — same convention as
`Inventory.AddItem` — instead of adding unconditionally.

New `Coin` (`IInteractable`, not an inventory item): picking one up calls
`PlayerCurrency.Add` for its `CoinType` and destroys itself, *unless* that
type is already capped, in which case it leaves the (partial) remainder
sitting in the world rather than deleting value for nothing. Coins aren't
carried or manually dropped — there's no inventory step, matching how
picking one up is meant to work as a direct wallet deposit.

Five small round coins (a scaled-down `Cylinder` primitive, one per
`CoinType`) placed in `TestScene`, each with its own color-matched
material (`CopperCoin.mat` etc.) so they visually read as their type both
sitting in the world and while physically dropping onto the ground.
Rigidbody set to ContinuousDynamic collision detection from the start
(see [[gridless-ground-tunneling]]).

### v0.1.36-dev — Currency: Copper/Iron/Silver/Gold/Platinum row on the Inventory screen
New `PlayerCurrency` — a five-coin ledger (`CoinType`: Copper, Iron,
Silver, Gold, Platinum), each balance starting at 0, with `Add`/`Spend`
already there for whatever earns/spends coins later even though nothing
does yet.

`InventoryScreen` gained a fixed header row above the scrollable content
(so it can't scroll out of view): 5 equal-width boxes spanning 90% of the
panel's width, centered, each with its coin type's name above it as a
label. Read-only for now — just displays `PlayerCurrency`'s live balances.
Added `PlayerCurrency` to the `Player` GameObject in `TestScene`.

### v0.1.35-dev — Always-on Health/Stamina/Hunger/Thirst bar HUD
New `VitalsBarHUD`, a permanent bottom-center 2×2 grid (Health/Stamina top
row, Hunger/Thirst bottom row) — deliberately independent of
`PlayerHealthMonitor`'s detailed text panel from a few versions back,
which stays gated behind wearing a monitor; this is a baseline glanceable
readout that's always there.

Each bar's full width represents 150% of a stat's normal max (100), not
100% — `fraction = Mathf.Clamp01(value / 150f)`, so under the game's
ordinary 0-100 range every bar's top third stays visually
empty/transparent by design (reserved headroom, not a bug), only filling
past two-thirds if something ever pushes a stat above 100. Color-coded per
stat (red/gold/orange/blue) with the numeric value overlaid as centered
text. Added to the `Player` GameObject in `TestScene`.

### v0.1.34-dev — Movement locks while any screen has the cursor unlocked
User report: typing a space into the rename box's text field also jumped
the player. Root cause was broader than renaming specifically —
`FirstPersonController.HandleLook` already skipped mouse-look while
`Cursor.lockState` wasn't `Locked` (the shared signal every screen —
Inventory, Crafting, Skills, `PlayerRenaming` — sets when it opens), but
`HandleMove` had no equivalent guard, so WASD and Space always drove the
player regardless of what screen was open.

Added the same `Cursor.lockState != CursorLockMode.Locked` early-return to
`HandleMove` that `HandleLook` already had, fixing it for every screen at
once rather than special-casing the rename box. Movement (including
gravity) now fully pauses while any screen is open, matching "lock the
controls until accepted or cancelled."

### v0.1.33-dev — Fix bugs found after pulling 0.1.5-0.1.32: worn-item visibility, a Canteen-corrupting eviction bug
User report after playtesting the batch of work from the other session (Sunglasses,
Navigation Computer, Health Monitor, Canteen, storage/UI overhaul): four things
looked wrong. Root-caused all four before fixing anything, per this project's own
verify-before-fixing habit — one turned out not to be a bug at all.

- **Worn items visible when looking down/around.** The "hide from your own camera
  while worn" fix `Backpack.cs` got several sessions ago (a `WornEquipment` layer,
  toggled in `SetCarried`) never made it onto the four equippables that shipped
  since: `Canteen`, `Sunglasses`, `NavigationComputer`, `PersonalHealthMonitor`. Added
  the identical `SetLayerRecursively` treatment to all four. Added a checklist to
  `CLAUDE.md` so this stops recurring per new equippable.
- **A held Canteen turning into an inert "CanteenItem" placeholder with no Fill/
  Drink.** Real root cause, found by reading code rather than guessing:
  `PlayerLoot.ReceiveEquipment()` already refuses to evict an equipment-holding hand
  slot via the generic drop path (correctly, since that path doesn't know how to
  detach a physical `IEquippable`) — but the sibling method `Receive()`, used for
  picking up *plain* items, was missing that same guard. Picking up a plain item
  while both hands were full, one occupied by a Canteen, evicted the Canteen through
  `PlayerDropping.DropFrom`, which matches `Inventory.RemoveItem`/`AddItem` by
  `ItemDefinition` alone — stripping the `equipment` reference and leaving the real
  Canteen orphaned (still attached to the player, but referenced by no inventory slot)
  while spawning a fake, non-functional "CanteenItem" stack in its place. Added the
  missing `occupant.equipment == null` guard to `Receive()`, matching
  `ReceiveEquipment()`'s existing conservative behavior. Documented the underlying
  gotcha (`InventoryTransfer.Move`/generic `RemoveItem`+`AddItem` silently strip
  `equipment` references) in `CLAUDE.md` — this is the second equippable-corruption
  bug from the same root cause, and won't be the last new equippable added.
- **Crafted Rock Knife landing in the main inventory instead of the backpack** — not
  a bug. Explicitly documented, intentional behavior from the crafting-materials
  commit two versions prior: crafting output only ever lands in the main inventory;
  only *inputs* can be drawn from the backpack/storage. Flagged to the user as a
  possible follow-up request, not fixed.
- **"Couldn't find a way to fill the Canteen"** — turned out to be the corruption bug
  above, not a missing feature. Fill/Drink already render unconditionally the moment
  a real Canteen occupies any equipment slot (verified by reading
  `InventoryScreen.DrawEquipmentSection` directly) — what the user had was the fake
  placeholder item from the eviction bug, which naturally had no Canteen-specific
  buttons since it wasn't a Canteen anymore.

### v0.1.32-dev — Crafting sees materials in your backpack and nearby storage
User report: materials sitting in an equipped backpack showed in the
Inventory screen but the Crafting screen (and `TryCraft`) only ever
checked the main inventory, so a recipe could read "have 0" while the
backpack clearly held enough.

`PlayerCrafting` now checks (and, on craft, draws from) every reachable
`Inventory`: the main inventory first, then an equipped backpack's
contents, then any `StorageBox` within `storageRange` (10m, same default
as `InventoryScreen`) — same idea as the move popup already being able to
send items to any of those, just applied to what a recipe can consume.
`GetAvailableCount` replaces the direct `PlayerInventory.GetCount` call in
`CraftingScreen`'s "have N" label, so the displayed count now matches what
`HasIngredients` actually checks. Output still only ever lands in the main
inventory — this only changes where *inputs* can come from. When crafting
consumes an ingredient split across sources, it takes from the main
inventory first, then the backpack, then boxes in distance order.

Extracted the nearby-box lookup (previously duplicated between
`InventoryScreen` and now `PlayerCrafting`) into a shared
`StorageBox.FindNearby` static method both call.

### v0.1.31-dev — Secret Wall: a message only Sunglasses can reveal
New `SecretMessageWall` (5m × 5m × 0.5m, medium gray, blocks movement like
any solid object) — a plain wall unless you're wearing Sunglasses
(`PlayerSunglasses.Equipped != null`) *and* actually looking at it
(raycast from `PlayerInteraction`'s camera hits this specific wall's
collider), in which case it draws "Hell Yeah Brother!" in bold black text
at its screen-projected position.

Not a child of `Player` — it's a world object, not player gear — so
rather than wiring a scene reference for one Easter-egg object, it looks
up `PlayerInteraction`/`PlayerSunglasses` once via `FindFirstObjectByType`
in `Start`. Placed one at `(0, 2.5, 8)` in `TestScene`, with a new
medium-gray `Wall.mat`.

### v0.1.30-dev — Multi-ingredient recipes, and a new Rock Hammer
`CraftingRecipe` could only ever hold one input item — no way to express
"needs a Stick *and* a Small Rock" for the Rock Hammer this version adds.
Replaced `inputItem`/`inputCount` with `Ingredient[] ingredients`
(`{item, count}` pairs). `PlayerCrafting.TryCraft`/`HasIngredients` now
take the `CraftingRecipe` itself rather than looking one up by a single
input item — that lookup-by-item indirection only ever worked because
every recipe had exactly one input, and had already gone unused outside
`TryCraft` itself since Crafting moved into its own screen (`CraftingScreen`
already iterates `crafting.Recipes` directly). `FindRecipe` is gone.

Migrated all 5 existing recipes to the new format (each becomes a
single-element `ingredients` array with its old input item/count — verified
each against what was already on disk before changing anything). New
`RockHammerRecipe.asset`: 1 Stick + 1 Small Rock → 1 Rock Hammer, trains
Crafting (+2). New `RockHammerItem.asset` ("Rock Hammer", max stack 1) — a
plain, non-equippable item with no custom prefab, so dropping one falls
back to the generic `DroppedItem` prefab like any other plain resource.

`CraftingScreen` now lists every ingredient per recipe ("needs 1x Stick
(have 3), 1x Small Rock (have 5)") instead of just one, still greying out
Craft when anything's short or the inventory has no room for the output.
Widened the panel (380×300 → 460×320) for the longer multi-ingredient
labels.

### v0.1.29-dev — Rock Node pieces are now "Small Rock"
Pure data change, no code touched. `Rock.asset`'s `itemName` changed from
"Rock" to "Small Rock" — it's the same `ItemDefinition` asset `RockChunk`
(what the Rock Node scatters when broken) and `RockKnifeRecipe` already
both pointed at, so renaming it in place means the Rock Knife recipe
automatically now reads as requiring Small Rock, with no reference to
update. Checked first that nothing else in the project referenced this
asset (only those two), so an in-place rename was safe rather than
needing a new item + reference migration.

### v0.1.28-dev — Crafting screen explains why a recipe can't be made
User report: clicking Craft on a Rock Knife with 6 Rock in hand did
nothing. `PlayerCrafting.TryCraft` was already correctly failing —
`Inventory.HasSpaceFor` returns false when the main inventory (only 4
slots by default) has no room for the output — but nothing ever told the
player that's what happened, so a full inventory and a genuine bug looked
identical from the UI.

`CraftingScreen` now checks `hasEnough`/`hasSpace` per recipe before
drawing its Craft button: `GUI.enabled = hasEnough && hasSpace` greys the
button out and makes it unclickable when the recipe can't be made, and the
label appends "— inventory full" when that's specifically why (insufficient
input already shows via the existing "have N" count). Widened the panel
(340 → 380) to fit the longer label.

### v0.1.27-dev — Sticks and the Rock Node respawn 3 minutes after being taken
`Pickup` gained an opt-in `canRespawn` (default off, so every existing
usage — items the player drops, chunks scattered out of a broken
`ResourceNode` — is unaffected). When enabled, taking the item no longer
destroys the GameObject: it hides the renderer/collider instead and starts
a `respawnDelay` (180s) countdown from `Time.time`, only running once
something's actually been taken — sitting there unpicked holds the timer
indefinitely, per the request. On expiry it reappears at its original
spawn position (captured in `Awake`) plus a small random horizontal
offset (`Random.insideUnitCircle * respawnScatter`, 0.5m).

`ResourceNode` (the Rock Node) got the identical pattern directly, since
it's never used for anything but a persistent world resource point —
breaking it now hides+times out the same way instead of destroying the
GameObject, and respawning also resets `hitsTaken` so it can be broken
again from scratch.

Enabled `canRespawn` on both `Stick Pickup` and `Stick Pickup 2` in
`TestScene`; left the Berry Pickup and everything else untouched.

### v0.1.26-dev — Sunglasses: a silver screen tint while worn on the face
New `Sunglasses` (`IInteractable` + `IEquippable`) carried by
`PlayerSunglasses`, built like `PlayerBackpack` rather than the wrist
gadgets — a single destination slot (`Face`) instead of trying two. Face
has capacity 2 (room for a second accessory later), so unlike the
capacity-1 wrist/back slots, `PlayerEquipment.GetEquipped`'s "first
equipped item" isn't reliable for finding *this* instance — `Equipped`
and `FindSlot` instead scan the Face slot's own entries for a `Sunglasses`
specifically. Same pickup-priority/Unequip-fallback/Drop pattern as every
other equippable this session.

While worn, `PlayerSunglasses.OnGUI` draws a light silver, 25%-alpha
full-screen texture over everything — a pure visual filter with no
gameplay effect. Unequipping or dropping them means `Equipped` goes null
and the overlay stops drawing, the same "equip gates the HUD" pattern as
the Nav Computer's compass and the Health Monitor's vitals panel.

Craftable from 1 Rock Knife (`SunglassesRecipe.asset`, trains Crafting,
skill gain 2 — cheaper than the electronic gadgets' 3), and one is placed
in `TestScene` at `(-3.5, 0.3, 1.5)` as a world pickup. World Rigidbody
set to ContinuousDynamic collision detection from the start (see
[[gridless-ground-tunneling]]).

### v0.1.25-dev — Personal Health Monitor: wrist-worn vitals HUD
Second wrist-worn gadget alongside the Navigation Computer, built the same
way: `PersonalHealthMonitor` (`IInteractable` + `IEquippable`, no inventory
of its own) carried by `PlayerHealthMonitor`, same
pickup-priority/Equip/Unequip-with-fallback/Drop pattern as
Backpack/Canteen/NavComputer. Craftable from 1 Rock Knife
(`HealthMonitorRecipe.asset`, trains Crafting), and one is placed in
`TestScene` at `(-3, 0.3, 1)` as a world pickup. Its world Rigidbody was
set to ContinuousDynamic collision detection from the start — see
[[gridless-ground-tunneling]] — instead of repeating the bug just fixed in
the previous version.

While worn on either wrist, it draws the exact "Vitals" panel that used to
be `PlayerVitals`'s own always-on top-right `OnGUI` — Health/Hunger/
Thirst/Stamina/Body Temp — which is now gone from `PlayerVitals` entirely.
`PlayerVitals` just exposes the numbers (`Health`, `Hunger`, etc., already
public) for `PlayerHealthMonitor` to read; the game no longer always shows
vitals, only while a monitor is equipped, matching how the Nav Computer
gates its compass. `InventoryScreen` got the same three-way
Equip/Unequip/Drop wiring the other wrist item already has, in both the
main inventory list and the Equipment section.

### v0.1.24-dev — Skills (U) and Crafting (O) become their own toggleable screens
Pulled both out of where they used to live — Skills was an always-on
bottom-left panel drawn directly by `PlayerSkills.OnGUI`; Crafting was an
inline "(craft X)" button next to a matching item in `InventoryScreen`'s
main list — into dedicated screens matching `InventoryScreen`'s own
open/close convention (centered panel, Close button, cursor
lock/unlock, only opens from normal gameplay so it can't stack on another
open screen).

New `SkillsScreen` (U) reads `PlayerSkills.Levels` (newly exposed —
`PlayerSkills` no longer draws anything itself) and lists each skill's
level. New `CraftingScreen` (O) reads `PlayerCrafting.Recipes` (also newly
exposed) and lists *every* known recipe with how many of its input you
currently have, rather than only showing a craft option for items already
sitting in your inventory. `InventoryScreen` no longer has any
crafting-related code — that recipe-lookup button is gone from its main
list.

`FirstPersonController`'s Escape handling now closes both new screens
alongside Inventory and the rename prompt, so they can't be left open with
a locked cursor. Added `SkillsScreen`/`CraftingScreen` to the `Player`
GameObject in `TestScene`.

### v0.1.23-dev — Fix dropped Backpack/Canteen/Navigation Computer falling through the floor
User report: dropping a Canteen or Navigation Computer made it appear
briefly then vanish. Root cause: `Backpack.prefab`, `Canteen.prefab`, and
the Navigation Computer's world Rigidbody all had `m_CollisionDetection: 0`
(Discrete) — every other droppable prefab (`DroppedItem`, `BerryPickup`,
`StickPickup`, `RockKnifePickup`, `RockChunk`) already used `2`
(ContinuousDynamic). `Ground` is a Plane mesh scaled to (10, 1, 10) — a
paper-thin `MeshCollider` — so a Discrete-mode Rigidbody falling even the
~1m `dropHeight` drop distance could tunnel straight through it with no
tolerance for the gap Discrete detection leaves, meaning it just kept
falling, off into the void.

Switched all three to ContinuousDynamic: `Backpack.prefab`,
`Canteen.prefab`, and the world-placed instances of Backpack, Canteen, and
Navigation Computer already sitting in `TestScene` (editing the prefabs
alone doesn't retroactively fix instances that aren't live prefab
connections — these three were baked copies, so each needed the same
scalar field fixed directly).

### v0.1.22-dev — Move popup can send an item straight to the backpack
User feedback: moving an item out of a nearby storage box's contents (or
a hand) always landed in the main inventory or a hand — there was no way
to send it straight into an equipped backpack, unlike the main inventory
list's row buttons, which already had "To Pack" alongside "To Storage".

`DrawMoveDestinations` (the popup opened by clicking an item in a
container's contents grid, a hand, or the equipment section) gained a
"To Backpack" option, shown whenever a backpack is worn and isn't already
the source — same guard pattern as the existing hand/storage options.
Bumped the popup's fixed height (240 → 270) to fit the extra button.

Also removed the temporary `Debug.Log` calls added earlier this session
while diagnosing what turned out to be a stale `Library` cache, not an
actual code bug, on the "To Storage" picker (see previous entry).

### v0.1.21-dev — Hover a storage container to see its name
New `StorageBoxHover`, attached to `Player` alongside `PlayerInteraction`.
Raycasts from the same camera every `Update` (its own `hoverRange`, 20m —
deliberately longer than interact range, since reading a label shouldn't
require being close enough to use the box) and, when the ray hits a
`StorageBox`, draws its `DisplayName` above the crosshair in `OnGUI`.
Reads `DisplayName` directly, so a renamed box's name shows immediately.

Positioned above the crosshair rather than below, where
`PlayerInteraction`'s own interact-prompt text draws — the two never
compete for the same spot since `StorageBox` isn't `IInteractable`.

### v0.1.20-dev — "To Storage" now lists nearby boxes by name
User feedback: `InventoryScreen` only ever offered the single *nearest*
StorageBox as a move destination, silently ignoring any others in range.
`storageRange` (10m) can easily contain more than one box, so there was no
way to choose which.

`nearbyStorage` (single) became `nearbyStorages` (`List<StorageBox>`,
nearest first — `FindNearbyStorageBoxes` now populates it instead of
returning one). Clicking "To Storage" — from either the main inventory
list or the move popup — no longer moves immediately; it switches the
popup into a picker mode (`choosingStorage`) listing every nearby box by
`DisplayName` (so a rename shows up here too), with **Back** to return to
the normal destination list and **Cancel** as before. The auto-expanding
"(nearby)" contents section still shows just the nearest box, unchanged.

### v0.1.19-dev — Navigation Computer: wrist-worn compass + speed HUD
New equippable gadget, `NavigationComputer` (`IInteractable` + `IEquippable`,
no inventory of its own — just a wearable), carried by new `PlayerNavComputer`
(`RequireComponent`s `PlayerInventory`/`PlayerEquipment`/`CharacterController`).
Pickup follows the same priority as Backpack/Canteen (equipped backpack, then
a free hand, then stashed in the main inventory), and `Equip` tries Left
Wrist then Right Wrist. `Unequip` uses the same fallback chain added for
`PlayerBackpack` a few versions back — main inventory, then a hand, then
drop — instead of risking the old no-op-when-full bug.

While a computer is worn on either wrist, `PlayerNavComputer.OnGUI` draws a
scrolling compass strip across the top-center of the screen (cardinal
labels positioned by their angular offset from `transform.eulerAngles.y`,
so they slide past as the player turns) with current horizontal speed
(from `CharacterController.velocity`, y-component zeroed) shown underneath.
Unequipping just stops drawing it — `Equipped` going null is the only
condition `OnGUI` checks.

`InventoryScreen` got the same three-way Equip/Unequip/Drop wiring
Backpack/Canteen already have, in both the main inventory list and the
Equipment section (worn = shown on Left/Right Wrist specifically, unlike
Canteen where any of its slots count as worn).

Craftable from 1 Rock Knife (`NavComputerRecipe.asset`, trains Crafting),
and one is placed in `TestScene` at `(-1.5, 0.3, 0.5)` as a world pickup so
it's usable without crafting first.

### v0.1.18-dev — Right-click a world object to rename it
New `IRenameable` (`DisplayName`, `Rename(string)`) and `PlayerRenaming`,
which right-click-raycasts using the same camera/range as
`PlayerInteraction`'s E-prompt. Hitting an `IRenameable` opens a small
text-entry window (Enter or Save to commit, Cancel or Escape to discard),
unlocking the cursor the same way `InventoryScreen` does. `StorageBox` is
the first (and so far only) `IRenameable` — since `InventoryScreen`
already reads a nearby box's name through `DisplayName`, a rename shows up
there automatically with no further changes needed.

Wired `PlayerRenaming.Close()` into `FirstPersonController`'s Escape
handling alongside `InventoryScreen.Close()`, and gated `InventoryScreen`'s
I-key toggle to only *open* while the cursor is locked — otherwise
pressing I while the rename window was open would stack the inventory
screen on top of it. Added the `PlayerRenaming` component to the `Player`
GameObject in `TestScene`.

### v0.1.17-dev — Small Storage Box spawned 20m from player start
No code changes — `StorageBox`'s capacity was already a `[SerializeField]`,
so this is purely a scene addition. Added a second, smaller box ("Small
Storage Box", 10 slots vs. the original's 20) to `TestScene` at
`(0, 0.2, -20)`, 20 meters from the player's spawn point `(0, 1.05, 0)`
and clear of every other placed object (all of which sit within ~3.4m of
spawn). Reuses the existing `Assets/Data/StorageBox.mat`, just scaled down
(0.45 x 0.35 x 0.35) to read as the smaller of the two at a glance.

### v0.1.16-dev — Storage boxes: auto-expand the inventory screen near a nearby box
New `StorageBox` — a stationary world container (not `IInteractable`, no
pickup/use prompt). Every enabled box registers itself in a static
`StorageBox.Active` list; `InventoryScreen` checks that list once per
`OnGUI` frame and finds the nearest box within `storageRange` (10m by
default). When one's in range, opening the I screen adds a third section
below Inventory/Equipment showing that box's contents as a clickable grid,
reusing the existing "where should this go?" move popup (now with a "To
Storage" destination alongside Drop/hands/inventory) so items can move
either direction. Plain inventory items also get a "To Storage" button
next to "To Pack", mirroring how backpack transfers already worked.

`DrawContainerContents` was generalized from taking an `IInventoryHolder`
to a plain `(Inventory, caption)` pair, since a `StorageBox` has no
Stash/SetCarried/equip-slot concept to justify that interface — it's just
another `Inventory` to render the same way a worn backpack's contents
already were.

Added one Storage Box to `TestScene` at `(3, 0.25, 0)`, clear of the
existing Backpack/Canteen/resource spawns, with a new
`Assets/Data/StorageBox.mat` (brown) so it reads as a container at a
glance.

### v0.1.15-dev — Unequip falls back to a hand/drop instead of no-op'ing, canteen spawns at start
User feedback: unequipping a worn backpack when the main inventory is full
did nothing — `PlayerBackpack.Unequip` only ever attempted
`playerInventory.Inventory.AddEquipmentItem` and returned `false` with no
other recourse. It now mirrors the fallback chain `PickUp`/`ReceiveEquipment`
already used: main inventory first, then Left Hand, then Right Hand, and
if all of those are full, drops the backpack into the world in front of the
player rather than leaving it stuck on the back.

Also added a Canteen to `TestScene` at `(-1, 0.3, 1.5)`, spawned alongside
the existing world-start Backpack so there's a liquid container to pick up
without needing to craft one first.

### v0.1.14-dev — Plain items in a hand use the same move popup as backpack contents
Follow-up to the previous version's popup, closing the scope gap flagged
there: clicking a plain item sitting directly in an equip slot (e.g.
something picked up into a hand) now sets `pendingMoveItem`/
`pendingMoveSource` and opens `DrawPendingMovePopup`, same as clicking an
item inside a backpack's contents grid — instead of moving straight to the
main inventory with no other choice. The two click sites now share one
popup and one set of destination rules instead of each hardcoding its own
single target.

### v0.1.13-dev — Popup for where a backpack item should go, instead of a hardcoded move
User feedback: clicking an item inside the backpack's contents grid always
moved it straight to the main inventory with no other option — should
offer Drop or move-to-hand instead, ideally as a menu of choices.

`DrawContainerContents` no longer moves anything itself — clicking an
occupied box now just records `pendingMoveItem`/`pendingMoveSource` and a
small popup (`DrawPendingMovePopup`) opens with the real set of
destinations: **Drop**, **To Left Hand**, **To Right Hand**, **To
Inventory**, **Cancel** — each hand/inventory option only shown if it
isn't already the source. Drawn last in `OnGUI`, after `GUILayout.EndArea()`
of the main panel, so it renders on top regardless of scroll position.
Cleared whenever the screen closes (`SetOpen(false)`), so a stale popup
can't reappear the next time it's opened.

Scope note: this only changes the backpack-*contents* click (the thing
actually reported). The separate "click a plain item sitting directly in a
hand" case (added two versions ago) still moves straight to inventory —
left alone since it wasn't part of what was asked, though it'd be a
straightforward follow-up to route through the same popup if wanted.

### v0.1.12-dev — A held (not worn) backpack isn't usable storage yet
User feedback on the previous version's routing change: a backpack picked
up into a hand showed "Unequip" (as if already worn) and exposed its
contents grid, when thematically holding a backpack in your hand isn't the
same as wearing it — you shouldn't be able to use it as storage, or
"unequip" something that was never equipped.

`InventoryScreen` now branches on which slot a backpack is actually in: on
`Back`, unchanged (Unequip + contents grid). Anywhere else (a hand), shows
**Equip** instead of Unequip, and the contents grid doesn't render at all —
`nestedHolder` is only set when `slotName == "Back"`.

Fixing this exposed a real duplicate-occupancy bug in `PlayerBackpack.Equip`:
it unconditionally removed the backpack from the *main inventory* before
placing it on `Back`, regardless of where it actually was. If it was
sitting in a hand instead (the new common case after last version's
routing change), that removal call found nothing there and silently did
nothing — the backpack would end up occupying *both* the hand slot and
`Back` simultaneously. `Equip` now calls the same `FindSlot()` used by
`Unequip`/`Drop` to locate it first, then removes it from wherever that
actually is.

### v0.1.11-dev — Backpack/Canteen pickup routes through PlayerLoot too; 20-cap
User-reported gap: picking up a Backpack (or Canteen) from the world always
stashed it straight into the main inventory — `Backpack.Complete`/
`Canteen.Complete` never went through the `PlayerLoot` hand/backpack
priority added last version at all, only `Pickup.Complete` did. Sticks
correctly went to a hand; the backpack itself didn't.

- `PlayerLoot` gained `ReceiveEquipment(item, IEquippable)`, same priority
  as `Receive()` but using `AddEquipmentItem`/`RemoveEquipmentItem` since
  Backpack/Canteen aren't stackable counts. Deliberately does *not* evict
  another equipment item from a hand to make room (only plain items) —
  swapping someone's held Canteen out for a picked-up Backpack felt like a
  rarer case not worth the added complexity.
- This exposed a real gap in `IEquippable`: it only had `DisplayName`, so
  there was no way to generically tell a newly-routed item to become
  visible (carried, e.g. landed in a hand) vs. stay hidden (stashed, e.g.
  packed inside a container). Promoted `Stash()`/`SetCarried(bool,
  Transform)` onto the interface — `Backpack` and `Canteen` needed zero
  code changes, since both already implemented matching methods.
- That promotion broke compilation: `PlayerInventory` also declared
  `IInventoryHolder` (which extends `IEquippable`), so it was suddenly on
  the hook for `Stash`/`SetCarried` too, despite never being a physical
  object. Checked whether anything actually used `PlayerInventory` as an
  `IInventoryHolder`/`IEquippable` polymorphically — nothing did, anywhere
  — so removed that conformance (and the `DisplayName` property that only
  existed to satisfy it) rather than bolting on meaningless no-op methods.
- `PlayerBackpack.Unequip`/`Drop` had the same latent bug already fixed for
  the routing itself: both assumed a backpack was either in `Back` or the
  main inventory, so a backpack that ended up in a hand couldn't actually
  be removed from it — clicking Drop would detach the physical object
  while leaving a "ghost" entry stuck occupying the hand slot. Added
  `FindSlot()` (Back, then both hands) so both methods find it wherever it
  actually is. `PlayerCanteen` already searched all its valid slots this
  way, so it wasn't affected.

Also, per a second request in the same message: `Inventory` now enforces a
hard `MaxStackCap = 20` centrally (`Mathf.Min(item.maxStack, MaxStackCap)`
wherever `maxStack` was used), rather than trusting each `ItemDefinition`'s
own value — applies to every `Inventory` (main, backpack, any equip slot)
from one place. A no-op today (Rock/Stick are already 20), but a real
ceiling against a future item being configured with an unintended stack size.

### v0.1.10-dev — Pickups route to Backpack, then hands, evicting if needed
User-requested mechanics change: picked-up items no longer go straight to
the main 4-slot inventory. New priority order, implemented in a new
`PlayerLoot` component:
1. **Backpack equipped** → item goes straight into its `Inventory`
   (`AddItem`, normal stacking/capacity rules — if the backpack is full the
   remainder stays on the ground, same as the existing full-inventory
   behavior).
2. **No backpack** → tries Left Hand, then Right Hand (`Inventory.AddItem`
   on each slot — stacks into a hand already holding the same item before
   trying an empty one).
3. **Both hands occupied by something that won't stack** → evicts whatever
   is in Left Hand (physically dropped into the world, not deleted), then
   places the new item there. Picking something up now never simply fails
   when there's no backpack — worst case it swaps out what's in your hand.

`PlayerDropping` gained a `DropFrom(Inventory, item)` alongside the existing
`Drop(item)`, so eviction reuses the exact same "spawn a physical pickup in
the world" path as the manual Drop button instead of duplicating it —
`Drop(item)` is now a one-line call to `DropFrom(playerInventory.Inventory,
item)`.

`Pickup.Complete` now calls `PlayerLoot.Receive` instead of
`PlayerInventory.AddItem` directly (falls back to the old direct-to-
inventory behavior if `PlayerLoot` is somehow missing).

**Necessary follow-on:** hands can now hold plain stackable items, not just
equippables like Canteen — but `InventoryScreen`'s equipment boxes were only
ever interactive for backpack/canteen contents. A plain item picked into a
hand would've been visible but permanently stuck with no UI path back out.
Made plain-item boxes in any equip slot clickable-to-move-to-inventory too,
same pattern as backpack contents.

### v0.1.9-dev — Consolidate all inventory UI into the I screen
User request: the always-on Inventory box and Back-slot (Backpack) panel
should be gone from the normal HUD entirely, with inventory only visible via
I. Rather than just hiding those panels behind an `IsOpen` check (already in
place from the previous overlap fix), folded their actual content into
`InventoryScreen` and deleted the three source `OnGUI` methods outright —
one screen, one place the logic lives, instead of three panels coordinating
visibility with a fourth.

- `PlayerInventory.OnGUI` (item list, craft/eat/drop/equip/to-pack buttons)
  → `InventoryScreen.DrawInventorySection`.
- `PlayerBackpack.OnGUI` (Unequip/Drop Backpack, per-item "To Inventory")
  → folded into `InventoryScreen.DrawEquipmentSection`'s Back row: Unequip/
  Drop buttons appear next to the slot, and each nested content box is now
  itself a button — click an item to move it back to the main inventory,
  replacing the old separate "To Inventory" button per row.
- `PlayerCanteen.OnGUI` (Drink/Fill/Unequip/Drop) → same treatment, appended
  to whichever slot (Left Hand/Right Hand/Waist) the canteen currently
  occupies.

`PlayerInventory`/`PlayerBackpack`/`PlayerCanteen` lost their now-dead
`crafting`/`dropping`/`eating`/`vitals`/`inventoryScreen` cross-references
along with the removed `OnGUI`s — they're back to pure state/logic holders,
UI-agnostic.

Stacking the full inventory list + all 14 equipment rows + nested container
contents in one fixed-height panel would have badly overflowed most window
heights (a rough estimate came out near 900px). Switched to a
`GUILayout.BeginScrollView` inside a screen-clamped panel
(`Mathf.Min(Screen.height - 40, 700)`) instead of hand-computing exact
content height — robust regardless of how many slots end up occupied.

### v0.1.8-dev — Inventory screen: show container contents, fix panel overlap
User-reported bug, two real causes:
- `InventoryScreen`'s per-slot boxes only ever reflected the *slot's* own
  capacity (Back = 1 box), so a box just displayed "Rough Backpack" and
  never looked inside it — adding Sticks to the backpack via "To Pack"
  changed nothing on screen. Fixed by detecting when an equipped item is
  itself a container (`is IInventoryHolder`) and drawing a nested row of
  *that* container's own capacity/contents underneath the slot row, wrapped
  at 6 per line. Panel height is now computed per-frame from whatever's
  actually equipped, rather than a fixed constant, so it doesn't reserve
  wasted space when nothing equipped is a container.
- A screenshot from testing showed `PlayerBackpack`'s own always-on panel
  (`Unequip`/`Drop Backpack`/`To Inventory`) rendered directly on top of the
  Equipment screen — both draw in overlapping screen regions. `PlayerBackpack`
  and `PlayerCanteen` now skip their own `OnGUI` entirely while
  `InventoryScreen.IsOpen` is true, since the Equipment screen is meant to be
  the single source of truth when it's up. Trade-off: Unequip/Drop for those
  two aren't reachable while the Equipment screen is open — close it (I or
  Escape) to use them, consistent with the screen being read-only for now.

### v0.1.7-dev — Sync Escape and I so the cursor/inventory-screen state can't drift
`InventoryScreen` (I) and `FirstPersonController`'s Escape toggle each
managed `Cursor.lockState` independently, with no knowledge of each other.
Opening the inventory with I then pressing Escape would re-lock the cursor
via `FirstPersonController` while `InventoryScreen.isOpen` stayed `true` —
the panel kept rendering, mouse-look resumed under it, and a second I press
would then close it instead of reopening it. Caught by the user asking
"do we have a way to close the inventory screen" and pointing out the two
controls could disagree.

Fix: `InventoryScreen` exposes a public `Close()`; `FirstPersonController`
calls it whenever Escape transitions the cursor *into* the locked state
(`!wasLocked`) — "cursor just got re-locked" now always implies "any open
screen is closed" as an invariant, regardless of which control the player
used or which order their presses happened in. Deliberately not building a
general cursor-state stack/owner system for this — two toggles was simple
enough to reconcile directly; revisit if a third one shows up.
### v0.1.6-dev — Inventory management screen (I)
`InventoryScreen`, toggled with I, lists all 14 `PlayerEquipment` slots in
one place (previously only visible piecemeal — Backpack/Canteen each drew
their own panel only while equipped, and there was no view at all for the
other 12 slots since nothing equips into them yet). Each row is a slot name
plus one box per unit of that slot's `Inventory` capacity (so `Face` draws
two boxes, everything else one), showing the occupying item's name if
filled or "Empty" if not — reads `Inventory.Slots`/`Capacity` directly, so
it stays correct automatically as items get added/removed elsewhere.

Read-only for now: no equip actions live here, since nothing yet targets the
12 slots beyond Back/Hand/Waist. Opening it unlocks and shows the cursor
directly (mirrors what Escape already does in `FirstPersonController`,
kept intentionally simple rather than building a shared cursor-state
stack for two independent toggles).

Existing debug panels (Inventory, Backpack, Canteen, Vitals, Skills) are
unchanged and still always-on — this is an additional full-picture view,
not a replacement.

### v0.1.5-dev — Full body-equipment slot layout
`PlayerEquipment` reworked from "one named slot holds one `IEquippable`" to
"each named slot is its own small `Inventory`" (capacity usually 1, `Face` is
2), since some requested slots needed to hold more than one item — the same
`AddEquipmentItem`/`RemoveEquipmentItem` flow already used for the main
inventory and for Backpack/Canteen's own internal storage, just applied one
level up. Full slot list: `Head`, `Face` (×2), `Neck`, `Chest`, `Back`,
`Left Arm`, `Right Arm`, `Left Wrist`, `Right Wrist`, `Left Hand`,
`Right Hand`, `Waist`, `Leg`, `Feet`. `Back` was already named `Back`, not
`Backpack` — no rename needed there.

`PlayerBackpack`/`PlayerCanteen` updated to equip through
`equipment.GetSlot(name).AddEquipmentItem(...)` instead of the old
single-slot `Equip`/`Unequip`/`CanEquip` API, which no longer exists.
`PlayerCanteen` also simplified from two explicit destination buttons
(To Hand / To Belt) to one `Equip` button that tries `Left Hand` → `Right
Hand` → `Waist` in order — matches how `Backpack`'s row already works, and
avoids the button row growing by one for every additional slot a future
equippable might be able to target.

No scene changes needed: `PlayerEquipment.slotNames` and
`PlayerCanteen`'s old `handSlotAnchor`/`beltSlotAnchor` fields were renamed/
restructured, and `TestScene.unity` still has the old serialized values for
them — Unity just ignores orphaned fields on load and falls back to the new
fields' C# defaults, which happen to already be what's wanted (the full slot
list; unassigned anchors falling back to the player transform). Validated
with a full batch-mode compile check rather than assuming that fallback
holds.

### v0.1.4-dev — Merge: reconcile Waterskin with Canteen (keep Canteen)
Both sessions independently landed on the exact string `"0.1.3-dev"` for
`GameVersion` despite representing different code — a version-number collision
git's text diff can't catch, since identical text isn't a conflict. Bumped to a
genuinely new number for this merge.

Bigger reconciliation than a technical merge: this session's Empty/Filled
Waterskin (found container, filled at the Water Puddle, drunk via `EdibleItem`)
and the other session's Canteen below solve the same problem — carrying and
drinking water — built in parallel with no coordination. Not something to
mechanically merge; the game would end up with two redundant, unrelated ways to
carry water. Kept Canteen (craftable, equippable to Hand/Belt, fits the game's
first-person/embodied-crafting pillar better than a passively-found container)
and removed Waterskin entirely — `WaterSource.cs`, `EmptyWaterskin`/
`FilledWaterskin`/`WaterskinDrink` assets, their pickup prefabs/materials, and
the `WaterSource` component on the Water Puddle (now just a decorative prop;
Canteen's `Fill` isn't tied to a specific world location). Berry's `EdibleItem`/
`PlayerEating` system is unaffected and still ships — it doesn't overlap with
Canteen at all, and Canteen deliberately doesn't use it (holds liquid state
directly rather than wrapping an `Inventory`).

### v0.1.3-dev — Berry eat/drink system, per-item drop visuals, physics fixes
Berry went from an instant-eat-on-touch world object to a real inventory item:
`Pickup` it like anything else, carry it, move it to the backpack, and `EdibleItem`
(new ScriptableObject, mirrors the existing `CraftingRecipe` pattern) drives an
"Eat"/"Drink" button that only appears in the personal-inventory panel — never in
the backpack panel, so a stored berry can't be eaten without taking it out first.
The `verb` field ("Eat" vs "Drink") is data-driven per `EdibleItem` rather than
hardcoded, so future consumables (soup, potions, whatever) don't need a code change.

**New general mechanism:** `ItemDefinition.worldPickupPrefab` — what a dropped item
looks like now depends on the item, not a single generic gray-cube fallback shared
by everything. Built one for Berry, Stick, Rock (reusing the existing
`RockChunk.prefab` instead of duplicating it) and Rock Knife; the backpack already
had its own dedicated drop visual and didn't need one. (Also built one for the
Empty/Filled Waterskin at the time — removed along with the rest of that system in
the merge above.)

**Real bugs hit building this, in order:**
- A `SerializedObject.objectReferenceValue` assignment silently produced a null
  reference (`fileID: 0`) for several fields despite no error and an identical
  pattern elsewhere in the same script succeeding. Root cause: assets created via
  `AssetDatabase.CreateAsset` earlier in the script, then referenced *after* an
  `EditorSceneManager.OpenScene()` call later in the same script, without an
  intervening `AssetDatabase.SaveAssets()` — the scene-open silently invalidated
  the uncommitted in-memory asset references. Fixed by re-fetching via
  `AssetDatabase.LoadAssetAtPath` *after* the scene is already open, rather than
  trusting pre-open references to survive. General rule worth remembering: never
  let object references cross an `OpenScene` call within the same batch-mode
  script — save assets first, or re-fetch after.
- Repeated the exact material-into-prefab mistake this project's own `CLAUDE.md`
  already documents: used `new Material(Shader.Find(...))` directly on new drop
  prefabs instead of saving it as a real `.mat` asset first. All five new drop
  prefabs rendered pink until fixed. Worth noting because it's a *documented*
  gotcha that still got missed under time pressure — a reminder to actually check
  `CLAUDE.md` conventions before repeating a pattern, not just after something
  breaks.
- The two thinnest new drop prefabs (Rock Knife at 0.05 units tall, Stick at 0.1)
  fell straight through the Ground collider — classic tunneling: Unity's default
  Discrete collision detection can miss a collision entirely if a thin, fast-moving
  collider passes a thin static collider between physics steps. Berry (a chunky
  sphere) was thick enough to never hit this. Fixed by setting
  `Rigidbody.collisionDetectionMode` to `ContinuousDynamic` on every
  Rigidbody-bearing pickup/dropped-item prefab, not just the two that visibly broke.

### Merge: canteen + panel-layout/versioning reconciliation
Built in parallel with the `v0.1.2-dev` work below on a separate Claude Code
session, discovered on push — same recurring situation as the two merge
entries further down, but a cleaner one this time: no fileID collision, just
a text conflict in this file's own version line/entries. Two real things to
reconcile though, not just text:
- The other session's Backpack debug panel moved to `Rect(320, 10, 280, 320)`
  as part of its own panel-overlap cleanup — which put its right edge at
  `x=600`, ten pixels inside where this session's new canteen Hand/Belt panels
  had been placed (`x=590`). Moved the canteen panels to `x=610` and gave them
  the same `DebugGUI.DrawPanel`/`Header`/`Label` treatment the other panels
  now use, instead of plain unstyled `GUILayout`.
- First time this session's Claude instance saw the new
  `CLAUDE.md`/`CHANGELOG.md` version-bump convention introduced by the other
  session (`GameVersion` + this file's "Current version" line, bumped
  together on every gameplay-affecting commit). The canteen commit predated
  discovering that rule, so this merge is also where it first gets applied
  here — bumped `0.1.2-dev` → `0.1.3-dev`.

### Canteen: craftable liquid container, first `IEquippable` beyond Backpack (`8670677`)
Craftable from 3 Sticks (trains Crafting), cylinder-shaped (body + cap
primitives, steel-grey `Canteen.mat`), can sit in the regular inventory or be
equipped to two new slots — Hand or Belt (`PlayerEquipment.slotNames` grew
from just `Back`). Holds liquid, not items: `Canteen` tracks a
`LiquidType?`/`Amount`/`Capacity` triplet directly rather than wrapping an
`Inventory`, with `Fill`/`Drink` (the latter restores `PlayerVitals` Thirst).

**Refactor forced by this:** `Inventory.Slot.equipment` and
`AddEquipmentItem`/`RemoveEquipmentItem` were typed to `IInventoryHolder`,
which assumes the equipped thing wraps an `Inventory` — true for `Backpack`,
false for `Canteen`. Pulled the common bit (`DisplayName`) out into a new
`IEquippable` base interface; `IInventoryHolder : IEquippable` adds
`Inventory` on top for container-type equippables. `PlayerEquipment` now
stores `IEquippable`, not `IInventoryHolder` — `Backpack` needed no code
changes, since it still satisfies the wider interface through the narrower
one.

Built via the batch-mode Editor-script workflow throughout (prefab
composition + wiring `PlayerCanteen`/the new recipe into `TestScene` via
`SerializedObject`, not hand-authored YAML) — validated with a full batch-mode
compile check and a duplicate-fileID scan before committing.

### v0.1.2-dev — Merge: backpack silhouette + cursor-lock/panel/worn-equipment fixes
Built in parallel with the silhouette rebuild below on a separate Claude Code
session, discovered on push (same situation as the vitals merge further down).
Real fileID collision again: this session's edit to `Backpack.prefab` (via
`PrefabUtility.LoadPrefabContents` → `SaveAsPrefabAsset`, round-tripping the same
asset) silently reassigned the root GameObject's fileID instead of preserving it —
a new gotcha distinct from the hand-authored-YAML case in the vitals merge. That
reassigned fileID then collided with a `StrapLeft` object the other session
independently created while rebuilding the same prefab into a multi-part
hierarchy. Resolved by taking the other session's full prefab/scene structure as
the base (correct fileID continuity with shared history) and re-applying this
session's changes on top, rather than trying to reconcile two structurally
different versions of the same file by hand.

Also corrected a design mistake caught during the merge: this session's first pass
set `m_Layer` to a new `WornEquipment` layer (excluded from the player's own
`Camera.cullingMask`) directly on the `Backpack` prefab asset. That's wrong — it
would make the backpack invisible even while just sitting in the world, since
nothing ever reset the layer back. Moved the logic into `Backpack.SetCarried()`
instead, toggling the whole hierarchy's layer at runtime (`WornEquipment` while
worn, `Default` on drop/unequip) — the prefab itself stays on `Default`.

Otherwise unchanged from this session's original fixes: clicking on-screen debug
buttons (Equip/craft/Drop) was unusable because any left-click while the cursor was
unlocked immediately re-locked and hid it before the click could register — Escape
now toggles the lock both directions instead of any-click relocking. Debug panels
(Inventory/Skills/Vitals/speed+version) got a shared `DebugGUI` background for
readability, which exposed a real pre-existing overlap between the Inventory,
Skills, and Backpack panel `Rect`s — repositioned to clear each other's edges.
(Also chased and ruled out a *third* apparent bug — Berry Bush, Water Puddle, and
two stick pickups looking like they were floating/overlapping — that was just a
flat featureless plane with no depth cues; verified exact Transform values before
touching anything rather than guessing fixes for things that weren't broken.)

### Backpack silhouette instead of a box (`69a79b8`)
Rebuilt `Backpack.prefab` and its `TestScene` instance as a body + tilted flap
+ two side straps + front pocket (all primitives, same `Backpack.mat`), instead
of one flattened cube. Built via the batch-mode Editor-script workflow — a
throwaway `Assets/Editor` script that composed the hierarchy with real Unity
APIs (`GameObject.CreatePrimitive`, `PrefabUtility.SaveAsPrefabAsset`,
`EditorSceneManager`) and was deleted after — rather than hand-authoring the
multi-child YAML directly. Composing a parent/several-children hierarchy by
hand is exactly the kind of edit that produces silent fileID mistakes (see the
merge entry above); letting Unity allocate the fileIDs itself sidesteps that
class of bug entirely.

### Merge with survival vitals (`91240b3`)
Built in parallel with the vitals work below on a separate Claude Code session,
discovered only on push. Real gotcha, not just a text conflict: both branches
independently added new Player components starting at the same scene fileID
(`1681626235`) — this session's `PlayerCrafting` vs. the other session's
`PlayerVitals`. Git's line-based merge didn't flag it, since the line itself
(`- component: {fileID: 1681626235}`) was identical on both sides — only the
*object it points to* differed. Caught by diffing the full fileID list of both
branches' `TestScene.unity` rather than trusting a clean `git merge` exit.
Resolution: kept `PlayerVitals` at `1681626235`, renumbered
`PlayerCrafting`/`PlayerDropping`/`PlayerBackpack`/`PlayerEquipment` to
`1681626239`–`242`. Also updated `Pickup.cs` and `Backpack.cs` to the
`IInteractable`/`IPunchable` → `GameObject` signature change introduced by the
vitals branch (see below) — `Backpack.cs` wasn't even flagged as conflicted by
git, since the other branch never touched it, so it would have silently failed
to compile if not caught by hand. Validated the merge with a Unity batch-mode
compile check rather than trusting the text merge alone — worth doing for any
future merge that touches `.unity`/`.prefab` files by hand, since those can
"merge cleanly" by git's rules while still being semantically broken.

### Crafting, dropping, and a backpack equipment system (`abb8a3a`)
Click-to-craft (Rock → Rock Knife, training a new Crafting skill), click-to-drop
on any inventory stack, and a carryable/wearable backpack. Extracted a reusable
`Inventory` class (capacity, slots, `HasSpaceFor`) out of `PlayerInventory`,
which is now capped at 4 slots; the backpack is a separate 8-slot container.
`InventoryTransfer.Move()` moves items between any two inventory-capable
objects (`IInventoryHolder`). `PlayerEquipment` adds named equip slots
(starting with "Back") — picking up the backpack stashes it as a regular
inventory item, an Equip button moves it onto the Back slot (visible, worn,
contents accessible), Unequip/Drop reverse that without ever losing contents.

**Consequence of the new slot cap:** `Pickup.Complete` and
`PlayerCrafting.TryCraft` both had to start checking for space *before*
consuming anything — otherwise a full inventory would silently delete a
picked-up item, or eat a crafting input without producing the output.

### Survival vitals: Health, Hunger, Thirst, Stamina, Body Temperature (`ba34403`)
`PlayerVitals` ticks Hunger/Thirst down over real time, drains Health on starvation/
dehydration and regens it when well-fed, and gates sprint on Stamina. Two consumables
(Berry Bush → Hunger, Water Puddle → Thirst, reusable) make the loop testable without
a full item-use/hotbar system.

**Refactor:** `IInteractable.Complete` / `IPunchable.OnPunch` now take the player's
`GameObject` instead of individual component references (inventory, skills, vitals).
The parameter list was about to keep growing with every new player subsystem — third
one (vitals) was the trigger to stop and pass the GameObject instead, letting each
interactable pull what it needs via `GetComponent`.

**Playtesting fixes:**
- Stamina drain/regen initially caused a same-frame flicker between sprinting/not at
  exactly 0 — regen resumed for a single frame, immediately re-enabling sprint, which
  drained it right back to 0, repeating every frame. Fixed with a proper exhaustion/
  recovery hysteresis: once exhausted, sprint stays locked out until stamina climbs
  back to 25, not just `> 0`. Worth remembering as a general pattern for any future
  binary gate driven by a continuously-draining/regenerating value.
- Jumping now costs stamina too (flat cost per jump, not per-second like sprint).
- Berry Bush's color turned out to be a genuinely bad pink/magenta choice
  (`0.55, 0.05, 0.35`), not a rendering bug — spent a round wrongly chasing it as a
  "one-off shader compile glitch" before actually computing what that RGB reads as.
  Changed to a proper deep red (`0.35, 0.05, 0.08`).

### Auto-open default scene when the Editor has none loaded (`600e631`)
Fixes a real onboarding bug: Unity's "last opened scene" state lives in the
gitignored, machine-local `Library/` folder, so a fresh clone opens to a blank
`Untitled` scene — looks like an empty grey world with nothing to move, even though
everything is actually there. `Assets/Editor/SceneAutoOpen.cs` runs on Editor load
and opens `EditorBuildSettings`' first scene whenever none is currently loaded.
Registering `TestScene` in `EditorBuildSettings.asset` (see next entry) was a
prerequisite for this — that setting only affects Player *builds* by itself, not
Editor auto-open behavior.

### Skill-via-use progression + register TestScene in Build Settings (`393bd76`)
`PlayerSkills` tracks per-skill level (0–100) with diminishing gains as level rises
(SCUM-style "slow mastery," per the design brief). Wired into gathering (sticks) and
mining (rock node) via a `trainedSkill`/`skillGain` pair on `Pickup` and
`ResourceNode`. Initial gain values (stick=1, rock=5) were roughly 10x too generous
after playtesting — hitting level 6.9 off 3 actions — tuned down to 0.05/0.5.

Also: `TestScene` had never been added to `ProjectSettings/EditorBuildSettings.asset`
(`m_Scenes: []`), which is what caused a real empty-world bug for a collaborator on a
fresh clone.

### Loot & gathering with punch-to-break resource nodes (`88f51a9`)
`IInteractable` (E to pick up/hold-gather) and `IPunchable` (left-click to break)
interfaces, a minimal `PlayerInventory`. Loose items (sticks) are instant E-pickup;
rocks are punched to break into 3 physical chunks (via `RockChunk.prefab`, with
`Rigidbody`) that scatter and get picked up individually. Originally rocks used
hold-E-to-gather like a generic resource node; changed to punch-to-break per explicit
request, tying into the design brief's Basic Combat pillar.

**Gotcha found and fixed in the same commit:** the project had the URP package
installed and URP-only shaders on every material, but `ProjectSettings/GraphicsSettings.asset`
had no pipeline asset assigned (`m_CustomRenderPipeline: {fileID: 0}`) — so everything
was still rendering under the Built-in pipeline, which shows pink for any shader it
doesn't recognize. Created `Assets/Data/URP-Asset.asset` + `URP-Renderer.asset` and
wired them into Graphics Settings. (A related but distinct bug hit later: a Material
created at runtime via `new Material(...)` embeds fine into a *scene* file but not
reliably into a *prefab* — the `RockChunk` prefab's chunks rendered pink until the
material was saved as a real `.mat` asset first, then referenced.)

### Add first-person player controller (`1d02e9a`)
`FirstPersonController.cs` — `CharacterController`-based WASD move, mouse look,
sprint, jump — using the new Input System directly (`Keyboard.current`/`Mouse.current`),
no `.inputactions` asset. `ProjectSettings/ProjectSettings.asset` already had
`activeInputHandler: 1` (new Input System only) from project creation.

### Add minimal test scene (`bc460c6`) / project scaffold (`d2e9641`)
First scene in the repo — ground plane, directional light, camera — built via Unity
batch mode (`Unity.exe -batchmode -nographics -quit -executeMethod ...`) rather than
the Editor GUI, since these sessions run headless. The general pattern used throughout
this project: write a throwaway `Assets/Editor/*.cs` script, run it via batch mode,
verify the result by grepping the saved `.unity`/`.asset` YAML, then delete the script
— keeps the repo free of one-off setup code while still allowing scene edits without
a human driving the Editor UI.

### Unity version: 6.3 LTS, not 6.0 LTS (`78d0c44`)
Originally targeted 6.0 LTS (`6000.0.32f1`), but that version has a disclosed
vulnerability (CVE-2025-59489, patched at `6000.0.58f2`+). Since nothing had been
built yet, moved to 6.3 LTS instead of just patching 6.0, for a longer support runway
at the same near-zero switching cost.

### Design doc reconciliation (`0e4b1a2` through `adfd358`)
Ben's `game-overview.md` (narrative/setting pitch) and this repo's `design-brief.md`
(systems/technical brief) started as independent docs and were reconciled — magic
system, currency ladder, real-Earth-vs-replica, factions/guilds/warbands split. See
`docs/reconciliation-questions.md` for the decisions made.

### Initial commit (`7e8f5d5`)
Repo scaffold + `game-overview.md`. Predates the Unity project itself — see
`docs/design-brief.md` for the full systems design.
