# Changelog

Notable changes to the Gridless project, newest first. Written for whoever (human or
Claude session) picks this repo up next — includes the *why* behind non-obvious
decisions, not just the *what*. Full detail is always in `git log`; this is the
skimmable version.

**Current version:** `0.3.175-dev` — must always match `GameVersion` in
`Assets/Scripts/FirstPersonController.cs` (shown on-screen in the bottom-left debug
panel). Bump both together in the same commit whenever gameplay code/scenes/prefabs
change; see `CLAUDE.md` for the exact rule.

## 2026-08-23 (9)

### v0.3.175-dev — Multiplayer sub-phase 2: a worn Backpack's nested inventory now networked too

`RequestMove`'s container-key scheme extended with `"worn:<slot>"` (that
slot's worn `IInventoryHolder`'s own `Inventory`), and `CmdMoveItem`
switched from the fixed-quantity `Move` to `MoveAsManyAsFit` to exactly
match `InventoryTransfer`'s own local semantics. `InventoryScreen.cs`
gained `ContainerKeyFor(Inventory)`, resolving a live `Inventory`
reference back to a container key (main inventory or any worn slot's
nested inventory); returns `null` for anything this scheme doesn't cover
(Furnace zones, NPC cargo), correctly falling through to the original
local path. `TryDrop`'s generic drag branch now routes through the
Command whenever both sides resolve to a known key. Live-confirmed:
dragged Sticks into and out of a worn Backpack's contents, correct count
both directions, zero errors. See `MULTIPLAYER_PLANNING.md` section 3
item 3 sub-phase 2.

## 2026-08-23 (8)

### v0.3.174-dev — Multiplayer sub-phase 2: multi-destination Equip wiring complete, real pre-existing dual-wield bug found and fixed

All four `TryEquipWithChoice` overloads (Canteen, NavigationComputer,
PersonalHealthMonitor, Tool) now route through a shared
`TryNetworkedEquip` helper at both apply points — the immediate
single-destination case and the popup's chosen-destination callback —
completing Equip wiring for every path `InventoryScreen.cs` supports.
Testing it surfaced a real pre-existing bug, not caused by the networking
work: `InventoryScreen.IsCurrentlyWorn` checked each carrier's own
`Equipped` property, which only returns the *first* match across that
type's valid slots — with a Knife in Left Hand and an Axe in Right Hand
(two Tools worn at once), the Axe always read as "not worn." Fixed by
scanning every body slot directly via `PlayerEquipment.GetEquipped`
instead. New `CLAUDE.md` gotcha logged — Canteen/NavComputer/HealthMonitor
share the identical shape and deserve the same "two worn at once" test
when that work comes up. Live-confirmed clean after the fix. Both Equip
and Unequip are now real, networked UI actions for every case
`InventoryScreen.cs` supports from the main inventory. See
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-23 (7)

### v0.3.173-dev — Multiplayer sub-phase 2: real Equip wiring done, both directions now networked

`EquipToSlotDispatch` (drag onto a known slot) routes through the Command
unconditionally from the main inventory — no ambiguity, the drag target
already picked the destination. `EquipWithChoice` (click-to-equip) adds a
`FindSingleValidSlot` check: an unambiguous single-destination type
(Backpack, Belt, Boot, Sunglasses, MiningFaceShield, Shirt, Jeans) routes
directly through the Command; a genuinely ambiguous multi-destination
type (Canteen, NavigationComputer, PersonalHealthMonitor, Tool) falls
through to the existing local choice-popup flow unchanged, since a click
has no destination to pass until the player picks one. Live-confirmed all
three real player-facing paths: drag-equip, single-destination
click-equip, multi-destination click-equip (popup still works correctly)
— zero errors across all three. Both Equip and Unequip are now real,
networked UI actions for the common case (main inventory ↔ any equipment
slot). See `MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-23 (6)

### v0.3.172-dev — Multiplayer sub-phase 2: first real UI wiring (Unequip), plus a real spawn-gap bug found and fixed

`InventoryScreen.UnequipDispatch` now routes through
`PlayerInventory.RequestUnequipInstance` whenever the target has a
`NetworkIdentity`, falling back to the original local switch defensively
otherwise — chosen first since Unequip needs no "which source"
disambiguation, unlike Equip. Real gap found live during testing: only
`PlayerDropping.SpawnPickup` had the `NetworkServer.Spawn` treatment;
unequipping a *crafted* Knife threw "Attempted to serialize unspawned
GameObject" since `PlayerCrafting.cs` instantiates its own equipment
output directly, never going through `SpawnPickup`. Extracted a shared
`NetworkSpawnHelper.SpawnIfNetworked(GameObject)` and applied it to every
real call site that instantiates an interactable item: `PlayerDropping`
(refactored), `PlayerCrafting` (the actual bug), `EquipmentSaveUtility`
(save/load restore), `PlayerWriting` (written Skill Books).
Deliberately NOT applied to `NPCEquipmentVisual`'s purely cosmetic,
physics-disabled NPC gear display. Live-confirmed clean after the fix:
equipped and unequipped a crafted Knife through the real UI, zero
errors. See `MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-23 (5)

### v0.3.171-dev — Multiplayer sub-phase 2: every equippable type networked, one generic Command covers all of them

Bulk pass: the remaining 48 equippable prefab variants (Belt/Boot/
Sunglasses/MiningFaceShield/Canteen/NavigationComputer/
PersonalHealthMonitor/Tool/Shirt/Jeans, Backpack's other 9 variants, plus
SkillBook/StorageBox — both also implement IEquippable, caught
automatically) given NetworkIdentity — 127 total prefabs now networked
(78 Pickups + 49 equippables). Real design improvement found applying the
Backpack pattern a second time: instead of converting all 10 remaining
carrier scripts to NetworkBehaviour with their own Command pairs (what
the Backpack pilot did), built ONE generic
RequestEquipInstance/CmdEquipInstance + RequestUnequipInstance/
CmdUnequipInstance pair on PlayerInventory, mirroring
InventoryScreen.cs's own EquipToSlotDispatch/UnequipDispatch switch
statements — one shared Command dispatching to whichever carrier's
existing Equip/Unequip method handles that type. None of the 10 carrier
scripts needed to become NetworkBehaviour; only the item instances
needed NetworkIdentity. Live-confirmed via a temporary debug keybind
(removed): equipped a real Belt and a real Boot through the same shared
Command, zero exceptions. Both major sub-phase 2 rollout pieces (world
pickups, all equippables) are now fully done, not pilots. See
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-23 (4)

### v0.3.170-dev — Multiplayer sub-phase 2: world-pickup networking done in full

Bulk pass: 78 of 127 unique `worldPickupPrefab` prefabs (every one
carrying a plain `Pickup` component — the other 49 are equippable types
with their own carrier flow) given `NetworkIdentity` and registered in
`NetworkManager.spawnPrefabs`. `Pickup.Complete()` now routes through a
server-authoritative Command (`PlayerInventory.RequestCompletePickup`/
`CmdCompletePickup`) when networked, reusing the exact same resolution
logic (renamed `ServerComplete`) with `NetworkServer.Destroy` in place of
a plain `Destroy` — one shared conversion covering all 78 prefabs at
once. Two real bugs found and fixed during rollout, both root-caused via
direct `Editor.log` reads rather than guessed at: (1) two already-placed
scene pickups turned out to be `NotAPrefab` (hand-placed, never inherited
the bulk `NetworkIdentity`), which cascaded into unrelated-looking
`NullReferenceException`s elsewhere (`PlayerInventory`, `NPCEncumbrance`,
`Furnace`) until traced to this one root cause; (2) two prefab-instance
objects needed the scene resaved to backfill valid scene IDs after their
prefab gained a `NetworkIdentity` — new process lesson logged for next
time this pattern comes up. Live-confirmed clean after both fixes: full
pickup Command round-trip working, zero exceptions in a fresh session.
This is the single largest piece of sub-phase 2's actual rollout, fully
done — not just a pilot. See `MULTIPLAYER_PLANNING.md` section 3 item 3
sub-phase 2.

## 2026-08-23 (3)

### v0.3.169-dev — Multiplayer sub-phase 2: real equippable-instance pilot (Backpack) networked and confirmed

Real gap found while planning to wire the previous session's plain-item
move Command into the actual UI: worn gear (Backpack, Canteen, Tool, ...)
doesn't move through `InventoryTransfer.Move` at all — `InventoryScreen.cs`
dispatches per-type to ~11 dedicated carrier components that take a
reference to the actual equippable *instance*, and a `[Command]` can only
receive a `GameObject`/`NetworkIdentity` for an object genuinely spawned
on the network — which no equippable or world Pickup had. World-pickup
networking and real equip/unequip Commands turned out to be the same
underlying blocker. Proved the pattern on one prefab instead of every
type: `NetworkIdentity` added to `MasterworkLeatherBackpackPickup.prefab`
specifically (Backpack alone has 10 tier/material variants — even "one
type" isn't one asset here) and registered in `NetworkManager.spawnPrefabs`;
`PlayerDropping.SpawnPickup` now calls `NetworkServer.Spawn()` for any
spawned object carrying a `NetworkIdentity`; `PlayerBackpack.cs` converted
to `NetworkBehaviour` with a real `RequestEquip`/`CmdEquip` and
`RequestUnequip`/`CmdUnequip` pair. Live-confirmed via a temporary debug
keybind (removed): Admin-Spawned a Masterwork Leather Backpack, equipped
it through the real Command (visually worn, nested contents accessible),
unequipped it (Back slot correctly emptied). Sub-phase 2's core sync +
Command infrastructure is now proven across every real shape it needs
(add-item, move-plain-item, equip-a-real-instance) — the actual rollout
(every other equippable prefab, every world Pickup, real UI wiring) is
explicitly deferred, mechanical-but-large follow-on work. See
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-23 (2)

### v0.3.168-dev — Multiplayer sub-phase 2: real equip/unequip Command working end to end

`PlayerInventory.RequestMove`/`CmdMoveItem` — moves an item between the
main inventory and a named `PlayerEquipment` body slot (either direction)
by reusing `InventoryTransfer.Move` server-side, not reimplementing its
already-correct equipment-aware logic. Containers addressed by a simple
string key since a Command can't carry a raw `Inventory` reference over
the wire. Live-confirmed via a temporary debug keybind (removed): moved a
Stick main→Left Hand (actually appeared in the Equipment panel), moved it
back (correctly re-stacked). Deliberately not wired into the real
`InventoryScreen.cs` drag-and-drop UI — that's a separate, larger, riskier
task (it moves between many other container types this scheme doesn't
cover). Sub-phase 2's core sync infrastructure — real `SyncList` sync on
both `PlayerInventory`/`PlayerEquipment`, two working Commands proving the
full request/validate/apply shape — is now a reasonable stopping point,
with UI-wiring and world-pickup networking logged as explicitly deferred
follow-on work. See `MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-23

### v0.3.167-dev — Multiplayer sub-phase 2: PlayerEquipment is now a NetworkBehaviour too

Real gap found before writing code: unlike `Inventory` (mostly plain
stackable slots), an equip slot is virtually always equipment-carrying by
definition, so excluding those from sync the way `PlayerInventory` does
would sync nothing. Fixed by syncing *which item* occupies each named
slot (`SyncedEquipmentSlot { slotName, itemId }`) rather than trying to
exclude equipment slots — the equipped object's own deep state (a worn
Backpack's contents, a Canteen's fill level) still isn't synced, same
complexity boundary as before, just correctly applied to this class's
actual data shape. Live-confirmed via a temporary debug `OnGUI` (removed):
correctly showed real starting gear on scene load, correctly tracked a
live equip/unequip/re-equip cycle. Both `PlayerInventory` and
`PlayerEquipment` now have working `SyncList` sync; neither yet has a
real Command for a remote client to actually request an equip/unequip —
still ahead. See `MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-22 (13)

### v0.3.166-dev — Multiplayer sub-phase 2: Player connection authority fixed, first real Command working end to end

Real gap found while designing the first Command: `Player` was a
server-owned scene `NetworkIdentity` with no connection ever assigned
ownership (`autoCreatePlayer` is off), so any `[Command]` on it would fail
with an authority error. Fixed via `GridlessNetworkManager.cs` (new
`NetworkManager` subclass, swapped onto the scene's `NetworkManager`)
overriding `OnServerReady` to call `AddPlayerForConnection` for the
existing scene Player. First attempt used `OnServerConnect` and hit a
real, informative error (`NetworkClient can't AddPlayer before being
ready`) — `OnServerReady` is the correct hook, confirmed via debug logging
showing `isOwned` flip `False`→`True` at exactly the right point.
`PlayerInventory.RequestAddItem`/`CmdAddItemById` (new): the first real
Command, live-confirmed end to end via a temporary debug keybind (removed)
— client request logged, then the Command's own server-side log with the
correct item/quantity/leftover, in order. Proves the actual
Command/validate/apply shape sub-phase 2 needs, now on the real
`PlayerInventory`. Still ahead: converting real callers (`Pickup.cs`,
`PlayerCrafting`, equip/unequip) to route through Command-validated
methods, and the equivalent work for `PlayerEquipment`. See
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-22 (12)

### v0.3.165-dev — Multiplayer sub-phase 2: real SyncList inventory sync built and live-confirmed

`PlayerInventory.SyncedInventorySlot` (a `[Serializable]` struct of
`itemId`/`count`) + `SyncList<SyncedInventorySlot> syncedSlots`, resolved
by string ID via `ItemDatabase` — Mirror can't natively sync a
`ScriptableObject` reference like `ItemDefinition`. Deliberately excludes
equipment-carrying slots (a worn Backpack/Canteen — a live object, not
just data), the same complexity boundary `SAVE_LOAD_PLANNING.md` already
drew for persistence v1. Polled server-side from `Update()` via a
change-signature comparison rather than hooked into every mutation call
site, since `Inventory` has dozens of direct mutators and no built-in
change notification. Live-verified with a temporary debug `OnGUI`
(removed after confirming): picked up a Potato Seed, correctly excluded
an equipment-routed Skill Book, correctly did NOT reflect Sticks that
landed in a separate equipped Backpack, then correctly reflected them
once moved into the main inventory — right items, right counts every
time. Still ahead: converting `AddItem`/`RemoveItem` and the other direct
mutation call sites into Command-validated calls, so a remote client can
actually request a change rather than just observe server state. See
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-22 (11)

### v0.3.164-dev — Multiplayer sub-phase 2: PlayerEquipment is now a NetworkBehaviour too

Same slice as `PlayerInventory.cs` earlier tonight: `PlayerEquipment.cs`
converted from `MonoBehaviour` to `NetworkBehaviour`, base-class change
only, no dynamic instantiation found anywhere to break. Live-tested the
full cycle: equipped an Axe (Left Hand slot updated correctly), chopped a
tree with it (confirms the equipped item is genuinely functional, not
just visually shown), unequipped it back into a worn Jeans' nested
inventory. Both core inventory-family scripts are now `NetworkBehaviour`
with solo play fully unaffected. The real remaining work — a `SyncList`
serializer for `ItemDefinition`-by-ID and Command-converting actual
mutation call sites — is still ahead. See `MULTIPLAYER_PLANNING.md`
section 3 item 3 sub-phase 2.

## 2026-08-22 (10)

### v0.3.163-dev — Multiplayer Phase 3 sub-phase 2 first slice: PlayerInventory is now a NetworkBehaviour

`PlayerInventory.cs` converted from `MonoBehaviour` to `NetworkBehaviour` —
deliberately isolated to just the base-class change, no new synced state
yet. Real complication found before writing code: Mirror doesn't natively
sync a `ScriptableObject` reference like `ItemDefinition`, so a genuinely
synced Inventory needs a custom `SyncList` serializer resolving items by
string ID (the same pattern `SaveManager`/`ItemDatabase.Find(id)` already
use), not a trivial step — logged as real follow-up work in
`MULTIPLAYER_PLANNING.md`. Confirmed all 36 `GetComponent<PlayerInventory>()`
call sites are simple reads, no dynamic instantiation to break. Live-tested:
Inventory screen, pickup, and drop all work normally. See
`MULTIPLAYER_PLANNING.md` section 3 item 3 sub-phase 2.

## 2026-08-22 (9)

### v0.3.162-dev — Multiplayer Bootstrap regression fixed: player-lookup race across 4 scripts

Ben caught it live: "the wolf never moved towards me." Root cause:
`HostileCreature.Start()`, `Campfire.Start()`, `PreyWander.Awake()`, and
`ResourceNode.Start()`'s disguised-shield-wearer lookup all shared the
same fragile pattern — a one-shot `FindFirstObjectByType<PlayerVitals>()`/
`PlayerMiningFaceShield` lookup, cached forever. Now that Player carries a
`NetworkIdentity` (v0.3.161-dev), it can be transiently deactivated at the
exact moment one of these objects' own `Awake()`/`Start()` runs, since
Unity doesn't guarantee cross-object execution order — whichever ran
first could permanently miss the player. Fixed uniformly: a lazy
`ResolvePlayerTarget()`/`ResolveShieldWearer()` retry, no-op once already
resolved, called at startup and again from `Update()` whenever still
null. Live-confirmed fixed: a Wolf genuinely attacked and damaged the
player, a Campfire genuinely warmed them. See `MULTIPLAYER_PLANNING.md`
section 3 item 3 for the broader lesson — this class of bug (a one-shot
cached player lookup) is now a latent risk anywhere else in the codebase
too, worth a deliberate sweep before sub-phase 2 starts.

## 2026-08-22 (8)

### v0.3.161-dev — Multiplayer Bootstrap complete: real Player, real server/client loop, solo play unchanged

Re-added `NetworkIdentity`/`NetworkTransformReliable` to the real Player —
this time it worked. With `NetworkAutoHost` guaranteeing a server exists
before Mirror ever needs to spawn scene objects, the original deactivation
bug (blank camera, `PlayerTool` NRE) did not recur. Live-confirmed with a
full gameplay pass: camera rendered, HUD worked, opened a Furnace, killed a
Wolf bare-handed, `Editor.log` showed clean `NetworkServer.active=True,
NetworkClient.active=True`. This closes out Multiplayer Phase 3 sub-phase 1
(Bootstrap) in full — `Player` is now a genuine networked object running
through Mirror's real server/client loop, with solo play behaviorally
identical to before. One unrelated minor note logged to
`BUGS_AND_ENHANCEMENTS.md`: the Wolf didn't fight back when attacked during
this test. Sub-phase 2 (Inventory + Equipment) is next. See
`MULTIPLAYER_PLANNING.md` section 3 item 3 for full detail.

## 2026-08-22 (7)

### v0.3.160-dev — Multiplayer Phase 3 Bootstrap: auto-host-on-load built and live-confirmed

`NetworkAutoHost.cs` (new, attached to `NetworkManager`): calls `StartHost()`
from `Start()` the instant `TestScene` loads, if not already networking.
This is what makes the "solo session = host alone" scope-shape decision
real — pressing Play now genuinely runs through Mirror's server/client loop
underneath with zero visible change. Live-confirmed via `Editor.log`:
`NetworkServer.active=True, NetworkClient.active=True`, no warnings (also
explicitly wired `NetworkManager.transport` to clear a harmless "no
Transport assigned" warning). Full gameplay spot-check afterward (Vendor
Stall screen, a Restoration wish tier-up) confirmed nothing regressed.
`NetworkIdentity`/`NetworkTransformReliable` on the real Player itself is
still the next piece — now that a server always exists at Play start, the
original deactivation bug shouldn't recur, but that's untested until
actually tried in a future session. See `MULTIPLAYER_PLANNING.md` section 3
item 3 sub-phase 1.

## 2026-08-22 (6)

### v0.3.159-dev — Multiplayer Phase 3 Bootstrap: prefab conversion done, real Awake-order bug found and fixed

Retried the Bootstrap prefab conversion in isolation (no `NetworkIdentity`
this time) after the earlier full revert. The exact same `PlayerTool` NRE
reproduced with zero Mirror components involved, ruling out the earlier
"Mirror deactivation" theory as the sole cause. Real root cause:
`PlayerBodyModel.Awake()` called `ApplyGender()`, which reaches into 11
other components (`PlayerTool`, `PlayerBackpack`, `PlayerBoot`, ...) that
only work once their own `Awake()` has already run — an implicit ordering
dependency on component-list position that `SaveAsPrefabAssetAndConnect`
disturbs. Fixed by deferring the initial `ApplyGender(isMale)` call to
`Start()`, which Unity guarantees runs after every component's `Awake()`
regardless of order — a genuine standalone bugfix. Two further "regressions"
found during debugging turned out to be false alarms, both confirmed via
temporary debug logging added to and then removed from `PlayerCombat.cs`:
a "craft" progress bar on a live Wolf is pre-existing `SkinnableCreature`
skin-interaction behavior, and "left-click does no damage" was
`PlayerCombat` correctly refusing to punch while a Bow was equipped. Live-
confirmed clean straight from `Editor.log`: 3 real punches, 9 damage each,
Wolf killed. `Assets/Prefabs/Player.prefab` now exists as a real, connected,
working prefab. `NetworkIdentity`/`NetworkTransformReliable` deliberately
not re-added this session — stopping at this clean checkpoint rather than
pushing into a second risky step same-session. See `MULTIPLAYER_PLANNING.md`
section 3 item 3 sub-phase 1 for full detail.

## 2026-08-22 (5)

### Multiplayer Phase 3 Bootstrap attempt — two real regressions found live, fully reverted (doc-only, no version bump)

Attempted the Bootstrap sub-phase: converted the scene-baked `Player`
GameObject (75 components — the entire player-side system stack) into
`Assets/Prefabs/Player.prefab` via `SaveAsPrefabAssetAndConnect`, added
`NetworkIdentity`/`NetworkTransformReliable`, and added an inert
`NetworkManager`/`KcpTransport` to `TestScene.unity`. Ben caught a real
regression within a minute of live-testing (blank "No cameras rendering"
screen, `PlayerTool` NRE) — Mirror deactivates any scene-placed
`NetworkIdentity` until a server spawns it, and `TestScene` had no way to
start one, so the whole Player hierarchy silently went inactive. Root-
caused live (confirmed via the Hierarchy's grayed-out inactive-object
indicator), `NetworkIdentity`/`NetworkTransformReliable` reverted, camera
confirmed working again. **A second, unexplained regression then
surfaced even in that reverted state**: bare-handed combat stopped
registering left-click, and E-key interaction started resolving to an
unexpected "craft" prompt while aiming at a Wolf (which doesn't implement
`IInteractable` at all). `git diff --stat` showed the saved scene had
shrunk ~128KB from the prefab conversion alone — far more than two small
component additions/removals could explain. Rather than debug forward
from a scene already shown to silently corrupt something once, reverted
the entire experiment via `git checkout -- Assets/Scenes/TestScene.unity`
plus deleting the untracked `Player.prefab`. Real conclusion logged in
`MULTIPLAYER_PLANNING.md`: converting the real 75-component Player object
via `SaveAsPrefabAssetAndConnect` isn't safe to treat as a small, inert
first step — a future attempt needs either a much smaller isolated test
object first, or a careful reference-by-reference audit of what that
conversion actually changes on an object this size. Sub-phase 1 is back
to fully unbuilt.

## 2026-08-22 (4)

### v0.3.158-dev — Multiplayer Phase 1 pilot — synced world object, live-tested

`NetworkStorageBoxSpike.cs` (new): a `NetworkBehaviour` with a
`SyncList<string> items` standing in for a real Inventory, placed in
`NetworkSpike.unity`. `NetworkSpikeMovement.cs` gained E (add "TestOre")/R
(remove top) keybinds, each a `[Command]` that validates a 3m range
server-side before mutating the box — the same client-requests/
server-validates/server-applies shape Phase 3's real `PlayerXXX.cs`
conversion will need at scale, proven once here on a single throwaway
world object instead of jumping straight into the real 32+-script
conversion. Ben live-tested with the same Host/standalone-Client two-process
setup as the Phase 0 test: added and removed items from both windows,
confirmed both sides always showed the identical, live-updating list —
genuinely shared server-owned state, not two independent local ones.
Verified via a second independent batch-mode process reading the saved
scene back through Unity's own API (this project's scenes use Force Binary
serialization, so a guid grep can't confirm an object reference landed —
see `CLAUDE.md`'s own gotcha on this) before considering the placement
correct. See `MULTIPLAYER_PLANNING.md` section 3 item 2 for full detail.

## 2026-08-22 (3)

### Multiplayer Phase 0 spike — live-tested for real (doc-only, no version bump)

The `NetworkSpike.unity` infrastructure spike (built 2026-08-19, v0.3.145-dev)
sat compiled-but-never-run for three days — Ben ran the actual two-process
test tonight. Temporarily added `NetworkSpike` to the Scene List via Unity
6's Build Profiles (the renamed Build Settings), built a standalone Windows
player, ran it alongside an Editor Play-mode Host over Mirror's KCP
transport. Both capsules were visible and moved live in both windows
regardless of which process's input drove the movement — confirms
`NetworkTransformReliable` with `syncDirection = ClientToServer` actually
replicates bidirectionally in this project, not just in theory. This is the
first real confirmation Mirror's transport/sync loop works here at all,
outside a clean compile. Doesn't settle the deeper movement-authority
question (client-authoritative vs. server-authoritative-with-reconciliation
— a client can still lie about its position under this model), but the
"does the toolchain even work" uncertainty is closed. Build Profiles' Scene
List was restored to `TestScene`-only immediately after. See
`MULTIPLAYER_PLANNING.md` sections 3 and 4 for the updated detail.

## 2026-08-22 (2)

### v0.3.157-dev — Vendor Stall till visibility + restock timer, full player
naming system, all live-tested

Direct follow-ups from live-playtesting v0.3.156-dev, all confirmed working
in a real session, not just batch-verified.

**Vendor Stall UI gaps closed.** `VendorStallScreen` now shows the till's
full 5-coin breakdown (Copper/Iron/Silver/Gold/Platinum) instead of just
Copper — the old display was hiding that the new all-5-coin-per-tick regen
was quietly growing 4 balances nobody could see. Confirmed live growing
evenly across a real session. New `VillageVendor.NextFullRefreshSeconds`,
same "payment due in Ns" convention the NPC screens already use, shown in
the Vendor Stall screen and persisted across save/reload — the restore path
is guarded so `Initialize()` can't clobber a restored timer back to a fresh
30-minute countdown (functionally tested for that guard specifically, and
confirmed live counting down correctly across two separate saves).

**A full player naming system**, closing the design conversation from
earlier the same session. New `PlayerIdentity.cs`, ported from
`NPCDialogue`'s proven naming shape but with its own dedicated Player-tab
entry point rather than `IRenameable`'s raycast trigger (right-click-rename
doesn't make sense on yourself). First rename is free; every one after that
costs Gold on a `PlayerFame`-band-tiered scale (`RenameCostGold`, symmetric
across both Infamous and Renowned — a well-known identity costs more to
replace either direction) plus a one-directional Fame penalty that only
fires when currently negative (`RenamePenalty` — discourages using a rename
to escape a bad reputation, matching `MULTIPLAYER_PLANNING.md`'s original
intent; kept as an independent mechanic from the symmetric Gold-cost table,
not the same rule doing double duty). Basic sanitization only (trim, 30-char
cap, strip control characters) — real profanity filtering is logged as a
genuine pre-multiplayer requirement in `BUGS_AND_ENHANCEMENTS.md`, not built
blind, since single-player has no one else who could be harmed by an
inappropriate name today. A "warning box telling the player to set a name"
idea was deliberately rejected after a "be mean" pass — no other feature in
this game nags the player to use it, and the real trigger point (a future
multiplayer character-creation flow) doesn't exist yet; building a stopgap
now would just be thrown away later. Functionally tested 21/21 (free first
rename, cost/sanitization gating, the full Fame-tier cost/penalty table, and
that the penalty actually applies during a real rename) — real mistake
caught and fixed along the way: the rename control was first built into the
wrong menu (`PlayerMenuScreen`, the Tab-key menu) instead of the
`` ` ``-key `GameMenuScreen` Ben had actually asked for; found live from a
screenshot, and left on the Tab menu at Ben's call once he'd already found
and used it there.

**Logged, not built**: Iron Arrow reported flying backwards like the
original Stone Arrow bug — a real code check found every arrow shot uses
the identical generic flight visual and the same already-fixed orientation
correction regardless of material, so Stone and Iron should look identical
in flight; genuinely unexplained yet. Logged with the code-check findings
for a future live re-test with a screenshot, rather than guessed at further.

Two new memories saved from this session: batch-mode tests are a necessary
floor, not a sufficient ceiling, for any feature touching real-time
component lifecycle, save/reload, or UI interaction (this stretch of work
found real bugs in exactly those categories that no batch test could have
caught); and a wildcard-globbed Unity Editor path can silently match a
second installed version and crash every batch-mode command with no useful
error (hit live this session — pin the exact version from
`ProjectVersion.txt` instead, now documented as its own `CLAUDE.md` gotcha).

## 2026-08-22

### v0.3.156-dev — Vendor Stall + Bank Box made real, earned structures; full
multi-denomination currency system; a real, previously-latent save/reload bug
found and fixed via actual live playtesting

The single largest feature of the session, built on top of MVP2B's original 6
items (`MVP2B_PLANNING.md` has the full extended design writeup). Grew out of
a long conversational "walk through real usage" pass with Ben — multiplayer
village-hopping, currency exchange, skill book trading, food-recipe supply —
that surfaced real gaps and caught real mistakes before any of them shipped
broken, including an off-list-sell exploit path, a tool-value curve that
couldn't hit the requested price spread, a redundant "Village Lockbox" idea
rejected before being built, and (the big one) a genuine persistence bug only
a real save→exit→reload cycle could have caught.

**Vendor Stall and Bank Box are now real, earned structures, not free
starting fixtures.** New `BuildPiece.requiresVillageFlagAndHiredNpc` gate
(≥1 Village Flag + ≥1 currently-hired NPC — a much lower bar than City
Statue's Masterwork+10) plus a one-per-Flag cap (`PlayerBuilding
.NearestFlagAlreadyHasStructure<T>`, checked at the real placement position).
`VendorStallPiece` (6 Plank/2 Cloth/2 Rope) and `BankBoxPiece` (4 Plank/4
Iron Ingot/2 Rope) are real assets + prefabs now, registered in
`BuildPieceDatabase`. Both grant +10 Fame on placement. The previously
always-free, pre-placed `Village Vendor` and `Bank Box` scene fixtures were
removed from `TestScene.unity` — a fresh game now starts with neither,
Copper-only, until the gate is met. The Vendor Stall's Tripo3D-generated
model (colorful striped awning, wood-grain counter, rope-lashed frame) is
wired onto the prefab, scaled/grounded per the project's own model-placement
checklist, and verified by actually rendering it (hit and fixed the
documented `-nographics` pitfall along the way).

**A dedicated, back-loaded value curve for weapons/tools.** Every tool tier
shares identical flat ingredients (an Axe is always 1 Rock + 2 Stick
regardless of tier), so the general 25x tier-scaling spread couldn't express
"a Masterwork tool took dramatically more skill to make." New
`CraftTierScale.ToolMarketValueModifier`, derived from `SkillRequirement`
(the real skill-investment curve, so it auto-rescales if tier difficulty
ever changes) rather than hand-picked numbers — verified against real recipe
data: Crude 5.00 → Rudimentary 6.25 → Normal 24.45 → Fine 160.63 →
Masterwork 1,250.00, genuinely back-loaded (a geometric first attempt was
tried and rejected for not actually matching "plausible early, hard
endgame"). 43 items now flagged `sellableByVendor` (9 original + 8 seeds +
26 tools).

**`VendorStall` core rewrite**: supply/demand pricing (a bounded ±30% stock-
based adjustment on both buying and selling), tier-gated off-list selling
(any `sellableByVendor` item not currently displayed can still be sold, at a
live-computed price, gated by the same Flag-tier ceiling that already gates
what's stocked — closes a real exploit where a low-tier vendor could
otherwise be forced to deal in high-tier goods). `VillageVendor`'s stocking
rewritten for 8 distinct items (6 general + 2 dedicated seed slots, no
duplicates) at a random 1-to-maxStack quantity each, replacing the old
duplicate-with-replacement logic.

**Real, live-playtest bugs found and fixed in the same session** (not just
compile-checked): `PlayerInteraction`'s raycast could hit the vendor's
internal stock box instead of the stall itself (showed "Storage Box" as the
prompt) — fixed by disabling that box's collider, since it only ever needs
to be accessed programmatically. `ShowGhost`'s aiming preview turned out to
fully instantiate the prefab, live gameplay components included — the ghost
was tripping the new one-per-Flag check against itself (blocking every
placement attempt) and running `VillageVendor`'s real init logic just from
being aimed, before any placement was confirmed — both fixed by gating on
`PlacedPiece`, which only a genuine confirmed placement ever has.
`VendorStallScreen` only ever checked the player's bare main inventory for
both buying and selling (confirmed live: Fiber spawned directly into a worn
Backpack still couldn't be sold) — fixed with a real `ReachableInventories`
reach (main + Backpack + nearby StorageBox), which required changing
`VendorStall.SellToVisitor`/`BuyFromVisitor`'s signature to a proper
multi-inventory list. Added a "Sell Other Items" UI section once it became
clear the off-list-selling backend had no way to actually be triggered from
the screen. Redesigned `VendorStallScreen` from a row list to a tile grid
(matching Crafting/Build's existing convention).

**A full multi-denomination currency system**, prompted by a 2,100-Copper
Masterwork Pickaxe already exceeding both the 250-per-type wallet cap and
the old 500-Copper-only till. New `CoinValue`/`CoinSpender` (the 10:1
Copper→Iron→Silver→Gold→Platinum ladder made explicit and reusable, plus a
deliberately scoped-down greedy spend/denominate algorithm — real coin-
breaking change-making was evaluated and ruled out as too much complexity/
risk for this pass, documented directly in `CoinSpender.cs`'s own header).
The vendor's till changed from a bare Copper-only `int[5]` to a real
`Lockbox` (an already-built component, reused rather than reinventing
per-type capacity — gained `[RequireComponent(typeof(SaveId))]` so it
persists), tier-scaled to the linked Village Flag (a "be mean" pass rejected
a separate Fame-gated "Village Lockbox" idea as pure redundancy with this —
Flag-tier scaling already delivers "grow the village to trade in better
goods" on its own). Regen now adds 1 of every coin type per tick, not just
Copper. A Pay-from-Bank fallback, gated on an unlocked Bank Box, lets a
purchase draw straight from the Bank (existing fee) when the wallet alone
can't cover it; a sale payout that would overflow a wallet balance now
routes the excess into Bank instead of the old silent-loss bug (`PlayerCurrency
.Add`'s leftover return value was never checked) — and refuses the sale
outright if no Bank Box is unlocked yet, rather than losing currency.

**A real, previously-latent persistence bug, only caught by an actual live
save→exit→reload cycle** — present since the original MVP2B build, not
introduced by any of tonight's work. `VillageVendor.EnsureStockBox`/
`EnsureTillLockbox` both create their container via a script-driven
`AddComponent`, which auto-adds a `SaveId` through `RequireComponent` but
doesn't reliably fire that `SaveId`'s own `Reset()` (documented directly in
`SaveId.cs`) — so its id silently stayed empty. Every other creation site in
the codebase (`RestorePlacedPieces`, `RestoreNpcs`) already calls
`GenerateIfMissing()` explicitly for exactly this reason; these two never
had it. Confirmed directly from the save file (`stockSaveId`/`tillSaveId`
both `null`) and confirmed fixed the same way: build a Vendor Stall, save,
exit, reload — identical stock and till, not a fresh reroll.

Nearly everything in this entry was functionally tested against real
running code via throwaway batch-mode scripts as it was built (60+ individual
checks across value-curve, stocking, gate-logic, reachable-inventory, and
currency-system passes) — but the save/reload bug above is the clearest
reminder yet that batch tests are a real safety net, not a substitute for an
actual live Play-mode pass. See `WORKING_ON.md`'s history (cleared this
commit) for the full blow-by-blow if needed.

## 2026-08-21

### v0.3.155-dev — MVP2 closed out in full; MVP2B Commerce (`VendorStall` + Village
Vendor) designed and built end-to-end; NPC NavMesh pathfinding (Phases 0-2); a
cluster of live-playtest fixes

The single largest commit of the session, spanning three real threads. See
`MVP2_PLANNING.md`, `MVP2B_PLANNING.md`, `COMMERCE_PLANNING.md`, and
`NPC_NAVIGATION_PLANNING.md` for full design detail on each.

**MVP2 closed out.** Confirmed live: NPC freeze/unfreeze, Iron Arrow, Bow
Release animation. Removed the "NPC name reverts" tracking item — never
reproduced across multiple live-testing passes. Moved the multi-day
work-shift timer from MVP2 scope to `BUGS_AND_ENHANCEMENTS.md` as an
enhancement rather than a blocker. Confirmed the SaveId mass-regeneration
concern is a non-issue via real testing (spawn → save → exit → reload,
watched the Editor log for the right timing).

**MVP2B Commerce — `VendorStall` core + Village Vendor driver, all 6 items
built and functionally tested (not just compiled) against real running
code.** Full "propose → be mean → fix" design pass with Ben before any
code: found and fixed a real exponential tier-compounding bug in the
pricing formula during design (tier scaling was being applied at every
recursive ingredient step instead of once at the top). Built:
- `StorageBox` ownership gate (`isPlayerOwned`, defaults `true`) — gated at
  the real access choke point (`FindNearby`, shared by `InventoryScreen`
  and `PlayerCrafting`), not just the UI layer.
- `ItemValueCalculator` + new `RecipeDatabase` (reverse item→recipe
  lookup, following the existing `ItemDatabase`/`SkillDatabase`
  convention) + `ItemDefinition.baseValue`/`sellableByVendor`. Smoke-
  tested against a real 2-step recipe chain (Iron Ore → Ingot → Arrowhead)
  to confirm the compounding fix holds.
- `VendorStall` core — atomic `SellToVisitor`/`BuyFromVisitor` (every
  precondition checked before any state moves; a failed transaction never
  charges the buyer or touches their inventory). Functionally tested 5/5
  against real transaction scenarios.
- `VendorStallScreen` (mirrors `FurnaceScreen`'s shape).
- `VillageVendor` driver — no owner, priced entirely off
  `ItemValueCalculator`, stock gated by the linked Village Flag's current
  tier, reactive per-slot restock plus a 30-minute full refresh, till
  regenerates over real time. Functionally tested 9/9; caught and fixed a
  real rounding bug (low-`baseValue` items could round buy/sell to the
  same integer, breaking the price-spread guarantee).
- Persistence — a real ordering hazard was found and fixed *before* it
  could bite live: `VillageVendor`'s setup used to run in `Start()`, which
  has no ordering guarantee against `SaveManager.Load()`'s own `Start()`,
  risking a fresh random reroll clobbering real saved state. Fixed by
  deferring setup to the first `Update()` tick instead, which Unity *does*
  guarantee runs after every object's `Start()`. Verified with a real
  capture → destroy → restore round trip.
- 9 real starter items seeded with `baseValue`/`sellableByVendor` (Stick,
  Fiber, Plank, MRE Ration, all 5 Ore tiers).

**NPC NavMesh pathfinding — Phases 0, 1, and 2 built**, replacing the
straight-line-plus-local-deflection movement that hit a real structural
ceiling live (an NPC stuck ping-ponging off a wall it can't route around,
a door several meters away being outside what local deflection can ever
discover). `com.unity.ai.navigation` added; `NavMeshAgent` as a
pathfinding oracle on `NPCGathering` (falls back to the old system
automatically, additive not a hard cutover); `NavMeshRebaker` hooked into
`PlayerBuilding.Confirm()` and `PlayerPieceUpgrade`'s upgrade/destroy
paths to keep the mesh current as players build; `NavMeshObstacle`
carving on both Door prefabs plus `Door.OpenForNPC()` so an NPC can open a
door in its way. Compile-verified only, not yet live-tested — this is the
actual repro that started the whole evaluation, so it's the one that
matters most to confirm next.

**Live-playtest fixes, same session:**
- Shared file-based debug logger (`DebugLog.cs`) — opt-in per-object
  logging (NPC jobs, Furnace, Campfire) to a plain text file, so a Claude
  session can read exactly what happened after a live test instead of
  Ben copy-pasting Console output.
- Fixed a spawned-then-placed `StorageBox` never actually saving —
  `PlayerBuilding.Confirm()`'s re-place branch assumed an existing
  instance already had a `PlacedPiece`/`SaveId`.
- `Furnace.QueueStatusText` — surfaces exactly why a queued smelting
  recipe isn't starting (missing ingredient, output full, waiting its
  turn) instead of a flat `[Queued]` label with no signal.
- `StorageBox` pick-up/re-place as a real permanent-placement round trip
  (implements `IEquippable`, new `PlayerBuilding.ArmExistingPiece` mode) —
  picking one up used to lose its custom name entirely.
- Fixed renaming a box also triggering pickup when the new name contained
  the letter "e" — the rename text field and the raw `eKey` interact read
  were two independent views of the same keypress; wired into the
  existing `SuppressInteraction` flag.

Compile-verified via full-project batch mode throughout. Most of tonight's
work is not yet live-tested — see `MVP2B_PLANNING.md`'s own status line and
`WORKING_ON.md`'s history for exactly which pieces still need a real
Play-mode pass.

## 2026-08-20 (3)

### v0.3.154-dev — Live-playtest bug-fixing pass: physics friction, NPC stuck-recovery, building-piece grounding, and a floor-collision layer gap

A long live-testing session surfaced a real cluster of physics/movement/
building bugs, most fixed same night, all verified against the actual data
rather than assumed. See `BUGS_AND_ENHANCEMENTS.md` for full root-cause
detail on each.

- **No friction anywhere, physics objects rolled/slid down hills.** Zero
  `PhysicMaterial` existed anywhere in the project. New
  `Assets/Data/HighFrictionGround.physicMaterial` (friction ~1.0,
  `frictionCombine = Maximum` so it dominates regardless of what any
  touching object's own collider has) applied to the Terrain's
  `TerrainCollider`. `BerryPickup`/`BerrySeedPickup` (the only two
  `SphereCollider` pickups — friction alone can't stop a rolling sphere)
  got `Rigidbody.constraints = FreezeRotation`. New shared
  `RigidbodySettler.cs` freezes any settled Rigidbody kinematic after
  ~1.5s of near-zero velocity — a defense-in-depth safety net against
  slope creep, batch-applied to all 132 Rigidbody-bearing prefabs.
- **NPC stuck-in-geometry recovery was too weak, and its fix then needed a
  fix of its own.** `NPCMovement.StuckTracker`'s old recovery just
  reversed the desired movement direction for one frame at normal
  `moveSpeed` — too weak to clear a real corner wedge (a Miner stuck at a
  wall/floor corner). First fix: hard-teleport the mover 4m in the escape
  direction once stuck. That fix then needed its own raycast validation
  after a live report of an NPC walking through a wall — the teleport now
  runs the same widening-angle search `FindClearDirection` already uses
  for normal deflection, checked against the full 4m bump distance, and
  clamps to whatever's actually clear.
- **StorageBox/Bookshelf/Desk silently sank into whatever they were
  placed on.** Root cause turned out unrelated to an early theory about
  Foundation-tier upgrades changing floor height (checked directly and
  ruled out — Foundation tiers are geometrically identical, and upgrading
  preserves exact position). Real cause: all three had a pivot-not-at-
  base model (same class of bug Furnace/Anvil hit earlier this session)
  with no `groundOffset` set at all. Fixed (`StorageBoxPiece.asset`
  0.25, `BookshelfPiece.asset` 0.9, `DeskPiece.asset` 0.4); a full audit
  of every other free-placed `BuildPiece` came back otherwise clean.
- **NPCs sank through built floors and could clip through walls near
  one.** `GroundHeight.Sample` (every NPC mover's per-frame Y sampling)
  only raycasts a dedicated "Ground" physics layer by design — but
  `Foundation.prefab`/`PlankFoundation.prefab` were on Default, not
  Ground, so an NPC crossing a built floor got sampled straight through
  to bare terrain below, sinking low enough to slip under a Wall's
  collider too. Both Foundation prefabs moved onto the Ground layer;
  Wall/Pole/Door/Roof deliberately left off, since they're not meant to
  be walked on top of.
- **Foundation-to-Foundation tiling snap required aiming almost exactly
  at the edge socket.** `snapRadius` bumped 1.5 → 2.5 (matches
  Foundation's own half-width) — aiming at the middle of an adjacent
  piece's face, the natural way to try tiling floors together, was
  previously outside the snap window entirely.
- **A placed Foundation went missing after a save/reload — investigated,
  not currently reproducible.** Confirmed via direct `save.json`
  inspection that the object was genuinely never captured (not
  misplaced), and the same session's 4 freshly-placed Foundations all
  saved correctly with real ids — likely a one-off predating some of
  this session's own earlier `SaveId` fixes. Root-caused a related
  live report (player clipping through the floor near that spot) as a
  direct, fully-explained symptom of the missing floor — measured a
  real 0.39–0.44 unit gap between bare terrain and the structures resting
  there. Resolution is placing a new Foundation there, not a code fix.
- **Investigated, no bug found**: the originally-reported Anvil/Furnace-
  sinking-into-a-Foundation issue. Foundation's collider matches its
  visual mesh exactly on both tiers, and Anvil/Furnace's stored
  `groundOffset` values match fresh measurements to 4 decimal places —
  the structures in the original report most likely predated the
  `groundOffset` fix from earlier this session.
- **Procedural tree entry closed** — `GenerateTree.cs` no longer exists
  in the repo (already cleaned up long ago); `TreeBark.mat` is still
  genuinely in use by the Log item, not orphaned. Nothing to delete.

Also written this session (design-only, nothing built): `HUNTER_PLANNING.md`
(a new Hunter NPC job — hunts passive prey only, scavenges any dead
creature including Wolves, no new combat-death risk), `COOKSTOVE_PLANNING.md`
(a Furnace-shaped automated cooking structure, gated to build on real
Cooking skill earned by hand at a Campfire first), and an NPC "find door"
pathing idea logged in `BUGS_AND_ENHANCEMENTS.md`.

All of the above is compile-verified only — not yet live-tested this
commit.

## 2026-08-20 (2)

### v0.3.153-dev — Craftable Anvil now uses the real Anvil model, not a Rock placeholder

Ben asked directly why the placeholder couldn't just be the real anvil
model already visible in the pre-placed scene — it turns out it could:
`Assets/Models/Anvil.glb` (a genuine Tripo3D-generated model, already
imported) was sitting right there the whole time, parented as a child of
the same `AnvilSurface` trigger object found in last night's build. Last
night's extraction used `.transform.root`, which walked past this child
entirely up to the "Scattered Boulders" ancestor — the actual mistake was
never looking one level too far, just the wrong direction.

**A second mistake surfaced fixing the first, caught before it shipped
wrong**: the natural fix, `FindFirstObjectByType<AnvilSurface>()`, isn't
reliable in this scene — `Boulder.prefab` (confirmed the night before)
bakes an `AnvilSurface` component into *every* scattered plain-Rock
instance across the whole map, not just the one real functional Anvil,
so "first found" can return any of ~24 scattered Boulders. It did:
the first rebuild attempt produced a prefab whose mesh guid resolved to
`Rock_Quaternius.glb`, not `Anvil.glb` — caught by checking the actual
mesh reference directly rather than trusting the script's own success
log. Fixed by disambiguating on the real Anvil's known exact position
(confirmed directly against the scene file) instead of "whichever one
enumerates first."

The rebuilt `AnvilPiece.prefab` now correctly references `Anvil.glb`
(verified via mesh guid, and by actually reading the freshly-baked icon —
a real anvil on a wood stump, not a rock). Re-measured and reapplied
`groundOffset` for the new model (0.378, down from the Boulder
placeholder's 1.169 — a real, purpose-built prop needs much less
correction than a generic rock did). Recipe/ingredients unchanged.

Compile-verified via full-project batch mode, every step verified
directly (mesh guid, icon render, `groundOffset` value) rather than
trusted from log output alone. Not yet live-tested.

## 2026-08-20

### v0.3.152-dev — Player-built Anvil and Furnace were sinking into the terrain

Found live: a player-built Furnace looked much smaller than the original
fixed one. Measured directly (per `CLAUDE.md`'s own model-grounding
protocol — `Renderer.bounds` vs. the prefab's own pivot, not guessed) —
the reused Furnace model's pivot sits at its vertical *center*, a full
world unit above its actual base. `PlayerBuilding`'s free-placement code
plants a piece's pivot directly at the raycast ground-hit point with no
correction, so the model sank a full unit into the terrain, leaving only
the top half visible — reading as "too small" rather than "half buried."
The original fixed scene furnace never showed this, because whoever
placed it originally corrected for the same gap by hand (`m_LocalPosition
.y: 1.36` baked into the scene, confirmed directly).

Checked the Anvil placeholder the same way while the tooling already
existed — same issue, worse (1.17 units of gap): `Boulder.prefab`'s own
scatter-placement script apparently already corrects for this when
scattering ordinary Boulders, but a `BuildPiece`-placed Anvil never goes
through that path.

Fixed generally, not per-piece: new `BuildPiece.groundOffset` field
(opt-in, 0 default, same "most pieces don't need this" shape as
`groundReach`), added onto the ground-hit Y position during free
placement. Set to the measured value on both `FurnaceBuildPiece.asset`
(1.0) and `AnvilBuildPiece.asset` (1.1689) — reusable for any future
piece built from a reused/extracted model with the same pivot mismatch.

Compile-verified via full-project batch mode, both offsets verified
directly in the saved asset files. Not yet live-tested.

## 2026-08-19 (8)

### v0.3.151-dev — Dropping a stack of Logs only ever spawned one choppable node

A real gap in the previous entry's own fix, confirmed live: breaking 5
dropped Logs only ever yielded 2 Planks total (one choppable node's
worth), not 5 nodes' worth — `PlayerDropping.SpawnPickup` spawned exactly
one instance of the `worldPickupPrefab` and only ever applied `count` via
`Pickup.Configure`, which `Log.prefab` (a `ResourceNode`, not a `Pickup`)
doesn't have — so the dropped quantity was silently discarded down to a
single node regardless of how many were actually dropped.

Fixed generally, not Log-specifically: when the spawned prefab isn't a
stack-representable `Pickup`, `SpawnPickup` now spawns the remaining
`count - 1` as separate instances, scattered the same way
`ChoppableTree.Complete()` already scatters a felled tree's own logs.
Applies to any future item in the same situation, not hardcoded to Log —
and since `SpawnPickup` is shared by both `PlayerDropping.DropFrom` and
`AdminSpawnScreen`'s dev spawn tool, this also fixes Admin-spawning
multiple Logs at once for free. `Log.prefab` already has a `Rigidbody`
(confirmed), so the scattered instances settle apart from each other via
physics without any extra grounding logic needed.

Compile-verified via full-project batch mode. Not yet live-tested.

## 2026-08-19 (7)

### v0.3.150-dev — A dropped Log is choppable again

Found live: dropping a carried Log spawned a plain, inert `Pickup`
(`LogPickup.prefab`) with no chop mechanic at all — you could pick it
back up, but not chop it in place, unlike a Log a felled Tree produces.
Not a bug (working exactly as originally built), but Ben's ask for
consistency: a Log should behave the same regardless of how it ended up
on the ground. Fixed with a one-line swap — `Log.asset.worldPickupPrefab`
now points at the real choppable `Log.prefab` (`ResourceNode`) instead of
`LogPickup.prefab`. Confirmed safe first: `PlayerDropping.SpawnPickup`
already gracefully handles a prefab with no `Pickup` component (skips
quantity configuration rather than erroring), so this doesn't preserve
exact dropped quantity as multiple choppable nodes — dropping a stack of
5 spawns one choppable Log, same as dropping 1 — but that's consistent
with how felling a Tree already works (a fixed `logCount` per tree, not
scaled to anything) and directly resolves the actual inconsistency Ben
flagged.

This makes `LogToPlankRecipe.asset` (built earlier the same session to fix
Log's dead-end-item bug) redundant — Ben's call: removed it entirely
(asset deleted, `PlayerCrafting.recipes` registration removed, verified
0 remaining scene references) rather than leave two ways to get Planks
from a Log sitting around. Drop-and-chop is strictly better anyway (same
Plank output, plus a Stick chance the plain recipe never had).

Compile-verified via full-project batch mode, verified directly in the
saved asset file. Not yet live-tested.

## 2026-08-19 (6)

### v0.3.149-dev — Only the nearest of multiple nearby StorageBoxes was ever shown

Found live: two StorageBoxes placed next to each other made the second
one completely inaccessible. `InventoryScreen`'s "nearby StorageBox"
section already computes `nearbyStorages` as a full, distance-sorted list
of every box in range (`StorageBox.FindNearby`) — the display code just
only ever indexed `nearbyStorages[0]`, discarding the rest. Fixed by
drawing a section for every box in the list instead of just the first —
reuses the same `DrawContainerContents` call already proven safe to
invoke multiple times per frame (the worn-containers section already does
this for Back/Waist/Chest/Leg simultaneously). Also live-confirmed the
same session: the restore-order fix from the previous entry actually
works — a renamed "ore box" correctly kept its name across a real save/
reload (inventory contents not separately re-checked, but very likely
fine given both were failing via the exact same mechanism).

Compile-verified via full-project batch mode. Not yet live-tested.

## 2026-08-19 (5)

### v0.3.148-dev — ChoppableTree stuck bug fixed, two real save/load regressions root-caused

Three real fixes, all found via the same playtest session, two of them
genuinely serious:

- **`ChoppableTree` stuck-with-no-animation bug, fully fixed.** The
  `[TreeStuckDiagnostic]` logging from the previous entry paid off
  immediately — live numbers confirmed a consistent, deterministic offset
  on every tree instance tested (`pivotDistance=3.99, harvestRange=3.00,
  colliderSurfaceDistance=0.00`): an NPC could be physically touching a
  tree's collider and the game would still think it was a meter too far
  away, because `NPCGathering`'s approach check measured distance to the
  tree's transform pivot, not its actual collider surface. Fixed by
  measuring against `Collider.ClosestPoint` for `ChoppableTree` targets
  specifically (the one type with this mismatch — `ResourceNode`/
  `StorageBox` don't have it), reusing the exact same measurement the
  diagnostic itself already computed. Diagnostic logging removed.
- **A real equipment-restore bug, found via a save file comparison, not
  guesswork.** Ben reported a Canteen losing its fill and a Hammer
  disappearing from a worn pair of Jeans' own pocket — both after a real
  save/reload. Direct inspection of the actual `save.json` proved the
  *capture* side was already 100% correct (`"Leg": [{"item":
  "SettlersJeans", "equipment": {"nested": [{"item":
  "MasterworkHammer", ...}]}}]`, and the Belt's Canteen correctly showing
  `"liquid": "Water", "amount": 100.0`) — so the bug was entirely on
  restore. Root cause: `Inventory.AddEquipmentItem` silently returns
  `false` when a slot is already at capacity, and `PlayerEquipment`'s
  body slots (Leg, Waist, ...) start pre-occupied by the scene's own
  baked-in default "Settlers" starting gear — so restoring the *real*
  saved Jeans/Belt into an already-full slot was silently discarded,
  leaving the scene's empty default gear in place. Confirmed by a real
  counter-example: the Chest slot's Shirt (with Rations inside) restored
  correctly, because — unlike Jeans/Belt/Sneakers, which are baked
  directly into the scene with no guard — the Shirt uses a runtime
  `Start()` auto-equip with an "already equipped?" check, which loses the
  race safely if `SaveManager.Load()` runs first. Fixed at the root:
  `InventorySaveUtility.Restore` now calls a new `Inventory.Clear()`
  (destroying any equipment GameObject already occupying a slot) before
  restoring — a restore now always produces exactly the saved state
  regardless of what the inventory already held, protecting every call
  site (player inventory, every equipment slot, NPCCargo, StorageBox) at
  once rather than requiring each caller to remember to clear first.
- **A restore-ordering bug affecting every player-built StorageBox (and
  latently, GardenPlot/GardenPlot4x4).** Ben found a StorageBox he'd
  named and stocked before saving came back after reload with a generic
  name and empty — a serious regression in what used to be one of the
  most solid parts of the save system. Root cause: `SaveManager.Load()`
  called `RestoreWorldObjects<StorageBox>` (find-by-SaveId, apply saved
  inventory/name) *before* `RestorePlacedPieces` (which actually
  recreates a player-built structure that doesn't pre-exist in a fresh
  scene load). For a StorageBox already sitting in the scene, that order
  doesn't matter — it's already there to find. For one built during
  play, it doesn't exist yet at that point, so the lookup silently fails
  and the real saved state is never applied; `RestorePlacedPieces` then
  recreates a bare, empty, default-named copy with nothing to backfill
  it. `Furnace`'s own restore call already ran in the correct order
  (after `RestorePlacedPieces`) — that's exactly why Furnace's own richer
  state-saving worked when it was tested; every other world-object
  restore call is now reordered to match.

Compile-verified via full-project batch mode. Not yet live-tested — all
three need a real save/reload pass to confirm.

## 2026-08-19 (4)

### v0.3.147-dev — Craftable Anvil and Furnace

Closes the highest-priority gap found during the new-player-experience
playtest — neither Anvil nor Furnace had any way for a player to build
one; both existed only as the single fixed fixture already in
`TestScene.unity`. Real recipes, deliberately never requiring Nail or
Ingot directly for either piece (both need to be craftable from raw
gathered materials alone) — with one exception worked out with Ben:
Furnace's recipe *can* safely use Nail, because `NailRecipe.asset` (only
just checked directly, not assumed) requires an Anvil surface but its
only ingredient is raw Iron ore, no Ingot — so as long as Anvil is built
first, Nails become legitimately reachable before Furnace ever needs
them.

- **Furnace** — 8 Nail + 6 Small Rock + 4 Plank, trains Metalworking. A
  clean duplicate of the existing fixed fixture (real model, real
  `Furnace`/`FurnaceScreen`/`FurnaceSurface` behavior carried over
  intact), now wrapped in a real `BuildPiece` + baked icon.
- **Anvil** — 6 Small Rock + 2 Plank + 2 Iron, trains Metalworking. Hit a
  real snag building this one: the scene's actual "Anvil" object turned
  out to have **no visual mesh of its own at all** — just an invisible
  `AnvilSurface` trigger volume, apparently parented under the "Scattered
  Boulders" container for convenience. A first attempt duplicated that
  whole container by mistake (214 objects, ~71 boulders) before this was
  caught via a direct file check and fully cleaned back out. Resolved
  once `Boulder.prefab` itself turned out to already carry both
  `ResourceNode` *and* `AnvilSurface` on the same object (a shared
  template) — built the placeable Anvil from that directly (strip
  `ResourceNode` so it isn't also mineable, keep `AnvilSurface`, add
  `PlacedPiece`), per Ben's explicit call to reuse an existing prop as a
  placeholder rather than spend a Tripo3D generation on a real model
  right now. Reads as a plain stone slab, not a recognizable anvil shape
  — a real model is still a worthwhile future swap, logged as a known
  placeholder, not a finished asset.

Both pieces registered in `PlayerBuilding.allPieces` (a hand-maintained
scene array, not database-driven — checked directly, not assumed) and
`BuildPieceDatabase` (for `SaveManager`'s save/restore lookup). Every
step verified directly in the saved files, including one case where a
script's own log message ("AnvilSurface kept: False") turned out to be
flat wrong — the actual saved prefab had the component correctly, a
timing quirk in the log check itself, not a real problem. Compile-
verified via full-project batch mode. Not yet live-tested.

## 2026-08-19 (3)

### v0.3.146-dev — Live-testing fixes: dead-end Log item, Small Rock mystery root-caused, Fiber Backpack, Stick supply

A full new-player-experience live-testing pass (build a structure, place
Furnace/Anvil/StorageBoxes, hire and assign NPCs, cook) surfaced several
real bugs, fixed the well-understood ones, and root-caused one that
looked mysterious:

- **Crude Fiber Backpack rejected as an NPC tool** — a third Backpack
  family (distinct from the original ladder and the Leather ladder),
  never added to any of the 4 jobs' Backpack tool-acceptance lists. Same
  registration-gap shape as the already-fixed Leather Backpack bug, just
  a different item slipping through the same net. Fixed by adding its
  guid to `MineOreJob`/`ChopWoodJob`/`ForageJob`/`MetalworkingJob`.
- **The "Log" item was a genuine dead end** — a Woodworking NPC felling a
  standing Tree yields a raw Log straight to cargo, but zero
  `CraftingRecipe` anywhere consumed it (confirmed via direct guid
  search) — once collected, useless to anyone, player or NPC. Fixed with
  a new `LogToPlankRecipe.asset` (1 Log → 2 Plank, trains Woodworking,
  no station required), mirroring the guaranteed portion of what
  breaking a placed Log node already yields — deliberately without the
  bonus-Stick chance, which stays a placed-Log-only mechanic since this
  recipe is reachable by both players and NPCCrafting.
- **Stick supply bumped** — `Log.prefab`'s `bonusChunkChance` raised
  0.3 → 0.6. Ben's framing: Sticks are a critical early-game bottleneck
  (Rope, Backpacks) and were too scarce.
- **Small Rock mystery, fully root-caused, not just theorized.** A
  brand-new Woodworking-only NPC turned up with 8 Small Rock in cargo,
  no explanation. Found by reading `NPCGathering.FindTarget()` directly:
  its scan of the `ResourceNode` pool has zero job-kind gating, unlike
  the Bush pool (`searchesBushes`) and loose-pickup pool
  (`collectLoosePickups`), which both got that exact gate after nearly
  identical past bugs. `ConsiderHarvestable`'s tool check only applies
  *if* a target declares `RequiredTools` — and `Boulder.prefab` (the
  plain Rock/Small Rock node, distinct from the tool-gated ore Boulders)
  has `requiredTools: []`, completely empty, so the check never
  triggered and any job could harvest it purely on distance. This also
  quietly falsifies a comment already in the codebase claiming the
  Harvestable pool was "naturally segregated by RequiredTools" — true
  for every ore Boulder, false for plain Rock. Fixed with a new
  `NPCJobDefinition.harvestsToollessRock` field, same shape as
  `searchesBushes`/`collectLoosePickups`, gating toolless targets
  specifically (ore Boulders, which all declare real tools, are
  unaffected) — only `MineOreJob` sets it true.
- **Temporary `[TreeStuckDiagnostic]` logging added** to
  `NPCGathering.cs` for a still-unsolved bug: an NPC getting stuck at a
  standing `ChoppableTree` with no animation for 60+ seconds (reproduced
  twice), while chopping a placed Log works cleanly for the same NPC.
  Working theory (Ben's): a collider/harvest-range mismatch specific to
  `ChoppableTree`. Logs pivot-distance vs. `harvestRange` vs. actual
  collider-surface distance, throttled to once per 2s, whenever an NPC
  is stuck approaching a Tree — will confirm or refute the theory on the
  next live occurrence. Not yet removed; remove once root-caused.

Compile-verified (multiple full-project batch-mode checks) and every
change verified directly in the saved asset/scene files, not just
trusted from script logs. Not yet live-tested.

## 2026-08-19 (2)

## 2026-08-19 (2)

### v0.3.145-dev — Multiplayer Phase 0: infra spike built

`MULTIPLAYER_PLANNING.md`'s long-open Phase 1 proposal ("bare
NetworkManager, two instances seeing each other move, no gameplay
touched") built for real, prompted by a critical re-audit: Mirror had sat
completely untouched for 6 days since import (zero `using Mirror`
references, zero commits besides the import) while the single-player
codebase it'll eventually need to convert grew ~50% in that same window
(115→177 scripts, 32→48 `PlayerXXX.cs`, 12→27 NPC scripts). Persistence
also shipped in that window (`SaveManager`, now wired into 28 files) in a
single-player-shaped way — one omnibus JSON file, no per-player keying —
which is a real new complication for a future conversion, not present in
the original audit.

Deliberately isolated from the real game to keep collision risk with
concurrent single-player work at zero: `Assets/Scenes/NetworkSpike.unity`
(a throwaway scene, **not added to `EditorBuildSettings`'s scene list**,
so it can't interfere with `SceneAutoOpen.cs`'s single-scene convention or
accidentally ship) plus `Assets/Prefabs/NetworkSpikePlayer.prefab` — a
plain capsule with `NetworkIdentity` + `NetworkTransformReliable`
(`syncDirection: ClientToServer`, the client-authoritative baseline the
planning doc's open movement-authority question is weighing against a
server-authoritative alternative) + a new `NetworkSpikeMovement.cs`
(WASD/turn, gated on `isLocalPlayer`, reads the New Input System directly
— deliberately not `FirstPersonController`, so none of the 48
`PlayerXXX.cs` scripts are touched by this pass). The scene's
`NetworkManager` GameObject carries `KcpTransport` (Mirror's bundled
default transport) and `NetworkManagerHUD` (Mirror's own manual-test UI —
Host/Client/Server buttons at runtime, no custom UI needed for this
spike). Compile-verified via batch mode (zero `CS####` errors); scene/
prefab YAML verified directly (`playerPrefab` reference resolves to the
correct fileID, `syncDirection` serialized correctly). **Not yet
live-tested with two actual connected processes** — that's a manual step
for Ben (temporarily add `NetworkSpike.unity` to Build Settings, build a
standalone Player as one client, run the Editor as Host via the HUD for
the other) since it needs two real running processes, not something a
batch-mode script can validate.

## 2026-08-19

### v0.3.144-dev — Shared NPC obstacle-avoidance + stuck-recovery

Closes out `BUGS_AND_ENHANCEMENTS.md`'s most-confirmed-live open bug: a weak
single-normal-based obstacle deflection was duplicated, unfixed, in
`NPCGathering`/`NPCCrafting`/`NPCTraining`/`NPCGuarding`'s own `MoveToward`
(each could point straight into a second obstacle at a corner and stall an
NPC permanently — confirmed live 3x+ against a Boulder). `NPCSeekFlag`
already had the real fix (a widening left/right angle search, built
2026-08-16) but it stayed a one-off rather than getting shared.

New `Assets/Scripts/NPCMovement.cs` — a plain static helper (same
"static class, read by whoever needs it" shape as `GroundHeight.cs`):

- **`FindClearDirection`** — `NPCSeekFlag`'s widening-search algorithm,
  generalized with the `ignoreTarget` parameter the other 4 scripts need
  (so a mover's own destination object doesn't count as "blocked" once
  close enough to hit it). All 5 scripts' `MoveToward` now call this one
  implementation instead of their own copy — each script keeps its own
  ground-sampling/facing/`moveSpeed`/`obstacleCheckDistance` untouched, only
  the deflection block was pulled out.
- **`StuckTracker`** — a small plain class, one instance per mover. Checks
  every ~2s whether the NPC has covered a minimum distance since the last
  check; after 3 consecutive slow intervals (~6s of near-zero net
  progress), the next move gets a hard reverse shove instead of the normal
  probe, then resets. A physical escape hatch, not a per-script "abandon
  target and re-plan" policy — each job component already re-evaluates its
  target on its own cadence, so freeing the NPC physically is enough.

Mitigates (doesn't formally close) the separately-logged `NPCSeekFlag`
no-timeout-while-approaching gap — it can no longer wedge forever, but
there's still no hard timeout backstop for a genuinely escape-proof pocket.
Compile-verified via batch mode (zero `CS####` errors); not yet
live-tested.

## 2026-08-18 (18)

### v0.3.143-dev — Iron Arrow recipe registration, Furnace QTY labels, diagnostic cleanup

Three fixes from a live-testing session that put v0.3.142-dev's Iron
Arrow build through its paces:

- **Iron Arrow's 6 new recipes never appeared in Crafting at all** —
  `PlayerCrafting.recipes` is a hand-maintained array on the Player
  object in `TestScene.unity`, not a dynamic scan, and the recipes were
  never added to it. Fixed via a throwaway batch script, verified by
  grepping the scene for all 6 recipe guids directly.
- **`FurnaceScreen` showed no stack-count label** for any item with a
  baked icon (Iron Ore, Iron Ingot, ...) — same root cause as the
  already-fixed `CampfireScreen` gap, just never applied here. Same fix.
- Pulled the leftover `[MinerStuckDiagnostic]` logging and its 4 backing
  fields from `NPCGathering.cs` — the oscillation fix it was tracking has
  now been live-confirmed durable across two separate sessions.

See `BUGS_AND_ENHANCEMENTS.md` for full detail. Compile-verified; not yet
live-confirmed.

## 2026-08-18 (17)

### v0.3.142-dev — Iron Arrow: a stronger, Iron-Ingot-based Stone Arrow counterpart

Ben's ask, evaluated critically before building (see
`BUGS_AND_ENHANCEMENTS.md` for the full "be mean" critique) rather than
built to the literal spec — a flat ×2 on the existing damage table would
have left Crude Iron identical to Crude Stone (the table starts at 0) and
broken the tier system's "Masterwork means the same relative power
everywhere" promise. Shipped instead: Iron beats Stone at every tier
(0/1/2/4/6 → **2/3/4.5/7/9.5**, ~58% higher ceiling, not 100%).

- `IronArrowheadRecipe` (1 Iron Ingot → 2 Iron Arrowhead, Metalworking,
  requires the Anvil) + 5 assembly recipes (1 Iron Arrowhead + 1
  tier-matched Trimmed Stick → 5 arrows, Woodworking) — same shape as the
  existing Stone Arrow family.
- New `ItemDefinition.arrowDamageBonus` (sentinel `-1` = use the shared
  `CraftTierScale.ArrowDamageBonus(tier)` table) lets a different arrow
  material deal different damage at the same `CraftTier`, without a
  second material-keyed table on `CraftTierScale` itself.
  `PlayerRangedCombat`/`NPCGuarding` both read the new
  `ItemDefinition.EffectiveArrowDamageBonus` now.
- Visually distinct, not just a reskin-by-name: reused Stone Arrow's
  shared geometry (all 5 Stone tiers already share one mesh) with the
  Tip/Arrowhead submaterial swapped to a new metallic
  `IronArrowheadMetal.mat` — verified by reading the baked icon PNGs
  directly, not just trusting the batch log.
- `GuardRangedJob`'s hardcoded Arrow `acceptableItems` list updated with
  the 5 new guids (the exact "Leather Backpack silently rejected as an
  NPC tool" gotcha shape, caught before it recurred) — a Guard can be
  handed either Stone or Iron Arrows now.

Compile-verified; not yet live-confirmed.

## 2026-08-18 (16)

### v0.3.141-dev — Player Map blank-screen root cause fixed; audio system reclassified

`PlayerMapExploration`'s fog-of-war grid (`revealed`/`gridWidth`/
`gridHeight`/`worldBounds`) lives in plain (non-`[SerializeField]`)
fields, populated only in `Awake()`. A mid-Play-mode domain reload (the
exact hazard this session already confirmed elsewhere) resets those
fields without `Awake()` running again, and `MapScreen.EnsureTexture()`
silently built a 0×0 `Texture2D` from the zeroed dimensions instead of
throwing — no error, just a blank Map. Fixed with a new
`EnsureInitialized()` lazily called from every public entry point on
`PlayerMapExploration`, not just `Awake()` — the Map now self-heals
instead of rendering blank if its backing state ever goes missing, for
any reason. See `BUGS_AND_ENHANCEMENTS.md` for the full story.
Compile-verified; not yet live-confirmed (the trigger case is one the
project has since agreed to avoid entirely, so there's no cheap way to
force a repro).

Also moved the "Gameplay audio system" entry from Bugs to Enhancements —
it was never a regression, just a system that hasn't been built yet.
Doc-only, no code change.

## 2026-08-18 (15)

### v0.3.140-dev — Gable-end roof geometry fixed on both Rectangular House prefabs

`RectangularHouseTwig`/`RectangularHousePlank` capped their short (gable)
ends with a `RoofPanel` rotated 90° sideways instead of a real vertical
gable infill — the sloped panel poked through past the ridge instead of
closing the triangular gap. Turned out the real fix already existed and
was just never used: a proper Gable Panel piece
(`TwigGablePanelPiece`/`PlankGablePiece`, full `BuildPiece` + model +
recipe + icons) was built at some earlier point but never placed into
either pre-built house. Swapped the 2 misapplied roof-panel instances per
prefab for the correct Gable Panel at the identical `WallTop` socket
transform via a throwaway batch-mode script
(`Assets/Editor/FixGableEnds.cs`, deleted after running). Verified by
grepping both saved prefabs' YAML for the new piece's guid at the
expected position/rotation. See `BUGS_AND_ENHANCEMENTS.md` for the full
story. Compile-verified; not yet live-confirmed in Play mode.

## 2026-08-18 (14)

### v0.3.139-dev — Bug-list clearing pass: action popup, worn Canteen, item weights

A batch pass through the open bugs list, split into what actually needed
fixing vs. what turned out to already be fine or was previously
deliberately deferred:

- **`InventoryScreen`'s action popup lost clicks to the slot underneath
  it** — `HandleSlotEvents` now no-ops entirely while `pendingActionItem
  != null`, so the grid stops competing for input with the popup drawn
  on top of it.
- **A Canteen clipped to a worn Belt was invisible to both
  `requiresCanteenWater` checks** (`PlayerCrafting.FindEquippedCanteen`
  and `Campfire.FindPlayerCanteen`) — both only ever checked hands. Both
  now also check the worn Belt's own `Inventory` for a clipped Canteen,
  same relationship `PlayerBelt.DropClippedEquipment` already accounts
  for elsewhere.
- **24 `ItemDefinition`s given real, deliberate `weight` values**
  (raw/refined materials, the Leather Backpack ladder, standalone gear,
  wearable gadgets, Soccer Ball) — the original 2026-08-10 backlog
  artifact turned out to be partly stale (Stick/Plank/the Trimmed Stick
  ladder/Rock were already tuned since), re-verified each item's actual
  current state before touching anything.
- **`LeatherBackpackRecipe.asset`'s placeholder ingredients (6 Cloth + 4
  Rope) swapped for real materials** (4 Leather + 2 Rope) — Leather has
  existed as a real, obtainable item (Deer kills) since 2026-08-15, and
  was specifically waiting on this recipe to be updated.

**Investigated but not touched, found to need more than a quick fix:**
Bow Release animation always returning to StandingIdle needs a real
Animator Controller rework (masked layer or per-stance transitions), not
safe to do blind without live iteration. Both open `IconBaker` icon
entries (`TwigGablePanelPieceIcon` tiny/off-center, the Plank-tier
color/lighting trade-off) turn out to already be investigated dead ends
— both were shipped as-is per Ben's own explicit call after real
troubleshooting, not oversights; re-attempting either blind would just
repeat abandoned guesswork. `WovenGrassCloth.mat`'s near-black-metallic
concern checked directly against the rendered icon — renders correctly,
closed as a non-issue.

Compile-verified via batch-mode Unity (0 errors). Not yet live-tested.

## 2026-08-18 (13)

### v0.3.138-dev — Fix: NPC screens didn't pause the NPC; tool-giving ignored worn containers

Two more real bugs found live back-to-back:

- **Managing an NPC via `NPCHiringScreen`/`NPCJobScreen` never paused it**
  — only `Talk` (via `NPCDialogue`) did. Ben: "walked up, talked, and the
  npc still moved" while the Assign Job menu was open. Added
  `NPCHiring.SetMovementPaused(bool)`, mirroring `NPCDialogue.BeginDialogue`/
  `EndDialogue`'s exact four-component pause pattern
  (`NPCWander`/`NPCGathering`/`NPCCrafting`/`NPCGuarding`) — not routed
  through `NPCFreeze`, since that toggle represents a deliberate player
  choice a temporary UI-open pause must not silently clear on close. Both
  screens now pause on open, unpause on close.
- **NPC tool-giving only ever checked the player's main inventory** — a
  Pickaxe/Backpack genuinely being carried inside a worn Backpack (the
  normal way to carry more than a handful of items) always read "0 in
  inventory," a known gap logged since 2026-08-17. New
  `PlayerCarriedItems.cs` (mirrors `InventoryScreen.GetWornContainers()`'s
  exact slot list/`IInventoryHolder` lookup) adds `GetTotalCount`/
  `RemoveOne`, checking the main inventory first, then every worn
  container. `NPCJob.TryGiveTool`/`SwapTool` and `NPCJobScreen`'s own
  "have N" display all route through it now.

Compile-verified via batch-mode Unity (0 errors). Not yet live-tested —
Editor was closed for this pass.

## 2026-08-18 (12)

### v0.3.137-dev — FIXED: the Miner position-oscillation mystery, for real this time

Root cause found by fully enumerating every component on the NPC prefab
rather than continuing to test individual movement-system theories:
`NPCGathering`/`NPCCrafting`/`NPCGuarding` all live permanently on every
NPC prefab (the established "bail early if wrong job kind" convention)
and all run their own `Update()` every frame regardless of which job is
actually assigned. Each one's `!ready` branch called
`wander.SetPaused(false)` **unconditionally on every idle frame**, not
just when genuinely releasing a pause it held — so for a Mining-job NPC,
`NPCCrafting`'s and `NPCGuarding`'s own `!ready` branches were both
independently calling `SetPaused(false)` every single frame, racing
against `NPCGathering`'s own `SetPaused(true)` with no defined winner
(Unity doesn't guarantee `Update()` order between sibling components).
On whichever frames the "wrong kind" component happened to run after the
active one, `NPCWander`'s own independent wander-target-seeking silently
took over movement for a frame before the active job component reclaimed
control next frame — exactly matching the small, semi-consistent drift
chased across the last several passes.

Fixed in all three components with a `wasActive` flag: each only calls
`wander.SetPaused(false)` on a genuine active→inactive transition, never
on every idle frame. `NPCTraining`/`NPCSeekFlag`/`NPCFlee` were checked
and confirmed to not have this pattern already. Also added a
belt-and-suspenders safeguard (Ben's idea) in `NPCGathering`'s Harvesting
branch: the position is snapshotted the instant the NPC settles into
range and forcibly re-asserted every frame while harvesting, guaranteeing
zero visible drift regardless of what (if anything) else ever touches
this transform in the future.

**Live-confirmed immediately** — clean single MOVING→HARVESTING
transitions per target, no oscillation, held the full harvest duration,
correctly moved on to a new ore node afterward. This closes out the
entire Miner-stuck saga from tonight (bush-targeting, harvestRange
mitigation, and now the actual root cause) and very likely explains
every prior "NPC seems stuck/frozen" report across the whole project,
not just Mining — `NPCCrafting`/`NPCGuarding` had the identical bug for
their own respective off-duty NPCs.

## 2026-08-18 (11)

### v0.3.136-dev — Round-2 diagnostic logging for the still-unexplained Miner drift

Live-testing v0.3.135-dev's `harvestRange` widening (2m → 3m) found the
oscillation just re-centered exactly on the new boundary rather than
disappearing — direct evidence this is a real logic-anchored bug, not
ambient noise around wherever the NPC happens to settle. Also live-ruled-
out this session: `job.IsReady` flicker (reads a stable dictionary, no
flicker candidate) and a second interleaved NPC sharing the same generic
clone name (confirmed via both the Roster and the Map — only one Miner
exists, assigned and nearby).

Every quick theory now exhausted (Apply Root Motion, Rigidbody physics,
`CharacterController`, obstacle deflection, job-family mistargeting,
`job.IsReady` flicker, duplicate NPCs), so `NPCGathering.cs` gained a
second, more targeted diagnostic: logs unconditionally whenever
`transform.position` differs between the top of one frame and the top of
the next *while the component itself wasn't the one moving it*
(`lastWasMoving == false`) — narrowed to just the anomalous case so it
doesn't drown in normal per-frame movement noise during an actual
approach. This should directly answer whether something outside
`NPCGathering.Update()` entirely is responsible, since the component's
own Harvesting branch only ever calls `FaceToward` (rotation-only).

Compile-verified via batch-mode Unity (0 errors). Needs a live session to
read the output — next step for this specific mystery.

## 2026-08-18 (10)

### v0.3.135-dev — Mitigate the Miner position-oscillation bug: harvestRange 2m → 3m

With the wrong-target bug fixed (v0.3.134-dev) and root motion/physics/
`CharacterController` all ruled out live, the underlying position drift
itself (~0.1m each move↔harvest transition) is still unexplained — but
Ben's fix doesn't need the mechanism understood: `harvestRange` bumped
2m → 3m gives a full extra meter of margin, comfortably absorbing drift
of that size regardless of cause. Checked for a stale prefab override
before just touching the C# default (same gotcha as `workDurationSeconds`
two passes ago) and found one — `NPCFactoryWorker.prefab` had
`harvestRange: 2` baked in, so both the code default and the prefab
value were updated together. Confirmed no separate override exists on
`NPCFactoryWorkerMale`/`Female` or in `TestScene.unity`.

Compile-verified via batch-mode Unity (0 errors). Not yet live-tested —
Editor was closed for this pass.

## 2026-08-18 (9)

### v0.3.134-dev — Fix: Mining/Woodworking NPCs could target bushes meant for Forage

Live-testing the `[MinerStuckDiagnostic]` logging from the last pass ruled
out both leading theories (Apply Root Motion confirmed enabled on the NPC
Animator but disabling it live changed nothing; no Rigidbody exists on
either the NPC or `HerbBush`, ruling out physics push-back) but surfaced
the real underlying issue: a Mining-job NPC walked straight past ore to
reach the nearest HerbBush, then tried to play its Mining swing animation
on it. Root cause — `NPCGathering.FindTarget()`'s `INPCSearchable` pool
(BerryBush/HerbBush) has no tool requirement at all (searching is
bare-handed), so unlike the `INPCHarvestable` pool (naturally segregated
by RequiredTools — a Miner's Pickaxe can't satisfy a Tree's Axe
requirement), nothing stopped *any* Gathering-kind job from freely
targeting a bush purely on distance. Exact same shape as the
already-fixed `collectLoosePickups` gap from 2026-08-13 (a Mine Ore NPC
"stuck gathering sticks").

Fixed with the same pattern: `NPCJobDefinition` gained a
`searchesBushes` bool (default false), gating the Searchable pool scan in
`FindTarget()`. Only `ForageJob.asset` sets it true; `MineOreJob`/
`ChopWoodJob` correctly have no override. This should also eliminate the
specific repro that drove the position-oscillation investigation, though
the underlying distance-flip-flop mechanism itself remains technically
unexplained — `[MinerStuckDiagnostic]` logging left in place in case it
recurs with a legitimate Forage NPC targeting a real bush.

Compile-verified via batch-mode Unity (0 errors). Not yet live-tested —
Editor was closed for this pass.

## 2026-08-18 (8)

### v0.3.133-dev — Diagnostic logging for the Miner-stuck-cycling-animations bug

Re-adds targeted logging for the top-priority backlog bug (weak
single-deflection obstacle avoidance), this time aimed at a more specific
symptom Ben reported live: a Miner near a Boulder cycling between move and
mining animations rather than being fully frozen. Theory: `IsActingOnTarget`
(what drives the animation split) flips purely on straight-line distance
to the target crossing `harvestRange` — if an obstacle sits between the
NPC and its target, `MoveToward`'s deflection could plausibly nudge
distance back and forth across that boundary without ever actually
routing around the obstacle, producing exactly this flicker.

`NPCGathering.cs` now logs (not throttled — the point is catching a fast
flip-flop) every time it crosses the move↔harvest boundary, with the
live distance/target/position, plus a throttled (2/sec) log whenever
`MoveToward`'s raycast actually detects an obstacle, including which
collider, the hit point, and the resulting deflection vector. Next live
session near a stuck Miner should make the actual mechanism obvious.

Compile-verified via batch-mode Unity (0 errors). Not yet live-tested —
Editor was closed for this pass.

## 2026-08-18 (7)

### v0.3.132-dev — Fix: Guard stayed locked onto a threat forever after killing it

Solves the real Guard-stuck mystery, found via the `[GuardDiagnostic]`
logging added last pass: the Guard was never broken at all — it was
correctly in `Attacking` state, having actually found and killed a Wolf
(confirmed live: the Wolf was lying dead nearby, then manually skinned by
Ben). The bug is what happens *after* a kill: `ThreatStillValid()` only
ever checked distance, never whether the creature was still alive, and a
killed creature's `GameObject` is never destroyed —
`SkinnableCreature.Complete()` just `SetVisible(false)`s it and schedules
a much-later `Respawn()` — so `currentThreat` never actually went null.
The Guard stayed locked in `Attacking`, futilely trying to re-damage a
corpse it could never touch again (`TakeDamage` early-returns once
`isDead`), never returning to patrol. Confirmed live: even manually
skinning the Wolf didn't unstick it, since skinning only hides the object,
it doesn't destroy or revive it.

Fixed by checking `IsDead` in both `ThreatStillValid()` (drops a dead
threat immediately) and `FindNearestThreat()` (never picks a dead
creature as a new target in the first place — matters for the window
before a kill's corpse eventually respawns). Also removed the
`[GuardDiagnostic]` logging now that it's served its purpose.

Compile-verified via batch-mode Unity (0 errors). Not yet live-tested —
Editor was closed for this pass.

## 2026-08-18 (6)

### v0.3.131-dev — Diagnostic logging for the Guard-still-not-moving bug

Live-testing v0.3.129-dev's patrol approach/orbit fix found the Guard
still isn't approaching the Flag at all — confirmed via the Player Map
that there's only one Flag on the whole terrain (ruling out the "stuck on
a different, older Flag" theory), and the Guard was stationary for 5+
real minutes with the player standing right at the Flag. Two close static
re-reads of `NPCGuarding.cs` found nothing conclusively wrong —
`NPCWander`/`NPCFreeze` both correctly gate on `isPaused`, the approach/
orbit math looks right on paper. Added temporary `[GuardDiagnostic]`
logging (once/second, not every frame) covering: whether `Update()` even
reaches the patrol branch (vs. bailing on `isPaused`/not-ready, or
latching onto a `currentThreat` that only turns to face and never moves),
and inside `UpdatePatrol()` itself — which Flag it resolved, distance/
radius/mode (approach vs. orbit), and the actual before/after position
delta around the `MoveToward` call. Next live session with the Console
open should make the actual blocker obvious instead of guessing a third
time.

Compile-verified via batch-mode Unity (0 errors). Not yet live-tested —
Editor was closed for this pass.

## 2026-08-18 (5)

### v0.3.130-dev — Fix: hired NPCs were really running a 300s pay cycle, not 3600s

Solves the payment-timer mystery flagged 2026-08-17 (a timer reading
"298s" right after payment, then "5s" shortly after — genuinely
inconsistent with `NPCHiring.workDurationSeconds = 3600f`, which kept
checking out correct on every direct code grep). Root cause, found by
grepping prefabs/scene directly instead of the script: `NPCFactoryWorker
.prefab` had a **stale serialized override**, `workDurationSeconds: 300`,
left over from before the field's C# default was bumped 300→3600 (see the
field's own comment — "testing/playing across a longer session made 5
minutes too short a leash"). Exactly the "changed `[SerializeField]`
default doesn't apply to existing scene/prefab instances" gotcha
`CLAUDE.md` already documents — the code was always right, the prefab was
silently overriding it back the whole time. Confirmed both
`NPCFactoryWorkerMale.prefab` and `NPCFactoryWorkerFemale.prefab` (the
prefabs `VillageFlagSpawner` actually spawns) are nested prefab variants
of `NPCFactoryWorker.prefab` with no override of their own, so they were
both silently inheriting the stale 300 the whole time too. Fixed by
correcting the base prefab's value to `3600`; confirmed no separate
override exists on either Male/Female or anywhere in `TestScene.unity`
(consistent with 0 pre-placed NPCs since v0.3.118-dev).

The other half of that original report — an NPC's custom name reverting
to default at the same moment — remains unexplained and is **not** what
this fix addresses; logged separately in `BUGS_AND_ENHANCEMENTS.md`.

Compile-verified via batch-mode Unity (0 errors, all 3 prefabs reimported
cleanly). Not yet live-tested — Editor was closed for this pass.

## 2026-08-18 (4)

### v0.3.129-dev — Fix: Guard patrol math couldn't converge on a small radius; diagnostics cleaned up

Live-testing v0.3.128-dev's new player-set patrol leash immediately found a
real bug: Ben set a 2m radius and the Guard never got any closer to the
Flag. Root cause — the orbiting patrol target's tangential speed works out
to exactly `radius × (moveSpeed / radius) = moveSpeed`, the Guard's own top
speed, **regardless of radius**. At the original 35-75m radii this was
invisible (the target crawled slowly enough to always be reachable), but at
2m the target now circles as fast as the Guard can walk — a Guard starting
outside the circle was chasing a point that never got any closer, since
keeping up angularly ate its entire speed budget.

Fixed by splitting `NPCGuarding.UpdatePatrol()` into two modes: **approach**
(walk straight toward the nearest point on the circle — a fixed, catchable
target — whenever outside `patrolRadius + 0.5m` tolerance) and **orbit**
(only once already within range, using an angle recomputed from the Guard's
own current bearing each frame rather than a persistently-incrementing one,
so there's no jump entering orbit mode).

Also confirmed, via direct Console evidence during the same live session,
that two previously-open mysteries were both non-issues:
- **Campfire utensil-slot persistence** — `[CampfireSaveDiagnostic]` logs
  showed `existing=Campfire` resolving correctly and a clean 0→1 slot-count
  round-trip on the Frying Pan. The original failure report didn't
  reproduce; capture/restore code was correct all along.
- **"Ore not breaking"** — `[GatherDiagnostic]` logs showed
  `succeeded=True ... stillAvailable=False` on a real harvest, confirming
  `ResourceNode.TryHarvestForNPC` correctly marks nodes unavailable. Was
  always just a mid-timer snapshot, not a real bug.

Both temporary diagnostic logging blocks removed from `SaveManager.cs` and
`NPCGathering.cs` now that they've served their purpose.

Compile-verified via batch-mode Unity (0 errors, real full pass). The
patrol fix itself is not yet live-tested — Editor was closed for this pass.

## 2026-08-18 (3)

### v0.3.128-dev — Guard patrol radius is now a player-set leash, not a reused tier-scale

Fixes the `NPCGuarding` patrol-radius bug flagged 2026-08-17 (a Masterwork
Flag gave every Guard a 75m patrol circle, since it was reusing
`CraftTierScale.VillageFlagRevealRadius` — a scale tuned for the Player
Map's fog reveal, not a Guard's patrol size). Ben's fix, chosen over a new
dedicated tier table: make it a per-NPC configurable leash, same shape as
`NPCGathering.MaxRangeFromDeposit`, rather than tying it to Flag tier at
all.

- `NPCGuarding` gained a `patrolRadius` field (default 15f, min-clamped 1f)
  and `PatrolRadius` property, replacing the `VillageFlagRevealRadius` call
  in `UpdatePatrol()` entirely.
- `NPCHiringScreen` gained a "Patrol radius (around Flag):" row, same
  text-field-plus-Set-button shape as the existing "Work range (from
  deposit box):" row, gated on the NPC's actual assigned job kind being
  Guarding.
- **Also fixed the already-logged leash-persistence bug for both leashes
  at once**: neither `NPCGathering.MaxRangeFromDeposit` nor the new
  `NPCGuarding.PatrolRadius` were ever captured by `SaveManager`, so both
  silently reset to their component defaults on every reload. Both now
  round-trip through `CaptureNpc`/`RestoreNpc`.

Compile-verified via batch-mode Unity (0 errors, real full pass). Not yet
live-tested — Editor was closed for this pass.

## 2026-08-18 (2)

### v0.3.127-dev — Auto-Run toggle moved up; Leather Backpack now a valid NPC tool

Two small fixes, both requested directly after the Cooking playtest above:

- **Auto-Run toggle moved to the top of `CampfireScreen`**, right under the
  Lit/Unlit status line — it was buried at the very bottom of the panel
  (below Fuel, next to Light), easy to miss entirely (caused a false "auto-run
  doesn't work" report during tonight's playtest that turned out to just be
  the toggle never having been switched on).
- **Leather Backpack (all 5 tiers) was silently rejected as an NPC tool** —
  `MineOreJob`/`ChopWoodJob`/`ForageJob`/`MetalworkingJob` only listed the
  original plain Backpack ladder's 5 tier guids in their "Backpack"
  `ToolRequirement.acceptableItems`, never backfilled once the newer Leather
  Backpack ladder shipped. Added all 5 Leather Backpack tier guids to each of
  the 4 jobs.

Compile-verified via batch-mode Unity (0 errors, real full pass), both
changes confirmed present via direct grep. Not yet live-tested — Editor was
closed for this pass.

## 2026-08-18

### v0.3.126-dev — Cooking fixes: Fried Egg edible, quantity display, Auto-Run

A live Cooking playtest (Ben, 2026-08-18) confirmed the skill/quality system
end-to-end (gate hiding/unlocking Steak and Potatoes at 15, success/failure
rolls, XP gain, tier unlock, multi-item Output stacking) but surfaced 4 real
gaps, all fixed same session:

- **Fried Egg couldn't be eaten** — `PlayerEating.edibles` in `TestScene.unity`
  is a hand-maintained array, and `FriedEggEdible.asset` was missing from it
  (every other cooked item's Edible was present). Added.
- **No quantity shown** on `CampfireScreen`'s Ingredients/Output/Fuel slots for
  any item with a baked icon — `DrawBox` put the count into the box's
  `GUIContent` text, which gets blanked out the moment `slot.item.icon != null`.
  Added a separate `QTY: {count}` label below the box, same fix
  `InventoryScreen.DrawSlotBox` already has for this exact case.
- **No Auto-Run** — Campfire cooking was single-shot (had to click "Cook"
  once per item even with a full ingredient stack) and the fire always went
  out after exactly one fuel unit even with more stacked in the slot, since
  neither auto-relight nor auto-repeat existed. Added an opt-in `Auto-Run`
  toggle (off by default, same shape as `Furnace.AutoRunEnabled`) that
  relights from remaining fuel and re-cooks the last recipe as long as it's
  still satisfiable. Saved/restored alongside the rest of Campfire's state.
- **Utensil slots (Grill/Cooking Pot/Kettle/Frying Pan) appeared not to
  survive save/reload** — static review of the capture/restore path,
  `SaveId`, and the scene's Campfire `PrefabInstance` found nothing
  conclusively wrong; everything looks structurally correct. Added temporary
  `[CampfireSaveDiagnostic]` logging to `SaveManager.RestorePlacedPieces`
  instead of guessing further — next live save→reload with the Console open
  will pin down whether `existing` resolves to the right object. **Not yet
  fixed** — see `BUGS_AND_ENHANCEMENTS.md`.

Compile-verified via batch-mode Unity (0 errors, real full pass).

**Live-tested same night, immediately after.** All 3 real fixes confirmed
working: Fried Egg is eatable, `QTY:` labels show correctly across
Ingredients/Output/Fuel/Transfer slots, and Auto-Run genuinely relights +
auto-repeats once toggled on (an initial "doesn't appear to work" report
turned out to be the toggle simply never having been switched on, not a
bug). Also ran a full audit of all 6 cooked-item Edibles while at it —
every one now correctly registered and eatable/drinkable (Herbal Tea
correctly uses `verb: Drink`). Follow-up requested: the Auto-Run toggle
sits at the bottom of the panel and is easy to miss — move it up near the
Lit/Unlit status line. The utensil-slot save/reload bug is still open,
diagnostic logging still in place, needs a live save→reload with the
Console open to actually read the output.

## 2026-08-17 (9)

### v0.3.125-dev — Fix: job-kind-gated UI was showing for the wrong jobs

Live-testing the NPC management pass immediately found two real UI bugs,
both the same root pattern: `NPCJobScreen`'s "Set Deposit Container"
button showed for *every* non-Crafting job, including Guarding — but
`NPCGuarding` never reads `job.DepositContainer` at all, so setting one
on a Guard visibly did nothing and genuinely misled Ben live. The new
work-range leash field had the identical issue: it checked "does this
NPC have an `NPCGathering` component" (true for every NPC, since all
three job components coexist on the same prefab) instead of "is
Gathering this NPC's actual current job." Both fixed by checking the
NPC's real assigned job kind explicitly instead of inferring it from
component presence or a `!=` exclusion. Confirmed live — the leash field
now only appears for the Mine Ore job.

Also found and logged, not yet fixed: `NPCGuarding`'s patrol radius
reuses `CraftTierScale.VillageFlagRevealRadius` (tuned for the Player
Map's fog reveal, not patrol distance) — a Masterwork Flag gives a Guard
a 75m-radius patrol circle, huge relative to the 200×200 unit terrain,
explaining a Guard observed wandering far from its post. And the
already-logged weak obstacle-avoidance pattern in `NPCGathering`/
`NPCCrafting`/`NPCTraining`/`NPCGuarding` was confirmed live a second and
third time tonight (a Miner stalling near a Boulder, twice) — now the
most-confirmed live bug of the whole session.

## 2026-08-17 (8)

### v0.3.124-dev — Real bug fix (Flag rename crash) + a full NPC management pass

**Real save bug found and fixed**: a renamed Village Flag lost its name
on the *next* reload, root-caused via a temporary diagnostic log (added,
used, then removed once fixed) to an `ArgumentNullException` in
`SaveIdRegistry.Unregister` — `SaveManager.RestorePlacedPieces`
re-instantiates a missing structure from its raw `BuildPiece.prefab`,
which has no `SaveId` baked in; `RequireComponent`'s auto-add doesn't
reliably fire `Reset()` for a runtime `AddComponent` (same gotcha the
original v0.3.119-dev placement fix covers, just hit a second time in
the restore path). The freshly-added `SaveId.id` stayed null, and
`AssignId` calling `Unregister` on it threw — silently aborting the rest
of that restore iteration, including the `villageName` restore step
right after. Fixed with two changes: `SaveIdRegistry.Unregister` now
guards against a null/empty `Id`, and `RestorePlacedPieces`/`RestoreNpcs`
both call `GenerateIfMissing()` before `AssignId()`. **Live-tested end to
end** — renamed a Flag, saved, exited, relaunched, name survived.

**A full "NPC management" pass, 5 chunks, each compiled clean before the
next**:
1. **Tool Swap** — `NPCJobScreen` used to only show "Give" on an empty
   tool slot; upgrading an equipped tool meant firing the NPC and
   losing everything else too. New `NPCJob.SwapTool` lists every owned
   tier and lets a specific one be picked, returning the replaced tool
   to the player's inventory instead of destroying it.
2. **`NPCFreeze`** — a "Frozen (stay in place)" toggle on
   `NPCHiringScreen`, built as a standalone reusable component (no
   `RequireComponent` chain) so a future Traveling Trader can reuse it
   without being an `NPCHiring`.
3. **Take / Take All cargo buttons** — `NPCHiringScreen`'s cargo display
   was read-only; an unpaid/fired NPC's cargo was never actually lost
   but was permanently unreachable. Works remotely from the Roster too.
4. **Deposit-anchored work-range leash** — `NPCGathering.searchRadius`
   re-centers on wherever the NPC currently stands, letting it drift
   outward indefinitely across hops (very likely what stranded the
   Miner from earlier tonight). New `MaxRangeFromDeposit`, configurable
   per NPC via `NPCHiringScreen`, anchors to the actual `DepositContainer`
   position instead — deliberately not the Village Flag, which is the
   right anchor for Guarding's patrol but not for a Gatherer's home base.
5. **Color-coded Map markers + Roster tools** — `MapScreen.DrawNpcMarkers`
   now tints each dot by status. `NPCRosterScreen` gained a waiting-count
   header + "Pay All", a per-row "Locate"/"Stop" driving a new waypoint
   compass, and `NPCHiring` gained a static `OnPaymentDue` event feeding
   a new `PlayerNPCPaymentToast` (Y=270, checked against all 7 other
   existing toasts before picking it).

All 5 chunks compile-verified; none live-tested in Play mode yet.

## 2026-08-17 (7)

### v0.3.123-dev — NPC gender/auto-naming, Map/Roster NPC tracking, longer work cycle

**NPCs now randomly spawn Male or Female with a real auto-assigned
name.** `VillageFlagSpawner` split its single `hireableNpcPrefab` field
into Male/Female variants (previously only ever spawned Male), coin-flip
picks between them, and a new `NPCNameGenerator.PickUnique` assigns a
name from a static list — preferring one not already in use by a
currently-active NPC. `NPCDialogue` now implements `IRenameable` (same
right-click flow `StorageBox`/`VillageFlag` use) so a player can rename
on top of the auto-assigned default. Both name and gender are captured/
restored by `SaveManager`, including the recreate-on-load path — a
restored NPC comes back as the same gender it was, not a fresh coin
flip.

**The Map and a new `N`-bound NPC Roster screen both now track NPCs
live.** `MapScreen.DrawNpcMarkers` mirrors `DrawFlagMarkers`' exact
pattern (a fresh scan every `OnGUI` frame — real live position, no
extra plumbing). `NPCRosterScreen` (new) lists every NPC with name/job/
status/distance; "Manage" opens the same `NPCHiringScreen` a walk-up-
and-E interaction would. Built directly off live-testing pain — this
same session involved diagnosing a wandering Miner and a frozen Guard
one at a time by physically walking to each.

**NPC work cycle lengthened from 5 to 60 real minutes** (`NPCHiring
.workDurationSeconds`) — the 5-minute placeholder was too short a leash
now that Village-Flag-spawned NPCs are the only source and testing
spans much longer real sessions.

Also found and logged (not yet fixed): giving an NPC a Leather Backpack
is silently rejected — all 4 jobs needing a Backpack tool
(`MineOreJob`/`ChopWoodJob`/`ForageJob`/`MetalworkingJob`) only list the
original plain Backpack's 5 tier guids, never updated when the separate
Leather Backpack family was added later. Also: `NPCJob.TryGiveTool`/
`NPCJobScreen.HasAny` only check the player's top-level inventory, never
a worn Backpack's nested contents. And the same weak single-deflection
obstacle-avoidance `NPCSeekFlag` had (fixed in v0.3.116-dev) is still
present, unfixed, in `NPCGathering`/`NPCCrafting`/`NPCTraining`/
`NPCGuarding` — confirmed live via a Guard that got permanently stuck
near a Boulder.

## 2026-08-17 (6)

### v0.3.122-dev — PlayerMagic actually saves now

`PlayerMagic` used to silently re-randomize the player's starting
lineage on every scene load — no capture, no restore, ever. Hit live
this session: a skill book's Elemental grant survived a blank-screen
crash purely by coincidence (the reroll happened to land on Elemental
again), which is what surfaced the bug in the first place once the
Magic tab was actually checked.

`PlayerMagic.Awake()` now only assigns a free random starting lineage
for a genuinely new character, guarded on `SaveManager.SaveExists` (same
pattern `GardenPlot4x4`'s own fresh-start init already uses).
`SaveManager` gained a real capture/restore pair for every known lineage
(via `SkillDatabase` resolution, reusing the existing `PlayerMagic
.LearnLineage`) and the selected wish (new `PlayerMagic.FindWish`/
`IdForWish`). A new `AssignRandomLineageIfNone` keeps old save files
(written before this fix, with no lineage data at all) from loading into
zero known magic. Also fixed `MagicScreen.cs`'s "Lineage:" header, which
only ever displayed the single old `StartingLineage` field and could
show a stale, misleading lineage once a player knew more than one —
it now lists every lineage from the real `KnownLineages` set.

**Live-tested, full round trip**: read a second lineage book
(Elemental, on top of Restoration), saved, exited, relaunched — both
lineages survived, the header listed both, both wishes cast correctly
(Heal Self, Spark — the latter lighting a Campfire), and both trained
their skills live (Restoration → 3.0, Elemental → 2.0).

## 2026-08-17 (5)

### v0.3.121-dev — Egg icon fixed; Campfire/Furnace legacy fixtures joined the save system

**Egg icon**: `Egg.asset` had `icon`/`previewIcon` both null since it
shipped — a plain `IconBaker` pass (`EggPickup.prefab` → `Egg.asset`,
128px preview) fixed it. Confirmed by actually reading the rendered
PNGs, not just trusting the batch log — both show a real, visible egg.

**Campfire/Furnace save gap**: live testing v0.3.119-dev's Village Flag
fix immediately surfaced a second, different bug — a Campfire's lit
state/fuel/utensils/ingredients reset to empty after every reload, even
though the Flag itself now correctly persisted. Root cause: this
project's original Campfire and Furnace are fixtures hand-placed
directly in `TestScene.unity` back when the game was first built —
they predate the whole `PlacedPiece`/`SaveId` save system entirely, so
`CaptureWorldObjects<PlacedPiece>` never even saw them. Fixed via a
one-off Editor migration (mirrors the original 2026-08-13 StorageBox/
ResourceNode/NPCHiring migration): the scene's Campfire retroactively
got a real `PlacedPiece` (linked to `CampfirePiece.asset`) + `SaveId`.
The Furnace has no `BuildPiece`/prefab at all (a single fixed fixture,
never player-buildable), so it got its own direct `[RequireComponent
(typeof(SaveId))]` and became a standalone top-level `SaveManager`
category (`["furnaces"]`, found-and-restored the same simple way
`StorageBox` already is) instead of trying to force it through the
`PlacedPiece` system. Both migrated components verified via direct YAML
read (real non-empty GUIDs), not just a clean batch log.

Also (minor, side effect of the earlier truncated grep confusion):
confirmed live that `Campfire.prefab`'s `cookableItems` array already
includes `FriedEggCookable` — that registration gap was already fixed
by an earlier pull, this doc/session had it listed as still-open in
error.

## 2026-08-17 (4)

### v0.3.120-dev — Village Flag spawn: real values locked in

Live testing across the last two versions confirmed both the spawn-loop
math and the Village-Flag save fix actually work, so `VillageFlagSpawner`
no longer needs its temp test values. `BaseIntervalMinutes` reverted to
the real 30 real-minute baseline. `spawnDistanceFromFlag` kept
permanently lower than its original 40m — 20m, Ben's call — rather than
reverting all the way back.

## 2026-08-17 (3)

### v0.3.119-dev — Fix: placed structures still weren't actually saving

v0.3.118-dev's built-structure persistence shipped compile-verified only
— live testing it immediately after found it didn't actually work.
Ben admin-spawned a Village Flag, saved, restarted, and `save.json`
showed `"placedPieces": []`, completely empty.

Root cause: `PlacedPiece.cs`'s `[RequireComponent(typeof(SaveId))]` gets
triggered by a runtime `AddComponent<PlacedPiece>()` call (both
`PlayerBuilding.Confirm` and `AdminSpawnScreen.SpawnPiece` add it this
way) — and `SaveId.Reset()`, which generates its GUID, doesn't reliably
fire when a required component is auto-added via a scripted
`AddComponent` call, only via the Editor's own "Add Component" button.
`SaveId.cs`'s own migration-script comment already documented this exact
gotcha; it just wasn't applied to this new code path. Every placed
structure's `SaveId.Id` silently stayed empty, and both
`SaveIdRegistry.Register` and `CaptureWorldObjects` quietly skip
anything with an empty ID — no error, no warning, no hint anything was
wrong short of reading the actual save file.

Fixed by calling `GetComponent<SaveId>()?.GenerateIfMissing()` explicitly
right after `AddComponent<PlacedPiece>()` at both call sites. Compile-
verified; still needs a real save/reload round trip to confirm it holds.

## 2026-08-17 (2)

### v0.3.118-dev — Built structures + hired NPCs now save/restore; 0 starting NPCs

Live-testing find: a Masterwork Village Flag placed the night before was
simply gone after a reload, and the knock-on effect was worse than a
missing decoration — a live-tested Ranged Guard (`NPCGuarding`) that
should have been circling it was instead wandering aimlessly, since its
patrol behavior depends on a `VillageFlag` existing in the world at all.
Root cause: `SaveManager.cs`'s capture list never covered `BuildPiece`
placements at all — every wall, foundation, Campfire, Furnace, Village
Flag, and City Statue a player builds vanished on reload. Full design in
`SAVE_LOAD_PLANNING.md` section 11.

New `BuildPieceDatabase` (`ItemDatabase`-shaped stable-ID lookup, wired
into `DatabaseRepopulator`, 28 `BuildPiece` assets indexed) plus a
`["placedPieces"]` capture/restore pair in `SaveManager.cs`. Unlike every
other saved category, a placed structure doesn't pre-exist in a fresh
scene, so restore re-instantiates the piece's own prefab at its saved
position/rotation and reassigns its saved `SaveId` (new
`SaveId.AssignId`) — fixes Village Flag (plus its display name),
City Statue, and every plain Wall/Foundation/Roof Panel. Campfire and
Furnace additionally get their full runtime state back: lit status,
real-time fuel-burn and cook/smelt timers, the active recipe (resolved
by matching `outputItem` against the instance's own registered recipe
list via `ItemDatabase`, sidestepping a dedicated `CookableItem`/
`SmeltableItem` database), the Furnace's 4-slot recipe queue, its 3
linked StorageBox references, and every inventory slot on both
structures.

**0 starting NPCs, Ben's call**: all 6 pre-placed Factory Worker NPCs
removed from `TestScene.unity` — no more "just walk up and hire the guy
standing there." The Village Flag spawn loop (`VillageFlagSpawner.cs`)
is now the only source of hireable NPCs in the game. This immediately
made the identical save gap relevant to `NPCHiring` — with zero NPCs
baked into the scene, a hired NPC would never come back after a reload
either. Fixed the same way: `SaveManager.RestoreNpcs` re-instantiates
from `VillageFlagSpawner.HireableNpcPrefab` (new public getter) when a
saved NPC's `SaveId` isn't found in the scene.

Also caught in the same pass: `ItemDatabase.asset` was missing Chicken
Meat entirely — never repopulated since that item was added — fixed by
running `DatabaseRepopulator` as part of this work.

**Verified via compile only — not yet live-tested with a real save →
reload round trip.** That's the immediate next step.

## 2026-08-17 (1)

### v0.3.117-dev — Three small fixes found live by Ben right after the MVP2 status review

- **`NPCJobScreen`'s family tabs overflowed the panel, making Guarding
  unreachable.** Found live: with 5 job families now wired in (Mining/
  Woodworking/Gathering/Metalworking/Guarding) at a fixed 130px each, only
  3 fit inside the 480px panel — Guarding's tab rendered off-panel and
  was never clickable, a ticking problem as more families get added later.
  `DrawFamilyTabs()` now wraps onto additional rows (`tabsPerRow` computed
  from `PanelWidth`/`TabWidth` rather than hardcoded, so it keeps working
  as the family list grows). `PanelHeight` bumped 420 → 460 for the extra
  row's headroom.
- **Egg still couldn't be cooked — the real, previously-diagnosed blocker,
  now actually fixed.** `BUGS_AND_ENHANCEMENTS.md` already root-caused this
  live on 2026-08-16: `Campfire.prefab`'s `cookableItems` array never
  included `FriedEggCookable` (only 5 other recipes were registered), so
  `Campfire.cs`'s Ingredients-box allowlist rejected Egg outright regardless
  of the earlier `requiredSkillLevel` fix. Added `FriedEggCookable` to the
  array — one line, a prefab data edit, no code change. (Chicken Meat still
  correctly gets rejected — no recipe uses it yet, unrelated and not a bug.)
- **Bow/Arrow had no right-click Equip option.** Both are plain
  `ItemDefinition`s (not `IEquippable`), same category as Pickaxe/Axe, so
  `InventoryScreen`'s click-action popup never offered Equip for them —
  drag-to-hand already worked, this was just a missing shortcut. Added an
  `Equip` branch to `DrawPendingActions()` for `isRangedWeapon`/`isArrow`
  items, dispatching to a new `TryEquipToHand()` that moves the item's
  whole available stack into the first free hand (Left, then Right) via
  the same `InventoryTransfer.MoveAsManyAsFit` call the drag path already
  uses — same operation, just reachable from a click too.

Verified via batch-mode compile (clean, `Tundra build success`, return
code 0) — not yet live-tested in Play mode.

## 2026-08-16 (23)

### v0.3.116-dev — Village Flag NPC: wider arrival range, real obstacle
avoidance, swapped to the Kevin Iglesias model

Live-testing find: the NPC spawned by the Village Flag near the timber-
frame building never closed the distance — `NPCSeekFlag.MoveToward`'s old
obstacle handling only tried a single perpendicular deflection off
whatever it hit, which could point straight into a second obstacle at a
corner and stall the NPC there permanently. Replaced with a real
directional search (`FindClearDirection`): tries the desired heading
first, then widens outward left/right in 15° steps until it finds a clear
raycast, falling back to a full reverse only if genuinely surrounded.
`ArriveRange` also widened 2m → 5m (Ben's call — this NPC is idling near
the Flag waiting to be hired, not interacting with it the way an
Anvil/Furnace/Desk surface requires, so it doesn't need to walk all the
way in).

Also swapped `VillageFlagSpawner`'s `hireableNpcPrefab` from
`NPCFactoryWorker.prefab` (the old blocky placeholder model, visibly out
of place next to every other NPC) to `NPCFactoryWorkerMale.prefab` (the
Kevin Iglesias Human Dummy model already used for the game's other
pre-placed hires) — a scene-only reference swap, no new asset.

**Still carrying two explicit TEMP TEST VALUES in `VillageFlagSpawner.cs`
from this same live-testing stretch** (`BaseIntervalMinutes` 30→3,
`spawnDistanceFromFlag` 40→15, both marked `REVERT before committing` in
comments) — committed as-is at Ben's explicit request so live testing of
the spawn loop can continue at the faster pace. **Revert both before this
becomes anything other than an active test build.**

## 2026-08-16 (22)

### v0.3.115-dev — Fix: PlayerAutosave toast overlapped PlayerCrafting's

Caught live within minutes of v0.3.114-dev shipping — Ben's screenshot
showed a crafting outcome message and a skill-gain toast stacked at the
top of the screen, which was the trigger for actually checking every
existing toast position in the project instead of just the one
(`PlayerSkills`) the original placement was checked against.
`PlayerCrafting.cs` already owns y=110 for its own craft-outcome
messages — `PlayerAutosave`'s toast moved to y=150, clear of both
existing top-center toasts (`PlayerSkills` at y=70, `PlayerCrafting` at
y=110). Same screenshot incidentally confirmed a real crafting outcome
live for the first time — the "Close, but not quite" Barely-Fail
downgrade message, see `TEST_FEATURE_PLAN.md`'s crafting section.

## 2026-08-16 (21)

### v0.3.114-dev — Autosave added (doesn't replace the manual Save button)

Ben's ask: about to spawn food, eat, heal back up from the 0-HP save
state, and wait out the Village Flag's real-time spawn timer — wanted a
safety net so a long unattended wait doesn't risk losing progress if
something goes wrong, without needing to remember to hit Save first.

- New `PlayerAutosave.cs` (`[RequireComponent(typeof(SaveManager))]`) —
  calls the existing `SaveManager.Save()` every 10 real minutes and
  shows a 15-second top-center toast ("Game autosaved."), same shape as
  `PlayerSkills.cs`'s own tier-unlock toast (`DebugGUI.DrawPanel` +
  `DebugGUI.Header`).
- `SAVE_LOAD_PLANNING.md`'s original "manual Save button only, no
  autosave for v1" scope updated to reflect this — the manual button in
  `GameMenuScreen` is completely untouched, this is a second trigger
  layered on top, not a replacement.
- Attached to the `Player` object in `TestScene.unity`, next to the
  existing `SaveManager` — verified via direct YAML grep.
- **Toast position had a real bug, fixed in v0.3.115-dev right after** —
  see that entry above.

## 2026-08-16 (20)

### v0.3.113-dev — Tuning: Hunger/Thirst drain slowed 3x

Ben's feedback from live play: Hunger/Thirst dropped very fast — worth
checking against the actual numbers, not just a gut feeling. Confirmed:
Hunger emptied in 20 real minutes, Thirst in 12, both from full — a
Meal-tier food (40 Hunger) only bought back 8 minutes, meaning eating
almost constantly just to keep pace.

- `PlayerVitals.hungerDrainPerSecond`/`thirstDrainPerSecond` both slowed
  3x: Hunger now empties in 60 real minutes, Thirst in 36 — keeps the
  same relative pace between the two (~1.67x) while turning survival
  upkeep into an occasional task instead of a constant one.
- Updated in both places per CLAUDE.md's own stale-`[SerializeField]`-
  default gotcha — the C# default *and* `TestScene.unity`'s already-
  serialized override, confirmed matching after the edit.
- Ben picked "~3x slower" from a menu of options (2x/3x/5x/exact) rather
  than an arbitrary number — a real tuning decision, not a guess.

## 2026-08-16 (19)

### v0.3.112-dev — Fix: Cooking skill deadlock, Feather's broken model/icon, dropped-loot tunneling

Three bugs found live by Ben in one Editor session, all fixed.

- **Cooking skill deadlock**: every `CookableItem` that grants Cooking XP
  required Cooking ≥5 to unlock, while the only recipe reachable at
  Cooking 0 (Raw Meat → Cooked Meat) grants no XP at all — a genuine
  progression dead end, not just a slow grind. Fixed by lowering
  `FriedEggCookable.requiredSkillLevel` from 5 to 0, giving the game a
  real entry-level Cooking recipe (Egg + Frying Pan, single ingredient)
  a fresh character can actually reach.
- **Feather's model was genuinely broken, not just missing an icon**:
  `Feather.glb`'s mesh had 2 of its 4 quad vertices coincident at the
  origin, leaving only one real (degenerate-looking) triangle — a thin
  spike, confirmed via direct render, not a feather silhouette. Replaced
  with a real model (`Tools/Blender/GenerateFeatherModel.py`, a tapered
  vane + quill). Also hit the same glTF-remap-doesn't-apply bug as
  Chicken Meat and fixed it the same way (material assigned directly on
  the wrapper prefab). The vane's material also needed `_Cull: 0`
  (double-sided) — thin single-sided foliage-style geometry is an easy
  place to get the front-face winding backwards. `IconBaker`'s tight-fit
  reprojection couldn't frame this particular thin/tall shape correctly
  no matter which `cameraDirection` override was tried (confirmed
  correct geometry/material via a simple direct face-on render first,
  ruling those out) — baked with a small dedicated camera setup instead
  of fighting the generic tool further.
- **Dropped loot (at least Egg and Leather) fell through the world**:
  root-caused to two compounding issues. (1) `SkinnableCreature.Complete()`
  called `DropLoot()` *before* disabling the corpse's own Collider, so a
  freshly-spawned pickup could land overlapping a still-solid corpse,
  and Unity's physics-overlap separation impulse could eject it through
  nearby terrain — fixed by disabling the collider first. (2) A broader
  audit found 49 of the project's 74 `Pickup` prefabs still used
  Discrete collision detection, the exact "small fast-moving object vs.
  a thin static collider" tunneling risk `PlayerCoinDrop.cs` had already
  been fixed for — switched all 49 to `ContinuousDynamic`, matching the
  25 that already had it right.
- Verified via batch-mode compile, direct YAML/asset grep, and rendered
  checks throughout — not yet confirmed live in Play mode.

## 2026-08-16 (18)

### v0.3.111-dev — Fix: Village Flag right-click rename only worked on the pole

Bug caught live by Ben — right-clicking the visible flag banner did
nothing; renaming only worked by aiming at the thin pole instead.

- **Root cause**: all 5 `VillageFlag_*.prefab` tiers only had a Collider
  on the `Pole` child (a `CapsuleCollider`, ~0.025m world-space radius
  once scaled) — the `Banner` child (the actual colorful flag cloth
  players naturally aim at) had no Collider at all. `PlayerRenaming`'s
  right-click raycast had nothing to hit unless aimed almost exactly at
  the thin pole.
- **Fix**: added a single `BoxCollider` on each tier's root, sized and
  centered to the combined Pole+Banner renderer bounds (measured per
  tier, not a shared guess) with a small padding margin — right-clicking
  anywhere on the visible flag now hits something. The original pole
  `CapsuleCollider` is left in place, harmless.
- Confirmed live by Ben immediately after: renamed a Flag to "Phoenix,"
  which also correctly appeared on the Player Map — incidentally
  confirmed `MapScreen`'s label-per-flag drawing has no duplicate-render
  bug either (an earlier screenshot showing two overlapping "Unnamed
  Village" labels turned out to be two separate Flags, not one label
  drawn twice).

## 2026-08-16 (17)

### v0.3.110-dev — Fix: flying arrow faced backwards in flight

Bug caught live by traskmi hunting a Chicken with a Bow and Arrow
(2026-08-16) — the arrow visual flew fletching-first, arrowhead trailing.

- **Root cause**: `FlyingArrow.Launch()` orients the object via
  `Quaternion.LookRotation(end - start)`, which points local +Z toward
  the flight direction — but `ArrowFlightVisual.prefab`'s nested Arrow
  model carries its own baked rotation (authored for how the arrow looks
  equipped/held, not in flight), which points the arrowhead toward local
  -Z instead. The two combined backwards.
- **Fix**: an extra `Quaternion.Euler(0, 180, 0)` in `Launch()`'s rotation
  calculation, correcting for the mismatch without touching the model's
  own equip-context rotation. Confirmed via a diagnostic render with
  color-coded direction markers (red = fire direction, green = behind)
  before and after — arrowhead now clearly leads toward the red marker.
- Same root-cause family as CLAUDE.md's other "imported model's authored
  forward axis doesn't match Unity's `LookRotation` convention" gotchas
  (`NPCWander`'s `modelForwardOffsetY`, `PreyWander`'s equivalent) — worth
  checking for if any other script combines a nested-prefab visual with
  `LookRotation` the same way.
- Verified via a rendered diagnostic (not just a compile check) — not yet
  confirmed by an actual in-Play-mode shot, though the visual mechanism
  is now provably correct.

## 2026-08-16 (16)

### v0.3.109-dev — Pig built, closes out item 8's animal roster

Ben added `Assets/Animal pack deluxe v2/` (a full animal asset pack) and
asked to implement the Domestic Pig from it, closing the last open animal
in item 8's Chicken/Pig/Deer/Rabbit lineup.

- **No Tripo3D scaling gotcha this time** — unlike every generated model
  in this project, this pack's `Domestic_pig` measures a realistic 0.76m
  tall × 1.44m long × 0.55m wide raw, already close to a real pig's
  proportions next to the 1.8m player. Only needed the usual small
  ground-offset correction (+0.04), no rescale.
- **Same legacy-Built-in-shader-invisible-under-URP gotcha as HumanDummy/
  Rabbit** — `Domestic_pig_mat.mat` used `fileID: 46` (Built-in Standard).
  Converted to `Universal Render Pipeline/Lit`, carrying over the albedo
  (`domestic_pig_col_unity`), normal map, and occlusion map this pack
  actually ships (unlike Rabbit's simpler material, this one had all
  three wired already, just on the wrong shader) — confirmed visually via
  a render, not just a clean log.
- **The pack's own shipped `Domestic_pig_anim_controller` wasn't usable
  as-is** — every transition has empty conditions (a no-parameter random
  idle-cycling demo setup, not driven by any Speed value). Built a fresh
  `PigAnimator.controller` instead, same 2-state Idle/Run shape Rabbit's
  own controller uses, driven by `PreyWander`'s existing `Speed` float —
  confirms `PreyWander.cs` really was built generic (2026-08-16, same
  session): adopted here with zero changes to that script.
- **`Pig.prefab`** — `BoxCollider` sized to measured bounds, `PreyCreature`
  (`maxHealth = 25`, same 5-tier Knife `requiredTools` + Gathering
  `skinningSkill` Deer already uses, Raw Meat ×2-3 loot — between Rabbit's
  1-2 and Deer's 2-4, no Leather since Deer already owns that drop), plus
  `PreyWander` for real idle/wander/flee AI from the start (unlike
  Chicken/Deer, which still need retrofitting). 2 instances placed in
  `TestScene.unity`.
- Verified via a rendered visual check (not just YAML) that the material
  fix actually took, plus direct scene-guid grep for placement — not yet
  live-tested in Play mode.

## 2026-08-16 (15)

### v0.3.108-dev — Chicken Meat, third Chicken loot drop

Ben's ask: give Chicken a third loot drop ("Chicken Meat") alongside its
existing Feather/Egg, needing a new model and icons.

- **Model** — `Tools/Blender/GenerateChickenMeatModel.py` (new, follows the
  established headless-Blender pipeline), a classic drumstick silhouette:
  rounded meat mass (`ChickenMeat`, pinched teardrop sphere) plus a thin
  bone cylinder (`Bone`) with a knuckle sphere (`Knuckle`) protruding from
  it, distinct from Raw Meat's existing beef-slab look. Exported to
  `Assets/Models/ChickenMeat.glb`.
- **`PreyCreature.cs` gained a third loot slot** (`lootItemC`/min/max/
  chance) as a plain mirror of the existing A/B fields rather than a
  restructure into an array — the only creature that needs a third drop
  right now, and this way Chicken/Deer's existing scene data needed zero
  migration. Chicken's scene instance wired to drop it.
- **New icon-rendering gotcha found and fixed, on top of the already-
  documented "un-remapped embedded glTF material" one**: `ChickenMeat.glb`'s
  `.meta` had a materially correct `externalObjects` remap (`ChickenBone_mat`/
  `ChickenMeat_mat` → real extracted `Universal Render Pipeline/Lit` `.mat`
  assets) — `AssetImporter.GetExternalObjectMap()` even reported it
  correctly — but **glTFast's importer never actually substituted it on the
  instantiated renderers**, at any reimport tried (`ImportAsset(ForceUpdate)`,
  `importer.SaveAndReimport()`, even re-adding the remap fresh via
  `AddRemap(new SourceAssetIdentifier(embeddedMat), ...)`, the exact
  CLAUDE.md-documented pattern from the Berry Seed fix). Every instantiated
  copy kept resolving to the original embedded `Shader Graphs/glTF-
  pbrMetallicRoughness` material regardless — only caught by instantiating
  and checking `AssetDatabase.GetAssetPath()` on the actual assigned
  material, not by trusting the `.meta`/`GetExternalObjectMap()` alone (which
  both looked correct throughout). Symptom was subtle, not the usual fully-
  invisible case: that Shader Graph material happened to still render, just
  fully overexposed to flat white — nearly invisible against a light
  background, easy to misdiagnose as a camera-framing or material-color
  problem instead (two framing-angle bake attempts were tried first and
  both failed for this reason, not a framing reason). **The reliable fix
  ended up bypassing the importer's remap entirely**: `PrefabUtility.
  LoadPrefabContents` on the wrapper prefab, walk its renderers, assign
  `sharedMaterial` directly to the real `.mat` assets, `SaveAsPrefabAsset`
  — a real per-instance override on the wrapper prefab that doesn't depend
  on the `.glb`'s own import-time material resolution at all. Not yet
  confirmed whether this also silently affected the earlier Berry Seed fix
  (that one was only ever verified via `.meta` guid grep, never an actual
  instantiated-material check) — worth a follow-up look, logged in
  `BUGS_AND_ENHANCEMENTS.md`.
- Icons baked via `IconBaker.cs` at the default 3/4-from-above camera angle
  (the two angle-override attempts before finding the real cause turned out
  unnecessary once the material was actually correct).
- Verified via direct instantiated-material inspection + rendered-pixel
  read (not just a clean batch-mode log) and by reading the final baked
  icon images directly — not yet live-tested in Play mode.

## 2026-08-16 (14)

### v0.3.107-dev — Rabbit built, first real Prey Creature wander/flee AI

Ben sourced a Rabbit model individually (`Assets/Rabbits/`, not from the
shared `ithappy` pack — it doesn't include one) and asked to build it in
fully: material fix, gameplay wiring, and real movement, all in one pass.

- **Hit the exact same legacy-Built-in-shader-invisible-under-URP gotcha
  as the HumanDummy incident** — `Rabbit1.mat`/`Rabbit_Eyes.mat` used
  `fileID: 46` (Built-in Standard, recognizable by the all-zeros-plus-`f`
  guid convention). Converted to `Universal Render Pipeline/Lit`, reading
  the old shader's `_MainTex`/`_Color`/`_BumpMap` values first and writing
  them into `_BaseMap`/`_BaseColor`/`_BumpMap` explicitly rather than
  trusting a blind shader swap (same discipline the HumanDummy fix
  established — `GetColor`/`GetTexture` on a property the old shader
  never had silently returns a zeroed default, not an error).
- **New `RabbitAnimator.controller`** — built from the asset's own
  `Rabbit@Idle.fbx`/`Rabbit@Run.fbx` clips via `UnityEditor.Animations`
  (2-state Idle/Run blend on a `Speed` float, no exit-time so it reacts
  instantly). The source prefab shipped with an `Animator` component but
  no controller assigned at all.
- **New `PreyWander.cs` — the first real Prey Creature movement**,
  closing the "PreyCreature's movement half unbuilt" gap Chicken/Deer
  have carried since they shipped (`MVP2_PLANNING.md` item 8). Combines
  `NPCWander`'s flat-ground idle/wander shape and `NPCFlee`'s away-from-
  player shape into one state machine (a single creature needs both
  halves together, unlike those two components which live on different
  object types). Built generic, not Rabbit-specific — Chicken/Deer (or
  Pig, once sourced) can adopt the same component later once they have
  Idle/Run clips to drive; not retrofitted onto them this pass.
  `SkinnableCreature` gained a public `IsDead` accessor so this sibling
  component (not a subclass) can stop driving movement/animation once
  the creature dies, same check `HostileCreature` already does
  internally.
- **New `Rabbit.prefab`** — `PreyCreature` (maxHealth 10, Raw Meat ×1-2
  @ 100%, Knife-gated skinning, trains Gathering, same shape Chicken/
  Deer already use) + `PreyWander`. Model was already sensibly scaled by
  its own author (0.3 uniform, real-world bounds ~0.23×0.27×0.32m) and
  nearly grounded (3cm offset) — measured via the same "never guess an
  imported model's scale/pivot" diagnostic pattern this project always
  uses before placing one, confirmed no rescale was actually needed this
  time. Two instances placed in `TestScene.unity`.
- Pig is still the one open animal from the original 4 — Ben separately
  picked the LowPoly Pigs Pack (Red Deer, $20) as the best Asset Store
  candidate, not yet purchased; see `BUGS_AND_ENHANCEMENTS.md`.
- Verified via batch-mode compile + direct YAML grep (material shader/
  `_BaseMap` values, prefab component fields, Animator controller
  override, scene placement) only so far — not yet live-tested in Play
  mode.

## 2026-08-16 (13)

### v0.3.106-dev — NPC Guarding built, closing out MVP2 item 2

`GUARDING_PLANNING.md`'s design, built same day — the last unstarted
piece of MVP2 item 2 ("Expand NPC hiring"). Bigger than the other job
families: this is the project's first real NPC health/death system and
first NPC-initiated combat.

- **New `NPCVitals.cs`** — `IDamageable`, real health, **permanent**
  death (no respawn timer, unlike `SkinnableCreature`'s creature-kill
  shape) — clears the NPC's job (`NPCJob.ClearJob()`, same tool-loss-for-
  good convention `NPCHiring.Fire()` already uses) and destroys the
  GameObject. Slow out-of-combat regen, paused while `NPCGuarding.
  IsFighting`. Lives on every hired NPC (not just Guards), same
  always-present convention `NPCCrafting`/`NPCTraining` already use.
- **`HostileCreature` generalized to retaliate, not just chase the
  player** — previously hardcoded to a single `player` target with no
  way to redirect. New `RedirectAggro(Transform)` lets `NPCGuarding`
  provoke a Wolf onto itself after landing a hit; without this, a Guard
  could hit a Wolf forever and never take damage back, making "real
  health, can die" (Ben's own call) true in name only. Player-facing
  damage delivery (`PlayerVitals.Damage`) is byte-for-byte unchanged —
  the generalization only added a fallback through `IDamageable.
  TakeDamage` for a non-player target.
- **`NPCJobDefinition.JobKind` gains `Guarding`** — third sibling
  alongside `Gathering`/`Crafting`, same early-out-if-wrong-kind pattern.
- **New `NPCGuarding.cs`** — reuses `HostileCreature`'s own `Idle`/
  `Chasing`/`Attacking` state shape, retargeted at `HostileCreature`
  instances instead of the player, with a patrol state on top: circles
  the nearest placed Village Flag at that Flag's own `CraftTierScale.
  VillageFlagRevealRadius` (Ben's framing: "simulate patrolling around
  the village," direct reuse of the table built for the Player Map
  earlier the same session). No Flag placed → stands at its current spot
  instead (detection ring still active), same "nothing to do, don't
  crash" fallback `NPCCrafting`/`NPCTraining` already use. Attack
  resolution mirrors `PlayerCombat.ResolveAttack`/`PlayerRangedCombat.
  Fire`'s damage math (`WeaponDamageBonus`/`ArrowDamageBonus`/
  `BowDamageBonus`), just on a fixed cooldown instead of the player's
  manual draw-and-hold — an NPC ranged attack doesn't consume Arrow
  stock (the Arrow tool slot is a permanent equipped loadout, same as
  every other `NPCJob` tool, not a stack the way the player's own hand
  slot is).
- **Two `NPCJobDefinition`s under one shared "Guarding" family/skill** —
  `GuardMeleeJob` (Weapon: all 5 Knife tiers) and `GuardRangedJob`
  (Weapon: all 5 Bow tiers, Arrow: all 5 Arrow tiers). Split into two
  jobs rather than one, because `NPCJob.IsReady` requires *every*
  `toolRequirements` entry filled — a static per-job list can't
  conditionally drop the Arrow slot for a melee-only Guard. Both show
  under one "Guarding" tab in `NPCJobScreen` — zero UI changes needed,
  same family-tabs-then-job-tiles shape every other family already uses.
- **New `Guarding` `SkillDefinition`** (Combat category, same as
  Archery), trained per successful hit.
- **"Negative Fame players" as a target — explicitly not built.** Ben's
  own call: that targeting rule is about other players once multiplayer
  exists, not the single local player today. `NPCGuarding`'s threat scan
  is written generically enough (a dedicated `FindNearestThreat` pass
  over one hostile type) that adding a second hostile-target type later
  is an additive change, not a redesign.
- Talk (`NPCDialogue.BeginDialogue`) now also pauses `NPCGuarding`, same
  gap-closing fix already applied to `NPCCrafting`/`NPCTraining` when
  each shipped.
- **Real, not just theoretical, gap closed in passing**: `BUGS_AND_
  ENHANCEMENTS.md`'s "Fame: Kill NPC blocked on hired-NPC death system"
  entry is now half-true — the death system exists, but nothing yet
  distinguishes "the player killed this NPC" from "a Wolf did," so the
  actual Fame hook is still a separate follow-up, not built here.
- Verified via batch-mode compile + direct YAML grep (skill/job asset
  field values, prefab component wiring, `NPCJobScreen` family/job
  registration) only so far — **not yet live-tested in Play mode.** This
  is the riskiest untested feature of the whole session: real combat,
  real death, and a brand-new movement shape (circular patrol) all at
  once.

## 2026-08-16 (12)

### v0.3.105-dev — Village Flag is nameable, name shows on the Player Map

Ben's follow-up ask on the Village Flag work: it should be nameable like
a Storage Box, and the name should show on the Player Map.

- **`VillageFlag` now implements `IRenameable`** (`villageName`,
  default "Unnamed Village") — the existing `PlayerRenaming` right-click
  flow (raycast → `GetComponentInParent<IRenameable>`) picks it up for
  free, no new interaction code needed. Re-saved all 5 existing
  `VillageFlag_{Tier}.prefab`s so the new field is baked into their YAML
  explicitly rather than relying on Unity's "missing field uses the C#
  default" behavior for a field added after those prefabs were last
  saved.
- **`MapScreen` now draws a labeled marker for every placed Village
  Flag** — a small red marker plus its `DisplayName` in a text label
  above it, shown unconditionally (not gated by fog reveal, same
  reasoning the player's own position marker already gets — you
  obviously know where your own Flag is). Refactored the player marker's
  own world-to-map-pixel math into a shared `MapPointFor` helper rather
  than duplicating it for the new Flag markers.
- Verified via batch-mode compile + direct YAML grep (`villageName`
  present with its default in each re-saved prefab) only so far — not
  yet live-tested in Play mode.

## 2026-08-16 (11)

### v0.3.104-dev — City Statue gate built, Player Map Flag/Statue reveal hooks wired

Chunks 4 and 5 of the "Settlement Growth Loop" — the Village→City
progression gate (`VILLAGE_FLAG_PLANNING.md` section 6) and the Player
Map's own long-flagged "not yet wired" follow-up (`PLAYER_MAP_PLANNING.md`
section 1), built together since both hook into the same
`PlayerBuilding.Confirm` placement moment.

- **`BuildPiece` gained two independent gate flags**, mirroring
  `CraftingRecipe.requiresAnvilSurface`/`requiresFurnace`'s exact "opt-in
  bool most recipes don't need" shape: `requiresCityStatus` (a reusable
  gate — true means a `CityStatue` must already exist somewhere in the
  world, for whatever future City-tier structure needs one) and
  `requiresCityFoundingConditions` (the one-off check only the Statue's
  own piece sets — a Masterwork Village Flag placed **and** at least 10
  currently-hired NPCs, a live precondition checked at placement time,
  not a lifetime counter).
- **New `CityStatue.cs` marker** — permanent once placed (Ben's explicit
  call): `Exists` is a pure "does one exist right now" scan, nothing
  tracks *how* it got there, so firing NPCs back below 10 afterward
  doesn't revoke city status.
- **`PlayerBuilding.CanPlace` refactored into a new public `LockReason`**
  — returns *why* a piece is locked (skill level, missing City Statue,
  or unmet founding conditions) instead of a bare bool. Fixed a latent
  bug this refactor would otherwise have exposed: `BuildScreen`'s old
  warning label unconditionally read `piece.trainedSkill.skillName`,
  which would have NPE'd the moment a state-gated piece with no
  `trainedSkill` at all (the City Statue) was ever shown locked.
- **New `CityStatuePiece.asset`** (placeholder base+column prefab, real
  Blender model still open) — 20 Rock + 10 Iron Ingot + 5 Gold Ingot, no
  skill gate at all (purely state-gated, per the design doc's own
  framing). Placing it grants `PlayerFame.GrantCityStatue()` (+50,
  proposed, not yet Ben-confirmed as final) — `requiresCityFoundingConditions`
  doubles as the trigger for this, since only the Statue's own piece
  would ever set it.
- **Player Map reveal hooks wired**: `PlayerBuilding.Confirm` now calls
  `PlayerMapExploration.RevealCircle` for both a placed Village Flag
  (`CraftTierScale.VillageFlagRevealRadius(tier)` — a new per-tier table,
  35m Crude up to 75m Masterwork, total radius not additive on top of the
  25m walking base) and a placed City Statue (a flat 125m, since it's a
  one-time milestone, not a tier ladder). Closes the one open item
  `PLAYER_MAP_PLANNING.md` had left since the Map itself shipped
  (v0.3.98-dev): "the Village Flag itself is being built in a separate
  parallel pass, so the hook-up is a follow-up once that lands."
- Verified via batch-mode compile + direct YAML grep (`BuildPiece` field
  values, prefab component, `PlayerBuilding.allPieces` registration) only
  so far — **not yet live-tested in Play mode.** A real test needs a
  Masterwork Flag placed and 10 NPCs actually hired first, so this is a
  longer live-test setup than most items in this list.

## 2026-08-16 (10)

### v0.3.103-dev — Village Flag spawn loop built

Chunk 3 of the "Settlement Growth Loop" (`VILLAGE_FLAG_PLANNING.md`
sections 3-4) — the Village Flag's 5-tier recipe ladder (built earlier
the same session by a parallel pass) now actually does something: a
placed Flag draws in new hireable NPCs on a real timer.

- **New `VillageFlag.cs` marker** — added to all 5 existing
  `VillageFlag_{Tier}.prefab`s with the correct `CraftTier` baked in per
  prefab (each tier is already a genuinely different prefab from the
  earlier build, not one mesh scaled).
- **New `VillageFlagSpawner.cs`** on the Player — every
  `currentIntervalMinutes` (30 real minutes baseline, only accruing once
  at least one Flag is placed), spawns a fresh `NPCFactoryWorker`-shaped
  hireable NPC ~40m out from the strongest placed Flag and sends it
  walking in. Interval formula: `baseInterval / fameFrequencyMultiplier
  × flagTierMultiplier` — Fame divides (higher Fame = shorter wait),
  Flag tier multiplies directly (`CraftTierScale.
  VillageFlagIntervalMultiplier`, a new dedicated small table, Crude
  1.0x down to Masterwork 0.6x, deliberately restrained per this
  project's own "a ratio tuned for one quantity doesn't transfer to
  another" tier-scaling gotcha). With more than one Flag placed, the
  single highest-tier one drives the shared timer — multi-Flag balance
  was explicitly left undesigned, this is the simplest defensible
  reading.
- **`PlayerFame` gained a canonical `FameBand` enum + `Band`/
  `SpawnFrequencyMultiplier`** — the same 5-band table (Infamous 0.5x
  through Renowned 1.5x) `PlayerMenuScreen`'s Fame tile has displayed a
  label for since 2026-08-14, now the one source of those boundaries
  instead of two separate copies that could drift; `PlayerMenuScreen.
  FameBandLabel` was removed in favor of calling `fame.Band` directly.
  This is also the exact table `FAME_PLANNING.md`'s Traveling Trader
  visit-frequency design has been sitting on since 2026-08-14 waiting for
  a real spawn mechanism — now it has one, reusable once commerce exists.
- **New `NPCSeekFlag.cs`** on the hireable NPC prefab — walks toward a
  fixed point (reuses `NPCWander`'s move/ground-sample/face plumbing via
  a small local copy, same reuse shape `NPCFlee.cs` already established
  for its own move-away behavior), resumes ordinary wandering once it
  arrives at the Flag (unhired NPCs standing near a Flag behave exactly
  like any other pre-placed hire), and despawns if not hired within
  `stickAroundMinutes` — the *inverse* of the current spawn interval
  (`(baseInterval × baseStickAround) / currentInterval`, `baseStickAround`
  = the design doc's proposed 10-minute anchor, not yet Ben-confirmed as
  final). Lives permanently on the prefab (same always-present,
  gated-by-whether-it-was-triggered convention `NPCCrafting`/`NPCTraining`
  already use) — every pre-placed hire already in the world also carries
  it, harmlessly inert since nothing calls `BeginSeeking` on them.
- **Explicitly left open, matching the design doc's own flagged gaps**:
  exact Flag-tier multiplier numbers and the 10-minute stick-around
  anchor are both first-pass, tune-by-playtesting; an unhired NPC that
  times out despawns outright rather than rejoining the general world
  population (the design doc left this undecided — despawn needs no new
  system, so it's the pick here).
- Verified via batch-mode compile + direct YAML grep (per-prefab tier
  values, `NPCSeekFlag`/`VillageFlagSpawner` component wiring) only so
  far — **not yet live-tested in Play mode**, and note this needs a real
  multi-real-minute Play session to actually observe a spawn (the
  30-minute baseline is long for a quick manual check — worth a
  temporarily-shortened interval for the first live test pass).

## 2026-08-16 (9)

### v0.3.102-dev — NPC Training via Desk/Bookshelf built

Chunk 2 of the "Settlement Growth Loop" (`NPC_TRAINING_PLANNING.md`,
planned earlier the same day) — a hired NPC can now be sent to study a
skill book at a Desk, closing the loop skill books have been missing
since they shipped: writing a book previously had nothing to consume it
except the player's own one-time read.

- **Two new placeholder `BuildPiece`s**: Desk (4 Plank + 2 Stick) and
  Bookshelf (6 Plank), both Woodworking-trained, Crude tier — real
  Blender models still open, same "functional shape first, art later"
  convention this session's Village Flag recipes already used. `Desk
  Surface.cs` is a bare marker (mirrors `AnvilSurface`/`FurnaceSurface`
  exactly) `NPCTraining` walks the nearest one of.
- **Bookshelf is a flagged `StorageBox`, not a new component** —
  `StorageBox` gained an optional `restrictToSkillBooks` bool that
  computes its `Inventory`'s `restrictedTo` list from a live
  `ItemDatabase` scan at `Awake` (any `ItemDefinition` whose
  `worldPickupPrefab` carries a `SkillBook`), not a hand-authored item
  list — a new skill-book item is automatically allowed with no
  registration step, closing the exact registration-array risk
  `EFFICIENCY_AUDIT.md` already flagged for `ItemDatabase` et al. Reuses
  every bit of `StorageBox`'s existing rename/pickup/`InventoryScreen`
  auto-detection behavior for free. `ItemDatabase` gained a small public
  `AllItems` accessor to support the scan.
- **New `NPCTraining.cs`** — a one-shot interrupt (mirrors `NPCDialogue`'s
  Begin/End shape, not a continuous job loop like `NPCGathering`/
  `NPCCrafting`). Validates Desk availability *before* consuming the
  book (simpler and safer than discovering "no Desk in range" after an
  already-spent book has nowhere to walk to), consumes the book upfront,
  walks to the nearest Desk, waits 2 real minutes, then grants the
  recipe/lineage and resumes whatever job the NPC was already doing.
  Pauses `NPCWander`/`NPCGathering`/`NPCCrafting` for the duration, same
  `SetPaused` convention `NPCDialogue` already established — and closed
  a real gap in `NPCDialogue` itself while there: Talk previously only
  paused `NPCGathering`, leaving a Metalworking-assigned NPC free to keep
  crafting mid-conversation.
- **`NPCJob` gained a `grantedRecipes`/`knownLineages` bank** — mirrors
  `PlayerCrafting.bookGrantedRecipes`/`PlayerMagic.knownLineages` exactly,
  just on the NPC's own job/tool-state component. A crafting/weapon book
  grants the recipe as a standing exception `NPCCrafting.IsSatisfiable`
  now checks (`|| job.HasGrantedRecipe(recipe)`); a magic book banks the
  lineage inertly — NPCs have no spellcasting system at all today, so
  nothing reads it yet (Ben's explicit framing: forward compatibility,
  not a stub to apologize for — no bonus-level tracking either, since
  nothing would ever read a magnitude).
- **New `NPCTrainingScreen.cs`** — opened via a new "Train" button on
  `NPCHiringScreen` (a general NPC action independent of job assignment,
  not nested inside `NPCJobScreen`). Book picker reads from the player's
  main inventory *and* every `StorageBox` within 10m (a Bookshelf is just
  one such box — a book left in an ordinary box still counts too,
  matching the design's "shelving first isn't required" framing). A book
  that would grant nothing new (`NPCTraining.CanTrainWith`) is shown
  disabled with an "(already known)" label rather than offered as a real
  wasteful option.
- **`PlayerFame` gained `GrantNpcTraining()`** (+0.25, smaller than Hire's
  own +1 since this repeats far more often once a player has several
  NPCs and a steady book supply).
- **`NPCHiring.Fire()` now cancels in-progress training cleanly**
  (`NPCTraining.CancelTraining`) instead of leaving a fired NPC stuck
  mid-walk to a Desk with its other components paused forever — the
  already-consumed book is lost, not refunded, matching this project's
  consume-upfront-not-refunded convention everywhere else.
- Placeholder Desk + Bookshelf instances placed in `TestScene.unity` near
  spawn for easy testing, same precedent the "Prefab buildings" and
  found skill books already set.
- Verified via batch-mode compile + direct YAML grep (component wiring,
  `PlayerBuilding.allPieces` contents, placed-instance `PrefabInstance`
  blocks) only so far — **not yet live-tested in Play mode.**

## 2026-08-16 (8)

### v0.3.101-dev — NPC bench-crafting (Metalworking pilot) built

Chunk 1 of the "Settlement Growth Loop" (see the published design
artifact and `NPC_JOB_GENERALIZATION_PLANNING.md` section 7, planned
2026-08-16 earlier the same day) — an NPC can now be assigned to a real
crafting job, not just gathering.

- **`NPCJobDefinition` gained `JobKind{Gathering,Crafting}`**, default
  `Gathering` so `MineOreJob`/`ChopWoodJob`/`ForageJob` needed no data
  migration. `NPCGathering.Update()` gained a matching early-out so it and
  the new `NPCCrafting` can sit on the same NPC prefab without fighting
  over which one is actually driving.
- **New `NPCCrafting.cs`** — sibling to `NPCGathering`, not an extension
  of it. Player queues specific `CraftingRecipe`s per NPC (mirrors
  `Furnace.recipeQueue`/`ToggleQueue`/`MaxQueueSize` member-for-member,
  including its 4-slot cap — no depth number was specified for this queue
  specifically, so Furnace's own cap carried over rather than inventing
  one). Crafting is deterministic — no `CraftOutcomeRoll`, same
  unattended-automation precedent `SmeltableItem`/`CookableItem` already
  set. A recipe needing `requiresAnvilSurface`/`requiresFurnace` sends the
  NPC walking to the nearest qualifying surface first (same
  nearest-in-range scan `NPCGathering` uses for harvest targets);
  `requiresCanteenWater` recipes are excluded outright (NPCs have no
  Canteen concept). Materials/output flow through two player-assigned
  `StorageBox`es directly — no NPC cargo involved, since nothing is ever
  carried.
- **`PlayerNPCDeposit` generalized** from a hardcoded `NPCJob.
  SetDepositContainer` target to any `Action<StorageBox>` callback — the
  new `NPCCraftingScreen` needed the identical point-and-confirm
  targeting flow for its own materials/output box pickers, and
  duplicating that raycast/E-confirm loop for a second caller would've
  just been the same code twice. `NPCJobScreen`'s existing "Set Deposit
  Container" button is unaffected (now passes `job.SetDepositContainer`
  explicitly instead of the job itself).
- **New `NPCCraftingScreen.cs`** — opened from `NPCJobScreen`'s new
  "Manage Crafting Queue" button (shown instead of the Deposit Container
  section for `JobKind.Crafting` jobs, since there's no world node to
  deposit from on this path at all). Recipe list is family-scoped to the
  assigned job's own family, read directly off `PlayerCrafting.Recipes`
  — no new per-job recipe-list data needed. Each row shows a live
  Ready/Not ready indicator using the same four-way satisfiability check
  (`materials`/`tool`/`skill`/`space`) the loop itself uses, so the
  player can see *why* an NPC is idle instead of guessing.
- **Pilot: `MetalworkingJob.asset`** (new `NPCJobDefinition`, `kind =
  Crafting`, `toolRequirements = [Backpack]` copied from `MineOreJob`'s
  own Backpack requirement) — proves the walk-to-Furnace case against the
  existing `IronIngotRecipe` with zero new recipe data, per the planning
  doc's own pilot choice.
- **Real batch-mode gotcha hit and fixed along the way**: a first attempt
  at appending `MetalworkingJob` into the scene's `NPCJobScreen.jobs[]`
  array via `SerializedObject` reported full success (`ApplyModified
  Properties`/`SaveScene` both returned clean) but the saved YAML showed
  the new slot as `{fileID: 0}` — the asset reference had been loaded
  *before* the batch script's own `EditorSceneManager.OpenScene` call,
  which CLAUDE.md's existing stale-reference gotcha already warns can
  silently null out a reference used afterward, even with no prefab-
  content cycle involved. Fixed by reloading the asset reference *after*
  `OpenScene`, and by rebuilding the array clean (not append-only) to
  clear the stray null slot the first attempt had already saved.
- Sewing/Woodworking/Stonework/Carpentry/Forging/Minting as actually
  *assignable* crafting jobs are explicitly not built yet — the design is
  family-agnostic, so each is a data-only follow-up once this pilot is
  confirmed working live, not built speculatively ahead of that.
- Verified via batch-mode compile + direct YAML grep (component
  references, array contents, prefab wiring) only so far — **not yet
  live-tested in Play mode.**

## 2026-08-16 (7)

### ItemDefinition/BuildPiece assets now show their own icon as their Project/picker thumbnail (editor tool, no version bump)

traskmi caught this live in the new VMS browser: opening the object
picker for a recipe's Item field showed every item as an identical
generic ScriptableObject placeholder, no way to tell Iron from IronOre
at a glance.

- **`Assets/Editor/IconPreviewEditors.cs`** — `ItemDefinitionEditor`/
  `BuildPieceEditor`, two thin `CustomEditor`s overriding
  `RenderStaticPreview` to render each asset's own `icon` Sprite as its
  thumbnail, instead of Unity's default placeholder. Both types share
  the same `icon` field name (per `IconBaker.cs`'s own header comment
  noting it already wires both generically) so one shared helper
  (`IconPreviewUtility.RenderFromSprite`) covers both.
- **GPU blit + `ReadPixels`, not `Sprite.texture.GetPixels()`** — works
  regardless of the source PNG's Read/Write Enabled import setting
  (the baked icons aren't marked readable); same reasoning `IconBaker`
  itself renders via a `RenderTexture` rather than reading pixels off
  an arbitrary source directly.
- **Neither override touches `OnInspectorGUI`**, so this doesn't change
  VMS's detail pane at all — `Editor.CreateEditor` now resolves to
  these custom editors for Items/Build Pieces instead of a plain
  default `Editor`, but the base class's default `OnInspectorGUI()` is
  the same rendering VMS was already getting.
- An item/piece with no `icon` assigned returns `null` from the
  override, which is exactly what falls back to Unity's normal
  placeholder — no special-casing needed for in-progress/unfinished
  assets.
- Verified via a throwaway batch-mode script (`IconPreviewVerify.cs`,
  deleted after running, launched **without** `-nographics` since
  `Graphics.Blit` needs a real graphics device — same trap `IconBaker.cs`
  already documents): confirmed `Editor.CreateEditor` resolves to the
  new custom editor types, `RenderStaticPreview` produces a correctly-
  sized (32×32) texture for two known-icon items (Iron Ingot, Fried
  Egg), and a blank item with no icon returns `null` cleanly rather than
  throwing. **Cannot confirm from batch mode** that the Project window
  or object picker actually display the new thumbnail live — needs a
  human look in the Editor.

## 2026-08-16 (6)

### VMS admin browser: a tabbed Editor Window for Items/Recipes/Cookables/Skills/NPC Jobs/Build Pieces (editor tool, no version bump)

traskmi's ask, following up on the "central database" discussion earlier
this session — build the actual browser the backlog note in `CLAUDE.md`
promised, covering all 6 core data types in one pass.

- **`Assets/Editor/VmsTypeInfo.cs`** — a 6-entry descriptor table
  (`ItemDefinition`, `CraftingRecipe`, `CookableItem`, `SkillDefinition`,
  `NPCJobDefinition`, `BuildPiece`) plus a shared `LoadAll(Type)` scan —
  same `AssetDatabase.FindAssets($"t:{T}")` → `LoadAssetAtPath` pattern
  `DatabaseRepopulator.LoadAll<T>()` already uses, just `System.Type`-
  parameterized so the window can call it with a runtime-selected tab.
- **`Assets/Editor/VmsWindow.cs`** — `Gridless > VMS Admin Browser`. Tab
  strip across the 6 types, a search box (matches each asset's own
  filename — the one field guaranteed to exist on every type, and the
  same stable ID the database `Find()`/`IdFor()` system already keys
  on), a filtered list on the left, and a live detail pane on the right.
- **Detail pane deliberately uses Unity's own default inspector**
  (`Editor.CreateEditor(selected).OnInspectorGUI()`), not a hand-rolled
  per-type layout — renders every field type already in play (arrays,
  enums, nested `[System.Serializable]` classes, self-references)
  correctly for free and stays correct automatically if a field is
  added/renamed later. Gets real Undo (Ctrl+Z) support for free too,
  since `OnInspectorGUI()` calls `ApplyModifiedProperties()` internally
  — deliberately not the `...WithoutUndo()` pattern `IconBaker.cs`/
  `PickupPrefabBuilder.cs` use, since those are one-shot generators and
  this is a hand-editing tool.
- **No autosave** (matches the rest of the project): explicit Save
  button with a dirty asterisk, plus auto-save-if-dirty when switching
  tabs or selection so an edit-then-click-next flow doesn't lose work.
- **New-asset creation included in v1** — `New` button →
  `EditorUtility.SaveFilePanelInProject` (defaults into `Assets/Data/`,
  native overwrite-prompt) → `AssetDatabase.CreateAsset`. No fields
  auto-populated; filled in via the same generic detail editor as any
  edit.
- Items/Skills/NPC Jobs tabs show a one-line reminder to run
  `Gridless > Repopulate Databases` after adding a new asset — those 3
  types are indexed by `ItemDatabase`/`SkillDatabase`/`NPCJobDatabase`
  (see the regeneration-determinism fix earlier this session);
  Recipes/Cookables/Build Pieces have no such index, so no reminder.
- **No new data store** — per the backlog note this follows through on,
  VMS never touches `ItemDatabase`/`SkillDatabase`/`NPCJobDatabase`
  directly and doesn't create equivalents for the other 3 types. All
  reads are a fresh `AssetDatabase` scan (gone the instant `OnGUI`
  returns), all writes land on each asset's own `.asset` file — the
  actual data stays exactly as distributed/git-mergeable as it already
  was.
- **No version bump** — `Assets/Editor/**` isn't in this project's
  version-bump trigger list (`Assets/Scripts/**`, `Assets/Scenes/**`,
  `Assets/Prefabs/**`) and this is a dev-only tool with zero runtime
  effect, same precedent as `DatabaseRepopulator.cs` itself.
- Verified via a throwaway batch-mode script (`VmsVerify.cs`, deleted
  after running): confirmed a clean compile and exercised the exact
  scan/name-resolution logic the window uses for all 6 types —
  `Items=125, Recipes=62, Cookables=6, Skills=21, NPC Jobs=3,
  Build Pieces=25`, item/skill/job counts matching this session's
  earlier `DatabaseRepopulator` run exactly. **This only confirms the
  data layer** — actual window behavior (tabs, search, Undo, Save, New)
  needs a live Editor GUI pass; see `TEST_FEATURE_PLAN.md`.

## 2026-08-16 (5)

### v0.3.100-dev — Deterministic ItemDatabase/SkillDatabase/NPCJobDatabase regeneration + O(1) lookup

traskmi asked whether a "central database" for items/recipes would help
or hurt merge collisions between sessions. Investigating that question
surfaced a real, already-live bug rather than a hypothetical one — see
the new "ItemDatabase/SkillDatabase/NPCJobDatabase regeneration used to
be nondeterministic" gotcha in `CLAUDE.md` for the full writeup.

- **Root cause**: `DatabaseRepopulator` (mandatory before any commit
  adding a new `ItemDefinition`/`SkillDefinition`/`NPCJobDefinition`)
  rebuilds all three databases from an `AssetDatabase.FindAssets` scan
  every run, and the old `EditorSetItems`/`EditorSetSkills`/
  `EditorSetJobs` did a plain unsorted assignment. `FindAssets`
  enumeration order isn't stable across machines/runs, so two
  independent (correct) regenerations of the identical item set could
  produce two differently-ordered arrays — a full-array-reshuffle merge
  conflict with zero actual content difference.
- **Fix**: sort by the asset's own stable ID (`item.name`, same string
  `IdFor`/`Find`/save-load already key on) inside each `EditorSetX`
  before assigning. Verified fixed by running `DatabaseRepopulator`
  twice independently and diffing `ItemDatabase.asset` byte-for-byte —
  identical output both times (pre-fix, this diffed).
- **Also switched `Find(id)` from a linear `foreach` scan to a lazily-
  built `Dictionary<string, T>`** on all three databases (`ItemDatabase`,
  `SkillDatabase`, `NPCJobDatabase`) — fine at 125 items today, not
  something to leave as an O(n) scan while the item count keeps growing.
- Ran `DatabaseRepopulator.RepopulateAll` in batch mode after the fix
  and confirmed `Items=125 Skills=21 Jobs=3` (unchanged counts, as
  expected — this is a reordering/lookup fix, not a data change).
- **Backlog, not built this session**: keep item/recipe data as
  individual `Assets/Data/*.asset` files (the right git-merge unit for
  two people editing in parallel — don't inline into one blob). A future
  admin "VMS" browser for items/recipes should read this now-
  deterministic index to list everything, but write edits back through
  each item's own `.asset` file, not a new central data store.

## 2026-08-16 (4)

### v0.3.99-dev — Player Map explored state now survives save/load

Ben caught this live: explored the map, saved, reloaded, and the map had
reset to mostly fog — exactly the gap `PLAYER_MAP_PLANNING.md` and
`BUGS_AND_ENHANCEMENTS.md` already had flagged as known-missing, now
closed.

**`PlayerMapExploration.CaptureRevealedBase64()`/`RestoreRevealedBase64()`**
— bit-packed, not one bool per cell verbatim (the explicit ask when this
gap was first flagged): a 100×100 grid packs into 1,250 bytes regardless
of how much is actually revealed. Wired into `SaveManager.CapturePlayer`/
`RestorePlayer` alongside vitals/skills/inventory — this is per-player
state, not a `SaveId`-keyed world object.

**Caught and fixed a real gap in my own verification method, not the
game code**: a first-pass batch-mode diagnostic reported a false PASS —
`AddComponent<PlayerMapExploration>()` doesn't fire `Awake()`
automatically in pure edit-mode scripting (the same documented "Unity
lifecycle methods don't reliably fire in batch edit-mode" gotcha
`CLAUDE.md` already has for `Object.Instantiate()`/`OnEnable`, just a
new trigger for it — `AddComponent`, not `Instantiate`). The grid was
silently 0×0 on both sides, so capturing and restoring nothing "matched"
trivially. Fixed the diagnostic (not the real code) by invoking `Awake()`
via reflection, then got a genuine result: 666 cells revealed, captured,
restored, and confirmed identical cell-by-cell.

## 2026-08-16 (3)

### v0.3.98-dev — Player Map: fog-of-war core, plus a WorldBounds utility it needed first

Ben's ask, designed conversationally then built the same session (see
`PLAYER_MAP_PLANNING.md`).

**`WorldBounds.cs`** — a real prerequisite, not busywork: nothing in
this project had a "how big is the world" concept before now. Reads
`Terrain.activeTerrain`'s own position + `TerrainData.size` directly
(same "static utility, read by whoever needs it" shape `GroundHeight.cs`
already established), so it stays correct automatically if the Terrain
is ever resized or regenerated — including the future Terrain/hills
conversion already flagged in `BUGS_AND_ENHANCEMENTS.md`. Verified
against the real scene via a direct batch-mode check, not just
compiled: confirmed exactly 200×200 units (X/Z: -100 to 100), not the
"roughly 200×200" guess this was based on before.

**`PlayerMapExploration.cs`** — fog-of-war state. Splits the playable
world into a 2m-cell grid (a 200×200 world is 100×100 = 10,000 cells, a
trivial plain `bool[,]`, no need for a sparse representation at this
scale) and permanently reveals cells within 25m of the player every
frame. `RevealCircle(worldPos, radius)` is public and ready for a
Village Flag/City Statue to call once those exist (being built in a
separate parallel pass — see `WORKING_ON.md`) — no changes needed here
when that lands, just a new caller.

**`MapScreen.cs`** — `M` opens it, same open/close/cursor-lock shape
`GameMenuScreen`'s own backquote toggle already established. Renders
`PlayerMapExploration`'s grid as a fog texture (only rebuilt when
something's actually changed, tracked via a `RevealVersion` counter —
same "don't redo the work every frame" discipline `GardenPlot4x4`'s own
visual-stage update already uses) plus a player-position marker.
`GameMenuScreen.ControlsList` updated with the new binding, per this
project's own standing rule.

**Explicitly not built this pass**: Flag/Statue reveal hooks (ready,
just not wired — the Flag itself doesn't exist in the game yet) and
save/load persistence for explored state (a real follow-up, same
category of gap Skill Books had before its own save/load increment).
Verified via batch-mode compile + direct scene YAML grep only — not yet
live-tested in Play mode.

## 2026-08-16 (2)

### v0.3.97-dev — Fried Egg: Frying Pan's second recipe

traskmi's ask: 1 Egg in a Frying Pan, 30s, 5 Health + Hunger.

- **New low-poly Blender model** (`Assets/Models/FriedEgg.glb`, 178 tris —
  two meshes, a squashed-oval white + an off-center dome yolk, separate
  materials) via the established headless-Blender pipeline.
- **`FriedEggCookable.asset`**: 1x Egg, `requiredAccessory` = Frying Pan
  (same accessory-gate Steak and Potatoes already uses), 30s cook time,
  Cooking skill level 5 — matches Grilled Meat/Herbal Tea's tier of basic
  single-accessory recipes rather than Steak and Potatoes'/Meat Stew's
  higher-effort ones.
- **`FriedEggEdible.asset`**: 5 Health via `vital`/`restoreAmount`. Hunger
  is flagged, not an exact match — `EdibleItem` restores Hunger through a
  fixed 5-rung `FoodTier` ladder (15/25/40/60/90), and traskmi's requested
  20 sits exactly between two rungs. Shipped on `FoodTier.LightMeal` (25)
  rather than `Snack` (15) — a genuine coin-flip, easy one-line change if
  the other rung is preferred.
- **Ran `DatabaseRepopulator.RepopulateAll`** (new tool since this session
  last touched the repo, `EFFICIENCY_AUDIT.md`) — required after adding
  any new `ItemDefinition`, or save/load's `Find()`-based restore silently
  can't see it. Confirmed the new item's guid actually landed in
  `ItemDatabase.asset` before considering this done, not just that the
  tool logged success.
- Verified every new asset directly via saved YAML (ingredient/output
  guids, `cookDurationSeconds: 30`, `restoreAmount: 5`, `foodTier: 1`),
  not just the batch script's own log output.

## 2026-08-16 (1)

### v0.3.96-dev — Harvesting a crop has a chance to return a seed, and 7 pre-grown plots close the seed-sourcing gap for real

Ben's ask: for all 7 Garden Plot vegetables, add a seed chance on
harvest, then place real growing plants in the world so a fresh game
actually has somewhere to get that first seed from.

**New `CropDefinition.seedDropChance`** (0-1, default/set 0.3 — same
"flat chance, not guaranteed" convention as `WolfPelt`/`PreyCreature`'s
own loot-chance fields), rolled independently per harvested unit in
`GardenPlot4x4.TryHarvest` — on a hit, 1 seed lands in the
backpack-then-main-inventory, same placement priority the crop item
itself already uses.

**New `GardenPlot4x4.PreplantedCell` mechanism** — `cells` is a private,
runtime-only array (nothing about cell state was ever serializable), so
"already growing" couldn't just be authored into the scene directly.
Added a small `[SerializeField] PreplantedCell[]` (cellIndex/crop/count)
consumed once in a new `Start()` via the existing `RestoreCell()` save-
restore method, guarded on `SaveManager.SaveExists` — a loaded save
always wins, matching the "only apply to a truly fresh game" convention
`PlayerShirt`/`PlayerJeans`/etc.'s starting-gear guards already use.
**7 `GardenPlot4x4` instances placed scattered around `TestScene.unity`**
(one per crop, spaced ~14 units apart, clear of spawn/buildings/
Chicken/Deer), each pre-seeded with 7 already-`Ready` cells of that one
crop, count 5 — immediately harvestable on a fresh game, no Admin Spawn
required.

**Closes "Garden Plot seeds are Admin-Spawn-only" for real** (not via
the originally-envisioned `WildCarrotPatch`-style wild forage nodes —
Ben's call: reuse the existing plot/cell mechanic instead of building a
whole new standalone-wild-plant object type). Updated
`BUGS_AND_ENHANCEMENTS.md` accordingly.

## 2026-08-15 (18)

### v0.3.95-dev — Deer is live: killable/lootable via PreyCreature, and it finally answers "where does Leather come from"

Ben's ask: put the Deer model (`ithappy/Animals_FREE`) in the game, same
treatment as the Chicken — a loot table dropping Meat and Leather.

**New `Leather.asset`** — the first real source of Leather in the game,
closing a question `BUGS_AND_ENHANCEMENTS.md` has flagged as open since
2026-08-06 ("where Leather comes from — implies hunting/animals, which
don't exist at all yet"). Own real model
(`Tools/Blender/GenerateLeatherModel.py`, a folded/draped hide swatch —
subdivided plane + Solidify modifier + per-vertex jitter for an uneven
drape, distinct from Cloth's flat weave), pickup prefab, and icon via
the now-standard `PickupPrefabBuilder`/`IconBaker` pipeline.

**Deer placed in `TestScene.unity`** — same `PreyCreature` treatment as
Chicken: `MovePlayerInput` disabled (legacy Input Manager, incompatible
with this project's New-Input-System-only setup), `PreyCreature` added
in the same batch run as the initial `Instantiate()` (per the
established "don't add a component to an already-saved PrefabInstance
in a later run" gotcha). Loot: Raw Meat (2-4, guaranteed — the same
shared meat item every creature already drops) + Leather (1-2,
guaranteed), Knife-gated (all 5 tiers), trains Gathering.

**A real scale question, resolved by rendering a comparison, not
guessing.** The Deer's measured `Renderer.bounds` came back 2.29m
tall — taller than the 1.8m player, suspicious for a deer. Rendered it
next to a 1.8m reference cube via the same batch-render technique
`IconBaker` uses (a throwaway diagnostic script, deleted after) rather
than assuming either "the pack must be right" or "the bounds must be
right." The render showed why: edit-time (no `Animator` ticking, no
Play mode) leaves the model in an odd reared-up bind/idle pose — front
legs raised, neck thrown back, antlers up — which inflates the
Y-extent of a static bounds measurement without reflecting the actual
four-legs-on-the-ground standing height. `CreatureMover`'s own
`CharacterController`-based gravity/ground-snap corrects this at
runtime regardless of edit-time placement precision, same as it
already does for Chicken — no manual rescale applied, matching
Chicken's own precedent of trusting this asset pack's native scale.

**Still standing, no wander/flee AI** — same known limitation as
Chicken (`PreyCreature`'s movement half is unbuilt, see
`MVP2_PLANNING.md` item 8).

## 2026-08-15 (17)

### v0.3.94-dev — Meat Stew: Cooking Pot's first recipe, closes out MVP2 item 9's accessory-recipe gap

Ben's ask: Raw Meat + Potato + Carrot + Water, cooked in the Cooking
Pot — the one accessory still sitting with zero recipes after last
commit's Grill/Frying Pan/Kettle recipes.

**New merged model** (`Tools/Blender/GenerateMeatStewModel.py`) — a
copy of the Cooking Pot geometry with a flat broth disc and 4 visible
chunks (meat, potato, 2 carrot pieces) clustered on top, inside the
rim. Unlike Herbal Tea's lean-against-the-side placement, everything
here sits on top of the pot — visible from any horizontal camera
angle, so the Blender-Y/Unity-Z occlusion gotcha that bit Herbal Tea's
icon bake didn't apply; read correctly in both the standalone preview
and the real baked game icon on the first pass.

**`MeatStewCookable`**: Raw Meat x1 + Potato x1 + Carrot x1 + 20
Canteen Water, 50s (longest cook time yet — the most involved recipe),
`requiredAccessory` = Cooking Pot. **Gated at Cooking 25/skillGain
2** — above Steak and Potatoes' Cooking 15, reflecting it's the most
complex recipe built so far (3 ingredients + water, vs. Steak and
Potatoes' 2). `MeatStewEdible`: Feast tier (90 Hunger), registered in
`PlayerEating.edibles`.

This closes the "Cooking Pot has zero recipes" gap flagged after
v0.3.90-dev — all 4 Campfire accessories now have at least one real,
skill-gated recipe. MVP2 item 9's only remaining open piece is
wild-forage seed sourcing (still admin-spawn only).

## 2026-08-15 (16)

### v0.3.93-dev — Cooking skill/quality-tier system: the last open piece of MVP2 item 9

Designed (`COOKING_SKILL_PLANNING.md`, confirmed via AskUserQuestion)
and built same session. The `Cooking` `SkillDefinition` existed since
early in the project but was used by nothing — `Campfire` cooking was
100% deterministic, no skill gate, no risk, no growth.

**Decided:** binary success/fail, not a crafting-style tier ladder
(Ben's call — swapping in a whole different `ItemDefinition` per
outcome, like `CraftingRecipe.lowerTierItem`/`higherTierItem`, would
need 2-3 new items per dish; not worth it for cooking). Reuses
`CraftOutcomeRoll` directly (the same 5-outcome roll `PlayerCrafting`
and `PlayerWriting` already share) collapsed to two buckets — you get
the dish, or the (already-consumed) ingredients are wasted — plus a
mild Health hit (`Campfire.CookingFailureDamage = 5`, half crafting's
`SpectacularFailureDamage`) on the worst outcome only.

**`CookableItem`** gained `trainedSkill`/`skillGain`/
`requiredSkillLevel`, mirroring `CraftingRecipe`'s shape but with
`requiredSkillLevel` as a flat int rather than routing through
`CraftTierScale.SkillRequirement(outputItem.tier)` — food items don't
use the `CraftTier` ladder for this, and CLAUDE.md's own tier-scaling
gotcha already warns against reusing a scale tuned for one quantity
(crafting-quality tiers) on an unrelated one (cooking difficulty).
`trainedSkill == null` skips the roll entirely and always succeeds,
same "opt-in" convention crafting's skill-less gadget recipes already
use — `RawMeatToCookedMeatCookable`, the original baseline recipe, is
deliberately untouched by this system, staying exactly as free and
risk-free as it's always been.

**`Campfire.cs`** gained `HasRequiredCookingSkill()` (mirrors
`PlayerCrafting.HasRequiredSkill`, wired into both
`GetAvailableRecipes()` and `StartCooking()` — an under-leveled recipe
doesn't even show as an option) and `ResolveCookingOutcome()` (mirrors
`PlayerCrafting.ResolveOutcome`, called from `TickCooking()` once the
timer finishes rather than at `StartCooking` time — ingredients are
already consumed upfront either way, same convention as crafting). A
new `LastCookMessage`/`ShowCookMessage` pair gives `Campfire` a small
result toast — it has no `OnGUI` of its own (unlike `PlayerCrafting`,
which draws its own directly), so `CampfireScreen.DrawRecipeSection()`
reads and renders it instead.

**Tuning** (Ben confirmed the planning doc's proposed numbers as-is,
no changes): Grilled Meat and Herbal Tea both Cooking 5/skillGain 1.0;
Steak and Potatoes Cooking 15/skillGain 1.5 (a more involved
two-ingredient sear, higher gate). Verified via compile + direct YAML
grep of all 3 recipes' new fields — not yet live-tested in Play mode.

## 2026-08-15 (15)

### v0.3.92-dev — Steak and Potatoes + Herbal Tea: Frying Pan/Kettle get their first recipes, Cooking gains a real water mechanic

Two more Ben-requested recipes, closing out both remaining Campfire
accessories.

**Steak and Potatoes** (Frying Pan) — Raw Meat x1 + Potato x1, 45s,
Feast tier (90 Hunger — a full combined meat+starch meal, a step above
Grilled Meat's HeartyMeal). **Got its own real merged model
(`Tools/Blender/GenerateSteakAndPotatoesModel.py`)** — a pan with a
seared steak slab (two dark sear-mark bars) and a potato, replacing the
Cooked-Meat-reused placeholder it initially shipped with; Ben asked for
the merge explicitly mid-build.

**Herbal Tea** (Kettle) — Herb x1 + Water, 20s, Snack tier but restores
30 Thirst directly (a real drink, not a meal). **This is the first
`CookableItem` to need water**, which didn't exist as a cooking
mechanic at all before now — `CraftingRecipe.requiresCanteenWater`/
`canteenWaterAmount` existed for ordinary crafting (Healing Paste) but
`CookableItem` had no equivalent. Ported the same pattern:
`CookableItem` gained the same two fields, `Campfire.cs` gained
`HasCanteenWater()`/`FindPlayerCanteen()` (mirroring
`PlayerCrafting`'s own versions) wired into both `GetAvailableRecipes()`
(gates whether the recipe shows as available) and `StartCooking()`
(consumes the water on commit, same "not refunded" convention
`PlayerCrafting.StartCraft` already established). **Model**
(`Tools/Blender/GenerateHerbalTeaModel.py`) is a copy of the Kettle
geometry with the existing `Herb.glb` model leaned against its base,
per Ben's explicit ask — imported directly rather than hand-rebuilt,
scaled off its longest in-plane dimension (it's a ~3mm-thick flat leaf,
so naively normalizing off its height first blew it up to nonsense
size) and rotated with both an X and Y component (a pure single-axis
tilt left it edge-on/invisible from some angles).

**Real bug, caught by looking at the actual baked icon, not trusting a
clean batch log**: the herb initially baked into the icon as
completely invisible despite rendering fine in this script's own
preview camera. Root cause — **Blender's Y axis becomes Unity's
*negative* Z on glTF import**, and `IconBaker`'s fixed default camera
sits at `(+X, +Y, -Z)` looking toward `+Z`; the herb's Blender-space
`-Y` offset became Unity `+Z`, the *far* side of the kettle from that
camera, fully occluded behind the body. Fixed by flipping the offset
sign, verified by re-baking and looking at the actual icon PNG again
— exactly the "don't trust the log, check the real render" discipline
CLAUDE.md's other model-placement gotchas already establish, just a
new specific failure mode (an axis-convention flip across the Blender→
Unity import boundary, not a bounds/grounding issue).

Both new `EdibleItem`s registered in `PlayerEating.edibles` (the
separate manually-curated array from `GrilledMeatEdible`'s registration
last commit — caught missing this step for Steak and Potatoes too
before it shipped, same gap class).

## 2026-08-15 (14)

### v0.3.91-dev — Grilled Meat: the first Grill-accessory-gated Campfire recipe

Ben's ask: a recipe requiring 1 Herb + 1 Meat. New `GrilledMeat.asset`
(reuses Cooked Meat's model/icon as a placeholder, same convention
already documented for Cooked Meat itself) + `GrilledMeatEdible.asset`
(HeartyMeal tier, 60 Hunger — one step up from Cooked Meat's Meal/40,
reflecting the extra ingredient/accessory) + `GrilledMeatCookable.asset`
(Herb x1 + Raw Meat x1, 40s, `requiredAccessory` = Grill).

This is the first `CookableItem` to actually exercise the accessory-
gating path built back in v0.3.30-dev — `RawMeatToCookedMeatCookable`
was the only other one, and it's open-flame/no-accessory. Appended
directly to `Campfire.prefab`'s `cookableItems` array (that field lives
on the prefab asset itself, not a per-instance scene override, so
every Campfire placed from here on picks it up for free) and to
`PlayerEating.edibles` — a second, separate manually-curated array
(`EFFICIENCY_AUDIT.md` item 1, not yet covered by `DatabaseRepopulator`)
that would have left the new item silently uneatable if skipped, same
class of gap the audit already flagged. Verified via direct YAML grep
of both arrays' tails, not batch-log trust.

## 2026-08-15 (13)

### v0.3.90-dev — Campfire cooking accessories: Grill/Cooking Pot/Kettle/Frying Pan get real models, icons, and recipes

Closes the one open gap `CAMPFIRE_PLANNING.md` flagged after the
v0.3.30-dev cooking rebuild — the 4 accessory `ItemDefinition`s
(`grillSlot`/`cookingPotSlot`/`kettleSlot`/`fryingPanSlot`) had a fully
working slot/recipe-gating structure but no model, icon, or recipe.

**New Blender models** (`Tools/Blender/GenerateCookwareModels.py`) — a
grate-on-legs Grill, a two-handled Cooking Pot, a spouted/handled
Kettle, and a long-handled Frying Pan, all built from primitives (no
bezier work needed this time) and sized against the 1.8m player
reference (Grill ~0.32m across, Cooking Pot ~0.23m diameter, Kettle
~0.17m, Frying Pan ~0.37m tip-to-tip). All 4 read correctly on the
first render pass, no fix-up bugs this time.

**`PickupPrefabBuilder.cs`'s first real use — caught and fixed a real
bug in it.** Built last commit but never exercised end-to-end until
now: `BuildAndWire`'s `wireItem` step computed the `ItemDefinition`'s
asset path via `AssetDatabase.GetAssetPath(itemAsset)` *after*
`PrefabUtility.SaveAsPrefabAsset` had already run — another instance of
CLAUDE.md's stale-reference gotcha (asset references can go stale
across a prefab-content-saving operation, same family as the
`LoadPrefabContents`/`UnloadPrefabContents` case already documented
there), so `itemAsset` was already stale by that point and
`GetAssetPath` silently returned an empty string, throwing on the
re-fetch. Fixed by capturing the path up front, before any prefab
operations run. All 4 pickup prefabs + icons (via `IconBaker`, reused
unmodified) built cleanly once fixed.

**Recipes — proposed for approval before building, per Ben's explicit
ask, not built unilaterally.** Approved as proposed: Grill 2x Iron
Ingot, Cooking Pot 3x Iron Ingot, Kettle 2x Copper Ingot, Frying Pan 2x
Iron Ingot, all `requiresAnvilSurface` (same pattern as
`NailRecipe`/`RudimentaryShovelRecipe`). **Building them caught a second
real bug, this time a wrong assumption of my own**: the first attempt
set `trainedSkill` to `Assets/Data/Smithing.asset`, which — despite the
name — is actually a `GuildDefinition`, not a `SkillDefinition` (its
`m_EditorClassIdentifier` reads `GuildDefinition`, not
`SkillDefinition`), so `AssetDatabase.LoadAssetAtPath<SkillDefinition>`
silently returned null and every recipe's `trainedSkill` landed as
`{fileID: 0}` with no compile error. Caught by grepping the saved
recipe YAML directly rather than trusting the batch log's "DONE"
message — same discipline as every other silent-failure gotcha in
`CLAUDE.md`. The real skill is **Forging** (`Assets/Data/Forging.asset`)
— an existing `SkillDefinition` and `CraftingScreen` discipline tab
that had zero recipes using it until now, the correct home for
forged-metal cookware as distinct from `Metalworking` (ore→ingot
smelting) or `Stonework` (stone tools). Fixed by deleting the 4
misconfigured recipe assets, stripping the resulting null slots out of
`PlayerCrafting.recipes`, and rebuilding against Forging — verified via
direct YAML grep of both the recipe assets' `trainedSkill` field and
the scene's `recipes` array tail (exactly 4 new guids, no stray nulls).

## 2026-08-15 (12)

### v0.3.89-dev — Efficiency audit: SkinnableCreature base class, PickupPrefabBuilder tool, orphaned items resolved

Ben asked for a full codebase efficiency pass ("look for opportunities
for making efficiencies"); this works through the resulting
`EFFICIENCY_AUDIT.md` priority list (items 1–2, database auto-populate
and `HandSlots` consolidation, already landed in the prior two commits).

**`SkinnableCreature.cs`** — new shared base class extracted from
`HostileCreature.cs`/`PreyCreature.cs`, which had grown ~90 near-
identical lines between them (Awake/TakeDamage/Die/Complete's tool-
gate-skill-XP-respawn flow/HasAnyRequiredToolInHand/Respawn/
SetVisible) since being built four hours apart the same night. The
base owns health, the dead/alive lifecycle, the tool-gated hold-to-
skin interaction, and respawn; each subclass only supplies its own
`DropLoot()` shape (`HostileCreature`'s Pelt+Meat chance-based drop vs.
`PreyCreature`'s two independent loot slots) plus, for `HostileCreature`
only, its own `AIState{Idle,Chasing,Attacking}` state machine (renamed
from the old conflated `State{Idle,Chasing,Attacking,Dead}` enum — the
Dead half is now the base's `isDead` bool). Every `[SerializeField]`
config field kept its exact original name, so Wolf.prefab's and the
placed Chicken's already-serialized values needed no migration —
confirmed by grepping both directly for the actual field values
post-refactor, not by trusting a clean compile.

**`PickupPrefabBuilder.cs`** (new permanent `Assets/Editor/` tool,
alongside `IconBaker.cs`/`SceneAutoOpen.cs`/`PrefabBuildingPlacer.cs`)
— every one of the project's 83 `*Pickup.prefab` files so far was built
by a bespoke throwaway script re-deriving the same instantiate-measure-
ground-add-BoxCollider-add-Rigidbody-add-Pickup-save sequence from
scratch, which is also where several real bugs originated this session
(the Stone Arrowhead's arbitrary 2.4x oversizing, a crop pickup's VFX-
rig bounds contamination). This consolidates that logic into one
reusable batch-mode tool.

**Orphaned items resolved** — `MediumRock.asset` (dead since
`MediumRockChunk.prefab` switched `Pickup`→`ResourceNode` in
v0.1.90-dev, no other references anywhere) deleted outright, per Ben's
call. `SoccerBall.asset` (registered in `ItemDatabase` with an icon and
a `worldPickupPrefab` reference, but genuinely unreachable — its target
prefab, the existing kickable-toy `SoccerBall.prefab`, had no `Pickup`
component at all, so it could never actually enter inventory) wired in
for real: added a `Pickup` component to the existing `SoccerBall.prefab`
(coexists fine with its `SoccerBall.cs` kick behavior — pick it up if
you want it in inventory, kick it if you don't) plus a new
`SoccerBallRecipe.asset` (Cloth ×3, Sewing skill) appended to
`PlayerCrafting.recipes`.

`DatabaseRepopulator` re-run after both changes: `ItemDatabase` Items
120→119 (MediumRock dropped).

## 2026-08-15 (11)

### v0.3.88-dev — Closing the archery gaps: icons, flying arrow, draw UI, aim zoom, real draw animation

Ben asked what was still missing from Bow & Arrow; this closes every
gap that was actually buildable tonight.

**Icons** — baked for all 11 new items (Stone Arrowhead, 5 Bow tiers, 5
Arrow tiers) via the existing `IconBaker` tool, same as every other
icon pass this session.

**New `FlyingArrow.cs`** — the fired shot now has a visible flight
instead of a silent invisible hit. Purely cosmetic (the hitscan raycast
already resolved the hit instantly, same as `PlayerCombat`'s punch) —
one shared visual reused for every Arrow tier, matching the existing
choice not to give arrows per-tier visual variation. Verified via
render that its orientation lines up with the travel direction.

**Draw-progress UI + aim zoom** — added directly to
`PlayerRangedCombat.cs`: a simple `OnGUI` progress bar while drawing,
and a camera FOV lerp toward `zoomFOV` (45°) while drawing, back to
normal on release.

**Real draw animation — corrected after Ben caught a wrong assumption.**
Said outright that no archery animation existed; Ben pushed back
("I thought our animation pack had archery animations") and was right —
a full `HumanF@BowShot01`/`HumanM@BowShot01` Load/Hold/Release set
(plus Idle/Damage/Death/Parry variants) was sitting in the existing
Human Animations pack the whole time, just not searched for specifically
enough. Wired a full-body Load→Hold→Release state swap into both
`PlayerAnimatorFemale.controller` and `PlayerAnimatorMale.controller`
(Ben's call over a masked upper-body layer — much lower risk to build
blind via batch scripting, reasonable since drawing-while-sprinting
isn't a real case here). New `PlayerBodyModel.ActiveAnimator` property
exposes the currently-active gendered Visual's Animator so
`PlayerRangedCombat` can drive `IsDrawingBow`/`ReleaseBow` directly.
**Known limitation, not hidden**: the Release state always exits back
to `StandingIdle` specifically, not whatever stance the player was
actually in before drawing (Kneeling, say) — a masked layer wouldn't
have this problem, but per-stance return transitions needed more
Animator complexity than felt justified for a first pass built with
zero visual preview available.

**Explicitly still not built, and correctly so** — checked before
claiming anything: this project has zero audio infrastructure anywhere
(`AudioSource`/`AudioClip` are used nowhere in `Assets/Scripts`), so
sound effects would mean inventing a whole new system from scratch, not
closing a bow-specific gap. NPC archery is really the unstarted
"Guarding" NPC job family, a separate feature. Neither was faked.

Verified via compile + direct YAML grep of every new state/parameter/
transition/motion-clip reference. **The Animator wiring specifically
still needs a real Play-mode look** — transition timing (`exitTime`
values) are reasonable guesses against clip length, not tuned by eye,
and batch mode fundamentally can't preview whether a state machine
actually reads right in motion.

## 2026-08-15 (10)

### v0.3.87-dev — Chicken is now actually killable: Feather/Egg loot + first Prey Creature

Ben's ask: "let's add [Feather/Egg] to the chicken loot table... when we
kill a chicken, we get crafting materials." The Chicken placed earlier
tonight was purely decorative — no combat/death behavior existed for it
at all — so this needed a real component, not just items.

Two new from-scratch Blender models (Feather, Egg), both player-scale-
checked. One real modeling bug hit along the way: the Feather's initial
two-piece vane+quill assembly had a placement bug (the quill ended up
disconnected and misaligned from the vane) — fixed by simplifying to a
single tapered blade shape instead of fighting the alignment, a better
trade for an item this small anyway.

**New `PreyCreature.cs`** — killable and lootable via the same tool-
gated hold-to-skin/respawn shape `HostileCreature` already proved out
for the Wolf, deliberately stripped of every aggressive behavior (no
detection/chase/attack state machine). Built generic/reusable, not
Chicken-specific, so Pig/Deer/Rabbit can use the same component later —
this is explicitly *not* yet the full Prey Creature archetype the
Hunting Expansion design calls for (idle/wander until approached, then
flee); that movement behavior still doesn't exist. The existing Chicken
instance in `TestScene.unity` was deleted and re-placed fresh with
`PreyCreature` configured in the same batch run, rather than adding the
component to the already-saved instance — that exact pattern (new
component on an existing PrefabInstance, added in a later separate run)
already failed twice tonight for the old GardenPlot's SaveId migration.
Also caught a real `GameObject.Find` name-collision risk before it could
bite: `Chicken_001.prefab` has 3 objects sharing that exact name (root +
child), so locating the existing instance used a parent-null Transform
scan instead of `GameObject.Find`, per this project's own established
gotcha.

Chicken now drops 1-3 Feather + 1 Egg (both guaranteed) when killed and
skinned with a Knife, training Gathering — matching Wolf's own skill
convention (confirmed via its prefab, not assumed). Verified via
compile + direct YAML grep, including confirming the loot-item guids
match Feather/Egg exactly and that exactly one Chicken instance exists
in the scene (no duplicate left behind from the delete-and-replace).
**Not yet live-tested in Play mode.**

## 2026-08-15 (9)

### v0.3.86-dev — Bow & Arrow built (MVP2 item 8, ranged combat)

The full "Hunting Expansion" design doc turned into real, working content
in one pass: 3 new from-scratch Blender models (Bow — a bezier-curve
stave + string, 1.31m; Stone Arrow — shaft/tip/fletching, 0.66m; Stone
Arrowhead, 5cm standalone), all player-scale-checked and grounded before
use. Two real modeling bugs hit and fixed along the way: a group-rotation
step that spun each arrow piece around its own individual origin instead
of a shared pivot (the arrowhead and fletching flew apart from the
shaft — fixed by recognizing the arrow was already built lying flat from
construction, no extra rotation needed at all), and a hand-authored
bezier bow curve whose alternating handle directions produced an S-wiggle
instead of a single outward bow arc (fixed by switching to Blender's
`AUTO` handle type across 5 co-curving points). The standalone Arrowhead
was also initially scaled 2.4x too large (11cm vs. a real 3-5cm knapped
flake) from an arbitrary "make it visible" guess rather than a checked
reference — caught and corrected to 5cm.

**New items/recipes**: Stone Arrowhead (2 Rock → 1, Stonework). Bow —
full 5-tier ladder exactly mirroring Knife's shape (2 Stick + 1 Rope per
tier, same cost at every tier, `lowerTierItem`/`higherTierItem`
cross-linked for `CraftOutcomeRoll`), Woodworking, one hand slot (no
two-handed equip system — Ben's "what if we were lazy" pivot: the OTHER
hand holds whichever Arrow tier you want, doubling as ammo selection
with zero new equip plumbing). Stone Arrow — 5 *parallel* recipes gated
by which Trimmed Stick tier you feed in (not skill), deterministic
output (no roll) — 1 Arrowhead + matching-tier Trimmed Stick → 5 arrows,
Woodworking. `ItemDefinition` gained `isRangedWeapon`/`isArrow` flags,
mirroring `isMeleeWeapon`'s exact shape.

**New `PlayerRangedCombat.cs`** — hold-left-click-to-draw, release-to-
fire, sibling to `PlayerCombat` rather than folded into it (charge-and-
release is a fundamentally different shape from melee's instant tap).
`PlayerCombat` now bails out of punching whenever a Bow is held, so the
two scripts never both react to the same click. Full formula: draw
ramps over 1.2s, Strength caps max draw (`0.5 + 0.5×Str/100`), damage is
`(Random(2,4) + arrowBonus + bowBonus) × drawFraction`, range is
`25m × drawFraction`, accuracy is a random angular spread cone
(`±8°` Crude down to `±0.3°` Masterwork) further tightened by Dexterity,
cooldown is `0.5s × (1 − Dex/100×0.5)`. Arrow/Bow damage bonus tables are
deliberately their own dedicated scales, not reused from melee's
`WeaponDamageBonus` — same "a ratio tuned for one quantity doesn't
transfer to another" reasoning as Encumbrance vs. capacity.

**New `Archery` skill** (Combat category, matching Melee/Bare-handed),
trained per shot.

Also placed a `Chicken_001` (ithappy Animals_FREE, Ben's new asset pack)
into `TestScene.unity` — real animated model, `MovePlayerInput`
component disabled (drives via the legacy Input Manager, which this
project's `activeInputHandler: 1` doesn't support). No AI behavior yet;
the Prey Creature archetype it's meant to demonstrate isn't built.

Verified via compile + direct YAML grep of every cross-reference
(recipe tier cross-links, material overrides confirmed genuinely
distinct per Bow tier, skill wiring) plus two real renders (5-tier Bow
wood-color progression, and the full Bow/Arrow/Arrowhead set together).
**Not yet live-tested in Play mode** — the actual draw/fire feel,
Strength/Dexterity scaling, and hitting a real target are all still
unconfirmed live.

## 2026-08-15 (8)

### v0.3.85-dev — Garden Plot save/load persistence + a real SaveId collision bug fixed

Ben's explicit ask from earlier tonight: "we're going to need to fix
that" (Garden Plot growth state resetting on every reload). Both
`GardenPlot` (single-plot POC) and `GardenPlot4x4` now capture/restore
via the exact same `SaveId` + `CaptureWorldObjects<T>`/
`RestoreWorldObjects<T>` generic pattern `SaveManager.cs` already uses
for `StorageBox`/`ResourceNode`/`NPCHiring` — full per-cell state (crop,
seed count, elapsed grow time) for all 16 cells, reconstructing each
cell's timer against the new session's `Time.time` on load.

**Also rebuilt `ItemDatabase.asset` from a stale 85 items to 107** —
confirmed via guid grep that none of last night's 13 new crop/seed items
were registered in it (a manually-curated list, not auto-discovered),
which would have silently broken `ItemDatabase.Find()` on Garden Plot
restore despite `IdFor()` working fine on capture. The extra 22 beyond
just tonight's 13 means other items were already missing before this —
this fixes NPC tool save/restore too, not just Garden Plot.

**Real, pre-existing bug found and fixed at the root while building
this — needs live verification.** `RequireComponent(typeof(SaveId))`'s
auto-add only runs `Reset()` once per loaded prefab *template* per
session, not once per placement: confirmed live via two freshly-
instantiated `GardenPlot4x4` clones reporting the identical GUID.
`SaveIdRegistry.Register` silently overwrites on collision, so **every
instance of the same placeable built in one session likely shared one
SaveId** — only the last-registered one would ever restore correctly;
every earlier one (2nd StorageBox, 2nd Garden Plot, ...) silently comes
back empty on load, no error at all. Not new to Garden Plot — affects
`StorageBox` too, and may have already cost saved data before tonight.
Fixed in `SaveId.cs` itself: `OnEnable` now detects if its current id is
already claimed by a different live instance and regenerates before
registering — self-healing, protects every current and future `SaveId`
user without touching each placement call site individually. Could not
be confirmed in batch mode (`OnEnable` doesn't fire for `Instantiate()`
calls from pure edit-mode batch scripting — spent real time chasing this
before recognizing it as an edit-mode-only quirk, not representative of
real Play-mode gameplay) — architecturally sound, but needs a real
Play-mode test: build 2+ of the same placeable, save, reload, confirm
both restore their own contents.

One loose end, low-stakes: the single pre-existing single-plot
`GardenPlot` already sitting in `TestScene.unity` (near 4,-4, from an
earlier session) couldn't get a `SaveId` retrofitted via batch script —
two different approaches both failed to persist the added component to
the saved scene file. Not investigated further since it's a leftover
test object from the now-superseded single-plot POC; `CaptureWorldObjects`
just silently skips it, same as before this fix. Every *new* Garden Plot
built from now on gets a working `SaveId` automatically.

## 2026-08-15 (7)

### v0.3.84-dev — Garden Plot live-test follow-ups: seed-count display, Admin Spawn quantity, real icons

Four small fixes/additions from Ben's live Garden Plot testing session:

- **Garden Plot save/load gap logged as backlog** (`BUGS_AND_ENHANCEMENTS.md`)
  — confirmed via grep that growth state isn't persisted anywhere;
  Ben's explicit call: "we're going to need to fix that." Not built this
  pass, just tracked.
- **Remaining-seed-count now shown in `GardenPlotScreen4x4`'s context
  panel** for a Growing/Ready cell — root cause of live confusion
  ("I put a pack of 10 in, it just vanished"): planting silently
  consumes the whole stack upfront and tracks the remainder purely
  internally (`GardenPlot4x4.GetSeedCount` existed but was never
  actually displayed anywhere). Turned out to be expected behavior once
  investigated, not a bug — the real issue was Admin Spawn only ever
  granting 1 unit per click (see below), so the "packet" being tested
  only ever had 1 seed in it to begin with.
- **Admin Spawn gained a Quantity field** (`AdminSpawnScreen.cs`) —
  previously every Spawn click granted exactly 1 unit with no way to
  request more, forcing 10 separate clicks to test a real 10-seed
  packet. Garbage/empty/non-positive input falls back to 1.
- **Real icons baked for all 13 new items** (7 seed packets + 6 crop
  pickups) via the existing `IconBaker` tool — both the small inline
  icon and a 128x128 preview, wired directly onto each `ItemDefinition`.
  Verified by actually opening the generated PNGs, not just trusting the
  batch log (`IconBaker`'s own doc flags `-nographics` as a silent-
  failure trap for exactly this kind of run).

## 2026-08-15 (6)

### v0.3.83-dev — Real crop pickup visuals (Carrot/Potato/Turnip/Ginger/Sweet Potato/Onion)

Closes the gap flagged in the previous Wild Harvest entry: all 6 crops
covered by the pack now have a real `worldPickupPrefab` instead of the
generic gray dropped-item cube, built from each crop's own `Bunch`
model (Onion's is `P_OnionBunch.prefab`, which lives outside the
`Plants/` folder where the numbered ones are — missed on first pass,
found by Ben asking to look at it directly).

Real bug hit and fixed mid-build: every Bunch source model ships with a
hidden "harvest spawn" VFX rig (`TrailRenderer` + `ParticleSystem`,
driven by the pack's own `BunchAnim.cs` demo-scene script) whose bounds
don't shrink proportionally with the rest of the mesh under scaling —
harmless at the milder scale factors most crops needed, but it
completely dominated Onion's collider size at its much steeper 0.128×
scale-down (a correctly-scaled ~0.07m onion ended up wrapped in bounds
still reporting ~0.39m tall). Caught by actually rendering the result,
not just trusting the measured numbers — same "don't trust a passing
check alone" discipline as this project's other embedded-asset gotchas.
Fixed by stripping the VFX rig (a demo-scene flourish a static inventory
pickup doesn't need anyway) before measuring or building the final
prefab. A follow-up fix was needed too: the first strip attempt deleted
the VFX rig's *parent* GameObject, which turned out to also be the real
mesh's parent — took the mesh down with it. Corrected to remove only the
actual `TrailRenderer`/`ParticleSystem` GameObjects and just the
`BunchAnim` component, leaving the mesh's real parent intact.

Each crop scaled independently to a real-world-ish target height (Carrot
0.30m with greens, Potato 0.10m, Turnip 0.28m, Ginger 0.14m, Sweet Potato
0.18m, Onion 0.07m) — same player-relative-scale discipline as every
other imported model in this project, not assumed from the source pack.
Verified via a side-by-side render of all 6 (correct silhouettes, no
stray VFX, Onion correctly reads much smaller than the rest) plus direct
YAML grep of each `ItemDefinition.worldPickupPrefab` reference.

## 2026-08-15 (5)

### v0.3.82-dev — Fix: 7 crop EdibleItems never registered

Real bug, caught by Ben asking "do the new crops have the 'eat'
mechanism?" — creating an `EdibleItem` asset isn't enough on its own;
`PlayerEating.edibles` is a manually-curated array on the scene's Player
GameObject (same pattern as `PlayerBuilding.allPieces`), not something
that auto-discovers new assets. `CarrotEdible`/`PotatoEdible`/
`CornEdible`/`GingerEdible`/`TurnipEdible`/`OnionEdible`/
`SweetPotatoEdible` all existed correctly (verified via YAML) but were
never added to that list, so none of the 7 crops were actually eatable
in-game despite the asset wiring looking complete. Same class of gap as
the Fame save/load bug from earlier this session — an asset/field exists
and looks right in isolation, but nothing actually reads it. Fixed by
appending all 7 guids to `PlayerEating.edibles` in `TestScene.unity`.

## 2026-08-15 (4)

### v0.3.81-dev — Real growth-stage art via Wild Harvest: Root Vegetables

Ben purchased and imported the Wild Harvest: Root Vegetables Asset Store
pack (`Assets/NV3D/Wild Harvest/`), resolving `COOKING_AND_GARDENING_PLANNING.md`
section 3's open Asset-Store-pack-vs-Blender question in favor of the
pack — for the growing-plant visuals specifically (the seed *packet*
model from the last entry stays custom Blender art either way). Confirmed
genuinely URP-native (`WH_Foliage.shader` targets `UniversalPipeline`
directly) before touching anything, so none of this project's prior
legacy-shader-invisibility gotchas applied.

Real finding from the pack's own source (`PatchController.cs`), not
assumed: the numbered `Plant_1`–`_12` prefabs per crop are genuine
sequential growth stages (the pack's own demo controller cycles through
them as a `List<GameObject>`), not just random variety — confirmed
further by measuring actual `Renderer.bounds` across all 12 stages for
several crops (small→medium→large trend, not perfectly monotonic but
real). Covers 6 of 7 crops (all but Corn, which isn't a root vegetable
and keeps its placeholder cube).

**Ben's call: use the pack's full stage list as-is, on our own existing
mechanic** — not collapsed to a fixed 3 stages, and not the pack's own
standalone click/timer demo mini-game (which would have meant abandoning
the seed-packet/inventory/cell system built earlier tonight).
`CropDefinition.growingVisualPrefab` (single mesh, scaled) became
`growthStagePrefabs` (ordered `GameObject[]`); `GardenPlot4x4` no longer
scales one mesh through 3 fixed multipliers — it now swaps between
whichever stage prefab the cell's real-time progress currently falls
into (`Mathf.FloorToInt(progress * stageCount)`), only destroying/
instantiating when the target stage index actually changes. Corn's
single placeholder became a 1-element `growthStagePrefabs` array, so the
same code path covers it with no special-casing.

Verified via direct YAML grep of all 6 crops' 12-entry arrays plus
Corn's 1-entry array. **Not yet live-tested in Play mode** — worth a
close look given the growth-stage swap is genuinely new logic, not a
straight reuse of the single-plot POC's proven scale-based approach.

## 2026-08-15 (3)

### v0.3.80-dev — Seed Packets (7 crops) + 4 new crops (Ginger/Turnip/Onion/Sweet Potato)

Ideated live with Ben: a shared Blender seed-packet model
(`Tools/Blender/GenerateSeedPacketModel.py` — a small folded paper
envelope, ~8x9cm), reused across all 7 Garden Plot crops via 7 color-
coded material variants (Carrot orange, Potato tan, Ginger pale gold,
Turnip violet, Onion dusty yellow-brown, Sweet Potato rust-red, Corn
bright yellow) instead of 7 separate models — no per-crop lettering
baked in (unreliable on an imported model, per this file's own
text-on-model precedent), color plus the item's own 2D icon does the
disambiguating job. Embedded glTFast material extracted and remapped to
a real `Universal Render Pipeline/Lit` asset first, same fix as the
Berry Seed invisibility bug earlier this project — confirmed visually
via an actual render (orange, visible, correctly shaped), not just a
clean YAML check, given this exact failure mode's history in this repo.

**"10 seeds per packet" (Ben's design call):** all 7 seed items got
`maxStack` dropped from 20 to 10 and renamed `"X Seed Packet"` — a full
stack now literally *is* one packet. No mechanic change was needed:
`GardenPlot4x4.TryPlant` already consumes the whole stack on planting and
tracks the remaining count per-cell, decrementing on each auto-replant —
exactly "plant straight out of the packet, decrease the quantity" with
zero new code.

**4 new crops** (Ginger 7 min, Turnip 8 min, Onion 9 min, Sweet Potato 10
min) — same seed/crop `ItemDefinition` + `EdibleItem` + placeholder
growing-visual + `CropDefinition` pattern as Carrot/Potato/Corn.
`GardenPlot4x4.prefab`'s `registeredCrops` now lists all 7. Growing-plant
visuals are still placeholder primitives for all 7 (unchanged from last
entry) — only the seed-packet *item* model is real art now, not the
mature plant.

Built across 2 separate batch-mode Editor script invocations (content
creation, then the existing-prefab crop-array expansion) specifically to
avoid this project's own stale-prefab-reference gotcha. Verified via
direct YAML grep of every cross-reference plus one actual render.
**Not yet live-tested in Play mode.**

## 2026-08-15 (2)

### v0.3.79-dev — 4x4 Garden Plot mechanic built

`COOKING_AND_GARDENING_PLANNING.md` section 3's full 16-cell Garden Plot,
built on top of the resized single-plot model from earlier tonight. New
`GardenPlot4x4Piece` recipe (8 Plank + 6 Stick, Crude tier, trains Cooking,
`groundReach = 5` since it's Foundation-tile-sized) placeable via the
Build tab, registered into `TestScene.unity`'s `PlayerBuilding.allPieces`.

`GardenPlot4x4.cs` runs 16 independent cells, each the exact same
"plant a whole seed stack, harvest one plant, auto-replant the next from
the stack" mechanic `GardenPlot.cs` (the single-plot POC) already proved
out — generalized to any number of `CropDefinition`s instead of one
hardcoded Berry Bush. **Deliberately doesn't reuse `Inventory` for the
cells** despite the planning doc's original "plain Inventory.Slot per
cell" framing — `Inventory`'s slot list compacts via `RemoveAt` whenever a
stack empties, so a per-cell index into it isn't stable, which would have
silently scrambled which cell is which after any harvest. Cells are a
fixed `CellCount` array instead, sidestepping the mismatch entirely.

New `GardenPlotScreen4x4` (E opens it, same popup family as
`CampfireScreen`) shows all 16 cells as buttons; clicking one selects it
and a context panel below offers whatever that cell's state allows
(plant one of the registered crops, watch progress, or harvest).
Deliberately skips a second drag-and-drop implementation — nothing here
needs a quantity or a specific slot, so a click-based context panel
covers the whole mechanic with much less state to get wrong.

Three real crops built: Carrot (5 min), Potato (10 min), Corn (15 min) —
new seed + crop `ItemDefinition`s (6 total), `EdibleItem`s (raw crops are
eatable, same as Berry), and `CropDefinition` assets tying each together.
**Visuals are plain colored primitives (cylinder/sphere/cube), not real
crop models** — the Asset-Store-pack-vs-Blender question from the
planning doc is still unresolved, so this ships mechanically complete
with placeholder art rather than blocking on that decision. Seed sourcing
is Admin-Spawn-only for now too — the wild forage nodes
(`WildCarrotPatch` etc.) from planning doc section 4 aren't built, so
there's no in-world way to obtain seeds yet; logged as a follow-up in
`BUGS_AND_ENHANCEMENTS.md`.

Verified via compile + direct YAML grep (all 16 anchors, both array
wirings, the scene component addition) after every batch step, each step
its own separate Unity invocation specifically to avoid the project's own
stale-prefab-reference gotcha. **Not yet live-tested in Play mode.**

## 2026-08-15 (1)

### v0.3.78-dev — Fix Fame never being saved/loaded

Real bug, found live investigating "I got a Rudimentary skill notice but
no Fame increase" (Ben, after a Wolf kill): `PlayerFame`'s `fame` field
was never wired into `SaveManager` at all when Fame shipped (v0.3.64-dev)
— confirmed via grep, zero references anywhere in `SaveManager.cs`. Every
Fame gain (Hire/Fire, guild join/leave, and skill-tier-unlock grants) was
silently wiped on every reload, resetting to the scene's default 0 —
across a session with as many Editor restarts as tonight had, extremely
plausible as the actual explanation, though not confirmed for this
specific grant (logged as an open follow-up, see
`BUGS_AND_ENHANCEMENTS.md`). His save data did independently confirm the
underlying tier-unlock was real (`Bare-handed` skill level 10.55, crossed
the Rudimentary threshold of 10).

New `PlayerFame.RestoreFame(float)` (sets the absolute value, same
"restore vs. earn" distinction `PlayerSkills.RestoreLevel` already draws
against `GainExperience`), wired into `SaveManager.CapturePlayer`/
`RestorePlayer`. Also directly patched Ben's live save file with his
current known Fame value so tonight's session isn't lost by this exact
bug on the very next load.

### v0.3.77-dev — Fix Garden Plot's visible model and real collider being in two different places

The real root cause behind "no E menu" even standing right on the box.
`GardenPlotRelocate.cs` (v0.3.72-dev, the Boulder-overlap fix) used
`GameObject.Find("GardenPlot")` to relocate the plot — but the imported
model's own nested node is *also* named "GardenPlot" (glTFast names
imported roots after the source file), so `Find` grabbed the visual
child instead of the actual root. It moved the visible mesh to the new
spot while the real `Collider`/`GardenPlot` script silently stayed
behind at the original, still-Boulder-adjacent position — no error, a
plausible "moved to X" log, and even a YAML check that looked fine (the
position values were real, just applied to the wrong object).

Diagnosed live with Ben via a `FindObjectsByType<GardenPlot>()`-based
probe (component type instead of name, sidestepping the ambiguity) that
printed the real component's transform vs. the player's actual distance
to it — confirmed a straightforward but real `interactRange` miss
first (a `GameObject.Find` name collision one level up, same mechanism,
different symptom — `MissingComponentException` from an earlier
diagnostic script grabbing the same wrong object), then this deeper one
once the numbers didn't add up. Fixed by finding the real `GardenPlot`
component directly, resetting the model child's stray offset back to
identity, restoring `PlantAnchor`'s intentionally-nonzero offset (briefly
zeroed by the first fix pass), and moving the actual root — each step
using `PrefabUtility.RecordPrefabInstancePropertyModifications`, matching
`CLAUDE.md`'s own "per-instance runtime data" gotcha. New `CLAUDE.md`
gotcha on the `GameObject.Find`-vs-imported-model-node-name collision
itself.

### v0.3.76-dev — Fix Garden Plot not seeing seeds carried in a worn Backpack

Real bug, found live (Ben: seeds in hand, planter box in front of him,
no way to plant). `GardenPlot.Complete()` only ever checked the
player's main `PlayerInventory` — it had no idea a worn Backpack has its
own separate nested `Inventory`. Since Berry Seed (a stackable item)
routes into an equipped Backpack first on pickup (`PlayerLoot`'s
existing priority), seeds carried the normal way were invisible to the
plot's own count check, silently no-oping on every E-key press with no
error or feedback. `TryPlant`/`Harvest` now check and pull from both the
main inventory and a worn Backpack's contents (harvest deposits into the
Backpack first too, same priority `PlayerLoot` already uses).

### v0.3.75-dev — Scale up Berry Seed's pickup model

Found live (Ben: still couldn't find spawned Berry Seeds even after the
visibility and rolling fixes). Measured cause: the model's actual bounds
were 1.4cm across — the collider (radius 0.007) matched, but that's over
10x smaller than Berry's own working pickup (radius 0.09). Scaled the
model + collider up together (×3.57) to a 5cm bounds size, measured
against real `Renderer.bounds` rather than guessed, and confirmed
visually via a render screenshot — still reads as a small seed, just
actually spottable on the ground now.

Also manually seeded Ben's save file (`save.json`, backed up first) with
10 Berry Seed directly in his worn backpack's saved inventory, so testing
doesn't depend on Admin Spawn at all going forward.

### v0.3.74-dev — Stop Berry Seed pickups rolling downhill

Found live (Ben, immediately after the invisibility fix made them
visible enough to notice): a tiny `SphereCollider` (radius 0.007) with a
`Rigidbody` and no rotation constraints just rolls indefinitely on any
slope — physically correct for an unconstrained sphere, not a bug in any
game-specific code. Fixed by freezing all three rotation axes on
`BerrySeedPickup.prefab`'s `Rigidbody` (`m_Constraints: 0` → `112`,
i.e. `FreezeRotationX|Y|Z`) — it still falls and settles via gravity
normally, it just doesn't spin/roll away anymore.

### v0.3.73-dev — Fix invisible Berry Seed world pickup

Found live (Ben: Admin Spawn "isn't working" for Berry Seed, while Berry
itself spawned fine through the identical code path). Real cause:
`BerrySeed.glb`'s embedded material was never extracted into a real
`Universal Render Pipeline/Lit` `.mat` asset the way every other working
model's material already is — it rendered fully invisible under URP via
the raw glTFast-import shader, same failure shape as the HumanDummy
legacy-shader bug earlier this session, different mechanism. Fixed with a
new `Assets/Data/BerrySeedPickup.mat` remapped onto the model's importer
via `AssetImporter.AddRemap` (glTFast's importer is
`GLTFast.Editor.GltfImporter`, not Unity's built-in `ModelImporter` —
`AddRemap`/`SaveAndReimport` are on the base `AssetImporter` class either
way). Verified via the model's own `.meta` file (not the prefab/scene —
the remap lives on the model's own import settings) and a render
screenshot, since this bug class's whole signature is "looks fine
structurally, renders invisible." Checked every other imported model for
the same issue (grepped every `.glb.meta` for an existing material remap)
— found none, confirmed this was specific to Berry Seed's own generation,
not a project-wide pattern worth a wider fix pass. New `CLAUDE.md` gotcha.

### v0.3.72-dev — Fix Garden Plot spawning inside a Boulder

Found live (Ben, screenshot): the single pre-placed `GardenPlot`
(v0.3.71-dev) landed right on top of a Boulder at (4, -4) — that spot
just hadn't been checked against the scene's existing scatter content.
Moved to (8.62, 0.28, -2.09), confirmed clear (4m+) of every Boulder/
Tree/Bush/structure in the scene via script rather than picking another
coordinate blind.

### v0.3.71-dev — Single-plant Garden Plot proof of concept (MVP2 item 9)

Scoped-down first build of the Gardening system designed in
`COOKING_AND_GARDENING_PLANNING.md` — one small raised bed, one plant
(Berry Bush), proving the core "plant a seed stack, harvest auto-replants
the next until the stack is exhausted" mechanic before investing in the
full 4×4/16-cell grid design.

New `GardenPlot.cs` (single-slot `IInteractable` — E plants your whole
current Berry Seed stack at once, E harvests when ready and immediately
starts the next seed if any remain), 3-stage growth (thresholds at 1/3
and 2/3 of a 5-real-minute timer, `localScale` 0.35×/0.65×/1.0×) reusing
the existing `BerryBush` model directly for the growing-plant visual (its
own `BerryBush` component/colliders stripped at runtime — purely
decorative reuse). New small raised-bed model
(`Tools/Blender/GenerateGardenPlotModel.py`, ~0.8m wood-frame box with a
soil interior). New `Cooking` `SkillDefinition` and `GardenPlotPiece`
`BuildPiece` (2 Plank + 2 Stick, Crude tier, trains Cooking) — placed via
the existing Build tab, same free-placement pattern `Campfire` already
uses. One instance placed directly into `TestScene.unity` for immediate
testing.

Deliberately deferred: `GardenPlotPiece`'s icon (blank tile for now), the
ready-state highlight material swap, and of course Carrot/Potato/Corn +
the full 16-cell grid itself — see `COOKING_AND_GARDENING_PLANNING.md`
section 5.

### v0.3.70-dev — Place the 4 prefab buildings into TestScene.unity

Populates the world with the 4 buildings built in v0.3.69-dev: `SmallHutTwig`
and `SmallHutPlank` near (±20, 20), `RectangularHouseTwig` and
`RectangularHousePlank` near (±20/25, -25) — a loose square around the
player's (0,0,0) spawn point, each Y sampled from real terrain height via
`GroundHeight.Sample`.

Real gotcha hit and fixed along the way — logged in `CLAUDE.md`:
`EditorSceneManager.OpenScene(path)` (no explicit `OpenSceneMode`) +
`SaveOpenScenes()` silently no-op'd in batch mode — every placement logged
real success (correct positions, no errors), but the `.unity` file on disk
was never actually written, caught only by checking the file's own modified
timestamp against wall-clock time. Fixed by threading an explicit `Scene`
handle through every step (`OpenSceneMode.Single`,
`PrefabUtility.InstantiatePrefab(prefab, scene)`,
`EditorSceneManager.SaveScene(scene)` with its `bool` return value logged)
instead of relying on the ambient "currently open scene."

### v0.3.69-dev — "Prefab" buildings dev tool (MVP2 item 10)

A dev-facing level-design tool for fast world population, per the scope
decision resolved this session (not the bigger, deferred player-facing
blueprint feature). Full design in `MVP2_PLANNING.md` item 10.

Four composite building prefabs (`Assets/Prefabs/Buildings/`) assembled
by replicating `PlayerBuilding`'s own socket-snap placement math
(`ResolveFollowing`/`Confirm`) directly in a batch-mode Editor script,
instead of live Play-mode piece-by-piece placement: `SmallHutTwig`/
`SmallHutPlank` (1 Foundation, 4 Walls, 4 Roof panels meeting at a
center pyramid ridge — confirmed correct via render screenshot) and
`RectangularHouseTwig`/`RectangularHousePlank` (2 tiled Foundations, 6
perimeter Walls, 6 Roof panels — ships with a known gable-end roof
geometry gap, see `BUGS_AND_ENHANCEMENTS.md`, since no gable-end/hip-roof
piece exists yet). Real geometry hit along the way: Twig's Foundation
prefab is irregularly named `Foundation.prefab` with no "Twig" prefix,
unlike every other Twig piece — caught by the loader's own missing-
prefab error, not silently.

New permanent `Assets/Editor/PrefabBuildingPlacer.cs` (same "keep,
don't delete" category as `IconBaker.cs`, not one-off setup code) adds
four `Gridless/Place Prefab Building/...` Editor menu items — drops the
chosen building at the current Scene view's pivot XZ, sampling real
terrain height via the existing `GroundHeight.Sample`, with full Undo
support and auto-selecting the new instance for immediate fine-tuning.
Editor-only (menu item, not a runtime Admin Spawn button) — Ben's call,
matches this project's existing world-population workflow (Trees/
Boulders/Bushes were all placed via batch-mode Editor scripts, not
in-Play-mode tools).

### v0.3.68-dev — Fix Intelligence double-dip reading a self-written book

Closes the `BUGS_AND_ENHANCEMENTS.md` bug logged earlier today: reading
a Skill Book always granted the flat 0.25 Intelligence gain, even when
the reader already knew the book's subject — always true for a
self-authored book, since writing one requires already knowing the
recipe/wish. `PlayerReading.TryRead()` now checks
`crafting.HasRequiredSkill(book.TargetRecipe)` (recipe books) or the new
`PlayerMagic.IsWishUsable(wish)` (wish books, split out of `CanAttempt`
so the check isn't polluted by a Will-cost gate that's irrelevant here)
*before* granting the recipe/wish, and only pays the Intelligence gain
when the read actually teaches something new.

## 2026-08-14 (14)

### v0.3.67-dev — Fix `MissingReferenceException` crash from a stale Inventory drag surviving a tab switch

Real bug found live (Ben: dragged an admin-spawned Masterwork Pickaxe to
equip it, "nothing happens at all," then a `MissingReferenceException`
appeared later after dropping a second pickaxe near a hired NPC).
Root cause: `InventoryScreen`'s drag state (`dragCandidate`/`dragItem`/
`dragSource`/`dragEquipment`) is only ever resolved by
`HandleGlobalDragRelease()`, called from `DrawPopups()` — but
`PlayerMenuScreen.OnGUI()` only calls `DrawPopups()` while
`currentTab == Tab.Inventory`. Starting a drag, then clicking a
different tab (Skills/Crafting/Player/...) *before* releasing the mouse,
freezes that drag state indefinitely with no reset — clicking a tab
button never called `InventoryScreen.ResetPopups()` (only closing the
whole menu via Tab did). The next ordinary drag-release back on the
Inventory tab, potentially much later, resolved the stale drag against
whatever drop zone the mouse happened to be over, calling `Stash()` on
the originally-dragged `Tool` — which by then had been separately
dropped in the world and destroyed. Fixed in `PlayerMenuScreen.
DrawTabBar()`: switching away from the Inventory tab now calls
`InventoryScreen.ResetPopups()` first, same as closing the whole menu
already did.

### v0.3.66-dev — Fix `GetLastRect` console spam in the Inventory screen

`InventoryScreen.DrawContent()` called `GUILayoutUtility.GetLastRect()`
immediately after `GUILayout.BeginScrollView(...)`, which Unity's IMGUI
disallows (its own scroll-view group has no layout entries yet at that
point) — spammed "You cannot call GetLast immediately after beginning a
group" every OnGUI frame the Inventory screen was open. Found live (Ben:
console screenshot while reading a Skill Book, unrelated to the book
itself — it fires on any Inventory-screen frame). Fixed by caching the
scroll view's rect from the *previous* frame's `GUILayout.EndScrollView()`
instead (`lastScrollViewRect` field), used by `HandleAutoScroll` for the
drag-to-edge auto-scroll feature — one frame of lag, imperceptible for
this purpose.

Also confirmed live: the two pre-placed `SkillBook` found-books
(`TEST_FEATURE_PLAN.md` section 31) read cleanly with no errors, each
granting the Intelligence XP tick, with the Spark wish/lineage grant
correctly no-op'ing on the second read since it was already known.

### v0.3.65-dev — Factions removed from the design entirely

Ben's call: Factions (the separate reputation/trust-standing system
alongside Merchant Guilds and Warbands, per `design-brief.md`'s original
reconciliation) never got built and duplicated what the newly-built Fame
system already does. Fame absorbs its role everywhere Faction was
referenced — Warband conduct and Settlement Warfare outcomes now move
Fame directly, confirmed explicitly before editing anything.

Removed the inert `Faction: None` Player-tab tile (`PlayerMenuScreen.cs`)
along with the now-dead `DrawPlaceholderTile` helper it was the last
caller of. Updated every forward-looking design doc: `design-brief.md`'s
three-system "Factions, Guilds & Warbands" section is now two systems,
"Guilds & Warbands"; `game-overview.md`'s "Player-Created Factions" pitch
renamed to "Player-Formed Warbands" (what it actually described —
territorial player groups — was always closer to Warbands than to the
personal-reputation Faction concept); `skill-path-space.md` and
`BUGS_AND_ENHANCEMENTS.md` updated to point at Fame instead.
`reconciliation-questions.md` (a historical decision record) got a
correction note rather than a rewrite — same "don't rewrite history,
append a correction" discipline `CHANGELOG.md` itself follows.

## 2026-08-14 (11)

### v0.3.64-dev — Fame system built

Every input/output effect from `FAME_PLANNING.md` that had something
real to hook is now built: new `PlayerFame` component (a single -1000 to
1000 float), Hire +1/Fire -0.5/unpaid-wages -0.5-per-cycle (hooked into
`NPCHiringScreen`/`NPCHiring`), skill-tier mastery in any discipline
including the core stats (new `PlayerSkills.TierUnlocked` event +
`CraftTierScale.FameOnTierUnlock` — the "everyone knows the Hulk for his
strength" case needed no new detection logic, just `PlayerFame`
subscribing), guild Join +1/Leave -1 (`PlayerGuilds`), and the
negative-Fame NPC-flee output effect (new `NPCFlee.cs` on
`NPCFactoryWorker.prefab` — every NPC within ~10m flees at 2x wander
speed, pausing their job until the player leaves, reusing `NPCWander`'s
move/ground-sample/face plumbing aimed away from the player instead of a
random point). Real Player-tab tile with a band-name sub-line
(Infamous/Notorious/Neutral/Known/Renowned), replacing the old
`DrawPlaceholderTile("Fame", "0")`.

Four pieces stay explicitly unbuilt, each logged as its own
`BUGS_AND_ENHANCEMENTS.md` follow-up rather than built blind: Kill NPC
(-10, blocked — hired NPCs don't implement `IDamageable` at all); Player
death (-2, blocked — **found live while building this pass**: there is
no player-death detection anywhere in the codebase, `PlayerVitals.health`
just clamps at 0 via `Mathf.Max` with no event ever firing); Start/Close
a guild (+3/-6, blocked — `GuildDefinition` is a pre-authored asset only,
no player-driven guild creation exists); and business-reach Fame plus the
Traveling Trader (blocked on an entire vendor/commerce system that
doesn't exist in any form — the biggest prerequisite in the whole doc).

**Real bug caught mid-build**: adding `using System;` to `PlayerSkills.cs`
(for the new `Action<CraftTier>` event) created an ambiguous reference
between `UnityEngine.Random` and `System.Random` at an existing
`Random.Range` call site — a real compile error that a first batch-mode
run somehow reported as clean (likely a stale/incomplete run from the
Editor being open at the time, not caught until a second, genuinely
project-locked run surfaced it properly). Fixed by fully qualifying
`UnityEngine.Random.Range`.

Verified via batch-mode compile (0 CS errors) + direct YAML grep of
every new script and scene/prefab wiring. **Not yet tested in Play
mode.**

## 2026-08-14 (10)

### v0.3.63-dev — fix: Player/NPC models actually invisible for real this time (legacy shader)

The v0.3.62-dev `GraphicsSettings` revert was a red herring — Ben reported
the models were still invisible after it. **Real root cause, found by
walking the Inspector live together** (Hierarchy → body mesh child →
Skinned Mesh Renderer → Materials): all 7 `HumanDummy*.mat` variants used
the legacy Built-in Render Pipeline shader `Unlit/Texture`, incompatible
with this URP project — latent since v0.3.4-dev (confirmed via git
history on the material file, zero changes since the original NPC visual
import), unrelated to anything from this session, just never actually
manifested as full invisibility until now. Fixed by swapping to
`Universal Render Pipeline/Unlit`.

That fix introduced a second bug in the same edit: `Unlit/Texture` never
actually exposed a `_Color` property, so migrating it via
`Material.GetColor("_Color")` silently returned transparent black instead
of throwing, written straight into the new shader's `_BaseColor` — right
shader, right texture, wrong tint, same invisible-model symptom. Caught
by grepping the saved `.mat` YAML for the actual color value, not by
trusting a clean batch-script log. Corrected to white (matching the
shader's own default, confirmed by Unity omitting the now-redundant
property from the saved file entirely).

**Confirmed fixed live by Ben** after this second pass — not just a
clean compile. Two new `CLAUDE.md` gotchas written up: the corrected
record on the `GraphicsSettings` red herring (don't stop investigating
once you've found *a* plausible suspect — verify the actual symptom is
gone), and the real one (a legacy Built-in shader can render fully
invisible under URP instead of the usual pink "shader missing"
indicator, and `GetColor` on a nonexistent property fails silently).

## 2026-08-14 (9)

### v0.3.62-dev — fix: Player/NPC models invisible (bad GraphicsSettings from two commits ago)

Real regression, reported live by Ben ("the npc and player models are
invisible now"). Root-caused to `ProjectSettings/GraphicsSettings.asset`'s
`m_LightsUseLinearIntensity`, silently flipped `0` → `1` as an
unintended side effect of running `IconBaker` (v0.3.57-dev, the Ingot
build — `IconBaker` needs a real graphics device, unlike every other
batch-mode script this session) and committed without actually verifying
it in the Editor, wrongly judged "benign" at the time. Reverted to `0`,
its value for the entire rest of the project's history. New `CLAUDE.md`
gotcha written up — any unexpected `ProjectSettings/*.asset` diff is a
real regression risk, not a rounding artifact to wave through, and this
category of breakage won't show up in a compile check or YAML grep, only
an actual Play-mode look.

## 2026-08-14 (8)

### v0.3.61-dev — Melee weapon damage framework, first applied to the Knife

Ben's ask: tier-based damage bonus for the Knife (Crude/Rudimentary +0,
Normal +1, Fine +1.5, Masterwork +2 on top of the base 9-damage punch),
built as a reusable framework for future weapons rather than a
Knife-specific special case. Superseded the original five-weapon-skill
plan (Archery/Spear/Sword/Gun/Bare-handed) with one shared **Melee**
skill (Ben: "let's generalize it under Melee") — a display/categorization
choice only, no mechanical link to the Strength stat, confirmed before
building.

New `CraftTierScale.WeaponDamageBonus(tier)` — deliberately its own
table, not reused from `Modifier`/`WeightModifier` (same "a ratio tuned
for one quantity doesn't transfer to another" lesson those two already
document). New generic `ItemDefinition.isMeleeWeapon` flag and
`PlayerEquipment.GetHandItems()` (finds *what's* held without knowing its
identity ahead of time, unlike the existing `HasInHand` which checks one
specific known item). `PlayerCombat` now checks both hands for a flagged
weapon on every swing — bare-handed and Melee-trained-with-a-weapon are
resolved by the same code path, one small helper method, not a branch
scattered through the swing logic. All 5 Knife tiers flagged; any future
melee weapon (Spear, Sword) just needs the same flag, no `PlayerCombat`
changes required. Ranged combat (Archery/Gun) stays a separate, still
fully open gap.

Verified via batch-mode compile (0 CS errors) + direct YAML grep of every
new asset and the scene wiring. **Not yet tested in Play mode.**

## 2026-08-14 (7)

### v0.3.60-dev — Sand dig sites, first piece of the digging system

Unblocked by the Rudimentary Shovel (v0.3.59-dev, same day) — the
`BUGS_AND_ENHANCEMENTS.md` digging plan's `requiredTools` gate finally has
a real tool to point at. Two design questions the backlog explicitly left
open got resolved: digging trains the existing `Gathering` skill (not a
new dedicated one), and what Sand actually gets used for (a new Building
material tier, a new Glassmaking line — both real scope) is deliberately
scoped to MVP3, not built this pass.

New generic `ResourceNode.holeVisualPrefab` field — a persistent child
object (despite the "Prefab" name) toggled active/inactive, folded
directly into `SetVisible` rather than scattered across its 5 call sites,
so every one of them (including save/load restore) gets the "leaves a
hole on break, hides it on respawn" behavior for free. Three simple
Blender-generated props back it: the standing sand patch, a small clump
(the actual `Sand` pickup, scattered on break), and a dirt-brown hole.

**Real bug caught and fixed**: `SandDigBuilder`'s first run left
`ResourceNode.trainedSkill`/`requiredTools[0]` silently null on the saved
prefab — both references were loaded once at the top of the build script
and used only after later `NewScene`/`SaveAsPrefabAsset` calls, the same
stale-reference trap `CLAUDE.md` already documents for `OpenScene`/
`LoadPrefabContents`, just with a different failure shape (`GameObject`
model references carried across the same boundary survived fine;
`ScriptableObject` references didn't). Fixed by re-fetching immediately
before use, then correcting the already-saved prefab asset in place
(same guid, so the already-placed scene instance picked up the fix
automatically) rather than deleting and rebuilding.

**Second real bug, caught by actually looking at the baked icon**: Sand's
first color choice (`0.76, 0.68, 0.48`, a "realistic" sand tan) baked to
near-white — unlike the Ingot family, Sand is a non-metallic diffuse
material, so it gets full lighting contribution from `IconBaker`'s bright
ambient + two directional lights instead of the mostly-specular, dimmer
response a metallic surface gets at the same albedo. Darkened
empirically (`0.55, 0.47, 0.32`) until it read correctly.

New `Sand.asset`, `SandPickup.prefab`, `SandDigSite.prefab`, one
`SandDigSite` instance placed in `TestScene.unity`. Verified via
batch-mode compile (0 CS errors) + direct YAML grep of every new asset
and the scene wiring. **Not yet tested in Play mode.**

## 2026-08-14 (6)

### v0.3.59-dev — Rudimentary Shovel: new item, recipe, and real Blender model

First tier of a future full Shovel ladder (`BUGS_AND_ENHANCEMENTS.md`'s
digging/water-scarcity section) — Ben's call, deviating from that entry's
original sketch in two ways: **Metalworking discipline, not Stonework**
(1 Iron Ingot + 1 Rudimentary Trimmed Stick, requires the Anvil, gated at
Rudimentary-level Metalworking via the existing `outputItem.tier`-driven
skill gate — no new gating code needed), and a new **tier-matched-materials
rule** for the whole future ladder: each tier needs its matching Trimmed
Stick tier, not Pickaxe's "same ingredients every tier, quality from a
skill-margin roll" convention.

Real model via the headless-Blender pipeline (Ben: "let's try a blender
model first"), new `Tools/Blender/GenerateShovelModel.py` (kept
permanently, like `GenerateCampfireModel.py`/`GenerateSkillBookModels.py`,
for the future tiers to reuse) — a bmesh-built tapered blade + cylinder
handle + sphere grip, built directly at real-world meter scale rather than
needing a post-import rescale. Measured bounds confirmed a good size
(0.97m total) and correct base pivot before wiring anything up, per
`CLAUDE.md`'s mandatory checklist.

**Real bug caught and fixed the same session, benefiting this model too**:
`IconBaker`'s near-black-metallic bug (v0.3.58-dev, same day) — the
Shovel's blade is a metallic material, so this model's icon would have hit
the identical problem if the fix hadn't already landed first. Icon baked
clean on the first attempt as a result.

New `RudimentaryShovel.asset`, `RudimentaryShovelPickup.prefab` (Rigidbody
+ BoxCollider + `Tool` component, same shape every other tool uses),
`RudimentaryShovelRecipe.asset`, wired into `PlayerCrafting.recipes` in
`TestScene.unity`. Verified via batch-mode compile (0 CS errors) + direct
YAML grep of every new asset and the scene wiring. **Not yet tested in
Play mode.**

## 2026-08-14 (5)

### v0.3.58-dev — IconBaker fix: metallic materials no longer bake near-black

Real bug found by actually looking at the five new Ingot icons side by
side (Ben: "let's look at them and decide") rather than trusting the
material YAML's color values — every one, including Iron's pre-existing
icon, baked as a near-flat-black silhouette with only a faint edge
highlight, indistinguishable from each other regardless of their actual
tint. Root cause: `IconBaker`'s render rig set
`RenderSettings.customReflectionTexture = null` (with
`defaultReflectionMode = Custom`) — a fully metallic material
(`metallicFactor: 1`, the Ingot family's imported glTF material) has
almost no diffuse response to direct/ambient light at all, so with zero
environment reflection to sample, it renders essentially black regardless
of its base color. Fixed with a small neutral-gray reflection cubemap
generated at bake time, giving metals a legible, hue-neutral reflection
without reintroducing the wrong-color skybox cast `Custom + null` was
originally chosen to avoid. Benefits every metallic item baked through
this tool going forward, not just the new Ingots.

Also re-tuned Silver and Platinum's tints after the lighting fix made a
second problem visible: Silver's original tint (`0.75, 0.75, 0.78`) was
too close to Iron's own existing color to read as distinct even once
properly lit, and pure brightness increases (`0.92`, then `0.97`) barely
moved the perceived difference — this render pipeline compresses
brightness-only separation on metallic materials far more than expected.
A hue shift (Platinum's cool blue-gray, then the same trick applied to
Silver) reads far more clearly than brightness alone. Final tints: Silver
`(0.82, 0.90, 0.98)`, Platinum `(0.80, 0.83, 0.90)` — both cool-toned but
distinguishable from each other and from Iron's neutral gray. All five
Ingot icons re-baked. Verified by directly viewing each rendered PNG, not
just checking the underlying color values — the exact discipline this bug
slipped through the first time.

## 2026-08-14 (4)

### v0.3.57-dev — Copper/Silver/Gold/Platinum Ingots

Closes two long-standing `BUGS_AND_ENHANCEMENTS.md` gaps: Silver/Gold/
Platinum had no refined "bar" item, and Copper's own refined item had no
recipe consuming it. New `CopperIngot`/`SilverIngot`/`GoldIngot`/
`PlatinumIngot` `ItemDefinition`s, each with a prefab cloned from
`IronIngot.prefab` (same base mesh, retinted material) and baked icons,
plus a `CraftingRecipe` (10 ore → 1 ingot, Metalworking-trained, requires
Furnace) and a matching `SmeltableItem` for the Furnace's automated queue
— full parity with Iron's existing pattern, both wired into
`PlayerCrafting.recipes`/`Furnace.smeltableItems` in `TestScene.unity`.

**Two real bugs caught before shipping:** the base ingot material
(`Assets/Models/IronIngot.glb`'s imported material) turns out to use a
glTF-style shader (`baseColorFactor`), not URP Lit's usual `_BaseColor`/
`_Color` — the first tint attempt silently left every new ingot sharing
Iron's own gray color, caught by grepping the saved `.mat` YAML for the
actual color values rather than trusting the batch script's clean exit,
then fixed in place (same material guid, so the already-built prefabs'
references stayed valid). Second: Copper turned out to have its own
separate refined intermediate item (`Copper.asset`, distinct from
`CopperOre.asset` — confirmed by checking what `IronIngotRecipe` actually
consumes, `Iron.asset` not `IronOre.asset`, and the v0.1.117/121-dev
changelog entries for how each metal's punch-chain actually resolves) —
unlike Silver/Gold/Platinum, which never got that second item and stay at
their raw Ore as the true final tier. The first build wired all four
ingots to consume their `*Ore` item directly, which was correct for three
of the four but wrong for Copper; fixed by repointing
`CopperIngotRecipe`/`CopperOreToIngotSmeltable` to consume `Copper.asset`
instead. Verified via batch-mode compile (0 CS errors) + direct YAML grep
of every new/edited asset and the scene wiring. **Not yet tested in Play
mode.**

## 2026-08-14 (3)

### v0.3.56-dev — Random weather (Weather Maker)

New `RandomWeatherController.cs` — Ben's ask: "let's add some random
weather... change every 5 real minutes." Deliberately simple: every 300
real seconds, picks a random `WeatherMakerPrecipitationType` (None/Rain/
Snow/Sleet/Hail) and a random intensity (0.3-0.8, or 0 for None), and sets
`WeatherMakerPrecipitationManagerScript.Instance.Precipitation`/
`.PrecipitationIntensity` directly — no need to touch Weather Maker's own
WeatherZone/Profile system, since the manager already smoothly tweens
between precipitation types on its own (`PrecipitationChangeDuration`, a
few seconds by default). Feeds straight into the already-shipped
`PlayerWeatherEffects` cooling bridge with zero changes needed there.
New standalone `RandomWeatherController` GameObject added to
`TestScene.unity`. Verified via batch-mode compile (0 CS errors) + direct
YAML grep of the scene addition. **Not yet tested in Play mode** — Ben
plans to watch a few real-time change cycles once this lands.

## 2026-08-14 (2)

### v0.3.55-dev — Dexterity & Constitution shipped, Intelligence gets a small global XP multiplier

Full design in `DEXTERITY_CONSTITUTION_PLANNING.md` — designed and built
same day. Constitution and Dexterity join Strength/Intelligence as fully
mechanical core stats; both previously sat display-only with a growth bar
that never moved.

**Constitution** grows Max Health (`100 + 4.42×(Con-2)^1.5`, cap 200) and
Max Stamina (`100 + 8.84×(Con-2)^1.5`, cap 300) — both were hardcoded flat
100 everywhere in `PlayerVitals.cs` until now. A pure power law couldn't
hit both a sane low anchor (no regression for a fresh character) and a
front-loaded curve at once — worked out live in the planning doc, resolved
with an additive bonus on top of a fixed 100 baseline instead. Trained by
exercise, not adversity: continuous while sprinting (~4 real days per
+0.25), plus a secret bonus scaled by kick distance on `SoccerBall`
contact (not shown anywhere in UI — a deliberate easter egg).

**Dexterity** adds one more multiplicative term to `FirstPersonController`'s
existing speed chain (`speed = baseSpeed × dexterityMultiplier ×
staminaMultiplier × encumbranceMultiplier`), linear from 0% at the display
floor to +30% at the cap. Trained by sprinting (shared with Constitution —
same action, two payoffs), sneaking (Kneeling/Crawling/Prone + moving),
jumping (flat 0.1 per jump), and completing any `CraftingRecipe` (flat 0.1,
any outcome). The manual-vs-machine distinction Ben wanted (hand-crafting
trains it, Furnace/Campfire automation doesn't) needed no new field —
`CraftingRecipe` is already the "player actively did it" type in this
codebase, while Furnace/Campfire automation already lives in the separate
`SmeltableItem`/`CookableItem` types.

**Intelligence** (already shipped) gets a refinement: a small global XP
multiplier on every *other* skill's gains, `xpGained *= 1 + intLevel/2000`
(+5% at Intelligence 100), applied inside `PlayerSkills.GainExperience`
via an internal check so none of its many existing call sites needed to
change. Supersedes a much bigger (+50%-at-cap) version that lived in
`BUGS_AND_ENHANCEMENTS.md`, explicitly too big for "very small" (Ben,
2026-08-14).

New `PlayerConstitution.cs`/`PlayerDexterity.cs` components on Player,
wired to the `Constitution.asset`/`Dexterity.asset` `SkillDefinition`s;
`PlayerSkills` gained an `intelligenceSkill` reference wired to
`Intelligence.asset`. `PlayerMenuScreen` gained
`DrawDexterityTile`/`DrawConstitutionTile` (replacing the generic
`DrawStatTile`, now dead and removed), matching `DrawStrengthTile`/
`DrawIntelligenceTile`'s sub-line pattern — Dexterity shows its live speed
bonus, Constitution its two vital caps. `PlayerVitals` gained growable
`maxHealth`/`maxStamina` fields (`SetMaxHealth`/`SetMaxStamina`, pushed
every frame by `PlayerConstitution` — a continuously-recomputed pattern
like `PlayerEncumbrance.Capacity`, not `GrowMaxWill`'s discrete-increment
one) plumbed through every existing `Mathf.Min(100f, ...)` clamp and
`SaveManager`'s capture/restore. Verified via batch-mode compile (0 CS
errors) + direct YAML grep of the new scene wiring. **Not yet tested in
Play mode.**

## 2026-08-14 (1)

### v0.3.54-dev — Weather Maker integration, MVP2 item 5 (design + full build + live testing)

Full design/build detail in `WEATHER_MAKER_PLANNING.md`. Digital Ruby's
Weather Maker (v8.1.0) replaced the old procedural sky texture
(`Assets/Data/Sky.mat`/`Assets/Textures/SkyTexture.png`, deleted — this
resolves `CLAUDE.md`'s `Mathf.SmoothStep` cloud-coverage gotcha by
replacing the system it was found in, not by fixing the math in place)
with a real sky, cloud, day/night, and precipitation system.

**Two genuinely project-wide changes, each explicitly confirmed with Ben
before running** rather than assumed safe just because they were
technically scriptable: the URP Render Pipeline Asset now points at
`WeatherMakerURPProfile` (was `Assets/Data/URP-Asset.asset`), and color
space switched from Gamma to Linear (URP is designed/tested for Linear,
so this arguably fixes a pre-existing mismatch rather than introducing a
new one).

**New `PlayerWeatherEffects.cs`** bridges Weather Maker's live
precipitation intensity (rain/sleet/snow/hail, whichever is currently
strongest) into `PlayerVitals.bodyTemperature`, reusing the existing
`WarmNear` method directly — its `MoveTowards`-based implementation is
already symmetric, so a colder target cools instead of warms with no
separate method needed. This is the actual gameplay payoff MVP2 item 5
was chasing (the Constitution/warm-food tie-in), not just visuals.

**Live-tested more thoroughly than almost anything else this session**:
Ben watched a complete day/night cycle end to end in the Editor — clear
day sky with clouds, a purple dusk gradient, a genuinely striking
orange/red sunset, and full night with a real textured, cloud-occluded
moon — confirming HUD/UI stayed intact through both project-wide render
changes, not just a clean-compile assumption.

**Three real bugs hit and fixed live, now documented for future
sessions**: two missing built-in Unity modules
(`com.unity.modules.wind`/`screencapture`, needed by Weather Maker's own
wind-zone and screenshot scripts); a Mirror API version mismatch in
Weather Maker's optional network-sync script (`NetworkConnection
.connectionId` doesn't exist on this project's installed Mirror version
— patched with an explicitly-commented `GetHashCode()` stand-in, since
the whole method is unreachable in the current single-player-only scope
anyway); and a shipped day/night profile with `Speed`/`NightSpeed` both
frozen at `0` — found live when Ben asked how long until night and the
honest answer, read directly from the binary-serialized asset via a
throwaway batch script (grep can't parse it), was "never, at this
setting." Duplicated the profile and tuned to a ~3 real-minute day for
fast testing (Ben's explicit call) — will want slowing down before real
play.

New `TEST_FEATURE_PLAN.md` section 32. Still open: actual precipitation
hasn't been observed live yet (needs a temporary rain/snow weather-zone
profile), so `PlayerWeatherEffects`'s real cooling effect is unverified,
and the current fast day/night pace is a testing stand-in, not final.

## 2026-08-13 (27)

### v0.3.53-dev — Skill books, MVP2 item 7 (design + full build, Phases 0–3/5/6)

Full design in `SKILL_BOOKS_PLANNING.md` (with a
[rendered summary artifact](https://claude.ai/code/artifact/2af217f7-450e-4e4b-9b09-6411a8b72115)),
built the same day. Reading grants a bounded head start; writing risks a
bounded failure. Two consumer types, one unified mechanic:

- **Crafting/weapon skill books** grant one specific `CraftingRecipe` as a
  standing exception to the normal skill gate — never touches the
  discipline's actual level, never grants anything else at that tier.
- **Magic wish books** (e.g. "Fireball") do the same for a `WishRecipe`
  *and* unlock its lineage if not already known — confirmed against
  `PlayerMagic.cs` as one unified mechanic, not two separate systems.
  `PlayerMagic.StartingLineage` (a single field) became a real
  `knownLineages` set + `LearnLineage`/`GrantWish`, with no cap — a
  player can eventually know all four lineages.
- **Writing** reuses `PlayerCrafting`'s existing outcome-roll formula
  directly (extracted into a new shared `CraftOutcomeRoll.cs` — margin =
  author's Intelligence vs. the subject's tier requirement, no new
  formula needed). Consumes 1 Paper + 1 Ink per attempt regardless of
  outcome; `SpectacularFailure` also deals 2–10 damage to the author;
  only `BrilliantSuccess` grants a lineage tome a 1–10 bonus starting
  level. Author's Intelligence trains on any non-failure, scaled by
  outcome quality — this is also Intelligence's first real trigger+effect
  pair (mirroring `PlayerEncumbrance`'s Strength pattern), a candidate
  MVP2 item 1 had only ever sketched.
- **Reading** turned out not to fit the originally-sketched
  `PlayerEating.TryEatFrom` shape (that only works for plain
  `ItemDefinition`-only consumables) — a `SkillBook` is equipment-backed
  (a real physical instance carrying its own target), so `PlayerReading`
  hooks into `InventoryScreen`'s `pendingActionEquipment` popup instead,
  the same shape `Canteen`'s Drink/Fill buttons already established
  there. Grants the recipe/wish exception, a small Intelligence tick,
  then permanently destroys the book.

New "Writing" tab in `PlayerMenuScreen` (Tab key, no new keybinding) —
lists every recipe/wish the player currently knows, each with a Write
button. New models (Book/Scroll/Paper/Ink) generated via a new permanent
Blender script, `Tools/Blender/GenerateSkillBookModels.py` (Ben's call
over the usual Tripo3D pipeline) — Scroll's own model exists but stays
unused, reserved for a future separate random-roll item. Paper/Ink now
have a real recipe source (1 Plank → 4 Paper, 2 Berry → 1 Ink, both
Crude/no-skill-gate) instead of Admin-Spawn-only. Two "found" books
placed directly in `TestScene.unity` (`MasterworkKnifeRecipe` and
`SparkWish`) as the smallest honest stopgap for random world drops, since
no loot-chest/loot-table system exists anywhere in this codebase yet.

**Explicitly not built**: Phase 4 (NPC training) is correctly blocked —
NPCs have zero crafting/bench-work system at all yet
(`NPC_JOB_GENERALIZATION_PLANNING.md`'s own deferred bench-crafting
scope), so a granted recipe would have nothing to attach to. Rare
magic-teaching NPCs and NPCs writing their own books are both explicitly
deferred to a later MVP, per the design.

Two real, generalizable bugs caught and fixed live, now documented in
`CLAUDE.md`'s gotcha list: `SkillBook.TargetRecipe`/`TargetWish`/
`BonusLevel` were plain C# auto-properties, invisible to Unity's scene
serializer — harmless for a book written/read within one Play session,
but silently lost on reload for a book placed directly in a saved scene.
Fixed with real `[SerializeField]` backing fields, plus a second trap:
even then, a plain field assignment on a prefab instance's component
still needed an explicit `PrefabUtility.
RecordPrefabInstancePropertyModifications` call to actually serialize.
Both caught by grepping the saved scene YAML directly, not by trusting
"the script logged success."

Verified via 10+ rounds of batch-mode compile (0 CS errors throughout) +
direct YAML grep of every new asset/prefab/scene reference. New
`TEST_FEATURE_PLAN.md` section 31 — a full manual Play-mode checklist.
**Not yet live-tested in Play mode** — every check so far has been
structural (compile + YAML), same status save/load carried until its own
live round-trip confirmation. Cross-referenced against `MVP2_PLANNING.md`
(advances item 1, item 7's NPC phase blocked on item 2, creates a
follow-up flagged in `SAVE_LOAD_PLANNING.md` section 10 for item 6's save
system to eventually capture this new state).

**Live-feedback fix, same day**: Ben's first live look flagged the Player
tab's Intelligence tile as the odd one out — Strength already shows a
derived sub-line (Encumbrance), Intelligence didn't show anything for
its own new mechanic. Added a matching "Reading & Writing — Paper: X,
Ink: Y" sub-line, same `DrawTile` shape Strength's Encumbrance line
already uses.

## 2026-08-13 (26)

### v0.3.52-dev — Save/load live-tested; Settler's Shirt worn-offset tune

**Save/load persistence, live-tested for real** (v0.3.51-dev's build,
confirmed the same day): Ben ran an actual Editor-restart round trip
(exit and relaunch, not just re-entering Play mode) — worn equipment
(Backpack, and a Settler's Belt with a Canteen clipped to it, from
starting gear) and nested equipment contents (11 Sticks placed inside the
worn Backpack) both came back exactly. Confirms the recursive
nested-equipment capture (`EquipmentSaveUtility`/`InventorySaveUtility`,
the plan's hardest piece) actually works, not just compiles. Marked done
in `MVP2_PLANNING.md` item 6 and `BUGS_AND_ENHANCEMENTS.md`'s matching
entry; `TEST_FEATURE_PLAN.md` section 30 updated with what's confirmed
vs. still open (full vitals round-trip, Canteen liquid specifically,
StorageBox, ResourceNode respawn timing, Hireable NPC state).

**`PlayerShirt` worn offset live-tweaked**, same Play-mode-Inspector
workflow established for Boot/Backpack: `wornPositionOffset` from
`(0, 0, 0)` to `(0, -0.33, 0)`, `wornEulerOffset` from `(0, 0, 0)` to
`(0, 89, 0)`.

## 2026-08-13 (25)

### v0.3.51-dev — Save/load persistence, v1

Full implementation of `SAVE_LOAD_PLANNING.md`'s plan — a manual Save
button (` menu, Player tab), no autosave, loading automatically on game
start if a save file exists at `Application.persistentDataPath/save.json`
(Newtonsoft.Json, already a package dependency from earlier Mirror/PurrNet
evaluation). Nothing in this project persisted anything before this.

**Stable identity.** New `SaveId` component (auto-generating GUID, same
"small single-purpose marker" convention as `IWaterSource`/`IRenameable`)
+ `SaveIdRegistry` scene-scan lookup, now required on `StorageBox`/
`ResourceNode`/`NPCHiring`. New `ItemDatabase`/`SkillDatabase`/
`NPCJobDatabase` — stable-ID lookups (asset file name as the ID) for the
three `ScriptableObject` reference types save data needs to resolve,
living in `Assets/Resources/` so `Resources.Load` works in a build. A
one-off Editor script populated all three (84 items, 18 skills, 3 jobs)
and added `SaveId` to all 85 existing world objects in `TestScene.unity`
— **hit and fixed a real batch-mode bug live**: `EditorSceneManager.
SaveScene(scene)` without an explicit path opens a native Save File
dialog, which silently cancels with no error in batch mode (`-nographics`,
no UI) — the whole save quietly no-op'd twice in a row while logging
"success." Fixed by passing `scene.path` explicitly. Also found
`SceneAutoOpen`'s `EditorApplication.delayCall` doesn't reliably fire
before a `-executeMethod` batch run reaches the method body — the first
two runs silently operated on an empty untitled scene (zero Player, zero
world objects found) while still logging success. Fixed by calling
`EditorSceneManager.OpenScene` explicitly instead of relying on it. Script
deleted after running, per convention.

**The recursive nested-equipment piece** (section 4 of the plan, the
hardest part): new `EquipmentSaveUtility` + `InventorySaveUtility`, a
genuinely recursive pair — any `Inventory` (player, `PlayerEquipment`
slots, `NPCCargo`, `StorageBox`) captures each slot's item + count, or,
for an equipped slot, calls into `EquipmentSaveUtility` for that
instance's own extra state (`Canteen`'s liquid/amount) and, for anything
implementing `IInventoryHolder` (Backpack/Boot/Belt), its own nested
`Inventory` — which calls back into `InventorySaveUtility`. Restoring an
equipped slot instantiates a fresh copy of the item's real
`worldPickupPrefab`, replays that state, and leaves it `Stash()`ed;
`PlayerBodyModel.RefreshAllAnchors()` (new — just calls the existing
`ApplyGender` sweep) then bone-attaches every worn item in one pass,
reusing the exact mechanism a gender toggle already had for "populated
slot, needs a real anchor" — no per-equipment-type restore code needed
beyond that.

**Captured**: Player vitals/skills/currency/inventory/full equipment
(recursive)/position+yaw/gender; `StorageBox` name + contents;
`ResourceNode` respawn state (stored as **seconds remaining**, not an
absolute `Time.time` — meaningless across a restart, and this project's
stated future goal is real multi-day timers once persistence exists, so
the format is already shaped for that); Hireable NPCs (hired/waiting-for-
payment/work timer, assigned job + equipped tools + deposit container
cross-reference, cargo, skills, position). **Explicitly deferred, per the
plan**: loose world pickups, built structures, Lockbox/Bank contents.

New small additions to support restore: `FirstPersonController.Teleport`
(disables `CharacterController` before moving the transform, same
"CharacterController-disable dance" `AdminSpawnScreen` already documents),
`Canteen.RestoreLiquid`, and direct-set restore methods on
`PlayerVitals`/`PlayerSkills`/`PlayerCurrency`/`NPCSkills`/`NPCJob`/
`NPCHiring` (bypassing each one's normal gated-mutation methods, which
correctly don't apply to loading a value that's already valid).

Verified via 3 rounds of batch-mode compile (0 CS errors each) + direct
YAML grep of the saved scene/database assets. **Not yet tested in Play
mode** — needs a save/reload-the-Editor/load pass with a real mix of
state (worn Backpack with contents, a hired NPC with cargo, a
partially-respawning ore node) before this is considered done; see
`TEST_FEATURE_PLAN.md`.

## 2026-08-13 (24)

### v0.3.50-dev — Boot and Backpack: live-tweaked final values baked in

Switched from guess-and-screenshot to Ben live-tweaking both `PlayerBoot`
and `PlayerBackpack`'s offset fields directly in the Play-mode Inspector
(gender-toggle in the ` menu forces a re-anchor so edited values actually
take visible effect, since neither carrier re-applies its offset every
frame). Baked in the results as the new script defaults:

- **`PlayerBoot`**: position `(0, -0.93, 0.35)`, rotation `(0, 90, 0)` —
  notably a **yaw** correction, not the pitch this session's blind
  guessing kept trying. "Looks closer," not yet declared final.
- **`PlayerBackpack`**: position `(0, 0.05, -0.18)`, rotation
  `(0, -90, 0)` — refines round 9's guessed height with a precise
  live-tested value; the `-90` yaw from round 8's direct instruction was
  exactly right. Mirrored onto the 3 NPC job assets' Backpack
  `attachPositionOffset`/`attachEulerOffset` for consistency.

## 2026-08-13 (23)

### v0.3.49-dev — Backpack height tune: rotation confirmed correct, just too low

Ben: "backpack is in the right alignment, needs to be higher on the
body." First fully-positive rotation confirmation in this whole chain —
only the Y position needed raising, from `-0.3` (sitting down near the
hips) to `-0.15` (mid-back). Mirrored onto the 3 NPC job assets'
Backpack `attachPositionOffset`.

Boots: after another guess round showed the pitch fix was real progress
(no longer standing on its end) but yaw/position still wrong (facing
sideways, stacked front-to-back instead of side-by-side), switched
approach — Ben is now live-tweaking `PlayerBoot`'s offsets directly in
the Play-mode Inspector rather than continuing blind guess-and-screenshot
rounds. No code change from that yet; final numbers will get baked in as
the new defaults once found.

## 2026-08-13 (22)

### v0.3.48-dev — Fix: Boot orientation was a pitch problem, not yaw; Backpack yaw tune

Two more corrections from live feedback.

- **Boots, correctly diagnosed this time**: Ben's exact words — "shoes
  should be parallel with the feet... not perpendicular" — with a
  reference photo. This reframed the whole problem: it was never a
  front-to-back (yaw) issue, which is the axis every prior Boot rotation
  attempt tried. It's the same ground-lying-vs-mounted pitch mismatch
  already diagnosed and fixed on the Backpack and Jeans —
  `worldPickupPrefab` is authored to lie flat on the ground for display,
  so with no pitch correction the shoe stands on its end (toe pointing
  up, perpendicular to the ground) instead of lying flat with the toe
  pointing forward (parallel). Applied the same `-90°` X pitch correction
  used for Backpack/Jeans, removing the yaw entirely.
- **Backpack, a precise instruction this round, not a guess**: "backpack
  should be rotated on the vertical axis 90 degrees." Added that 90° yaw
  on top of the existing 180° (net `-90°`/`270°`). Mirrored onto the 3
  NPC job assets' Backpack `attachEulerOffset` for consistency.

Both compiled clean. Boot's pitch axis is now backed by a clear
diagnosis (not blind trial); the Backpack yaw is a direct instruction
rather than a guess — both still need live re-confirmation.

## 2026-08-13 (21)

### v0.3.47-dev — Revert: Boot rotation trial made it worse

Live feedback confirmed two things this round: the Backpack yaw-only
revert (v0.3.46-dev) looks correct now — matches a reference photo Ben
supplied of normal backpack-wearing posture. And the Boot `180°` yaw
trial from v0.3.43-dev made the shoes look jumbled/overlapping instead
of clean, on a placement that was already confirmed correct before that
trial. Same mistake pattern as the Backpack: a speculative rotation
change added on request rather than because anything was actually
broken, never reverted once it turned out not to help. `PlayerBoot.
wornEulerOffset` reverted to identity, the last confirmed-good state.

**Where this leaves the equipment-visual work**: Backpack and Boots are
both confirmed correct now. Belt/Canteen drop cascade (v0.3.46-dev) not
yet re-verified live. The other 7 types (Sunglasses, Mining Face Shield,
Personal Health Monitor, Navigation Computer, Shirt, Jeans, Canteen
worn-position) still haven't had any live confirmation at all.

## 2026-08-13 (20)

### v0.3.46-dev — Fix: real Belt/Canteen drop path; revert a mistaken Backpack pitch

Two fixes, both from live testing.

- **The actual bug behind "canteen still attached after dropping the
  belt"**: the v0.3.45-dev fix (`PlayerBelt.DropClippedEquipment`) was in
  the wrong place — the Inventory screen's Drop button
  (`DrawItemDropPopup`) calls `PlayerDropping.DropFrom` directly, not
  `PlayerBelt.Drop`, so that fix never ran for the actual reported case.
  Moved the cascade logic into `PlayerDropping.DropFrom` itself and
  generalized it beyond Belt/Canteen: any `IInventoryHolder` equippable
  (a worn Backpack holding another equipped item, etc.) now cascades the
  drop to whatever physical equipment is nested in its own `Inventory`.
  `PlayerBelt.DropClippedEquipment` stays too — still correct for
  `PlayerBelt.Drop`'s own internal callers — just no longer the fix for
  the path that was actually broken.
- **Reverted a mistaken Backpack rotation.** Tracing back through the
  live-feedback rounds: the Backpack's position was already confirmed
  correct by v0.3.42-dev. Round 3's `-90°` X pitch was added based on a
  wrong diagnosis — a floating shape thought to be part of the Backpack
  model that turned out to be the Jeans (fixed separately, v0.3.44-dev/
  v0.3.45-dev). That pitch correction was never undone once the real
  culprit was found, and it broke what was already working — confirmed
  by Ben supplying a reference photo of correct backpack-wearing posture
  and reporting it "not aligned at all." Reverted `PlayerBackpack.
  wornEulerOffset` (and the 3 NPC job assets' matching Backpack
  `attachEulerOffset`) back to yaw-only (`0, 180, 0`), the last
  confirmed-working rotation.

## 2026-08-13 (19)

### v0.3.45-dev — Fix: Belt drop orphaned a clipped Canteen; Jeans rotation round 5

Two fixes from live testing (Ben: dropped Shirt and Belt, "minor bug -
the canteen should have dropped as well").

- **Real logic bug, `PlayerBelt.Drop`**: a Canteen clipped to a worn Belt
  is a pure data relationship (registered in `belt.Inventory`), not a
  Transform-hierarchy one — its physical object is bone-attached directly
  via `EquipmentAttach.Carry`, not parented under the Belt GameObject.
  Dropping the Belt alone left the Canteen still visibly "worn," floating
  in place with no owner. New `PlayerBelt.DropClippedEquipment` detaches
  and drops every physical equipment item still clipped to the belt in
  the same call, scattered slightly so they don't all land exactly on top
  of the belt itself.
- **Jeans rotation, round 5**: the `-90°` X pitch from v0.3.44-dev was
  real progress — moved from "straight up past the head" to "sideways,
  hanging near the hand/arm" — confirming X-pitch is the right axis, just
  not enough of it. Doubled to `-180°` to swing the rest of the way from
  horizontal to pointing down.

Both compiled clean; the rotation number is still an unconfirmed guess.
**Backpack alignment reported still wrong** (Ben, no screenshot yet this
round) — holding off on a blind third guess there pending clearer
evidence of what's actually off now (position vs. rotation vs. something
else).

## 2026-08-13 (18)

### v0.3.44-dev — Fix: floating blue shape was Jeans, not the Backpack (live-feedback round 4)

Live feedback (Ben: "that's not any better") revealed a misdiagnosis, not
just a wrong number: the persistent blue/black rolled shape above the
head survived unchanged across two rounds of Backpack-only tuning (a Y
position fix, then an X pitch fix) — meaning it was never the Backpack in
the first place. Re-diagnosed by elimination: color matches denim, and
`PlayerJeans` was the one worn-clothing carrier that still had zero
rotation correction (`wornEulerOffset` at identity) while every other
type had at least attempted one. Same theory as the Backpack fix applies:
`worldPickupPrefab` is authored lying flat on the ground (the convention
every dropped pickup in this game uses) — with no rotation correction,
Jeans' legs point in whatever direction was "up" while lying flat, which
parented to an upright Hips bone reads as legs pointing straight up past
the head instead of down. Added the same `-90°` X pitch correction
already tried on the Backpack. Same coin-flip-on-sign caveat as before —
not yet re-verified live.

## 2026-08-13 (17)

### v0.3.43-dev — Tune: Backpack pitch, Boot rotation (live-feedback round 3)

Live feedback (Ben, two screenshots): the Backpack's bag body now sits
correctly on the back (v0.3.42-dev's Y fix worked), but the same rigid
model has a rolled bedroll-style extension at its top that juts up past
the head. Theory, not just another blind number: `worldPickupPrefab` is
authored to look right *lying flat on the ground* (how every dropped
pickup in this game displays) — the existing 180° yaw only corrects
which way it faces, not the pitch needed to stand a ground-lying prop
upright against a back. Added a `-90°` X (pitch) correction on top of
the existing yaw, to `PlayerBackpack.wornEulerOffset` and mirrored onto
the 3 NPC job assets' Backpack `attachEulerOffset` (same consistency
convention as the position fix). Genuinely a coin flip on sign — could
need `+90` instead if this over/under-rotates it the wrong way.

Boots: position confirmed correct, but asked to try rotating too (may be
front-to-back reversed) — trying a 180° yaw flip on `PlayerBoot.
wornEulerOffset` as the first attempt.

Both unconfirmed guesses, not yet re-verified live.

## 2026-08-13 (16)

### v0.3.42-dev — Tune: Backpack sat too high (Chest bone, live-feedback round 2)

Live feedback on v0.3.41-dev (Ben, screenshot): Boots now correctly at
the feet (confirmed working), but the Backpack sat far too high — near
the neck/shoulders instead of the back. Root cause understood, not just
tuned blind: `HumanBodyBones.Chest` sits quite high on a Humanoid rig
(near the collarbone), and the original offset only pushed the model
backward (`z: -0.15`), never down — so it hovered above the shoulders
rather than centering on the back. Added a real downward push
(`y: -0.3`, `z: -0.2`) to `PlayerBackpack.wornPositionOffset`, mirrored
onto `MineOreJob`/`ChopWoodJob`/`ForageJob`'s Backpack
`attachPositionOffset` to keep player and NPC placement consistent (the
deliberate design from v0.3.41-dev). Belt also looked off in the
screenshot, but from a rear-oblique angle that's plausibly just
occlusion/camera angle rather than a real bug — left untouched pending
clearer evidence. Still an unconfirmed guess, not yet re-verified live.

## 2026-08-13 (15)

### v0.3.41-dev — Full equipment-visual sweep: every IEquippable now bone-attaches

Ben's ask, after seeing v0.3.40-dev live: "let's determine all the
equipment placement change before implementing" — a Backpack/Sneakers
misalignment report plus "pickaxe isn't wired to the hand" turned into a
full audit (`Explore` agent) before touching anything. Findings and fixes:

- **Real bug, not misalignment (explains the Pickaxe): a second, older
  equip path bypassed bone-attachment entirely.** `PlayerLoot.
  ReceiveEquipment`'s hand-fill branch (world pickup into a free hand —
  the common case) called `equippable.SetCarried(true, transform)`
  directly, parented to the **player root**, never touching `PlayerTool`'s
  bone-attach logic — a Pickaxe picked up normally off the ground landed
  at the player's root origin, effectively invisible, despite being
  correctly registered as equipped. Only the inventory-screen equip path
  (`PlayerTool.EquipTo`) went through bone-attachment; a Pickaxe equipped
  that way would have worked. `Canteen` had the identical exposure (also
  accepts hand slots). Fixed: `PlayerLoot` now dispatches to `PlayerTool.
  CarryPickedUp`/`PlayerCanteen.CarryPickedUp` for those two types instead
  of the blanket root-anchor fallback.
- **New `EquipmentAttach.Carry()`** — the repeated "resolve bone (with
  fixed-Transform-then-root fallback), SetCarried, Place" pattern
  extracted into one shared call, used by every carrier now including
  `PlayerTool`/`PlayerBackpack` (retrofitted, no behavior change there).
- **All 9 remaining `IEquippable` types now bone-attach**, previously all
  on the old fixed-Transform-or-player-root pattern with zero animation
  tracking:
  - `Boot` → `Hips` (a single combined-pair mesh, not two separate
    per-foot meshes — a static hip anchor is the pragmatic first pass;
    splitting into per-foot meshes attached to `LeftFoot`/`RightFoot`
    would be a bigger, separate change if this doesn't read well live).
  - `Belt` → `Hips`, `Shirt` → `Chest`, `Jeans` → `Hips` (body-conforming
    garments/accessories, identity offset as a first guess).
  - `Canteen` → `LeftHand`/`RightHand` (per which hand, preserving its
    existing two-anchor distinction) or `Hips` for the belt-clip case.
  - `Sunglasses`/`MiningFaceShield` → `Head`, small forward offset.
  - `PersonalHealthMonitor`/`NavigationComputer` → `LeftLowerArm`/
    `RightLowerArm` (per which wrist was actually chosen, not collapsed
    to one side the way Tool's hand attachment is).
- **`PlayerBodyModel.ApplyGender` now re-anchors all 11 carriers**, not
  just Tool/Backpack — every equipped item was bone-parented under the
  *previous* gender's bones, so a gender switch needs every carrier's
  `RefreshAnchor()` called or items stay attached to a now-inactive body.
- All offset numbers are first-pass guesses (same honest framing as
  every placement change this session) — expect a live-tuning pass once
  Ben looks at all of these worn simultaneously. Verified via batch-mode
  compile (0 CS errors, clean on the first full-batch attempt) only —
  **not yet live-tested**.

## 2026-08-13 (14)

### v0.3.40-dev — Player equipment now bone-attaches too (same system as NPCs)

Ben's ask: apply the same equipment-visual treatment to the player, partly
so he can test the placement directly in third person rather than only
watching an NPC. The player is a different case from NPCs — a held
Pickaxe/worn Backpack is a *real* physical `IEquippable` object already
(`Tool`/`Backpack`, moved via `SetCarried`), not pure bookkeeping needing
a decorative copy — so this isn't `NPCEquipmentVisual` reused, it's the
same *placement math* applied to the existing carry system.

- **New `EquipmentAttach.cs`** — extracted the root-relative placement
  formula `NPCEquipmentVisual` already uses (and just got fixed in
  v0.3.39-dev) into a shared static helper, so both call sites use
  identical, already-tested logic instead of two copies drifting apart.
  `NPCEquipmentVisual` now calls it too (pure refactor, no behavior
  change there).
- **`PlayerBodyModel` gained `GetBone(HumanBodyBones)`** — returns a bone
  from whichever gendered Visual is currently active, not a fixed scene
  reference, since gender can change at runtime.
- **`PlayerTool`/`PlayerBackpack`** now resolve their anchor through
  `PlayerBodyModel.GetBone` (`RightHand` / `Chest`) instead of the old
  fixed `handAnchor`/`carrySlot` scene objects — those two Transforms
  (`HandAnchor`, `BackpackAnchor`, plain children of the Player root with
  hand-picked static offsets, predating the visible body) are kept only
  as a fallback if `PlayerBodyModel` or the bone lookup isn't available.
  The old anchors never moved with animation at all; bone attachment
  does. Backpack reuses the exact same starting offset numbers as
  `NPCJobDefinition`'s Backpack requirements (`(0,0,-0.15)` + 180° turn)
  for a consistent starting guess between player and NPC.
  Tool starts at identity offset, same as NPCs' Pickaxe/Axe.
- **Gender-switch re-anchoring**: `PlayerBodyModel.ApplyGender` now calls
  `PlayerTool.RefreshAnchor()`/`PlayerBackpack.RefreshAnchor()` after
  swapping the active Visual — without this, anything currently held/worn
  would stay parented under the *previous* gender's now-inactive (and
  invisible) model instead of moving to the new one.
- Verified via batch-mode compile (0 CS errors) only — **not yet
  live-tested**, same honest framing as every placement change this
  session. This is the piece Ben can test directly, though, unlike the
  NPC-only equipment work.

## 2026-08-13 (13)

### v0.3.39-dev — Fix: NPC equipment visual — Pickaxe invisible, Backpack misplaced

Live feedback on v0.3.38-dev (Ben: gave an NPC a Fine Pickaxe — no Pickaxe
appeared at all; the Backpack appeared but sat wrong, near the hand
instead of on the back). Two real bugs, not just "needs tuning":

- **The Pickaxe wasn't just misplaced, it was gone** — `Tool.cs`
  (`FinePickaxePickup.prefab` and every other tool tier) declares
  `[RequireComponent(typeof(Rigidbody))]`/`[RequireComponent(typeof(Collider))]`.
  `NPCEquipmentVisual`'s original `StripWorldBehavior` called `Destroy()`
  on exactly those two components — Unity silently refuses to destroy a
  component something else on the same object still requires (logs an
  error, leaves it in place), so the Rigidbody survived, still
  non-kinematic, still gravity-affected. A live Rigidbody child isn't
  actually carried by its parent Transform once physics starts simulating
  it — it falls/drifts away under gravity independent of the hand bone it
  was parented to. That's almost certainly why it read as "not showing"
  at all. Fixed by disabling instead of destroying (`Rigidbody.isKinematic
  = true`, `Collider.enabled = false`, `Pickup`/`ResourceNode.enabled =
  false`) — a kinematic Rigidbody is purely transform-driven, so it just
  follows the bone like any other child, no physics involved.
- **Offsets were interpreted in the wrong space** — `attachPositionOffset`/
  `attachEulerOffset` were applied as the instance's *local* position/
  rotation under the attach bone, meaning "0.15 back" meant "0.15 along
  whatever direction that bone's own local Z axis happens to point" — a
  hand/chest bone's local axes reflect its bind-pose orientation, which is
  rig-specific and not something to guess blind (explains why the
  Backpack's rearward offset instead put it up near the hand). Fixed by
  computing the offset relative to the **NPC's root transform**
  (`transform.TransformVector`/`transform.rotation *`) instead — "0.15
  behind" now reliably means behind the character regardless of which
  bone it's parented to. Position still tracks the bone during animation
  (still parented as its child, same as before) — this only changes what
  the *initial* attach offset means.
- Verified via batch-mode compile (0 CS errors) only — **still not
  live-tested**, this fix is itself unverified until Ben looks again.
  `TEST_FEATURE_PLAN.md` section 27 covers this.

## 2026-08-13 (12)

### v0.3.38-dev — NPC equipment visual attachment (Pickaxe/Axe/Face Shield/Backpack)

Ben's ask, direct follow-up to the animation work: "have the npc equip to
body, the equipment we give him." Previously an NPC's given tools were
pure bookkeeping (`NPCJob.equippedTools`, a label→item dictionary) — the
Pickaxe/Axe a player handed over never appeared on the model.

- **New `NPCEquipmentVisual.cs`** — each frame, diffs `NPCJob.EquippedTools`
  (new public accessor) against what's currently attached and
  instantiates/destroys accordingly. Reuses each `ItemDefinition`'s own
  `worldPickupPrefab` (the same mesh a dropped item uses) as the held
  model — no dedicated held-model asset exists for any tool, and this
  project's convention has always been "ship the obvious v1 from what
  already exists, tune live" rather than blocking on new art. Strips the
  instantiated copy's `Rigidbody`/`Collider`/`Pickup`/`ResourceNode`
  components — a bone-parented decoration shouldn't also be an
  independently-interactable world object.
- **`ToolRequirement` gained `attachBone`/`attachPositionOffset`/
  `attachEulerOffset`** (`NPCJobDefinition.cs`) — attach point is data on
  the requirement itself, not hardcoded per label in script, so retuning a
  position later is a data edit. Wired: Pickaxe/Axe → `RightHand` (default,
  identity offset), Mining Face Shield → `Head` (small forward offset),
  Backpack → `Chest` with a rearward offset + 180° turn (a front-facing
  dropped-pickup model needs flipping to read as worn on the back).
- Added to both `NPCFactoryWorkerMale.prefab`/`NPCFactoryWorkerFemale.prefab`,
  wired to each one's own `Animator` (`Animator.GetBoneTransform` needs a
  real Humanoid rig — confirmed both `HumanCharacterDummy_M/F.fbx` import
  as Humanoid, not Generic).
- **All three offset numbers are first-pass guesses, not yet visually
  confirmed** — same honest framing as every other new-model placement
  this session (per `CLAUDE.md`'s pivot-grounding/scale-vs-player rules,
  visual placement always needs a live look before calling it done, and
  this hasn't had one yet). Expect the Pickaxe/Axe grip and Backpack
  position to need real tuning once actually seen on an NPC.
- Player-side equipment visual attachment (third-person) remains
  explicitly out of scope — Ben's ask was scoped to NPCs; the player's own
  `PlayerCameraMode.cs` third-person view still only reveals worn
  equipment via the `cullingMask` flip, not correctly bone-positioned
  (documented gap since v0.3.34-dev).
- Verified via batch-mode compile (0 CS errors) + YAML grep (all 4
  `ToolRequirement`s' new fields, both prefabs' `NPCEquipmentVisual`
  component with a non-null `animator` reference). **Not yet
  live-tested** — see `TEST_FEATURE_PLAN.md`.

## 2026-08-13 (11)

### v0.3.37-dev — Fix: Mining NPC getting distracted by loose Sticks

Bug report (Ben, live): a Mine Ore NPC's cargo showed a maxed Stick x20
stack plus Herb/Plank alongside its ore, and it seemed "stuck gathering
sticks" instead of continuing to mine.

Root cause: `NPCGathering.FindTarget`'s loose-`Pickup` pool (added
v0.3.32-dev to close the loop after a Forage job's bush search scatters
items) was scanned **unconditionally for every job**, not just Forage.
With no job-relevance filter, a Mining or Woodworking NPC would compete
for whatever loose item was nearest on pure distance — a cluster of
light, always-in-range Sticks near a chop site could out-compete
farther-away ore every time `FindTarget` re-ran, effectively stalling the
NPC's actual assigned job. This is the real-world manifestation of the
side effect `NPC_JOB_GENERALIZATION_PLANNING.md` section 3a flagged as
"worth Ben's explicit sign-off before shipping, not an assumption baked
in silently" — now that sign-off has come back as "no, don't do that."

Fixed with a new per-job opt-in: `NPCJobDefinition.collectLoosePickups`
(default `false`). Only `ForageJob.asset` sets it `true` — the only job
whose targets (`BerryBush`/`HerbBush`) don't yield directly into cargo
and actually need the follow-up collection step. `MineOreJob`/
`ChopWoodJob` need no data change (default already correct). Verified via
batch-mode compile (0 CS errors) + YAML grep of `ForageJob.asset`.

## 2026-08-13 (10)

### v0.3.36-dev — Fix: NPC job tool requirements only accepted one CraftTier

Bug report (Ben, live): a Fine Backpack in inventory couldn't be given to
an NPC — the Assign Job screen kept showing "(none in inventory)" and a
disabled Give button for the Backpack requirement.

Root cause: `NPCJobDefinition.ToolRequirement.acceptableItems` is meant to
follow the same "any tier satisfies it" convention every other tool gate
in this project uses (`ResourceNode.requiredTools`, `CraftingRecipe.
requiredTools` — an array of all 5 `CraftTier` variants, not just one).
`MineOreJob.asset`'s Backpack requirement only listed the Normal-tier
`BackpackItem` guid, and its Pickaxe requirement only listed the
Crude-tier `CrudePickaxe` guid — a Fine Backpack or any non-matching
Pickaxe tier silently didn't count. `ChopWoodJob.asset`/`ForageJob.asset`
(built this same session, v0.3.32-dev) inherited the identical narrow
pattern for their own Backpack requirement, copied from `MineOreJob`
without checking whether the source was itself correct.

Fixed directly in the data — all three job assets' Backpack requirements
now list all 5 `CrudeBackpackItem`/`RudimentaryBackpackItem`/
`BackpackItem`/`FineBackpackItem`/`MasterworkBackpackItem` guids;
`MineOreJob`'s Pickaxe requirement now lists all 5 Pickaxe tiers too.
Mining Face Shield untouched — confirmed it has no tier ladder (a single
item), so 1 guid was already correct there. No code changes — this was
purely a data-authoring gap, not a bug in `NPCJob.TryGiveTool`/
`NPCJobScreen.HasAny`'s matching logic itself. Verified via batch-mode
load (0 import errors) + YAML grep of all three edited assets.

## 2026-08-13 (9)

### v0.3.35-dev — Player body: Male/Female toggle (` menu, Player tab)

Direct follow-up to v0.3.34-dev, same day: that build shipped a fixed
Male `Visual` (the `HumanDummy_M White` Kevin Iglesias prefab) with no way
to pick Female. This adds the missing choice as the first real content in
`GameMenuScreen`'s previously-blank Player tab.

- **Both gendered Visual instances now exist simultaneously** as siblings
  under the Player transform — a new `Visual_Female`
  (`HumanDummy_F White` prefab, same `WornEquipmentLayer` treatment and
  identity local transform as the renamed `Visual_Male`) sits inactive
  until toggled on, rather than being instantiated/destroyed at toggle
  time. Deliberate choice: `PlayerAnimatorDriver` and `NPCVisualGroundFix`
  each hold a direct serialized reference into whichever Visual is "the"
  body — destroying the active one out from under them would orphan those
  references, the same "don't carry a reference across a destroy
  boundary" caution `CLAUDE.md` documents for editor-script prefab-content
  edits, just applying to a runtime swap instead of a batch-mode one.
- **New `PlayerBodyModel.cs`** — `SetGender(bool male)` just
  `SetActive`-toggles both instances and re-points
  `PlayerAnimatorDriver`/`NPCVisualGroundFix` at whichever is now active
  (via two new small setters on those classes:
  `PlayerAnimatorDriver.SetAnimator`, `NPCVisualGroundFix.SetVisual` —
  the latter also resets that script's ground-correction `initialized`
  flag so it re-measures from the newly-active model's own bind pose
  instead of reusing the previous gender's offset). Male is the default,
  matching what v0.3.34-dev already shipped, so a fresh scene load looks
  unchanged unless the player actually opens the menu and switches.
- **`GameMenuScreen.DrawPlayerTab()`** — two tab-style buttons (reusing
  the same `TabSelected`/`TabUnselected` styling `DrawTabBar`/
  `NPCJobScreen`'s family tabs already use), calling `PlayerBodyModel.
  SetGender` directly. No new key binding, so `ControlsList` doesn't need
  an entry.
- Verified via batch-mode compile (0 CS errors) + YAML grep of the saved
  scene (`PlayerBodyModel`'s 4 field references all non-null,
  `Visual_Female`'s Animator controller override pointing at
  `PlayerAnimatorFemale.controller`, `m_IsActive: 0` confirming it starts
  hidden). **Not yet live-tested** — see `TEST_FEATURE_PLAN.md`.
- One coordination note, not a build detail: this landed right after
  v0.3.33-dev/v0.3.34-dev shipped from a second, concurrent Claude Code
  session (NPC animation, then player visible body) — held off starting
  this until that session's `TestScene.unity` edits were actually
  committed and pushed, specifically to avoid two uncommitted batch-mode
  scene saves racing each other (`WORKING_ON.md` is what caught the
  overlap before it became a real collision).

## 2026-08-13 (8)

### v0.3.34-dev — Player visible body + first/third-person camera toggle

MVP2 item 4, player half — direct follow-up to v0.3.33-dev's NPC animation
pass, same day. The player previously had zero visible mesh (invisible
`CharacterController` + eye-height camera only); third person is
meaningless without something to actually see, so this ships both
together.

- **`FirstPersonController.Pitch`/`CurrentStance`** — two new read-only
  properties, no behavior change, same reasoning as `NPCGathering`'s
  animation-driving properties: avoids re-deriving pitch from the camera's
  baked local-rotation Euler angles later, and avoids scattering raw
  private-field reads across new scripts.
- **`PlayerAnimatorDriver.cs`** (new) — mirrors `NPCAnimatorDriver.cs`'s
  frame-delta Speed technique. Also tracks the previous stance to fire a
  `StanceChanged` trigger only on an actual change.
- **`PlayerAnimatorMale.controller`/`PlayerAnimatorFemale.controller`**
  (new) — 9 states (Idle/Walk/Sprint × Standing, Idle/Walk × Kneeling/
  Crawling/Prone), driven by `Speed`/`IsSprinting`/`Stance`/
  `StanceChanged`. The stance-select Any-State transitions are gated on
  **both** `Stance == N` and the `StanceChanged` trigger, not `Stance == N`
  alone — a level-only gate would re-trigger every frame while already in
  that stance and stomp the Idle↔Walk blending happening inside it.
  Crawling reuses the pack's single Prone-crawl clip (there's no separate
  skeletonized crawling-idle in the source pack) paired with the
  stationary Prone-idle clip for its own idle state.
- **`PlayerCameraMode.cs`** (new) — V-key toggle. Reveals the body via a
  single `cullingMask` flip (the `Visual` body's renderers sit permanently
  on `WornEquipmentLayer`, the same layer worn equipment already uses and
  which the camera already excludes project-wide) rather than a new layer
  or per-frame visibility toggling. As a side effect, currently-worn
  equipment also becomes camera-visible in third person — correct
  behavior, not a bug, though it won't be bone-attached/positioned
  correctly since equipment-visual-attachment is still unbuilt. Third
  person is a chase camera following wherever the player's already facing
  (same single mouse-look scheme, no independent orbit state) with a
  SphereCast obstruction clamp — no in-repo precedent existed for camera
  collision, built from scratch.
- Player lives directly in `TestScene.unity`, not a prefab (unlike NPCs) —
  the wiring script used `EditorSceneManager.OpenScene`/`SaveScene`
  instead of `PrefabUtility.LoadPrefabContents`, a genuinely different
  mechanic from the NPC build, not a copy-paste of it.
- Explicitly out of scope, same standing decision as before: a
  first-person arms/view-model, and equipment-visual attachment (bone
  positioning). Verified via YAML grep (no null motion refs across either
  controller, all component references on the Player object non-null) —
  script deleted after running. **Not yet live-tested** — see
  `TEST_FEATURE_PLAN.md` section 25.

## 2026-08-13 (7)

### v0.3.33-dev — NPC animation: locomotion + per-job work actions

MVP2 item 4 (NPC half only — player-body animation deferred to a separate
plan). NPCs previously had a proof-of-pipeline single-state Idle Animator
Controller (`NPCIdle.controller`, built 2026-08-11 just to surface/fix the
Humanoid-retargeting ground-sink bug `NPCVisualGroundFix` corrects) — no
Walk state, nothing reacting to movement or job actions. Replaced with a
real controller per gender.

- **`NPCJobDefinition.WorkAnimationType`** (`None`/`Mining`/`Chopping`/
  `Gathering`) — a new data-driven field tagging which Kevin Iglesias
  `Work/` animation loop a job plays, set on the three shipped job assets
  (`MineOreJob`→Mining, `ChopWoodJob`→Chopping, `ForageJob`→Gathering). The
  pack's `Work/` subfolder names happened to line up 1:1 with the three
  job families already built in `NPC_JOB_GENERALIZATION_PLANNING.md`.
- **`NPCGathering.IsActingOnTarget`/`CurrentWorkAnimation`** — two new
  read-only properties, no behavior change; expose the existing
  `harvestTimer` dwell window (already tracked for the harvest/search
  action itself) to the animator layer.
- **`NPCAnimatorDriver.cs`** (new) — drives `Speed`/`IsWorking`/`WorkType`
  Animator parameters. Deliberately computes `Speed` from raw frame-to-
  frame position displacement rather than reading `NPCWander`/
  `NPCGathering`'s internal movement state directly, so it stays correct
  regardless of which script currently owns the NPC's transform.
- **`NPCAnimatorMale.controller`/`NPCAnimatorFemale.controller`** (new,
  replacing `NPCIdle.controller` on both Factory Worker prefabs) — Idle/
  Walk (`Speed` threshold) plus WorkMining/WorkChopping/WorkGathering
  (any-state transition on `IsWorking` + `WorkType`, back to Idle once
  `IsWorking` clears). v1 uses each job's single `...Loop` clip directly
  (`HumanM/F@Mining01 - Loop Ground`, `TreeChopping01 - Loop`,
  `Gathering01`) — the pack's Begin/Stop transitional clips for Mining/
  Chopping are deliberately skipped for now, addable later without
  restructuring anything.
- Built via a throwaway batch-mode Editor script (`AnimatorController` API
  — states/params/transitions are fully scriptable), split into two
  separate `-executeMethod` invocations (controller-build+job-tagging,
  then prefab-wiring) specifically so the prefab-wiring phase's
  `PrefabUtility.LoadPrefabContents` cycle couldn't leave any reference
  from the first phase stale, per `CLAUDE.md`'s documented gotcha.
  Verified via YAML grep (no `fileID: 0` motion references, correct
  controller guids on both prefabs) — script deleted after running.
- **Not yet live-tested** — per `CLAUDE.md`, Humanoid retargeting can't be
  reliably evaluated in batch mode. Needs a Play-mode pass checking foot
  sliding on Walk, whether `NPCVisualGroundFix` still keeps feet planted
  under the new work-action poses (a mining/chopping stance may sink
  differently than the idle pose did), and that transitions read sensibly
  across a full ~3s harvest cycle.

## 2026-08-13 (6)

### v0.3.32-dev — NPC job generalization: Woodworking + Berry/Herb foraging

Built from `NPC_JOB_GENERALIZATION_PLANNING.md`'s design (same day). The
Hireable NPC system's gathering loop, formerly mining-only in name (though
mostly generic already in code), now spans three job families with the
same underlying loop.

- **`NPCMining.cs` renamed to `NPCGathering.cs`** — a `MonoBehaviour`
  rename, not a functional one (Unity resolves the component by script
  GUID, preserved via a hand-carried-over `.meta` file, so
  `NPCFactoryWorker.prefab`'s existing component reference survived
  intact — verified via YAML grep, not assumed). Internal fields renamed
  too (`mineRange`/`mineDuration` → `harvestRange`/`harvestDuration`,
  `MineCurrentTarget` → `HarvestCurrentTarget`) — the prefab's serialized
  values were migrated by hand in the same commit (a plain rename doesn't
  auto-carry old field values forward; see CLAUDE.md's serialized-default
  gotcha) rather than relying on the new field defaults happening to
  match, even though in this case they did.
- **New `INPCHarvestable` interface** — `ResourceNode` (already had this
  exact shape as `TryMineForNPC`, just renamed to `TryHarvestForNPC`) and
  `ChoppableTree` (new — standing Trees previously had no NPC-compatible
  path at all) both implement it. `ChoppableTree` gained the same
  scatter-for-player/direct-yield-for-NPC split `ResourceNode` already
  established: `Complete()` (the player's hold-to-chop) is completely
  untouched, `TryHarvestForNPC` instead yields a new `logItem` field
  directly into cargo and skips the physical Log-scattering step
  entirely, since an NPC has no "walk over and collect what I just
  knocked loose" action.
- **New `INPCSearchable` interface** — `BerryBush`/`HerbBush` implement it
  (F-search half only; Ben's explicit call to skip `BerryBush`'s separate
  chop-for-Trimmed-Stick action, which stays player-only). Kept as its
  own interface rather than folded into `INPCHarvestable`, since
  triggering a search doesn't put anything into cargo directly — it just
  seeds the world with `Pickup` objects.
- **`Pickup.cs` gained an NPC-safe collection path** (`TryPickupForNPC`,
  plus `SkillGain`/`Quantity` read accessors) — same "no `PlayerLoot`/
  `PlayerInventory` dependency" treatment `ResourceNode`/`ChoppableTree`
  already got.
- **`NPCGathering.FindTarget` now scans three candidate pools**, not one:
  `INPCHarvestable` targets (walk to it, harvest, cargo grows
  immediately), `INPCSearchable` targets (walk to it, trigger the search,
  nothing lands in cargo yet), and loose `Pickup` objects already in the
  world (walk to it, collect, cargo grows) — the last pool is what closes
  the loop after a bush search: on a later pass, the NPC finds the
  `Pickup`s its own search produced (or any other loose one nearby) and
  collects them. No new state machine needed — this falls out of the
  existing "keep finding and doing the nearest useful thing" loop for
  free. **Known, flagged side effect, not a bug:** since loose `Pickup`s
  are scanned generically, a foraging NPC will also collect any other
  nearby dropped item, not just what its own search produced.
- **New data**: `ChopWoodJob.asset` (family Woodworking, requires an Axe
  — any tier — + a Backpack) and `ForageJob.asset` (family Gathering, no
  tool beyond a Backpack, covers both `BerryBush` and `HerbBush` as one
  combined job rather than two — matches how `MVP2_PLANNING.md` already
  bundled "Gathering (Berry/Herb bushes)" as one line item). Both wired
  into `NPCJobScreen.families`/`jobs` alongside the existing Mining
  family. `Tree.prefab`'s `ChoppableTree` component got its new
  `logItem` field pointed at the real `Log` `ItemDefinition`.
- Deterministic-yield precedent (matching the Furnace/Campfire automation
  from `v0.3.31-dev`) doesn't apply here — gathering was already
  deterministic before this change (`ResourceNode.TryMineForNPC` never
  had a risk roll); nothing new introduced one.
- Verified via batch-mode compile (0 CS errors, caught and fixed one
  missed reference in `NPCDialogue.cs` that the first compile pass
  flagged) + YAML grep of the saved assets/scene (`ChopWoodJob.asset`/
  `ForageJob.asset` contents, `Tree.prefab`'s `logItem`,
  `NPCJobScreen.families`/`jobs` arrays, `NPCFactoryWorker.prefab`'s
  renamed fields). **No live Play-mode pass yet** — see
  `TEST_FEATURE_PLAN.md`. Bench-crafting families (Metalworking, Sewing,
  etc.) remain explicitly deferred, per `NPC_JOB_GENERALIZATION_PLANNING.md`
  section 7.

## 2026-08-13 (5)

### v0.3.31-dev — Furnace real state + unattended automation

Furnace goes from a bare `FurnaceSurface` proximity marker (see
`WOOD_AND_FUEL_PLANNING.md`) to a real, self-contained production
structure — same popup family as Campfire (`FurnaceScreen`, opened by E),
but built for automation rather than a player standing there watching it.
Ben's ask: apply the Campfire treatment, but let the player select an
output box, queue up to 4 recipes, and designate a nearby StorageBox each
for fuel and raw materials.

- **New `Furnace.cs`** on the existing scene `Furnace` GameObject
  (`FurnaceSurface` stays untouched alongside it — `CraftingRecipe.
  requiresFurnace`/`IronIngotRecipe` still gate purely on that marker,
  unrelated to this new system). On-board Fuel (2 slots, same `FuelTier`/
  `FuelItem` system as Campfire), Materials (4 slots), and Output (4 slots)
  inventories.
- **New `SmeltableItem.cs`** ScriptableObject — deliberately separate from
  `CraftingRecipe` even though `IronIngotRecipe` already smelts Iron Ore
  near a Furnace today. That recipe is player-driven (skill-gated,
  chance-of-creation roll, crafted from the Crafting tab); `SmeltableItem`
  is for the Furnace's own unattended queue — deterministic, no skill, no
  risk — same reasoning `CookableItem` stayed its own type instead of
  reusing `CraftingRecipe` for Campfire cooking. First instance:
  `IronOreToIngotSmeltable` (10 Iron → 1 Iron Ingot, 60s).
- **Up to 4 queued recipes, sequential** (Ben's call via `AskUserQuestion`):
  `FurnaceScreen` lists every registered `SmeltableItem` with a toggle
  button; the Furnace round-robins through whichever are queued, one at a
  time, so an always-satisfiable recipe up front can't starve the others.
- **True unattended automation** (Ben's call — pulls forward part of
  `WOOD_AND_FUEL_PLANNING.md`'s section 5 vision): `Furnace.Update()` ticks
  every frame regardless of whether the player is nearby or has the popup
  open, same as Campfire's fuel timer already does. With Auto-Run on, the
  Furnace lights itself whenever it has fuel and a non-empty queue — no
  Light button, since the point is it works with nobody there to click one.
- **Three optional StorageBox links** (Fuel Source / Materials Source /
  Output), assigned via `FurnaceScreen`'s picker — lists every StorageBox
  within the Furnace's own `storageLinkRange` (not the player's), so
  anything offered actually works once assigned. On-board slots stay the
  source of truth (Ben's call: on-board + auto-refill/drain, not a raw
  passthrough) — `AutoRefill`/`AutoDrain` top up Fuel/Materials from their
  linked boxes and push Output into its linked box each tick, so a
  temporarily unlinked or out-of-range box doesn't stall production.
- `FurnaceScreen.cs` — same self-contained drag-and-drop implementation as
  `CampfireScreen.cs` (not a shared extraction, same reasoning as before).
  Transfer section scoped to Backpack + Hands, matching Campfire's.
- `FirstPersonController` gained `furnaceScreen` field, closed on Escape
  alongside every other popup.
- Verified via batch-mode compile (0 CS errors) + YAML grep of the saved
  scene/asset (Furnace component's `fuelItems`/`smeltableItems` arrays,
  `FurnaceScreen` on the Player GameObject). **No live Play-mode pass yet**
  — see `TEST_FEATURE_PLAN.md`.

## 2026-08-13 (4)

### v0.3.30-dev — Campfire cooking rework: utensils, multi-ingredient recipes, drag-and-drop popup

Real-time iteration on the new `CampfireScreen` popup, driven by Ben
looking at the built UI live and requesting changes in sequence. Cooking
went from a single auto-cooking slot to a full recipe system:

- **`CookableItem` restructured** to mirror `CraftingRecipe`'s own shape
  — `ingredients[]` (item+count array, was a single `rawItem`) and
  `outputItem`/`outputCount` (was a single `cookedItem`). Existing
  `RawMeatToCookedMeatCookable.asset` migrated in place (1 Raw Meat →
  1 Cooked Meat, unchanged behavior, new shape).
- **4 new Cooking Utensil items**: Grill, Cooking Pot, Kettle, Frying Pan
  (`Assets/Data/*.asset`) — plain, non-equippable, maxStack 1, no
  model/icon yet (same known-placeholder gap `CAMPFIRE_PLANNING.md`
  already flagged; admin-spawnable in the meantime, same auto-discovery
  every `ItemDefinition` gets).
- **`Campfire.cs`**: 4 new capacity-1 accessory slots (one per utensil,
  each restricted to exactly that item), a 4-slot ingredient input pool,
  and a 4-slot output bank (all plain `Inventory` instances, same
  restriction mechanism as the existing fuel slot). `GetAvailableRecipes()`
  filters the registered `cookableItems` down to ones whose accessory (if
  any) is seated and whose full ingredient list is present. `StartCooking()`
  is a new manual commit action — consumes ingredients immediately (same
  upfront-consume convention `PlayerCrafting` already uses), starts a
  real-time timer, one recipe at a time. **This replaces the old always-
  auto-cook-when-conditions-are-met behavior — a deliberate philosophy
  change, Ben's explicit call**, not an oversight.
- **`CampfireScreen.cs` rewritten** from simple Add-1/Take buttons to a
  real drag-and-drop UI: Fuel (wood-only), 4 Utensil boxes, 4 Ingredient
  boxes, 4 Output boxes (drag-source only — the cook mechanic is the only
  thing allowed to populate them), a Recipe section listing only
  currently-satisfiable recipes as buttons, and a Transfer section
  scoped to exactly Backpack contents + Left/Right Hand (Ben's explicit
  scope — not the full Inventory tab). The drag-and-drop mechanics are a
  **self-contained implementation inside `CampfireScreen.cs`**, not a
  literal extraction from `InventoryScreen.cs` — deliberately mirrors
  that screen's proven interaction model (press-hold-release with a
  distance threshold, cursor-following ghost, highlighted drop zone) to
  avoid regression risk to a heavily-tested, bug-history-laden file for
  a feature that doesn't need any of its 11-equippable-type dispatch
  logic (every box here is a plain, unequippable `Inventory`).
- **One real infra hiccup mid-build**: a batch-mode Unity compile check
  hung indefinitely after an earlier invocation collided with a leftover
  `bee_backend` process from an interrupted run (an "Editor is open"
  attempt caught and retried per the established protocol). Diagnosed via
  process inspection (confirmed the blocking PID no longer existed, so
  the batch process was genuinely deadlocked, not just slow), confirmed
  with Ben before killing the stuck process, then a fresh invocation
  compiled clean immediately.
- **Three live-feedback fixes**, from Ben looking at the actual popup
  (not something batch-mode verification could have caught):
  1. The Ingredients/Cooked Items grid had zero `GUILayout.Space` between
     boxes — 4 adjacent empty slots visually merged into one solid
     rectangle instead of reading as 4 separate boxes. Fixed with a
     consistent `BoxGap` (8px) applied between every box in the grid, the
     Utensils row, and the Hands row.
  2. The popup's fixed 520x640 panel didn't fit Ben's screen, and his
     touchpad has no working scroll gesture in this window — the Close
     button was genuinely unreachable, not just inconvenient. Panel
     width/height are now responsive (`Mathf.Min(max, Screen.dimension *
     0.92f)`, same pattern `PlayerMenuScreen.DrawScrollable` already
     uses) instead of relying on scrolling to cover the gap.
  3. Section order reshuffled per Ben's request: Cooking Utensils →
     Cooked Items → Recipe → Transfer (Backpack/Hands) → Ingredients →
     Fuel, with Fuel now the last section (bottom of the popup, right
     above Light/Close) and Ingredients directly above it.

Not yet done: models/icons for the 4 utensils (structurally usable via
Admin Spawn today, visually a blank placeholder), and a live Play-mode
pass — this entire rework has been verified via compile checks + YAML
grep only, the same way v0.3.27-dev/v0.3.28-dev were, but the surface
area here is much larger (a real UI redesign built through live back-
and-forth, not a single pre-planned chunk).

## 2026-08-13 (3)

### v0.3.29-dev — Stylized Nature Megapack world-dressing test

Imported "Stylized Nature - Megapack" (Rystek Software, Unity Asset
Store, $27) into `Assets/LJPackages/` and used it to turn `TestScene.unity`
into a 4-biome test bed, at Ben's request ("divide into quarters... throw
some of these in"). The pack turned out to be a bundle of 6 separate
biome sub-packs (AutumnForest, CommonForest, DesertEnvironment,
SpringEnvironment, Wetlands, WinterEnvironment) — 190 prefabs total, URP
Shader Graph wind/water shaders, 28 terrain layers, all `.fbx`/`.png`/
`.exr` already covered by the project's existing Git LFS rules. Confirmed
zero compile conflicts with the project's own scripts (the pack ships no
C# at all).

- **4 quadrants, 4 of the 6 sub-packs**: NE=CommonForest,
  NW=AutumnForest, SW=DesertEnvironment, SE=WinterEnvironment (Wetlands
  and SpringEnvironment held back for a future pass). Terrain spans
  (-100,-100) to (100,100); a 20-unit keep-out radius around the origin
  protects the existing Anvil/Furnace/Campfire base area, left untouched.
- **Props scattered per-category** (groundcover/bushes/rocks/trees/misc,
  classified by simple name-keyword heuristics off each biome's actual
  prefab set) with **every single instance individually grounded by
  measuring its own renderer bounds after scale/rotation** — never
  assumed a pivot-at-base convention across ~500 placed objects from an
  unfamiliar third-party pack, per CLAUDE.md's imported-model rule.
- **Terrain itself re-painted to match**, not just props scattered on
  top of the old uniform grass — added the pack's own terrain layers
  (forestGround/Ground/Sand/Snow) via a new alphamap pass, with a smooth
  bilinear blend at the quadrant cross and a soft blend back to the
  original grass near the base, using the CLAUDE.md-documented
  `SmoothThreshold` helper (not `Mathf.SmoothStep`) for both blends.
- All work done via throwaway batch-mode Editor scripts (scatter,
  terrain paint, preview renders), verified via YAML grep + rendered
  preview screenshots each step, deleted after use.
- **False alarm caught and resolved**: preview renders (crude unbaked-
  lighting test camera) showed a couple of CommonForest tree/bush
  prefabs as solid black silhouettes — flagged to Ben rather than
  guessed at, and confirmed via real in-Editor screenshots to be purely
  a preview-lighting artifact, not a real asset/shader problem. Good
  example of the "verify, don't assume" discipline paying off in both
  directions — catching a real question and also not chasing a phantom
  bug once better evidence arrived.

Not yet done: Wetlands/SpringEnvironment quadrants, any manual terrain
sculpting (height variation between biomes), and a `TEST_FEATURE_PLAN.md`
entry (this is world-dressing, not a player-facing system, so it isn't
covered by that checklist the way gameplay features are).

## 2026-08-13 (2)

### v0.3.28-dev — Campfire dedicated popup UI (E-key)

Replaces the buried "Campfire (nearby)" section that used to sit at the
bottom of the Inventory tab's scroll view with a proper focused popup —
closes out the discoverability finding from the v0.3.27-dev session
(Ben's live report: "there's no mechanism to transfer fuel," even though
the mechanism was technically present, just an unlabeled row on an
already-busy screen). Design was already fully decided in
`CAMPFIRE_PLANNING.md`; this is the build.

- New `CampfireScreen.cs`, same shape/family as the existing
  `LockboxScreen.cs` — a small centered popup (420x420), opened by
  `Campfire.Complete()` (E) instead of the old direct-light attempt.
  Shows lit/unlit status + fuel countdown, the fuel slot (current
  contents + Take, or Add-1 buttons per eligible fuel type currently in
  the player's main inventory), the cooking slot (same shape), and a
  Light button (enabled only when unlit and fuel is loaded) — lighting
  moved from a direct E-tap into this button, per the decided design.
  Deliberately simple/button-based, no drag-and-drop, and deliberately
  scoped to the player's main `PlayerInventory` only (not backpack/worn-
  container contents) — mirrors `LockboxScreen`/`BankScreen`'s equally
  narrow wallet-only scope.
- `Campfire.Prompt` simplified to `"Open Campfire"` (matches Lockbox's
  `"Open {DisplayName}"` convention) — lit/fuel status moved into the
  popup itself instead of the world-space prompt text.
- `Campfire.cs` gained public accessors (`FuelItems`, `CookableItems`,
  `IsLit`, `FuelSecondsRemaining`, `HasFuel`) and a public
  `TryLightFromScreen()` wrapper around the existing private `TryLight()`
  so the popup's Light button can trigger the same path E used to.
- Old "Campfire (nearby)" section removed from `InventoryScreen.cs`
  entirely (fully superseded, not left as dead code) — along with
  `Campfire.Active`/`Campfire.FindNearby`, which had no other caller left
  once that section was gone.
- `CampfireScreen` wired onto the Player GameObject in
  `TestScene.unity` and into `FirstPersonController`'s Escape-closes-every-
  open-screen list, same pattern every other screen (`LockboxScreen`,
  `BankScreen`, etc.) already follows.
- While investigating a related live bug report (Spark/R not lighting a
  fueled Campfire), confirmed via code read that nothing about this
  session's Blender rebuild regressed it — Spark still requires the wish
  to be actively selected in the Magic tab AND held for a real duration
  (no on-screen feedback by design), a likely explanation not yet
  confirmed against a completed hold.

Still open per `CAMPFIRE_PLANNING.md`: the 4 accessory items (Grill/Soup
Pot/Kettle/Frying Pan), Wood Stove, the water-safety mechanic, and
`TEST_FEATURE_PLAN.md`'s open question of whether StorageBox's identical
nearby-section pattern should also move to a popup for consistency.

## 2026-08-13

### v0.3.27-dev — Campfire Blender model rebuild

Replaced the pre-Blender placeholder (5 scaled cylinders) with a real
from-scratch Blender model per `CAMPFIRE_PLANNING.md` section 3: a ring
of 8 irregular low-poly rocks around a shallow-teepee pile of 6 charred
sticks. Built with a persisted, reusable script this time —
`Tools/Blender/GenerateCampfireModel.py`, run headless
(`blender --background --python ...`) — rather than the throwaway/lost
approach used for the earlier Trimmed Stick tiers, since nothing survived
from that precedent to reuse. Exports two separate meshes/renderers
(`Rocks`, `Wood`) on purpose, matching the earlier "logs swap material
when lit, rocks stay static" decision.

- **Rocks** use the project's existing `RockChunk.mat` directly — no new
  rock material needed.
- **Wood** gets two new materials generated procedurally in Unity
  (`CampfireWoodUnlit.mat`, `CampfireWoodLit.mat`), both built from a
  256x256 charred-wood albedo texture and a matching ember-glow emission
  texture. Both use the CLAUDE.md-documented `SmoothThreshold(x, edge0,
  edge1)` hand-rolled helper for the char-blotch/ember-band thresholding
  — not `Mathf.SmoothStep`, which is the wrong function for this (see the
  CLAUDE.md gotcha; this is the first real re-application of that lesson
  since it was written).
- `Campfire.cs` reworked: the old `Renderer[] renderers` (every renderer,
  swapped uniformly) is replaced with a single `[SerializeField] Renderer
  woodRenderer` — `SetLit()` now only swaps material on the wood, the
  rocks renderer keeps `RockChunk.mat` permanently.
- Model scaled against measured bounds (not assumed) to a 0.95m footprint
  — CLAUDE.md's "scale against the player, not the raw import" rule,
  confirmed by rendering the actual result and eyeballing it next to the
  known player scale rather than trusting the number in isolation.
  Grounding likewise verified by measuring actual renderer bounds after
  scaling, per the pivot-grounding gotcha (came back already correctly
  grounded — offset 0 — but was verified, not assumed).
- Verified via direct YAML grep of the saved prefab (material guids on
  both renderers, the `Campfire` script's new `woodRenderer`/
  `unlitMaterial`/`litMaterial` fields, the resized `CapsuleCollider`)
  plus two rendered preview screenshots (lit and unlit) reusing
  `IconBaker`'s render-to-PNG technique, not just trusting a "no errors"
  log.
- One real bug hit and fixed mid-build: the first prefab-edit attempt
  destroyed the `Rocks`/`Wood` children along with their temporary
  wrapper object, because `PrefabUtility.InstantiatePrefab` creates a
  *nested* prefab-instance link — reparenting children out of it and then
  destroying the wrapper destroys them too. Fixed by using a plain
  `Object.Instantiate` (disconnected copy) instead, since the geometry is
  being permanently absorbed into `Campfire.prefab`, not kept as a live
  nested link. A second, smaller bug (mutating a `Transform`'s children
  while `foreach`-ing over them, which corrupts the enumerator) was also
  hit and fixed the same run.
- Both throwaway Editor scripts used to build this (`BuildCampfireModel.cs`,
  `RenderCampfirePreview.cs`) deleted after use, per convention.

Still open per `CAMPFIRE_PLANNING.md`: the 4 accessory items (Grill/Soup
Pot/Kettle/Frying Pan — need their own models/icons), Wood Stove, the
water-safety mechanic, and the decided-but-not-built E-key popup UI for
loading fuel/food.

## 2026-08-12

### v0.3.26-dev — Campfire rebuilt: craftable, fuel-burning, cooking, warmth

Full rework of `Campfire.cs` per `CAMPFIRE_PLANNING.md`, built in 4
approved chunks — was a single hardcoded scene prop only lightable via
the Elemental Spark wish, with zero connection to fuel, cooking, or
warmth. Now:

- **Craftable/placeable.** New `CampfirePiece` `BuildPiece` (4 Rock + 3
  Stick, Woodworking skill, `unlockTier: Crude` — the earliest unlock),
  placed via the Build tab through the exact same zero-`BuildSocket`
  free-placement path `StorageBox` already uses — no changes needed to
  `PlayerBuilding.cs` itself.
- **Two ways to light it.** A new tool-free, instant `IInteractable` "E"
  action alongside the original Spark wish — `Campfire` now implements
  both interfaces off one shared `Prompt` (they declare an identical
  signature, and `PlayerInteraction` never actually renders `IWishTarget`'s
  copy, so this is safe).
- **Real fuel.** Reuses `FuelTier`/`FuelItem` exactly as built for the
  Furnace — 1 fuel slot, a real burn timer ticking in real time while lit
  independent of anything else, auto-extinguishing at zero. Lighting
  (either method) now requires fuel and consumes 1 unit.
  `InventoryScreen` gained a "Campfire (nearby)" section (mirrors the
  nearby-`StorageBox` pattern) so fuel can actually be loaded.
- **Real cooking.** New `CookableItem` type (mirrors `EdibleItem`/
  `FuelItem`'s pattern) with an optional `requiredAccessory` gate for a
  future 4-accessory-slot system (Grill/Soup Pot/Kettle/Frying Pan —
  named but not built; each still needs its own model/icon before it's
  usable). 1 cooking slot, auto-cooks over time while lit and the player
  stands within range — progress pauses, not resets, if the fire goes out
  or the player steps away. New "Cooked Meat" item (Meal-tier `FoodTier`,
  reuses Raw Meat's model as a placeholder — no visual distinction from
  raw yet) — Raw Meat itself still has no `EdibleItem`, so cooking is the
  only way to eat it.
- **Real warmth.** `PlayerVitals.WarmNear` — Body Temperature's first
  actual gameplay effect after being 100% decorative since it was added.
  A lit Campfire nudges it toward 80 while the player's within range.
  `VitalsBarHUD` gained a 4th row for it, previously debug-overlay-only.

One real bug caught mid-build, not shipped: the first pass at the Raw
Meat → Cooked Meat `CookableItem` asset had a null `rawItem` — a stale
`ItemDefinition` reference carried across a `PrefabUtility.LoadPrefabContents`
cycle, the exact gotcha documented in `CLAUDE.md`. Caught via direct YAML
verification before it shipped, fixed with a targeted re-fetch.

Batch-mode compile check passed (0 `CS####` errors) after every chunk.
Manual Play-mode verification still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.25-dev — Pickupable Log item, real wood-item weights, Furnace FuelTier data layer

First implementation chunk from `WOOD_AND_FUEL_PLANNING.md`: Log becomes a
real inventory item, Stick/Plank's untuned weights get fixed, and the
Furnace fuel-tier data layer exists (though the Furnace itself still has
no fuel-burning logic — that's a later chunk).

**Log is now pickupable.** `ResourceNode` gained an optional secondary (F
key) "Pick up Log" action alongside its existing primary "Hold to break"
chop — a new `pickupItem`/`pickupCount` field pair, defaulting to null/off
so every other node (Boulders, ore, Tree) is unaffected. Picking up costs
no tool and grants no skill XP (unlike chopping, which still requires an
Axe and trains Woodworking), and removes the node outright with no
respawn, same as chopping does today. New `Log` `ItemDefinition` (15 lbs,
maxStack 5, no `CraftingRecipe`) reuses the existing Log ResourceNode's
own placeholder cylinder mesh/material directly for its world-pickup
prefab (`LogPickup.prefab`) — no Tripo3D generation needed since the
source visual was already a primitive, not an imported model.

**Wood-item weights fixed.** Stick and all 5 Trimmed Stick craft-tiers
had no `weight` set at all (silently defaulting to `ItemDefinition`'s
bare `1f`) — now `0.5` lbs. Plank was the same gap, now `3` lbs. Log
enters at `15` lbs, meaningfully heavier than either, matching its size
as a full section of a tree trunk rather than a hand-sized branch or a
single board.

**`FuelTier`/`FuelItem` data layer** (`Assets/Scripts/FuelTier.cs`/
`FuelItem.cs`, mirrors `EdibleItem`/`MedicineItem`'s pattern): a
deliberately separate tier axis from `CraftTier`/`FoodTier` — fuel
efficiency (burn duration), never a smelting-recipe gate. Stick + all 5
Trimmed Stick tiers register as Tier 1 (5 min/item, craft quality doesn't
affect burn efficiency), Plank as Tier 2 (10 min/item) — 7 new `FuelItem`
assets. Log is **not** wired as fuel yet (its tier/duration wasn't
decided in planning). Tiers 3-5 are placeholder numbers reserved for
Coal/Gas/Electricity, not yet designed.

Batch-mode compile check passed (0 `CS####` errors) at every stage.
Manual Play-mode verification still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.24-dev — 5-tier Hunger restoration system + MRE model lying-flat fix

Two follow-ups from live testing of the MRE Ration (v0.3.23-dev): the model
stood upright instead of lying flat when dropped/spawned in the world
(Ben's screenshot report — Tripo3D's own preview renders it "standing up"
like a display product shot, so the raw import inherited that pose), and
the MRE didn't restore Hunger at all despite being food.

**New `FoodTier` system** (`Assets/Scripts/FoodTier.cs`), mirroring
`CraftTier.cs`'s enum + static-scale pattern but for a genuinely different
axis — food substantiality, not crafting quality (CLAUDE.md already warns
against reusing one tier scale for an unrelated quantity, so this is a
dedicated one, not a repurposed `CraftTier`): Snack(15) / Light Meal(25) /
Meal(40) / Hearty Meal(60) / Feast(90) Hunger restored. `EdibleItem` gained
a `foodTier` field, applied unconditionally in `PlayerEating.TryEatFrom` —
every food item now restores Hunger on this shared scale, while the
existing `vital`/`restoreAmount` pair is reserved for a secondary effect
(Health, for MRE). Berry retuned to Snack (was a flat 20 Hunger via
`vital`, now 15 via the tier, `restoreAmount` zeroed since it has no
secondary effect); MRE Ration set to Meal (40 Hunger), on top of its
existing 25 instant + 15/60s Health.

**MRE model orientation fix:** rotated 90° about Z (same technique
`Shirt.cs` uses for its own dropped-pose fix) so the pouch's tall axis
becomes horizontal and its thin axis becomes vertical, then re-grounded
and resized the collider against the rotated bounds — verified via YAML
grep the collider now sits flush at y=0 (previously would have landed
~0.1m *below* ground on the first attempt at this fix; caught and
corrected before saving by reviewing the math, not by a second live
report).

Batch-mode compile check passed (0 `CS####` errors). Manual Play-mode
verification still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.23-dev — MRE Ration: starting food closes out basic starting gear (MVP2 item 3)

New "MRE Ration" item — a sealed foil ration pouch, modeled via Tripo3D
(clean on the first attempt), scaled/grounded against the player (0.20 x
0.15 x 0.06m, matching a real MRE's footprint) and verified via YAML grep
before completion. 0.3 lbs, no `CraftingRecipe`. Two spawn directly into
the starting Settler's Shirt's own pocket storage at game start — new
`PlayerShirt.startingRationItem`/`startingRationCount` fields, same
single-purpose `Start()` pattern as the shirt/belt/canteen/boots starting-
gear mechanisms, just targeting the shirt's own `Inventory` instead of a
body equipment slot.

Eaten via the same right-click Eat action every other `EdibleItem` uses —
no new UI needed, `InventoryScreen`'s pending-action menu already shows
Eat generically for any item with a registered `EdibleItem`. Restores 25
Health instantly plus 15 more over 60 seconds: `EdibleItem` gained an
optional `healOverTimeAmount`/`healOverTimeDuration` pair layered on top of
its existing instant `restoreAmount`, and `PlayerEating.TryEatFrom` now
calls `PlayerVitals.StartHealOverTime` when set — reuses the exact
mechanism Medicine and the Heal Self wish already use, rather than a new
one. Every existing `EdibleItem` (just Berry so far) defaults to zero/no
change in behavior.

Closes the last real gap flagged in `MVP2_PLANNING.md` item 3 against
`docs/game-overview.md`'s "a small cache of survival rations" line — basic
starting gear is now fully done (clothing, canteen, and food).

Batch-mode compile check passed (0 `CS####` errors); hit one real batch-
mode infra snag along the way, not a code bug — a stale `bee_backend` lock
from an earlier run left a hung `Unity.exe` batch process; killed and
retried cleanly. Manual Play-mode verification still needed — see
`TEST_FEATURE_PLAN.md`.

### v0.3.22-dev — Craft tier colors + Crafting screen tier-sort/filter

Visual/UX pass on `CraftTier` (plan + approved mockup in
`CRAFT_TIER_COLORS_PLANNING.md`): items now read their quality tier at a
glance instead of only via the text prefix (`CraftTierNames`). Every tier
gets a color, including Normal — Crude gray, Rudimentary white, Normal
green, Fine blue, Masterwork gold. New shared lookup `CraftTierColors` in
`CraftTier.cs`, mirroring the existing `CraftTierNames`/`CraftTierScale`
pattern.

Technical approach decided after ruling out wrapping draws in `GUI.color`
(it would re-tint icon art itself and muddy `DebugGUI.Slot`'s already-dark
background) — instead, a thin per-tier **border** texture and a per-tier
**text color** for the item name, both new lazily-cached additions to
`DebugGUI.cs` (`TierBorder`, `TierName`/`TierNameCentered`,
`SlotForTier`), applied in `InventoryScreen.DrawSlotBox` and
`CraftingScreen.DrawIcon`/`DrawTile`. Icon art itself stays untouched.

Crafting screen also gained a **tier filter row** (All + 5 colored chips,
ANDed with the existing discipline-tab/search filter) and a **sort-
direction toggle** ("Tier 1 → 5" / "Tier 5 → 1"). Tier-ascending is now
the *default* browsing order, replacing the old implicit family-grouped
array order (Ben's call — this scatters families apart, e.g. Crude Knife
next to Crude Pickaxe rather than next to Rudimentary Knife, but "finding
recipes would be easier" won out over preserving family grouping as the
default).

Batch-mode compile check passed (0 `CS####` errors) — caught and fixed one
real miss along the way: `CraftTier.cs` had no `using UnityEngine;` before
this change (it only used primitive types), so adding `Color` fields to it
needed the using added too. Manual Play-mode verification still needed —
see `TEST_FEATURE_PLAN.md`.

### v0.3.21-dev — Settler's Sneakers: Boots gets its starting-gear auto-equip

Real gap found live (Ben's report): Boots was the one starting-gear slot
that never got the auto-equip-at-spawn treatment when Sneakers was added
as a plain, Admin-Spawn-only Boots variant. Fixed with the same split
Jeans already has — a new "Settler's Sneakers" `ItemDefinition`/prefab
(reuses the existing `Sneakers.glb` model and scale exactly, not a
rename of the plain item) that auto-equips, while plain "Sneakers" stays
as-is. `PlayerBoot.cs` gained the `startingBootPrefab` field + `Start()`
mechanism — fourth caller of the pattern `PlayerShirt` established, after
Shirt/Jeans/Belt.

Batch-mode compile check passed (0 `CS####` errors). Manual Play-mode
verification still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.20-dev — Starting Canteen clipped to the Settler's Belt, ground Canteen removed

The player now spawns with a Canteen already attached to the Settler's
Belt's one attachment point, not just the belt itself — closes the loop on
this session's starting-gear work (Shirt, Jeans, Belt, now Belt's own
contents). `PlayerCanteen` gained a `startingCanteenPrefab` field and a
`Start()` that instantiates it and attaches directly into the equipped
belt's `Inventory` (reusing the existing `AnchorFor(BeltSlot)`), rather
than going through the normal `EquipTo` path — that path assumes the
canteen already has a source `Inventory` to remove it from, which doesn't
apply to a freshly instantiated starting item.

**Real cross-component ordering issue, fixed properly rather than worked
around:** this only works if `PlayerBelt.Start()` (which equips the
Settler's Belt itself) has already run by the time `PlayerCanteen.Start()`
fires — Unity doesn't guarantee any particular `Start()` order between
sibling components on the same GameObject by default. Fixed with
`[DefaultExecutionOrder(-10)]` on `PlayerBelt`, not a fragile assumption
about component-add order or a manual `ProjectSettings` edit.

Removed the pre-spawned ground Canteen from `TestScene.unity` (same
reasoning as the Boots/Grass Belt/Backpack cleanup last entry) — starting
gear now covers it.

Batch-mode compile check passed (0 `CS####` errors). Manual Play-mode
verification still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.19-dev — Settler's Belt (auto-equipped, Canteen-only slot), and starting-gear ground items removed

New "Settler's Belt" — auto-equips at spawn (`PlayerBelt.Start()`, third
caller of the starting-gear mechanism after Shirt and Jeans), no
`CraftingRecipe`. Unlike every other belt, it has exactly one attachment
point restricted to Canteen only, not generic. `Belt.cs` gained a new
`restrictedTo` field (`ItemDefinition[]`, empty/unrestricted by default —
every existing belt's behavior is unchanged) that feeds `Inventory`'s
existing `restrictedTo` mechanism, the same one Boot's named sub-slots
already use — Settler's Belt is just the first belt to actually set it.

Model generated via `Tools/Tripo3D` (`a plain brown leather belt with a
simple metal buckle, coiled...`) — clean result on the first attempt.
Scaled to match the *existing* Grass Belt's real in-scene footprint
(measured directly, not guessed) for visual consistency between belts,
rather than an independent player-relative estimate — a small variant on
the "scale against the player" rule for an item type where "consistent
with its siblings" is the more relevant reference. Grounded, collider fit,
icon baked, verified via a diagnostic dropped-pose render before
completion.

**Also (Ben's mid-session call): removed the pre-spawned Military Boots,
Grass Belt, and Backpack world pickups from `TestScene.unity`.** Now that
starting gear (Shirt, Jeans, Belt) covers a new player's basic equipment
via auto-equip, these ground-placed instances were redundant leftovers
from before that existed — confirmed via a batch script matching by source
prefab rather than hand-editing the scene YAML.

Batch-mode compile check passed (0 `CS####` errors). Manual Play-mode
verification still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.18-dev — Sneakers: new Boots item, no new code

Fourth entry in the Boots family alongside Civilian/Hiking/Military —
reuses `Boot.cs` directly (`slots: []`, slot-less like Civilian Boots,
Ben's call) rather than adding any new script. Model generated via
`Tools/Tripo3D` (`a pair of casual white and grey athletic sneakers...`) —
clean result, exactly two shoes on the first attempt (no repeat of the
Combat Boots "3 boots in one mesh" incident). Measured, scaled against the
player (0.22m tall, shorter than Combat Boots' 0.32m since these are
low-top), grounded, collider fit, and verified via a diagnostic dropped-
pose render before completion, per the same checklist Jeans just
established. Icon baked via `IconBaker.cs`. No `CraftingRecipe`, matching
the other three Boots items (none of them have one either).

Pure data/prefab work — no `.cs` files touched, batch-mode compile check
run anyway for consistency and passed clean. Manual Play-mode verification
still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.17-dev — Jeans: new Leg-slot wearable, two variants

New `Jeans.cs`/`PlayerJeans.cs` pair, structurally identical to the
Settler's Shirt work (`Shirt.cs`/`PlayerShirt.cs`) — `IEquippable` +
`IInventoryHolder`, 4 general-purpose pocket slots, worn on `Leg` instead
of `Chest`. `InventoryScreen.GetWornContainers()`'s worn-container slot
list now checks `"Leg"` too (`{"Back", "Waist", "Chest", "Leg"}`), and the
usual four dispatch switches (`EquipWithChoice`/`EquipToSlotDispatch`/
`UnequipDispatch`/`IsCurrentlyWorn`) got a `Jeans` case each — same
mechanical pattern every equippable added this session has followed.

**Two ItemDefinitions sharing one model**, same idea as the three Boots
tiers sharing `CombatBoot.glb`: `Settler's Jeans` (auto-equips at spawn —
`PlayerJeans.Start()`, same single-purpose starting-gear mechanism
`PlayerShirt` established) and plain `Jeans` (obtainable via Admin Spawn
only for now). Neither has a `CraftingRecipe` yet — deliberate, a recipe
for the plain variant is planned for later.

Model generated via `Tools/Tripo3D` (`a pair of blue denim jeans, casual
work pants with visible pockets...`) — clean result with visible cargo
pockets on the first attempt. Followed the full checklist this session's
Boots incident established: measured raw bounds, scaled against the
player (0.95m worn height, roughly half the 1.8m player, before any
grounding math), then grounded the pivot and re-fit the collider — not
just eyeballed. Also pre-empted the Shirt's rigid-worn-pose drop problem
from the start (`Jeans.SetCarried(false, ...)` lays the model flat via the
same `Euler(0, 0, 90)` rotation, applied and verified before this shipped
rather than found live afterward). **Verified via a diagnostic render of
the actual dropped pose before considering this done** (Ben's explicit
requirement this round) — reads correctly as a pair of jeans lying flat,
not oversized.

Icons baked for both items via the existing `IconBaker.cs` tool.

Batch-mode compile check passed (0 `CS####` errors). Manual Play-mode
verification (auto-equip at spawn, 4-pocket contents grid, drop/re-pickup,
plain Jeans via Admin Spawn) still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.16-dev — Boots scaled to player, and the real drag-drop coordinate bug

**Combat Boots were comically oversized** — Ben's live report, with a
screenshot showing a single boot roughly the size of the player's whole
upper body. Measured: the "fixed 2-boot" model from v0.3.15-dev came in at
raw bounds ~0.93 x 1.00 x 0.98, i.e. a boot the size of a washing machine
relative to the player's actual `CharacterController` (height `1.8`,
confirmed 1 world unit = 1 meter). Scaled down to a believable 0.32m tall
(roughly 1/6 of player height, appropriate for a tall lace-up combat boot),
re-fit colliders/grounding, re-baked icons.

**New permanent rule in `CLAUDE.md`**: every model brought in via
`Tools/Tripo3D` (or any future source) must have its size checked against
the player before being considered done — Tripo3D does not generate at
real-world relative scale (the Furnace needed scaling *up* 2x, Boots needed
scaling *down* ~3x, from the same pipeline with no scale hint in either
prompt), so there's no default that's "usually right."

**Bigger find, while chasing why the new drop-zone hover highlight
(v0.3.15-dev) landed on a caption label instead of the actual slot box in
Ben's screenshot**: every slot box lives inside `DrawContent()`'s
`GUILayout.BeginScrollView`, which reports child rects in a coordinate
space local to the scrolled content (offset by `-scrollPos`, clipped to the
viewport). But drop resolution (`HandleGlobalDragRelease`) and the hover
highlight both run later from `DrawPopups()`, entirely outside that
scroll view/`BeginArea` nesting, in true absolute screen space — comparing
an unconverted local rect against `Event.current.mousePosition` in that
outer context is comparing two different coordinate systems. **This is
almost certainly the actual root cause of the original "drag a Knife onto
the Boot's Knife Sheath does nothing" report** (2026-08-12, investigated
earlier this session and attributed — incorrectly, it turns out — to the
sheath's small hit target rather than a coordinate bug), since
`HandleGlobalDragRelease`'s hit-test has the exact same mismatch as the
highlight's mispositioning. Fixed at the source: `RegisterDropZone` now
converts every captured rect to true screen space via
`GUIUtility.GUIToScreenRect` at registration time (while still inside all
the active clip transforms), so both the highlight and the actual drop
resolution now compare against the same coordinate system.

Batch-mode compile check passed (0 `CS####` errors). Manual Play-mode
verification (boots read as correctly-sized now, and — the important one —
retry the original Knife Sheath drag with the coordinate fix in place)
still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.15-dev — Combat Boots model fix (3 boots → 2), drag drop-zone highlight

**The v0.3.14-dev Combat Boots regeneration came back with 3 boots instead
of 2** — Ben's live report. Confirmed via a diagnostic script (dumped every
`MeshFilter`/`Transform` in the imported model) that it's genuinely one
fused mesh containing 3 boot shapes, not a scene/prefab duplication bug on
our end (only 1 `MeshFilter`, 1 `Transform` — Tripo3D's usual single-fused-
mesh output, just with unwanted extra geometry baked in this time).
Regenerated with an explicit prompt (`"a matching pair of exactly two...,
one left boot and one right boot, ..., no extra objects"`) — clean result,
confirmed visually. Same overwrite-in-place + rebuild-child-fresh fix
pattern as before (`Assets/Models/CombatBoot.glb`, all 3 prefabs, icons
re-baked).

**Also investigated a reported bug: dragging a Masterwork Knife from a
Backpack into a worn Boot's Knife Sheath "wouldn't move."** Traced every
step of the drop path (`TryDrop` → `InventoryTransfer.MoveAsManyAsFit` →
`Move` → `Inventory.HasSpaceFor`/`AddEquipmentItem`) against this exact
scenario — the Knife Sheath's `allowedItems` list correctly references all
5 Knife tiers by guid, and the logic checks out as correct. Confirmed with
Ben that the drag ghost did appear and follow the cursor (so drag detection
itself works) — most likely explanation is the Knife Sheath's small
(70×44px) box sitting directly next to its Pistol Holster (which accepts
nothing, `allowedItems: []`) with zero visual feedback about which target a
drop would land on, making a few-pixel miss indistinguishable from the item
"just not moving."

**Fixed the underlying UX gap regardless of whether that was the exact
cause here:** `InventoryScreen` now outlines whichever drop zone is under
the cursor while dragging (`DrawDropZoneHighlight`, drawn every frame
`isDragging` is true, using the same frame's `dropZones` registry the drop
resolution itself reads) — a yellow border around the exact box a release
would land on, so a near-miss is now visible in real time instead of
discovered after the fact.

Batch-mode compile check passed (0 `CS####` errors). Manual Play-mode
verification (new boots model shows exactly 2 boots, boot icons updated,
retry the Knife Sheath drag with the new highlight visible) still needed —
see `TEST_FEATURE_PLAN.md`.

### v0.3.14-dev — Combat Boots model regenerated, Boots icons added

Regenerated the shared boots model via `Tools/Tripo3D` (`a pair of black
leather combat boots, military style, lace-up, thick rubber sole...`) —
clean, clearly-readable result on the first attempt. Civilian/Hiking/
Military Boots all share this one model (`Assets/Models/CombatBoot.glb`),
so overwriting it in place (same file path, same `.meta`/guid) meant all
three prefabs picked up the new model automatically with zero prefab edits
needed for the reference itself.

- **Real gotcha hit:** overwriting the `.glb` in place broke each prefab's
  nested child reference anyway, just not the way expected — the new
  generation has a different internal glTF node structure than the old
  placeholder, so the existing child `PrefabInstance`'s modification data
  (tied to specific old node fileIDs) silently stopped resolving to
  anything on reimport. `GetComponentsInChildren<Renderer>()` came back
  empty with no error. Fixed by replacing each prefab's child outright with
  a fresh `PrefabUtility.InstantiatePrefab` of the reimported model instead
  of trying to repair the stale reference — same "don't trust a clean
  batch-log exit code" lesson as the `LoadPrefabContents` gotcha in
  `CLAUDE.md`, just a new specific failure mode of it (silently-empty
  hierarchy, not a missing/null object).
- Re-measured actual bounds and re-fit each prefab's `BoxCollider` +
  grounded the model at true floor level per the imported-model-pivot
  gotcha, since the old collider/position were sized for the previous
  placeholder, not this new model.
- **Baked icons for all three Boot items** (`CivilianBootsItem`/
  `HikingBootsItem`/`MilitaryBootsItem`), which never had one before
  (`icon: {fileID: 0}` since they were added) — closes the
  "Boots-missing-icon" gap called out as explicitly out of scope back in
  the drag-and-drop rework (v0.3.11-dev) and the original inventory bug
  report. Used the existing `IconBaker.cs` tool, one call per item's own
  prefab.

Batch-mode compile check passed (0 `CS####` errors — no `.cs` changes this
round, verification only). Manual Play-mode verification (visually confirm
the new boots in-world/worn, and that all three items show their new icons
in the inventory grid) still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.13-dev — Settler's Shirt: new wearable, auto-equipped at spawn

New non-craftable wearable: a black work shirt with "GRIDLESS" across the
chest, worn on the Chest slot, holding its own 4 general-purpose inventory
slots — structurally the same `IInventoryHolder` pattern as a Backpack, just
on `Chest` instead of `Back` with a smaller capacity. The player now starts
the game already wearing one.

- **Model via `Tools/Tripo3D`** (`a plain black cotton work shirt,
  button-front, long sleeves, simple settler frontier clothing...`) — clean
  on the first generation, no unwanted graphics baked in, imported as
  `Assets/Models/SettlersShirt.glb`.
- **"GRIDLESS" is a real `TextMesh` child, not baked into the AI texture.**
  Text-in-mesh generation is a known weak spot (this repo's own prior
  generations already show instructions getting ignored, e.g. a knife that
  kept its handle despite "no handle" in every prompt attempt) — a
  real-time-rendered `TextMesh` sidesteps that risk entirely: guaranteed
  crisp, correctly-spelled text, free to reposition/resize later without
  spending more Tripo3D credits.
- **New `Shirt.cs`/`PlayerShirt.cs` pair**, same shape as
  `Backpack.cs`/`PlayerBackpack.cs`. Built source-aware from day one (no
  retrofit needed, same as `Tool`/`PlayerTool` before it).
  `InventoryScreen.GetWornContainers()` needed `"Chest"` added to the slot
  names it checks for a worn `IInventoryHolder` — the one real code change
  beyond copying the Backpack pattern; everything else about rendering a
  worn container's contents already generalized automatically.
- **Auto-equip at spawn is a new, `PlayerShirt`-only mechanism** — no
  generic "starting gear" system existed anywhere in the project before
  this (checked). `PlayerShirt.Start()` (not `Awake()`, so every other
  component's `Awake` — including `PlayerEquipment` building its slot
  dictionary — has already run) instantiates a fresh instance and equips it
  onto Chest if nothing's worn there yet, guarded so it only ever fires
  once per session.
- **Not craftable, no `CraftingRecipe` asset** — Admin Spawn already
  auto-discovers every `ItemDefinition` via an `AssetDatabase` search
  (confirmed in `AdminSpawnScreen.cs`), so it's spawnable there with zero
  extra wiring.
- **Real gotcha hit and fixed while building the prefab:** the model's
  front turned out to be local `-X`, not `+Z` as first assumed (confirmed
  by rendering the model from all four cardinal directions and inspecting
  each — `+X` was the back, `-X` the front with the collar/buttons/
  pockets). The `TextMesh`'s own rotation needed a second fix on top of
  that: `TextMesh` isn't backface-culled, so an initial rotation attempt
  that pointed the text 180° off didn't hide it, it rendered it mirrored
  backwards — confirmed via a close-up render showing "GRIDLESS" flipped
  before landing on the correct `Quaternion.Euler(0, 90, 0)`.
- Icons baked via the existing `IconBaker.cs` tool, with a per-asset
  `cameraDirection` override (`(-1, 0.8, -1)`) since the front being `-X`
  means the tool's default framing angle would've shown the back.

**Fix found during Ben's first live look:** dropped, the shirt read as
oversized and still stood upright in its worn/fitted torso shape (Tripo3D
generated it as rigid worn geometry, not flat cloth) instead of looking
like a discarded garment lying on the ground. `Shirt.SetCarried(false, ...)`
now applies a 90° rotation on drop specifically to lay the model's thin
front-to-back axis vertical instead of its tall collar-to-hem axis —
confirmed via a diagnostic render that this reads as a shirt lying flat
rather than a floating torso. Also scaled the prefab root down to 0.7x,
confirmed via the same render.

Batch-mode compile check passed (0 `CS####` errors, re-verified after this
fix). Manual Play-mode verification (confirm auto-equip at spawn, 4-slot
contents grid, drop/re-pickup, and now the lie-flat drop pose in the live
3D view) still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.12-dev — Tools are now equippable: real held-in-hand models

Follow-on to v0.3.11-dev's drag-and-drop rework. All 20 tool items (Knife/
Pickaxe/Hammer/Axe, 5 tiers each) were plain stackable items with no physical
form while held — moving one into a hand was purely a data operation
(`Inventory.AddItem`), so nothing showed in the player's hand. Ben's call:
tools should behave like the 8 existing "equippable" items (Backpack,
Canteen, etc.) — a real GameObject that's shown/hidden and parented to a hand
anchor via `IEquippable.SetCarried`/`Stash`.

- **New `Tool.cs`/`PlayerTool.cs` pair**, structured identically to the 8
  existing equippable/carrier pairs (`Boot.cs`/`PlayerBoot.cs` was the
  closest template — no named sub-slots, no belt). `Tool.CanEquipToSlot`
  only allows `"Left Hand"`/`"Right Hand"`. `PlayerTool` uses a single
  `handAnchor` field rather than Canteen's two — the scene's existing
  `HandAnchor` object was already the *same* Transform Canteen uses for both
  hands, so a second field would've been pretending there are two anchor
  points when there's only one. Built source-aware from day one (no retrofit
  needed this time, unlike the other 8 during the drag-and-drop rework).
- **No new art required.** Every tool's `worldPickupPrefab` (e.g.
  `Assets/Prefabs/RockKnifePickup.prefab` for Crude Knife) already had a
  real, distinct mesh on a `Rigidbody`+`Collider` root — structurally
  identical to what `SetCarried` needs. The only prefab change was swapping
  the root `Pickup` component for `Tool` (batch-converted via a throwaway
  `Assets/Editor/ToolMigrationSetup.cs`, verified by grepping all 20 saved
  prefabs' YAML for the correct `Tool` script + `itemDefinition` guid before
  deleting the script — confirmed clean, no leftover `Pickup`, no null refs).
- **`PlayerCrafting.AddCraftedOutput` and `PlayerDropping.SpawnPickup` needed
  zero changes** — both were already written generically against
  `IEquippable`/`TryGetComponent<Pickup>` (the equivalent Admin Spawn gap had
  already been fixed previously), so crafting a tool and admin-spawning one
  both correctly produce a real physical instance now, automatically.
  `PlayerEquipment.HasInHand` (the tool-gate check used by `ResourceNode`/
  `ChoppableTree`/`HostileCreature`/`PlayerCrafting`) also needed no change —
  it reads `Inventory.GetCount`, populated the same way regardless of
  whether a slot came from `AddItem` or `AddEquipmentItem`.
- **Real bug caught and fixed while implementing:**
  `PlayerCrafting.BreakHeldTool` (the "your tool broke" spectacular-failure
  path) called the generic `hand.RemoveItem(tool, 1)` directly — exactly the
  `RemoveItem`/`AddItem`-strips-`equipment`-reference gotcha `CLAUDE.md`
  already documents. Now equipment-aware: finds the matching slot, and if
  it's equipment-backed, routes through `RemoveEquipmentItem` and destroys
  the physical instance outright (a broken tool is destroyed, not dropped),
  instead of orphaning a still-visible-but-untracked GameObject in the
  player's hand.

Batch-mode compile check passed (0 `CS####` errors). Manual Play-mode
verification (equip a tool, craft one from scratch, mine with one, break one
via spectacular failure) still needed — see `TEST_FEATURE_PLAN.md`.

### v0.3.11-dev — Inventory: drag-and-drop replaces the button/popup UI

Grew out of five reported inventory bugs — a Canteen picked up into a
Backpack had no UI path to equip it (only the main inventory grid ever had
an Equip button), the same gap existed for Boots, Boots also have no icon,
Healing Paste/Bandage's skill gate (see v0.3.10-dev above), Healing Paste
couldn't be applied from a hand/backpack, and the Wolf Pelt couldn't be
dropped. Root-caused the first two to a shared bug: every equippable's
carrier script (`PlayerCanteen`, `PlayerBoot`, etc.) hardcoded "remove from
the main inventory" as the equip source regardless of where the item
actually was, so equipping something found in a backpack silently corrupted
state (stale entry left behind, duplicate added at the destination) instead
of working.

Rather than patch each destination gap as reported, replaced
`InventoryScreen`'s entire button/popup model with drag-and-drop:

- **Press-and-hold an item and drag it to where it should go.** Every slot
  box (main inventory, equipment slots, backpack/boot/storage contents) is
  both a drag source and a drop target. An invalid drop (wrong slot for the
  item's type, target full, released over nothing) needs no rollback — the
  underlying `Inventory` data is never touched until a drop actually
  succeeds, so it "snaps back" for free.
- **A plain click (no drag) opens a short action menu** — Drop / Eat /
  Apply / Drink / Fill / Equip / Unequip, whichever apply to that item —
  replacing the old destination-button list (`DrawMoveDestinations`) and
  the ~230 lines of duplicated per-type Equip/Unequip/Drop button branches
  that used to live in `DrawInventorySection` and `DrawEquipmentSection`.
- **Partial-stack drag**: no modifier drags the whole stack, Shift drags
  half (rounded down, min 1), Ctrl drags exactly 1. `InventoryTransfer`
  gained a `quantityCap` overload of `MoveAsManyAsFit` to support this.
- The destination being exactly where the player dropped the item also
  eliminates the "which slot did Equip guess?" ambiguity that caused the
  Canteen bug in the first place — dragging a Canteen onto the worn Belt's
  own contents grid equips it there directly, no picker needed.

**Foundation work that made this safe:**
- Every one of the 8 equippable carriers (`PlayerBackpack`, `PlayerBelt`,
  `PlayerBoot`, `PlayerCanteen`, `PlayerNavComputer`, `PlayerHealthMonitor`,
  `PlayerSunglasses`, `PlayerMiningFaceShield`) got a source-aware
  `Equip`/`EquipTo` overload that removes from the `Inventory` the caller
  explicitly names, instead of guessing via `FindSlot` and falling back to
  the main inventory. Drag always knows exactly where an item came from, so
  this is what makes "drag a Canteen out of a Backpack onto the Belt" (or
  any other type, from any container) actually work.
- Added `IEquippable.CanEquipToSlot(string slotName)`, implemented by all 8
  equippable types. Nothing previously stopped a Boot from being data-added
  to the Head slot — no code path did it only because every existing caller
  already knew its own hardcoded slot. Dragging introduces the new
  possibility of dropping any equippable onto any body-slot rect, so this
  is the explicit gate that replaces "no code path does it" with "no code
  path is allowed to."

**Explicitly out of scope, not touched:** Boots still have no icon (needs
actual sprite art, unrelated to this). `BankScreen`/`LockboxScreen` are
currency-only (no item slots) and Storage Box contents already rendered
inside `InventoryScreen` itself, so neither needed touching.

**Two fixes found during Ben's first live pass:**
- A follow-up edit (restoring "Empty" text on equipment slots) introduced a
  `CS1503` — a ternary mixing `string` and `GUIContent` doesn't resolve to
  either `GUILayout.Box` overload — that wasn't caught before Ben tested,
  since the compile check had only been re-run once, before that edit. Unity
  silently dropped into Safe Mode, which looked exactly like a runtime
  interaction bug (nothing clickable) until Ben pasted the actual Console
  error. Fixed by wrapping both branches in an explicit `new GUIContent(...)`.
- **Left-click alone was too twitchy** — an ordinary click naturally moves
  the mouse a couple of pixels between press and release, enough to cross
  the original 6px `DragThreshold` and pick the item up instead of opening
  the action menu (confirmed live: clicking a Backpack in a hand slot
  immediately "grabbed" it). Fixed two ways: **right-click now opens the
  action menu directly**, with no drag/threshold ambiguity at all (right
  MouseDown never starts a drag, so there's nothing to disambiguate), and
  `DragThreshold` was loosened 6f → 12f so a plain left click has more
  headroom too.

Batch-mode compile check passed (0 `CS####` errors, re-verified after both
fixes above). Manual Play-mode drag verification still in progress with Ben
— see `TEST_FEATURE_PLAN.md`.

### v0.3.10-dev — Healing Paste & Bandage: Medical 25 → Medical 0

Both items' skill requirement isn't a standalone field — it's derived from
their `ItemDefinition.tier` via the shared `CraftTierScale.SkillRequirement`
table (Crude→0, Rudimentary→10, Normal→25, Fine→50, Masterwork→100). Both
were `tier: 2` (Normal, hence 25); changed to `tier: 0` (Crude, hence 0).

Confirmed safe before touching anything: both `HealingPasteRecipe.asset` and
`BandageRecipe.asset` have null `lowerTierItem`/`higherTierItem` — genuinely
single-tier items (like Rope/Cloth), not one rung of an actual Crude→
Masterwork ladder, so this doesn't imply missing "Crude Healing Paste"/
"Fine Bandage" variants. Also confirmed the tier change has no side effect
on craft duration — `PlayerSkills.GetHoldDuration` derives hold time from
the *player's own current skill level* (`TierForSkillLevel`), not the
item's stored tier, so this is purely a gate-requirement change.

Follow-up to v0.3.8-dev — traskmi's live look at the placed Furnace called it
"very small."

- **Scaled 2x**: world bounds went from (0.65, 1.00, 0.44) to
  (1.30, 2.00, 0.87) — now taller than the 1.8m player and clearly more
  substantial than the 0.76m-tall Anvil, matching a furnace's real-world
  role as a bigger structure than a compact worked-metal block.
- **Found and fixed a real grounding bug while inspecting the size**:
  `CrudeFurnace.glb`'s pivot sits at its *center* height, not its base —
  confirmed by noticing the object's measured world-bounds center exactly
  equalled its transform position. This is the exact "imported model pivot
  is not reliably at its base" gotcha `CLAUDE.md` already documents for
  third-party/AI-generated models — the original v0.3.8-dev placement
  script assumed a base pivot and didn't check, so the Furnace was already
  sitting at a slightly wrong height before the resize made it more
  noticeable. Fixed by measuring real renderer bounds and computing the
  pivot's actual offset from the true base, rather than trusting `y = 0`
  (or in this case, the originally-sampled ground `Y`) to mean "resting on
  the ground."
- **BoxCollider re-fit from fresh post-scale bounds** rather than trusting
  the scale to carry through the existing local-space size correctly on
  its own — confirmed by grepping the saved YAML: local `m_Size` came back
  unchanged (0.65, 1.00, 0.44), which is correct since collider size is
  local-space and the parent's new 2x scale applies automatically at
  runtime.
- Verified directly in the saved scene YAML (`m_LocalScale`, repositioned
  `m_LocalPosition.y`, refit `BoxCollider.m_Size`), not just the batch
  script's own log output.

### v0.3.8-dev — Iron Ingot: new item, new "requires a Furnace" crafting gate

First new item added via the headless-Blender pipeline confirmed working this
session (`Assets/Models/IronIngot.glb`, 54 tris, metallic material, pivot at
base by construction). Also the first crafting-station requirement beyond
Anvil — smelting needs real heat, not just a hard hammering surface.

- **New "requires a Furnace" gate, mirrors `requiresAnvilSurface` exactly:**
  `CraftingRecipe.requiresFurnace` (bool) + `FurnaceSurface` (trivial marker
  component, same shape as `AnvilSurface`) + `PlayerCrafting.HasNearbyFurnace`
  (same 2m range, same call shape) + `CraftingScreen` UI updates (warning
  label, gating, `DrawQuantityAndCraft` signature) — every call site that
  handles `requiresAnvilSurface` now has a `requiresFurnace` sibling.
- **A Furnace is now placed in `TestScene.unity`**, ~2.5m from the existing
  Anvil, built from `Assets/Models/CrudeFurnace.glb` (generated v0.3.5-dev,
  sat unused until now) with a bounds-fitted `BoxCollider` and
  `FurnaceSurface` attached.
- **`Iron Ingot`** (`Assets/Data/IronIngot.asset`, tier matches Iron/Copper) —
  crafted only, not a world-gathered resource (`canRespawn: false` on its
  pickup, same as a tool). Recipe: 10x Iron → 1x Iron Ingot, Metalworking
  skill, requires the new Furnace gate — structurally mirrors
  `NailRecipe.asset` (the existing `requiresAnvilSurface` template).
- **Batch-mode asset creation split into two scripts/runs** (data assets,
  then scene editing), per the documented "don't trust a reference across an
  `OpenScene` boundary" gotcha — every asset used in the scene-editing pass
  was re-loaded fresh after `EditorSceneManager.OpenScene`, not carried over
  from the first script's run.
- **Verification caught two of my own mistakes, not two real bugs** — worth
  recording since it's a good example of why "grep the saved YAML" beats
  trusting a script's own success log: I first checked the wrong guid for
  the recipe (the item's, not the recipe's) and used a plain
  `m_Name: Furnace` grep that doesn't match how a `PrefabInstance`'s name
  override actually serializes (`propertyPath: m_Name` under
  `m_Modifications`, not a direct field). Both the Furnace placement and the
  recipe registration were correct on the first try — my initial verification
  queries were just wrong, not the build.
- **Admin Spawn Screen gets a search box** (traskmi's separate ask, same
  session) — the item list was getting long. Same
  `searchQuery`/`TextField`/Clear-button pattern `CraftingScreen` already
  uses, filtering by `itemName` substring. New items need zero extra wiring
  to appear there — confirmed by reading `AdminSpawnScreen.cs` first, it
  already auto-discovers every `ItemDefinition` via `AssetDatabase.
  FindAssets("t:ItemDefinition")`.

## 2026-08-11

### v0.3.7-dev — NPC ground-sinking: continuous correction instead of one-shot

Follow-up to v0.3.5-dev's `NPCVisualGroundFix.cs`, which corrected once on the
first `LateUpdate` after enable then disabled itself — Ben's live retest still
showed sinking, root cause unconfirmed at the time.

- **Ruled out two candidate causes before touching the script:** grepped
  `Assets/Animations/NPCIdle.anim`'s curves directly — it only animates arm
  DOFs (Left/Right Arm Down-Up/Front-Back, Left/Right Forearm Stretch), no
  Root/Hip/leg curves at all, so the sinking isn't authored into the clip
  itself. Also checked `NPCIdle.controller` — exactly one state (Idle,
  default, no transitions), ruling out transition-blend timing.
- **Working theory:** the one-shot correction's first `LateUpdate` likely ran
  *before* the Animator evaluated its first real pose on scene load. Since
  bind-pose feet already line up correctly (per the v0.3.5-dev entry), an
  early measurement would compute ~zero correction, apply it, then
  permanently disable itself — before the true post-animation offset
  appeared a frame or two later. **Not live-confirmed** — batch mode can't
  reliably evaluate Humanoid retargeting timing (established in
  v0.3.4-dev/v0.3.5-dev), so this can only be verified in a real Play-mode
  session.
- **The fix:** `NPCVisualGroundFix` now corrects every `LateUpdate` instead
  of once, so it can't get stuck on a stale early measurement regardless of
  which frame the Animator actually settles on. X/Z are captured once at
  first run and held fixed (only Y is corrected every frame) so a bounds
  asymmetry can't introduce sideways drift. Also makes this robust to any
  future idle animation with a vertical bob, which the old one-shot design
  could never have tracked correctly anyway.
- **Needs a live Play-mode check** to confirm the sinking is actually gone —
  flagging here rather than marking this fixed outright, same honesty as the
  v0.3.5-dev entry it follows up on. Cheap at the current NPC count (one
  renderer each, ~6 NPCs); revisit with an update-interval throttle if NPC
  count grows much further.

### v0.3.6-dev — Drink/fill a Canteen directly from a container

Closes the "Drink/fill directly from a container" bug — same shape as the
earlier Eat/Apply-from-container fixes, but for a Canteen sitting in a
backpack or storage box instead of a food/medicine item.

- **`InventoryScreen.DrawMoveDestinations`** now shows real Drink/Fill
  buttons when the selected slot holds a Canteen, alongside the existing
  Eat/Apply/Drop/hand/storage options — previously the only option was the
  generic move popup, forcing the Canteen back to a hand slot before it
  could be used.
- **New field, `pendingMoveEquipment`** — the physical equipment instance
  behind the clicked slot, not just its `ItemDefinition`/`Inventory`.
  Unlike Eat/Apply (which consume an item count via `TryEatFrom`/
  `TryApplyFrom`), Drink/Fill mutate the Canteen instance's own water level
  directly, so the fix needed the real object reference. Kept in sync at
  all 5 places `pendingMoveItem` gets set — explicitly nulled at the 3
  plain-item sites so a stale equipment reference can't leak into an
  unrelated popup later.
- Move-popup fixed height bumped again (360 → 420) — Drink/Fill can now
  show alongside the Boot slot buttons if a Canteen happens to be selected
  while Military Boots are worn.
- Verified compiling clean via batch mode; not separately playtested live
  beyond that.

### v0.3.5-dev — NPC idle pose/facing fixes, Crude Furnace model

Wraps up the NPC animation work and adds one new asset — a shorter,
token-constrained session, so scope stayed intentionally tight.

- **NPC idle pose**: built `Assets/Animations/NPCIdle.anim`/`.controller`
  (Humanoid muscle curves via `AnimationUtility.SetEditorCurve` — the
  first attempt used `AnimationClip.SetCurve` directly, which silently
  didn't bind), assigned to both `NPCFactoryWorkerMale/Female.prefab`.
  Batch-mode preview rendering couldn't confirm it (Humanoid retargeting
  doesn't reliably evaluate via `Animator.Update()` outside real Play
  mode), but **confirmed working live** — arms down, no more T-pose.
- **NPC facing/sliding fixed**: `NPCWander.modelForwardOffsetY` changed
  from `90` (tuned for the old model) to `0`. Took two guesses to land —
  `-90` produced the mirror-image symptom (sliding right instead of
  left), which pointed at `0` as the actual fix. **Confirmed working
  live** — NPCs now correctly face their direction of travel.
  - **Related, not yet confirmed fixed**: NPCs visibly sink partway into
    the ground once the idle animation is actually driving the Animator
    (not present before the animation was added) — likely the same
    Mecanim root-height quirk that made the pose hard to preview.
    Attempted fix: `NPCVisualGroundFix.cs` (new script, one-time
    self-correcting `LateUpdate` that measures real animated bounds and
    nudges the visual to compensate), wired onto both prefabs. **Ben's
    live test after this fix still showed sinking** — root cause not
    actually confirmed, only guessed at. Left as the one known open
    issue; see `TEST_FEATURE_PLAN.md`/next session for how to debug it
    cheaply (inspect `NPCVisualGroundFix`'s corrected state live in the
    Inspector rather than more screenshot round-trips).
- **`Assets/Models/CrudeFurnace.glb`** — generated via the existing Tripo
  API pipeline (`Tools/Tripo3D/Generate-Model.ps1`), clean on the first
  attempt, reads clearly as a crude clay/stone smelting furnace with a
  chimney and visible embers. 42.7MB — likely higher-poly than the
  "mid-poly" prompt wording asked for, same recurring pattern as other
  Tripo generations in this project (not yet checked exactly). Imported
  for review only, same treatment the Anvil got — no
  `ItemDefinition`/prefab/recipe exists yet, pending an actual Smelting
  system design.

### v0.3.4-dev — NPC visual replaced with the Human Character Dummy (male/female)

Closes out the "NPC model looks bleh" complaint from earlier this session —
first real character-model swap, not just a texture/animation tweak.

- **Imported the free "Human Character Dummy" asset** (Kevin Iglesias, Asset
  Store #178395, landed at `Assets/Kevin Iglesias/`) — both a male and
  female rig, each a correctly-configured Humanoid `Animator`/`Avatar` (52
  bones, confirmed valid via batch), accepted as-is (plain mannequin look,
  no clothing/face detail) per Ben's explicit call rather than waiting on
  the still-undecided Survivor Models Pack or a Tripo-generated character.
- **Two new prefabs**, `NPCFactoryWorkerMale.prefab`/
  `NPCFactoryWorkerFemale.prefab` — every NPC behavior component
  (`NPCWander`/`NPCHiring`/`NPCJob`/`NPCSkills`/`NPCEncumbrance`/
  `NPCCargo`/`NPCMining`, plus the `CapsuleCollider`) carried over
  unchanged from the original `NPCFactoryWorker.prefab`; only the visual
  child swapped. Scaled to match the existing 1.4-unit collider height
  (measured each model's actual bounds rather than assuming — `0.71`/
  `0.74` scale factors) and corrected for the model's pivot not sitting at
  its feet (same class of gotcha as the v0.2.8-dev buried-object bug,
  checked this time rather than repeated).
- **All 6 NPCs in the scene now use it** — 3 of the 5 scattered NPCs on
  Male, 2 on Female, plus the original hand-placed `NPCFactoryWorker` near
  spawn (predates the scattering pass, swapped to Male).
- **Two real bugs found live by Ben screenshotting the result in Play mode,
  not caught by batch verification alone:**
  1. The original `NPCFactoryWorker.prefab`'s root GameObject carries its
     own `MeshFilter`/`MeshRenderer` directly (material `mat13`), separate
     from its 5 named child mesh objects. The first build pass only
     destroyed child *GameObjects*, missing that root-level component, so
     the old mesh rendered underneath/through the new visual — the stray
     orange geometry and "still looks like the old model" screenshots.
     Fixed by also stripping the root's `MeshFilter`/`MeshRenderer`, with
     an explicit stray-renderer check before saving each prefab.
  2. There's a 6th NPC in the scene — the original hand-placed one — that
     isn't part of the "Scattered NPCs" group the first swap pass walked,
     so it was silently skipped entirely. Caught by switching verification
     from "walk this one parent" to `FindObjectsByType<NPCHiring>` (every
     instance in the scene, regardless of parent).
- **Known, expected gap — not a bug:** no `AnimatorController` exists yet,
  so every NPC currently stands in the model's raw bind pose (arms out,
  "T-pose") rather than a natural idle. `NPCWander.modelForwardOffsetY`
  also left at its old tuned value (`90`) — unverified whether the new
  model needs a different facing offset, can't confirm without watching
  one walk in Play mode.
- Verified in fresh batch reloads throughout, not just trusted from each
  build script's own log — final check: 6/6 NPCs have zero root
  `MeshRenderer` and exactly one renderer each (the Human Dummy's own
  `SkinnedMeshRenderer`).
- **First change carried through the new `WORKING_ON.md`-first workflow
  end to end** — tracked as one running entry across the initial build and
  both bug fixes, version bumped once here at actual commit time.

### v0.3.3-dev — Inventory UI: a worn item's multiple slots share one row

Ben's report after actually looking at the Military Boots' two slots in the
Inventory tab: stacked as separate full-width rows, they wasted a lot of
vertical space for what's really one worn item.

- **`InventoryScreen`'s "Inventory" panel now groups consecutive rows that
  share the same equip slot** (`PreviewSlotName` — e.g. Military Boots'
  Knife Sheath and Pistol Holster, both "Feet") onto a single horizontal
  line with one shared preview icon, instead of each getting its own
  preview+row. Backpack (Back) and Belt (Waist) render exactly as before —
  nothing else currently shares a slot name, so they still get one row
  each.
- Also the first real-world confirmation that the Boot slot UI built in
  v0.3.1-dev actually works end-to-end in a live Play session, not just
  batch-verified — Ben's screenshot showed Backpack/Belt/Boot contents all
  rendering correctly side by side.
- **First change under the new workflow** (see `CLAUDE.md`/`WORKING_ON.md`):
  tracked as an in-progress `WORKING_ON.md` entry while being built, no
  version bump until this actual commit — not bumped-and-changelogged
  per-step the way every earlier change today was.

### v0.3.2-dev — A set of Military Boots placed as starting gear

Ben's ask: spawn Military Boots into the game at start. Matches this
project's existing convention for starting gear — the Stick/Canteen/
Backpack/Plank/Mining Face Shield/Crude Fiber Belt cluster near spawn are
all hand-placed world pickups, not a code-driven "give player starting
items" system (checked `PlayerInventory.Awake()` — it starts genuinely
empty). Followed the same pattern rather than inventing a new one.

- **"Military Boots (Starting Gear)"** placed 1.6 units from the Player,
  clear of the existing item cluster (1.2-unit minimum clearance enforced
  against every other root object near spawn).
- **Pivot offset measured, not assumed** — the boot model was authored
  with its sole's bottom at local Z=0, so a 0 offset was expected, but
  measured it anyway (0.036, small but real, likely the bevel modifier's
  rounding pulling the mesh bounds slightly inward) rather than repeating
  the v0.2.8-dev mistake of trusting an assumption. Corrected before
  placing.
- **Verified in a fresh batch process**: `Boot`/`Rigidbody`/`Collider` all
  present, mesh bottom sits exactly flush with `GroundHeight.Sample` at
  its position (diff `0`), confirmed distance from Player.

### v0.3.1-dev — Boot slots wired into the Inventory tab, same as Backpack/Belt

Closes the UI gap flagged in v0.3.0-dev — the Knife Sheath/Pistol Holster slots
existed in code (`Boot.GetSlot`) but nothing in `InventoryScreen` showed or let
you fill them.

- **`Boot` can't fit `IInventoryHolder`** the way Backpack/Belt do — that
  interface assumes one homogenous `Inventory`, but a Boot can have multiple
  independently-restricted named slots (Knife Sheath *and* Pistol Holster) at
  once. Generalized `GetWornContainers()`'s return type instead of forcing
  Boot into the wrong shape: a flat `WornContentsRow` (preview slot name,
  caption, `Inventory`) that Backpack/Belt populate with one row each (same
  as before) and an equipped Boot populates with one row per configured slot.
- **Equip/Unequip/Drop wired into both `DrawInventorySection` and
  `DrawEquipmentSection`**, mirroring Backpack/Belt's existing shape exactly
  — Boot now shows correctly in the Feet slot list ("Equipped" + icon, not
  the raw item name) via the same `isWornContainer` check Back/Waist already
  used, extended to include Feet.
- **New: per-slot "To {label}" destination buttons** in the item move popup
  (`DrawMoveDestinations`) — without this, the slots would render but stay
  permanently empty, since nothing in the existing pattern lets a player
  click *into* an empty container slot (Backpack/Belt/Storage don't either;
  filling always happens via a destination button from wherever the item
  currently sits). Restriction enforcement stays entirely inside
  `Inventory`/`InventoryTransfer` — the button doesn't pre-check eligibility,
  so trying to move a disallowed item just silently moves nothing rather
  than needing UI-side validation logic.
- **Bumped the move popup's fixed height** (300f → 360f) to leave room for
  a Military Boot's 2 extra buttons.
- **Verified functionally in a fresh batch process** (not just compiled):
  equipped a Hiking Boot onto the Player, moved a Knife into its Knife
  Sheath via the exact same `InventoryTransfer.MoveAsManyAsFit` call the new
  UI button uses — landed correctly — then tried a Rock the same way — fully
  rejected, confirmed absent from the slot afterward.

### v0.3.0-dev — Combat Boot model + 3 boot variants with type-restricted equipment slots

First all-Blender (no Tripo) model in the project, and a new equipment mechanic:
items that themselves hold restricted-type inventory slots, not just general
cargo capacity.

- **`Assets/Models/CombatBoot.glb`** — built entirely procedurally in Blender
  (`bpy`/`bmesh`, no Tripo generation at all), per Ben's explicit "let's use
  an all-Blender approach" after an honest first-look review flagged real
  problems (a hard box-to-cylinder seam between the foot and ankle shaft,
  a barely-tapered toe, only 1 of 4 lace rows actually crossing, a stray
  white cap on the shaft top). Accepted as-is for now ("let's use it")
  rather than iterating further — 5,308 faces, 5 materials (leather with a
  procedural noise-driven bump, rubber sole, metal eyelets, waxed-cord
  laces), real-world scale (~28cm long, ~30cm tall).
- **New mechanic: type-restricted equipment slots**, extending `Inventory`
  with an optional `ItemDefinition[] restrictedTo` (null/empty = unrestricted,
  the default — every existing `Inventory` everywhere else in the game is
  unaffected). Enforced in `AddItem`/`SpaceFor`/`HasSpaceFor`/
  `AddEquipmentItem` alike, so a restricted slot rejects a disallowed item
  from every code path that can add to an `Inventory`, not just one.
- **`Boot.cs`/`PlayerBoot.cs`** (new, mirror `Belt.cs`/`PlayerBackpack.cs`'s
  shape) — worn at the existing `PlayerEquipment` "Feet" slot (previously
  unused by any real item). Unlike Belt's generic attachment points (any
  `IEquippable` counts the same regardless of kind), a Boot's slots are
  named and type-restricted, e.g. a "Knife Sheath" that only accepts a
  Knife.
- **Knife's 5 tiers don't chain via `ItemDefinition.baseItem`** (checked
  directly — `CrudeKnife`/`FineKnife` both have `baseItem: {fileID: 0}`,
  unlike Trimmed Stick's raw-material chain), so "only a Knife" couldn't
  reuse `IngredientMatching`'s substitute-matching. Used a plural
  `allowedItems` list instead (matching the existing `requiredTools`
  convention already used elsewhere), populated with all 5 Knife tiers
  explicitly.
- **Three variants, one shared component, no visual differences** (Ben's
  explicit scope — same `CombatBoot.glb` for all three):
  - **Civilian Boots** — no slots, plain equippable.
  - **Hiking Boots** — one "Knife Sheath" slot (any Knife tier).
  - **Military Boots** — "Knife Sheath" + a "Pistol Holster" slot
    **deliberately left with an empty `allowedItems` list** — no Pistol
    `ItemDefinition` exists yet, flagged as a future item rather than
    faked with a placeholder reference.
- **Verified in a fresh batch process**: all three `ItemDefinition`/prefab
  pairs correct (slot counts 0/1/2, Knife Sheath lists all 5 tiers by name,
  Pistol Holster genuinely empty), `PlayerBoot` present on the Player,
  `worldPickupPrefab` wired on each item, prefab colliders sized to the
  model's real bounds.
- Spawnable via the existing Admin item-spawn list (auto-lists any
  `ItemDefinition`) — no recipe built yet, same "import now, wire crafting
  later" treatment the Anvil got.

### v0.2.9-dev — 5 Wolves and 5 hireable NPCs scattered — scene-prep plan complete

Closes the last two scatter bullets from the scene-prep plan (Trees/ore/bushes
shipped v0.2.6-dev), and resolved the two open "still not decided" questions
first:

- **Currency/tools for hiring 5 NPCs turned out not to need any build work.**
  Checked the code before assuming: `PlayerBank` already starts with 25 Gold
  and `Exchange` can downgrade Gold→Silver→Copper at a fixed 10:1 ratio per
  tier — comfortably covers the 50 Copper needed to hire all 5 (10 each,
  `NPCHiring.hireCoinAmount`) through the existing banking loop, no starting-
  balance change needed. Pickaxe/Mining Face Shield/Backpack are all real
  `ItemDefinition`s, craftable normally or Admin-spawnable for quick testing.
- **Deposit container: confirmed already fully flexible, no code or scene
  change needed.** `PlayerNPCDeposit` lets the player target any `StorageBox`
  per NPC individually — Ben's own framing: "I can have multiple NPCs feeding
  it, or I can have individual ones... presort the mining resources near the
  Anvil." Left as-is; this is a live gameplay choice, not a build decision.
- **5 Wolves, 5 NPCs (fixed count, not a random range like Trees/ore — Ben's
  call, maximizes stress-test coverage for this pass).** Same seeded-batch
  discipline as v0.2.6-dev (`ScatterWolvesAndNPCs.cs`, deleted after use,
  seed `20260812`), occupancy list seeded from all 148 existing objects
  (28 original root objects + the 120 scattered in v0.2.6-dev, correctly
  recursing into the "Scattered ..." parent containers this time instead of
  just root objects).
  - **Wolves get an extra placement rule Trees/ore/bushes didn't need**: a
    minimum 15-unit distance from the Player's own spawn position, not just
    from other objects — a fresh spawn shouldn't get immediately jumped.
    Verified: closest placed Wolf was 56.9 units from spawn, well clear.
  - **Pivot-offset check applied up front this time** (the lesson from
    v0.2.8-dev) — read both Wolf's and NPCFactoryWorker's pivot-to-base
    offset from their existing hand-placed instances before placing anything.
    Both came back `0` (pivots already at their base), so no correction was
    needed, but this was verified, not assumed.
- **Verified in a fresh batch process**: 5/5 Wolves and 5/5 NPCs placed, zero
  missing components (`HostileCreature`/`NPCWander`), zero out-of-bounds, zero
  height-sampling error, and a full closest-15-pairs scan confirmed nothing
  newly placed landed in a tight cluster — the single sub-4m pairwise distance
  found (`Canteen`↔`Berry Bush`, 0.5m) is a pre-existing hand-placed cluster
  near spawn, unrelated to this pass.
- **Scene layout/organization** (the one item explicitly left open in the
  original scene-prep plan) is deferred to a future enhancement — the scene
  now has a solid, fully-populated starting point; see
  `BUGS_AND_ENHANCEMENTS.md`.

### v0.2.8-dev — Fixed scattered Trees/Boulders sitting buried in the terrain

Caught by Ben live-testing right after v0.2.7-dev's material fix made the
Terrain finally visible — the scattered content (v0.2.6-dev) mostly wasn't
visible at all, since it was sunk into the hillside.

- **Root cause:** `ScatterSceneContent.cs` set each scattered instance's Y to
  the raw `GroundHeight.Sample()` result with no further offset. That's
  correct for objects whose pivot sits at their visual base (confirmed true
  for `BerryBush`/`HerbBush` — diff of 0 against sampled ground height, no
  fix needed there), but **wrong for `Tree.prefab` and `Boulder.prefab`**,
  whose pivots sit well above their visual base — Tree's pivot is ~4m up the
  trunk, Boulder's is ~0.6m above its resting point. Every scattered Tree was
  effectively buried trunk-and-all (only canopy tips, if that, breaking the
  surface); every scattered Boulder (all ore tiers, since they're all
  `Boulder.prefab` underneath) was buried ~0.6m deep.
  - **Confirmed by comparing against the correctly-offset hand-placed
    originals**, not guessed: the hand-placed "Tree" and "Boulder" scene
    objects (survivors of the v0.2.5-dev re-leveling pass, which correctly
    preserved their original pivot offsets) show a fixed diff against
    `GroundHeight.Sample()` at their own position — 3.988921 for Tree, 0.6
    for Boulder — while every scattered clone showed diff 0. That fixed,
    position-independent diff is each prefab's own pivot-to-base offset.
- **Fix:** read those two diffs live off the hand-placed originals (not
  hardcoded — avoids a stale constant if either prefab's pivot ever changes)
  and added them to every child under `Scattered Trees` / `Scattered
  Boulders` respectively. **First attempt at this used `GameObject.Find`**
  to locate the hand-placed "Tree"/"Boulder" originals, which searches the
  *entire* hierarchy, not just root objects — since every scattered clone
  reuses the exact same name, it matched a buried scattered clone instead of
  the correctly-offset original, computing a bogus 0 offset (silently a
  no-op, caught before it was treated as done by re-running the verification
  check, not trusted on the fix script's own log). Fixed by walking
  `scene.GetRootGameObjects()` explicitly instead.
- **Verified in a fresh batch process**: every scattered Tree now shows
  diff `3.988921` against its own ground sample (matching the original
  exactly), every scattered Boulder shows diff `0.6`.
- **Lesson for future scattering work**: a prefab's placement Y needs
  `GroundHeight.Sample(...) + pivotOffset`, not just the raw sample — check
  each new prefab's own pivot-to-base offset (easiest way: compare a known-
  good hand-placed instance's Y against a fresh ground sample at its
  position) before batch-placing it, rather than assuming pivot-at-base.

### v0.2.7-dev — Fixed the Terrain rendering solid magenta in-game

Caught by Ben live-testing right after v0.2.6-dev's scattering pass shipped —
the ground rendered as Unity's "broken shader" magenta everywhere, while every
other object (Boulder, Tree, Anvil) rendered fine.

- **Root cause: `Terrain.materialTemplate` was `null`.** The Terrain conversion
  in v0.2.4-dev correctly built the `TerrainData` and its `TerrainLayer`
  (`GrassTerrainLayer.asset`, confirmed still correctly wired — 1 layer,
  `GrassTexture_Healed` diffuse texture, verified via batch diagnostic) but
  never explicitly assigned a material to the `Terrain` component itself. With
  no template, Unity falls back to auto-generating one — and in this URP
  project (`Assets/Data/URP-Asset.asset`), that fallback didn't resolve to a
  working shader, hence magenta (Unity's standard "shader not found/broken"
  color) rather than an outright missing-texture look.
- **Fix:** created `Assets/Data/TerrainMaterial.mat` using the
  `Universal Render Pipeline/Terrain/Lit` shader (`Shader.Find`, not a manual
  guid) and assigned it to `Terrain.materialTemplate` directly. The shader
  reads the `TerrainData`'s layers automatically — no texture reassignment
  needed on the material itself, since the layer data was already correct.
- **Verified in a fresh batch process** (not just the fix script's own log):
  reopened `TestScene.unity` and re-read `materialTemplate` (now
  `TerrainMaterial`, was `NULL`), confirmed the terrain layer and render
  pipeline were untouched by the fix.
- **Lesson for future Terrain/URP work:** always explicitly assign
  `materialTemplate` when creating a `Terrain` component via script — don't
  rely on the auto-generated default, at least not in this URP setup.

### v0.2.6-dev — Trees, ore Boulders, and both bushes scattered across the new Terrain

The actual placement pass the scene-prep work in v0.2.1-dev through v0.2.5-dev was
building toward — one batch script, one seeded run, everything verified by
re-reading the saved scene fresh in a separate process (not the run's own log).

- **29 Trees** (`Random.Range(20, 76)`, fixed seed `20260811`) scattered with 4m
  clearance from each other and every pre-existing object (28 root objects seeded
  into the occupancy list up front, so nothing lands on the Player, Campfire, Water
  Puddle, etc.).
- **71 ore Boulders**, unifying ore under the single `Boulder.prefab` per Ben's
  explicit call rather than 5 separately-modeled node types — Copper 25, Iron 14,
  Silver 5, Gold 2, Platinum 1, plain Rock 24 (rolled from the proposed scarcity
  ranges in `BUGS_AND_ENHANCEMENTS.md`). Each instance's `ResourceNode` config
  (`chunkPrefab`/`trainedSkill`/`skillGain`/`requiredTools`) is read live via
  `SerializedObject` off the existing 5 named Ore Nodes, not hand-copied.
  - **Copper/Iron kept non-disguised, Silver/Gold/Platinum kept disguised** — a
    call made during implementation, not re-confirmed with Ben first, worth a
    second look. Reasoning: Iron's current revealed material is an
    in-scene-embedded `Material` (no guid, not a portable asset), so disguising
    it would need new asset work; kept Copper non-disguised too rather than an
    asymmetric Copper-only exception. Both still read as identifiable via their
    required-tool prompt before breaking, same as today.
- **10 Berry Bush + 10 Herb Bush** (Ben's explicit count, not the smaller
  "propose your own range" default floated earlier), 2m clearance each.
- **Placement algorithm**: up to 40 random-reroll attempts per object, checking
  distance against every already-occupied point (own clearance + the candidate's),
  sampling `GroundHeight.Sample` for Y only once a valid (x,z) is found, then
  immediately adding the new point to the occupancy list so later placements
  respect it too. All requested counts placed with zero rerolls exhausted — no
  `SCATTER_WARN` in the log.
- **Verified independently, not just via the run's own log**: reopened
  `TestScene.unity` in a fresh batch process and checked child counts (29/71/10/10,
  exact match), zero out-of-bounds placements, zero missing required components,
  minimum pairwise spacing (5.3m, above every configured clearance), max terrain-
  height sampling error (~1e-6, i.e. every object actually sits on the terrain
  surface it was sampled against), and spot-checked one Silver Boulder (both
  hidden/revealed materials set, correct chunk prefab) and one Copper Boulder
  (no hidden material, correct chunk prefab and renderer material).
- Closes the tree/boulder/bush scattering bullets in `BUGS_AND_ENHANCEMENTS.md`.
  Still open from that plan: Wolves (up to 5, spawn-point-distance rule) and
  NPCs (3-5, currency/tools readiness), plus overall scene layout/organization.

### v0.2.5-dev — Every existing scene object re-leveled onto the new hilly Terrain

Closes the one real migration cost flagged (not just implied) when the Terrain
conversion shipped in v0.2.4-dev — every object placed before today still assumed
flat `y=0` ground.

- **28 root-level scene objects re-leveled** — Player, both Wolves, the NPC, every
  Ore Node, Rock Node, Boulder, Tree, Water Puddle, both Storage Boxes, Campfire,
  Anvil, Berry Bush, both Herb Bushes, and every loose world pickup (Stick,
  Canteen, Backpack, Plank, Mining Face Shield, Crude Fiber Belt, SoccerBall).
  Only `Ground` itself and `Directional Light` were excluded — nothing else in
  the scene needed skipping.
- **Additive, not flattening** — new Y = old Y + sampled ground height at that
  (x,z), not "snap directly onto the terrain surface." Every object had a
  deliberate small offset above the old flat ground (Campfire's 0.3 lift, various
  collider-center offsets), and this preserves those exactly instead of erasing
  them by pinning everything to the raw terrain height.
- **Used `GroundHeight` itself** (the same utility Wolf/NPC movement already
  uses) rather than a separate one-off sampling method — one code path for
  "what's the ground height here," not two that could quietly drift apart.
- **Verified by re-reading the saved scene in a fresh process**, not just
  trusting the script's own log — reopened `TestScene.unity` from disk after
  saving and re-read five spot-checked objects' positions (Player, the NPC,
  Copper Ore Node, Water Puddle, Campfire), confirming the file on disk actually
  matches what the script reported, not just what was true in memory during the
  same run.

### v0.2.4-dev — Ground is now a real 200×200 Terrain with gentle hills

Continues the scene-expansion prep from last night (grass texture, Tree/Boulder
prefabs, ground-height tracking) — the actual Terrain/hills conversion those were
all preparing for.

- **`Ground` converted from a flat Unity Plane to a real `Terrain` +
  `TerrainCollider`** — the old `MeshFilter`/`MeshRenderer`/`MeshCollider` were
  removed from the same GameObject (kept the name/identity, not a new object) and
  replaced in place, so nothing elsewhere needed to change what it looks up by
  name.
- **200×200** (the confirmed 4x-area target), positioned at `(-100, -5, -100)` so
  the playable area stays centered on world origin — Terrain content spans
  `[position, position+size]`, not centered on its own transform by default, so
  this needed an explicit offset rather than just dropping it at `(0,0,0)`.
- **Gentle rolling hills via Perlin noise**, deliberately low-frequency (one
  noise cycle spans ~80 world units) for broad rolling shapes instead of small
  bumpy noise, baked once with a fixed coordinate offset (not a random seed) so
  re-running the generation script reproduces the identical terrain — matches
  this project's "generate once, bake it in" discipline rather than
  regenerating every launch. Height range centered so the *average* surface
  height lands close to world Y=0, matching every existing placed object's
  flat-ground assumption at the mean (exact per-object re-leveling is still a
  separate, not-yet-done pass — see `BUGS_AND_ENHANCEMENTS.md`).
- **New `TerrainLayer`** (`Assets/Data/GrassTerrainLayer.asset`) reusing the same
  already-healed, already-seamless grass texture from last night rather than a
  new one — Terrain doesn't take a plain `Material` the way a `MeshRenderer`
  does, it needs a dedicated `TerrainLayer` asset. Tile size set to 5×5m,
  matching the flat Plane's old ~5m/tile density as a starting point — flagged
  as worth revisiting once actually seen at the real 200×200 scale, same as
  already noted when the texture first shipped.
- **`Ground`'s dedicated physics layer carried over correctly** (confirmed live,
  not assumed) — `GroundHeight`'s raycast (built last night specifically to be
  terrain-representation-agnostic) needed zero changes to work against the new
  Terrain surface instead of the old flat Plane.
- **Verified via batch, not just "no errors"**: confirmed the saved size/position/
  layer/TerrainLayer directly from the scene and asset files, then sampled actual
  world height at 6 points spanning the terrain (center, mid-radius, near the
  edges) through `GroundHeight` itself — the same code path Wolf/NPC movement
  uses — confirming real variation (~1.6m spread across the sampled points,
  genuinely "gentle") and confirming a point well outside the terrain's real
  extent correctly falls back instead of returning something wrong.

### v0.2.3-dev — Ground-height tracking for Wolf/NPC movement, ahead of the Terrain/hills work

Second concrete step of the scene-expansion prep (after Tree/Boulder prefab
extraction in v0.2.2-dev) — built now, on today's still-flat `Ground`, so it's
already correct by the time real hills exist instead of needing a retrofit across
three files under time pressure later.

- **New `GroundHeight` static utility** — one shared raycast-down helper used by
  `HostileCreature`, `NPCWander`, and `NPCMining` instead of three separate
  copies, matching this project's usual "promote to a shared piece once a third
  use case shows up" pattern. Terrain-representation-agnostic by design — it's
  just "raycast down the Ground layer, use whatever height comes back," so it'll
  work identically once `Ground` becomes a real hilly Terrain without needing to
  change again.
- **New dedicated "Ground" physics layer** (`ProjectSettings/TagManager.asset`,
  slot 3 — no custom layers existed in this project before now). `Ground`'s
  `MeshRenderer`/`MeshCollider` GameObject is now on it, and `GroundHeight`'s
  raycast is restricted to that layer specifically. **Real bug this avoids, not
  just tidiness**: a plain "raycast everything" approach would have let a Wolf or
  NPC walking near a Boulder or Tree snap onto the *top* of that object's own
  collider instead of the ground beside it. Confirmed via batch: sampling directly
  at the Boulder's (x,z) position correctly returns ground level (~0), not the
  boulder's own collider top (0.6).
- **Wired into all three movement systems** at the exact point each already
  computes a new (x,z) via flat `Vector3.MoveTowards` — Y now gets sampled and
  snapped there instead of being left untouched.
- **Verified as a genuine no-op on today's ground, not just "compiles"**: sampled
  height at multiple points on the existing flat `Ground` came back ~0 (down to
  float noise, ~1e-17), a point well outside `Ground`'s 100×100 extent correctly
  fell back to the caller's own Y instead of returning something wrong, and a
  fresh Wolf instance's Y stayed sane (no jump, no `NaN`) after one movement tick
  through the new height-snap path.

### v0.2.2-dev — Tree and Boulder extracted into real, reusable prefabs

First concrete step of the scene-expansion work (see `BUGS_AND_ENHANCEMENTS.md`'s
"Next Session: Scene, Save/Load, Digging & Water" plan) — the prerequisite gap
found while planning the tree/boulder scattering: neither existed as a real
`.prefab` asset, only as one-off `TestScene.unity` instances (gameplay components
added directly to the imported model in the scene, never saved out separately).

- **`Assets/Prefabs/Tree.prefab`** — extracted from the tree's existing scene
  instance via `PrefabUtility.SaveAsPrefabAssetAndConnect`, which both creates the
  new asset and rewires the original scene object into a real instance of it
  (rather than leaving an orphaned duplicate). **Renamed from its old label** —
  the GameObject was still called "Big Tree by 3Donimus (CC-BY, comparison only)",
  a stale name from when it was placed purely for a visual side-by-side against a
  procedural tree, well before it was made choppable and became an actively-used
  gameplay object. Renamed to plain "Tree" before extraction — that stale name
  would otherwise have become the name of every future scattered copy.
- **`Assets/Prefabs/Boulder.prefab`** — same extraction, no rename needed.
  **Found along the way**: Boulder already carries an `AnvilSurface` component
  alongside `ResourceNode` — meaning every future scattered boulder will also work
  as a crafting proximity point (`CraftingRecipe.requiresAnvilSurface`), not just
  an ore/rock source. Not something anyone had to add; it was already on the one
  existing instance and comes along for free with every new copy.
- **Verified via batch, not just "no errors on save"**: instantiated two fresh
  copies of each prefab (independent of the original scene instance), confirmed
  `ChoppableTree`/`Collider` on the trees and `ResourceNode`/`AnvilSurface` on the
  boulders all came through intact on every copy, and that the instances are
  genuinely independent objects, not accidentally sharing state.

### v0.2.1-dev — New grass texture for Ground, generated + hand-fixed

Landed early, ahead of the Terrain/hills work it was originally scoped alongside
(see `BUGS_AND_ENHANCEMENTS.md`'s scene-expansion plan) — Ben wanted to evaluate a
real replacement for `GrassTexture.png` (a blurry 1024x1024 placeholder from very
early in the project) before committing to the full Terrain migration.

- **Source generated via Gemini**, not Tripo3D — Tripo3D is a 3D-*model* texturing
  tool (re-textures meshes), not suited to a standalone flat tileable ground
  texture, so this used an external image generator instead, with a written prompt
  spec (seamless/tileable, top-down, flat lighting, natural color variation, no
  discrete marks) handed to Ben to run.
- **First attempt had two real problems, caught by testing rather than assumed
  fine**: rendered a 2×2 tile in Blender (`Mapping` node UV scale, orthographic
  camera) to check honestly before ever touching the live scene, which showed a
  visible seam. Applying it to `Ground.mat` at the real 20×20 tiling density (not
  just the 2×2 test) showed an even more obvious checkerboard/grid pattern in
  actual game lighting — confirms the isolated test undersold how bad tiling
  artifacts get at full density.
- **Second Gemini attempt, with a stronger prompt, was worse, not better** — an
  internal repeating polka-dot pattern baked into the source image itself, which
  compounded into a very obvious regular grid once tiled, plus the same small
  sparkle/watermark-like artifact showed up again in almost the same spot despite
  the prompt explicitly asking to avoid it — two attempts in a row suggests that's
  a consistent quirk of this generator, not something a prompt rewrite reliably
  fixes.
- **Fixed the original (better) image directly instead of continuing to re-roll**:
  a numpy-based offset-and-heal pass (Blender's Python, `numpy` confirmed bundled)
  — wraparound-shifts the image by half its width/height so the tiling seam moves
  from the edges to the center, blends a narrow band around that new center seam
  to soften the discontinuity, and patches out the sparkle artifact (located via a
  brightness/desaturation pixel mask, then replaced with a feathered clean patch
  sampled from elsewhere in the image, verified by comparing mean pixel values
  before/after the patch — not just assumed from a visual glance). First blend
  pass was too strong (an obvious blurry cross, worse than the seam it was meant
  to hide) — caught by re-inspecting the result rather than shipping the first
  attempt, then re-tuned narrower.
- **Result**: `Assets/Textures/GrassTexture_Healed.png`, now `Ground.mat`'s
  `_BaseMap`/`_MainTex` (same 20×20 tiling scale as before — a fair like-for-like
  swap). Confirmed live: no more checkerboard, artifact fully gone, one faint
  residual seam line visible up close (a genuine brightness mismatch between the
  original image's edges that blur alone softens but doesn't fully erase) —
  reads more like a natural terrain shading line than a tiling artifact at normal
  play distance. **Ben's call: good enough to keep for now**, revisit only if it
  actually bothers gameplay once the real Terrain/hills work changes the tiling
  density and viewing angles anyway.

### v0.2.0-dev — Milestone bump: Phase 1 (MVP) complete, Phase 2 (MVP 2) begins

No gameplay code changed in this entry — a deliberate version-number milestone,
not a build. `0.1.x` covered the entire Phase 1 MVP arc, ending with Hireable
NPCs' Chunk 6 (`0.1.198-dev`) closing out the last of Phase 1's 11 wishlist items.
Ben's call: bump to `0.2.0-dev` to mark that transition explicitly, and increment
from here for Phase 2 (MVP 2) work — see `BUGS_AND_ENHANCEMENTS.md`'s new
"Enhancements — Phase 2 (MVP 2) Backlog" section (draft, not yet finalized) for
what that covers.

### v0.1.198-dev — Hireable NPCs, Chunk 6: the work timer — v1 complete, Phase 1 fully built

Sixth and final chunk. The Hire/Fire/Pay state machine's `IsWaitingForPayment`/
`TryPay` (built empty back in Chunk 1, "ready for whatever wants to draw on it
later") finally has a real caller. With this, every one of Phase 1's 11 MVP items
has shipped — see `docs/design-brief.md`'s MVP Progress Check-In for the full tally.

- **New `NPCJob.IsReady`** — `AssignedJob != null && HasAllTools(AssignedJob)`,
  pulled out of `NPCMining`'s own duplicated version of the same check so both it
  and the new work timer agree on what "working" means instead of drifting apart.
- **New work timer on `NPCHiring`** — a 5-minute real-world stand-in for the design
  brief's original "5 real days" (this project still has zero persistence — no
  `DateTime`/save-load anywhere — so a genuine multi-day timer can't be built or
  tested without a save system that survives closing the Editor; real persistence
  stays a separate, later prerequisite). Only ticks while `NPCJob.IsReady` — an
  unassigned or unequipped NPC isn't "working," so its clock shouldn't run down
  either, matching Ben's own framing exactly. Once it elapses, `IsWaitingForPayment`
  flips on and the timer resets; `TryPay`/`Fire` both reset it too.
  `NPCHiringScreen` now shows "Working — payment due in Ns" while active, and the
  existing "Waiting for payment" + Pay button (Chunk 1) finally has something
  that actually sets the flag it was built to respond to.
- **`NPCMining` now refuses to work while unpaid** — added `IsWaitingForPayment` as
  a third condition alongside `IsReady` in its own readiness gate, rather than
  routing through the existing `SetPaused`/`NPCDialogue` mechanism, which is driven
  by multiple independent callers (Talk pausing/unpausing) that could otherwise
  fight over the same shared bool and incorrectly resume a should-still-be-stopped
  NPC. Mid-walk cargo isn't lost when payment comes due — it just holds in place,
  same as any other "not ready" state, and resumes exactly where it left off once
  paid.
- **Verified via batch**: forced the timer past its threshold directly (real
  elapsed time is unreliable in edit-mode batch, same class of limitation hit in
  every prior chunk's verification) and confirmed the full cycle end to end —
  `IsWaitingForPayment` flips true and the timer resets, `NPCMining` correctly
  finds no target while unpaid, `TryPay` clears the flag and resets the timer
  again, and `Fire` resets it too even mid-countdown.

### v0.1.197-dev — Hireable NPCs, Chunk 5: container-targeted deposit + return-to-mining

Fifth of six chunks. The NPC no longer just stops when full — it walks back to a
player-designated Storage Box, drops everything off, and goes right back to mining.

- **New point-and-confirm targeting** (`PlayerNPCDeposit`) — Ben's own design
  explicitly compared this to Building's socket selection ("point at a target,
  confirm"). "Set Deposit Container" in `NPCJobScreen` closes the menu, re-locks
  the cursor so the player can aim normally, and shows "[E] Set X as deposit point"
  once a `StorageBox` is in the crosshair. New `PlayerInteraction.
  SuppressInteraction` flag makes this safe: `StorageBox` already implements
  `IInteractable` for its own "pick up the box" action, so confirming a deposit
  target on the same E press would otherwise also pick the box up in the same
  keystroke. Escape cancels targeting without also unlocking the cursor into a
  state nothing else expects (targeting runs *with* the cursor locked, unlike
  every other screen here, so it needed its own branch in
  `FirstPersonController`'s Escape handling rather than reusing the existing
  close-all-screens chain).
- **New `NPCJob.DepositContainer`** — deliberately *not* cleared by job
  reassignment (unlike equipped tools) since a Storage Box is a physical spot in
  the world, not a consumable tool; changing jobs doesn't invalidate "where should
  mined stuff go." `Fire()` still clears it, same full-reset treatment as
  everything else.
- **`NPCMining` gained a return-to-deposit state.** Once it can't find any node it
  can both use and carry, it now checks for a deposit point: if one's set, it walks
  there (same bump-and-turn movement Chunk 4 already uses, just re-parameterized to
  target either a `ResourceNode` or a `StorageBox`), drains cargo into it, and goes
  right back to searching. **If no deposit point has been set yet, falls back to
  Chunk 4's original behavior** (just stops) rather than assuming one exists —
  assigning Mine Ore without ever setting a deposit point still works, it just
  caps out once full instead of self-managing.
- **Leftover-safe transfer** — if the box doesn't have room for everything, whatever
  doesn't fit stays in cargo instead of being lost, same "leftover" convention
  every other item transfer in this game already uses.
- **Verified via batch**: placed a live NPC next to the real "Small Storage Box,"
  loaded its cargo with 6 Copper, set it as the deposit container, and drove the
  actual `UpdateReturning` state directly — confirmed cargo went from 6 to 0 and
  the box's inventory went from 0 to 6, nothing lost or duplicated.

### v0.1.196-dev — NPCHiringScreen scroll fix

Confirmed live straight off Chunk 4's playtest: a working NPC mining several ore
types at once (Copper/Iron/Silver Ore/Gold Ore and more) overflowed the fixed-size
hire menu panel, cutting off items with no way to see the rest ("this window may
need a scroll bar" — Ben). The Stats/Carrying section (the only part that grows
without bound as an NPC mines more item types) is now its own bounded
`GUILayout.BeginScrollView`/`EndScrollView` area — the Talk/Hire/Assign Job/Fire
buttons above it stay fixed and always visible, only the stats+cargo list scrolls.
Panel grew slightly (320×420 → 340×460) to give the scroll view room to breathe.

### v0.1.195-dev — Hireable NPCs, Chunk 4: the actual autonomous mining loop

Fourth of six chunks, and the one that finally makes Chunks 1-3's scaffolding do
something — the NPC now genuinely walks out, mines real ore nodes, carries the
yield, and trains its own skill, with no player input once assigned.

- **New `NPCMining`** — once assigned Mine Ore and fully equipped, finds the
  nearest available `ResourceNode` within 50m it can both use (a matching tool)
  and carry the output of, walks to it, mines it, repeats. Stops (doesn't wander,
  doesn't work) once full — Chunk 5 adds walking back to a deposit container and
  resuming. Targets **real** world `ResourceNode` objects (every Ore Node, Rock
  Node, and the Boulder — every `ResourceNode` in this scene is mining-flavored,
  trees/bushes are separate component types), not a fake parallel system.
- **`ResourceNode` gained an NPC-compatible break path** (`TryMineForNPC`/
  `PeekYield`) alongside the existing player-only `Complete()`, which is hard-wired
  to `PlayerEquipment`/`PlayerSkills` and therefore unusable by an NPC. The new
  path skips the tool check (`NPCMining` checks the NPC's own equipped tools
  against `RequiredTools` itself first) and returns the mined item/count directly
  instead of spawning physical world pickups, since an NPC has no "walk over and
  grab it" step to collect them with.
- **Real discovery mid-build, not assumed**: ore nodes turned out to be
  **multi-stage** — Copper Ore Node's `chunkPrefab` is itself *another*
  `ResourceNode` (`CopperOreChunk`, no tool required), which only then yields the
  real `Pickup` (`CopperChunk`, the actual Copper item) — mirroring the player's
  own break-the-node-then-break-the-chunk-then-pick-it-up flow. Found live via a
  batch verification pass that came back with a `Pickup` component missing where
  one was assumed to always exist. **Fixed** by having `PeekYield` walk the whole
  `chunkPrefab` chain recursively down to the real item, multiplying counts along
  the way (3 chunks × 2 sub-chunks × 1 each = 6 total, confirmed exactly via
  batch), same guarded-depth-walk shape `IngredientMatching.Satisfies`'s `baseItem`
  chain already uses. An NPC has no equivalent to the player's multi-step physical
  process, so this collapses the whole chain into one resolved item+count.
- **New `NPCCargo`** — a real `Inventory` (same class `PlayerInventory`/
  `PlayerEquipment` slots/`Lockbox` already use) holding what's been mined but not
  yet deposited, rather than a bare number. **`NPCEncumbrance` revised**: now
  computes `CarriedWeight` from `NPCCargo`'s actual contents (same pattern
  `PlayerEncumbrance.ComputeCarriedWeight` uses for the player) instead of Chunk
  3's manually-incremented `AddCarriedWeight`/`RemoveCarriedWeight`, which never
  had a real caller anyway — keeps weight and actual carried items from ever being
  able to drift apart.
- **Bump-and-turn obstacle avoidance** (Ben's call, 2026-08-10: "build a collision
  idea to allow the npc to hit something and change direction") — a short forward
  raycast before each move step; if something's in the way, the NPC slides along
  the tangent of the obstacle's surface normal instead of pushing straight through
  or getting stuck. Not real pathfinding, just enough to route around a single
  obstacle edge, matching the project's existing no-NavMesh constraint
  (`HostileCreature`/`NPCWander` already live with the same limitation).
  `NPCWander.FaceToward` made public so `NPCMining` reuses the exact same
  model-forward-offset correction rather than duplicating it.
- **Trains the job's own family skill (Mining), not the node's `trainedSkill`**
  (still `Gathering` on every ore node in the scene — `Mining` didn't exist until
  this session's Chunk 2). The same physical action training a different skill
  depending on who's doing it is a real quirk, flagged in
  `BUGS_AND_ENHANCEMENTS.md` rather than silently retconning every existing ore
  node's `trainedSkill` (which would also change what the *player* trains by
  mining them) mid-chunk without Ben's sign-off.
- **`NPCDialogue` now also pauses `NPCMining`** (optional `GetComponent`, not a
  hard requirement — not every NPC has a job loop) — Talk should freeze the NPC
  completely regardless of which component happens to be moving it at that moment.
- **Cargo is now visible too** — `NPCHiringScreen`'s Stats section gained a
  "Carrying" list (item name × count) alongside the stats/Encumbrance line already
  there.
- **Verified end-to-end via batch** (not just structural checks this time, given
  the size of this chunk): placed a live NPC instance 3m from the real Copper Ore
  Node, gave it the job/tools programmatically the same way the real UI would,
  confirmed `FindTarget` correctly resolves a valid nearby node, confirmed
  `PeekYield` resolves the full multi-stage chain to 6 Copper, and confirmed an
  actual `TryMineForNPC` call added 6 Copper to cargo, grew Mining from 0 to 0.5,
  set `CarriedWeight` to 6, and put the real node on cooldown — all numbers
  matched hand-calculation exactly. Two more batch-mode-only quirks hit and fixed
  along the way (not game bugs): edit-mode scene loading doesn't run `Awake()` on
  scene-resident objects any more than it does on freshly-instantiated ones, so
  `ResourceNode`'s cached `renderers` was null until the verification script
  invoked `Awake()` explicitly via reflection, same as Chunk 3's fix.

### v0.1.194-dev — Hireable NPCs, Chunk 3: core stats, encumbrance cap, skill-gated tiers

Third of six chunks. Gives the NPC real stats for the first time — up to now
`NPCJobDefinition.tier` was inert and nothing about the NPC was visible at all.

- **New `NPCSkills`** — a deliberately separate, smaller component rather than
  reusing `PlayerSkills` directly: `PlayerSkills.OnGUI` draws a "skill increased"
  banner on whichever screen it's attached to, which would mean an NPC's skill
  ticking up fires a Player-styled banner on the *player's* screen (`OnGUI` runs
  per-instance, not per-player) — confusing, and not asked for. Same
  diminishing-returns growth curve as `PlayerSkills.GainExperience`, just silent.
  Seeded with Strength/Dexterity/Constitution/Intelligence at displayed **3.0**
  (raw level 30 — above a fresh player's own starting 2.00, since a hired worker
  isn't a total novice) and Mining at **0** (true zero, same as every crafting/
  gathering skill starts for the player).
- **New `NPCEncumbrance`** — same capacity curve as `PlayerEncumbrance`
  (`17.3925 × Strength^1.5`, confirmed live via a batch check: Strength 3.0 →
  **~90.4 lb** capacity). `CanPickUp(weight)` gates at 80% of capacity, reusing
  `PlayerEncumbrance.BetterGainThreshold` directly rather than a new NPC-only
  constant — confirmed live (10 lbs allowed, 1000 lbs rejected against the ~90.4
  lb capacity). Deliberately stricter than the player's own pickup gate (blocked
  at/over 100%) — an NPC always keeps a buffer instead of maxing out, Ben's
  explicit call. `CarriedWeight` stays 0 for now — no `AddCarriedWeight` caller
  exists yet (Chunk 4's mining loop is what will), and there's no Strength-grows-
  from-load tick either, unlike the player — building that against a value that
  can never move yet would be premature; it lands with Chunk 4 instead.
- **Job tiers actually gate now.** `NPCJobScreen` hides any job the NPC's family
  skill hasn't reached, reusing `CraftTierScale.SkillRequirement` directly (job
  tier 1 → `CraftTier.Crude` → level 0, tier 2 → Rudimentary → level 10, ...)
  rather than inventing a second threshold curve — confirmed live via batch
  (`Mine Ore`, tier 1, requires level 0, so it's always available at Mining 0).
  Shows "No jobs unlocked at this NPC's current skill yet." distinctly from "No
  jobs in this family yet." so an empty family and a not-yet-earned family read
  differently.
- **NPC stats are now visible for the first time** — `NPCHiringScreen`'s hired
  view gained a Stats section: every entry in `NPCSkills.Levels` (Attribute-
  category skills shown on the .25-10 displayed scale, same as the player's own
  Player tab; everything else — Mining — shown as a raw 0-100 level, same as
  SkillsScreen), plus an Encumbrance line paired with Strength the same way
  `PlayerMenuScreen`'s own Strength tile already does it.
- **Real technique note, not a bug this time**: an early verification attempt
  (instantiating the prefab in batch mode and reading live stat values) came back
  wrong — Strength read as the 0.25 floor instead of 3.0. Not a code bug:
  `Awake()`/`Update()` don't run on edit-mode-instantiated objects without
  `[ExecuteAlways]`, so the dictionary was legitimately never populated in that
  check. Fixed by invoking `Awake()`/`Update()` explicitly via reflection in the
  verification script itself — the actual game code was correct the whole time
  and behaves normally in real Play Mode.

Second of the six chunks. Adds the actual job-assignment UI on top of Chunk 1's
Hire/Fire/Pay shell — a hired NPC can now be pointed at a real job and handed the
tools it needs, though nothing runs autonomously yet (Chunk 4).

- **New `NPCJobDefinition`** (data, mirrors `CraftingRecipe`'s role): a job name, a
  `family` that's a real discipline `SkillDefinition` (not an NPC-only concept —
  the NPC's job skill is meant to be a genuinely trainable skill later), a `tier`
  (not enforced against anything yet — every job shows regardless of NPC skill
  until Chunk 3 adds a real level to gate on), and `ToolRequirement[]` — each one a
  named category (`"Pickaxe"`) with an OR-set of acceptable items, same convention
  `ResourceNode.requiredTools` already uses. A job needing several categories at
  once (Pickaxe *and* Shield *and* Backpack) lists each as its own requirement,
  since that's an AND across categories, not one shared OR list.
- **New `Mining` skill** (`SkillCategory.Gathering`) — didn't exist before despite
  being name-dropped in `SkillDefinition`'s own doc-comment ("Gathering, Mining").
  **New `MineOreJob.asset`**: family Mining, tier 1, needs Crude Pickaxe + Mining
  Face Shield + Backpack (the baseline tier of each — the tool-requirement arrays
  support multiple tiers per category, only one is wired in for now, trivially
  expandable later without a code change).
- **New `NPCJob`** (NPC-side): tracks the assigned job and which tools have been
  handed over, in a runtime-only dictionary (same convention `NPCHiring`'s
  `isHired`/`isWaitingForPayment` already use — no `[SerializeField]` needed for
  state that only ever changes through code). `TryGiveTool` pulls one matching item
  from the player's **main inventory only** (not hands/backpack — simplest first
  pass) and hands it to the NPC. **Re-assigning to a genuinely different job wipes
  every tool already given — Ben's explicit "lost forever" rule** — but
  re-confirming the *same* already-assigned job is a no-op on the equipped set, not
  a wipe (matters once a player re-opens the screen to give a tool they missed the
  first time). `NPCHiring.Fire()` now also clears the job.
- **New `NPCJobScreen`** — family tabs → job tiles, same two-step shape as
  `CraftingScreen`'s discipline tabs → recipe tiles, since Ben's own design was
  explicitly modeled on the Crafting menu ("first you pick the family... it offers
  up the tiers"). Opened from a new "Assign Job" button in `NPCHiringScreen` (hired
  state only), which closes itself first — same one-modal-at-a-time pattern Talk
  already used. Selecting an unassigned job shows an "Assign" button (with a
  lost-tools warning if reassigning away from an existing job); the assigned job
  shows its tool requirements with a "Give" button per missing one, greyed out if
  the player's inventory doesn't have it.
- **No visual equip** — `NPCFactoryWorker`'s model has no rig/attachment points, so
  handed-over tools are tracked as data only, not rendered on the NPC. Matches this
  session's general pattern of shipping the mechanical layer before the visual one
  (`HostileCreature`'s death is "just a rotation" for the same reason).
- **Known real bug hit and fixed during this build**: the batch script wiring
  `NPCJobScreen.families`/`.jobs` via `SerializedObject` on the Player scene object
  silently produced `{fileID: 0}` (empty) array entries despite the script running
  and logging success — re-confirmed live by re-reading the saved scene YAML
  directly rather than trusting the log, per this project's own established
  discipline. **Fixed** by patching `TestScene.unity`'s YAML directly with the
  correct `{fileID, guid, type: 2}` references instead, then re-verified by
  re-opening the scene in a fresh batch pass and reading the values back through
  `SerializedObject` (not just the raw text) — confirmed both arrays actually
  resolve to the `Mining`/`MineOreJob` assets by name.

First of the six chunks scoped in `BUGS_AND_ENHANCEMENTS.md`'s Hireable NPCs
design session. No job logic yet — this is purely "can the player hire this NPC,
does it cost real money, can they fire it" — but it's the foundation every later
chunk (job assignment, the mining loop, the work timer) hangs off of.

- **E on the NPC no longer goes straight to dialogue — it opens a menu.** New
  `NPCHiring`, now the *only* `IInteractable` on the NPC (two implementers on one
  GameObject would leave `PlayerInteraction`'s `GetComponentInParent<IInteractable>()`
  picking one arbitrarily, so `NPCDialogue` gave up the interface — see below). New
  `NPCHiringScreen` on the player, same `Open(target)`/`Close()`/`IsOpen` shape and
  cursor-unlock behavior `LockboxScreen` already established, wired into
  `FirstPersonController`'s Escape-closes-everything chain the same way.
- **Hire spends real currency** — `PlayerCurrency.Spend`, which has existed since
  the Commerce system shipped but had nothing drawing on it yet ("ready for
  whatever wants to draw on it later," per its own comment). Costs 10 Copper by
  default; the Hire button greys out via `GUI.enabled` if the wallet can't cover it,
  same pattern `LockboxScreen`'s Deposit/Withdraw buttons already use.
- **Fire resets hire state immediately**, no confirmation step. Tools will be lost
  for good on Fire once Chunk 2 (equip hand-off) exists — Ben's explicit call,
  documented now even though there's nothing to lose yet.
- **`IsWaitingForPayment`/`TryPay` exist already, unused** — nothing sets that flag
  yet (Chunk 6's 5-minute work timer is what will), but building the full
  Hire/Fire/Pay state machine in one pass now means Chunk 6 only has to flip a bool,
  not come back and redesign this file.
- **`NPCDialogue` restructured, not removed** — dropped `IInteractable` entirely,
  gained a public `BeginDialogue()` + `DisplayName` that `NPCHiring`'s menu calls
  into. Talk still works exactly as before (pauses `NPCWander` for the line's
  duration) — it just lives behind the new menu's "Talk" button instead of firing
  directly off E.
- Confirmed live: hiring correctly deducted 10 Copper from the wallet.

Straight off Combat/First Aid, Ben moved to the last unstarted Phase 1 pillar —
placing the model named a session earlier ("SD Macross Factory Worker by Tipatat
Chennavasin") "to start the process of NPC interaction." Deliberately the smallest
possible first step: **Place it + idle wander**, chosen explicitly over a
stand-in-only prop or a talk-first version, since nothing about NPC AI, dialogue, or
hiring is designed beyond the Phase 1 wishlist's name for the item.

- **Import pipeline.** Raw `.glb` was a static (no armature/animations) 6-mesh
  chibi/SD figure, ~0.71m tall, facing an arbitrary Blender axis. Rejoined into one
  mesh in Blender, uniformly scaled to a 1.4m target height (kept deliberately
  shorter than a realistic ~1.7m human — it's an SD/chibi figure, scaling it to full
  human height would fight the model's own proportions), and re-origined to
  feet-at-ground before export — so, unlike Wolf/RawMeat earlier this session, no
  additional Unity-side scale was needed. Learned from those same two bugs:
  `CapsuleCollider` numbers were hand-computed directly in local space (radius 0.3,
  height 1.4, center (0, 0.7, 0)) instead of measured off `Renderer.bounds`, so the
  world/local double-scale bug had no chance to recur here.
- **New `NPCWander`** — same shape as `HostileCreature`'s movement (no NavMesh in
  the project, flat-ground `Vector3.MoveTowards`): picks a random point within 6m of
  its spawn, walks over at 1.2 m/s, pauses 2-5s, repeats. `SetPaused(bool)` freezes
  the whole state machine (walk progress and pause countdown both hold in place)
  rather than resetting anything, so wandering resumes exactly where it left off.
- **Real bug found and fixed live: model faced 90° off from its travel direction**
  ("our npc is moving the right" — confirmed via follow-up question as crab-walking
  sideways, not a facing-direction confirmation). The model's authored forward axis
  didn't match Unity's `Quaternion.LookRotation` convention (local +Z). Root-caused
  by re-deriving the Blender→glTF→Unity axis chain from the model's own preview
  renders (front/side orthographic camera shots taken during the Blender build,
  confirming the character's nose pointed along Blender +X) — independently
  cross-checked by working out which way "crab-walking to its right" implies the
  mismatch runs. Both derivations agreed on the fix: a `modelForwardOffsetY = 90`
  correction applied on top of `LookRotation` in `NPCWander.FaceToward`, exposed as
  a serialized field (not hardcoded into the math) so a future model on this same
  component just needs a different number, not a rewrite, if it needs -90 or 180
  instead.
- **New `NPCDialogue`** — added after Ben's live follow-up ("when I put my cursor on
  the model, it doesn't give me a choice to have a dialog" — this pass had
  deliberately shipped with zero interaction first). `IInteractable`, tap E
  (instant, not hold — same reasoning `PlayerCombat`/`PlayerPieceUpgrade` already
  used for tap-not-hold actions): shows "Talk to Factory Worker" in range, and on
  press shows one static placeholder line for 4 seconds via its own `OnGUI` box,
  calling `NPCWander.SetPaused(true)` for the duration per Ben's explicit call
  ("engaging the dialog should stop movement until the dialog is complete") and
  unpausing automatically when it ends. Re-pressing E mid-dialogue dismisses early.
  No branching, no memory, no real conversation system — one line, matching exactly
  how far NPCs are designed today.
- **Credits:** SD Macross Factory Worker by Tipatat Chennavasin, CC-BY via Poly
  Pizza — added to both `GameMenuScreen`'s Credits tab and
  `Assets/Models/THIRD_PARTY_CREDITS.md` in the same pass (not left for a follow-up
  catch, unlike the Strawberries gap found earlier this session).
- **Net Phase 1 read:** still 10 of 11 by the wishlist's own bar (this is a
  placeholder, not *hireable* or *autonomous* anything), but the last item's "zero
  code, not even partially" gap is closed — there's a real, walking, talkable NPC to
  build the rest on top of rather than a blank slate. See `docs/design-brief.md`'s
  MVP Progress Check-In for the corresponding update.

### v0.1.190-dev — Basic Combat + Basic First Aid, closing the second of three remaining Phase 1 items

Straight off Encumbrance, Ben moved to the two remaining unstarted Phase 1 pillars —
"let's ideate on the combat and first aid." Both shipped end-to-end this pass,
deliberately scoped to the design-brief's own "floor of the combat/healing module"
language, not the deeper Phase 2 versions (ranged weapons, hunting/taming, surgery).

- **A real fight, from scratch, with nothing to fight before today.** Neither the
  Animal & Hunting module nor Hireable NPCs exist yet, so — Ben's explicit call —
  this needed a minimal placeholder hostile rather than either system. **Wolf by
  Quaternius** (public domain, Poly Pizza), imported at a corrected ~1.05m/1.9m
  scale (the raw import came in the size of a car). New `HostileCreature` (generic,
  not Wolf-specific — reusable for a future second creature): idle until the player
  closes within 10m, chases at 5 m/s, gives up past 20m, bites for 8 dmg on a 1.5s
  cooldown once in range. Dies at 60 HP, flops onto its side (no animation system
  exists yet — a static rotation is the whole "death" visual), and becomes
  skinnable with a Knife (same tool-gated hold-to-break shape `ResourceNode`
  already uses) for a 50% chance of 1 Wolf Pelt plus a guaranteed 1–2 Raw Meat.
  New `IDamageable` interface — a clean, reusable contact point for any future
  combat target, not hardcoded to Wolf.
- **The player's half: a bare-handed punch, deliberately not `IInteractable`'s
  hold-and-release model** (an attack has to resolve on a tap, not a multi-second
  hold — same reasoning `PlayerPieceUpgrade` already used for a different action).
  New `PlayerCombat`: Left Mouse Button, short-range raycast, ~9 dmg on a 0.7s
  cooldown, trains a new **Bare-handed** skill — the first of the five
  weapon-usage skills named back in the original Crafting/Gathering/Skills Pipeline
  planning (2026-08-05) to actually exist as a real `SkillDefinition`, finally
  giving `SkillCategory.Combat` its first real entry. Silently no-ops while a
  Build piece is armed (Building's own Left Click takes priority on a click).
- **Two real bugs caught mid-build, not shipped blind:**
  - **Ground tunneling** on the new Wolf Pelt/Raw Meat drop prefabs — a
    documented gotcha from an earlier session (a plain Discrete `Rigidbody` can
    tunnel straight through the paper-thin Ground collider) that this session's
    own setup script forgot to apply proactively. Confirmed live by Ben (a Raw
    Meat drop landed under the world) and fixed with `ContinuousDynamic`.
  - **Colliders ~20x too small on any scaled-down prefab** — the same batch
    setup script measured a model's bounds *after* scaling it down (world space)
    but assigned those numbers straight to `Collider.size`/`center`/`radius`
    (local space), so Unity's own scale multiplier applied a second time.
    Confirmed live ("finally found the pick up point. that's VERY VERY small") on
    both the Raw Meat pickup and the Wolf's own hitbox — the latter meaning
    punches were likely whiffing far more than they should have. Fixed by
    dividing the wrongly-world-scale numbers back down by the transform's own
    scale on both affected prefabs.
- **Passive Health regen slowed 20x** (1/s → 0.05/s, ~33 minutes for a full
  heal with zero effort) — Ben's call, live-testing combat: the old rate healed
  a full 0–100 in under 2 minutes, making a real Wolf bite feel consequence-free.
  Deliberately punishing until First Aid (below) gives the player a real
  counter-lever.
- **First Aid — a real crafted-consumable healing loop, reusing existing systems
  wherever one already fit** rather than inventing new plumbing:
  - **Herb Bush**, a full *duplicate* of Berry Bush (Ben's explicit call: "we
    don't want to use the existing berrybush... we need to duplicate and
    rename it") rather than reusing/repurposing it, simplified down to just the
    search mechanic (no chop action — herbs have no branches to trim). **First
    shipped on E, live-corrected to F** after Ben hit exactly the confusion this
    invited: Herb Bush reuses Berry Bush's own visual model outright, so it
    looks identical, and Berry Bush's own search is on F (E there is spoken for
    by its chop action) — matching that key removed the "looks the same, acts
    different" trap a first pass on E caused live.
  - **Healing Paste — Herb + Canteen water, not a normal ingredient.** There's no
    stackable "Water" item in this game at all; water is a tracked fill-level on
    the equipped Canteen instance. Rather than invent a Water item/economy, new
    `CraftingRecipe.requiresCanteenWater`/`canteenWaterAmount` fields (mirroring
    the existing `requiresAnvilSurface` shape) gate and consume real Canteen
    water on craft — a new `Canteen.ConsumeWater` mirrors `Drink`'s exact
    mechanics minus the Thirst restore. 3 Herb + 20 water → 1 Healing Paste,
    training a new **Medicine** discipline (its own tab in the Crafting screen
    now, alongside Woodworking/Stonework/etc.). Heals 10 HP over 10s.
  - **Bandage — 1 Cloth + 1 Healing Paste, heals 15 HP over 10s** — a second,
    stronger tier riding the exact same Medicine recipe/consumable plumbing, no
    new mechanism needed.
  - **Apply Medicine — a real mirror of eating, not a new UI pattern.** Ben's
    explicit ask: "an apply medicine function that mimics eating — it consumes
    one of the resource." New `MedicineItem` (mirrors `EdibleItem` exactly) +
    `PlayerMedicine` (mirrors `PlayerEating` exactly, `TryApply`/`TryApplyFrom`)
    + an "Apply" button wired into both places Eat already shows (the main
    inventory list and the generic hand/backpack/container move popup) —
    routes through `PlayerVitals.StartHealOverTime`, the same method
    Restoration's Heal Self wish already uses.
- **A second confirmed case of `IconBaker`'s tight-fit framing bug** — the
  Bandage icon baked as two thin crossing lines, not the roll+tail shape;
  isolated (same method as the earlier Gable Panel case) via a manual bake with
  a plain fixed-orthographic camera on the identical geometry, which came out
  clean. Same root cause, still not fixed in `IconBaker` itself — shipped with
  the same manual-bake workaround as Gable Panel.
- **Net Phase 1 read: 10 of 11 MVP items now built.** Only Hireable autonomous
  NPCs remains — Ben's already lined up the first step (an SD Macross Factory
  Worker model, to place as a first NPC-interaction placeholder), not yet built
  as of this entry.

### v0.1.189-dev — Encumbrance ships end-to-end, plus the Player tab's first real content

Ben's ask, continuing straight off the prior session's Building work: "let's ideate
on the encumbrance." What started as one Phase 1 wishlist line (`design-brief.md`:
"carried weight affects movement speed and stamina; carry capacity and movement
efficiency improve as related skills increase with use") became a full day's build:
four new core stats, a real Player tab, a Guild system, and Encumbrance itself
working end-to-end from carried weight through to movement, Strength growth, and
health cost.

- **Core stats, reconciled against an existing design-brief decision.**
  `design-brief.md` (2026-08-08) explicitly rejected a point-buy Strength/
  Constitution/Dexterity/Intelligence attribute screen — flagged directly, not
  silently overridden. Resolution: all four are ordinary `SkillDefinition`s (new
  `SkillCategory.Attribute`), grown via the exact same `PlayerSkills.GainExperience`
  diminishing-returns curve every crafting skill uses, just displayed on the new
  Player tab instead of the Skills tab. Displayed on a **.25–10 scale**
  (`PlayerSkills.GetAttributeValue`, `max(0.25, level/10)`), not the raw 0–100 every
  other skill shows — Ben's call, floor at .25 so untrained never reads as literal
  zero. All four start at displayed 2.00 (raw level 20), not the .25 floor —
  `PlayerSkills.StartingLevel[]`, a small new seeding array, generic enough that
  future non-craft skills can reuse it too. Only Strength has any mechanical hook
  today (see Encumbrance below); Dexterity/Constitution/Intelligence are display-only
  — their planned hooks (movement efficiency, max Health/Stamina, Will growth + a
  global skill-XP multiplier) are logged in `BUGS_AND_ENHANCEMENTS.md`, explicitly
  written to follow Strength's exact pattern when built.
- **Player tab, previously a deliberate blank stub, gets real content.** A tile grid
  (`DebugGUI.Slot` boxes): the 4 stats + Fame/Faction (placeholders — no backing
  system, reputation-flavored rather than skill-via-use) fill a 3-tile-per-row grid;
  a new Guild system's tiles (see below) follow as full-width one-per-row entries.
  Tile width is computed from `Screen.width`, not a fixed pixel size, so the grid
  fills the screen edge-to-edge on any resolution (Ben's follow-up call the same
  day, after the fixed-220px version left visible empty space). Each stat tile
  carries a labeled "Growth" progress bar — same anatomy as `VitalsBarHUD`'s vital
  bars (background + fill + centered label) per Ben's call to make it "look like
  the health bar" — filling 0→1 toward the *next .25 point*, not the 0–100 cap (a
  bar that's nearly always empty for the first many hours of play wouldn't read as
  useful feedback).
- **Guilds — a small new system riding the same tab.** `GuildDefinition`
  (ScriptableObject, just a name) + `PlayerGuilds` (membership list, capped at
  `MaxGuilds = 3`). Three test guilds — Masonry, Carpentry, **Smithing** (picked to
  match the trade-noun register of the other two rather than reusing the existing
  "Metalworking" skill name). Join/Leave via a new "Admin — Guilds" section on the
  existing Admin tab (no in-world way to join yet) — confirmed live: joining adds a
  full-width tile to the Player tab immediately, leaving removes it, Join disables
  once at the 3-guild cap.
- **Encumbrance — the actual Phase 1 item, built in layers with Ben reviewing each
  one before the next:**
  - `ItemDefinition.weight` (new field, lbs, defaults to 1). `PlayerEncumbrance`
    (new component) sums it across the main inventory, every `PlayerEquipment` slot
    (including a worn Backpack's own item weight), and that Backpack's contents —
    deliberately **not** nearby Storage Boxes, unlike `PlayerBuilding`/
    `PlayerPieceUpgrade`'s `ReachableInventories` — a storage box in the world isn't
    weight you're carrying, and dropping things into one is the intended way to
    unburden yourself.
  - **Capacity formula**, decided by building a comparison artifact (linear vs. a
    small exponential curve, both anchored so Strength 10.00 caps at 550 lbs) and
    having Ben pick from the actual resulting numbers rather than guessing blind:
    `Capacity = 17.3925 × Strength^1.5`. Deliberately front-loaded — ~49 lbs at the
    starting Strength of 2.00, not the ~110 lbs flat scaling would give. Ben's own
    framing: "it makes the player need to concentrate on inventory management and
    strength in the early game."
  - **Strength grows from carrying weight**, tiered by load ratio: ≤50% capacity,
    no gain (a light load teaches nothing); 50–80% marginal; 80–90% better; 90–95%
    the best rate; >95% ("Overloaded") the rate drops back down *and* Health drains
    at 2/s while sustained. **Real-time-calibrated**, not a raw XP number picked by
    feel: Ben's spec was "at a strength of 2, it should take 2 actual days of
    playing to raise by .25" — solved as a continuous-growth equation
    (`dL/dt = R(1 - L/100)`, `GainExperience`'s own diminishing curve treated as an
    ODE) anchored to the fastest tier. Every tier beyond that slows down further for
    free, off the same formula, correctly reaching exactly zero gain at Strength 10
    (level 100) — no separate slowdown mechanism was ever needed, just calibrating
    the existing one to the right magnitude.
  - **Real float-precision bug caught before shipping, not after:** at the
    calibrated rate, a single frame's gain is smaller than what a `float` can even
    represent added onto Strength's current level — confirmed by simulation (level
    stayed frozen after a simulated 2 days), not assumed. Fixed with a pending-gain
    accumulator that only flushes to `GainExperience` once enough has banked up to
    survive float precision at any level up to 100; re-simulated afterward to
    confirm the fix actually lands the target (22.4997 vs. a 22.5 target — correct).
  - **Movement tiers share Encumbrance's own thresholds, not a separate pair.**
    First pass used an independent 100%/150% breakpoint (a leftover from before the
    real capacity numbers existed); Ben's follow-up call — "let's match the movement
    rates to strength rates" — replaced it with the same 50/80/90/95% breakpoints
    Strength's own gain tiers use, now `public` on `PlayerEncumbrance` so both
    systems read one source of truth. ≤50% full speed; 50–80% 0.85x; 80–90% 0.65x,
    sprint disabled; 90–95% 0.45x; >95% 0.25x plus an extra 5/s Stamina drain while
    moving — the worst movement tier and the health-cost tier are now the same
    threshold, not two to track separately.
  - **Pickup blocked at/over capacity** (`PlayerLoot.Receive`/`ReceiveEquipment`) —
    "whatever you try to pick up, you can't" once `LoadRatio >= 1.0`. Existing
    callers already treated "nothing fit" as "leave it on the ground," so no
    caller-side change was needed.
  - **Reflected against the original wishlist line** once built: capacity growing
    with Strength ✅, movement speed/stamina cost ✅, tied to loot (pickup gate) ✅,
    tied to storage (only on-person weight counts) ✅. The one line-item gap —
    "movement *efficiency* also improving with skill," not just capacity — was
    explicitly closed as *not wanted*, Ben's call after reviewing the relative
    numbers: "I think that the relative amounts apply nicely. no change to that."
- **Item weights, tuned in passes, with a new shared table for "better = lighter."**
  A first attempt inverted the existing `CraftTierScale.Modifier` (built for
  capacity/price, 0.2x–5x) directly for weight — produced a 25 lb Crude Backpack and
  a hypothetical 5 lb Crude Knife, both rejected on sight ("a 5lb knife would be
  horrible... a 25lb backpack would be terrible as well"). Replaced with a dedicated,
  much narrower `CraftTierScale.WeightModifier` (Crude 1.5x → Masterwork 0.6x) — see
  `CLAUDE.md`'s new "a tier-scaling ratio tuned for one quantity doesn't transfer to
  another" gotcha, written up specifically so this doesn't get relearned by a future
  session or the other collaborator. Applied to the full Backpack (5 lbs Normal →
  7.5/6/5/4/3), Knife (1 lb → 1.5/1.2/1/0.8/0.6), Axe/Hammer (4 lbs →
  6/4.8/4/3.2/2.4), and Pickaxe (6 lbs → 9/7.2/6/4.8/3.6) ladders. Small
  Rock/Copper/Silver/Gold/Platinum Ore also hand-assigned (1.5/0.9/0.9/1.8/1.5 lbs)
  from an ad hoc tier-position mapping Ben specified directly. **32 `ItemDefinition`s
  still sit at the untuned 1 lb default** — full categorized list generated and
  logged in `BUGS_AND_ENHANCEMENTS.md` with a link, not silently left for someone to
  rediscover from scratch.
- **Testing-friction cleanup:** `AdminSpawnScreen`'s pre-existing 80-Plank/24-Stick/
  12-Rope auto-grant (built last session for testing the Plank build tier) was
  silently putting a fresh character 56 lbs into Encumbrance before they'd picked
  anything up — call disabled (method kept, easy to re-enable when next testing the
  Plank path specifically), so a fresh spawn now genuinely starts at 0 lbs carried.

### v0.1.188-dev — Plank tier for the whole Building System, and the real doorway bug found at last

Ben's ask: "we need plank versions of all of these building panel
models. the planks should fit nicely and should be visually
appealing... we don't want the gaps." Then, mid-build: Plank pieces
must also be directly buildable once a player has the skill, not just
upgrade-only targets — "any version they have the skill for, and
upgrade to the next as they get skills to do it."

- **7 new Plank pieces** (Wall, Half-Wall, Door-Frame Wall, Door, Roof
  Panel, Gable Panel, Pole) plus a **real visual for Plank Foundation**,
  which had never gotten one — it was still a plain Unity default Cube
  with a flat-color material from whenever the Twig→Plank upgrade path
  was first wired, invisible as a placeholder until Ben actually looked
  at one ("the plank foundation doesn't look real good"). All 8 now
  share one real Blender pipeline: a solid flat panel (or, for
  Door-Frame Wall, three boxes matching its own known-good doorway
  collider split; for Gable, a triangular prism; for Pole, square
  post-and-beam framing) with a baked board-seam-and-grain texture
  (`ShaderNodeTexWave` bands + `ShaderNodeTexNoise` grain, multiplied
  over a flat tan matching `PlankFoundation`'s own established color)
  instead of Twig's bundled-branches-with-gaps look — a real material-
  tier visual difference, not just a recolor.
- **Every Plank piece reuses its Twig counterpart's socket layout
  exactly** (same local positions/types), so all of `PlayerBuilding`'s
  existing placement branches (`wallOntoFoundation`, `roofOntoWall`,
  `doorOntoFrame`, the Door-Frame Wall's collider split, Pole's ground-
  tiling/stacking) work unmodified — zero new C# needed for placement,
  only for wiring each piece up as data. Confirmed via a full
  batch-mode sweep after all 8 were built: all 17 pieces resolve in
  `PlayerBuilding.allPieces`, all 8 Twig→Plank `nextTier` links point
  correctly, Roof's placement sign still lands inward, Door's still
  lands centered in its frame, the Door-Frame Wall's doorway is still
  walkable, and Pole's tiling/stacking both land with zero gap —
  reusing the same verification techniques proven on the Twig family
  rather than re-deriving them.
- **Plank pieces are directly buildable, not upgrade-only.** Set
  `unlockTier = CraftTier.Rudimentary` (skill level 10) on all 7 new
  pieces and wired them into `TestScene`'s `PlayerBuilding.allPieces`
  directly, per Ben's mid-build correction. Also retroactively fixed
  `PlankFoundationPiece.asset`, which had sat at `unlockTier: 0`
  (Crude — no skill gate at all) and was never in `allPieces` to begin
  with, meaning it was *only* ever reachable via the Hammer upgrade
  action, not really "directly buildable" despite existing as a real
  BuildPiece asset since v0.1.156-dev.
- **Real Blender bug found and fixed mid-build, not just flagged**:
  `shade_smooth()` on a low-poly box (the Pole's square posts/beams,
  and — found copy-pasting the same mistake into every panel script —
  the flat wall/roof/gable panels too) averages vertex normals across
  each shared corner's adjacent faces, which reads as an obviously
  rounded look on thin square posts but a much subtler, easy-to-miss
  lighting/brightness shift across even a large flat panel face. Caught
  first on Pole (posts rendered as tubes, not square lumber) and fixed
  there; initially suspected the *other* symptom — every Plank icon
  reading pale/washed-out — was the same bug wearing a different face,
  but disproved that directly (flat-shading the icon test model made no
  visible difference) before accepting the paleness is a separate,
  still-open issue (see `BUGS_AND_ENHANCEMENTS.md`) rather than
  conflating two bugs that happened to surface the same day.
- **The real doorway-walkability bug, finally found.** Logged
  2026-08-09 as unsolved after a resize (1.2m×2.0m → 1.5m×2.4m) passed
  every batch-mode check but still failed live. The missing piece came
  from Ben's own diagnostic observation: walking was blocked, but
  jumping or running through cleared it — the signature of a step-
  height problem, not a doorway/collider problem at all. Foundation's
  own exposed lip (top surface 0.4m above ground) was deliberately
  raised from 0.2m to 0.4m on 2026-08-09, the same day, a few hours
  before the doorway resize — but the Player's `CharacterController.
  stepOffset` (0.3m) was never revisited against that change. 0.4m >
  0.3m: walking onto *any* Foundation edge from ground level should
  have been blocked project-wide the whole time, not just at a
  doorway — the doorway just happened to be the one place someone
  actually tries to walk onto a Foundation, since Walls block every
  other edge outright. Every earlier check (`Physics.OverlapCapsule`,
  even a real `CharacterController.Move()` simulation without gravity)
  missed this because none of them tested walking up onto the
  Foundation's own edge specifically — they all tested clearing the
  doorway opening itself, which was never actually the problem. Fixed
  by raising `stepOffset` to 0.45 (keeping the lip height Ben chose,
  rather than reverting it) — confirmed via a real grounded
  `CharacterController.Move()` simulation (matching `FirstPersonController`'s
  own gravity/isGrounded handling) walking from open ground, through
  the doorway, past an open Door, all the way through — and confirmed
  live by Ben immediately after: "the door works much better just
  walking through it now."
- **Admin Spawn granted enough Stick/Rope for Twig testing but nothing
  for Plank** — Ben hit this immediately trying to test the new
  upgrade path ("waiting for the tree to respawn, only have 7 planks,
  so I can't test the upgrade"). Added an 80-Plank starting grant next
  to the existing one in `AdminSpawnScreen.Awake()`, covering one of
  every Plank piece (69 total) with slack to spare.
- **A real, separate bug found right after: upgrading Twig→Plank failed
  with "Not enough materials" even with 20 Plank on hand** ("same as
  the door" — hit on both Door-Frame Wall and Door, not piece-specific).
  Root cause: `PlayerPieceUpgrade.HasIngredients`/`RemoveIngredients`
  only ever called `inventory.GetCount()`/`RemoveItem()` directly — the
  player's own main-inventory list only, never the equipped Backpack or
  nearby StorageBox. `PlayerBuilding` already reaches all three
  (`ReachableInventories`) for placing a *new* piece; the *upgrade*
  action never got the same treatment. Same class of bug as the
  original "can't eat a Berry" fix — an item sitting somewhere other
  than the main list is invisible to a check that only looks there.
  Fixed by porting `PlayerBuilding`'s exact `ReachableInventories`/
  `GetAvailableCount` shape into `PlayerPieceUpgrade` (new
  `PlayerBackpack`/`StorageBox` reach, `storageRange` field). Verified
  with a real functional test, not just a compile check: 12 Plank
  placed in a nearby `StorageBox`, zero in the player's own main
  inventory — confirmed `GetAvailableCount` found all 12,
  `HasIngredients` passed, and calling `Upgrade()` actually consumed
  the box's Plank and swapped the placed piece from Twig to Plank
  Door-Frame Wall.

### v0.1.187-dev — Twig Pole: elevates a Foundation on stilts

The `PoleTop` socket type has sat unused in `SocketType` since the
Building System's very first pass (named ahead of time, 2026-08-08) —
this is what it was for. Ben's design, refined over three short
exchanges: "same as Foundation, but without the flat horizontal parts
filled in," then "4 poles and frame, one piece," landing on 4 corner
posts + a frame around the top + a frame around the middle, no solid
floor, genuinely walkable underneath.

- **One piece, Foundation's exact footprint (5m×5m), hollow.** 4
  corner posts (2m tall), a beam frame at the top (what a Foundation
  rests on) and another at half-height (bracing) — no slab anywhere.
  12 Stick, no Rope (plain post-and-beam joinery, not lashed like the
  Wall family — a deliberate visual differentiation, not an oversight).
- **Pole tiles beside a Foundation using zero new code** — its own 4
  edge sockets are plain `FoundationEdge` type, positioned/rotated to
  exactly match Foundation's own (North/South/East/West at the same
  local points, same yaw), so it drops straight into the existing
  generic flat-tiling formula every Foundation-to-Foundation placement
  already uses. Confirmed via batch mode: zero gap between a tiled
  Pole's own edge socket and the Foundation it tiled against.
- **Foundation stacking is the one genuinely new piece.** Foundation
  gained a new `SocketPoleTop` at its own local origin — the *exact*
  same point its `FoundationEdge` sockets already sit at, 0.4m below
  its own visible top surface. That means a stacked Foundation keeps
  its existing "sits slightly embedded" look, just embedded into the
  Pole's top frame instead of the ground, with no new offset math —
  same origin-socket trick as Wall/Roof/Door/Gable, just applied to a
  piece (Foundation) that predates that convention. New
  `foundationOntoPole` branch in `PlayerBuilding.ResolveFollowing`,
  reusing the same `LookRotation(socket.forward, up)` formula the
  majority of origin-socket cases already use (verified unnecessary to
  negate — Foundation's 4-fold symmetry means sign doesn't change the
  outcome, unlike Door). Confirmed via batch mode: zero gap between
  Foundation's own new socket and the Pole's top socket, and the
  resulting elevated Foundation's top surface lands at exactly 2.4m
  (2.0m post height + Foundation's own 0.4m socket-to-top-surface
  offset) — a sensible, walkable stilt height.
- **Per-element colliders, not one bounding box** — learned directly
  from Door-Frame Wall's own doorway bug two days ago: a single AABB
  over this whole open frame would have made the entire hollow middle
  solid. 4 post colliders + 8 beam colliders instead, matching only
  the real geometry, so the space underneath is actually walkable.
  This time the lesson was applied proactively, not found live.
  Icon baked cleanly via `IconBaker`'s normal path on the first try
  (unlike Gable Panel's still-open framing bug the day before).

### v0.1.186-dev — Twig Gable Panel: fills the roof's triangular gable ends

Building System item 2 of tomorrow's 3-item plan (see 2026-08-09's
closing notes): the wall-to-roof gap above the Foundation's other two
edges — the ones that don't carry a Roof Panel.

- **The math**: a gable roof's ridge runs across the *middle* of the
  building, parallel to the two walls carrying Roof Panels — meaning
  the two remaining walls (perpendicular to the ridge) sit under a
  *triangular* gap, not a rectangular one. Base = 5m (the wall's own
  width), apex height = 2.5 × tan(35°) ≈ 1.75m — the exact same rise
  the Roof Panel already climbs over its own 2.5m horizontal reach, so
  the triangle's sloped edges land at 35° too and the roof panels sit
  flush against them with no gap, by construction rather than by luck.
- **Twig Gable Panel**: branches follow a linear triangular envelope
  (`height(x) = apexHeight × (1 - |x|/2.5)`) instead of Wall's uniform
  height, two lashing bars placed at a fixed *fraction* of each
  branch's own height (not a fixed world height) so they read as
  straight lines following the roofline instead of a flat bar poking
  through the sloped edge partway across. Built vertically like Wall/
  Half-Wall (no tilt, no Blender→Unity export sign surprise to check
  for this time — confirmed empirically anyway: same-sign and negated
  placement rotations gave identical results, since this piece is
  symmetric in its own local X, unlike Door).
- **Zero new C# needed.** The Gable Panel's own attach socket is
  `WallTop` (matching Roof Panel's own convention exactly), so it
  drops straight into `PlayerBuilding`'s existing `roofOntoWall`
  placement branch — same target-socket type, same armed-socket type,
  no new case to add. Confirmed via a full 4-wall test (Roof Panels on
  North/South, Gable Panels on East/West): the Gable's own apex height
  (4.422) lines up with the computed ridge height (4.4367) within the
  same branch-radius tolerance the Roof-to-Roof ridge meeting already
  accepted.
- 6 Stick + 3 Rope, same Woodworking skill gate as the rest of the
  Wall family. Wired into `TestScene`'s `PlayerBuilding.allPieces` and
  confirmed resolving via a fresh batch-mode read (all 8 pieces now
  present).
- **Known gap, not fixed here**: the icon renders unreadably small and
  off-center regardless of camera direction tried — including one
  direction already proven working for Roof Panel's own similarly
  flat/wide shape — while a simpler fixed-orthographic-size debug
  camera (bypassing `IconBaker`'s tight-fit corner-projection framing
  entirely) produced a clean, well-framed result with the identical
  direction. That isolates the bug to `IconBaker`'s own tight-fit math
  specifically, not the camera angle or this piece's geometry — but
  the actual root cause wasn't found before Ben called it (reasonable
  — this had already gone through several dead-end attempts). Shipped
  with the rough icon rather than keep guessing blind; worth root-
  causing if another asset hits the same framing bug.

### v0.1.185-dev — A kickable Soccer Ball, for fun

Ben's ask, verbatim: "let's have a moment of fun." A real truncated-
icosahedron soccer ball, built and textured entirely in Blender, that
gets booted a random distance when the player walks into it.

- **Genuine soccer-ball geometry, not a texture trick**: start from an
  icosahedron (`primitive_ico_sphere_add(subdivisions=1)` — exactly 12
  verts/20 triangular faces), bevel every *vertex* by one segment (the
  standard trick for turning an icosahedron into a truncated
  icosahedron — each vertex becomes a small pentagon, each triangle
  shrinks into a hexagon), then push every vertex back onto the sphere
  so the flat polyhedron panels curve like a real ball. Confirmed
  exactly 12 pentagons + 20 hexagons came out the other end. The
  classic black/white pattern is just two flat materials assigned by
  each face's own vertex count (5 → black, 6 → off-white) — no UV
  texture or baking needed at all, unlike every wood-grain piece so
  far.
- **`SoccerBall.cs`** — a pure physics toy, not an `IInteractable`/
  `ISecondaryInteractable`: `OnCollisionEnter` checks for a
  `CharacterController` on whatever it touched, reads
  `PlayerVitals.IsSprinting`, and launches the ball with
  `Rigidbody.linearVelocity` set from the real projectile-range formula
  (`speed = sqrt(distance × gravity / sin(2 × angle))`) so it actually
  *lands* at the randomly-picked distance instead of just getting some
  force shoved at it. Normal contact: 3-7m at a random 5-30° angle.
  Sprinting: 5-12m at 5-45°. A short cooldown (0.4s) stops a rolling
  ball from re-triggering the kick every physics tick while still
  touching the player.
- **Two real sign/math checks caught before relying on a live kick to
  find them** (same discipline as the Roof/Door placement bugs): (1)
  confirmed `Quaternion.AngleAxis(-angle, player.right) * forward`
  actually tilts *upward*, not into the ground — hand math alone
  wasn't trusted this time. (2) A reflection-based check called the
  ball's own private kick method directly for both the normal and
  sprint case, then reverse-solved the resulting `Rigidbody.linearVelocity`
  back into an implied angle/distance via the same range formula, to
  confirm the actual physics output lands inside the requested ranges
  — not just that some plausible-looking vector got set.
- Regulation size-5 dimensions (22cm diameter, 0.43kg mass). One
  instance placed 3m in front of the player's spawn point in
  `TestScene` for immediate testing. Also given a real `ItemDefinition`
  (`SoccerBall.asset`) purely so it has a baked icon — not wired into
  Pickup/drop mechanics, since the ball's only interaction is the
  kick-on-contact, not inventory. Same "asset exists ahead of the
  mechanic using it" pattern already accepted elsewhere in this
  project (see `BUGS_AND_ENHANCEMENTS.md`'s Copper/Iron entries).
- **Two real bugs caught live within moments of Ben testing it, neither
  about the ball model itself.** First: "you clearly have never seen a
  soccer ball" — a plain grey cube with a "Pick up Soccer Ball" prompt,
  not the real ball. Root cause: `SoccerBall.asset` had no
  `worldPickupPrefab` set, so `PlayerDropping.SpawnPickup` (used by
  Admin Spawn, which now auto-lists the item since it's a real
  `ItemDefinition`) fell back to the game's generic placeholder pickup
  prefab instead. Fixed by wiring `worldPickupPrefab` to the real
  `SoccerBall.prefab` — confirmed via batch mode that the fallback
  simulation now produces the actual ball, and (a useful side effect)
  that it correctly gets *no* Pickup component and therefore no
  "Pick up" prompt at all, since `SoccerBall.prefab` never had one —
  matching the original intent of a pure physics toy, not an
  inventory item.
- **Second: "when the player runs into it, it doesn't get kicked, you
  just run over it."** Not a tuning problem — `CharacterController.Move()`
  resolves movement through its own kinematic capsule cast, not the
  normal PhysX solver, so it never fires `OnCollisionEnter` on anything
  it touches. `SoccerBall.cs` only listened for `OnCollisionEnter`,
  which is exactly why the batch-mode kick-math check (which called the
  kick method directly, bypassing real collision entirely) passed clean
  while live contact did nothing — the check never exercised the actual
  contact-detection path at all. Fixed by making `TryKick` public and
  adding `OnControllerColliderHit` to `FirstPersonController` — the
  real message `CharacterController` *does* send, on the player's own
  GameObject, once per thing it bumps into — which calls into the
  ball directly. `OnCollisionEnter` stays too, for genuine
  Rigidbody-vs-Rigidbody contact. Re-verified both the normal and
  sprint kick land in range through the now-public method, and
  confirmed the new method actually exists on `FirstPersonController`
  via reflection rather than just eyeballing the diff.
- **Third bug, right after the kick started working: "it rolled forever,
  and never stopped... rolled off the edge of the screen."** Not a fluke
  and not a friction-tuning problem — a rigid sphere in pure rolling
  (no slip) has ~zero relative velocity at its own contact point, so
  friction does essentially no work on it. An idealized PhysX sphere
  with only default friction genuinely never stops rolling on a flat
  plane; this is real physics, not a bug in Unity's friction model.
  The only real fix is Rigidbody linear/angular damping (continuous
  exponential decay, independent of contact/friction) — the original
  values (`linearDamping`/`angularDamping` 0.15/0.05) were far too low.
  Simulated several candidate values directly with `Physics.Simulate`
  (manual stepping, `SimulationMode.Script`) rather than guessing:
  0.15/0.05 never settled within 20 simulated seconds and covered
  33m+ (matches Ben's report almost exactly); 0.6/0.6 settles in
  ~5.6s over ~8m, chosen as a natural "rolls a good distance on grass
  then stops" feel over the faster-settling options tested (which felt
  more like hitting mud than rolling to a stop). Re-verified against
  the actual saved prefab's own values afterward, not just the test's
  local override.

### v0.1.184-dev — Door solution: Half-Wall, Door-Frame Wall, and a real working Door

The Building System's door plan, built in the order agreed with Ben: the
cheap reuse first, the genuinely new piece last.

- **Twig Half-Wall** — 2.5m wide (half of Wall's 5m), same 2.6867m height
  and bundled-branches-plus-rope visual language, half the branch count.
  Snaps to a Foundation edge with the exact same `wallOntoFoundation`
  placement math Wall already uses — no new socket logic needed at all,
  since its own `SocketBottom` sits at local origin same as Wall's.
  4 Stick + 2 Rope.
- **Twig Door-Frame Wall** — same 5m×2.6867m footprint as Wall, with a
  1.2m×2.0m doorway cut into the branch fan: thicker uniform-radius jamb
  posts flank the opening, a wood header beam (not rope) bridges the top,
  and the two rope lashing bars split around the gap instead of floating
  across it. New self-pairing `SocketType.DoorFrame` (same shape as
  `WallTop`'s), exposed as `SocketFrame` at the doorway's hinge-side
  bottom corner. Confirmed by direct measurement: placed on a Foundation
  edge, the frame socket lands exactly 0.6m off the wall's own root, not
  centered — the actual hinge corner, not a guess. 10 Stick + 4 Rope.
- **Twig Door** — the only genuinely new piece: no existing system
  animates, hinges, or times anything on a placed piece. `Door.cs` is the
  first `IInteractable` on a `PlayerBuilding`-placed piece. Modeled
  spanning local X 0 → doorWidth in Blender so its own local origin sits
  at the hinge corner — the same point it's placed at *and* the pivot it
  rotates around at runtime (no separate hinge child needed). Opens away
  from wherever the player is standing at the moment of the click (dot
  product against the door's own forward decides which side, so the leaf
  always swings to the side the player *isn't* on — Ben's ask: "that way
  it won't ever cause a problem opening or closing"), auto-closes 60s
  later if left open. 4 Stick + 2 Rope.
- **A second real export-sign surprise, caught the same way as Roof's.**
  Door's own measured bounds after import showed the leaf sitting in
  local **-X**, not the 0→+doorWidth range it was built in — the
  Blender→glTF→Unity pipeline flips both the "long axis" *and* the
  X axis relative to a naive same-sign copy (a consistent 180°-about-Y
  relationship once both this and Roof's own sign finding are put
  together, not two unrelated bugs). Hand math alone would have placed
  the door leaf 1.15m off the doorway's actual center — outside the
  opening, behind the jamb. Caught first, before it ever needed a live
  report: a throwaway batch-mode check (`DoorPlacementCheck.cs`,
  since-deleted) placed the door both ways and measured which one's
  bounds actually centered on the doorway gap. The *negated* formula
  was correct here — opposite of `wallOntoFoundation`/`roofOntoWall`,
  which both use the same-sign version. `Door.cs`'s own swing-direction
  math needed no change either way, since it only cares about relative
  sides, not an absolute forward convention.
- **Caught live, not by a batch-mode check: Door originally used E
  (`IInteractable`), same as every other interaction — but E is also
  `PlayerPieceUpgrade`'s own click-to-upgrade/hold-to-destroy key on
  any `PlacedPiece` (which every placed piece becomes, Door included).
  Ben found it immediately testing the real building: "since destroy
  the panel relies on E, there's no key press that opens the door" —
  with a Hammer equipped (the normal state while building), E never
  reached the door at all.** Root-cause fix, not a workaround: switched
  Door onto `ISecondaryInteractable` (F) instead — a system that
  already exists in this codebase for exactly this shape (an optional
  second action on its own key, currently used elsewhere for e.g. a
  water source's "Fill"). Door has no primary E action at all now, so
  it doesn't implement `IInteractable` in the first place — there's no
  overlap left to flag, not just a mitigated one. Confirmed via a
  throwaway reflection-based check that toggling `ISecondaryInteractable`
  directly still opens/closes correctly after the interface swap.
- **The real bug behind "F does not open the door" (Ben's next live
  report) wasn't the keybind at all — `TwigDoorFrameWallPiece`'s
  `BoxCollider` was sized from the whole mesh's AABB, and an AABB can't
  carve out a hole. It silently spanned the doorway gap too**, making
  the opening solid: unwalkable, and swallowing any interaction raycast
  aimed through it before it ever reached the Door standing behind. Same
  category of bug as Foundation's visual/collider mismatch earlier this
  week — a collider built from "the whole mesh's bounds" without
  checking whether the mesh actually fills that whole box. Fixed by
  splitting the one `BoxCollider` into three (`ColliderLeftFlank`,
  `ColliderRightFlank`, `ColliderHeader`) matching the solid geometry
  only, leaving the doorway's own X/Y region genuinely open. Confirmed
  via a throwaway raycast check: a ray through the doorway gap now hits
  the placed `TwigDoorPiece` and resolves its `ISecondaryInteractable`
  directly, while a ray at the flanking section still hits the wall.
- **A third real bug, found immediately after fixing the collider one:
  the doorway was open but still physically too tight to walk
  through.** Foundation's own edge socket sits 0.4m below Foundation's
  actual walkable top surface — the "mostly buried" offset every wall
  already inherits invisibly (it's why a Wall's base sits flush instead
  of floating), but for a doorway it directly eats into the usable
  headroom instead of being hidden inside a solid wall. The original
  1.2m×2.0m opening measured fine on paper but only gave 1.6m of
  *effective* clearance above the real floor — less than the
  CharacterController's 1.8m height. Resized both `generate_doorframe_wall.py`
  (1.2×2.0 → 1.5×2.4) and `generate_door.py` (1.1×1.95 → 1.35×2.3) to
  match, rebuilt both prefabs in place (same guids/paths, so neither
  the `BuildPiece` assets nor `TestScene`'s wiring needed touching).
  Confirmed this time with a batch-mode capsule check sized to the
  exact CharacterController dimensions (radius 0.4, height 1.8)
  standing at the doorway center — not just re-measuring the nominal
  opening size again, which is exactly what missed the deficit the
  first time. (One methodology wrinkle along the way: a capsule tested
  with its bottom sitting *exactly* at floor level always reads as
  "overlapping" the ground — normal standing contact, not a block —
  so the check needed a small standoff off the floor to give a real
  answer instead of a permanent false positive.)
  **Not actually fixed, per Ben's next live test (2026-08-09)** — the
  doorway is still unwalkable in the real game despite the resize and
  the clean batch-mode capsule result. Logged as an open bug in
  `BUGS_AND_ENHANCEMENTS.md` rather than shipped as resolved; the gap
  between a synthetic instantiate-and-query check reporting clear and
  the live `CharacterController.Move()` still blocking is itself the
  open question for next session.
- **Scene wiring bug from Roof Panel recurred twice more** (Half-Wall,
  then again before the fix stuck) — the reload-before-use guard added
  after Roof's own incident didn't actually fix it; the real cause looks
  like a timing race between `AssetDatabase.Refresh()`/`SaveAssets()`
  and the scene write, not a stale C# reference. Every subsequent piece
  in this batch (Door-Frame Wall, Door) skipped the automated
  `SerializedObject` scene-array write entirely and went straight to
  reading the asset's real guid (`AssetDatabase.AssetPathToGUID`) and
  patching `TestScene.unity`'s YAML directly, verified each time via a
  fresh batch-mode read of `PlayerBuilding.AllPieces`. Root cause still
  not found — flagging here so a future session doesn't re-attempt the
  "just reload the asset first" fix and rediscover the same failure.
- All three new pieces wired into `TestScene`'s `PlayerBuilding.allPieces`
  and confirmed resolving via a fresh batch-mode read. Full project
  recompile clean.

### v0.1.183-dev — Twig Roof Panel: two-piece ridge roof, same Blender pipeline as Wall

The Building System's third piece, and the first one that snaps onto
another *piece's* socket instead of onto Foundation. Ben's ask: two
panels, placed one at a time, one per wall, oriented to meet correctly
at a ridge — reusing Wall's exact visual language ("same visual").

- **Geometry built the same way as Wall**: `generate_roof_panel.py`,
  bundled branches + 2 rope lashing bars, same procedural Object-space
  wood-grain shader baked to a diffuse texture via Cycles. The one real
  difference: branches run along the panel's *slope* instead of
  standing vertically, and the 35° pitch is baked directly into the
  mesh (built flat, then rotated 35° about the width axis and applied)
  rather than handled as runtime math — same "simplify by design"
  approach that made Wall's own placement trivial.
- **Math**: for a 5m building width, each panel needs 2.5m horizontal
  reach to the ridge. At 35°, slant length = 2.5/cos(35°) ≈ 3.05m,
  ridge rise above wall-top ≈ 2.5×tan(35°) ≈ 1.75m — confirmed by
  direct measurement of the imported mesh (1.98m rise incl. branch
  radius, matches).
- **New `WallTop` socket on `TwigWallPiece.prefab`** (`SocketTop`, at
  the wall's own measured top surface Y=2.6867, read straight off its
  existing `BoxCollider` rather than a remembered number) and a
  matching `SocketEave` on the new `TwigRoofPanelPiece.prefab`, sitting
  at the model's local origin — the Blender build pins the eave edge
  there through the bake-in rotation, same trick as Wall's own
  `SocketBottom`. `BuildSocket.IsCompatibleWith` gained `WallTop`
  self-pairing, mirroring `FoundationEdge`'s.
- **`PlayerBuilding.ResolveFollowing` gained a `roofOntoWall` branch** —
  and a real sign bug caught before it shipped. Hand math suggested the
  panel's baked-in "eave→ridge" run direction needed the wall socket's
  outward-facing `LookRotation` *negated* to point back inward toward
  the building. Wrong: the Blender→glTF→Unity export flips the sign of
  the axis the branches run along, so the *same*-sign `LookRotation`
  (identical formula to `wallOntoFoundation`) is actually correct.
  Caught by writing a throwaway batch-mode check that placed a panel
  both ways and measured which one landed toward the Foundation's
  center rather than trusting the hand math — the negated version put
  the whole panel outside the building. A second throwaway check placed
  two panels on Foundation's opposite North/South edges end-to-end:
  their ridges land within 0.22m of each other (branch-radius padding)
  at identical height, a real ridge line, confirming both the math and
  the sign fix together before calling it done.
- **New recipe**: Twig Roof Panel, 10 Stick + 5 Rope (same
  `TwigWallPiece` skill gate), icon baked via the existing `IconBaker`
  at Wall's own 32/128 resolution. Wired into `TestScene`'s
  `PlayerBuilding.allPieces`.
- Not yet done, floated by Ben in the same planning pass but explicitly
  deferred: a half-width Wall variant, a door-frame Wall variant, and a
  Door piece with player-aware opening direction + 1-minute auto-close.
- **Caught live, not by a batch-mode check**: the Build tab didn't show
  Twig Roof Panel at all after the setup script ran — the new
  `PlayerBuilding.allPieces` array slot serialized as `{fileID: 0}`
  (null) instead of the piece's real guid. The setup script wired the
  scene reference in the same run it created the `BuildPiece` asset,
  without IconBaker's own established fix for exactly this failure
  mode (reload an asset fresh right before use, since
  `AssetDatabase.CreateAsset`/scene-load calls can invalidate an
  object reference held from before they ran) — same trap, different
  script. Fixed by writing the correct guid directly into
  `TestScene.unity`'s YAML and confirming via a fresh batch-mode read
  of `PlayerBuilding.AllPieces` that all four pieces (including Twig
  Roof Panel) now resolve.
- **Caught live again — the baked icon read as a single thin diagonal
  line**, not a fan of branches. Not a mesh or material bug (the actual
  in-game piece renders correctly, confirmed by Ben's live screenshot
  of the finished roof) — `IconBaker`'s fixed 3/4-from-above camera
  angle happens to run close to parallel with this specific panel's
  baked-in slope direction, foreshortening the whole fan into a
  sliver. Every other piece baked so far is closer to axis-aligned, so
  this never came up before. Fixed by giving `IconBaker.BakeAndWire`/
  `BakeOne` an optional `cameraDirection` override (default `null`
  keeps every existing icon's framing untouched) and re-baking just
  this one icon from a near-frontal angle instead. Confirmed via a
  throwaway multi-angle render comparison before picking the fix,
  rather than guessing at a new angle blind.

### v0.1.182-dev — Foundation raised a bit higher; starting materials for wall-testing

Two follow-ups after confirming the Wall snap system and the Foundation
alignment fix both work correctly:

- **Foundation's exposed lip doubled, 0.2m → 0.4m.** Ben's call after
  seeing the now-correctly-aligned platform in game: "needs to be
  slightly higher." `BoxCollider.center.y` raised from -0.3 to -0.1 on
  both `Foundation.prefab` and `PlankFoundation.prefab` (still 1m
  thick, same `-0.6`/`+0.4` split instead of `-0.8`/`+0.2` — still
  reads as buried, just less of it). Twig Foundation's visual
  re-aligned to the new collider position using the same measured-
  bounds correction from the previous entry (only the position offset
  changed, not the scale — thickness is unchanged). Plank Foundation's
  visual is a plain Cube driven directly by a position value, not a
  separate baked model — updated to match directly, no measurement
  script needed for that one.
- **Starting test materials, 24 Stick + 12 Rope — enough for exactly 3
  Twig Walls** (8 Stick + 4 Rope each). Ben's ask, for iterating on
  Wall placement without gathering/chopping first every time. Added to
  `AdminSpawnScreen.Awake()` (same Editor-only scoping as the rest of
  that tool — a testing convenience, not real game design; real players
  gather their own materials) rather than as a permanent starting
  loadout.

### v0.1.181-dev — Foundation's visible mesh never actually matched its own collider; Admin Spawn's ground raycast simplified

Two real bugs found live-testing the Wall, the second one hiding
inside a report that first looked like something else entirely.

**The actual bug: `Foundation.prefab`'s visible Twig mesh has never
matched its own `BoxCollider`.** Reported as "Admin spawn Twig
Foundation isn't working" — clicking Spawn appeared to do nothing,
while Plank Foundation worked fine from the same code path. Ruled out
several wrong theories in order before finding the real one: not a
missing prefab reference (resolved fine in isolation), not a Play-mode-
only exception (a reflection-driven live test threw nothing), not a
broken material/shader (confirmed via the Scene view's selection
outline actually tracing real geometry — the silhouette was there, just
not filled in from that framing), not a Ground collider/mesh mismatch
(a direct raycast test against the real Ground object landed exactly
at Y=0, matching its mesh precisely). The real answer, found by
measuring the instantiated prefab's actual renderer bounds against its
collider bounds directly: the `BoxCollider` spans Y -0.8 to +0.2 (the
documented "1m thick, mostly buried" design, correct and undisturbed),
but the rendered mesh spans Y **-2.29 to -0.10** — more than a meter
lower, with even its *top* sitting under the visible ground plane.
Left over from the earlier double-scaling fix (v0.1.169-dev-ish): the
footprint (X/Z) got corrected to a true 5×5, but the vertical scale/
position was never actually re-verified against the collider — nothing
exposed it before now because `IconBaker` frames every icon from the
mesh's own measured bounds regardless of where those bounds sit
relative to the collider, so it always looked correct in an icon,
completely independent of whether it lined up with the physical piece
in the world. Fixed by rescaling the nested model on its Y axis alone
(footprint-preserving) so its measured height matches the collider's
1m exactly, then repositioning it so its bottom lines up with the
collider's bottom — verified by direct before/after bounds measurement,
not just a visual glance. A same-day Twig Wall reported as "floating"
was very likely this same bug in disguise: it was resting correctly on
an already-placed Foundation's (real, correctly-positioned) collider
top — there was simply nothing visible underneath it to show that.

**Real but smaller: `AdminSpawnScreen.SpawnPiece` spawned pieces
directly at the player's own feet, then rescued them from potential
burial by teleporting the player onto the new piece's own measured top
surface afterward.** For a large, flat, ground-level piece like
Foundation, that rescue meant the player was immediately standing on
top of whatever just spawned, looking outward across an unbroken
horizon — visually indistinguishable from nothing having happened at
all, which is what "isn't working" first looked like before the real
bug above was found. Root-cause fix instead of another vantage-point
workaround: spawn a few meters in front of the player rather than at
their own position, so the piece is never underfoot to begin with — no
burial risk at the source, so the teleport-the-player-onto-it rescue
and the `CharacterController`-disable dance it needed are both gone
entirely, not just papered over.

### v0.1.180-dev — Twig Wall: a real placeable Wall, modeled and textured entirely in Blender

Ben's ask: "let's do the blender deal to create a twig wall, and do the
entire texture run as possible within blender" — the model *and* its
texture built and baked fully offline this time, no Tripo3D API call
anywhere in the pipeline. Scope confirmed up front (asked rather than
assumed): not just the asset, but a genuinely placeable Wall that snaps
to a Foundation edge in-game.

**Geometry**, `bpy`/`bmesh`, matching Twig Foundation's "bundled sticks
and branches lashed together with rope" material language as a
vertical piece instead of a flat platform: 15 tapered vertical branch
cylinders (each individually jittered in radius, height, lean, and a
gentle organic wobble — same lesson as every prior Blender prop, avoid
uniform/robotic repetition) packed across a 5m span (matching
Foundation's own edge length) up to ~2.6m tall, plus 2 thin horizontal
lashing bars wrapping across near the top and bottom on a separate
"rope" material. Two materials on one mesh, same pattern as the Stone
Knife/Hammer.

**Real texture baking, new ground for this project:** the wood
material's color comes from a procedural Noise+Wave node graph (fine
grain stretched along the branch axis, plus a coarser blotch pattern
for tonal variation, both driven by Object-space 3D coordinates rather
than UV — keeps the pattern continuous across all 15 branches instead
of reading as 15 separately-seamed tiles) baked to a real 1024×1024
image via Cycles (`bpy.ops.object.bake`, `type='DIFFUSE'`) — glTF can't
carry a procedural node graph, so this is the step that makes the
texture actually exportable. A plain `smart_project` UV unwrap exists
purely to give the bake somewhere to write pixels; it's not what drives
the pattern. The rope material stays flat-colored, deliberately not
baked — thin and small on screen, not worth the extra material-baking
complexity (confirmed baking a multi-material mesh with only one
material carrying an image node works cleanly, no error, just an
informational skip for the un-baked one).

Proved the whole bake pipeline on a plain test cube before touching the
real geometry — cheap to debug there, expensive to debug tangled with
the wall's actual mesh. Two real Blender gotchas hit and fixed on a
separate tiny test object (a Berry Seed-scale detour) earlier the same
day turned out to matter here too and were already known going in:
`obj.bound_box` needs a real depsgraph pass before it's trustworthy,
and small objects can clip against the default camera near-plane.
Neither actually recurred on the wall itself (it's not tiny), but the
awareness carried forward.

**Making it a real placeable Wall, not just an importable model:**
- `BuildSocket.IsCompatibleWith` extended — `FoundationEdge` now also
  accepts a `WallBottom` socket (previously only paired with itself).
- `PlayerBuilding.ResolveFollowing` gained real per-socket-type
  placement math for this pairing, closing the gap its own
  `panelHalfSize` comment already flagged ("Only correct for square,
  axis-aligned pieces like Foundation; Wall/Door will need real
  per-socket alignment math when they're added"). A Wall snapping onto
  a Foundation edge now stands vertically (`Quaternion.LookRotation`
  against the socket's own outward-facing direction) instead of lying
  flat and offsetting sideways the way a second Foundation panel does.
  Simplified by a real design choice: the wall's own `WallBottom`
  socket sits at its exact visual origin (bottom-center), placed
  directly at the Foundation's own edge-socket position — Foundation's
  socket already sits ~0.2m below its visible top surface (the slab is
  buried per its existing "1m thick, mostly buried" design), so the
  wall's base ends up slightly embedded in the slab rather than
  floating above it, with zero extra offset math and no risk of the
  two connected sockets drifting outside `BuildSocket`'s same-point
  tolerance.
- New `TwigWallPiece.asset` (8 Stick + 4 Rope, Woodworking-trained,
  Crude tier — a judgment call, not specified) and
  `TwigWallPiece.prefab`, added to the scene's `PlayerBuilding.allPieces`
  so it's actually selectable from the Build tab.

Verified via batch-mode checks (measured import bounds confirmed
height lands on Unity's Y axis with no axis-correction needed, and a
direct logic test confirmed both socket-compatibility directions and
the prefab's own socket wiring) — the live in-game placement/snap feel
itself still needs a real playtest.

### v0.1.179-dev — Berry Bush searching gets its "super success" bonus: a 2% Berry Seed chance

Closes most of a long-open enhancement request (`BUGS_AND_ENHANCEMENTS.md`,
originally 2026-08-07): "search the berry function... random chance of
finding up to 4 berries. additionally, a super success chance of
finding a berry seed." The base random-yield search already existed
(v0.1.169-dev); this adds the missing second half.

`BerryBush.CompleteSecondary` now rolls a separate, independent chance
(`berrySeedChance`, `[Range(0,1)]`, wired to 0.02 = 2%) on every search,
regardless of the normal 0-3 berry roll's outcome — a search that finds
zero berries can still find a seed, and a full-yield search can find
one too. Deliberately independent rolls, not a bonus conditioned on
"finding the max," since nothing in the original ask implied that
coupling.

New Berry Seed item, modeled the same way as the recent Blender props:
a small teardrop/almond shape built via `bmesh` (136 verts, one
material, dark reddish-brown). Two real bugs hit building it, both
from working at a genuinely tiny scale (0.014m long) for the first
time this session:
- The Blender preview render came back blank — `obj.bound_box` read as
  a stale zero-size box with no depsgraph evaluation pass between
  building the mesh and reading it back; switched to computing bounds
  directly from `mesh.vertices` instead.
- Still blank after that fix — the real cause was the camera's default
  near-clip plane (0.1m), well *larger* than the camera-to-object
  distance for an object this small, clipping the entire model out of
  frame. Fixed by setting `clip_start`/`clip_end` proportional to the
  object's own measured radius instead of leaving Blender's default.
- Also needed one round of the now-familiar color-darkening fix
  (same root cause as the Stone Hammer, v0.1.177/178-dev) — the first
  material read too light once baked through `IconBaker`.

Unity side: new `BerrySeed.asset`/`BerrySeedPickup.prefab`
(`SphereCollider`, `ContinuousDynamic` Rigidbody per the known thin-
Ground-collider tunneling gotcha), icon baked via `IconBaker`, wired
into the scene's `BerryBush.prefab` and verified resolving correctly
via a batch-mode read-back before considering this done.

**Not done, and not asked for:** whether a Berry Seed ever becomes
plantable — that question is exactly as open as when first raised.
This entry only adds the item and its spawn chance.

### v0.1.178-dev — Stone Hammer head redesigned: crosswise, not a fatter cylinder

Same-day follow-up to v0.1.177-dev. Ben's reaction to the shipped
result, verbatim: "these models are horrible. can we make the hammer
head look progressively like a real hammer instead of a cylinder of
rock on a handle?" — accurate. The first version built the head as a
continuation of the *same* axis as the handle, just widening — a
lollipop/mace silhouette, not a hammer, regardless of how well the
surface detail or tier progression worked.

Rebuilt from scratch with the one change that actually mattered: the
head is now a **separate tube built along a perpendicular axis** (Z,
crosswise) centered where the handle (built along X, as before) meets
it — the classic sledgehammer/maul silhouette, immediately readable at
icon scale. Two independent ring-lofted meshes merged into one bmesh
rather than one continuous profile function; the handle's far end
extends slightly into the head's solid volume so there's no visible
seam. Tier progression (head size shrinking from chunky/lumpy to
compact/refined, surface noise fading, color darkening, a lashing
collar at Fine/Masterwork) carried over unchanged from v0.1.177-dev's
logic, just now applied along the head's own Z-axis length instead of
continuing the handle's X-axis.

Caught the fix's own rendering trap before it shipped: the throwaway
Blender preview script had a fixed guessed camera position left over
from the old shaft-and-blob layout, which badly cropped the new
off-center head. Fixed by computing the camera position/orthographic
scale from the object's actual bounding box instead of a hand-tuned
guess - the same lesson `IconBaker` already applies for the real game
icons, now applied to the throwaway Blender-side preview tooling too.

Unity side: same in-place model swap as before. All 5 pickup prefabs
re-swapped and re-baked; visually confirmed as a real, recognizable
hammer at every tier before considering this done.

### v0.1.177-dev — Stone Hammer tiers get real Blender models; design constraint from Ben: the shaft doesn't improve with Hammer tier

Same Blender pipeline as the Trimmed Stick and Stone Knife, applied to
the Stone Hammer — all 5 tiers previously shared one placeholder model
at the same scale. Ben's direction shaped the design directly: "since
the hammer requires a trimmed stick, let's make the shaft of the
hammer a wooden shaft, and the improvement would be in the shape of
the hammer head" — a Trimmed Stick is a real crafting ingredient with
its own separate tier ladder, so the Hammer's own tier shouldn't
re-skin it. The shaft is one plain wooden material/shape across all 5
tiers; every bit of tier progression lives in the head instead — both
its silhouette (large and organically lumpy at Crude, shrinking and
tightening toward a compact precise cylinder by Masterwork) and its
surface (chipped-stone noise fading to smooth, color darkening from
grey flint toward near-black polished stone), plus a lashing-cord
carving detail at the neck once refined enough (Fine/Masterwork).

Two real bugs hit and fixed along the way, both worth remembering for
the next tiered-prop build:

- **Crude/Rudimentary's silhouette came out as illegible white
  "feathers"** in the baked icon, not a solid stone head. Root cause:
  fixing it required bumping those tiers from 5 to 6 sides so they'd
  clear the `sides >= 6` smooth-shading threshold (5 sides + per-vertex
  chip noise was producing near-degenerate sliver faces that read as
  thin bright streaks from IconBaker's fixed camera angle) — a genuine
  geometry fix, confirmed first in a plain Blender render before ever
  touching Unity.
- **That same shading fix then washed the color out to near-white**,
  even after darkening it once (the same fix that worked for the Stone
  Knife). Root cause this time was physical, not a bug: a rough/diffuse
  (high-Roughness) material under IconBaker's uncapped directional
  lights (no tonemapping) reflects a much larger share of incident
  light back toward camera once its surface is smooth-shaded and
  facing the lights broadly, than the same material flat-shaded (where
  roughly half the faces sit in shadow) ever did — every previously-
  baked rough/matte icon happened to be flat-shaded, so this never
  surfaced before. Tried lowering `IconBaker`'s ambient intensity
  first (1.0 → 0.3); barely moved the result, confirming the
  directional lights themselves were the real driver, not ambient —
  reverted that change to stay consistent with the already-completed
  full re-bake sweep. Fixed instead by pushing Crude/Rudimentary/Normal
  head color much darker than their apparent "rough stone grey" input
  value would suggest (down to ~0.04-0.08 linear) to compensate.

Unity side: same in-place model-swap pattern as the Knife (all 5
existing pickup prefabs already correctly referenced by their item
assets, so no rewiring needed), collider re-measured from each
model's actual bounds (~0.40m long, head bounds shrinking tier over
tier from the shape change alone). The original placeholder
`StoneHammer.glb` is left in place, unreferenced.

### v0.1.176-dev — Full icon re-bake against the IconBaker ambient fix

Follow-up to the blue-tint bug found while baking the Stone Knife
(previous entry): re-baked every existing icon rather than leaving the
other 52 to carry a possibly-subtler version of the same wrong blue
ambient cast. One throwaway sweep script, `IconRebakeSweep.cs`, walked
every `ItemDefinition` and `BuildPiece` asset and called
`IconBaker.BakeAndWire` again straight from each item's own
`worldPickupPrefab`/`prefab` — no need to track down each item's
original source model separately, since `IconBaker` only needs a
`Renderer` anywhere in the hierarchy and doesn't care about the extra
`Pickup`/`Collider`/`Rigidbody` components a pickup prefab carries.
56 items + 2 build pieces re-baked, 0 failures. Spot-checked a spread
across material types (metal axe head, silver ore, copper, stone) —
all read correctly with no unwanted color cast; nothing regressed.

### v0.1.175-dev — Stone Knife tiers get real Blender models; IconBaker's blue-tint bug found and fixed

Same approach as the Trimmed Stick tiers (v0.1.173-dev), applied to the
Stone Knife: all 5 craft tiers previously shared one placeholder
Tripo3D model (`CrudeStoneKnife.glb`) at different non-uniformly
stretched scales — a real gap, and Ben's ask: "let's see if we can use
blender to create better models for all 5 tiers... I'm thinking we can
use noise applied base colors."

Built via `bpy`/`bmesh`: a blade+handle shaft assembled ring by ring
(60 segments), with a flattened diamond/lens cross-section (4 points
for Crude/Rudimentary, 6-8 for Normal through Masterwork) instead of
the stick's round one — width and thickness both follow a length-wise
profile that stays roughly round through the grip, then widens into a
leaf-shaped blade before tapering to a point. Two materials per mesh
(blade and handle get independent face `material_index` assignment),
so tier progression covers both shape and color at once:

- **Crude → Normal**: blade edge noise (two layered sine components
  per angular slot — chip-sized + fine micro-texture; a single
  low-frequency term first read as a smooth wavy ribbon, not chipped
  flint) fades to 0, radial segments rise 4→6, blade color moves from
  dull grey flint toward a cleaner grey.
- **Fine/Masterwork only**: a decorative handle-wrap detail (shallow
  carved rings around the grip, like a wound cord binding) — first
  attempt used the same depth/spacing as the stick's carving and came
  out as a stack of beads, not a wrapped cord; widened and shallowed
  until it read as ribbed/fluted instead.
- **Blade color** shifts from grey flint to near-black glossy obsidian
  by Masterwork (paired with falling Roughness, 0.90 → 0.10); handle
  color warms from dark leather-brown to a lighter wood/bone tone.

Real bug found and fixed in the shared `IconBaker` tool while baking
these: every icon renders in a scratch scene that `IconBaker` never
configured explicitly, so it inherited Unity's default skybox-ambient
lighting (a blue-gradient procedural sky) — invisible on every icon
baked so far since they all happened to be warm/saturated colors, but
a strong, wrong blue cast on the knife's neutral grey stone (confirmed
via a debug pass: the actual material `baseColorFactor` values were
exactly correct, gamma-encoded as expected — the bug was purely in
the render environment, not the material data). Fixed by setting flat
white ambient and disabling environment reflections in `BakeOne()`
before rendering. Not yet re-applied to the other 52 existing icons —
they may have a subtle version of the same cast that was never
noticeable against their own warmer palettes; worth a full re-bake
sweep at some point but not done here.

Separately, darkened the actual blade base colors after the ambient
fix — the original values (chosen against the *buggy* blue-tinted
render) turned out too light/washed-out once the render was color-
correct, nearly blending into the icon background at the Crude end.

Unity side: swapped each of the 5 existing pickup prefabs' model child
in place (`RockKnifePickup`, `RudimentaryKnifePickup`,
`NormalKnifePickup`, `FineKnifePickup`, `MasterworkKnifePickup` — all
already correctly referenced by their item assets, so no rewiring
needed) rather than creating new prefabs, mirroring the Masterwork
stick retexture swap. Collider re-measured from each model's actual
bounds (~0.28m long, consistent across all 5 — a real size chosen
directly rather than inherited from the old placeholder's arbitrary
non-uniform stretch). The original `CrudeStoneKnife.glb` placeholder is
left in place, unreferenced.

### v0.1.174-dev — Masterwork Trimmed Stick gets a real Tripo3D-generated wood texture

Same-day follow-up to the flat-color Blender sticks: tested whether
Tripo3D can texture a model *we* built rather than one it generated,
since that combines controlled procedural geometry with real PBR
texture quality. It can — `texture_model` accepts any model registered
via `import_model` (an uploaded external file), not just Tripo3D's own
generations.

New `Tools/Tripo3D/Texture-Model.ps1`, a genuinely different pipeline
from `Generate-Model.ps1`: that script's `v3` REST API
(`openapi.tripo3d.ai/v3/generation/...`) has no documented endpoint for
texturing an existing mesh, so this one uses the task-based `v2` API
(`api.tripo3d.ai/v2/openapi`, `POST /task` with a `type` field) instead —
confirmed against Tripo3D's own official Python SDK source
(`VAST-AI-Research/tripo-python-sdk` on GitHub), since the interactive
docs site is a JS-rendered SPA no simple fetch can read. Same API key
works for both surfaces. Three real dead ends hit before landing on the
working shape:

- The obvious `/upload` endpoint rejected the `.glb` outright — turned
  out to be image-only despite the SDK routing model files through it
  as a "legacy" fallback.
- The real path is STS-credentialed S3 upload (`POST /upload/sts/token`
  returns temporary AWS credentials), which needs actual SigV4 request
  signing — installed the `AWS.Tools.S3` PowerShell module
  (`Install-Module -Name AWS.Tools.S3 -Scope CurrentUser`, plus the
  NuGet provider it depends on) rather than hand-rolling AWS's signing
  algorithm.
- The STS response's `s3_host` pointed at a real `us-west-2` AWS
  bucket, not a custom endpoint — the first attempt hardcoded
  `us-east-1` and got a clean "region is wrong" error back.

Ran the real pipeline once end to end: uploaded
`TrimmedStickMasterwork.glb`, imported it as a Tripo3D task (free, 0
credits), then textured it with a wood-grain prompt ("rich polished
walnut wood, fine warm honey-brown grain... hand-oiled lacquered
finish... photorealistic PBR wood material") at detailed quality (20
credits). Confirmed via `GET /user/balance`: 340 credits remaining.
Notable finding — texturing an existing model costs the *same* as a
full from-scratch `text_to_model` generation (both 20 credits at
default settings, confirmed against this session's own Twig Foundation
log) — so the real advantage of building geometry in Blender first was
never cost, it's that Blender can guarantee a coherent tier family in a
way independent Tripo3D generations can't.

Tripo3D's pipeline re-normalized the model's scale during import/
texture (came back 1.0m long instead of the original 0.6m) — caught by
re-measuring bounds after the swap rather than trusting the source
file's known dimensions, and corrected by rescaling the pickup's model
instance back to match the other 4 tiers and Stick's own real length.
`TrimmedStickMasterworkPickup.prefab`'s model child swapped to the new
textured asset (`Assets/Models/TrimmedStickMasterworkTextured.glb`),
collider re-measured, icon re-baked. The original flat-color
`TrimmedStickMasterwork.glb` is left in place, unreferenced, in case the
pure-Blender version is wanted again. Crude/Rudimentary/Normal/Fine
still use the flat-color material — this was a single-tier test, not
yet applied to the rest of the set.

### v0.1.173-dev — Trimmed Stick tiers get real models/icons, generated entirely in Blender (no Tripo3D)

Filled a real gap — all 5 Trimmed Stick craft tiers (Crude through
Masterwork) previously had no icon at all, and only Crude had a world
pickup (a placeholder reusing the plain Stick's branch model). This
doubled as the test Ben asked for after the Blender wall-separation
research earlier in the session: could Blender build models from
scratch, not just edit/split existing Tripo3D output?

It can. `bpy`/`bmesh` scripted headless (`blender --background --python
...`) built a tapered shaft (`bmesh`, ~40 rings along the length, quad
bridged between them) and varied it procedurally per tier — a case
Tripo3D genuinely can't do well, since 5 independent AI generations
wouldn't relate to each other as a coherent progression:

- **Crude → Normal**: fewer radial sides (5/6/8) and a smooth
  low-frequency per-angular-slot wobble (not independent per-ring
  noise, which read as spiky static — first attempt looked like a
  crystal, not a branch) fading to ~0 by Normal, plus a single-arc bend
  fading out the same way. Reads as an unevenly-shaped, roughly-trimmed
  branch, straightening out tier by tier.
- **Fine**: dead straight, smooth-shaded 12 sides, two shallow carved
  rings (a Gaussian falloff cut inward at fixed points along the
  shaft) — "a few little carvings," per Ben's direction.
- **Masterwork**: 20 sides, straight, plus a finer/shallower ring
  pattern and a wrapping spiral groove layered on top for a genuinely
  ornate, engraved look (first attempt used the same depth as Fine's
  rings plus a strong spiral — read as lumpy/caterpillar-like, not
  ornate; toned down to a crisper, narrower carve).

Real bugs hit and fixed along the way: (1) `primitive_cone_add` only
has two vertex rings (base + tip) — bend/noise/carving had nothing to
act on until rebuilding the shaft manually via `bmesh` with real
length-wise resolution; (2) `bpy.ops.mesh.subdivide` on the cone
subdivided radially too, not just along the length, ballooning one
tier from 10 to 3,250 verts; (3) the length-position parameter `t` was
computed as `x / length` (range roughly ±0.5) instead of normalized to
0–1, so ring/spiral positions landed off the actual mesh and the bend
formula tilted the stick diagonally instead of bowing it.

Unity side: one throwaway batch-mode script (`TrimmedStickSetup.cs`,
deleted after running, matching every other one-shot Editor script in
this repo) built a `Pickup` prefab per tier (`BoxCollider`/`Rigidbody`
sized from the model's actual measured bounds, `ContinuousDynamic`
collision per the known thin-Ground-collider tunneling gotcha) and
called `IconBaker.BakeAndWire` per tier (32px icon, 128px preview —
matches the existing convention). All 5 came out at 0.6m long,
matching the original Stick's own collider size. `BerryBush.prefab`'s
chop-drop (`trimmedStickPrefab`) was repointed from the old placeholder
to the new Crude-tier pickup, and the placeholder `TrimmedStickPickup.prefab`
was deleted as orphaned once nothing referenced it anymore.

Verified by rendering a quick preview PNG per tier straight out of
Blender before ever touching Unity (caught the bugs above early/cheap),
then by reading back each `ItemDefinition.asset`'s wired `icon`/
`previewIcon`/`worldPickupPrefab` GUIDs and visually inspecting the
baked icons themselves.

### v0.1.172-dev — Admin-spawned pieces were floating at head height, not on the ground

Same-day follow-up to the previous entry's "no longer bury the player"
fix — Ben spawned a Twig Foundation, reported "can't climb onto it...
can't even try running and jumping," and a corrected screenshot showed
why: the piece was floating roughly 1.8m above real ground, legs
dangling in open air, not sitting flush with a small lip like it's
supposed to.

Root cause: `AdminSpawnScreen.SpawnPiece`'s ground-detection raycast
(cast straight down from 2m above the player) hit the **player's own
`CharacterController` capsule** (top at `Center.y + Height/2` = 1.8)
before it ever reached the actual terrain — `Physics.Raycast` doesn't
exclude the caster's own collider by default. The piece spawned at
that wrong, elevated hit point instead of on the ground. Fixed by
disabling the `CharacterController` for the duration of the raycast
(and the subsequent stand-the-player-on-top repositioning from the
previous fix, which needed the same treatment to avoid fighting a
direct `transform.position` set).

This likely explains most of the original "can't climb onto it, need
stairs/ramp" report on its own — a real ~1.8m gap obviously isn't
reachable by a normal jump, versus the intended ~0.2m lip. Worth
re-testing before concluding stairs/ramps are urgently needed for
Foundation specifically.

Verified via a full batch-mode compile check.

### v0.1.171-dev — Berry Bush gets a genuinely distinct look; Admin-spawned pieces no longer bury the player

Live-testing found two more real bugs, plus a design fix that resolves
a confusing report from earlier in the session:

- **Berry Bush now uses the "Generated Berry Bush" leafy model instead
  of the Strawberries cluster.** Root cause of "the berries didn't get
  fixed... you can't do anything with the bush": the standing bush and
  every loose dropped Berry used the *exact same* Strawberries model —
  visually indistinguishable, so a correctly-scattered, perfectly
  pickable berry looked identical to the bush itself sitting right next
  to it. Ben's call: reuse the "Generated Berry Bush" model (an
  existing decorative comparison prop from an earlier session, `Assets/Models/GeneratedBerryBush.glb`,
  never wired to any script) as the bush's real visual instead — now
  genuinely different shapes, can't be confused again. All
  `BerryBush` field wiring (chop tools, Trimmed Stick/Berry prefabs,
  skill, cooldowns) carried over untouched; `berryPrefab` still points
  at the original `BerryPickup.prefab`, so search still drops the same
  strawberry-cluster pickup as before — that prefab was never touched
  either, still plain `Pickup` (pick up + eat), never had chop/search.
  Removed the now-redundant decorative duplicate from the scene, since
  its model is doing real work now instead of just sitting there for
  comparison. Also **shrank the loose Berry pickup** (0.35m bounds →
  0.18m) — sized to look right next to itself as the old bush, it read
  as oversized (a third of the *new*, bigger bush's size) once the bush
  became a visually distinct, larger plant; re-baked its icon at the
  new size.
- **Admin-spawned Build Pieces no longer bury the player.** Ben:
  "when I spawned the twig foundation, it pushed me under the world. I
  should be pushed up onto it instead." Root cause: the spawn raycast
  originates from the player's own position, so Foundation's collider
  (extends 0.8m below ground, only 0.2m above) materialized wrapped
  around the player's feet — `CharacterController`'s own depenetration
  resolved downward instead of up. Rather than rely on physics to
  guess correctly, `AdminSpawnScreen.SpawnPiece` now explicitly stands
  the player on the freshly-spawned piece's own *measured* top surface
  afterward (disabling/re-enabling `CharacterController` around the
  direct position set) — generalizes to any piece shape, not just
  Foundation's specific dimensions.

Verified via full batch-mode compile checks; every visual re-baked and
spot-checked directly (Berry's icon still reads clearly as strawberries
at the smaller size, thanks to the tight-fit framing from the icon fix
just before this).

### v0.1.170-dev — Admin tab can spawn Build Pieces directly, for testing

Ben: "let's spawn a foundation in place for now" — extended the
existing Editor-only `AdminSpawnScreen` (already spawns any
`ItemDefinition` for free) with a second list for `BuildPiece`s. Spawn
places the piece on the ground directly under the player via a
straight-down raycast, free of materials/skill gates, and tags it with
a real `PlacedPiece` component so upgrade/destroy still works on it
exactly like a normally-built one — a genuine placed piece, not a
lookalike prop. Same Editor/testing-only scoping as the item list
(`#if UNITY_EDITOR`, won't appear in a build).

Verified via a full batch-mode compile check.

### v0.1.169-dev — Scattered berry unpickable; real Twig Foundation model; icons re-baked to fill the frame

Three pieces, caught/requested in quick succession:

- **A scattered Berry could be permanently unpickable.** Root cause:
  `BerryBush.SpawnScattered` used `Random.insideUnitSphere` for both the
  spawn offset and the launch direction — capable of landing (and
  settling, after gravity) close enough to overlap the bush's own
  `SphereCollider` (radius 0.175). Unlike `ResourceNode`/`ChoppableTree`,
  `BerryBush` never disables its collider (the whole point of the
  redesign is it stays interactable throughout), so a scattered item
  landing that close got its raycast permanently shadowed by the bush
  itself — confirmed live via a screenshot showing the bush's own chop/
  search prompt while aimed at what looked like a loose berry. Fixed by
  spawning on a fixed 0.45 horizontal ring (guaranteed outside the
  collider) and pushing further in that same outward direction instead
  of a fully random one. Affects both chop's Trimmed Sticks and search's
  Berries, since both go through the same helper.
- **Real Twig Foundation model**, via the Tripo3D API — a genuine
  lashed-twig-and-rope platform on short legs, replacing the plain
  procedural Cube slab. Hit the same "stuck at 99%, actually succeeded
  server-side" pattern documented in `Tools/Tripo3D/README.md`; recovered
  by polling `GET /v3/tasks/{id}` directly rather than re-generating.
  Swapped into `Foundation.prefab`'s `Slab` child only — the root's
  `BoxCollider` and all 4 `BuildSocket`s are completely untouched, so
  gameplay footprint/snapping/upgrades need no changes. Hit and fixed a
  real double-scaling bug along the way (the `Slab` parent still carried
  its old `{5,1,5}` cube-fitting scale, which stacked with the new
  model's own footprint-fit scale to 25x instead of 5x) — caught by
  re-baking the icon and seeing obviously-wrong bounds in the log before
  it ever got visually reviewed.
- **`IconBaker` reframed to actually fill the icon** — Ben: "when I look
  at it, its hard to see the object clearly." Root cause: camera framing
  sized off `maxDim` (the single largest axis) with a flat padding
  guess, which was never what the fixed 3/4-angle camera actually
  projects — the further a shape diverges from a cube (a wide flat
  Foundation, a long thin Nail), the more empty space that guess left.
  Now projects the AABB's 8 corners into camera space and sizes/centers
  to the *true* on-screen extent, ~8% margin. Exposed a new
  `IconBaker.BakeAndWire` so a sweep script could re-bake all existing
  icons in one process instead of 50+ separate Unity launches — every
  icon and BuildPiece tile in the game (53 total) re-baked in one pass,
  0 skipped, 0 failed.

Verified via full batch-mode compile checks throughout; every visual
result (Twig Foundation, Storage Box, Nail) spot-checked directly before
moving on.

### v0.1.168-dev — Build tab gets the same tile-grid + search treatment as Crafting

Ben: "let's do the same thing with the build tab" — same visual/browsing
layer as Crafting's redesign, but deliberately **not** the batch/timer/
cancel machinery, since placement has no analog for it: each piece is
still one deliberate walk-and-aim act in the world, not something that
produces instantly into inventory. `PlayerBuilding.ArmPiece` and the
whole placement flow are completely untouched.

- **`BuildPiece` gained `icon`/`previewIcon` fields**, same shape as
  `ItemDefinition`'s. Baked both existing pieces (Twig Foundation,
  Storage Box) via `IconBaker` from their own placed-piece prefabs.
- **`IconBaker` generalized** — it was hardcoded to `ItemDefinition`
  (a `LoadAssetAtPath<ItemDefinition>` type-gate), which silently
  rejected `BuildPiece`. Wiring already happened generically via
  `SerializedObject.FindProperty` by field name, so the fix was just
  loosening the load/type-check to `UnityEngine.Object` — no other tool
  needed for this, matching its own "one reusable icon tool" intent.
  (Hit a real `CS0104` ambiguous-`Object` compile error along the way —
  `System.Object` vs `UnityEngine.Object` — fixed by fully qualifying.)
- **`BuildScreen` rewritten** from a text list to the same tile shape as
  Crafting: big icon (blank spacer if unset), live materials have/need,
  a skill-requirement line, and the existing Arm/Armed button —
  unchanged interaction, just a tile instead of a row. `PlayerBuilding`
  gained a public `GetAvailableCount` (same reach as its existing
  `ReachableInventories`) so the tile can show live counts; `HasIngredients`
  now just calls it instead of duplicating the summation.
- **Search bar**, same shape as Crafting's — Build has no discipline
  tabs to override, so this is a plain substring filter over
  `pieceName`.

Verified via a full batch-mode compile check; both new icons read
clearly at preview size before wiring.

Ideated first (an HTML mockup, matching the game's existing dark
debug-panel look), then Ben resolved every open question in one message:
background-continuing timer, `CraftTierScale.HoldDuration` reused for
per-item time, cancel-with-refund, tool-break stops the batch. Two real
systems, not just a reskin:

- **`PlayerCrafting` gained a real batch-crafting queue**, replacing the
  old instant single-craft `TryCraft` entirely. `StartCraft(recipe,
  quantity)` removes ingredients for the *whole* batch up front (same
  all-or-nothing gate checks `TryCraft` always had — tool, skill, Anvil
  surface, output space), then `Update()` ticks one item at a time on a
  timer sized by `PlayerSkills.GetHoldDuration` — the exact same
  skill-scaled duration ladder gathering already uses, so higher skill
  crafts faster, not a bespoke number. Deliberately **not** gated on the
  Crafting tab being open or any key held — closing the menu or walking
  away doesn't pause it, unlike every hold-and-release interaction
  elsewhere in the game. `MaxCraftable(recipe)` (materials-only, read by
  the new Max button) and `CancelCraft()` (refunds ingredients for
  whatever hadn't completed yet — already-crafted items stay, nothing to
  undo there) round it out. **Tool-break stops the batch:** if a
  spectacular-failure roll breaks the required tool mid-batch, the next
  tick detects the tool's gone and stops with a refund instead of
  silently no-oping through the rest of the queue.
- **`CraftingScreen` rewritten from a flat text list to a tile grid** —
  each tile: a big icon (`previewIcon`, falling back to `icon`, falling
  back to a blank spacer — Ben's call — rather than a placeholder glyph;
  8 recipe outputs, mostly the Trimmed Stick tiers, don't have one baked
  yet), materials with live have/need counts, tool/skill/Anvil
  requirement lines, a quantity stepper, Craft, and Max. While a tile's
  own batch is running, the stepper/Craft/Max row is replaced by a
  progress bar + Cancel, reusing the exact green-fill bar look
  `PlayerInteraction`'s gathering hold already uses. Only one batch at a
  time — every other tile's Craft/Max greys out with "Crafting queue
  busy" while one's active.
- **Search bar**, right above the grid: case-insensitive substring match
  against the recipe's output item name, ignoring the discipline tab
  filter entirely while active (searches every discipline at once, not
  just whichever tab happens to be selected) — "ax" finds every unlocked
  or locked Axe tier in one view. Clearing the box reverts to the normal
  per-discipline tab view.

Verified two ways: a full batch-mode compile check, and a direct
state-machine test (via reflection, since `Time.deltaTime` doesn't tick
meaningfully in a non-play batch script) confirming `StartCraft`'s
upfront removal, a second `StartCraft` correctly refusing while one's
active, and `CancelCraft`'s partial refund math, all matched exactly.

See `docs/design-brief.md`'s new section for the full shape and the
ideation mockup.

Ben: "let's add an action to the berry bush as well. e should chop it if
you have a knife or ax in your hand. you should get trimmed sticks. f
should search the bush to find 0 to 3 berries which would drop to the
ground..." — replaces the old single instant-E-grabs-a-berry model
entirely with two independent gather actions.

- New `BerryBush.cs` implements both `IInteractable` (E — hold to chop,
  gated on any Knife or Axe tier in hand, same shape as
  `ChoppableTree`) and `ISecondaryInteractable` (F — search, no tool
  needed). Each action has its own independent 180s respawn cooldown
  (`chopRespawnAt`/`searchRespawnAt`) — the bush itself never
  disappears, only each specific action goes quiet for a while, unlike
  `ResourceNode`/`ChoppableTree`'s hide-the-whole-object model. Chopping
  scatters 2 loose Trimmed Stick pickups (Crude tier) and trains
  Woodworking; searching rolls 0–3 and scatters that many loose Berry
  pickups — both reuse `ResourceNode`'s exact scatter-with-`Rigidbody.AddForce`
  shape.
- **Real structural snag, resolved:** `Berry.asset.worldPickupPrefab`
  turned out to point at the *same* `BerryPickup.prefab` used for the
  placed bush — a dual-purpose prefab. Repurposing it in place would
  have broken dropping a Berry from inventory (no more `Pickup`
  component to receive `PlayerDropping`'s `Configure` call). Split into
  three: `BerryPickup.prefab` stays exactly as it was (the loose,
  droppable single Berry — still `Berry.asset`'s `worldPickupPrefab`,
  and now also what `BerryBush`'s search action spawns), a new
  `BerryBush.prefab` (no `Pickup`, no `Rigidbody` — static, reuses the
  same Strawberries visual) is the actual world bush, and a new
  `TrimmedStickPickup.prefab` (reuses `StickPickup`'s branch model as a
  placeholder visual) is `CrudeTrimmedStick.asset`'s new
  `worldPickupPrefab`, since chopping needed a real ground-pickup
  prefab to scatter and Trimmed Stick never had one before (it was
  always crafted straight to inventory).
- Scene's placed "Berry Bush" swapped from a `BerryPickup.prefab`
  instance to the new `BerryBush.prefab`, same position — verified by
  reading back the saved scene YAML, not just the batch log.

See `docs/design-brief.md`'s new section for the full shape. Verified
via a full batch-mode compile check.

Ben: "the berry doesn't respawn. fix it" — fair demerit (🍓💀, see
`AWARDS.md`): `canRespawn: 0` was sitting right in the `Pickup` field
block I read and edited earlier today fixing Berry's null `item`
reference, on the same object type (Stick pickups) I'd just been
comparing it against for their own respawn behavior, and I didn't act
on it. Added a `canRespawn: 1` override on the Berry Bush's scene
`PrefabInstance` (mirroring exactly how the two Stick Pickup scene
instances already override it, rather than changing
`BerryPickup.prefab`'s own default — keeps a future non-respawning use
of the same prefab, e.g. a dropped Berry, unaffected). Verified by
having Unity actually open the scene and read back the resolved
component value, not just trusting the hand-edited YAML: `canRespawn=True
respawnDelay=180`.

### v0.1.164-dev — Stick pickup never worked at all; Push's hold was fragile to aim jitter

The likely real explanation for the whole-session "stick doesn't decrease"
mystery, plus a genuine Magic System bug found live-diagnosing the
"kinetic skill isn't pushing anything" report:

- **`StickPickup.prefab`'s `Pickup.item` was null** — third instance of
  the exact same bug class as Berry (`BerryPickup.prefab`, fixed
  earlier today). This pickup point (world model literally named
  `TreeBranch_PolyByGoogle`) never actually granted a real Stick at
  all; walking up and picking one up did nothing. Swept every other
  `*Pickup.prefab` in the project for the same pattern and found two
  more: `RopeCoilPickup.prefab` and `RockKnifePickup.prefab` (the
  Crude Knife's world pickup) — both fixed. (`DroppedItem.prefab`'s
  null `item` is correct as-is — it's the generic fallback template
  `PlayerDropping` configures dynamically per-instance at spawn time,
  not a bug.)
- **Push's hold was fragile to any one-frame raycast flicker.**
  `PlayerInteraction.HandleWish` required the raycast to resolve the
  *exact same* GameObject on every single frame of the hold — stricter
  than the E-interaction hold (`HandleInput`), which has no such check
  at all. Any momentary aim jitter, or a multi-collider model (like
  Backpack) briefly resolving a different collider, silently reset
  progress to 0 — and since wishes deliberately show no progress bar
  (Ben's "zero on-screen hints" call), this was completely invisible.
  Confirmed live: holding R on a Backpack (which does have a
  Rigidbody) for several seconds produced nothing, no message either
  way — ruled out lineage (Kinetic, correct), Will (100/100, well
  above Push's 60 cost), and hold-duration awareness (held
  continuously) before finding this. Relaxed to match E's proven
  model — accumulate whenever a valid target is resolved and R is
  held, no frame-to-frame identity requirement. `lastWishGameObject`
  removed entirely.

Verified via a full batch-mode compile check.

### v0.1.163-dev — Foundation: 1m thick, mostly buried (superseding the "raised above ground" pass)

Ben, immediately after the previous fix: "let's make the foundation 1
meter thick. that way it will appear to be sitting in the ground with
the top slightly above the ground and visible as a real foundation" —
a different, more specific look than fully-raised. `Foundation.prefab`
and `PlankFoundation.prefab`'s Slab child + collider now both scale to
`y: 1` (was `0.3`) and sit at `y: -0.3` (was `0.15`), putting the top
0.2m above ground and burying the remaining 0.8m — reads as a real
poured foundation wall rather than a thin raised deck. Same pure-offset
approach as the prior pass: sockets stay root-relative, so
snapping/upgrades need no other changes.

### v0.1.162-dev — Foundation raised above ground; Drop gets a quantity picker

Two follow-ups from the same bug report, both Ben's call:

- **Foundation raised above ground.** Ben thought "5m" meant Foundation
  was 5m *thick* and expected it to stand above the grass — it's
  actually a 5m × 5m *footprint*, 0.3m thick, and was positioned with
  its top surface flush with y=0 (a poured-slab look). Ben's call: raise
  it instead, so the whole 0.3m slab sits above ground level (bottom at
  y=0) and reads as a visible platform. Moved both `Foundation.prefab`
  and `PlankFoundation.prefab`'s Slab child + collider from
  `y: -0.15` to `y: 0.15` — a pure offset change, so every socket/
  snapping/upgrade calculation (all relative to the shared root)
  stays correct automatically, no other logic touched.
- **Drop gets a quantity picker.** Previously Drop always removed an
  item's *entire* stack with no way to choose less — fine for most
  stackable items, but the exact bug Ben hit: 2 Hammers (non-stacking,
  `maxStack: 1`, so 2 separate slots) meant "Drop" dropped both when
  only one was wanted. New `DrawItemDropPopup` mirrors the existing
  Coin-drop popup exactly (-10/-1/+1/+10/All steppers) — except it
  defaults to the *full* count already held rather than 0, since
  "drop everything" is the common case for items (unlike coins), so
  the popup doesn't turn a one-click action into a two-click one for
  that case. `PlayerDropping.DropFrom` gained a quantity parameter
  (old 2-arg call sites, e.g. `PlayerLoot`'s hand-eviction, are
  untouched — still drop everything, no popup needed there).

Verified via a full batch-mode compile check.

### v0.1.161-dev — Nail's wrong skill gate; Eat and Move both broke on non-main-inventory items

More bugs caught immediately in the same live-testing pass:

- **Nail required Metalworking 25, with no way to reach it.** `Nail.asset`
  (and `StorageBoxItem.asset`, same latent issue, not yet visibly broken)
  were created via `ScriptableObject.CreateInstance`, which left `tier`
  at its default `Normal` — `PlayerCrafting.HasRequiredSkill` reads
  `outputItem.tier` directly to compute the skill gate, so an item with
  no real tier ladder needs to explicitly opt out with `tier: 0` (Crude),
  same as Rope/Cloth already do. Fixed both.
- **Eating from a hand slot or a Backpack/Storage popup silently did
  nothing.** Root cause: `PlayerEating.TryEat` always removed from the
  main inventory specifically, regardless of where the item actually was
  — the new Eat button (added earlier today) found the edible fine and
  showed the button, but `RemoveItem` on the wrong inventory found zero
  and quietly failed, while the popup still closed as if it worked. Added
  `TryEatFrom(Inventory source, item)`; `TryEat` is now a thin wrapper
  for the main-inventory case.
- **Moving more than fits failed outright instead of moving what fits.**
  Every "To Left Hand"/"To Right Hand"/"To Backpack"/"To Inventory"/"To
  Storage" button passed the source's *full* matched count as the move
  quantity. For a stacking item this is usually fine, but a non-stacking
  item (Hammer, `maxStack: 1` — each occupies its own slot) breaks
  immediately: 2 Hammers into an empty single-capacity hand slot failed
  completely instead of moving the 1 that actually fits. New
  `Inventory.SpaceFor(item)` (how many more fit) and
  `InventoryTransfer.MoveAsManyAsFit(from, to, item)` (caps the move to
  `min(available, space)`) — every move call site in `InventoryScreen`
  now goes through it. Verified directly: 2 Hammers, empty hand, old path
  moved 0; new path moves 1, leaves 1 behind.
- **Investigated, not reproducible:** Ben's report that crafting Trimmed
  Stick didn't decrement Stick or increment Trimmed Stick. A faithful
  full-pipeline batch test (real `PlayerInventory`/`PlayerSkills`/
  `PlayerEquipment`/`PlayerCrafting`, Sticks in inventory, Knife
  equipped, `CrudeTrimmedStickRecipe`, 5 consecutive `TryCraft` calls)
  showed correct behavior every time — Stick decremented, Trimmed Stick
  incremented, skill rose, every attempt. No code bug found; needs a
  clearer repro (see `TEST_FEATURE_PLAN.md`/design-brief for the open
  question).

Verified via full batch-mode compile checks; the Hammer-move fix also
verified directly against the exact reported scenario, not just by
reading the code.

### v0.1.160-dev — Nail, the AnvilSurface gate, and a real buildable/pickupable Storage Box

Ben: "let's use the api to create a nail model... the recipe will call for
the iron chunks that are in inventory. you need a boulder or an anvil
within 2m and a hammer in hand" — followed by "let's create a recipe for
the storage box... 4 planks and 6 nails" and "we need to build icons for
the storage box as well. we should be able to pick it up."

- **Nail** — generated via Tripo3D (clean first attempt), imported as
  `Assets/Models/Nail.glb`, icon baked via `IconBaker`. `NailPickup.prefab`
  built from scratch (Pickup/Rigidbody/BoxCollider, same shape as
  `RopeCoilPickup.prefab`). `NailRecipe.asset`: 1 Iron → 5 Nails, trains
  Metalworking, any Hammer tier in hand (not consumed).
- **New general gate: `CraftingRecipe.requiresAnvilSurface`.** Not
  Nail-specific — a new `AnvilSurface` marker component (empty, just a
  tag) that any world object can carry; `PlayerCrafting.HasNearbyAnvilSurface`
  passes if any one is within 2m. Boulder is now tagged with it, and a
  real placed Anvil object (the model from the prior session, previously
  import-only) now sits in `TestScene` near the Boulder, also tagged —
  positioned using its actual measured bounds so it sits on the ground
  rather than floating/sinking (the project's documented model-pivot
  gotcha). `CraftingScreen` shows "— requires a Boulder or Anvil nearby"
  when out of range, same convention as the tool-in-hand gate.
- **Storage Box, built.** `StorageBoxPiece.asset` — a real `BuildPiece`
  (4 Plank + 6 Nail, trains Woodworking — Plank is the defining structural
  material per the established discipline-sort rule, Nail is a fastener
  like Rope was for Twig Foundation), placed through the existing Building
  System exactly like Foundation. Reuses the placeholder Cube-primitive
  look the fixed "Small Storage Box" scene object already had, extracted
  into a real reusable `StorageBox.prefab`.
- **Storage Box, pickupable.** `StorageBox.cs` now implements
  `IInteractable` directly — Ben's call: must be empty first (no risk of
  silently losing stored items), no tool required (a plain "pick up my
  furniture" interaction, deliberately not routed through
  `PlayerPieceUpgrade`'s Hammer-gated system at all). Picking one up
  destroys the placed instance and adds a new portable `StorageBoxItem`
  (icon baked) to inventory. That item's own `worldPickupPrefab` points
  right back at the same `StorageBox.prefab` — dropping/placing it later
  spawns a real, working, empty box again, not an inert prop, for free
  (`PlayerDropping.SpawnPickup` already gracefully skips its `Pickup.Configure`
  call when a prefab has no `Pickup` component, so this needed zero
  changes to the drop path). Wired onto both the new buildable Storage Box
  *and* the original pre-existing "Small Storage Box" scene object, so
  every box in the game is pickupable, not just newly-built ones.

See `docs/design-brief.md`'s new "Storage Box: Build, Pick Up, Place
Again" section for the full shape. Verified via full batch-mode compile
checks after each step, every asset/scene edit verified by reading back
the actual saved YAML.

### Anvil model generated and imported (doc-only, no version bump)

Ben: "let's use the api to create an anvil" — generated via
`Tools/Tripo3D/Generate-Model.ps1`, clean on the first attempt, imported
as `Assets/Models/Anvil.glb`. Deliberately stopped there per Ben's
call — no prefab, no scene placement, no recipe. There's no Forging/
Metalworking mechanic to attach it to yet (Core Pillars' "hammer + anvil
+ wood fuel + steel → sword" is still aspirational text, not a designed
system), so this is just the model sitting ready for whenever that gets
built. See `Tools/Tripo3D/README.md`'s "Current status" for the prompt
and details. No gameplay code changed, nothing on-screen differs.

### v0.1.159-dev (follow-up) — Build-cancel key conflicted with cursor unlock; Building couldn't see Backpack/Storage materials

Caught immediately by Ben re-testing the fixes above: arming a Foundation,
failing to place it ("Not enough materials"), then pressing Escape to get
out left the Player Menu unable to reopen at all ("nothing there" when
pressing Tab). Two separate real bugs, not one:

- **Escape was double-booked.** The build-cancel fix above bound cancel to
  Escape, but `FirstPersonController` already reads Escape the same frame
  to unlock the cursor. Both firing together left the cursor unlocked
  with nothing actually open — and `PlayerMenuScreen`'s Tab handler
  deliberately refuses to reopen while the cursor's already unlocked (so
  it can't stack on top of another open screen), so Tab silently did
  nothing. Moved build-cancel to **Right Mouse Button** instead, which
  nothing else in `FirstPersonController` reads.
- **`PlayerBuilding` only ever checked the main 4-slot inventory.** Ben
  reported having enough Stick/Rope and still getting "Not enough
  materials" — root cause: unlike `PlayerCrafting` (which already reaches
  main inventory → equipped Backpack → nearby Storage Box), Building
  never looked past the main inventory at all, from the very first
  version of the system. Gave `PlayerBuilding` its own
  `ReachableInventories()` mirroring Crafting's exact reach.
- **Couldn't eat a Berry sitting in a hand.** Same shape of gap as the
  Pickaxe-to-hand fix above, mirrored: Eat only ever showed in the main
  inventory list (`DrawInventorySection`), never in the shared move-popup
  used for a hand slot, Backpack, or Storage Box contents
  (`DrawMoveDestinations`). Added an Eat button there too, shown first
  when the item is edible.

Verified via a full batch-mode compile check.

### v0.1.159-dev — Four more live-testing bugs: Berry pickup, Plank size, build-cancel, ingredient substitution

Continuing the same-day system-test pass. Four issues from a single round
of feedback, all fixed:

- **Berry pickup did nothing.** `BerryPickup.prefab`'s `Pickup.item`
  field was never wired to the Berry `ItemDefinition` — `{fileID: 0}`,
  silently null since the prefab was made. The v0.1.139-dev model swap
  fixed the visual but not the underlying reference. Set directly.
- **Plank looked too small on the ground.** Bumped both the visual
  model's scale and the pickup `BoxCollider`'s size by 1.5x together, so
  the clickable area still matches what's visible.
- **No way to cancel out of build placement.** Once a piece was armed,
  Escape only stepped back from the rotate/confirm sub-phase to the
  following-ghost phase — never fully disarmed. Combined with "Not
  enough materials" leaving you re-armed (not un-armed), a failed
  placement could strand you following a ghost with no way out. Fixed:
  Escape while following now calls `ArmPiece(null)`. Also made
  `BuildScreen`'s "Armed" button itself clickable to un-arm, for a mouse
  path alongside the keyboard one.
- **Ingredient matching was exact-item-only.** Crude Axe (needs raw
  Stick) rejected an inventory full of Trimmed Stick; Crude Fiber
  Backpack/Belt (need raw Fiber) had no way to use Woven Grass Cloth, a
  pickup with no use anywhere until now. Ben's call: build a general
  mechanism rather than patch these two recipes. New
  `ItemDefinition.baseItem` field (refined item → the raw material it
  came from) plus a new `IngredientMatching` helper
  (`Satisfies`/`GetCount`/`Remove`) that both `PlayerCrafting` and
  `PlayerBuilding` now route through — exact stock is always spent
  before substitutes. See `docs/design-brief.md`'s new "Ingredient
  Substitution" section for the full shape.

Verified via a full batch-mode compile check.

### v0.1.158-dev — Fixed: no way to move a plain item from the main inventory to a hand

Caught by Ben during the first real system-test pass: a freshly crafted
Pickaxe (or any plain tool) sitting in the main 4-slot inventory
(`PlayerCrafting.AddCraftedOutput` sends plain output straight there,
not to a backpack) had **no path to a hand at all**. The "To Left Hand"/
"To Right Hand" options only ever existed inside a Backpack/Belt/Storage
Box's contents grid (`DrawContainerContents`, which makes every occupied
slot clickable to open the full move-destination popup) or on an item
already sitting in an equip slot — the main inventory list
(`DrawInventorySection`) only ever offered Eat/Drop/To Pack/To Storage
for a plain item, never a hand. A tool crafted with no backpack equipped
was effectively stuck — usable as a tool-gate check nowhere, since
`ResourceNode`/`ChoppableTree` both require it actually held in a hand,
not just carried.

- Added "To L Hand"/"To R Hand" buttons directly to the main inventory
  row, same `InventoryTransfer.Move` call the popup's own hand buttons
  already use — no new mechanism, just closing a real gap in an
  existing one.

Verified via a full batch-mode compile check.

## 2026-08-08

### MVP progress re-check, third pass (doc-only, no version bump)

Ben: "what's left in the mvp to work on" — updated `docs/design-brief.md`'s
MVP Progress Check-In section again rather than re-deriving from scratch.
Basic building moves from not-built to built (Foundation is real, not
complete — no Wall/Door/Pole/Floor/Ceiling/Roof/Equip-to-Define yet).
**Revised tally: 8 of Phase 1's 11 items built, 3 entirely unstarted:
Encumbrance & skill-based movement, Basic combat + first aid, and
Hireable autonomous NPCs** — nothing exists for any of the three, not
even partially. No gameplay code touched.

## 2026-08-08

### v0.1.157-dev — Upgrade/destroy: click a placed piece to upgrade, hold 5s to destroy

Ben: "lets go ahead and build it" — implements the click-vs-5s-hold
mechanic from the ideation above, plus a real Plank Foundation to
upgrade *to* (otherwise the mechanic would have nothing to prove out
end-to-end, same "ship a real working example" discipline as every
other system this session).

- **`BuildPiece.nextTier`** — the next rung of the material ladder, null
  at the top or if no upgrade exists yet.
- **`BuildSocket.FreeConnectedSockets`** (static) — frees every socket on
  a destroyed instance *and* whatever they were touching, without a
  stored bidirectional link: two snapped sockets end up at the exact
  same world position by construction (confirmed from the placement
  math), so "find the other side" is just "find any other occupied
  socket at that same point."
- **`PlacedPiece`** (new, trivial) — tags a real (non-ghost) instance
  with which `BuildPiece` it is; `PlayerBuilding.Confirm` now attaches
  one to everything it places.
- **`PlayerPieceUpgrade`** (new) — its own raycast/E-handling, not a
  reuse of `IInteractable`'s hold-and-release: releasing early *is* the
  upgrade action here, only holding past 5 seconds does something else
  (destroy), which is backwards from how every other hold in the game
  works (release early = cancelled). Requires a Hammer (any tier) in
  hand for both actions. Upgrade is destroy-and-replace-in-place at the
  identical transform, with old socket-occupied state carried over by
  nearest-position match. Destroy frees connected sockets and refunds
  nothing — a pure loss, per Ben's call.
- **`PlankFoundation.prefab`/`PlankFoundationPiece.asset`** — identical
  shape to Foundation (same 5×5 slab + 4 sockets), lighter material, 8
  Plank, Woodworking-trained. `TwigFoundationPiece.nextTier` now points
  to it, so the whole ladder step is real and testable, not just wired
  infrastructure with nothing on the other end.
- **Full UI on purpose** (unlike Magic) — a prompt names the upgrade
  target and shows the destroy countdown live, plus a "not enough
  materials"/"already highest tier" message, all deliberately visible.

Verified via a full batch-mode compile check and by reading back the
saved scene/asset YAML: `PlayerPieceUpgrade.hammerTiers` (all 5 real
references), `TwigFoundationPiece.nextTier` (guid matches
`PlankFoundationPiece.asset`'s own), and `PlankFoundation.prefab` (4
sockets) — not just trusting the batch log.

**Known gaps, flagged not hidden:** no progress bar for the 5s destroy
hold (text countdown only); Rock/Metal tiers still don't exist, so the
ladder stops at Plank for now; Wall/Pole/Door still don't exist, so
Foundation is still the only upgradable/destroyable piece.

### Roadmap notes: Nails + buildable Storage Box, storage-capacity motivation (doc-only, no version bump)

Ben, mid-build of the upgrade/destroy system: "we need to implement
nails (requiring iron and a hammer). this allows us to add a storage
box that can be built with planks and nails," then "with the amount of
materials to build a structure, we'll need to make sure we have storage
so we can collect enough resources." Both captured in `docs/design-brief.md`'s
Building System roadmap rather than expanding the in-progress
implementation pass — Nails fits the material web's already-sketched
but unbuilt Ingot→Forging→Forged Component branch (Forging-trained,
consuming `Iron` directly for now); the Storage Box would reuse the
existing `StorageBox`/`Inventory` components as a placeable `BuildPiece`
rather than a new storage mechanism; the storage-capacity concern is the
stated motivation for building it, not a separate ask. Not designed in
detail or built. Continuing the already-committed upgrade/destroy +
Plank Foundation build.

### Upgrade/destroy: Hammer required for both, destroy refunds nothing (doc-only, no version bump)

Ben: "destroying doesn't return materiel. upgrade or destroy requires
the hammer" — resolves the two open questions left from the previous
entry. Both now settled in `docs/design-brief.md`: destroy is **not**
bare-handed after all (Hammer required for both actions), and destroying
a piece is a **pure loss**, no partial material refund.

No code touched — pure design.

### Upgrade/destroy interaction corrected: click vs. 5s hold, not a skill-tiered hold (doc-only, no version bump)

Ben: "we should have click to upgrade, and a click and hold to destroy
- a 5 second timer." Corrects the upgrade-path entry from earlier the
same session (which wrongly modeled upgrade as a skill-tiered hold) and
adds a genuinely new mechanic — destroy — that hadn't been captured at
all. Updated `docs/design-brief.md`'s Building System section:

- **Click (instant, Hammer in hand) upgrades** one material tier
  (Twig→Plank→Rock→Metal). Same destroy-and-replace-in-place mechanics
  as before, just triggered by a tap, not a hold.
- **Click-and-hold for a flat 5 seconds destroys** the piece outright —
  not skill-tiered, unlike every other timed action in the game so far.
- **Flagged as architecturally new**: this is tap-vs-hold-threshold on
  one object (release early = upgrade, hold past 5s = destroy), not a
  hold building toward one single outcome the way every other
  `IInteractable` works (where releasing early always means "cancelled,
  nothing happened"). Needs its own dedicated logic on placed pieces,
  not a straight reuse of the existing hold-and-release code path.
- **Left open**: whether destroy needs the Hammer too (leaning no — bare-
  handed, just slow) and whether destroying refunds any materials.

No code touched — pure design correction/addition.

### Building upgrade path: Hammer + E upgrades a placed piece one material tier (doc-only, no version bump)

Ben: "we also want to have an upgrade path. if you have a hammer in
hand, you can upgrade from twig to plank etc." Added to `docs/design-brief.md`'s
Building System section, reusing existing pieces rather than inventing
new ones:

- **Reuses the existing 5-tier Hammer item** as the upgrade tool (same
  "any tier counts" gate convention every tool check already uses) —
  not a new dedicated tool.
- **Rides E, not the Left Mouse Button/scroll placement scheme** —
  upgrading an existing placed piece is an `IInteractable` hold-and-
  release action like everything else in the game, not a new placement.
- **Destroy-and-replace in place**: old instance destroyed, target
  tier's prefab instantiated at the same transform, socket-occupied
  state carried over so neighbors don't read the connection as freed.
- **Cost/skill training is just the target tier's own `BuildPiece`
  data** — upgrading to Plank costs and trains exactly what building a
  fresh Plank piece would, not a separate rule.

No code touched — pure design, added alongside the Stairs/Ramps/Shelves
roadmap note above in the same session.

### Building roadmap: Stairs, Ramps, Shelves added (doc-only, no version bump)

Ben: "we will need recipes for stairs, ramps, shelves, etc" — added to
`docs/design-brief.md`'s Building System section as tracked-but-not-
designed, split into the two categories they actually fall into:
Stairs/Ramps are **vertical connectors** (need sockets at two different
heights, which the current horizontal-only Foundation-to-Foundation
socket system doesn't support yet); Shelves and other furniture/fixtures
**mount onto a Wall** rather than tiling edge-to-edge with the
structural shell, closer to how `IWishTarget`/`IEquippable` attach to
something else. No code touched — Wall/Pole/Door are still the nearer
gap.

### v0.1.156-dev — Building System first slice: Foundation, free + edge-snapped placement

Ben: "well, no time for the present, let's build it in" — first
implementation off the Building System ideation above. Scoped to
**Foundation only**, same "skeleton + one real path" discipline as
Magic's first pass (Spark before Push/Heal Self) — Wall/Pole/Door reuse
this exact machinery later, not a second system.

- **`BuildPiece`** (new `ScriptableObject`) — sibling to `CraftingRecipe`/
  `WishRecipe`: prefab, ingredients (reuses `CraftingRecipe.Ingredient`
  directly), trainedSkill, unlockTier, skillGain, groundReach.
- **`BuildSocket`** (new component) — typed anchor point
  (`SocketType.FoundationEdge` is the only one used yet; `WallBottom`/
  `WallTop`/`WallSide`/`PoleTop` are named ahead of time so the enum
  doesn't need a second pass), `IsCompatibleWith` for pairing, `Occupied`
  flag so a used socket can't be double-claimed.
- **`PlayerBuilding`** (new component) — the placement state machine.
  Every frame while a piece is armed: raycast for a nearby unoccupied
  compatible socket first (edge-snap, position+rotation both implied,
  one click confirms); otherwise a free-placement ghost follows the
  raycast hit point. **Left Mouse Button** places/confirms, **scroll
  wheel** rotates during the free-placement pending step — the exact
  Valheim/Rust/Raft-borrowed scheme from the ideation, not mouse
  movement (which is already camera look in this game).
- **`BuildScreen`** (new tab, `PlayerMenuScreen`) — same select/arm shape
  as `MagicScreen`, but **unlike Magic, fully visible on purpose**: shows
  ingredient costs, skill-gate state, and a live ghost preview in the
  world. Building is a deliberate, learnable system, not a hidden one.
- **`Foundation.prefab`** — 5m×5m flat slab (collider matches), 4
  `BuildSocket`s at the mid-edges facing outward. **Scoped down from the
  full design**: no support-column/stilt visual yet (the design doc's
  "buried block vs. stilted platform" question is still open) — a second
  foundation still correctly inherits the first's exact top height when
  snapped, and the 5m ground-reach tolerance is checked before allowing
  a *snapped* placement, but the free-placement case (nothing to snap
  to) always matches the raycast hit exactly, so there's no visible
  pedestal to get wrong yet.
- **`TwigFoundationPiece.asset`** — 6 Stick + 3 Rope, Woodworking-trained
  (matches the existing Bow precedent: wood-defining material trains
  Woodworking even with Rope also consumed), Crude unlock (always
  available).

**Real gotcha avoided, not hit this time:** `PlayerMenuScreen`'s new
`[RequireComponent(typeof(BuildScreen))]` (and `BuildScreen`'s own
requirement of `PlayerBuilding`) meant Unity auto-created both the
moment the scene loaded, same as the `MagicScreen`/`PlayerMagic`
incident in v0.1.148-dev — but both new components already had
`[DisallowMultipleComponent]` from the start and the setup script used
`GetComponent ?? AddComponent` throughout, so no duplicates landed this
time. Verified by reading back the saved scene YAML for exactly one of
each.

Verified via a full batch-mode compile check and by reading back
`Foundation.prefab` (4 sockets, correct `socketType`) and
`TwigFoundationPiece.asset` (both ingredients, correct guids) directly
rather than trusting the batch log alone.

**Known gaps, flagged not hidden:** no support-column/stilt visual;
mixed-material structures, Pole/Wall/Door, structural-integrity
requirements beyond "a socket exists," and territory restrictions all
remain exactly as open as the design doc already says.

### Building System: own tab + Left Mouse Button/scroll-wheel placement (doc-only, no version bump)

Follow-on to the Building System ideation above, same session. Ben: "we
will need to add a building tab to our crafting area. it may be its own
tab," then "can we borrow the mechanics from another similar game?" —
two more real decisions added to `docs/design-brief.md`'s Building
System section:

- **Own tab, not folded into Crafting** — same reasoning that kept Magic
  out of the Crafting tab: neither wishes nor building pieces resolve
  via a click-Craft-into-inventory button, both happen out in the world.
  A Build tab lists unlocked pieces and lets the player select which one
  is armed, same select/active shape `MagicScreen` already has — but
  **unlike Magic, Building gets full UI support** (ghost preview,
  prompts, everything), since it's a deliberate learnable system, not a
  hidden one. Worth keeping the two visually distinct on purpose.
- **Placement input borrowed directly from Valheim/Rust/Raft's shared
  convention**: Left Mouse Button places and confirms, scroll wheel
  rotates in between. Not mouse movement — that's already camera look in
  this game, so it can't also drive rotation without fighting itself,
  which is exactly why those games use scroll/a dedicated key instead.
  Not R (reserved for hidden magic) or E (already overloaded). Left
  Mouse Button turned out to be genuinely unbound today — it did nothing
  since punch-to-break was retired — so this is a clean reuse, not a
  displaced binding.

No code written — pure design.

### Building System designed — Foundation/Pole/Wall/Door, socket-based placement (doc-only, no version bump)

Ideation session on Phase 1's last untouched item, "Basic building" —
Ben noticed Rope and Sticks already exist as real items and asked to
explore a "twig" building tier: "we shouldn't give the 'Use R' type
hint" energy but for construction — click to place, snap to edges.
Converged on a real, buildable shape. Full detail in `docs/design-brief.md`'s
new **Building System** section; summary:

- **Modular by shape, not material** — Foundation/Wall/Door (and later
  Floor/Ceiling/Window/Roof) each define a fixed shape+socket contract
  once; material (Twig now, presumably Plank/Rock/Metal later) is a
  separate layered axis, same "orthogonal" relationship the ore family
  already has between metal type and CraftTier. Building material tiers
  ride the *existing* Crafting pipeline's material web rather than
  inventing a new one — Plank/Rock/Metal building pieces can't exist
  before their own material refinement chain does.
- **Two placement flows**: free (click-drop, release-to-rotate,
  click-to-confirm) for anything with nothing to snap to; one-click
  socket-snap for anything with a compatible edge in range — position
  and rotation are both implied by the socket in that case.
- **Foundation** — 5m×5m, reaches up to 5m downward from the aimed point
  (top-anchored, not center) to level across moderate terrain; a
  second panel snapped to a first inherits its exact top height rather
  than re-raycasting, which is the actual leveling mechanism.
- **Pole** — up to 10m reach, manually placed ahead of a Foundation when
  5m isn't enough (cliffs, water), exposes its own top socket so it's
  usable standalone too. No pole-to-pole stacking; unreachable terrain
  just fails placement, no escalation path.
- **Wall** — 5m wide × 3m high, one segment per Foundation edge exactly.
  Height deliberately decoupled from Foundation's 5m (a burial-depth
  tolerance, not a room-height statement).
- **Door** — its own full piece, socket-compatible with the same slot a
  Wall would occupy — a swap, not a runtime cutout.

**Explicitly still open, written into the doc rather than assumed:**
Foundation's visual (buried block vs. stilted platform), whether mixed-
material structures are allowed, Floor/Ceiling/Window/Roof shapes,
structural-support requirements beyond "a socket exists," where building
is allowed once territory/multiplayer exist, and exact material costs.

No code written this session — pure design, same status the Magic System
had before its own first implementation pass.

### MVP progress re-check (doc-only, no version bump)

Ben: "how are we doing on our mvp progress" — updated the "MVP Progress
Check-In" section in `docs/design-brief.md` (originally written earlier
the same session, before the interaction-model rebuild and the whole
Magic System) rather than re-answering from scratch.

- **Magic lineage assignment + early-tier ability use moves from
  not-built to built** — the single real status change. Three of four
  lineages (Elemental, Kinetic, Restoration) now have one genuinely
  working wish each; Illusion is still empty, so this is "built," not
  "complete."
- **Loot & gathering's interaction model was rebuilt** (`IPunchable`
  retired, skill-tiered hold-and-release) — doesn't change its
  built/not-built status, already counted as built, but flagged as a
  real mechanical change, not just polish.
- **Revised tally: 7 of Phase 1's 11 items built, 4 entirely unstarted**
  (encumbrance, building, combat/first aid, NPCs) — was 6/11 and 5
  unstarted at the last check-in.

No gameplay code touched.

### v0.1.155-dev — Magic gets zero UI hints, by design: "something people play with in order to explore it"

Ben, from a screenshot of "Pick up Backpack    Wish it would move (3s)"
showing simultaneously: "let's not share the 'wish' on the r at all. I
want this to be something people play with in order to explore it." A
real design stance, not just removing a redundant label — magic should
be discovered through experimentation, not explained on screen.

- **`PlayerInteraction.OnGUI` no longer shows any wish prompt text or
  progress bar at all.** `ResolveWishTarget`/`HandleWish` are completely
  unchanged — holding R still fills progress, still rolls success/
  failure, still spends Will and trains skills exactly as before. Only
  the player-facing hint is gone; the only feedback now is the world
  itself reacting (a campfire lighting, an object sliding, health
  climbing) or not.
- **Removed the R entry from `GameMenuScreen.ControlsList`** (the `` ` ``
  Game Menu's Controls tab) too — leaving an explicit "R: cast a wish"
  reference there would undercut the same goal for anyone who checks
  Controls, which is a normal, non-spoiler-breaking thing players do
  early. E and F keep their existing prompts/entries; this is specifically
  about hiding magic, not interaction in general.

Verified via a full batch-mode compile check.

### v0.1.154-dev — Fixed: R wish prompts always showed a "[R]" hint, even alone

Ben, from a screenshot of "[R] Heal Self (3s)" showing while looking at
plain grass with nothing else active: "we shouldn't give the 'Use R'
type hint... for any skill." Real bug, not a style nitpick — the
disambiguation logic (`bool multiple = ... || wishText != null`) bracketed
R the moment *any* wish was present at all, regardless of whether
anything else was actually competing for the same prompt line, unlike E
(which only ever got bracketed when F was also active).

- `PlayerInteraction.OnGUI` rewritten: E/F keep their existing bracketed
  disambiguation between each other, unchanged. The wish prompt is now
  always appended plain, with no `[R]` prefix, whether it's alone or
  (hypothetically, not shipped anywhere) alongside E/F.

Verified via a full batch-mode compile check.

### v0.1.153-dev — Restoration's Heal Self: the first Unconditional wish

Ben: "let's add a 'heal self' skill that give 10 health over 30 seconds.
add to restoration skill set." First real use of the `Unconditional`
targeting mode added in v0.1.152-dev specifically for a wish like this —
no world object involved at all, just Will and skill.

- **`PlayerVitals` gained heal-over-time state** — `StartHealOverTime
  (amount, duration)` computes a flat rate and ticks it down each frame,
  same shape as `bodyTemperature`'s drift-toward-neutral. Re-casting
  while one's already active replaces it outright rather than stacking
  or extending (simplest behavior, no spec given otherwise).
- **New `HealSelfWish.asset`** — Restoration, Crude unlock,
  `targeting = Unconditional`, same 60/40 Will split as Spark/Push (no
  different numbers specified, kept consistent rather than inventing a
  third placeholder pair).
- **`PlayerInteraction` special-cases Heal Self** in its Unconditional
  dispatch branch (`currentWish == healSelfWish` → `StartHealOverTime
  (10, 30)`), same "fine for one wish, revisit if a second Unconditional
  wish needs a real effect-dispatch abstraction" placeholder status as
  `pushForce`'s handling of Push.
- Added to `PlayerMagic.allWishes` (now 3 entries total) — Restoration
  finally has a wish of its own, joining Elemental (Spark) and Kinetic
  (Push); Illusion is still empty.
- **No aiming required** — Unconditional wishes don't raycast at all
  (see `ResolveWishTarget`'s `Unconditional` branch, v0.1.152-dev); a
  Restoration character can hold R to heal anywhere, looking at anything.

Verified via a full batch-mode compile check and by reading back the
saved scene/asset YAML to confirm `targeting: 2` (Unconditional) on the
new asset and real (non-`fileID: 0`) references throughout.

### v0.1.152-dev — "Default skill" selection: the player picks which wish R attempts

Ben: "let's consider the thought of being able to set a default skill.
for example, I could set 'push' as default, and even if I was aiming at
a fire, it would try to push if I had that skill... setting the default
skill to 'fireball' means you could shoot a fireball anytime you had
enough will." Real problem this solves: once a lineage has more than one
wish (Fireball alongside Spark, per the design brief's own Elemental
ladder sketch), the old model — R does whatever the crosshair happens to
offer — has no way to choose between them, and no path at all for a wish
that needs no physical target (Fireball flying at nothing in particular).

- **`WishRecipe` gained a `WishTargeting` enum** (`SpecificObject` —
  needs an `IWishTarget` offering this exact wish, the default, matches
  Spark; `AnyRigidbody` — matches Push; `Unconditional` — no target
  needed at all, gated purely on lineage/skill/Will, not used by any
  shipped wish yet but the dispatch path exists for when Fireball lands).
- **`PlayerMagic` is now the single source of truth for the wish list**
  (`allWishes`, moved off both `MagicScreen` and `PlayerInteraction`,
  which each held their own separate references before — only worked
  because there were exactly two wishes total). Added `KnownWishes`
  (filtered by lineage), `SelectedWish`, and `SelectWish(wish)`.
  Auto-selects the first known wish in `Awake` so single-wish gameplay
  keeps working with zero menu trips — explicit selection only matters
  once a lineage actually has two.
- **`MagicScreen` gained a real action** — a Select/Active button per
  known wish, previously pure read-only reference.
- **`PlayerInteraction`'s `ResolveWishTarget` rewritten to dispatch off
  `magic.SelectedWish.targeting`** instead of "try IWishTarget, fall back
  to Rigidbody" — it now only ever checks the one targeting mode the
  selected wish actually needs. `HandleWish`'s completion routing
  branches explicitly on targeting mode too, not on "is currentWishTarget
  null," so a future Unconditional wish doesn't misfire down the Push
  AddForce path.
- `PushWish.asset` set to `targeting = AnyRigidbody`; `SparkWish.asset`
  needed no change (`SpecificObject` is the default).

**Real ops hiccup, not a code bug:** the first batch-mode rewiring
attempt hung for 5+ minutes — a stale `bee_backend` process left over
from an earlier session was holding the project's compile lock, so the
new Unity instance sat blocked rather than failing fast the way "another
Unity instance is running" normally does. Diagnosed by reading the
partial log (`bee_backend: error: More than one copy of bee_backend
running... PID waiting`), killed the stuck process, reran clean.

Verified via a full batch-mode compile check, a grep for dangling
references to the removed `pushWish` field, and by reading back the
saved scene YAML to confirm `PlayerMagic.allWishes` holds both real
references and `PushWish.asset`'s `targeting` reads `1` (AnyRigidbody).

### v0.1.151-dev — All magic unified onto R; new IWishTarget interface

Ben: "let's change the spark and all magic to activate with r. we'll use
the mouse cursor to determine the target." Clarified on ask: no change to
the mouse/camera model itself — still look-based, same crosshair raycast
as everything else; "the cursor" just meant "wherever you're looking,"
not a literal free-moving pointer. Net change: Spark moves off E onto R,
joining Push, so all magic now shares one input.

- **New `IWishTarget` interface** — `Prompt`, `GetWish(PlayerMagic)`
  (returns null if this target has nothing for the given magic right now:
  wrong lineage, or e.g. an already-lit campfire), `OnWishComplete(player,
  succeeded)`. Distinct from `IInteractable`: every wish rides R, not E,
  and gates on `PlayerMagic`, not a tool.
- **`Campfire` converted from `IInteractable` to `IWishTarget`** — no
  longer part of the E/hold-to-gather system at all. Same effect
  (`SetLit`), just invoked via `OnWishComplete` instead of `Complete`.
- **`PlayerInteraction` gained a unified `ResolveWishTarget`/`HandleWish`
  pair**, replacing the Push-only version from v0.1.150-dev. Each frame:
  raycast for an `IWishTarget` first (a specific object like Campfire);
  if none, or its `GetWish` returns null, fall back to a plain
  `Rigidbody` for the generic Push case. Same hold-and-release shape
  either way — one shared progress counter, one shared green bar, one
  shared "[R] ..." prompt slot, whichever kind of target is in play.
- `GameMenuScreen.ControlsList`'s R entry generalized from
  "Kinetic: wish it would move" to "Wish at whatever you're looking at —
  Spark/Push/etc."

Verified via a full batch-mode compile check and a grep for dangling
references to the removed Push-only fields (`currentPushTarget`,
`CanPush`, `pushHoldProgress`) — none found.

### v0.1.150-dev — Kinetic's Push wish: a second, R-bound interaction channel

Ben: "I think we need to bind a new key to magic, like maybe r if not
used. that way we can use a kinetic 'push' skill to push the mid size
rock a short distance." Confirmed `R` was genuinely unused (grepped the
whole `Assets/Scripts/` tree) before binding it.

- **Deliberately a new channel, not IInteractable/E like Spark.** Spark
  targets one specific pre-flagged object (Campfire); Push needed to
  target *any* nearby Rigidbody the player picked ("any nearby
  Rigidbody-bearing chunk," Ben's call over a single dedicated pushable
  object), which doesn't fit IInteractable's "one wishable object" shape.
  Retrofitting every Rigidbody-bearing prefab in the game with a wish
  interface wasn't worth it for one wish — `PlayerInteraction` instead
  runs a second, independent raycast for `R`, generic against
  `GetComponentInParent<Rigidbody>()` rather than a specific interface.
- Same hold-and-release shape as E: hold R while aiming at a Rigidbody,
  a green bar fills (same `DrawHoldBar` visual, shared with E's), duration
  set by `PlayerSkills.GetHoldDuration(Kinetic)` — same skill-tiered
  curve as everything else. On completion, `PlayerMagic.TryWish` runs the
  same success/failure roll Spark uses (50%→90% by margin, 60/40 Will);
  on success, `Rigidbody.AddForce` shoves the target (`ForceMode.Impulse`,
  magnitude 6, placeholder/tunable) in the camera's forward direction.
- **New `PushWish.asset`** (Kinetic, Crude unlock, 60/40 Will, same
  numbers as Spark — no reason given yet to differ, kept consistent
  rather than inventing new placeholders). Added to `MagicScreen.allWishes`
  alongside Spark, so a Kinetic character's Magic tab now lists it.
- **Prompt only shown to a player who actually knows Kinetic** — a real,
  deliberate divergence from the tool-gated prompts elsewhere (Pickaxe
  requirement shows to everyone, since anyone could pick one up). Under
  today's single-starting-lineage rule a non-Kinetic character can never
  attempt Push at all, so showing the prompt to them would be dead,
  misleading UI rather than an honest "here's what you're missing" like
  the tool prompts are.
- `GameMenuScreen.ControlsList` updated with the new R binding, noted as
  Kinetic-only.

Verified via a full batch-mode compile check and by reading back the
saved scene YAML to confirm both `pushWish` (on `PlayerInteraction`) and
the 2-element `allWishes` array (on `MagicScreen`) hold real references,
not `fileID: 0` — no repeat of the earlier stale-reference gotcha this
time, since assets were loaded after `OpenScene` from the start.

**Known gap, not fixed here:** if a player somehow holds both E and R at
once, both progress bars share the same screen position/texture — an
unlikely edge case, not engineered around.

### v0.1.149-dev — Spark gets a real success/failure roll; Will costs and regen tuned

Ben tested v0.1.148-dev live, confirmed Spark works end-to-end, then gave
real tuning numbers: "at a successful use of the wish, will should drop
by 60 points. it should regen 1 point every 5 seconds. a failure should
cost 40 points."

- **`WishRecipe.willCost` split into `successWillCost` (60) and
  `failureWillCost` (40)** — different outcomes now cost different
  amounts, which meant a wish attempt needed an actual outcome to
  determine first.
- **`PlayerMagic.TryWish` gained a binary success/failure roll**, same
  interpolated-by-skill-margin shape as `PlayerCrafting`'s existing
  chance-of-creation system (`RollOutcome`) — 50% success chance right at
  the unlock threshold, rising to 90% once ~20 skill points past it.
  Either outcome still trains the skill and spends Will (a failed attempt
  isn't a non-attempt); only success grows Will's max and lets `Campfire`
  actually light. This is closer to the ideation session's original
  "with luck, it would actually start" pitch than the "weakest-link
  against fuel tier" idea design-brief.md had settled on — **that idea
  was never built**, flagged directly in the doc rather than left to look
  like both are simultaneously true.
- **`CanAttempt` gates on `successWillCost`, not `failureWillCost`** —
  deliberate: success costs more, so requiring only the cheaper amount
  could let a roll succeed and then be unable to actually pay for it.
- Added a failure message ("The wish didn't take — Spark fizzled."),
  same stacking convention as `PlayerSkills`'/`PlayerCrafting`'s own
  messages (`y=150`, below both) — a held-and-completed action that does
  nothing with zero feedback was exactly the kind of silent-failure gap
  this project has repeatedly fixed elsewhere (see the chance-of-creation
  system's own history).
- **Will regen changed from a 4/s placeholder to 1 point per 5 seconds**
  (`0.2f`), per Ben's number.
- `SparkWish.asset` verified to actually deserialize the new fields
  correctly (`successWillCost=60 failureWillCost=40`, confirmed via a
  throwaway batch-mode script's log output, not just assumed) and
  resaved to drop the now-dead `willCost: 10` YAML.

Verified via a full batch-mode compile check, throwaway scripts deleted
after. Docs updated: `docs/design-brief.md`'s Magic System section now
flags the weakest-link-vs-actual-roll divergence explicitly.

### v0.1.148-dev — Magic System: first real slice — Will, starting lineage, and Spark lighting a Campfire

Ben: "let's build the magic system" — the first real implementation off
the same-day ideation session (see the doc-only entries below for the
design conversation). Scoped deliberately: build the full skeleton plus
one genuinely working wish, not all four lineages at once.

- **Will**, a real sixth `PlayerVitals` field — starts at 100, regens
  passively like Stamina (no drain-state needed, since Will is spent as
  one lump per completed wish, not continuously). `ConsumeWill`/
  `GrowMaxWill` added; `GrowMaxWill` raises the ceiling *and* tops up
  current Will, so growth reads as a real gain, not just cap-raising.
  Added to `VitalsBarHUD` as a new third row (single full-width bar,
  scaled against its own live `MaxWill`, not the other four bars' fixed
  150% scale — Will's ceiling grows, so a fixed scale would read as
  permanently-near-full over time).
- **`SkillCategory.Magic`** added (`SkillDefinition.cs`) — the four
  lineages' home in the Skills tab, alongside Gathering/CraftingDiscipline/
  Combat. Four new `SkillDefinition` assets: `Elemental`, `Illusion`,
  `Kinetic`, `Restoration`.
- **`PlayerMagic`** (new component) — assigns one random starting lineage
  per character at spawn (keeps Pillar 7's "no lineage-less players"),
  exposes `IsLineageKnown`/`CanAttempt`/`TryWish`. Learning additional
  lineages later is explicitly **not built** — rides the Phase 2
  skill-books mechanic, which doesn't exist yet, so every character only
  ever knows their one starting lineage for now.
- **`WishRecipe`** (new `ScriptableObject`) — sibling to `CraftingRecipe`:
  `lineage`, `unlockTier` (reuses `CraftTierScale.SkillRequirement`
  directly), `willCost`, `skillGain`. No material-tier weakest-link input
  on the data class itself — that's decided per wish target instead (see
  Campfire below).
- **Spark**, the first real wish, and **`Campfire`**, its target: an
  unlit campfire (primitive logs + kindling + a `Light`, same
  "primitives first" precedent Backpack set) that lights when a player
  who knows Elemental holds E through a skill-tiered duration (same
  `PlayerSkills.GetHoldDuration` mechanic gathering uses) with enough
  Will. **Simplification from the design doc, flagged not hidden:**
  lighting is unconditional once the gates pass — there's no fuel-tier
  input to cap quality against, so the "weakest-link vs. tinder tier"
  idea from the ideation session isn't actually implemented here.
- **New `Magic` tab** (`MagicScreen`, `PlayerMenuScreen`) — read-only
  reference (lineage known, Will current/max, known wishes with
  locked/unlocked state), not a clickable list, since wishes fire from
  the in-world E-hold prompt on their target, not a menu button.
- Placed one Campfire in `TestScene.unity` at `(-4, 0.3, -2)`.

**Real bug hit and fixed while wiring the scene:** adding
`[RequireComponent(typeof(MagicScreen))]` to `PlayerMenuScreen` meant
Unity auto-created an *empty* `MagicScreen`/`PlayerMagic` on `Player` the
moment the scene loaded — **before** the setup script's own
`AddComponent` calls ran, leaving two of each (the auto-created empty
one and the script's own). Fixed two ways: added `[DisallowMultipleComponent]`
to both (matching `PlayerVitals`/`PlayerSkills`'s existing convention,
should have been there from the start) and rewrote the wiring script to
`GetComponent ?? AddComponent` instead of assuming a fresh add. Also
re-hit this project's own documented gotcha in the process — object
references fetched *before* `EditorSceneManager.OpenScene()` go stale
(`fileID: 0`) once the scene opens; fixed by loading the lineage/wish
assets after opening the scene, not before.

Verified via a full batch-mode compile check (throwaway
`Assets/Editor/CompileCheck.cs`, deleted after) and by reading back the
saved scene YAML to confirm exactly one `PlayerMagic`/`MagicScreen` each
with real (non-`fileID: 0`) references, not just trusting the batch log.

**Known gaps, not fixed here:** Fireball (needs combat), scrolls and
learnable second lineages (Phase 2, ride skill-books), Illusion/Kinetic/
Restoration's own wishes, and Spark's missing weakest-link fuel-tier
input (see above).

### v0.1.147-dev — Punch-to-break retired: gathering/chopping now hold-and-release, skill-tiered

Ben: "let's build this pig!" — implements the interaction-model ideation
from this same session (see the doc-only entries below/above for the
design conversation this comes from). `IPunchable` is gone entirely.

- **`IPunchable` deleted outright.** `ResourceNode` (Rock Node, Boulder,
  the full Copper/Iron/Silver/Gold/Platinum Ore family) and `ChoppableTree`
  now implement `IInteractable` instead — same hold-E-to-fill/release-to-
  cancel model every other interactable already used, just with a real
  non-zero duration for the first time. `hitsToBreak`/`hitsToChop` counter
  fields removed; `OnPunch` became `Complete`, called once when the hold
  finishes rather than once per punch.
- **`IInteractable.HoldDuration` (a flat per-item constant, silently unused
  by anything until now) became `GetHoldDuration(GameObject player)`** —
  needs the acting player because duration is skill-dependent. All ~12
  always-instant implementers (Pickup, Backpack, Belt, Canteen, Coin,
  Lockbox, NavigationComputer, PersonalHealthMonitor, MiningFaceShield,
  Sunglasses, WaterSource, BankBox) got the mechanical one-line signature
  update, unchanged behavior (still instant).
- **Duration is skill-tiered, low tier takes longest**: `CraftTierScale`
  gained `HoldDuration(CraftTier)` (Crude 3s → Masterwork 0.5s — placeholder
  numbers, same "tune by playtesting" status as every other value in that
  table) and `TierForSkillLevel(float)` (the inverse of the existing
  `SkillRequirement`, walks the same 0/10/25/50/100 thresholds). `PlayerSkills`
  gained `GetHoldDuration(SkillDefinition)` tying the two together — a
  node/tree reads the player's live skill level, buckets it into a tier,
  looks up that tier's duration. No new per-instance scene data needed.
- **Real green progress bar added** to `PlayerInteraction`'s crosshair HUD,
  under the existing countdown-seconds text — only draws while a hold is
  actually filling.
- **Scoped to gathering/chopping only**, not every interactable — Pickup,
  equip, drink, bank, etc. all stay instant, matching "replaces punch-to-hit"
  rather than "everything now takes time." The Crafting screen's own
  instant "Craft" button is a deliberate **fast-follow, not done here** —
  different UI surface (menu-driven, not world-raycast), needs its own
  progress/cancel affordance.
- Updated `GameMenuScreen.ControlsList`: removed the dead "Left Mouse
  Button — Punch" entry, folded the hold behavior into the existing "E" row.
- Verified via a full batch-mode compile check (throwaway
  `Assets/Editor/CompileCheck.cs`, deleted after) — clean, no `CS####`
  errors.

**Known gaps, not fixed here:** tool-tier doesn't yet speed this up on top
of skill tier (the pipeline's "Tool-quality effects" bullet promises this,
not implemented); the Crafting screen's Craft button (see above); Escape
has no explicit cancel wiring (release already cancels, judged sufficient
per Ben's call during ideation).

### Magic System fully fleshed out — Will, tiered wishes, learnable lineages, scrolls (doc-only, no version bump)

Ideation session with Ben on the previously-thin Magic System placeholder,
sparked by his original "wish it would..." pitch (emote a wish, luck-based
success). Converged on a real, buildable shape reusing crafting's existing
mechanics rather than inventing parallel ones — see `docs/design-brief.md`'s
Magic System section for the full writeup. Summary of what got decided:

- **Wishes** trigger off pre-flagged contextual moments (same
  `IInteractable`/`ISecondaryInteractable` prompt pattern already shipped),
  not free-form intent parsing.
- **Will** — new sixth survival vital, added to Character Creation & Stats.
  Starts full like the other five; unlike them, its max pool grows through
  use rather than staying fixed. One shared pool per character.
- **Wishes are tiered `CraftingRecipe`-style recipes**, reusing two rules
  crafting already has: recipe-unlock gating (skill threshold before a wish
  is attemptable) and weakest-link output quality (capped by both caster
  skill tier and the tier of whatever material is present). Sketched an
  illustrative Elemental ladder (Spark → Fireball → forge-grade Spark) — the
  other three lineages' ladders are still unsketched, flagged Still Open.
- **Lineages are learnable, not a lifetime lock** — free starting lineage
  (keeps Pillar 7's "no lineage-less players"), any other lineage trainable
  later exactly like any of the other 16 skills in the game, no cap, pure
  player choice. Rides the existing Phase 2 skill-books mechanic as its
  unlock vehicle — **this piece is Phase 2 scope**, not Phase 1.
  Cross-referenced from the Phase 2 skill-books/magazines bullet.
- **Two scroll paths**, both Phase 2: found scrolls roll their lineage+wish
  **on read**, not on spawn (keeps the luck flavor genuine rather than being
  ordinary hidden loot); scribed scrolls are deterministic, gated on a
  dedicated Scribing skill *and* the source wish at Normal tier, and grant
  only the unlock — never skill progress — so buying a scroll never skips
  training.
- Updated Pillar 7 and the Character Creation & Stats "Magic lineage" bullet
  to match (randomized-at-start, not randomized-forever).
- **UI impact assessed against the real current code**, not imagined: a new
  `Magic` tab/`MagicScreen` on `PlayerMenuScreen` (read-only reference list,
  same shape as Skills — wishes fire from in-world prompts, not a Craft
  button); a new `Magic` value on the `SkillCategory` enum; Scribing needs
  no new UI at all (rides the existing Crafting tab as an ordinary
  discipline + recipes); `InventoryScreen` needs a new "Read" per-item
  action for Unidentified Scrolls; `VitalsBarHUD`'s hardcoded 2×2 grid has
  no slot for Will yet (same pre-existing gap Body Temperature already has).

**Still open, written into the doc rather than assumed:** the other three
lineages' wish ladders; whether the free starting lineage keeps any
permanent edge; whether Scribing should be its own skill or shared with the
Phase 2 crafting-manuals idea; Will's regen rule and whether Scribing itself
costs Will/materials; and whether the wish-trigger emote is a literal
chat/emote-wheel action or just reuses the E/F-interact pattern.

No gameplay code touched — pure design-doc session, no version bump per
`CLAUDE.md`'s doc-only-commit rule.

### Design brief comparison pass — MVP progress check-in (doc-only, no version bump)

Ben: "let's update, and do a comparison of our mvp doc again," after the long
item/model/icon audit stretch below. Read `docs/design-brief.md` end to end
and checked its claims directly against `Assets/Scripts/`, `Assets/Data/`, and
`TestScene.unity` rather than trusting the doc's own prior "shipped"/"still
open" notes.

- Added a new **"MVP Progress Check-In (2026-08-08)"** section rolling up
  Phase 1's 11 items against real code: 6 genuinely built (skill progression,
  food/water, loot & gathering, crafting-quality content, storage, skills UI),
  5 entirely unstarted (encumbrance, building, combat/first aid, magic,
  hireable NPCs). Net finding: tonight's very large volume of work was almost
  entirely deepening the two already-started pillars (loot & gathering,
  crafting-tier content), not starting a new Phase 1 pillar.
- **Found and fixed a real doc/code mismatch**: the design brief declared the
  `Mining` skill split from `Gathering` "decided... no longer open" back on
  2026-08-05, but no `Mining.asset` `SkillDefinition` was ever created — every
  `ResourceNode` in the scene, including the now-fully-shipped Silver/Gold/
  Platinum ore family, still trains `Gathering`. Flagged directly in the
  Skills section rather than left implied.
- Marked the Silver/Gold/Platinum hidden-ore + Mining Face Shield mechanic as
  **shipped** (it was written as a future plan; it's been real and working
  since `v0.1.60-dev`, confirmed by Ben's own playtest) — while also noting
  two real gaps: the Mining-tier-4 shield-bypass has no code to check (no
  Mining skill exists yet), and the Shield's own model is still the original
  placeholder Cylinder despite everything else in its recipe chain being real.
- Corrected a stale reference to the deleted Secret Message Wall (removed
  `v0.1.126-dev`) in the same ore/shield paragraph.
- Updated the Wood and Textiles material-web bullets to describe what
  actually shipped (Tree→Log→Plank has no Twigs/Saw step; Cloth/Fiber have
  real models now but no recipe or gather source yet) rather than only the
  original plan.
- Updated the "5 items without a defining discipline" note — Canteen is now a
  fully real item (model/fill/tint), not just a placeholder, even though it
  still trains no skill per that rule.

No gameplay code touched — `TEST_FEATURE_PLAN.md` unchanged, no version bump
per `CLAUDE.md`'s doc-only-commit rule.

### v0.1.146-dev — Fiber gets a real model (Grass Wispy by Quaternius)

Ben downloaded "Grass Wispy by Quaternius" (Poly Pizza, public domain)
by hand — last of the two raw materials off the audit list.

- Imported as `Assets/Models/GrassWispy_Quaternius.glb`, built
  `Assets/Prefabs/FiberPickup.prefab` (hardcoded item,
  `ContinuousDynamic`, measured bounds `0.23x0.25x0.24`), wired to
  `Fiber.asset.worldPickupPrefab` for the first time. Icon +
  previewIcon baked via `IconBaker` — reads clearly as a wispy tuft of
  grass/fiber strands.
- **Credits**: added to `Assets/Models/THIRD_PARTY_CREDITS.md` and the
  live Credits tab — `"Grass Wispy by Quaternius [Public Domain] via
  Poly Pizza"` — full treatment despite being public domain, same
  precedent as Wood Planks and Pickaxe.
- **Cloth and Fiber are now both done** — the last two items in the
  "raw materials" category from tonight's original audit.

### v0.1.145-dev — New "Woven Grass Cloth" item — second material path, per the tint experiment

Ben: "let's duplicate the cloth model and call it 'woven grass cloth'.
then run the standard path on it for tiers." Turns the v0.1.144-dev
tint evaluation into a real, permanent second item rather than a
throwaway test render.

- New `WovenGrassClothItem.asset` (itemName "Woven Grass Cloth",
  maxStack 20) — standalone, not part of any CraftTier ladder, same as
  `Cloth` itself.
- New `Assets/Data/WovenGrassCloth.mat` — a clone of Cloth's actual
  in-game material with `baseColorFactor`/`_BaseColor`/`_Color` tinted
  green, same static-variant pattern as the Copper/Iron/Silver/Gold/
  Platinum ore family (one shared mesh, separate tinted `.mat` assets)
  rather than Canteen's runtime-script approach — this is a
  permanently-different item, not one object whose state changes live.
- New `WovenGrassClothPickup.prefab` — reuses `PaleCloth.glb`'s mesh
  with the new green material, same measured-fit discipline as every
  other pickup (`0.25x0.20x0.28`, `ContinuousDynamic`, hardcoded item).
- Icon + previewIcon baked via `IconBaker`.
- **No recipe yet** — this is the material existing for a future
  clothing system to consume, not a craftable item today. Visually
  it's still the same smooth-folded cloth shape tinted green (the
  known limitation from the v0.1.144-dev evaluation — reads as green
  cloth, not distinctly "woven grass"), accepted as good enough for now
  per Ben's call.

### v0.1.144-dev — Cloth gets a real model (pale folded cloth); tint trick confirmed reusable

Ben wanted to ideate on Cloth/Fiber's visual treatment before building
anything — landed on: generate a pale cloth, confirm the Canteen-style
runtime tint trick generalizes to it (for potential future dyed/colored
cloth variants), then just ship the pale version since this was mostly
an evaluation pass.

- Generated via Tripo3D's API (`"a small folded square piece of cloth,
  pale off-white plain-woven fabric, visible woven texture and fold
  creases, isolated on a plain background, no person, no model,
  low-poly game asset"`, 20 credits) — clean on the first attempt.
- Imported as `Assets/Models/PaleCloth.glb`, built
  `Assets/Prefabs/ClothPickup.prefab` (hardcoded item, `ContinuousDynamic`,
  measured bounds `0.25x0.20x0.28`), wired to `Cloth.asset.
  worldPickupPrefab` for the first time. Icon + previewIcon baked via
  `IconBaker`.
- **Confirmed the material-tint technique generalizes**: cloned the
  material, set `baseColorFactor` to a green tint, rendered a
  throwaway evaluation preview (not committed to any asset) to check
  whether a tinted "woven grass cloth" variant would read well.
  Mechanically it worked identically to the Canteen fix — but visually
  it just read as a solid green cushion, not a grass texture, since
  tinting multiplies against the existing (smooth-folded, not woven-
  grain) albedo rather than adding new texture detail. **Conclusion**:
  the tint trick is solid for simple flat-color variants of the same
  base cloth (same pattern as the Copper/Iron/Silver/Gold/Platinum ore
  family sharing one rock mesh), but a genuinely "woven grass" look
  would need its own separately-generated texture, not a tint on this
  model. Not pursued further this session — pale cloth ships as-is.

### v0.1.143-dev — Hammer CraftTier ladder gets a real model (AI-generated stone hammer)

Ben: "I don't see a decent stone hammer, so let's go the api route" —
third tool ladder off the backlog, via Tripo3D this time instead of a
hand-downloaded model.

- Generated via Tripo3D's API (`"a crude stone hammer with a wooden
  handle, primitive tool, rough grey stone head bound to the handle
  with cord, isolated on a plain background, no person, no model,
  low-poly game asset"`, 20 credits) — clean on the first attempt, no
  500s, no timeout. Reads clearly as a stone-headed hammer bound to a
  wooden handle with cord, matching the game's established "crude
  primitive tool" aesthetic (same family as Crude Stone Knife).
- Imported as `Assets/Models/StoneHammer.glb`. Same 5-tier build as
  Pickaxe/Axe: first tier measured fresh (target length `0.6`, final
  bounds `0.60x0.55x0.59` — chunkier than the bladed tools, as expected
  for a hammer head), the other 4 reuse that exact fit.
- Icons + previewIcons baked for all 5 via `IconBaker`.
- No credits needed — Tripo3D API content has its own no-attribution
  commercial license (see `Tools/Tripo3D/README.md`), unlike the
  CC-BY/public-domain downloads used for Pickaxe and Axe.
- Same note as the other tool ladders: these 5 `ItemDefinition`s are
  referenced by `ResourceNode.requiredTools` wherever Hammer is gated
  (Lockbox, per `BUGS_AND_ENHANCEMENTS.md`'s Belt entry) — only model/
  icon/`worldPickupPrefab` touched, guids untouched.

### v0.1.142-dev — Axe CraftTier ladder gets a real model (Low Poly Axe by suerozcelik)

Ben downloaded "Low Poly Axe by suerozcelik" (Poly Pizza, CC-BY) by
hand — second tool ladder off the backlog, same shape as Pickaxe.

- Imported as `Assets/Models/Axe_suerozcelik.fbx` — **first `.fbx`
  import this session** (everything before was `.glb`). Unity's native
  FBX importer handled it directly with no extra steps; materials/
  colors came through intact with no separate texture files needed
  (confirmed by eye before baking the rest — reads clearly as a
  wood-handled axe with a metal head).
- Built 5 new prefabs from scratch (`CrudeAxePickup` through
  `MasterworkAxePickup`), same pattern as Pickaxe: first tier measured
  fresh (target length `0.6`, final bounds `0.25x0.60x0.04`), the other
  4 reuse that exact fit.
- Icons + previewIcons baked for all 5 via `IconBaker`.
- **Credits — CC-BY, attribution required**: added to
  `Assets/Models/THIRD_PARTY_CREDITS.md` and the live Credits tab —
  `"Low Poly Axe by suerozcelik [CC-BY] via Poly Pizza"`.
- Same note as Pickaxe: these 5 `ItemDefinition`s are referenced by
  every `ResourceNode.requiredTools` array gated on Axe (Tree, Log) —
  only model/icon/`worldPickupPrefab` touched, guids untouched.

### v0.1.141-dev — Pickaxe CraftTier ladder gets a real model (Pickaxe by CreativeTrio)

Ben downloaded "Pickaxe by CreativeTrio" (Poly Pizza, public domain) by
hand — first tool ladder tackled from the remaining backlog, same
"wire one model to all 5 tiers" shape as Knife.

- Imported as `Assets/Models/Pickaxe_CreativeTrio.glb`. Unlike Knife,
  no placeholder prefab existed at all for any Pickaxe tier — built 5
  new prefabs from scratch (`CrudePickaxePickup` through
  `MasterworkPickaxePickup`), each hardcoding its own tier's item.
  First tier measured fresh (uniform-scaled to a `0.6` target length,
  matching Stick's own held-tool scale — final bounds
  `0.39x0.07x0.60`), the other 4 reuse that exact fit so all 5 render
  identically instead of accumulating per-bake variance.
- Icons + previewIcons baked for all 5 via `IconBaker`.
- **Credits**: added to `Assets/Models/THIRD_PARTY_CREDITS.md` and the
  live Credits tab — `"Pickaxe by CreativeTrio [Public Domain] via
  Poly Pizza"` — full treatment despite being public domain, matching
  the precedent set for Wood Planks by Quaternius.
- **Note:** these 5 Pickaxe `ItemDefinition`s are also referenced by
  every `ResourceNode.requiredTools` array in the game (Copper/Iron/
  Silver/Gold/Platinum Ore Nodes, Boulder, Rock Node) — only the model/
  icon/`worldPickupPrefab` fields were touched, guids and all existing
  references untouched, confirmed nothing there needed updating.

### v0.1.140-dev — Removed the redundant "Fiber Belt" item

Ben, after reviewing what was actually left to build for it: "I think
the fiber belt is the grass belt already. so we can likely remove all
references for it." The Normal-tier `Fiber Belt` (`BeltItem.asset`) was
the original pre-ladder "Belt" item, renamed in v0.1.79-dev when
`Crude Fiber Belt` shipped as the ladder's first real tier — it had
never been given its own model/icon, was still a bare Cube placeholder
standalone GameObject in the scene (not even a real `PrefabInstance`),
and Rudimentary/Fine/Masterwork Fiber Belt were never built either.
Redundant with `Crude Fiber Belt`, which already has real content.
Confirmed via guid search before deleting (same discipline as every
other removal tonight) — nothing referenced `BeltItem.asset` except its
own `.meta` and the scene object. Deleted `BeltItem.asset` and the
scene's standalone "Belt" GameObject at `(-2, 0.3, 1.5)`.
`BUGS_AND_ENHANCEMENTS.md`'s Belt-ladder entry updated to note the
removal; `TEST_FEATURE_PLAN.md`'s regression check referencing the old
found Belt updated to say it's gone, not to expect it.

### v0.1.139-dev — Berry gets a real model (Strawberries by Jarlan Perez)

Ben downloaded "Strawberries by Jarlan Perez" (Poly Pizza, CC-BY) by
hand — last item off the double-gap list from tonight's audit.

- Imported as `Assets/Models/Strawberries_JarlanPerez.glb`, replacing
  `BerryPickup.prefab`'s placeholder Sphere. `ContinuousDynamic`
  confirmed/set, collider resized to the real measured bounds
  (`0.35x0.31x0.31`).
- **Found the same "standalone copy, not a real `PrefabInstance`" bug**
  on the scene's pre-placed "Berry Bush" (same class as Canteen in
  v0.1.128-dev and Backpack in v0.1.132-dev) — replaced with a real
  `PrefabInstance` at the same position so the model swap actually
  reaches it.
- Icon + previewIcon baked via `IconBaker`.
- **Credits — CC-BY, attribution required this time** (unlike Rock/Wood
  Planks by Quaternius, both public domain): added to
  `Assets/Models/THIRD_PARTY_CREDITS.md` and the live Credits tab
  (`GameMenuScreen.cs`) — `"Strawberries by Jarlan Perez [CC-BY] via
  Poly Pizza"`, exact text from the download popup.

### v0.1.138-dev — Crude Knife's model wired to the other 4 Knife tiers

Ben, after confirming Crude Knife already had the real Tripo3D model
and just needed the other tiers matched up to it — same shape as the
Backpack ladders: "let's wire up the crudeknife asset to the other 4
tiers and do the icon work."

- `RudimentaryKnife`/`Knife` (Normal)/`FineKnife`/`MasterworkKnife` all
  had real recipes already (`v0.1.69-dev`) but zero model/icon/
  `worldPickupPrefab` — same gap the Backpack ladder tiers were in
  before tonight.
- New prefabs (`RudimentaryKnifePickup`/`NormalKnifePickup`/
  `FineKnifePickup`/`MasterworkKnifePickup`), each hardcoding its own
  tier's item on `Pickup` (matching standard rules, even though — like
  `RockKnifePickup` itself — these are only ever used as a
  `worldPickupPrefab`, never a `chunkPrefab`). Rather than re-measuring
  the model fresh per tier, copied `RockKnifePickup.prefab`'s
  already-proven child scale/collider values directly, so all 5 tiers
  render pixel-identical instead of accumulating small per-bake
  variance.
- Icons + previewIcons baked for all 4 via `IconBaker` — confirmed
  identical bounds (`0.08x0.05x0.35`) to Crude Knife's own bake, proving
  the copied-fit approach worked exactly.

### v0.1.137-dev — Plank gets a real model (Wood Planks by Quaternius)

Ben downloaded "Wood Planks by Quaternius" (Poly Pizza, public domain)
by hand and asked for the full treatment: credits, model, icon, and a
scene spawn.

- Imported as `Assets/Models/WoodPlanks_Quaternius.glb`, replacing
  `PlankChunk.prefab`'s placeholder Cube — this is the real chunk
  `Log.prefab` drops when chopped (confirmed via guid cross-reference:
  `Log.prefab`'s `ResourceNode.chunkPrefab` already pointed to
  `PlankChunk.prefab`, and its `Pickup.item` was already correctly
  hardcoded to `Plank.asset` — the chop-drop path itself was never
  broken, just showing a placeholder). `ContinuousDynamic` already set,
  collider resized to the real measured bounds (`0.25x0.04x0.60`).
- **`Plank.asset.worldPickupPrefab` wired for the first time** — it was
  empty (`{fileID: 0}`) despite `PlankChunk.prefab` already existing
  and already being the correct chunk; Admin spawn / drop-and-repickup
  would have fallen back to a generic grey cube before this.
- Icon + previewIcon baked via `IconBaker`.
- **Credits**: added to `Assets/Models/THIRD_PARTY_CREDITS.md` and, per
  Ben's explicit ask this time, also to the live Credits tab
  (`GameMenuScreen.cs`) — `"Wood Planks by Quaternius [Public Domain]
  via Poly Pizza"`. Public domain doesn't strictly require this (see
  Rock by Quaternius above, which was deliberately left out of the live
  tab), but Ben asked for the full treatment here.
- Placed one in `TestScene.unity` at `(6, 0.3, 2)`.

### v0.1.136-dev — Removed the orphaned Wood item

Ben's call while triaging the model/icon audit's remaining double-gap
items: rather than give `Wood` a real model/icon, eliminate it outright
— the Stick/Plank material line already covers that role, and Wood had
been completely un-gatherable (`BUGS_AND_ENHANCEMENTS.md`) since the
tree-chopping rework in v0.1.83-dev replaced its old direct drop with
Log/Plank. Confirmed via guid search before deleting (same discipline
as the Tree/Secret Wall removal in v0.1.126-dev): `WoodChunk.prefab`
and `Wood.mat` were referenced by nothing except `Wood.asset` itself.
Deleted `Wood.asset`, `WoodChunk.prefab`, `Wood.mat`, and their `.meta`
files. `BUGS_AND_ENHANCEMENTS.md`'s Wood entry removed; its
cross-reference from the still-open `MediumRock.asset` (Rock item)
entry updated to point at this instead.

### v0.1.135-dev — Leather Backpack becomes its own 5-tier CraftTier ladder

Ben: "let's wire the leather backpack model to all 5 leather backpack
tiers" — same treatment the grass `Backpack` ladder just got
(v0.1.134-dev), applied to the brand-new `Leather Backpack` item.
`LeatherBackpackItem` (built last version) becomes the Normal tier;
built the other 4 from scratch:

- New `CrudeLeatherBackpackItem`/`RudimentaryLeatherBackpackItem`/
  `FineLeatherBackpackItem`/`MasterworkLeatherBackpackItem`, each with
  its own prefab (`CrudeLeatherBackpackPickup.prefab`, etc.),
  instantiating the same `CrudeLeatherBackpack.glb` model. Same
  capacity curve as every other tiered container this session (Crude 4
  / Rudimentary 6 / Normal 8 / Fine 12 / Masterwork 16), `tier` field
  set correctly on each (0/1/2/3/4, matching `CraftTierNames`'
  convention). All `ContinuousDynamic`, all wired to their own
  `worldPickupPrefab`.
- Icons + previewIcons baked for all 4 new tiers via `IconBaker`.
- **Only the Normal tier (`Leather Backpack`) has a crafting recipe**
  (the placeholder 6x Cloth + 4x Rope one from v0.1.134-dev) — the
  other 4 tiers are data + a real model/icon, but Admin-spawn-only for
  now, same situation as the grass `Backpack` ladder's own
  Crude/Rudimentary/Fine/Masterwork tiers.

### v0.1.134-dev — Grass model across the whole Backpack CraftTier ladder; new Leather Backpack

Ben: "let's wire the model to all tiers of the grass backpack" →
clarified as all 5 tiers of the `Backpack` `CraftTier` ladder (Crude/
Rudimentary/Normal/Fine/Masterwork — distinct from the already-real
`Crude Fiber Backpack`, a separate single-tier item). Then, catching
that this would orphan the Normal tier's existing leather model: "that
should orphan the leather backpack. let's create a leather backpack
crafting tier, under sewing. create recipes per our standard, and
we'll adjust the materials later."

- **All 5 `Backpack` ladder tiers now use the Grass Backpack model**
  (`Assets/Models/GrassBackpack.glb`, from v0.1.133-dev): `Backpack.
  prefab` (Normal) had its visual swapped from `CrudeLeatherBackpack.
  glb` to grass; four brand-new prefabs (`CrudeBackpackPickup`,
  `RudimentaryBackpackPickup`, `FineBackpackPickup`,
  `MasterworkBackpackPickup`) built from scratch for the other four
  tiers, which previously had **no prefab, no icon, no world pickup at
  all** — data-only, unreachable in play, per `BUGS_AND_ENHANCEMENTS.md`.
  Capacity per tier matches the design already logged there (Crude 4,
  Rudimentary 6, Normal 8, Fine 12, Masterwork 16); all wired to their
  `ItemDefinition.worldPickupPrefab`, all `ContinuousDynamic`.
- **New `Leather Backpack`** — a standalone item (same "single item
  outside the ladder" pattern as `Crude Fiber Backpack`), giving the
  leather model a real home instead of leaving it unused. New
  `LeatherBackpackItem.asset`, `LeatherBackpack.prefab` (instantiates
  `CrudeLeatherBackpack.glb` fresh — the model file itself was never
  touched, just no longer referenced by the Normal tier), capacity 8.
  **`LeatherBackpackRecipe.asset` — placeholder ingredients (6x Cloth +
  4x Rope), Sewing-trained, per Ben's explicit call to build the recipe
  shape now and swap in real Leather/hide materials once that material
  chain exists** (`BUGS_AND_ENHANCEMENTS.md` had previously held off on
  any Backpack-ladder recipes for exactly this reason — this doesn't
  fill in the ladder itself, just unblocks the new standalone item).
  Wired into `PlayerCrafting.recipes` in `TestScene.unity`.
- Icons + previewIcons baked for all 6 items (5 ladder tiers + Leather
  Backpack) via `IconBaker`. Cleaned up two more orphaned old icon
  files (`BackpackIcon.png`/`BackpackPreviewIcon.png`, superseded by
  `BackpackItemIcon.png`/`...Preview.png` once the Normal tier's model
  changed and its icon got re-baked under the item-asset-name
  convention).

### v0.1.133-dev — Crude Fiber Backpack gets a real model (woven grass basket)

Second double-gap item off tonight's audit. Ben: "let's use the api to
generate a woven grass backpack, create a good prompt and we'll just
use what it produces."

- Generated via Tripo3D's API (`"a small woven grass backpack, plant
  fiber cordage bag with shoulder straps, isolated on a plain
  background, no person, no model, low-poly game asset"`, 20 credits)
  — hit the same 20-minute server-side timeout pattern as the Grass
  Belt/Knife before it (client gave up, task actually succeeded a bit
  later; caught via direct task polling). **A clean, strong result on
  the first attempt** — a proper backpack silhouette this time (unlike
  the Grass Belt, which came back as a closed ring rather than an open
  strap): woven straw/grass basket body, brown leather straps, buckle
  closure. Used as-is per Ben's call.
- Download itself needed a resumed retry (`curl -C -`) — the 42MB file
  was still transferring when the tool's timeout killed the first two
  attempts; resuming from the partial download with a freshly-repolled
  URL (each `GET /v3/tasks/{id}` call returns a new signed URL, even
  well after success) finished it.
- Imported as `Assets/Models/GrassBackpack.glb`, replacing
  `Assets/Prefabs/CrudeFiberBackpack.prefab`'s placeholder Cube.
  **Also fixed the ground-tunneling gap while rebuilding it** —
  `Rigidbody.collisionDetectionMode` was still left at the
  `AddComponent<Rigidbody>()` default (`Discrete`), same standing
  lesson as every other chunk/pickup built this session.
- Icon + previewIcon re-baked against the real model.

### v0.1.132-dev — Icon/model audit "quick wins": orphaned Backpack wired, 15 missing previewIcons batch-baked

First items off the model/icon audit's punch list from planning
tonight's session:

- **`BackpackItem.asset.worldPickupPrefab` wired to `Backpack.prefab`
  for the first time** — the real `CrudeLeatherBackpack.glb` model
  existed and was even already sitting in `TestScene.unity`, but the
  `ItemDefinition` never actually referenced the prefab (a dropped
  Backpack would have fallen back to the plain grey `DroppedItem`
  cube).
- **Found the same "standalone copy, not a real `PrefabInstance`" bug
  the Canteen had, on the scene's own Backpack this time** — its visual
  happened to already be correct (someone had manually given it the
  right child model), but future prefab edits would never have reached
  it. Replaced with a real `PrefabInstance` at the same position,
  matching the Canteen fix.
- **Batch-baked `previewIcon` for 15 items that already had a small
  `icon` but no bigger preview image**: Copper, Canteen, Copper Ore,
  Crude Fiber Backpack, Crude Fiber Belt, Crude Knife, Iron, Mining
  Face Shield, Gold Ore, Iron Ore, Small Rock, Platinum Ore, Rope,
  Stick, Silver Ore — all via `IconBaker -previewResolution 128`, no
  code changes needed, the tool already supported this. Deleted one
  orphaned old icon file (`CrudeFiberBackpackIcon.png`, superseded by
  `CrudeFiberBackpackItemIcon.png` once this item got a `previewIcon`
  too — the default output name is derived from the `.asset` filename,
  which didn't match the original bake's naming).

v0.1.129/130-dev's tint and emission fixes both did nothing visible,
even after boosting the emission value into clearly-HDR territory —
because neither was ever actually reaching the material. Diagnosed by
dumping the real model's shader properties directly rather than
guessing a third time: the Canteen's real model (like every other
Tripo3D/glTFast-imported model in the project) renders with `Shader
Graphs/glTF-pbrMetallicRoughness`, which has **none** of the Unity/URP
property names the code was checking (`_BaseColor`, `_Color`,
`_EmissionColor` — all `HasProperty() == false`). It exposes glTF-spec
names instead: `baseColorFactor` and `emissiveFactor`. Every
`SetColor("_BaseColor", ...)` / `SetColor("_EmissionColor", ...)` call
since v0.1.46-dev's original tint fix has been silently doing nothing
on this model — it happened to go unnoticed because the game had no
glTFast-shaded Canteen until v0.1.127-dev's model swap; the old
placeholder Cylinder used a hand-authored URP/Lit `Canteen.mat`, where
`_BaseColor` genuinely did work.

- `Canteen.SetTint()`/`GetTint()`/`SetEmission()` now check a list of
  candidate property names (`_BaseColor`/`_Color`/`baseColorFactor` for
  tint, `_EmissionColor`/`emissiveFactor` for emission) instead of
  assuming one shader family — works correctly against both the old
  URP/Lit convention (still used by any hand-authored `.mat`, e.g. a
  future `emptyMaterial`/`filledMaterial` override) and glTFast's
  Shader Graph.
- **Verified against the actual runtime code path this time**, not just
  compiled: instantiated the prefab in an Editor script, manually
  invoked `Awake()` via reflection (edit-mode instantiation doesn't
  call it automatically, unlike Play mode — a real gap in how the
  previous two attempts were "checked"), called `Fill()`, and confirmed
  `emissiveFactor` actually reads back `(0.5, 2.5, 5)` afterward.
  **Confirmed live by Ben** — filled reads as a clear blue-navy tint
  against empty's neutral dark brown/black, both in a side-by-side
  comparison. Resolved.

Ben's playtest of v0.1.129-dev's landing fix worked cleanly, but
"not sure that I can tell the canteen has a blue glow" — the first
emission value (`(0.1, 0.35, 0.6)`, all channels under 1.0) was too
dim to register against this scene's bright outdoor daylight. Pushed
`Canteen.FilledEmission` to `(0.5, 2.5, 5)` — genuinely HDR (well above
1.0), strong enough to read clearly even without a Bloom post-process
pass spreading it further. **Not yet re-confirmed live.**

### v0.1.129-dev — Canteen: fill status in the contents grid, a real blue glow when full, lands upright

Three small Canteen enhancements from continued playtesting:

- **Fill status now shows in a container's contents grid** (e.g. clipped
  to a Belt's attachment point), not just the main Equipment row —
  `InventoryScreen.DrawContainerContents` shows `Water 100/100`/`Empty`
  in the same spot a stackable item's `QTY: N` label sits, so a Canteen
  reads the same way no matter which UI location it's shown in.
- **Filled state now uses actual emission, not just a `_BaseColor`
  tint.** The real metal canteen model's own albedo is near-black, and
  a `_BaseColor` tint multiplies against that — barely visible. Added
  `Canteen.SetEmission()` (enables `_EMISSION`, sets `_EmissionColor`)
  alongside the existing tint, so filled genuinely glows blue on top
  regardless of how dark the underlying material is; empty clears
  emission back to black (off).
- **Dropped/scattered canteens no longer tip onto their side.** Root
  `Rigidbody.constraints` set to freeze X/Z rotation (still free to
  spin/settle around its own vertical Y axis) — a `BoxCollider`'s flat
  edges catching against the ground didn't perfectly match the
  cylindrical mesh, so it could land tipped over. Now it always lands
  upright, like a real canteen would.

### v0.1.128-dev — Crude Fiber Belt placed in the scene; found a Canteen that wasn't a real prefab instance

Ben: "let's spawn it in the game on start for now as well" (the
Canteen), then mid-turn: "let's also spawn a grass belt."

- **`Crude Fiber Belt` placed in `TestScene.unity`** at `(4, 0.3, 1.5)`,
  a real `CrudeFiberBelt.prefab` instance — first time it's existed as
  a world pickup rather than craft-only.
- **Found a pre-existing standalone "Canteen" GameObject at `(-1, 0.3,
  1.5)`** while trying to place a new one — turned out one already
  existed in the scene, but it was a fully independent embedded copy
  (its own `Body`/`Cap` Cylinder children), not a `PrefabInstance` of
  `Canteen.prefab`. This meant the v0.1.127-dev model swap never
  actually reached it — it was silently still showing the old
  two-piece grey placeholder despite the prefab itself being fixed.
  Replaced it with a real `PrefabInstance` at the same position (so it
  picks up the new model, and any future prefab edit automatically),
  matching how every other world pickup this session is placed.
  **Lesson:** a prefab swap only reaches instances that are actually
  linked as `PrefabInstance`s — a standalone embedded copy (same
  pattern as the old "Belt"/tier-2 "Fiber Belt" object) silently
  diverges and needs checking for independently.

### v0.1.127-dev — Canteen gets a real model (simple metal canteen)

Ben: "let's use the api to create the canteen. we can make a simple
metal canteen. standard rules apply for item creation and icons."

- Generated via Tripo3D's API (`"a simple metal canteen with a screw
  cap, isolated on a plain background, no person, no model, low-poly
  game asset"`, 20 credits) — clean on the first attempt, no 500s, no
  timeout, no unwanted extra geometry. Reads clearly as a cylindrical
  metal canteen with a dark threaded cap.
- Imported as `Assets/Models/MetalCanteen.glb`, replacing
  `Canteen.prefab`'s old two-piece placeholder (a scaled Cylinder
  "Body" + a smaller scaled Cylinder "Cap") with a single real-mesh
  child, uniformly scaled to match the old footprint's height (`0.42`).
  Root `Rigidbody`/`BoxCollider`/`Canteen` component untouched — both
  were already correctly built (`ContinuousDynamic` already set),
  collider resized to the newly-measured bounds.
- **`CanteenItem.asset.worldPickupPrefab` wired for the first time** —
  previously empty/unset entirely, meaning Canteen was craft-only and
  couldn't be dropped-and-repicked-up or spawned via the Admin tool.
  Now it can.
- `Canteen.cs`'s runtime empty/filled tinting (creates a material clone
  from whatever the model's own material is, no dedicated
  `emptyMaterial`/`filledMaterial` assets were ever set) continues to
  work unchanged against the new single-renderer model — simpler than
  before, since there's only one renderer to tint instead of two.
- Icon baked via `IconBaker`. Fifteenth item with an icon.

### v0.1.126-dev — Removed the procedural Tree and the unused Secret Message Wall

Planning cleanup, Ben's call: while auditing every model in the project
(real vs. procedural vs. placeholder, for tomorrow's session planning),
two long-standing pieces of dead/redundant weight got flagged and
removed outright rather than just noted:

- **Procedural "Tree" removed entirely.** Built in v0.1.58-dev (branching
  trunk mesh + ~100 primitive-sphere foliage clusters + 2 real
  `TreeBranch_PolyByGoogle.glb` branches), it was the game's only
  harvestable tree until **Big Tree by 3Donimus** (`BigTree_3Donimus.glb`,
  a real CC-BY model) was made choppable in v0.1.91-dev specifically to
  replace it — the procedural version had documented shape problems
  (pole-like trunk, floating foliage, washed-out bark) that Big Tree
  fixed outright. It had already been trimmed from 4 scene instances
  down to 1 in the 2026-08-06 declutter pass; now the last one, plus its
  prefab (`Assets/Prefabs/Tree.prefab`) and two dedicated assets
  (`TreeTrunkMesh.asset`, `TreeFoliage.mat`), are gone. **Kept**:
  `TreeBark.mat` and `Log.prefab` — both still genuinely shared with Big
  Tree's own chop-drop chain (Log → Plank), confirmed via guid
  cross-reference before deleting anything, not assumed. Big Tree is now
  the game's only tree.
- **`SecretMessageWall.cs` deleted.** A self-contained Easter-egg script
  (reveals hidden text to a Sunglasses-wearing player looking at a
  specific wall) that, per this session's model audit, was never
  actually placed anywhere in `TestScene.unity` — confirmed via guid
  search before deleting, only reference anywhere was a comment in
  `ResourceNode.cs` (updated to drop the dangling mention). Dead code
  with zero scene footprint; no gameplay lost.
- `TEST_FEATURE_PLAN.md` updated: removed checklist entries that only
  ever tested the procedural Tree or referenced Secret Wall re-adding
  instructions; Big Tree's own chopping entry rewritten to stand alone
  (previously phrased as "same as the real Tree above/differs by X").

### v0.1.125-dev — Backpack + Belt contents merged into one Inventory panel

Ben, after seeing v0.1.124-dev's fix render Backpack and Belt as two
separate bordered "Inventory" panels side by side: "let's add the belt
to the inventory panel with the backpack instead of its own panel."
Restructured `InventoryScreen.DrawContent()` so there's a single
"Inventory" panel again (matching the pre-v0.1.124-dev look) that now
stacks one preview+contents row per worn container vertically inside
itself, instead of one bordered panel per container. Still 0 panels
(nothing at all) when no container is worn, same as before either fix.

### v0.1.124-dev — Backpack + Belt worn together only ever showed one contents panel

Ben's playtest of v0.1.123-dev's anchor fix: equipped a Canteen onto
the Belt's attachment point and it "still does not show up on the
inventory panel when equipped." Turned out to be a *different*,
already-tracked bug (`BUGS_AND_ENHANCEMENTS.md`, flagged 2026-08-06,
confirmed via playtest 2026-08-07) that just hadn't been fixed yet: Ben
had a Backpack equipped (Back) at the same time as the Belt (Waist),
and `InventoryScreen`'s side "contents" panel only ever rendered
**one** worn container at a time — `GetWornContainer()` checked Back
before Waist and returned on the first match, so the Backpack's panel
always won and the Belt's (with the Canteen genuinely inside it) never
rendered at all.

- `GetWornContainer()` (singular) replaced with `GetWornContainers()`,
  returning every worn `IInventoryHolder` across Back and Waist instead
  of just the first.
- `DrawContent()` now loops over that list, rendering one
  preview+contents panel per worn container side by side, instead of
  at most one.
- `DrawBackPreview()`/`GetBackSlotPreviewIcon()` (Back-only) generalized
  to `DrawContainerPreview(Sprite)`/`GetSlotPreviewIcon(string
  slotName)`, since there can now be more than one preview box on
  screen at once.
- No items were ever actually lost by this bug — the Canteen was
  correctly inside `Belt.Inventory` the whole time, just not rendered
  anywhere visible. Worth confirming in the next playtest that this
  reads clearly (nothing to recover, just now visible) rather than
  alarming.

### v0.1.123-dev — Canteen/Belt carry anchors were never wired up

Ben's playtest of v0.1.122-dev: equipped a Canteen onto the newly-visible
Crude Fiber Belt's attachment point and it "doesn't show up anyplace."
Not related to the belt's new model — investigation found `PlayerCanteen`
(`leftHandSlotAnchor`, `rightHandSlotAnchor`, `beltSlotAnchor`) and
`PlayerBelt` (`carrySlot`) were all pointing at `{fileID: 0}` (unset) on
the Player in `TestScene.unity`. Each falls back to the player's own
root `transform` when unset, so both the Belt itself (worn on Waist) and
anything equipped to it (or to a hand) were being parented at the
player's exact pivot point instead of a sensible carry position —
functionally equipped (shows correctly in the Equipment/contents UI,
`Belt.Inventory` genuinely holds the Canteen) but effectively invisible
in the 3D world.

Found `HandAnchor` (`0.3, 1.3, 0.4`) and `BeltAnchor` (`0.25, 0.9, 0`)
already sitting as real child transforms on the Player, alongside the
already-correctly-wired `BackpackAnchor` — these look like they were
built for exactly this purpose and just never got connected. Wired:

- `PlayerCanteen.leftHandSlotAnchor` and `.rightHandSlotAnchor` → both
  to the single existing `HandAnchor` (only one hand-anchor object
  exists, not a separate one per hand).
- `PlayerCanteen.beltSlotAnchor` → `BeltAnchor`.
- `PlayerBelt.carrySlot` → `BeltAnchor` (the belt itself was never
  anchored either — worth a specific look at whether the belt's own
  worn position looks right now, not just the Canteen clipped to it).

**Not yet re-verified live** — worth confirming a worn Canteen (both
via a hand and via a Belt point) now actually appears at a sensible
position, and that the Belt itself looks right worn on the body.
**Scope note:** Sunglasses/Mining Face Shield/Nav Computer/Health
Monitor equip with no dedicated anchor field at all (always the
player's root, by their code's design, not a similar oversight) —
untouched here, out of scope unless one of those turns out to have the
same visibility problem.

### v0.1.122-dev — Crude Fiber Belt gets a real model (green woven grass)

Ben: "let's use the api to create a green, woven grass belt. let's
import it into the game." Turned out most of the plumbing already
existed — `CrudeFiberBeltItem`/`CrudeFiberBeltRecipe` (8 Fiber → 1 Crude
Fiber Belt, trains Sewing) were already built and already wired into
`PlayerCrafting.recipes` in `TestScene.unity`; only the visual was
missing (`CrudeFiberBelt.prefab` was a plain scaled grey Cube). This
was purely an art-pass swap, not new gameplay.

- Generated via Tripo3D's API (`"a green woven grass belt, plant fiber
  cordage wrapped in a coil, isolated on a plain background, no person,
  no model, low-poly game asset"`, 20 credits). **Hit the same
  20-minute server-side processing timeout the Crude Stone Knife's
  first real attempt hit** (`CHANGELOG.md` v0.1.115-dev) — the script's
  own polling gave up, but the task kept running server-side and
  actually succeeded a few minutes later; polled `GET /v3/tasks/{id}`
  directly to catch the `model_url` before its 5-minute expiry instead
  of re-spending credits on a second attempt.
- Came back as a closed woven ring/wreath shape rather than an open
  strap with overlapping ends — confirmed with Ben this was fine to use
  as-is rather than regenerating (matches the existing placeholder's
  own "not a final art pass" caveat in `TEST_FEATURE_PLAN.md` — this
  is a real improvement over a flat grey box regardless of exact
  strap shape).
- Imported as `Assets/Models/GrassBelt.glb`, swapped into
  `Assets/Prefabs/CrudeFiberBelt.prefab` (uniformly scaled to match the
  old placeholder's `0.5` max dimension, `BoxCollider` resized to the
  real measured bounds `0.50x0.12x0.50`).
- Icon baked via `IconBaker`. Fourteenth item with an icon.
- **No `THIRD_PARTY_CREDITS.md` entry needed** — that ledger only
  tracks CC-BY-licensed third-party models; Tripo3D API-generated
  content has its own no-attribution-required commercial license (see
  `Tools/Tripo3D/README.md`), same as the Backpack/Knife/Rope before it.
- **Note:** the separate pre-placed "Fiber Belt" (`BeltItem.asset`,
  tier 2, found near `(-2, 0.3, 1.5)`) is a different item on a
  different standalone prefab, not a `CrudeFiberBelt.prefab` instance —
  still a plain grey Cube placeholder, unaffected by this change.

### v0.1.121-dev — Silver/Gold/Platinum were missing their mid tier; also fixed a near-frictionless scatter bug

Ben's playtest of v0.1.120-dev: broke a Silver Ore Node and the pieces
"bounced out of the game" before he could pick them up, and separately
noted Silver/Gold/Platinum "missed the mid tier size that required
breaking" — v0.1.120-dev shipped them as a 2-tier structure (Ground
Node → final Ore item directly), reusing the pre-existing `SilverOre`/
`GoldOre`/`PlatinumOre` items and their `*OreChunk.prefab`s as-is. Ben
confirmed he wanted full parity with Copper/Iron's 3-tier structure
instead, so:

- **`SilverOreChunk`/`GoldOreChunk`/`PlatinumOreChunk.prefab` converted
  from the final `Pickup` tier into the punchable mid-tier
  `ResourceNode`** (mirrors `CopperOreChunk`/`IronOreChunk`) —
  bare-handed, 1 hit, breaks into 2 of a new final tier, `respawnDelay:
  0`.
- **New `SilverOrePiece`/`GoldOrePiece`/`PlatinumOrePiece.prefab`** —
  the actual final `Pickup` tier now, smaller than the mid-tier chunk
  (same Gold-smallest/Platinum-largest ordering), `Pickup.item`
  hardcoded to the existing `SilverOre`/`GoldOre`/`PlatinumOre` items
  (no new item assets needed — these three stay 2-item-tiers total,
  just with a punchable step added in front of the existing one, unlike
  Copper/Iron which needed a whole new refined-metal item).
  `SilverOre`/`GoldOre`/`PlatinumOre.worldPickupPrefab` re-pointed from
  the (now mid-tier, no-longer-a-Pickup) `OreChunk` prefabs to these.
- **Root cause of the scatter/bounce report, found while converting**:
  the original pre-existing `SilverOreChunk`/`GoldOreChunk`/
  `PlatinumOreChunk` prefabs had `Rigidbody.linearDamping: 0`,
  `angularDamping: 0.05` — nearly frictionless, unlike every other
  chunk in the project (`RockChunk`: 1.5/2, this session's `CopperChunk`:
  2/3). Same impulse force, near-zero drag — pieces kept rolling long
  after landing instead of settling near the break point. Set both the
  mid-tier and new final-tier Rigidbodies to `2`/`3` (matching
  `CopperChunk`'s already-proven values) while doing this conversion
  anyway. **Confirmed fixed by Ben's playtest** — scatter behavior is
  "working much better" now, pieces settle near the break point instead
  of rolling away.
- Icons for `SilverOre`/`GoldOre`/`PlatinumOre` re-baked against the new
  final-tier `*Piece.prefab`s (previously baked against the mid-tier
  visual, which is fine but the final pickup is what most often shows in
  inventory).

### v0.1.120-dev — Silver/Gold/Platinum Ore Nodes rebuilt, disguised via Mining Face Shield

Ben: "let's now do silver, gold and platinum. let's use the same lessons.
vary the size of the boulders for each type. make sure that can only see
them with the mining shield on. spawn one of each into the game, and
spawn the mining shield into the game as well."

These three used to exist (`TEST_FEATURE_PLAN.md` still had a whole
section for them at v0.1.60-dev/v0.1.61-dev, disguise mechanic and all)
but were removed from `TestScene.unity` in the 2026-08-06 startup-scene
trim along with the Mining Face Shield itself — they were scene-embedded
GameObjects, never saved as reusable prefabs, so trimming them left no
trace beyond that stale test-plan section. Rebuilt from scratch rather
than restored, applying every lesson from this session's Copper/Iron
work:

- **New disguised Ground Node per metal** (`Silver Ore Node` at
  `(6, 0.4, -4)`, `Gold Ore Node` at `(8, 0.4, -4)`, `Platinum Ore Node`
  at `(10, 0.4, -4)`) — `Rock_Quaternius.glb`, deliberately distinct
  sizes per Ben's request: Gold smallest (`0.70x0.65x0.72`, rarest/
  smallest veins), Silver medium (`1.00x0.95x1.05`), Platinum largest
  (`1.80x1.15x1.35`, most imposing). `ResourceNode.hiddenMaterial`/
  `revealedMaterial`/`hiddenChunkPrefab` populated for the first time
  anywhere in the project — `hiddenMaterial` is Rock_Quaternius' own
  default imported material (read straight off the model, the same
  "generic rock" look Boulder already uses undyed, not a hand-picked
  stand-in), `revealedMaterial` is each metal's existing `*OreRevealed.
  mat`, `hiddenChunkPrefab` is the existing plain `RockChunk.prefab` —
  matching the code comment's own suggestion ("should be a plain Small
  Rock chunk prefab") that had sat unused until now. Gated behind any of
  the 5 Pickaxe tiers, same as Copper/Iron.
- **Existing `SilverOreChunk`/`GoldOreChunk`/`PlatinumOreChunk` prefabs
  kept as the final pickup tier** (already correctly built — hardcoded
  `Pickup.item`, `Rigidbody.collisionDetectionMode` already
  `ContinuousDynamic` — no fixes needed there) but upgraded from a
  placeholder `Cube` to the real `Rock_Quaternius` mesh + the metal's
  `*OreRevealed.mat`, for visual consistency with every other ore tier
  shipped this session. Sizes varied to match the Ground Node ordering
  (Gold smallest, Platinum largest).
- Same UV-mismatch smearing bug hit on Copper/Iron applied here too —
  `SilverOreRevealed.mat`/`GoldOreRevealed.mat`/`PlatinumOreRevealed.mat`
  were all still at the 1x tiling that smears on `Rock_Quaternius`'
  UV layout; fixed to 6x proactively rather than waiting for a bug
  report, same fix already confirmed twice this session.
- **New `MiningFaceShieldPickup.prefab`** — the item was craft-only
  until now (`MiningFaceShieldItem.asset.worldPickupPrefab` was empty).
  No custom model exists for it yet, so it's a simple flattened-
  cylinder placeholder visor (same "primitive until it's worth a
  Tripo3D generation" convention the Stick/Knife started with), root
  `Rigidbody` set to `ContinuousDynamic` from the start. Wired to the
  item and placed in `TestScene.unity` at `(6, 0.5, -6)`.
- Icons baked for `SilverOre`, `GoldOre`, `PlatinumOre` (existing items
  that never had one) and the new `MiningFaceShieldItem`, via
  `IconBaker`. Tenth through thirteenth items with icons.
- **`TEST_FEATURE_PLAN.md` updated**: the stale v0.1.60/61-dev section
  describing the old pre-trim nodes replaced with current coordinates/
  sizes; the 2026-08-06 trim note no longer lists these as missing.

### v0.1.119-dev — Copper resized bigger, Iron gets the full pipeline too

Copper Ore Node made noticeably bigger (`0.71x0.65x0.80` →
`1.15x1.06x1.30`) per Ben's request — and since that also exposed a
collider that was never actually resized to match (still radius 0.5 in
a leftover 0.8-scaled parent, effective 0.4 world radius), fixed that
too while resizing rather than leaving it undersized again.

Then mirrored the entire Copper pipeline onto Iron, applying every
lesson from building Copper the first time instead of rediscovering
each one:

- **Iron Ore Node** swapped from its own plain Sphere to
  `Rock_Quaternius.glb` + `IronOre.mat`, sized deliberately **flatter
  and wider than Copper** (`1.50x0.85x1.60` vs Copper's
  `1.15x1.06x1.30` — shorter in Y, bigger footprint) per Ben's request,
  so the two ore types read as distinct silhouettes rather than
  recolored copies of the same shape. Applied the 6x texture tiling
  fix to `IronOre.mat` up front (same UV-mismatch cause as Copper,
  same fix) instead of shipping the 1x-tiling smear again — still
  rendered an isolated preview to actually confirm it before finalizing,
  rather than assuming the lesson transfers without checking. Collider
  properly resized to cover the new visual from the start this time
  (parent scale reset to 1 up front too, so there's no repeat of the
  Copper Ore Node's leftover-0.8-scale collider gap).
- **`IronOreChunk.prefab`** converted `Pickup` → punchable `ResourceNode`
  (mirrors `MediumRockChunk`/`CopperOreChunk`), visual swapped to the
  same mesh/material family, sized between the Ore Node and the new
  Iron chunk.
- **New `Iron` item + `IronChunk.prefab`** — built with both Copper
  lessons applied from the start instead of needing a follow-up fix:
  `Pickup.item` hardcoded directly (not left for `Configure()`, which
  `ResourceNode.SpawnChunk()` never calls), and `Rigidbody.
  collisionDetectionMode` set to `ContinuousDynamic` explicitly in the
  same edit that created it.
- Icons baked for `IronOre` and `Iron` via `IconBaker`. Eighth and
  ninth items with icons.
- **Flagged in `BUGS_AND_ENHANCEMENTS.md`:** `Iron` has no crafting
  recipe consuming it yet either, same situation as `Copper`/Rock/Wood.

### v0.1.118-dev — Copper chunks were spawning permanently un-pickupable

Ben's playtest, walking the full break chain (Ore Node → Copper Ore
chunk → Copper): the smallest tier scattered correctly but couldn't be
picked up at all. This is the exact bug already flagged in
`BUGS_AND_ENHANCEMENTS.md` from the Stick-bonus-chunk incident earlier
this session — `CopperChunk.prefab` was built copying
`StickPickup.prefab`'s "leave `item` empty, `Pickup.Configure()` fills
it in later" convention, but that convention only works for prefabs
reached via `PlayerDropping.SpawnPickup()` (which calls `Configure()`).
`ResourceNode.SpawnChunk()` — the actual path that spawns
`CopperChunk` when a Copper Ore chunk breaks — never calls
`Configure()` at all, so the chunk's `item` stayed null and
`Pickup.Complete()` silently no-oped. Fixed by hardcoding `item`
directly on the prefab instead, the same way `RockChunk.prefab`
already does — works correctly in both the drop-from-inventory path
(`Configure()` just harmlessly re-sets the same value) and the
break-into-chunks path (now has a real value to begin with). The
underlying systemic gap (`SpawnChunk()` still never calls `Configure()`)
remains open for `StickPickup`'s existing use as a `bonusChunkPrefab`.

### v0.1.117-dev — Copper gets the Boulder-family treatment: real shape, two tiers, icons

Ben's idea: reuse `Rock_Quaternius.glb` (Boulder's mesh) with the
existing copper-speckled `CopperOre.mat` for a real Copper Ore shape,
and mirror the exact Boulder → punchable chunk → refined-material
tier structure the rock family got in v0.1.87/90-dev.

- **Copper Ore Node** (was a plain built-in Sphere since it was first
  added) now uses `Rock_Quaternius.glb`, sized/grounded to match the
  old sphere's exact footprint (measure-old-bounds-first discipline,
  same as every other visual swap this project).
- **Real bug caught before it shipped, not after:** rendered a quick
  isolated preview (reusing the icon-baking camera/lighting technique,
  in a fresh throwaway scene) before committing to anything, and the
  reused texture looked wrong on the new mesh — its small repeating
  copper-fleck pattern stretched into one big diagonal smear, because
  `CopperOreTexture.png` was tuned for a sphere's simple UV unwrap and
  `Rock_Quaternius` has a completely different UV layout from its
  Quaternius source. Fixed by bumping `CopperOre.mat`'s `_BaseMap`/
  `_MainTex` tiling from 1x1 to 6x6 — confirmed by re-rendering the
  same preview before touching the real scene object. (First preview
  attempt rendered the wrong thing entirely — a wide gameplay view
  instead of an isolated object — because it opened the live
  `TestScene` directly; switched to a fresh empty scene, the same
  technique `IconBaker` already uses, and that fixed it.)
- **`CopperOreChunk.prefab`** converted from a `Pickup` (plain Capsule
  primitve) into a punchable `ResourceNode` — same conversion
  `MediumRockChunk` got in v0.1.90-dev. No longer directly pickupable;
  punching it (1 hit) breaks it into 2 of a brand-new **Copper** item.
  Visual also swapped to `Rock_Quaternius` + `CopperOre.mat`, sized
  distinctly from both the Ore Node above it and the Copper chunk
  below it.
- **New `Copper` item + `CopperChunk.prefab`** — didn't exist before.
  Same mesh/material family, smallest tier's proportions. Rigidbody
  explicitly set to `ContinuousDynamic` collision detection in the same
  edit that created it — the exact mistake that broke Rope's drop
  earlier this session, this time caught before it shipped by applying
  [[project_gridless_ground_tunneling]] proactively instead of after a
  bug report.
- Icons baked for both `CopperOre` and `Copper` via `IconBaker` — one
  command each, no new script needed. Sixth and seventh items with
  icons.
- **Flagged in `BUGS_AND_ENHANCEMENTS.md`:** `Copper` has no crafting
  recipe consuming it yet, same situation as Rock and Wood — built
  ahead of the crafting need per Ben's call, not an oversight.

### v0.1.116-dev — Rope gets a real visual and an icon, first from scratch

`Rope.asset` never had a `worldPickupPrefab` at all — no placeholder
to swap, a genuinely new visual. Generated cleanly on the first
attempt this time (no 500s, no timeout, no unwanted extra parts like
the knife's handle) — `"a photorealistic small coil of rope, hemp
fiber texture, tightly wound, isolated on a plain background"`,
20 credits, reads exactly as asked: a tidy bundled coil.

- **New `Assets/Prefabs/RopeCoilPickup.prefab`**, built from scratch
  (root `BoxCollider` + `Rigidbody` + `Pickup` with `item` left unset,
  same "configured at drop time via `PlayerDropping.SpawnPickup()`"
  convention as `StickPickup.prefab`/`RockKnifePickup.prefab`) rather
  than modifying an existing one. Model uniformly scaled to a 0.28
  max-dimension target (no old footprint to match against, since there
  was never a placeholder — picked to sit in the same size range as
  other small hand-carried pickups like Small Rock). Wired directly
  onto `Rope.asset.worldPickupPrefab`.
- Icon baked via `IconBaker` — reads clearly as a small tan coiled
  bundle. Fifth item with an icon.

### v0.1.115-dev — Crude Knife gets a real visual and an icon

The Tripo3D API finally cooperated — see `Tools/Tripo3D/README.md` for
the full 4-failed-500s-then-timeout-then-success saga. Real model
imported and wired in this version:

- `Assets/Models/CrudeStoneKnife.glb` swapped in for
  `RockKnifePickup.prefab`'s old placeholder Capsule primitive (the
  Crude Knife's world pickup, referenced by `CrudeKnife.asset`) —
  sized to match the old placeholder's exact footprint (`0.08 x 0.05 x
  0.35`), collider/Rigidbody/Pickup untouched. Measured old bounds and
  collider size before removing anything, same discipline as every
  other visual swap this project.
- Icon baked via `IconBaker` (`-modelPath
  "Assets/Prefabs/RockKnifePickup.prefab" -itemAssetPath
  "Assets/Data/CrudeKnife.asset"`) — reads as a small dark blade at a
  diagonal, same treatment as Stick and Small Rock. Fourth item with
  an icon.
- **Known limitation, accepted as-is (Ben's call):** the model has a
  full handle/crossguard despite every prompt attempt explicitly
  saying "no handle" — Tripo3D seems to default "knife" toward a
  hilted shape regardless. Doesn't match `CrudeKnifeRecipe.asset`'s
  actual ingredient (1 Small Rock, no wood) implying a bare blade, but
  visually reads well as a crude knapped weapon either way.

### v0.1.114-dev — Stick gets an icon, first real use of IconBaker

`IconBaker.Bake -modelPath "Assets/Prefabs/StickPickup.prefab"
-itemAssetPath "Assets/Data/Stick.asset"` — one command, no new script.
Baked cleanly on the first try (32x32, reads as a small branch at a
diagonal). Third item with an icon overall, first one built entirely
through the new tool rather than a bespoke script.

### v0.1.113-dev — IconBaker: permanent tool for baking item icons

Every icon so far (Backpack, Crude Fiber Backpack, Small Rock) was a
bespoke throwaway `Assets/Editor/*.cs` script, rewritten from scratch
each time. Ben's call: consolidate it into one reusable tool so adding
an icon for a new model going forward is a single command, not a new
script.

- **New permanent `Assets/Editor/IconBaker.cs`** (not a throwaway —
  stays in the project). Batch-mode usage:
  ```
  Unity.exe -batchmode -quit -projectPath . -executeMethod IconBaker.Bake ^
    -modelPath "Assets/Prefabs/X.prefab" -itemAssetPath "Assets/Data/X.asset"
  ```
  Optional `-resolution` (default 32), `-previewResolution` (default 0 —
  skipped unless set; also bakes a bigger image and wires it to
  `ItemDefinition.previewIcon`), `-outputName` (defaults to the item
  asset's own filename + "Icon").
- Instantiates the model in a throwaway scene, frames it with an
  orthographic camera at a fixed 3/4-from-above angle sized to its
  measured bounds, renders to a transparent PNG, imports it as a
  Sprite, wires it onto the `ItemDefinition`. Same technique as every
  icon baked by hand so far, just parameterized.
- **Bakes in every trap discovered the hard way this whole icon
  effort:** aborts loudly if launched with `-nographics` (disables
  `RenderTexture` entirely — silent failure otherwise) instead of
  producing a blank icon; explicitly sets `spriteImportMode = Single`
  (default is Multiple, which produces no actual `Sprite` object at
  all without hand-sliced sub-sprites — `LoadAssetAtPath<Sprite>`
  silently returns null otherwise); reloads the `ItemDefinition`
  reference *after* baking rather than trusting one held across the
  `AssetDatabase.ImportAsset`/`SaveAndReimport` calls, which can
  invalidate it (hit this immediately on the tool's own first test run
  — `ArgumentException: Object at index 0 is null`).
- Verified end-to-end by re-baking Small Rock's icon through the new
  tool instead of its original bespoke script — output was pixel-
  equivalent (same model, same resolution); deleted the now-duplicate
  original file (`SmallRockIcon.png`) and kept the tool's own naming
  convention (`RockIcon.png`, matching `Rock.asset`'s filename).

### v0.1.112-dev — Hover tooltip shows an icon-only slot's item name

Contents-grid slots with an icon show nothing but the picture now (no
text at all) — Ben's ask: hovering the icon should show the item's
name, since it's otherwise not visible anywhere in that slot.

- Unity's **runtime** IMGUI (unlike the Editor's) never draws
  `GUI.tooltip` on its own — setting a `GUIContent`'s tooltip just
  makes the string available, nothing renders it without doing so
  explicitly. New `InventoryScreen.DrawTooltip()` checks `GUI.tooltip`
  each frame and draws a small floating panel-backed label near the
  cursor when it's non-empty.
- Drawn from `DrawPopups()` (called by `PlayerMenuScreen` after the
  scroll view/`BeginArea` end), not inside `DrawContent()`'s scroll
  view — same reasoning as the other popups there: needs to sit on
  top of everything, unclipped by the scroll rect, positioned in real
  screen space via `Event.current.mousePosition`.
- Scoped to icon-bearing contents-grid slots specifically — items with
  no icon still show their name as visible text in the slot itself,
  so a tooltip would just be redundant there.

### v0.1.111-dev — Empty contents grid slots were invisible, fixed

Ben's report right after v0.1.110-dev: removing the "Empty" text left
nothing visible at all where those slots used to be — no way to tell
how many total slots a container has when some are empty. Root cause:
`GUI.skin.box`'s default runtime appearance has too little contrast
against `DebugGUI.Panel`'s dark background to read as a box on its
own — it only looked fine before because the "Empty"/item text (and
its own default label coloring) was doing the actual visible work,
not the box style.

- New `DebugGUI.Slot` — an explicit solid mid-gray background (same
  `SolidTexture` technique `DrawPanel`/`Panel` already use, not a
  default skin style) — guarantees a slot reads as a distinct box
  regardless of what's inside it. Both empty and occupied contents-grid
  slots now use this instead of `GUI.skin.box`.

### v0.1.110-dev — Contents grid empty slots drop the "Empty" text

Ben confirmed the v0.1.109-dev icon fix worked (Small Rock renders
correctly with QTY: 9 beneath it), then pointed out empty slots in
this same grid still said "Empty" in text — wanted a plain gray box
instead, matching how the occupied slots read now that they're
icon-driven. Scoped to the contents grid specifically; the equipment
slot list's own "Empty" labels (Head/Face/Neck/...) are unchanged.

### v0.1.109-dev — Contents grid icon overlay, replacing broken GUIContent combo

Ben caught it immediately: the Small Rock icon didn't render in the
contents grid at all — the slot just showed truncated text ("ill Rock
x9"). Root cause: `ItemContent()`'s `GUIContent`(icon+text) combo,
which works fine in wider rows, silently breaks down at this grid's
tight 70x30 box — no room for a 32x32 icon and a full name/count
string together, and Unity dropped the icon rather than the text.

- Contents grid slots with an icon now draw it as a **separate overlay
  on top of a plain box** (`GUI.DrawTexture` after `GUILayout.Button`),
  the same technique the Back preview box already uses successfully —
  sidesteps `GUIContent` sizing entirely instead of fighting it again.
  Items with no icon still fall back to the old text-in-button
  rendering, unchanged.
- Both empty and occupied slots now use **`GUI.skin.box`** as their
  visual style (occupied ones via `GUILayout.Button(..., GUI.skin.box,
  ...)`, still fully clickable) — Ben's ask for the two to read as the
  same "gray filled box," not visually different states.
- `SubBoxHeight` bumped from 30 to 44 — it was literally shorter than
  the 32x32 icon itself before any padding, let alone room to fit one
  comfortably.

### v0.1.108-dev — "QTY: N" label under each backpack/storage contents slot

Ben's call, scoped specifically to the contents grid (`DrawContainerContents`)
— not the main inventory list, equipment slots, or move popup, which
all keep their current icon+text-beside-it look.

- Each occupied slot is now a small vertical group: the existing
  icon+name button on top, a new `"QTY: {count}"` label directly below
  it. Blank (not "QTY: 1") for a non-stackable item (`maxStack <= 1`,
  e.g. a Backpack) — still drawn as an empty label either way so every
  column in the row reserves the same height, keeping the grid aligned.
  Empty slots get the same blank label treatment for the same reason.

### v0.1.107-dev — Small Rock gets an icon (second item to have one)

Baked from the actual in-game model (`RockChunk.prefab`, same asset
Rock Node's chunks already use — a pale rock/pebble silhouette),
32x32, same offscreen-camera technique as the Backpack icons. Wired to
`Rock.asset` (the `Small Rock` item — yes, the filename and the item
name don't match, a pre-existing quirk, not something this change
touches). No `previewIcon` this time — Small Rock has no dedicated
big-preview UI the way a worn Backpack does, so only the small inline
icon was worth baking. Shows up automatically everywhere `ItemContent()`
already renders an item's icon (main inventory list, equipment slots,
container grids, move popup) — no `InventoryScreen.cs` changes needed,
that plumbing was already generic from the Backpack work.

### v0.1.106-dev — "Equipment"/"Inventory" relabeled onto their own panels

Ben's call: "Equipment" now labels the slot list panel specifically
(drawn inside it, not above the whole row), and "Inventory" moved down
from its old spot above the main inventory list to label the
preview+contents panel instead — the main inventory list above now has
no header at all, per Ben's choice when asked what should happen to
that spot once the text moved off it.

### v0.1.105-dev — The two panels sit side by side now, not stacked

Final layout pass on this back-and-forth: the slot list panel and the
preview-icon+contents panel were two separate `GUILayout.BeginHorizontal`
rows, so they stacked vertically. Combined them into one row — slot
list panel first (left), preview+contents panel second (right, only
when something's worn on Back/Waist) — matching Ben's original
red-box/green-box mockup. Dropped the `GUILayoutUtility`-measured
header-alignment math from v0.1.102-dev along with it; it doesn't
apply to a plain side-by-side row.

### v0.1.104-dev — Panel style was covering the whole screen, fixed

The `DebugGUI.Panel` style added in v0.1.103-dev rendered as one giant
black rectangle spanning nearly the entire screen instead of framing
just the equipment slot list and the icon+contents pair separately —
both sections merged into one indistinguishable black expanse with no
visible gap between them. Root cause: `new GUIStyle()` defaults to
`stretchWidth`/`stretchHeight = true`, so `GUILayout.BeginVertical`/
`BeginHorizontal` using it expand to fill all available space in their
parent row rather than shrink-wrapping to their actual content —
explicitly set both to `false`. Also added a `GUILayout.Space(10)`
between the two panels, which had no gap between them at all before.

### v0.1.103-dev — Equipment slot list and icon+contents get real panel backgrounds

Ben's mockup: both sections should read as distinct bordered panels
sitting on top of the 3D game view, not floating content directly on
top of it with no visual boundary.

- New `DebugGUI.Panel` — a `GUIStyle` wrapping the same background
  `DrawPanel()` already draws (matches the rest of the game's panel
  look), but usable directly with `GUILayout.BeginVertical`/
  `BeginHorizontal` so it auto-sizes to whatever's inside it instead of
  needing a pre-computed `Rect`.
- The equipment slot list (`Head`/`Face`/.../`Back`/...) and the
  Back-preview-icon-plus-contents-grid pair each now draw inside their
  own `DebugGUI.Panel`-styled group — two visibly separate boxed
  sections instead of everything floating loose.

### v0.1.102-dev — Icon+contents aligned under "Equipment" by measurement, not guesswork

Ben marked up a screenshot: equipment slot list contained cleanly on
its own (left column), icon+contents pair fully separate, positioned
under "Equipment" in the open area to the right — not bleeding into
or overlapping the slot list column the way the FlexibleSpace-centered
version from v0.1.101-dev did.

Root cause of that: centering the icon+contents *group* via symmetric
`FlexibleSpace` shifts the icon left of the group's own midpoint
(since the contents grid trailing after it is much wider than the
leading gap) — it can never land the icon under a header centered on
the full row width, only under the *group's* center, which isn't the
same point. Rather than fight that math, `DrawContent()` now measures
`GUILayoutUtility.GetLastRect()` right after drawing the header and
uses its actual real center to place the icon+contents row via
`GUILayout.Space()` — matching real numbers instead of assumptions
about how the surrounding layout distributes width.

### v0.1.101-dev — Preview box AND contents grid together, under "Equipment"

The two requirements from v0.1.99 and v0.1.100 actually both needed to
hold at once: icon under the header, contents grid right beside the
icon — not one or the other. Fixed by finding the worn container
*before* the slot list draws instead of after: new
`GetWornContainer()` does the same Back/Waist lookup
`DrawEquipmentSection()` used to do as a side effect of drawing (and
returned once it finished), so `DrawContent()` can now put the
icon+contents row directly under "Equipment" — centered as one group
via `FlexibleSpace` on both sides — with the slot list drawn
separately below. `DrawEquipmentSection()` is `void` now; nothing
needed its return value anymore once the lookup moved out.

### v0.1.100-dev — Back preview box moved under "Equipment" (final spot)

Back to its own centered row, right under the "Equipment" header — no
longer tied to the slot list or the contents grid's position. It's
independent of `wornContainer` now too: `DrawBackPreview()` checks the
Back slot's icon directly and draws nothing at all (not even a blank
frame) when there's nothing to preview, rather than needing
`DrawEquipmentSection()` to run first.

### v0.1.99-dev — Back preview box moved beside the backpack's own contents

Misread the previous request: "inventory slots" meant the worn
container's own storage grid ("Backpack contents"), not the player's
equipment slot list (Head/Face/Neck/...). Moved `DrawBackPreview()` to
sit between the equipment slot list and `DrawContainerContents()`, so
the picture is paired with what's actually inside it. As a side effect
this also fixes a leftover oddity from v0.1.98-dev: the box no longer
shows (blank frame) when nothing's worn on Back — it's now only drawn
inside the same `wornContainer != null` block as the contents grid it
sits beside, so it only appears when there's actually a contents grid
for it to sit next to.

### v0.1.98-dev — Back preview box moved beside the slot list, not above it

Ben's call after seeing v0.1.97-dev's centered-but-stacked layout: the
preview box and the Equipment slot list (Head/Face/Neck/.../Back/...)
were still two separate rows, box on top, slots below starting back at
the left edge. Restructured `DrawContent()` so the preview box is the
leftmost element of the *same* horizontal row as the slot list, with
the "Backpack contents" side column still following after — one row:
[preview box] [slot list] [container contents, if worn]. Removed the
`FlexibleSpace()` self-centering `DrawBackPreview()` grew in
v0.1.97-dev, since the box's position now comes from where it sits in
that row, not from centering itself in a lone one.

### v0.1.97-dev — Back preview box wasn't centered under "Equipment"

Ben compared two screenshots — the preview box was hugging the left
edge, sitting right above the "Head" row, instead of centered under
the "Equipment" header the way it should read as belonging to it.
Root cause: `DebugGUI.Header`'s `TextAnchor.MiddleCenter` centers the
*text* within a label that expands to fill its row, but `GUILayout.Box`
doesn't get the same treatment for a fixed pixel size — it just sits
at the left edge of whatever space it's given. Wrapped it in
`GUILayout.BeginHorizontal()` + `FlexibleSpace()` on both sides to
actually center the box control itself, matching where the header
text sits.

### v0.1.96-dev — Icon-only in every equip slot, crisp preview icon

Two more follow-ups once the preview box and hand-slot icon were both
visible: Ben pointed out a hand-held Backpack still said "Backpack"
next to its icon (the icon-only treatment from v0.1.95-dev only
applied to worn Back/Waist containers, not hand slots), and the new
96x96 preview box looked visibly blurry with no visible border.

- **Icon-only now applies to every equipment-section slot**, not just
  worn Back/Waist containers — any item with an icon shows icon-only
  there (hand, back, waist, wherever), falling back to the old text
  only for items with no icon. A hand-held Backpack now shows just its
  picture, no redundant "Backpack" label next to it.
- **New `ItemDefinition.previewIcon` field** — a separately-baked,
  higher-resolution image for large-preview UI, distinct from `icon`
  (kept small, ~32x32, for inline rows). Root cause of the blur:
  `GUIContent` images render at native pixel size with no fit-to-
  control scaling, so the 96x96 preview box was stretching a 32x32
  source 3x — genuinely blurry, not a bug in the box itself. Baked
  `BackpackPreviewIcon.png` at 128x128 directly from `Backpack.prefab`
  (not upscaled from the small one) and wired it to
  `BackpackItem.asset.previewIcon`; `DrawBackPreview()` now prefers it,
  falling back to `icon` for items that never get a dedicated one.
- **Preview box border was invisible** — it used `DebugGUI.DrawPanel`
  (a near-black overlay meant to sit on an already-dark full-screen
  panel), which blended into the game view behind it with no visible
  edge. Switched to a plain `GUILayout.Box`, the same visibly-bordered
  style every other slot box on this screen already uses.

### v0.1.95-dev — Icon polish: drop "Equipped" text, add a Back preview box

Two follow-up requests once the Backpack icon was actually visible.

- **A worn Back/Waist container's row now shows icon-only, no "Equipped"
  text**, when the item has an icon — falls back to the "Equipped" text
  it always showed if the item has none (Belt, for now), so nothing
  regresses for items that never get an icon.
- **New fixed 96x96 framed preview box** right under the "Equipment"
  header (`DrawBackPreview()`/`GetBackSlotIcon()` in
  `InventoryScreen.cs`) — shows a bigger version of whatever's worn on
  Back, blank (just the dark frame) when nothing's equipped there or
  the equipped item has no icon. Scoped to Back only, not a general
  "last clicked item" viewer — Ben's call between the two options.

### v0.1.94-dev — Icon baked for the wrong Backpack, fixed

Ben still saw no icon after v0.1.93-dev, even in the fixed all-render-
sites version. Root cause: **there are two entirely separate Backpack
items** — `CrudeFiberBackpackItem.asset` (the Sewing-craftable one,
`CrudeFiberBackpack.prefab`/`CrudeFiberBackpack.glb`) which is what I
baked an icon for, and `BackpackItem.asset` (the plain pre-placed
"Backpack", tier Normal, visual is `Backpack.prefab` wrapping
`CrudeLeatherBackpack.glb`) — a completely different item and model.
Ben's playtest had the **pre-placed** one equipped, not the crafted
one, so the icon I built was simply never going to show up regardless
of how many render sites it was wired into. Should have checked which
item was actually equipped before picking one to bake — didn't.

- Baked a new icon from `Backpack.prefab` (32x32, same offscreen-camera
  technique as before) and wired it to `BackpackItem.asset.icon`. This
  one has visible straps and reads more clearly as a backpack than the
  Fiber one's simpler low-poly shape.
- `CrudeFiberBackpackItem.asset`'s icon from v0.1.93-dev is left as-is
  — it's still correctly wired to its own real item, just wasn't the
  one on screen. Not wasted, just not what was being tested.

### v0.1.93-dev — Item icons: first one, on the Crude Fiber Backpack

Ben's request: show a real 2D image in the inventory instead of the
Crude Fiber Backpack always reading as plain text. First use of a new
`ItemDefinition.icon` field (`Sprite`, null by default) — every other
item stays text-only until it gets one, no behavior change for them.

- **Icon baked from the actual 3D model**, not hand-drawn: a batch-mode
  Editor script instantiates `CrudeFiberBackpack.prefab` in a throwaway
  scene, frames it with an orthographic camera at a 3/4 angle sized to
  its measured bounds, renders to a transparent 256x256
  `RenderTexture`, and saves the result as
  `Assets/Textures/Icons/CrudeFiberBackpackIcon.png`.
- **Two real gotchas hit along the way**, worth remembering for the next
  icon: (1) `-nographics` disables the graphics device entirely, so
  `RenderTexture.Create` silently fails in batch mode — dropped the
  flag for this one script (batch mode still shows no window without
  it, it just also initializes the GPU device); (2) the importer
  defaults a fresh PNG's `spriteMode` to Multiple, which needs
  hand-sliced sub-sprites before Unity will produce an actual `Sprite`
  object at all — `AssetDatabase.LoadAssetAtPath<Sprite>` silently
  returned null until `TextureImporter.spriteImportMode` was set to
  `Single` explicitly.
- Every place `InventoryScreen` renders an item — the main Inventory
  list, equipment slot boxes (including a worn Backpack's "Equipped"
  box, not just its unequipped stack), the Backpack/StorageBox contents
  grid, and the move popup's header — now goes through a shared
  `ItemContent()` helper (`GUIContent` with the item's icon texture set
  when present) instead of a plain string, so an icon shows up
  everywhere an item does, not just one list. Text label stays either
  way — icon is additive, never icon-only.
- **Regression caught by Ben immediately after the first version
  shipped:** the backpack was equipped, not sitting in the main
  Inventory list, so the icon (only wired into that one list at first)
  never showed — fixed by generalizing to every render site above.
  Separately, the icon was originally baked at 256x256; `GUIContent`'s
  image renders at the texture's **native pixel size** in a plain
  `GUIStyle` (no auto-fit-to-control), which would have blown out every
  40px-tall row. Re-baked at 32x32 — the actual intended display size —
  once this was caught before it ever reached Ben.

### v0.1.92-dev — Fixed Big Tree's collider floating above the tree

Ben reported still being unable to chop Big Tree right after v0.1.91-dev
shipped it. Root cause: a math error in the `CapsuleCollider` placement
— the center-Y formula had a spurious extra `+ height * 0.5f` term that
shifted the collider's world-space position up by half its own height
(~3.6 units) from where it should've been. Punches were landing on
empty air well above the visible trunk/canopy instead of the actual
mesh. Confirmed and fixed by comparing the collider's computed
world-space Y range directly against the tree's measured renderer
bounds — they now match exactly (`[-0.15, 7.04]` both). No change to
`ChoppableTree`'s config, only the collider's `center`.

### v0.1.91-dev — Big Tree by 3Donimus is now choppable

Ben's request: it's been sitting as a comparison-only decoration since
v0.1.86-dev, never interactive — make it work like the real Tree.

- Added `ChoppableTree` (the same component the procedural Tree uses,
  no new code needed) directly onto the Big Tree scene object, plus a
  `CapsuleCollider` sized from its actual measured bounds (it had no
  collider at all before — glTFast doesn't auto-generate one on import,
  same gotcha already known from the Boulder/Rock Node swaps). Config
  mirrors the real Tree exactly: 3 hits with an Axe (any of the 5
  tiers), drops 3 Logs, 0.5 Gathering skill gain per hit, 180s regrow.
- **Known simplification:** Big Tree has no "Stump" child the way the
  procedural Tree does, and `ChoppableTree` gracefully degrades to
  "just disappear, then reappear" when there's no Stump to swap to —
  so chopping it fully vanishes it for the regrow window rather than
  leaving a visible stump. Fine as a first pass; a real stump visual
  would be a follow-up if Ben wants one.
- Since Big Tree is now an actively-used gameplay object instead of a
  comparison prop, its CC-BY attribution ("Big Tree by 3Donimus [CC-BY]
  via Poly Pizza") now belongs in the Credits tab too, per the standing
  rule every other shipping asset already follows — added to
  `GameMenuScreen.DrawCreditsTab()` and `THIRD_PARTY_CREDITS.md` updated
  to match.

### v0.1.90-dev — Boulder's Rock chunk is now a punchable node, not a pickup

Ben's call, in response to playtesting the v0.1.89-dev chunk visual
fix: breaking the Boulder shouldn't just hand you a "Rock" item — the
Rock chunk should itself be a small resource you punch open into Small
Rock, matching Rock Node's own break-it-down pattern rather than acting
like a loose ground pickup (Stick, Berry, etc.).

- **`MediumRockChunk.prefab`** (the chunk Boulder spawns) had its
  `Pickup` component replaced with a `ResourceNode` — same component
  Rock Node and Boulder themselves use, implementing `IPunchable`. It's
  no longer directly pickupable at all: punching it (bare-handed, 1
  hit) breaks it into 2 **Small Rock** via `RockChunk.prefab` (the same
  chunk Rock Node already spawns), scattering with the same
  `scatterForce` (1.2) and `Gathering` skill gain (0.5) every other
  node uses. Its `Rigidbody`/`SphereCollider` from the v0.1.89-dev
  visual fix are untouched, so it still launches and settles physically
  when Boulder breaks — it just can't be picked up once it lands
  anymore, only punched. `respawnDelay` set to 0 (destroyed outright
  once broken) — it's a one-off spawn, not a fixed environmental node,
  same convention as a Log dropped by a chopped Tree.
- **The "Rock" item (`MediumRock.asset`) is now orphaned** as a direct
  side effect — nothing spawns it into inventory anymore, and nothing
  ever consumed it via a recipe either. Flagged in
  `BUGS_AND_ENHANCEMENTS.md` rather than deleted outright, since
  keeping vs. repurposing it is a content decision, not an
  implementation detail.

### v0.1.89-dev — Boulder's real chunk fixed, Credits image overflow fixed

Two follow-ups from playtesting the v0.1.88-dev fixes.

- **`MediumRockChunk.prefab`** (the "Rock" item's chunk — what actually
  spawns when Boulder breaks, distinct from `RockChunk.prefab` which
  only feeds Rock Node's Small Rock) was still the old procedural
  4-pebble sphere cluster from before the Stone model swap — missed
  because the Boulder work in v0.1.87-dev only touched Boulder's own
  root visual, not the separate chunk prefab it spawns. Confirmed via
  Ben's screenshot after successfully breaking the Boulder (proving the
  proximity fix in v0.1.88-dev worked) — the scattered chunks were
  plain fused grey spheres. Swapped in `Stone_PolyByGoogle.glb` at
  `(0.5, 0.42, 0.48)`, non-uniform and distinctly proportioned from
  `RockChunk.prefab`'s Small Rock target (`0.32, 0.22, 0.28`) so it
  reads as the tier above rather than a scaled duplicate. Measured the
  old pebble cluster's bounds before removing it, `SphereCollider`
  (radius 0.35) and `Pickup`/`Rigidbody` config left untouched —
  same discipline as every other stone swap this week.
- **Credits page image could overflow the tab vertically** — it was
  only bounded by 90% of screen width, so a wide image at a tall aspect
  ratio (like `tekim_trex.png`) could render taller than the visible
  menu area. GUILayout doesn't clip or auto-scroll, so anything below
  the image (the name line, Third-Party Assets list, Close button) just
  got pushed off-screen with no way back — Ben reported this as "no
  scroll bar" and the image looking uncentered (it was centered; the
  cut-off bottom/right just made it look wrong). Fixed by also capping
  height to 50% of screen height and shrinking width to match if the
  height cap binds first — the image can no longer push anything else
  off-screen regardless of window size or the image's own aspect ratio.

### v0.1.88-dev — Credits page polish, Boulder/Rock Node/Big Tree separated

Playtest catches after v0.1.86/87-dev shipped their visuals.

- **Credits page**: the attribution image now sizes itself to 90% of
  screen width with height derived from the actual source texture's own
  aspect ratio at draw time (`Screen.width * 0.9`, then
  `height/width` from `creditsImage`) instead of a hardcoded ratio, so it
  stays correct if the image is ever swapped. "Tekim" and "the T-Rex"
  combined onto a single centered line, "Tekim & The T-Rex".
- **Boulder, Rock Node, and Big Tree by 3Donimus were crowding each
  other at game start** — Ben's report ("they spawn on top of each
  other") pointed at the real gameplay Tree, but that object was already
  at `(-8, 0, -6)`, nowhere near the cluster. The actual culprit was
  **Big Tree by 3Donimus** (the CC-BY comparison-only decorative prop,
  never wired to any gameplay script), sitting at `(-3, 3.99, 3)` at
  3.02x scale — almost on top of both Boulder `(-4, 0.6, 4)` and Rock
  Node `(-2, 0.35, 3)`, and large enough at that scale to loom over both
  in Ben's screenshot. Moved Big Tree out to `(10, 3.99, 10)`, clear of
  everything. Separately, Boulder and Rock Node's own visuals grew
  noticeably larger in v0.1.87-dev/86-dev (real meshes replacing a plain
  sphere and hand-tuned procedural shape), so their old ~2.24-unit
  spacing read as cramped even without Big Tree involved — moved Rock
  Node to `(-2, 0.35, 8)`, now ~4.48 units from Boulder.
- **"Can't break the boulder into rocks"** — investigated but no
  code-level bug found: `PlayerInteraction.cs` resolves hits via
  `GetComponentInParent<IPunchable>()`, so even a child collider under
  Boulder's new visual would still correctly resolve to its
  `ResourceNode`; confirmed via `git diff` that the Rock_Quaternius swap
  added zero colliders anywhere, and glTFast has no collider-generation
  option in the version this project uses. Leading theory is the
  Boulder/Rock Node proximity above was causing misaimed punches to land
  on the wrong node — needs Ben to retest specifically now that they're
  separated; **not confirmed fixed yet**.

### v0.1.87-dev — Rock Chunk and Boulder get real visuals too

Continuing the Stone swap from v0.1.86-dev to the rest of the stone
family.

- **`RockChunk.prefab`** (the Small Rock pieces that scatter when Rock
  Node breaks) now reuses `Stone_PolyByGoogle.glb` instead of a plain
  Sphere — but scaled **non-uniformly** (0.32 × 0.22 × 0.28, not a
  uniform shrink of the parent's proportions) so it reads as a distinct
  broken fragment rather than a miniature clone of the main rock. No
  mesh-reshaping tool exists in this pipeline — per-axis scale variation
  is the actual lever available, and that's what "tweak the shape" means
  here. Collider's physical world-space size preserved through the
  root-scale reset (same discipline as the Stick swap's non-uniform-scale
  hazard fix, v0.1.73-dev). Verified with full float precision, not
  Vector3's default 2-decimal `ToString()` — the resulting scale is
  ~1.2e-7 (matches the source mesh's enormous native coordinates) and
  briefly logged as "0.00" in a first verification pass, which looked
  like corruption but wasn't; a fresh instantiate-and-measure confirmed
  the actual rendered size hits the target exactly.
- **Boulder's visual replaced** — `Rock_Quaternius.glb` (public domain,
  Poly Pizza), swapped in for the old hand-tuned procedural shape
  (displaced-mesh body + 8 clustered pebbles, v0.1.62-dev) rather than a
  crude placeholder like Rock Node's sphere was. Target size/position
  came from measuring the OLD visual's actual current bounds *before*
  removing it, not a fresh guess — the new model lands centered on the
  exact same X/Z and grounded to the exact same depth (min.y) the old
  one occupied, size-matched to its largest dimension. The old
  "Pebbles" child wrapper is gone entirely (a completely different art
  style mixed with leftover procedural pebbles would've looked
  incoherent) and the `SphereCollider`'s center reset to origin (the old
  offset was hand-tuned for the old mesh's asymmetric centroid, not
  meaningful for the new one) — radius (0.9) kept as-is.
- Both verified via scripted read-backs confirming old
  MeshFilter/MeshRenderer/child objects are actually gone (not just
  added-alongside) and the new child/collider state matches what was
  intended.

**Licensing note:** Rock by Quaternius is public domain — no
Credits-tab attribution required (unlike the CC-BY Poly Pizza models),
though `Assets/Models/THIRD_PARTY_CREDITS.md` tracks it anyway for
sourcing consistency. Optional credit text noted there if ever wanted.

### v0.1.86-dev — Fixed a real Tree naming collision, Rock Node's real visual, Credits tab catches up

Three unrelated fixes/additions that landed together during a live
playtest session:

- **`Tree.cs` renamed to `ChoppableTree.cs`.** Found via the Console
  during Ben's playtest: `Tree` collided with `UnityEngine.Tree` (the
  built-in Terrain component) — Unity's own warning: "AddComponent and
  GetComponent will not work with this script." Real correctness bug,
  not just noise; fixed by renaming the class (kept the same script
  guid where possible — Unity's file-watcher raced the rename since the
  Editor was open, assigned its own fresh guid, so `Tree.prefab`'s
  component reference was updated to match the actual guid Unity landed
  on rather than the one originally intended). The missing Play button
  Ben hit in the same session turned out to be an unrelated toolbar
  rendering glitch (confirmed via `Ctrl+P` working fine) — not caused by
  this, but found while investigating it.
- **Rock Node's visual replaced** — was a plain built-in Sphere
  primitive, now `Stone_PolyByGoogle.glb` (CC-BY, Poly Pizza). The raw
  glTF's mesh coordinates are enormous (millions of units) with a pivot
  far outside the visible geometry — instead of hand-deriving the
  correct scale/position, the swap script instantiates once to measure
  actual world-space bounds, computes scale from that, then re-measures
  and corrects position on all three axes (not just Y grounding, like
  the Big Tree fix — this pivot was off-center on X/Z too) from the real
  result. Landed centered exactly on the original position and grounded
  exactly where the old sphere touched down, confirmed via direct
  measurement, not assumption.
- **Credits tab actually has content now.** Ben caught, mid-playtest,
  that the Third-Party Credits ledger (`Assets/Models/
  THIRD_PARTY_CREDITS.md`) had been flagging this gap for two entries
  running without anyone actually closing it. `GameMenuScreen.
  DrawCreditsTab()` now shows the Tree branch and Stone CC-BY
  attributions (exact required text, Big Tree excluded — still
  comparison-only, not confirmed shipping) plus a new credits image
  (`Assets/Textures/CreditsImage.png`, from Ben's `tekim_trex.png`),
  centered horizontally above the existing "Tekim"/"the T-Rex" names.
- All three verified via scripted scene/asset read-backs (component
  presence, actual measured bounds, serialized field references) rather
  than just "it compiled" — same discipline as every other Editor swap
  this session.

### v0.1.85-dev — Despawn timer (120s) now covers everything a player drops, not just plain items

Ben's ask: shortened from the existing 15-minute plain-item despawn to
2 minutes, and extended to cover equipment/coins, which previously had
no despawn timer at all.

- **`Pickup.DespawnDelay`: 900s (15 min) → 120s (2 min).** Already
  existed for plain stackable items (manual Drop, hand-eviction
  fallback, Admin spawn) — just a number change.
- **New shared `Despawn.cs` component** — attached at runtime (not
  pre-authored) to anything without its own despawn concept. Investigated
  first and found a real gap: `PlayerDropping.DropFrom`'s equipment
  branch (Backpack/Belt/Canteen/etc. dropped from a container's move
  popup) and all 7 equippable carriers' own dedicated `Drop()` methods
  (the Equipment section's Drop button — a *separate* code path from
  `PlayerDropping`, confirmed by reading each one) never applied any
  despawn timer at all — a dropped Backpack would sit in the world
  forever. Same gap for dropped Coins (`PlayerCoinDrop`, fully custom
  spawn path, no `Pickup` involved).
- **Real risk caught and designed around, not just bolted on:** `Despawn`
  uses an absolute `Time.time` deadline, not elapsed active-time. That
  distinction matters specifically for equipment — `Stash()` deactivates
  the GameObject (pausing `Update()`), but a later re-equip reactivates
  it; a deadline already in the past would otherwise fire immediately on
  reactivation and destroy something the player is now *wearing*.
  Fixed by having every equippable's `Stash()` **and**
  `SetCarried(true, ...)` (both paths a pickup can end on, depending on
  where `PlayerLoot` lands it) destroy any `Despawn` component on
  themselves — new `Despawn.CancelOn(GameObject)` static helper, one
  line per call site rather than duplicating the get/null-check/destroy
  pattern 14 times across 7 classes.
- Dropped Coins get `Despawn` too, but need **no** cancellation logic
  anywhere — `Coin.Complete()` already destroys the whole GameObject
  outright on a successful pickup, so there's no "stashed then worn
  again later" lifecycle for a lingering timer to wrongly fire against.
- Verified via a clean full-project batch-mode recompile (all 12 touched
  scripts) and a `Grep` sweep confirming exactly 9 `AddComponent<Despawn>`
  attachment sites (7 carrier `Drop()`s + `PlayerDropping.DropFrom` +
  `PlayerCoinDrop.SpawnCoin`) and 14 `Despawn.CancelOn` cancellation
  sites (`Stash()` + `SetCarried(true, ...)` × 7 equippable classes) —
  matches the design exactly, not just "it compiles."

### v0.1.84-dev — Stick Pickup now grants 10 at once (playtest convenience)

Ben's ask: needed enough Sticks on hand to actually exercise the Trimmed
Stick tiers (5 recipes, each consuming 1 Stick, now also risking loss
entirely on a Bad/Spectacular chance-of-creation failure — see
v0.1.82-dev) without repeatedly walking back to re-gather one at a time.

- `TestScene.unity`'s "Stick Pickup" (`(2, 0.15, 3)`) now grants 10 per
  grab (`Pickup.quantity` 1 → 10), verified via a scripted scene
  read-back. "Stick Pickup 2" left untouched at 1, so there's still a
  normal single-Stick pickup to test that path too. Both still respawn
  after 180s, unchanged.
- Playtest convenience, not a balance decision — easy to revert or retune
  once actual playtesting is done.

### v0.1.83-dev — Tree chopping: Tree → Log(s) → Plank (+ chance of a Stick)

Ben's ask, reshaped an existing (undocumented-as-such) mechanic rather
than building from scratch: Tree.prefab already had a `ResourceNode` —
4 Axe hits, drop 3 Wood chunks, hide-then-respawn. Replaced with a real
two-stage chop, matching how Boulder → Rock → Small Rock already works
for stone.

- **New `Tree.cs`** (not a `ResourceNode` reuse — deliberately different
  shape): 3 Axe hits (down from 4) drop `logCount` (3) `Log` instances
  scattered nearby, then the tree visual swaps to a **Stump** child
  instead of fully disappearing. `Awake()` caches every `Renderer` under
  the object except the Stump's own as "the tree" — no need to hand-wire
  dozens of the procedural mesh's "Leaf Cluster" children individually.
  Stump regrows into a full tree after `regrowDelay` (180s, same number
  the old `ResourceNode` used) — decided in conversation to keep trees a
  renewable resource like every other `ResourceNode`, not a one-time
  removal.
- **`Log.prefab`** (new): a placeholder cylinder, physically scattered by
  the falling tree, chopped down like any other `ResourceNode` — 2 Axe
  hits → 2 `PlankChunk` (new `Plank` item, new `ItemDefinition`, plain
  untiered material) — **plus a new 30% chance of also dropping a Stick**
  (reusing the existing item/model rather than inventing a redundant
  "Branch" — Stick already got a real branch model this session,
  v0.1.73-dev). Trains Woodworking (refining raw wood), while the Tree
  chop itself still trains Gathering (raw extraction) — same discipline
  split as everything else.
- **`ResourceNode.cs` gained two small, generically reusable additions**
  (not Tree/Log-specific) to make the Log stage possible:
  - `bonusChunkPrefab`/`bonusChunkChance` — an optional chance-based
    extra spawn alongside the guaranteed `chunkPrefab`/`chunkCount`,
    rolled once per break. Unlike `CraftingRecipe.bonusItem` (always
    guaranteed), this one is a real roll — could be reused later for
    e.g. ore nodes occasionally yielding a gem.
  - `respawnDelay <= 0` now destroys the node outright instead of
    scheduling a respawn — needed since a `Log` is itself a one-off
    spawn from the tree, with no sensible "same spot" to respawn at the
    way a fixed Boulder/ore node has. Every existing `ResourceNode`
    already has a positive `respawnDelay`, so this is additive, not a
    behavior change for anything else.
- Verified end-to-end via a scripted read-back of the rebuilt
  `Tree.prefab` and `Log.prefab`/`PlankChunk.prefab` — confirmed the old
  `ResourceNode` is actually gone (not just added-alongside), the Stump
  starts inactive, and every field (tool list, skills, chunk counts,
  bonus chance) resolved to the intended value, not just that references
  parsed.

**First-pass numbers, not deeply tuned:** log count (3), Log's own hit
count (2), Plank yield (2), Stick chance (30%). Easy to retune later —
none of this shape depends on the exact values.

### v0.1.82-dev — Chance-of-creation: crafting can now succeed brilliantly, barely fail, or go badly wrong

Ben's framing ("I'm feeling mean"): every craft now rolls between five
outcomes instead of always just succeeding — a real risk/reward layer on
top of v0.1.80-dev's skill gate, using the same skill-margin math.

- **Five outcomes**, resolved after ingredients are already spent (a bad
  or spectacular failure is "the materials were wasted," not "the attempt
  silently didn't happen"):
  - **Brilliant success** — produces the *next tier up* (`CraftingRecipe.
    higherTierItem`, new field) instead of what was attempted.
  - **Success** — the intended item, as always.
  - **Barely fail** — produces the *next tier down*
    (`lowerTierItem`) instead.
  - **Bad failure** — materials lost, nothing produced.
  - **Spectacular failure** — materials lost, the held tool breaks (only
    for recipes with a real `requiredTools` list — just Trimmed Stick
    today; skipped everywhere else, per Ben's call), and 10 direct health
    damage via a new `PlayerVitals.Damage(amount)` (the game only had a
    healing API before this).
- **Both edge cases resolve to plain Success**, decided in conversation:
  Crude has no `lowerTierItem` (nowhere lower — "barely fail" just
  works), Masterwork has no `higherTierItem` (nowhere higher — "brilliant
  success" just works), and every single-tier item (Rope, Cloth, Crude
  Fiber Belt/Backpack, all 5 gadgets) has neither, so the same collapse
  applies uniformly without needing a separate "is this item tiered?"
  flag.
- **Odds scale with skill margin** — how far `trainedSkill`'s level is
  above the tier's `CraftTierScale.SkillRequirement`, capped at 20 points
  (`RiskMarginCap`). At margin 0 (just barely qualified): ~63% Success,
  ~20% Barely Fail, ~12% Bad Failure, ~3% Spectacular, ~2% Brilliant. At
  margin ≥20 (well-practiced): ~85% Success, ~4%/~1%/~0% for the three
  failure-side outcomes, ~10% Brilliant. First-pass numbers, not deeply
  tuned. Recipes with no `trainedSkill` (the 5 gadgets) skip the roll
  entirely and always succeed plainly, same as before this system
  existed.
- New `CraftingRecipe.lowerTierItem`/`higherTierItem` populated across
  all 25 existing ladder recipes (4 tools × 5 tiers + Trimmed Stick × 5)
  via a batch-mode script, verified against every single one (not just a
  sample) — confirmed Crude/Masterwork boundaries are null in the right
  direction and every middle tier points at its actual neighbors, plus a
  20,000-trial simulated-roll check at several margins confirming the
  live distribution matches the intended curve.
- Outcome messages (Brilliant/Barely-Fail-with-a-real-downgrade/Bad/
  Spectacular) show as a new on-screen message on `PlayerCrafting`, same
  pattern as `PlayerSkills`' skill-up toast but positioned just below it
  (`y=110`) so both can show at once without overlapping. Plain Success
  stays silent, same as before.

### v0.1.81-dev — Positive, randomized message when a skill improves — special line on tier unlock

Ben's ask, refined twice in conversation: echo a statement when a craft
skill improves (previously the only feedback was checking the Skills tab
manually), then made positive/celebratory and randomized rather than one
fixed line, then given a distinct message specifically for the moment a
gain unlocks a new `CraftTier` — the natural pairing with v0.1.80-dev's
skill-gated tiers.

- `PlayerSkills.GainExperience` fires on every gain that actually raises
  the level (silent at MaxLevel 100, where diminishing returns make
  `newLevel == current` — no false "increased" message once capped).
  Picks a random line from one of two pools:
  - **`MessageTemplates`** (6 variants, e.g. "Congratulations! You have
    increased your {skill} skill to {level}!") for an ordinary gain.
  - **`TierUnlockTemplates`** (2 variants per tier) instead, specifically
    when the gain crosses a `CraftTierScale.SkillRequirement` threshold
    for the first time (10/25/50/100 — Rudimentary/Normal/Fine/
    Masterwork). No entry for Crude — its threshold is 0, so there's
    never a real "just unlocked Crude" crossing to celebrate.
  - Crossing-detection compares `current`/`newLevel` directly (not just
    the rounded display), so a gain like 9.6 → 11.4 correctly fires the
    Rudimentary line even though neither endpoint is a whole number.
    Verified via a throwaway script simulating repeated gains and
    checking the exact message at each step, including that boundary.
- Drawn as a 3-second on-screen message, top-center at `y=70` — placed
  just below where `PlayerNavComputer`'s compass sits (`y=10` to `y=62`
  when worn) so the two never overlap regardless of whether a Navigation
  Computer happens to be equipped.
- No queue — a gain while a message is already showing replaces it and
  resets the timer, rather than stacking. Rapid repeat crafting (e.g. 10
  Crude Knives in a row) will mostly just keep refreshing the same
  message rather than showing 10 in sequence.

### v0.1.80-dev — Skill-gated crafting tiers (1/10/25/50/100)

Ben's call: use skill level 1, 10, 25, 50, 100 to denote the 5 `CraftTier`s
— crafting a given tier now actually requires having trained for it, not
just having the ingredients and (where applicable) a tool in hand.

- **Real deadlock caught and resolved before building:** skills start at
  0, and the only way to gain most disciplines today
  (Stonework/Woodworking/Sewing) is crafting the exact items this gate
  would restrict. Requiring Crude ≥ 1 would mean a fresh character could
  never craft a first item in that discipline at all — nothing else
  feeds these skills yet. **Resolved: Crude requires 0** (no real gate,
  identical to today's behavior); the curve applies starting at
  Rudimentary: Rudimentary 10, Normal 25, Fine 50, Masterwork 100.
- New `CraftTierScale.SkillRequirement(tier)` alongside the existing
  `Modifier(tier)`. New `PlayerCrafting.HasRequiredSkill(recipe)` checks
  `recipe.trainedSkill`'s current level against it — recipes with no
  `trainedSkill` (the 5 gadgets: Canteen/Sunglasses/Nav Computer/Health
  Monitor/Mining Face Shield) are completely unaffected, same pattern as
  `HasRequiredTool`. Wired into `TryCraft` (blocks the craft) and
  `CraftingScreen` (greys out the button, shows `— requires Stonework
  25`-style label).
- **Real bug caught before it shipped:** `Rope`/`Cloth` never had an
  explicit `tier` set on their `ItemDefinition`s, silently defaulting to
  `CraftTier.Normal` — would have required Sewing ≥ 25 just to make
  basic Rope, breaking the very recipes meant to build up Sewing from
  zero in the first place. Fixed by setting both explicitly to `tier: 0`
  — they're single-tier items with no real ladder, so "Crude" here just
  means "no gate," not a real quality claim.
- Verified via a scripted read-back of all 34 recipes in
  `PlayerCrafting.Recipes`, confirming every single one's resolved
  required-skill level (not just that the new fields parsed) — every
  Crude/Rudimentary/Normal/Fine/Masterwork tool and Trimmed Stick tier,
  Rope/Cloth, Crude Fiber Belt/Backpack, and all 5 gadgets.

**Immediate effect:** closes out the previously-documented "known,
expected placeholder behavior" that all 5 tiers of every tool were
craftable side by side with nothing gating the higher ones
(`TEST_FEATURE_PLAN.md`) — a fresh character can now only craft Crude
tools until Stonework reaches 10. **Still open:** the *ingredient*-quality
half of the weakest-link rule (every tier still costs identical
ingredients) — skill is the only thing gating tier today.

### v0.1.79-dev — Crude Fiber Belt / Crude Fiber Backpack: first real starter gear recipes

Ben's framing: now that Fiber exists, a real starter Belt and Backpack
should be craftable from it. First recipes ever to output actual
equipment (not a plain stackable), which required fixing a real
architecture gap along the way.

- **Fixed: `PlayerCrafting.TryCraft` couldn't produce a working
  equippable.** It always called `inventory.AddItem(...)` — a plain
  stackable add with no `.equipment` reference — so any equippable output
  would have landed as an inert, non-wearable stack. New
  `AddCraftedOutput` helper: if `outputItem.worldPickupPrefab` has an
  `IEquippable` component, instantiate it, `Stash()` it, and add it via
  `AddEquipmentItem` instead. Applies to `bonusItem` too, though nothing
  uses that combination yet. **Only fixes the crafting-output side** of
  the gap logged in `BUGS_AND_ENHANCEMENTS.md` — the Admin spawn tab's
  matching bug (`PlayerDropping.SpawnPickup`, a different code path) is
  still separately broken; corrected that entry rather than claiming both
  fixed.
- **`Belt` renamed to `Fiber Belt`** (guid unchanged, so the existing
  world pickup and `PlayerCanteen`'s belt-attachment logic didn't need
  touching) — establishes "Fiber Belt" as the ladder's base name per
  Ben's call, so future tiers read `Crude Fiber Belt` … `Masterwork Fiber
  Belt`, consistent with `CraftTierNames`.
- **New `Crude Fiber Belt`** — 8x Fiber → 1 Crude Fiber Belt, 2 attachment
  points (matches the already-decided Crude-tier point count), trains
  Sewing. First Belt tier to ever have a recipe.
- **New `Crude Fiber Backpack`** — a *distinct* item from the existing
  `Backpack` ladder (Ben's explicit call, not filling in the existing
  recipe-less `Crude Backpack`) — 15x Fiber → 1 Crude Fiber Backpack,
  capacity 4 (matches the existing Crude-tier capacity number). Trains
  Sewing.
- Both new items got real `.prefab` assets for the first time (previous
  world pickups were standalone scene GameObjects, not reusable prefabs)
  — `Assets/Prefabs/CrudeFiberBelt.prefab` /
  `Assets/Prefabs/CrudeFiberBackpack.prefab`, needed so
  `ItemDefinition.worldPickupPrefab` has something to instantiate.
  Placeholder flat-box visuals reused from the existing Belt/Backpack
  placeholders (`Backpack.mat` tint) — no dedicated art pass, and
  thematically a woven-fiber item probably shouldn't share a leather
  material long-term.
- Fiber costs (8 / 15) and Sewing `skillGain: 2` are first-pass numbers,
  not deeply tuned — same "reasonable starting point, adjust after
  playtesting" spirit as everything else in the tool tiers.

**Still open:** Rudimentary/Fine/Masterwork Fiber Belt and the
corresponding Fiber Backpack tiers aren't built — this pass only covers
the Crude "starter" tier of each, per the original ask. Leather sourcing
and the original (non-Fiber) Backpack ladder's recipes remain unbuilt too.

### v0.1.78-dev — Rope and Cloth recipes, both trained by Sewing

Next link in the Fiber chain, right after Fiber itself landed. Both new
recipes are pure Fiber refinement — no tool required, no byproduct.

- New `Rope.asset`/`Cloth.asset` `ItemDefinition`s — plain stackable
  materials (no tier), same shape as Fiber/Stick/Wood.
- `RopeRecipe`: 5x Fiber → 1 Rope. `ClothRecipe`: 10x Fiber → 1 Cloth.
  Both train **Sewing** (`skillGain: 2`, matching the flat rate every
  other base recipe uses) — the first two recipes to ever train that
  skill, which existed as an empty `SkillDefinition` since the discipline
  split (v0.1.70-dev) with nothing populating it. Already listed in
  `CraftingScreen`'s discipline tabs, so no scene wiring needed there.
- Both added to `PlayerCrafting.recipes` on the Player GameObject in
  `TestScene.unity` (32 recipes now, up from 30). Verified via a throwaway
  batch-mode script that opened the scene and read back
  `PlayerCrafting.Recipes` directly — confirmed ingredient counts, output
  items, and `trainedSkill` all resolved correctly, not just that the
  guids parsed.

**Still open:** Leather sourcing (implies hunting/animals — doesn't exist
at all yet), and the actual Backpack/Belt recipes now that Cloth (and
maybe Rope, for straps/drawstrings) exist as real ingredients. See
`BUGS_AND_ENHANCEMENTS.md`.

### v0.1.77-dev — Trimming a Stick now also yields Fiber

First real step on the Fiber → Cloth / Leather material chain flagged as a
blocker for Backpack/Belt recipes (`BUGS_AND_ENHANCEMENTS.md`). Ben's
framing: trimming a branch with a knife should realistically leave you
with some usable fiber, not just the trimmed stick.

- `CraftingRecipe` gained an optional secondary output — `bonusItem`/
  `bonusCount` (default null/1) — alongside the existing `outputItem`/
  `outputCount`. Guaranteed when set, same as every other recipe in the
  game; no randomness introduced. Most recipes don't need it and leave it
  unset.
- All 5 `TrimmedStick` recipes (Crude through Masterwork) now also output
  1 Fiber, flat across every tier — deliberately not scaled like the
  point/capacity curves elsewhere, since the Trimmed Stick recipes
  themselves are still identical scaffolding across tiers with no real
  differentiation yet.
- New `Fiber.asset` `ItemDefinition` — plain stackable raw material (no
  tier, no world pickup), same shape as Stick/Wood.
- `PlayerCrafting.TryCraft` checks space for and adds the bonus output
  alongside the primary one; `CraftingScreen`'s recipe list now shows it
  (`Trimmed Stick + 1x Fiber  (needs ...)`) and factors it into the
  "inventory full" check.
- No scene/prefab changes needed — this only touches recipe data and the
  crafting scripts, since Fiber has no physical world presence yet (only
  produced as crafting output).

**Still open:** this only answers "where does Fiber come from" — the
Fiber → Cloth refining step, a Leather source, and any real recipe for
Backpack/Belt are all still unbuilt. See `BUGS_AND_ENHANCEMENTS.md`.

## 2026-08-06

### v0.1.76-dev — Equip destination picker for multi-slot equippables

Ben's follow-up thought right after the Belt landed: now that Canteen can
go to Left Hand, Right Hand, *or* a worn Belt's attachment points,
clicking Equip silently picking whichever one the carrier tried first
isn't good enough — the player should see the real options and choose.
Same gap already existed for NavigationComputer/PersonalHealthMonitor
(Left Wrist or Right Wrist), just less noticeable with only 2 options.

- `PlayerCanteen`/`PlayerNavComputer`/`PlayerHealthMonitor` each gained
  `AvailableDestinations(item)` (every currently-free slot that would
  actually accept the item right now) and `EquipTo(item, destination)`
  (commit to one specific destination, instead of `Equip`'s old
  first-match-wins loop). `Equip(item)` is now a thin wrapper —
  `AvailableDestinations(item)[0]` through `EquipTo` — so existing callers
  keep working unchanged.
- Backpack/Belt/Sunglasses/MiningFaceShield **don't** get this treatment —
  each only ever has exactly one possible destination (Back/Waist/Face),
  so there's nothing to choose between; their Equip buttons still equip
  immediately, same as before.
- `InventoryScreen.cs`: new `TryEquipWithChoice` overloads (one per
  multi-destination type) replace the direct `.Equip()` calls at both
  click sites — the main inventory list, and a not-yet-worn item sitting
  in a hand slot in the Equipment section. 0 or 1 available destinations
  still equips immediately; 2+ opens a new popup (`DrawPendingEquipPopup`,
  same visual pattern as the existing "where should this go?" move popup)
  listing them as buttons.
- **Doesn't close** the two related, still-open gaps in
  `BUGS_AND_ENHANCEMENTS.md` ("No way to move an equipped item into a
  backpack" and "Equip directly from a container") — those are about
  different actions (moving an already-equipped item elsewhere, and
  equipping straight from a container's contents) that this popup doesn't
  touch. Related, not the same fix.

### v0.1.75-dev — Backpack retiered, new Belt equippable (design-only recipes)

Built the two pieces from the same design conversation logged in
`BUGS_AND_ENHANCEMENTS.md`: Backpack folded into the 5-tier `CraftTier`
ladder, and a new Belt equippable with generic attachment points. Neither
got a real recipe this pass — see "Still open" below for why.

**Backpack:**
- `Backpack.cs` gained an `itemDefinition` field (replacing the hardcoded
  `backpackName` string) so a physical Backpack instance can point at any
  tier's `ItemDefinition` — previously `PlayerBackpack` only ever
  recognized a single hardcoded `backpackItem`, which couldn't represent
  multiple coexisting tiers. `PlayerBackpack`'s Pick Up/Equip/Unequip/Drop
  now all read `backpack.ItemDefinition` instead.
- `RoughBackpackItem.asset` renamed to `BackpackItem.asset`, `itemName`
  "Rough Backpack" → "Backpack" (Normal tier, no prefix, per the existing
  `CraftTierNames` convention — same as `Rock Knife` → `Crude Knife`).
  Same guid, so the existing world pickup and `Assets/Prefabs/
  Backpack.prefab` (still orphaned/unreferenced, kept in sync anyway)
  didn't need re-linking.
- New `ItemDefinition`s for the other 4 tiers: `CrudeBackpackItem`,
  `RudimentaryBackpackItem`, `FineBackpackItem`, `MasterworkBackpackItem` —
  capacity table from the design conversation (4/6/8/12/16). **Data only
  for now** — no recipe, no world pickup, nothing can spawn them yet
  (see "Still open").

**Belt (new):**
- `Belt.cs` (new, mirrors `Backpack.cs`): worn at Waist, holds a fixed
  number of generic attachment points (`points = 6` for this Normal-tier
  instance) as its own `Inventory` rather than general storage — any
  attachment consumes exactly 1 point regardless of kind.
- `PlayerBelt.cs` (new, mirrors `PlayerBackpack.cs`): Pick Up/Equip/
  Unequip/Drop into the Waist slot.
- `PlayerCanteen.cs` reworked: a worn Belt now occupies the body's actual
  `Waist` `PlayerEquipment` slot, so a bare Canteen's fallback chain
  changed from `Left Hand → Right Hand → Waist` to `Left Hand → Right
  Hand → the equipped Belt's attachment points` (not a named
  `PlayerEquipment` slot, so this needed its own branch rather than
  reusing the old string-array slot list).
- `InventoryScreen.cs`: Equip/Drop buttons for a Belt sitting in the main
  inventory, Equip/Unequip/Drop + a nested contents side-column for the
  Waist slot (reusing the same `DrawContainerContents`/wornContainer path
  Backpack's Back slot already had — widened from Back-only to Back-or-
  Waist).
- New `BeltItem.asset` (Normal tier) + one world pickup (simple flat-box
  placeholder, no dedicated art this pass, tinted with the existing
  `Backpack.mat`) placed near Canteen's starter-gear spot at
  `(-2, 0.3, 1.5)`.

**Still open, deliberately not built this pass:**
- **No recipes for Backpack or Belt.** Ben's call mid-build: hold off
  until there's a real Fiber → Cloth textile chain and a way to source
  Leather, rather than faking it with placeholder ingredients (Stick/Wood)
  the way the tool tiers did. New backlog item in
  `BUGS_AND_ENHANCEMENTS.md` for that material chain.
- **Crafting can't produce a working equippable at all yet, independent of
  the above.** Discovered mid-build: `PlayerCrafting.TryCraft` always
  calls `inventory.AddItem(...)` — a plain stackable add with no
  `.equipment` reference — so even with a recipe, a "crafted" Backpack/
  Belt would land as an inert, non-wearable stack. Same root cause as the
  already-logged "Admin spawn tab can't spawn a working equippable
  gadget" bug. Logged as its own BUGS item; needs fixing before either
  recipe can actually work.
- **Only one worn container's contents show in the Inventory tab's side
  column at a time.** `InventoryScreen.DrawEquipmentSection`'s
  `wornContainer` is a single value, last-writer-wins across the
  `SlotOrder` loop — if both a Backpack (Back) and a Belt (Waist) are worn
  simultaneously, only the Belt's points render in the side column (Waist
  comes after Back in `SlotOrder`); the Backpack's contents don't disappear
  functionally, just visually. Pre-existing code only ever needed to
  support one worn container before Belt existed. Logged in
  `BUGS_AND_ENHANCEMENTS.md`.
- Only the Normal-tier Backpack is reachable in play today (the existing
  world pickup) — Crude/Rudimentary/Fine/Masterwork `ItemDefinition`s
  exist as data but have no spawn path (no recipe, not pre-placed, Admin
  spawn tab doesn't work for equippables either). Intentional — no point
  building world pickups/prefabs for tiers nothing can craft yet.
- Attachment types beyond a bare Canteen (Scabbard/Pouch/Holster) are
  still just the open design question already logged, not built.

### v0.1.74-dev — Backpack's visual replaced with an AI-generated model

First use of a Tripo3D API-generated (not just third-party CC-BY) model
actually wired into gameplay, and the first head-to-head test of API vs.
Tripo Studio web-UI output on the exact same prompt — see
`Tools/Tripo3D/README.md` for the full comparison writeup. The web UI
version came out rope-laced with no metal hardware; the API version (used
here) has a metal buckle and snap studs and reads more polished. Both
generated from the same "crude leather backpack" text prompt.

- Model: `Assets/Models/CrudeLeatherBackpack.glb` (Tripo3D API,
  commercial use included per API/pay-as-you-go terms — see
  `Tools/Tripo3D/README.md`).
- Replaces the 5-scaled-cube placeholder (Body/Flap/StrapLeft/
  StrapRight/Pocket) on both the scene's standalone "Backpack"
  GameObject in `TestScene.unity` (the actual functional one — equip/
  drop reparents this same instance under `BackpackAnchor` rather than
  instantiating from a prefab) and `Assets/Prefabs/Backpack.prefab`.
  Confirmed via guid search that the prefab is otherwise unreferenced
  anywhere in the project, but kept in sync in case that changes later.
- Scaled by 0.53 to match the old placeholder's measured height
  (renderer bounds, per the CLAUDE.md pivot/bounding-box gotcha); no
  rotation or centering offset needed — the new model was already
  centered on its local origin.
- Root transform's scale was already uniform `(1,1,1)` here, so none of
  the non-uniform-scale/collider-preservation care the Stick swap needed
  applied.

### v0.1.73-dev — Stick's visual replaced with a real branch model

First non-comparison use of an externally-sourced model in this project —
everything before this (the AI-generated berry bush, the Big Tree) was
placed for side-by-side visual review only, not actually wired into
gameplay. This one replaces the Stick item's placeholder box mesh
everywhere it appears: `Assets/Prefabs/StickPickup.prefab` (used when a
Stick is dropped or freshly spawned) and the two pre-placed "Stick
Pickup"/"Stick Pickup 2" world objects in `TestScene.unity` — confirmed
these were never actual prefab instances of `StickPickup.prefab` despite
looking identical, so both had to be updated independently, not just the
prefab.

- Model: "Tree branch by Poly by Google" (CC-BY, via Poly Pizza) —
  `Assets/Models/TreeBranch_PolyByGoogle.glb`, 610 vertices, single mesh.
  Attribution tracked in `Assets/Models/THIRD_PARTY_CREDITS.md`, still
  needs to land in `GameMenuScreen`'s Credits tab before release.
- The model's long axis was vertical (Y) on import; rotated 90° on X to
  lie flat along Z instead, then scaled so its length matches the old
  placeholder's (0.6). Real bug caught before it shipped: the affected
  GameObjects had a **non-uniform** root scale (`(0.1, 0.1, 0.6)`, sizing
  the old box mesh+collider together) — naively parenting the new model
  under that would have multiplied its own scale by the parent's
  non-uniform one, badly distorting it. Fixed by resetting each root's
  scale to identity and explicitly preserving the `BoxCollider`'s
  original world-space size on the collider itself instead of relying on
  transform scale to produce it.
- See CLAUDE.md's new bounding-box-placement gotcha (added earlier this
  session, prompted by the Big Tree sinking into the ground) — the same
  "don't assume an imported model's pivot/orientation matches what you
  expect" discipline applied here too, verified in-script before
  committing to the prefab/scene rather than eyeballed.

### v0.1.72-dev — Trimmed the default startup scene

Ben's planning pass: reviewed everything that spawns in `TestScene.unity`
at startup (29 named spawn points, later corrected to 33 actual root
objects once queried precisely) and cut it down to reduce clutter.

**Removed from `TestScene.unity`** (deleted outright, not disabled):
5 Coins (Copper/Iron/Silver/Gold/Platinum), Secret Wall, Navigation
Computer, Personal Health Monitor, Sunglasses, Mining Face Shield,
Silver/Gold/Platinum Ore Nodes, the larger Storage Box (Small Storage Box
kept), and 3 of the 4 Trees (1 kept). Backpack and Canteen were the two
gadgets explicitly kept as starter gear. Silver/Gold/Platinum Ore Nodes
specifically are needed again later for testing, not gone for good.

Verified via Unity's own `GetRootGameObjects()` (not raw YAML grep — Trees
are prefab instances and don't literally contain `m_Name: Tree` in the
scene file, which produced a false "0 Trees remain" alarm mid-task before
querying the scene directly resolved it): 33 → 16 root objects, exactly
matching the 17 removed.

### v0.1.71-dev — First material-web refining step: Stick + Knife → Trimmed Stick (trains Woodworking)

First real content in the **Woodworking** discipline tab, which has sat
empty since it was created a few hours earlier this same day — Stick
→(Knife, Woodworking)→ Trimmed Stick, straight from the material web in
`docs/design-brief.md`.

- **`CraftingRecipe` gained `requiredTools[]`/`requiredToolLabel`** — a
  recipe can now require a tool *held in a hand, not consumed*, on top of
  its normal consumed `ingredients`. Same "any tier counts" convention as
  `ResourceNode.requiredTools` (any of the 5 Knife tiers satisfies it, not
  just one specific tier). `PlayerCrafting` gained a `PlayerEquipment`
  reference and `HasRequiredTool()`; `TryCraft` checks it up front, and
  `CraftingScreen` greys out Craft and shows `— requires Knife in hand`
  when it's not met, same visual pattern as the existing
  materials/inventory-space gating.
- **5 new Trimmed Stick items + recipes** (Crude through Masterwork,
  Ben's call — full tier treatment from the start this time, not staged
  in as a single item first). Same "identical recipe across all 5 tiers"
  placeholder approach as yesterday's tool tiers: each costs 1 Stick +
  any Knife in hand, differing only in which tier's item comes out.
  Trains `Woodworking` (`skillGain: 2`, matching every other recipe's
  default).
- Both throwaway batch-mode runs hit a stale `bee_backend` lock from an
  earlier run that hadn't fully released — a Unity process sat idle for
  several minutes producing no output before failing. No project files
  were affected; killing the orphaned process and retrying compiled
  clean. Worth watching for again: if a batch-mode run goes unusually
  quiet, check for a lingering `Unity`/`bee_backend` process before
  assuming the run itself is broken.

### v0.1.70-dev — Discipline sub-tabs for Crafting/Skills, folder-tab styling, Crafting skill retired

Implements the discipline-sort model from today's earlier planning
conversation (see `docs/design-brief.md`'s 2026-08-05 Pipeline update) —
both the backend skill/recipe repointing and the UI to actually make a
25-recipe flat list navigable.

- **`SkillDefinition` gained a `category` field** (`SkillCategory`:
  Gathering / CraftingDiscipline / Combat) — which sub-tab of `SkillsScreen`
  a skill's level shows under.
- **6 new discipline skills**: Woodworking, Stonework, Metalworking,
  Forging, Minting, Sewing (all `CraftingDiscipline` category). `Gathering`
  migrated to the `Gathering` category (field didn't exist on it before
  today).
- **`Crafting` skill retired and deleted** (`Crafting.asset` removed —
  verified zero remaining references first, not just assumed). Every item
  now sorts into exactly one discipline by its defining material, so the
  generic catch-all no longer has anything left to cover. All 20 tool
  recipes (Knife/Hammer/Axe/Pickaxe × 5 tiers) repointed to **Stonework**
  — all four are stone-headed tools today. The 5 gadget recipes
  (Sunglasses, Nav Computer, Health Monitor, Mining Face Shield, Canteen)
  now train **no skill at all** (`trainedSkill = null`) rather than being
  force-fit into a discipline that was never designed for them — Ben's
  call, they were "just to test ideas up front anyway."
- **`CraftingScreen` sub-tabbed by discipline** — one tab per discipline
  skill (`disciplines[]`, an explicit hand-maintained list like
  `GameMenuScreen.ControlsList`, not discovered dynamically, so an empty
  discipline still gets its own tab with an honest "No recipes yet."
  placeholder) plus a fixed **Other** tab for the 5 no-skill gadget
  recipes.
- **`SkillsScreen` sub-tabbed by `SkillCategory`** — Gathering / Crafting
  Disciplines / Combat. Combat is permanently empty today (no weapon
  skills exist, no combat system to train them) — same honest-placeholder
  treatment as `GameMenuScreen`'s Audio/Graphics tabs.
- **File-folder tab styling** (`DebugGUI.TabSelected`/`TabUnselected`) —
  Ben's ask, applied consistently to all four tab bars in the game
  (`GameMenuScreen`, `PlayerMenuScreen`, and the two new sub-tab bars).
  The selected tab shares `DrawPanel`'s exact background color and sits
  flush against it (no border between tab and content); inactive tabs use
  a visibly darker, receded surface. Replaces the old bold-vs-plain-text
  distinction everywhere it was used. Pure procedural `GUIStyle`/solid-color
  textures, no imported graphics — first pass, will need the usual
  screenshot-feedback round to actually judge how it reads.

### v0.1.69-dev — Knife/Hammer/Axe/Pickaxe now come in all 5 CraftTiers

First implementation slice of the "next session" plan logged yesterday —
preceded by a planning conversation (see that plan's entry in
`BUGS_AND_ENHANCEMENTS.md` for the forks it resolved and why). Scope for
today, deliberately: get the data/UI scaffolding in place, not tune real
values. Spear and Bow are **not** part of this — deferred, since neither
has a function yet (no combat system) and Bow's designed recipe needs the
unbuilt Rope/Textiles chain; revisit once combat exists.

- **`ItemDefinition` gained a `tier` field** (`CraftTier`, defaults to
  `Normal`) — every item now has one, meaningful for the ones that
  actually come in a 5-tier ladder. Needed groundwork for the eventual
  weakest-link crafting rule, which has to read an ingredient's own tier.
- **Consolidated the 4 existing tools as the Crude tier**, not left as
  parallel duplicates: `Rock Knife`→`Crude Knife`, `Rock Hammer`→
  `Crude Hammer`, `Axe`→`Crude Axe`, `Pickaxe`→`Crude Pickaxe` (renamed via
  `AssetDatabase.RenameAsset`, so GUIDs — and every existing reference —
  stayed intact). Added the other 4 tiers per tool as new assets: 16 new
  `ItemDefinition`s + 16 new `CraftingRecipe`s, 20 of each total.
- **Recipes are intentionally identical across all 5 tiers of a tool**
  right now (Ben's call) — every tier costs the same ingredients as today's
  Crude version. There's no gate yet stopping you from crafting a
  Masterwork Knife as easily as a Crude one; that's expected, not a bug —
  the weakest-link rule that would actually enforce tier progression isn't
  built. Pure scaffolding for now.
- **All 20 recipes train the existing `Crafting` skill**, same as before —
  no new Woodworking/Stonework/Forging assets created. Raised during
  planning (Hammer alone plausibly touches Forging, Woodworking, *and*
  Stonework) and explicitly deferred rather than guess at a mapping nobody
  was confident in; easy to repoint later once the refining pipeline
  exists and settles which skill(s) each tool actually trains.
- **`ResourceNode.requiredTool` (single item) → `requiredTools[]` (any of
  these satisfy the gate) + `requiredToolLabel` (display string for the
  prompt).** Necessary fix, not optional: the old single-reference field
  would've only recognized *one* of the 5 Pickaxe/Axe tiers once they
  split, silently breaking ore/tree gating for the other four. Re-wired
  all 5 Ore Nodes (any Pickaxe tier) and `Tree.prefab` (any Axe tier) to
  the new array; Rock Node and Boulder correctly stay tool-optional
  (empty array).
- **`PlayerMenuScreen`'s Skills and Crafting tabs now scroll** — added
  ahead of the Crafting tab's recipe count jumping from ~9 to 25, which
  would otherwise run off the bottom of the screen. Inventory's tab keeps
  its own existing scroll view (pinned currency row) rather than getting
  double-wrapped.

### v0.1.68-dev — Admin tab: spawn any item in front of the player (Editor-only)

New **Admin** tab on `GameMenuScreen` (` key) — Ben's ask, queued up
yesterday as prep for testing tomorrow's batch of new craft-tier tools
without having to craft each one from zero first.

- New `AdminSpawnScreen`, holding the Admin tab's actual content (same
  split as PlayerMenuScreen's tabs each owning their own component).
  Lists every `ItemDefinition` asset in the project, alphabetized, each
  with a **Spawn** button that materializes one directly in front of the
  player.
- `PlayerDropping` gained a `SpawnPickup(item, count = 1)` method —
  extracted from the tail end of `DropFrom` (instantiate the item's
  `worldPickupPrefab`, or the generic fallback, and `Pickup.Configure` it)
  so Admin-spawning reuses the exact same "materialize a physical item"
  logic a manual Drop already uses, rather than duplicating it. `DropFrom`
  itself is unchanged in behavior, just calls the extracted method now.
- **Editor-only, deliberately:** the item list is discovered via
  `AssetDatabase.FindAssets("t:ItemDefinition")`, which only exists inside
  the Editor — auto-discovery means a newly-created item (like tomorrow's
  tool tiers) just shows up with nothing to remember to register, unlike
  `GameMenuScreen.ControlsList` or `PlayerCrafting`'s recipes array. The
  whole class is wrapped `#if UNITY_EDITOR` with a plain "Editor-only"
  message on the `#else` side, so a standalone build still compiles —
  this was never meant to ship, purely a testing aid.
- **Known gap, not fixed here:** the handful of `IEquippable`-carrier
  items (Backpack, Canteen, Sunglasses, Nav Computer, Health Monitor,
  Mining Face Shield) don't have a real `worldPickupPrefab` of their own
  (their physical form is a dedicated prefab, not the generic
  `Pickup`-based path) — spawning one here falls back to the generic
  dropped-item prefab and adds a plain, non-equippable stack rather than
  a working item. Not a blocker for tomorrow's tool work (those are plain
  stackable items), but worth a follow-up if the Admin tab needs to cover
  gadgets too.

### v0.1.67-dev — Worn backpack contents move to a side column in the Inventory tab

Ben's ask: the Equipment section's rows got hard to scan whenever a
Backpack was worn on Back, since its full contents grid rendered inline
directly under that row, pushing every later slot (Left Arm, Right Arm,
...) down by an unpredictable amount.

- `DrawEquipmentSection()` now returns the currently-worn container
  (`IInventoryHolder`, today only ever a worn Backpack) instead of drawing
  its contents inline. `DrawContent()` lays the equipment list and that
  container's contents out side by side (`GUILayout.BeginHorizontal`) —
  the equipment column stays a uniform single-column list regardless of
  what's worn.
- The Back row's box now just reads **"Equipped"** instead of the item
  name — the actual contents are visible right next to it in the new
  column, so repeating "Backpack" there was redundant.
- Side effect: this also removes the tight-spacing hazard the 2026-08-03
  fix (`SafeButton`, left-click-only) was originally guarding against —
  the contents grid no longer sits close enough beneath Unequip/Drop to
  make a stray click land on the wrong one. `SafeButton` itself stays, no
  reason to relax it.

### v0.1.66-dev — Darker panel background for readability

`DebugGUI.DrawPanel`'s shared background alpha raised from 0.65 to 0.92 —
Ben flagged the new Tab/` menus reading as too washed-out against a bright
sky. Since every screen (Bank, Lockbox, Inventory, Skills, Crafting,
GameMenuScreen, PlayerMenuScreen, plus the bottom-left debug HUD) draws
through this one shared 1x1 texture, the fix applies everywhere at once.

## 2026-08-04

### v0.1.65-dev — Consolidated Inventory/Skills/Crafting into one Tab-key Player Menu

New `PlayerMenuScreen`, toggled with **Tab** — same full-screen tabbed
pattern as `GameMenuScreen` (` key), four tabs: Player (blank, same
placeholder treatment as the ` menu's Player tab), Inventory, Skills,
Crafting. Replaces the three independently-hotkeyed screens (I/U/O) that
existed before — each one's own Update()/isOpen/OnGUI/hotkey was stripped
out and its content turned into a `DrawContent()` method that
`PlayerMenuScreen` calls into for whichever tab is active, so the
underlying logic (fields, dependencies, popups) didn't need to move, only
its screen chrome.

- `InventoryScreen` also gained `DrawPopups()` (its screen-centered move/
  coin-drop popups, drawn after `PlayerMenuScreen` ends its own full-screen
  area, only while the Inventory tab is active) and `ResetPopups()` (called
  when the whole menu closes, so a still-open popup doesn't stay stuck open
  next time it's reopened).
- Dropped the v0.1.50-dev 50%-`GUI.matrix`-scale boost on the Inventory
  content — the Tab menu is already a full-screen area, much larger than
  the old floating window that scale was compensating for. Flagged in
  `TEST_FEATURE_PLAN.md` to re-check readability; easy to reintroduce
  scoped to just that tab if it reads too small in practice.
- `GameMenuScreen.ControlsList` updated per the standing rule: removed the
  now-gone I/U/O rows, added a `Tab` row.
- `FirstPersonController` now holds a single `playerMenuScreen` reference
  (in place of the old `inventoryScreen`/`skillsScreen`/`craftingScreen`
  fields) in its Escape-close list.

### v0.1.64-dev — Full-screen tabbed game menu (` key): Player/Audio/Graphics/Controls/Credits

New `GameMenuScreen`, toggled with `` ` `` (backtick/grave) — same open/close/
cursor-lock convention as every other screen (only opens while the cursor is
already locked, so it can't stack on top of Inventory/Crafting/Skills/Bank/
Lockbox), wired into `FirstPersonController`'s Escape-close list alongside
them. First tabbed-navigation UI in the project — five tabs drawn as buttons
across the top of a full-screen panel, switching which section renders below.

- **Player** — deliberately left blank per explicit instruction, reserved for
  a future decision on what belongs here (Vitals? Skills? something else?)
  rather than guessing and having to undo it. No `PlayerVitals`/`PlayerSkills`
  dependency on the component at all right now, consistent with not adding
  code for something not actually used yet.
- **Audio** / **Graphics** — both honest placeholders ("no system exists yet
  — nothing to configure") rather than fake sliders that wouldn't control
  anything real. Neither an audio system nor a graphics/quality-settings
  system exists anywhere in the project yet.
- **Controls** — a flat, alphabetized (by key name, not grouped by category)
  reference list of every real key binding in the game today: `` ` ``, C, E,
  Escape, F, I, Left Mouse Button, Left Shift, Mouse Movement, O, Right Mouse
  Button, Space, U, WASD, X, Z. Per the request, this list is meant to be kept
  current — update `GameMenuScreen.ControlsList` whenever a new key mapping
  is added anywhere in the game.
- **Credits** — "Tekim" and "the T-Rex," exactly as given, placeholder for now.

### v0.1.63-dev — Fix: Rock/Small Rock chunks bouncing/rolling too far after breaking

User report: chunks scattered way farther than intended after breaking a
Boulder. Two compounding causes:

- `MediumRockChunk.prefab`'s `Rigidbody` had near-default damping (linear `0`,
  angular `0.05`) — never actually set when the prefab was created earlier
  tonight, just left at Unity's defaults.
- `RockChunk.prefab`'s existing damping (`0.5`/`0.5`) was tuned for its
  original Cube shape, which settles almost instantly once a flat face
  touches the ground regardless of damping — a Sphere (what it was swapped to
  a few versions ago this session) rolls far more freely with much less
  resistance at the same values, so the same damping that looked fine on a
  cube now lets it roll for a long distance.

Raised damping on both (`RockChunk`: 1.5/2, `MediumRockChunk`: 2/3) so chunks
still scatter with a visible initial burst but settle down quickly afterward
instead of continuing to roll. Also normalized Boulder's `scatterForce` from
`1.4` down to `1.2`, matching every other `ResourceNode` in the scene (it was
the one outlier).

### v0.1.62-dev — Boulder + Rock (new stone tier), Small Rock's chunk shape fixed

Ben pointed out `RockChunk.prefab` has always been a plain scaled Cube — more
noticeable now that the texture actually looks like rock. Explored shape
options (primitive clustering, a noise-displaced mesh, or a hybrid) and went
with the hybrid: a real displaced-sphere mesh (per-vertex random radial
displacement, not a primitive) for the main irregular silhouette, plus several
small clustered pebble spheres scattered on its surface.

- **`RockChunk.prefab`** (Small Rock's chunk, and Rock Node's broken-piece
  visual — same prefab, both uses) swapped from a Cube mesh/`BoxCollider` to a
  Sphere mesh/`SphereCollider`. Same prefab guid, so every existing reference
  (`Rock.asset`'s `worldPickupPrefab`, Rock Node's `chunkPrefab`, the
  `hiddenChunkPrefab` fallback on the disguised Silver/Gold/Platinum ore
  nodes) stayed valid with no further wiring needed.
- **New `Rock`** (file `MediumRock.asset`, item name "Rock") — a pure
  intermediate stage, same as Small Rock already is: never used directly in a
  recipe. Its chunk (`MediumRockChunk.prefab`) is the new hybrid shape: a
  0.35-radius displaced-sphere body plus 4 small pebbles.
- **New `Boulder`** — a world object (not an item; nothing to pick up
  directly) using the same hybrid technique at a bigger scale (0.9-radius
  body, 8 pebbles), placed in `TestScene` at `(-4, 0.6, 4)`. Breaks via the
  existing `ResourceNode`/`IPunchable` mechanic — bare-handed, no tool
  required, same as Rock Node (2 hits, yields 3 Rock).

**Scope boundary, deliberately not built here:** this only fixes the shapes
and wires Boulder → Rock through the *existing* punch-to-break mechanic. It
does not implement "Rock breaks down further into Small Rock" — that
mechanism (a recipe? a separate mineable object?) was discussed in concept
back when the tier was named but never concretely decided beyond "Rock is a
pure intermediate stage," so nothing was invented here to fill that gap. Also
doesn't touch the separately-planned randomized-size-on-spawn/yield-scaling/
duration-scaling design from that same conversation — this is shapes only.

**Safety net applied again** (same reasoning as the Tree's branching mesh):
can't verify the displaced-sphere triangle winding visually from this headless
session, so `RockChunk.mat`'s `_Cull` was set to `Off` — harmless on the
existing plain-primitive uses (Rock Node, Small Rock) too. Verified the full
guid chain (`MediumRock.asset` ↔ `MediumRockChunk.prefab`, `RockChunk.prefab`'s
new mesh/collider, `Boulder`'s `ResourceNode` fields) directly rather than
trusting the generator's success log, plus a clean duplicate-fileID scan and a
clean batch-mode compile.

### v0.1.61-dev — Fix: all 5 ore textures rendered as solid color blobs, not flecked rock

User screenshots (in-game, without and with the Mining Face Shield equipped)
showed the v0.1.60-dev ore nodes as near-solid colored spheres — reddish-brown,
green — instead of grey rock with metal flecks, and Silver/Platinum appeared not
to reveal at all when the shield was equipped.

Root-caused by reading the actual generated PNGs directly rather than guessing
from the in-game screenshots alone (`CopperOreTexture.png` was, in fact, a nearly
flat solid green image). Two compounding problems, found via standalone test
swatches inspected before touching any real asset:

1. **The real bug:** `Mathf.SmoothStep(low, high, rawNoiseValue)` — used for
   every fleck-coverage mask — doesn't threshold anything the way GLSL's
   `smoothstep(edge0, edge1, x)` does; Unity's version treats its third argument
   as an already-normalized `[0,1]` progress value and the first two as the
   *output range*, not threshold edges. The call was silently remapping every
   pixel into a narrow output band uniformly, never producing sparse flecks
   regardless of what threshold values were tried — confirmed by testing three
   different threshold pairs that all looked nearly identical, which is what
   exposed the real bug rather than a tuning problem. New gotcha documented in
   `CLAUDE.md` with the correct GLSL-style replacement (`SmoothThreshold`).
2. Also darkened every rock-matrix color palette — contrast alone (tested first,
   before finding the SmoothStep bug) didn't fix it, since some fleck colors
   (Silver's near-white especially) were already close to the original "light"
   rock color even before any blending.

All 5 ore textures (`CopperOreTexture.png`, `IronOreTexture.png`,
`SilverOreTexture.png`, `GoldOreTexture.png`, `PlatinumOreTexture.png`)
regenerated in place with the corrected math — same file paths/guids as before,
so no material or scene changes were needed. Verified by reading each
regenerated PNG directly before considering it fixed, not just by re-running the
generator and trusting the log.

**Flagged, not yet fixed:** the sky texture's cloud coverage (v0.1.55–57-dev)
used the identical buggy pattern — very likely the real explanation for why
clouds stayed faint across three tuning rounds that session. Noted in
`BUGS_AND_ENHANCEMENTS.md`'s sky entry for whenever that gets revisited.

### v0.1.60-dev — Full ore ladder (Iron/Silver/Gold/Platinum) + Mining Face Shield

First real implementation slice out of tonight's planning doc — the hidden-ore
detection mechanic from the Crafting, Gathering & Skills Pipeline section, built
in full (visual reveal *and* yield gating, not just the visual half).

- **Iron, Silver, Gold, and Platinum Ore Nodes** added (Copper already existed),
  each with its own procedurally generated texture (same tileable-noise technique
  as grass/sky/rock — a shared `GenerateOreTexture` helper this time, just
  different color palettes per metal) and its own chunk prefab/item, mirroring
  `CopperOreChunk.prefab`'s structure exactly. Placed in `TestScene` near the
  existing Copper Ore Node.
- **Iron stays visible**, same as Copper. **Silver, Gold, and Platinum are
  hidden** — they render as plain `RockChunk.mat` (indistinguishable from an
  ordinary Rock Node) until the player has a **Mining Face Shield** equipped, at
  which point `ResourceNode` swaps their material to the metal's true texture.
  This is the *exact* reveal mechanism already shipped for Sunglasses + the
  Secret Message Wall, generalized from a pure visual effect into one with a real
  gameplay consequence.
- **Yield gating, not just visual:** `ResourceNode` checks whether the node is
  revealed *at the moment it actually breaks* (not when punching started) —
  mining a hidden node without the shield yields `hiddenChunkPrefab`
  (`RockChunk.prefab`, i.e. plain Small Rock, the ore undetected and lost);
  with the shield on, it yields the real ore. New `ResourceNode` fields:
  `hiddenMaterial`, `revealedMaterial`, `hiddenChunkPrefab` — all null by default,
  so every previously-shipped node (Rock Node, Copper Ore, Tree) is completely
  unaffected; only a node that explicitly sets all three opts into this behavior.
- **New `MiningFaceShield`/`PlayerMiningFaceShield`** — structured identically to
  `Sunglasses.cs`/`PlayerSunglasses.cs` (single Face-slot equippable, same
  pickup/equip/unequip/drop chain, same `WornEquipment`-layer-while-worn fix from
  the `CLAUDE.md` equippable checklist), minus the screen-tint overlay — its
  effect is read externally via a new `IsWorn` accessor instead of drawn by the
  component itself. Wired into `InventoryScreen` as a sixth equippable type,
  following the existing Backpack/Canteen/NavComputer/HealthMonitor/Sunglasses
  pattern exactly (both `DrawInventorySection` and `DrawEquipmentSection`).
  Craftable (2 Small Rock + 1 Stick, trains Crafting), and one is placed in
  `TestScene` as a world pickup near the other wearable gadgets.

**Applied a lesson from earlier tonight's stale-reference bug directly:** every
asset-creation step in this run's generator script returns only a path (a plain
string, immune to Unity's object-reference staleness), never an object reference
— the final scene-wiring step opens the scene once and re-fetches *everything*
fresh via `AssetDatabase.LoadAssetAtPath` right there, rather than trusting
anything carried in from earlier in the script. Verified every single new/changed
guid reference directly against its target's `.meta` guid (item↔chunk-prefab both
directions for all 4 new ore types, hidden/revealed material and hidden-chunk
references on all 3 disguised nodes, the shield-item reference on
`PlayerMiningFaceShield`, and the full `PlayerCrafting.recipes` array for stray
nulls) before trusting the script's own success log — none were stale this time.
Also a clean duplicate-fileID scan on the resaved scene and a clean batch-mode
compile.

### Design planning: Mining skill decided, ore byproducts, hidden-ore detection (docs only)

Follow-up planning pass on the pipeline written up earlier tonight. Resolved the
previously-deferred `Mining` skill split from `Gathering`: Mining now owns all
ore-node gathering specifically (Gathering stays scoped to Sticks/Berries/plain
Rock). Added three new pieces to the Metal line in
`docs/design-brief.md`'s Crafting, Gathering & Skills Pipeline section: ore nodes
yield Small Rock alongside their primary ore (mining a vein realistically kicks
loose waste rock too); base ore yield scales down Copper→Platinum so the ladder
has real teeth; and a new **Mining Face Shield** (Face-slot equippable) reveals
Silver/Gold/Platinum nodes that otherwise look like plain rock — same reveal
mechanism already shipped for Sunglasses + the Secret Message Wall, generalized
into a real gameplay system, with a Mining-skill-tier-4 bypass once a player
doesn't need the gear anymore. Updated the `BUGS_AND_ENHANCEMENTS.md` pointer
entry to match. Still docs-only — nothing implemented, no version bump.

### Design planning: full crafting/gathering/skills pipeline (docs only, nothing built)

Extended planning conversation (not a build session) working out the "still open"
gap flagged when the five `CraftTier` names were first decided: what actually
determines an item's tier. Landed on a weakest-link rule (the lower of current
skill level and material quality), then kept expanding — a full gather → refine →
assemble material web across wood, stone, metal, and textiles; 6 new skills
(Woodworking, Stonework, Metalworking, Forging, Minting, Sewing); tool-quality
effects (yield/quality/speed); and a new click-once-and-locked interaction model
intended to replace the current punch-to-break mechanic entirely.

Written up in full in `docs/design-brief.md`'s new **Crafting, Gathering & Skills
Pipeline** section, with a pointer entry added to `BUGS_AND_ENHANCEMENTS.md`.
**Decided in shape, not in exact numbers** — several sub-questions are explicitly
still open (see that section). Nothing in this plan is implemented yet; no game
code, scenes, or prefabs changed in this entry, so no version bump.

### v0.1.59-dev — Rock texture, Copper Ore, and tool-gated gathering (Pickaxe/Axe)

Three-part request: give the rocks a real texture instead of flat grey, add a
Copper Ore resource, and add a couple of craftable tools. Design decisions
confirmed up front rather than assumed: tools (Pickaxe + Axe) actually gate
gathering — a Pickaxe must be held in a hand to mine Copper Ore, an Axe to chop
Trees — and Copper Ore is gathered the same punch-to-break way Rock Node
already works.

- **Rock texture.** Same tileable-noise technique as the grass/sky textures
  (`CHANGELOG.md` v0.1.53-dev onward) — a mottled grey stone texture applied to
  `RockChunk.mat`'s `_BaseMap`, which is shared by every loose Small Rock pickup
  *and* every chunk scattered from breaking Rock Node (they were already the same
  prefab). Also fixed a design inconsistency found along the way: Rock Node's own
  sphere had its own separate **embedded scene material** (created directly via
  `new Material(...)`, serialized inline into `TestScene.unity` rather than as a
  project asset) with no texture — repointed it to the same `RockChunk.mat` asset
  so the whole node and its broken chunks now visibly match.
- **`ResourceNode` gained an optional `requiredTool` (`ItemDefinition`) field.**
  Null (default) means punch bare-handed works, exactly Rock Node's existing
  behavior — nothing about it changed. When set, `OnPunch` checks
  `PlayerEquipment.HasInHand(requiredTool)` (new method — true only if the item is
  actually held in a hand right now, not just carried in inventory/a backpack)
  before registering the hit at all. `Prompt` also changes to `"Punch to break
  (requires X)"` when a tool is required, so the requirement is visible before
  ever swinging.
- **Copper Ore** — new `ItemDefinition`, a mottled-rock texture with scattered
  copper-orange flecks and rare green patina spots (same layered-noise approach,
  new color mapping), a `CopperOreChunk` prefab (mirrors `RockChunk.prefab`:
  scaled Cube, `Rigidbody` `ContinuousDynamic`, `Pickup`), and a new "Copper Ore
  Node" placed in `TestScene` at `(2, 0.4, -4)` — `ResourceNode` with
  `hitsToBreak: 2` (tougher than Rock Node's 1) and `requiredTool` set to
  Pickaxe.
- **Pickaxe and Axe** — plain, non-equippable `ItemDefinition`s (`maxStack: 1`,
  no custom `worldPickupPrefab` — falls back to the generic dropped-item cube,
  same deliberate choice Rock Hammer already made) craftable via two new
  recipes: Pickaxe (2 Small Rock + 1 Stick), Axe (1 Small Rock + 2 Stick), both
  training Crafting +2, added to `PlayerCrafting.recipes` on the Player.
- **Trees are now harvestable.** `Tree.prefab` gained a `ResourceNode` component
  directly on its trunk root (reuses the exact same hide/respawn logic Rock Node
  already has — `GetComponentsInChildren<Renderer>()` already correctly sweeps up
  the foliage children too, no changes needed there): `hitsToBreak: 4`,
  `requiredTool` set to Axe, yields a new **Wood** item via a new `WoodChunk`
  prefab. Previously the tree prefab (v0.1.58-dev) was purely decorative with no
  way to interact with it at all.

**A real bug found and fixed during this work, worth its own note — see the new
"asset references can go stale across `LoadPrefabContents`/`UnloadPrefabContents`"
gotcha in `CLAUDE.md`:** the first version of the generation script silently wrote
`requiredTool: {fileID: 0}` on the Copper Ore Node and failed to add the two new
recipes to `PlayerCrafting.recipes` at all — no exception, no compile error, the
script logged success. Root cause: those specific references were created earlier
in the script and used again *after* an unrelated `PrefabUtility.LoadPrefabContents`/
`UnloadPrefabContents` cycle (adding the `ResourceNode` to `Tree.prefab`), which
appears able to silently invalidate some in-memory asset references — a new,
not-fully-understood sibling to the already-documented `OpenScene` staleness
gotcha. Caught only by directly grepping the saved scene YAML for the expected
guids rather than trusting the script's own success log, and fixed with two small
follow-up scripts that re-fetched the references fresh via `AssetDatabase.LoadAssetAtPath`
immediately before use.

Verified end-to-end: every new/changed guid reference cross-checked directly against
its target asset's `.meta` guid (not just assumed from the script's intent), a
duplicate-fileID scan on the twice-resaved scene (clean), and a final clean
batch-mode compile.

### v0.1.58-dev — Procedural branching tree, real mesh geometry (not primitive composition)

Asked whether tree models could be procedurally generated; offered a choice
between combining stock primitives (Backpack.prefab's existing technique) or
actually generating trunk/branch geometry in code — went with the latter for
a more organic, less "blocky" result.

`GenerateTree.cs` (throwaway, run via batch mode then deleted) builds a tree
via recursive branching: starting from a single trunk segment, each branch
splits into 2–3 children at a random angle within a 32° cone of its parent's
direction (with a slight upward bias so branches don't droop after several
recursive levels), shrinking in length/radius each generation, 4 levels deep
(66 segments total this run — the exact shape is seeded, so it's
reproducible, not different every time the script runs). Each segment is a
tapered-cylinder (hexagonal cross-section, 6 sides) built from real vertex/
triangle data via a from-scratch `AddCylinderSegment` — not `CreatePrimitive`
— all combined into a single `Mesh` asset (`Assets/Data/TreeTrunkMesh.asset`).
Foliage stays simple: 2–3 scaled Sphere primitives clustered at each of the 41
terminal branch tips, colliders removed from the foliage spheres so they
don't block movement/interaction the way the trunk does.

**Risk mitigation, not guesswork:** this session can't render or screenshot
locally, so there was no way to visually confirm the hand-written cylinder
triangle winding order was actually correct — getting it backwards would make
the trunk invisible from outside (only visible from inside, due to backface
culling) with no compile error to catch it. Rather than gamble on it, verified
`_Cull` is a real property on this project's URP/Lit materials first (grepped
an existing `.mat` file), then set it to `Off` on `TreeBark.mat` — the trunk
renders regardless of which way the winding turned out, at the cost of
trivial double-sided overdraw on a low-poly mesh.

New `Assets/Prefabs/Tree.prefab` (mesh + bark material + non-convex
`MeshCollider`, matching `Ground`'s static-collider pattern — no `Rigidbody`
involved) with 4 instances placed in `TestScene`, each with randomized
Y-rotation and a small scale variance (0.85×–1.25×) so four copies of the
same mesh don't look identical, scattered clear of the existing object
cluster near spawn and the Secret Wall. Verified end-to-end: asset files on
disk, exactly 4 real `PrefabInstance` roots linked to `Tree.prefab`'s guid in
the scene (not the much larger raw match count from per-property
modification entries), and a clean duplicate-fileID scan on the resaved
scene.

**Still needs an in-Editor look** — same limitation as the sky work: can't
confirm visually from here whether the branching silhouette actually reads as
tree-like, whether the culling safety net was even necessary, or whether 4
levels/66 segments is too sparse or too dense at actual in-game scale.

### v0.1.57-dev — Fix: sky gradient direction was inverted, clouds still not reading as shapes

Second round of user-screenshot feedback on the sky. v0.1.56-dev's gradient
rendered backwards from intent — a deep blue band sat right at the horizon,
fading to pale going *up* — the opposite of both the code's intent (`Horizon`
pale, `Zenith` deep, blended by an assumed v=0-at-nadir/v=1-at-zenith mapping)
and of real atmospheric haze (pale near the horizon from more scattering,
deeper blue overhead). Strong, clean evidence `Skybox/Panoramic`'s actual
v-axis runs opposite to what was assumed. Rather than keep guessing the exact
convention, flipped `vEff = 1 - v` and used it everywhere instead of raw `v` —
corrects the observed symptom regardless of the precise underlying cause. This
also explains why clouds stayed barely visible in v0.1.56-dev: the cloud band
(meant to fade in right at the horizon) was very likely landing near the true
zenith instead — exactly where a level-pitched camera never looks.

Also sharpened the cloud shapes themselves: the one cloud visible in the
previous screenshot was a soft blurry brightening, not a distinct shape.
Narrowed the coverage threshold (0.46–0.58, was 0.42–0.62) for crisper edges,
and weighted the coarsest noise octave more heavily (0.65/0.25/0.10, was
0.55/0.30/0.15) for bigger, more clearly cloud-shaped blobs instead of fine
speckle blurring the outline.

`SkyTexture.png` regenerated in place again — same file path/guid, `Sky.mat`
and `TestScene`'s skybox reference needed no changes, reverified via the guid
chain.

### v0.1.56-dev — Fix: sky clouds barely visible from a normal camera angle

User screenshot from a roughly level-pitched first-person view showed the
v0.1.55-dev sky as an almost flat pale wash — no visible cloud shapes, no
visible horizon-to-zenith blue gradient either, just faint streaks. Not a
shader-compatibility problem (no pink, confirming the `Skybox/Panoramic`
choice was fine) — a content-tuning problem in the generated texture itself.
Two likely causes, both addressed (can't render/screenshot locally to isolate
which dominated):

- **Low color contrast.** `Horizon` (0.75, 0.85, 0.95) and `CloudColor`
  (0.97, 0.97, 1) were close enough to blend into each other rather than read
  as distinct shapes. Made `Horizon`/`Zenith` more saturated blues and
  `CloudColor` pure white.
- **Narrow visible band, coarse noise.** A level-pitched camera most likely
  only ever sees a narrow slice of the texture's v-range near the horizon
  (v≈0.5) — the old noise's coarsest octaves (period 5/10/20 across the
  *entire* pole-to-pole 0–1 range) put very little variation inside any
  narrow slice that close to a single value, so that band looked almost
  uniform regardless of contrast. Doubled every octave's period (10/20/40)
  so a narrow near-horizon slice still crosses enough lattice cells to show
  real variation. Also moved the cloud band's fade-in from starting at
  v=0.35 to v=0.45 (clouds now reach full strength right at the horizon,
  where a level camera actually looks) and lowered/widened the coverage
  threshold for denser, easier-to-spot clouds.

`SkyTexture.png` regenerated in place (`AssetDatabase.ImportAsset` reimport,
same file path/guid) — `Sky.mat`'s `_MainTex` reference and `TestScene`'s
`RenderSettings.skybox` needed no changes, verified by re-checking the guid
chain after regeneration.

### v0.1.55-dev — Procedural cloudy sky, replacing the built-in default skybox

Same technique and same request as the grass ground texture (v0.1.53/54-dev),
applied to the sky. `TestScene`'s `RenderSettings.m_SkyboxMaterial` was pointing at
Unity's built-in `Default-Skybox` (a `Skybox/Procedural` material, referenced via
its all-zero-except-`f` built-in-resource guid) — the plain blue gradient visible
in every prior screenshot, no clouds.

Before writing any code that sets shader properties, ran a throwaway inspection
script logging `Skybox/Panoramic`'s actual properties/defaults via `ShaderUtil`
rather than assuming names from memory — this project has hit "guessed shader
property, silently no-op'd or rendered pink" before (see `CLAUDE.md`'s URP gotcha
notes), and this shader turned out to have both a `_Mapping` and a separate
`_Layout` float property that aren't obviously distinguishable without checking.
Confirmed `_MainTex` is the texture slot, and `_Mapping`/`_ImageType` already
default to exactly what a standard equirectangular panorama needs (Latitude-
Longitude / 360 degrees) — so only `_MainTex` needed setting; `_Tint`/`_Exposure`/
`_Rotation` stay at their neutral shader defaults.

`GenerateSkyTexture.cs` (throwaway, run via batch mode then deleted) generates a
2048×1024 equirectangular texture: a `Horizon`→`Zenith` blue vertical gradient,
plus scattered white clouds from the same tileable value-noise function as the
grass texture — except only wrapped horizontally (`LatticeValue` wraps the U/
longitude coordinate into a period before hashing, same seamless-by-construction
trick, but leaves V/latitude unwrapped since top and bottom are poles, never
adjacent to each other and never need to tile). Cloud coverage is thresholded
(`SmoothStep`) rather than a smooth haze, so it reads as scattered clouds against
clear sky rather than uniform overcast, and fades out near the exact zenith and
below the horizon so clouds don't cap the sky or dip into ground-level view.

Created `Assets/Data/Sky.mat` (new `Skybox/Panoramic` material) and repointed
`TestScene`'s `RenderSettings.skybox` at it via `EditorSceneManager`/
`RenderSettings.skybox` in the script, rather than hand-editing the scene YAML —
verified afterward by cross-checking guids end-to-end (scene → `Sky.mat` →
`SkyTexture.png`) and a duplicate-fileID scan on the resaved scene (clean).

### v0.1.54-dev — Fix visible tiling grid in the grass texture with genuinely seamless noise

User feedback with a screenshot: v0.1.53-dev's grass read as an obvious repeating
checkerboard/waffle grid in play, not natural grass — worse than the "faint seams"
limitation that entry called out. Root cause was two-fold: `Mathf.PerlinNoise` gives
no periodicity guarantee at arbitrary frequencies, so every one of the 1,600 tile
repeats (40×40) had a visible seam *and* showed the exact same low-frequency blob
shape, which is what the eye actually locks onto as "a grid" — the seam alone
wasn't the main problem.

Rewrote the generator with a custom tileable value-noise function: a
`LatticeValue(x, y, period, seed)` hash that wraps `x`/`y` into `period` *before*
hashing, so sampling one full period to the right/down lands on the identical
wrapped lattice point — adjacent copies of the texture flow together with zero
seam by construction, not approximation. `TileableNoise` smoothstep-interpolates
between four such lattice corners. Layered three octaves (periods 5/10/20) for the
large mottled patches, same color gradient as before, plus one more (period 60)
for the fine blade-detail brightness variation.

Also reduced the material's UV tiling from 40×40 to 20×20 and doubled the source
texture to 1024×1024 — fixing the seam alone still leaves the exact same tile
repeating identically at every step, and cutting the repeat count in half reduces
how many chances the eye gets to pattern-match that repetition, independent of the
seamless fix. `Ground.mat`'s `_BaseMap` guid is unchanged (same file path,
re-imported in place), only `m_Scale` and the PNG's pixel content changed.

### v0.1.53-dev — Procedural grass texture on the Ground, replacing the flat green color

First texture-image asset in the project — everything until now was a flat
`_BaseColor` on a primitive. `Ground.mat` (`Universal Render Pipeline/Lit`) had no
`_BaseMap` assigned at all, just a solid green color; asked how to get a "realistic
grass" look, offered three routes (procedural in-engine, a supplied/downloaded
texture, or just explaining the steps) — went with the procedural route.

Throwaway `Assets/Editor/GenerateGrassTexture.cs` (run via batch mode, then
deleted, per the project's established workflow) generates a 512×512
`Texture2D`: three-octave `Mathf.PerlinNoise` for large mottled dark/mid/light
green patches (blended through a `DarkGreen`→`MidGreen`→`LightGreen` gradient),
plus a higher-frequency noise layer multiplied in as brightness variation to fake
individual-blade detail on top of the smooth patches. Noise sampling is offset
away from the origin — `Mathf.PerlinNoise` always returns exactly 0.5 at integer
coordinates, which otherwise shows up as a visible low-frequency grid artifact.

Saved as `Assets/Textures/GrassTexture.png`, imported with `WrapMode.Repeat` +
mipmaps + Bilinear filtering, then wired onto `Ground.mat`'s `_BaseMap` with
`m_Scale` (tiling) set to `(40, 40)` — `Ground` is a Plane scaled `(10, 1, 10)`
(100×100 world units), so 40 repeats puts each tile at 2.5 units, close enough
for the blade-detail noise to actually read at ground level without the pattern
looking like an obvious repeating grid from a distance. `_BaseColor` reset to
white so it no longer multiplies against (and darkens) the texture's real colors.

**Known limitation:** the noise isn't seamless at the texture's own edges (no
wrap-around blending was added), so at a tiling factor this high, faint repeat
seams may be visible up close on flat, uninterrupted ground. Good enough for a
first pass; a proper tileable-noise version (or a real photo-sourced texture)
would be the next step if the seams read as distracting in actual play.

### v0.1.52-dev — Fix: version number clipped off the bottom-left debug panel

The panel's `Rect` (height 56, positioned `Screen.height - 66`) was sized for 2
label lines back when it only showed Speed/Sprinting and the version. The Stance
line was added later (stance-system work, this session's merge) without resizing
the panel, so with 3 lines the bottom one — the version number — overflowed
`GUILayout.BeginArea`'s bounds and got clipped. Resized to fit 3 lines (height 76,
`Screen.height - 86`), keeping the same 10px margin on every edge.

### v0.1.51-dev — Canteen fill dead zone, and a misclick that dropped/unequipped the backpack instead of the item inside it

Third playtest pass on the water-source/inventory work above.

- **Standing close enough to see the Fill prompt didn't mean close enough to
  actually fill.** `Canteen.fillRange` (2m, measured from the canteen) was smaller
  than `PlayerInteraction.interactRange` (3m, measured from the camera) — the F/E
  prompt could be visible while `HasNearbyWaterSource()` still failed, silently, via
  both the direct F-key interaction and the pre-existing UI Fill button. Raised
  `fillRange` to 4m so it always exceeds `interactRange` with headroom. Same Unity
  serialization gotcha as the overdrink threshold: `TestScene.unity`'s Canteen
  instance had `fillRange: 2` baked in from before the new default existed, so the
  scene value needed its own fix alongside the code default.
- **Clicking an item inside a worn backpack sometimes dropped or unequipped the
  backpack instead.** Two independent reports (a Canteen click dropped it, a Rock
  click unequipped it into the main inventory) — root cause was layout, not logic:
  `DrawEquipmentSection`'s Back-slot row (Label + backpack box + Unequip/Drop
  buttons) sits directly above `DrawContainerContents`' item grid with almost no
  vertical gap, and the grid's `GUILayout.Space(20)` indent doesn't line up with the
  row above it — a middle slot in the grid (confirmed: slot 3) can horizontally
  align under the Unequip/Drop button column. Combined with Unity's `GUILayout.Button`
  responding to *any* mouse button (not just left-click, a long-standing IMGUI
  quirk), a right-click aimed at an item could land on the backpack's own
  Unequip/Drop instead. Fixed two ways: added a 6px gap between the row and the
  grid (reduced frequency but didn't eliminate it — confirms the diagnosis), and,
  more robustly, added an `InventoryScreen.SafeButton` helper that requires an
  actual left-click, applied to every Equip/Unequip/Drop button in the screen. A
  stray right-click can no longer trigger any of them regardless of exact pixel
  alignment.
- **`PlayerDropping.DropFrom` was still the one unguarded generic-removal path.**
  Flagged in `CLAUDE.md`'s gotcha note earlier this session as a latent risk (every
  current call site happened to guard it correctly, but the function itself had no
  check of its own). It's what the move popup's "Drop" option calls — now checks for
  an `equipment` reference first and releases the real object via `SetCarried(false,
  null)` instead of stripping the reference and spawning a fake pickup, matching the
  `InventoryTransfer.Move` fix from the previous entry.

### v0.1.50-dev — Inventory window scaled 50% larger for readability

Same request and same fix as the Bank window in v0.1.49-dev: scaled the whole
`GUI.matrix` by 1.5x around screen center in `InventoryScreen.OnGUI`, covering
the panel, the scroll view, and both popups (move destination, coin drop)
automatically since they draw later in the same `OnGUI` call.

One wrinkle Bank didn't have: `InventoryScreen`'s panel height was already
screen-responsive (`Mathf.Min(Screen.height - 40f, 700f)`) to avoid overflowing
shorter displays. Left unadjusted, scaling that already-capped height by another
1.5x could push the panel off the top/bottom of a smaller window. Divided the
on-screen height budget by `UiScale` before applying the existing cap
(`Mathf.Min((Screen.height - 40f) / UiScale, 700f)`) so the *post-scale* result
still respects the original margin instead of the pre-scale one.

### v0.1.49-dev — Bank window scaled 50% larger for readability

User feedback: the Bank window was hard to read. `GUILayout` uses fixed pixel
widths throughout (`GUILayout.Width(90)` etc.), so just growing `BankScreen`'s
outer panel `Rect` would only have added empty padding around the same small
text and buttons — not actually fixed the readability complaint. Instead scaled
the whole `GUI.matrix` by 1.5x around the screen center at the top of `OnGUI`
(restored at the end), which grows the panel, its text, its buttons, and both
popups (Deposit/Withdraw, Exchange — drawn later in the same `OnGUI` call, so
the scale already applies to them too) proportionally together, all still
centered on screen. `LockboxScreen` wasn't touched — this request was scoped to
the Bank window specifically.

### v0.1.48-dev — Dropped items despawn after 15 minutes

First slice of the item-holding-redesign backlog entry: just the despawn timer, not
the pickup-priority/unequip-fallback rework (still open, needs its own pass).

`Pickup` gained a `despawnAt` countdown (15 minutes, `DespawnDelay`) started inside
`Configure(item, quantity)` — which turns out to be called from exactly one place,
`PlayerDropping.DropFrom`, so it fires for every item the player actually drops
(manual Drop button, and the hand-eviction fallback `PlayerLoot` uses when both
hands are full with no backpack equipped) without needing a separate flag to
distinguish "dropped" from "world-placed" pickups. World-placed pickups (Sticks,
Berry Bush) and `ResourceNode`'s scattered chunks never call `Configure`, so they're
unaffected — they keep whatever `canRespawn` behavior they already had. Deliberately
a distinct timer from `canRespawn`/`respawnDelay` (3 minutes) — that one restores a
resource point in place; this one deletes a dropped item outright once nobody's
picked it up.

**Scope note:** doesn't cover the five equippables (Backpack/Canteen/Sunglasses/
NavigationComputer/PersonalHealthMonitor) — their `Drop()` methods detach an
already-existing physical object rather than instantiating a new `Pickup`, so they
don't go through `Configure` at all. Revisit once/if the equipped-item unequip-
fallback drop path (still unbuilt) needs the same timer.

### v0.1.47-dev — Fix: bank/lockbox popups let the coin type switch mid-transaction

Reported by Ben (filed in `BUGS_AND_ENHANCEMENTS.md`, commit `08d3c89`). In both
`BankScreen.cs` and `LockboxScreen.cs`, the coin-type table underneath a
Deposit/Withdraw (or Exchange) popup stayed fully clickable while the popup was open
— a click that landed on the table instead of the popup silently reassigned
`pendingType`/`pendingExchangeFrom` and reset the pending amount back to 0, so a
withdrawal could switch to a different coin type mid-flow without the player
intending it. Fixed by disabling (`GUI.enabled = false`) every background button on
the panel — coin table, Exchange buttons, Lockbox Buy row, Close — for the duration
any popup is open, consistent with the modal role those popups already play.

### v0.1.46-dev — Second playtest pass: canteen still white, overdrink threshold wrong, Sunglasses orphaned, direct water-source interaction

Merged with Ben's parallel session (v0.1.36-dev through v0.1.44-dev below), which had
already claimed the v0.1.34-dev/v0.1.35-dev numbers for unrelated work before either
session saw the other's commits — this entry and the one below were renumbered up
from their original local v0.1.35-dev/v0.1.34-dev to land after that chain instead of
colliding with it.

The v0.1.45-dev fixes below (originally v0.1.34-dev locally) didn't fully hold up
under a second round of testing —
three of the four "fixed" items had a real bug still hiding underneath, plus one
brand-new feature request.

- **Canteen full but still showing white.** Root cause was never the material/color
  logic added in v0.1.45-dev — it was that `Canteen.cs` looked up its `Renderer` with
  a plain `GetComponent<Renderer>()`, but the prefab's actual mesh renderers live on
  child objects ("Body"/"Cap"), not the root the script sits on. `rend` was `null`
  the entire time, so `UpdateVisuals()` was silently a no-op regardless of fill
  state. Switched to `GetComponentsInChildren<Renderer>()` and apply the tint to all
  of them. Also found the project uses URP, and `Canteen.mat` is a URP/Lit material —
  `Material.color` only reliably touches `_Color`, which URP/Lit doesn't render from
  (`_BaseColor` does); added a `SetTint`/`GetTint` helper that sets both so the color
  change is guaranteed to actually render regardless of shader.
- **Overdrink sickness threshold was wrong.** Implemented in v0.1.45-dev as ">100%
  thirst triggers sickness", but the actual intended design (confirmed by user) is
  "125% is the safe ceiling, sickness triggers only above it." Moved
  `overdrinkSicknessThreshold` from 100 to 125, and raised the `Restore(Thirst)` cap
  from 125 to 150 — without that headroom, thirst could never actually exceed 125
  through drinking and the sickness threshold could never trigger at all. Also found
  the threshold change alone wasn't enough: `TestScene.unity` had
  `overdrinkSicknessThreshold: 100` serialized directly onto the Player's
  `PlayerVitals` component from before this field existed at its new default — Unity
  doesn't retroactively apply a changed C# default to an already-serialized value, so
  the scene was silently overriding the code back to 100. Fixed the scene value
  directly.
- **Sickness could reduce health to 0 with no warning and no actual recovery.**
  Thirst was only draining at the slow ambient rate (~0.14/s) while sick, but
  sickness damage runs at 5 health/s — health hits 0 in 20s, long before thirst could
  ever drain down to the 50% recovery line. Added `overdrinkThirstRecoveryPerSecond`
  (10/s, vomiting/sweating out the excess) so sickness is now actually self-limiting
  and recoverable, and added a "SICK: Overdrank water!" warning to the Vitals HUD
  (`PlayerHealthMonitor`) via a new bold-red `DebugGUI.Warning` style — previously
  there was no UI indication sickness was even happening.
- **Sunglasses moved from a backpack to a hand became permanently unequippable.**
  Same root cause as the Canteen orphaning bug from v0.1.45-dev, still present
  despite that entry's changelog description — see the `CLAUDE.md` gotcha section for
  the full story of why the earlier fix didn't actually ship. `InventoryTransfer.Move`
  now detects an `equipment`-backed slot and preserves the reference across the move
  instead of stripping it.
- **New: interact directly with a water source, no inventory screen needed.** Added
  `ISecondaryInteractable` — a small additive extension to the existing single-key
  (E) interact system that lets an object also offer a second action bound to F,
  shown in the prompt alongside the first (e.g. `[E] Drink    [F] Fill Canteen`) only
  when there's actually a second option available. `WaterSource` now implements both:
  E always offers Drink (works with no carrier equipped); F offers Fill and only
  appears when the player has a water carrier equipped that isn't already full.
  `PlayerCanteen` needed an `Equipped` accessor added for this — unlike
  Sunglasses/PersonalHealthMonitor it never had one, since a canteen has no dedicated
  "worn" slot (holding it in a hand or at the waist is what equipped means for it).

### v0.1.45-dev — Fix bugs from playtesting: canteen visuals, fills from anywhere, overdrinking, equipment transfer

Five bugs found during canteen/backpack testing, rooted in several underlying issues:

- **Canteen visual feedback missing when dropped.** Canteen didn't change appearance
  based on liquid state — added material swapping (blue when filled, gray when empty)
  via `UpdateVisuals()` called after Fill/Drink. Materials are asset references with
  fallback to null if not assigned.
- **Canteen fills from anywhere on the map.** No location check existed — added
  `IWaterSource` interface and `WaterSource` component so only world objects marked as
  water sources allow filling. Canteen.Fill now checks `HasNearbyWaterSource()` within
  `fillRange` (2m default) before allowing a fill. Created `WaterSource.cs` as a simple
  marker component for placement in the scene.
- **Overdrinking mechanic not implemented.** Player could drink past 100% thirst with
  no consequence. `PlayerVitals` now allows Thirst to be restored up to 125% (changed
  `Mathf.Min(100f, ...)` to `Mathf.Min(125f, ...)` in Restore). When thirst exceeds
  `overdrinkSicknessThreshold` (100), player takes `overdrinkSicknessDamagePerSecond`
  (5 default) health damage. Sickness clears once thirst drops back to
  `overdrinkRecoveryThreshold` (50). Stored `isOverdrunkSick` state to gate the logic.
- **Moving items from a backpack/container orphans held equippables.** Root cause:
  `InventoryTransfer.Move` uses generic `RemoveItem`/`AddItem` which strip equipment
  references, leaving the real Canteen/Backpack physically attached but with no
  inventory slot referencing it (and no Fill/Drink/Equip buttons). This was a known
  gotcha already documented in `CLAUDE.md`. Added guard at the top of `Move()`: if any
  slot holding the item has `equipment != null`, refuse to move it (return false).
  Equipment-type items must route through type-specific handlers
  (PlayerCanteen.Equip/Unequip/Drop, PlayerBackpack.Equip/Unequip/Drop) instead.
- **Right-click-to-drop stick in backpack removes backpack.** Likely same root cause as
  the orphaning bug above — when the move popup tried to shift the item via the generic
  path, equipment references were stripped. The guard added to `InventoryTransfer.Move`
  should now prevent this by refusing the move entirely.

### v0.1.44-dev — Five crafting-quality tiers decided; purchasable coin Lockboxes
Updated `docs/design-brief.md`'s Phase 1 wishlist with the five decided
crafting-quality tier names — **Crude, Rudimentary, (no adjective —
Normal), Fine, Masterwork** — superseding `game-overview.md`'s
never-reconciled "Crude/Standard/Mastery" three-tier mention. New
`CraftTier` enum + `CraftTierNames` (the display-name prefix helper —
Normal gets none) + `CraftTierScale` (suggested capacity/price modifiers:
0.2×/0.5×/1×/2×/5×, chosen so every tier's numbers come out a clean whole
number off the Normal baseline).

New `Lockbox` — personal coin storage, purchasable from the bank in any of
the five tiers. Normal holds 2,500 of each coin type for 10 Gold; the
other four tiers scale both capacity and price by the same
`CraftTierScale` modifier (Crude: 500/2g, Rudimentary: 1,250/5g, Fine:
5,000/20g, Masterwork: 12,500/50g). Unlike `PlayerBank`, each Lockbox is
its own world object with its own balances — buying two doesn't pool
their capacity.

New `LockboxScreen` (E to open a specific Lockbox, no hotkey — same
reasoning as the bank) shows wallet vs. that box's balance per coin type
with Deposit/Withdraw. Deposit is capped by the box's remaining capacity
for that type; Withdraw is capped by both what the box holds *and* what
the wallet has room for (`PlayerCurrency.MaxBalance`) — pulling 1,000 Gold
isn't possible if the wallet can't hold that much even if the box does.
Neither direction charges the bank's 3% fee — purchasing a Lockbox isn't
one of the fee-bearing deposit/withdraw/exchange transactions, and moving
coins into your own already-purchased box is closer to personal storage
(like a `StorageBox`) than a bank transaction.

`BankScreen` gained a Lockbox shop section — Buy per tier, greyed out
below the Gold price — and now takes the `BankBox` it was opened from so
a purchased Lockbox spawns 2m in front of *that* box rather than the
player.

### v0.1.43-dev — Global bank: deposit, withdraw, exchange (Phase 3 commerce, early)
New `PlayerBank` — a global account (no per-branch ledger; any `BankBox`
reads/writes the same balances) separate from `PlayerCurrency`'s carried
wallet, with no cap unlike the wallet's 250. Clarified the exchange ladder
with the user before building it: a clean ascending 10:1 chain
(Copper→Iron→Silver→Gold→Platinum, matching both the design brief and the
`CoinType` enum order) — what was actually typed in the request would have
made Copper worth the same as Silver.

**Fee model:** every Deposit/Withdraw/Exchange charges `max(1, ceil(3% of
amount))`, but as an *extra* cost on the source side rather than skimmed
off the transferred total — depositing 100 costs 103 from the wallet and
the bank receives exactly 100, not 97. Chosen over skimming because it
keeps every transaction's *destination* amount exact and predictable, and
generalizes cleanly to Exchange (which also has a fixed 10:1 output ratio
that a skimmed fee would make fractional). `Exchange` operates on the
wallet, not the bank balance — bring physical coins to the counter, walk
away with different ones — and rounds an upgrade's input down to the
nearest clean multiple of 10 rather than ever producing a fractional coin.

New `BankBox` (`IInteractable`, E to open) and `BankScreen` — unlike
Inventory/Crafting/Skills there's no hotkey, since a bank is a place you
have to be at. Lists wallet vs. bank balance per coin type with
Deposit/Withdraw buttons, plus 8 Exchange buttons (up/down each of the 4
adjacent pairs) — all four routed through the same stepper-button quantity
popup pattern the coin-drop feature established, showing a live fee/total
preview before confirming.

`PlayerBank.Awake` seeds a starting bank balance of 25 Gold, separate from
`PlayerCurrency`'s existing starting wallet purse. One `Bank Box` placed
5m from the Small Storage Box in `TestScene`, with a new navy `BankBox.mat`.

Notably, this is a Phase 3 "Commerce system" feature (per the design
brief) built well ahead of Phase 1 completion — see the MVP status
comparison from earlier this session. Still missing from that section:
trading between players, central banking in cities, and the volatile gem
market; this covers the personal deposit/withdraw/exchange piece only.

### v0.1.42-dev — Regular movement drains stamina too, not just sprinting
Previously, walking (Standing, moving, not getting the sprint bonus) held
stamina flat — no drain, no regen. It now drains at a new, slower rate
(`PlayerVitals.walkStaminaDrainPerSecond`, 2/s vs. sprinting's 10/s) via a
new `IsWalking` flag `FirstPersonController` sets alongside `IsSprinting`
each frame, same pattern. This also covers holding Shift below
`SprintStaminaThreshold` (85%) — no speed bonus there, but it still counts
as active movement, not resting.

The 85% sprint-drain cutoff was already implicit (`CanSprint` requires
`stamina >= 85`, so `IsSprinting` — and its drain — turns off the instant
stamina crosses below it) — confirmed that's still exactly the behavior,
just with the new walk-drain now taking over below that point instead of
stamina holding flat. Regen is unchanged: still only stopped, kneeling,
crawling, or prone.

### v0.1.41-dev — Drop coins from the currency row
Clicking a coin box in `InventoryScreen`'s currency row now opens a
quantity popup (`DrawCoinDropPopup`) instead of doing nothing — stepper
buttons (±1/±10, "All") rather than a slider, matching this screen's
existing button-only popups and giving exact control a slider wouldn't at
a 250-coin scale. **Drop** spends that many via the new
`PlayerCoinDrop.DropCoins`.

New `PlayerCoinDrop` builds each dropped coin procedurally
(`CreatePrimitive(Cylinder)` + the matching material + `Rigidbody` +
`Coin`) rather than needing a prefab per type — `Coin` gained a
`Configure(type, amount)` method for this, the same pattern
`Pickup.Configure` already uses for generic dropped items. Coins spawn
individually (one `Coin` object per unit dropped, not a single
stack-of-N) at a small random horizontal offset in front of the player
and get a small physics impulse — same "scatter" approach
`ResourceNode.OnPunch` already uses for rock chunks — so a multi-coin drop
bounces apart on landing instead of stacking identically. Rigidbody set
to ContinuousDynamic from the start (see
[[gridless-ground-tunneling]]).

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
