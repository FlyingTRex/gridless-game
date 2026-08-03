# Changelog

Notable changes to the Gridless project, newest first. Written for whoever (human or
Claude session) picks this repo up next — includes the *why* behind non-obvious
decisions, not just the *what*. Full detail is always in `git log`; this is the
skimmable version.

**Current version:** `0.1.10-dev` — must always match `GameVersion` in
`Assets/Scripts/FirstPersonController.cs` (shown on-screen in the bottom-left debug
panel). Bump both together in the same commit whenever gameplay code/scenes/prefabs
change; see `CLAUDE.md` for the exact rule.

## 2026-08-03

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
