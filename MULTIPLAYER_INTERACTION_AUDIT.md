# Multiplayer Interaction Audit & Plan

Written 2026-08-28, the same night as two long live two-machine test sessions with
traskmi (`v0.3.207-dev` through `v0.3.209-dev`) that found and fixed 15+ real
multiplayer bugs, one at a time, as they were hit by accident during play. Ben's ask
afterward: scan the whole codebase and compare it against what real multiplayer
actually needs, so the *next* batch of these isn't found the same accidental way.

This is a survey and a plan, not a build session — nothing in this doc had been
fixed yet beyond what the live-test sessions already shipped (see `CHANGELOG.md`'s
`v0.3.207/208/209-dev` entries and `BUGS_AND_ENHANCEMENTS.md`'s "Real live
two-machine session" entry for that work), **until the same night's follow-up ask
("fix all 5 items found") worked through most of the "Recommended build order"
below — see `CHANGELOG.md`'s `v0.3.210-dev` entry for the full detail.** Status per
item, updated in place rather than re-written: `GardenPlot`/`GardenPlot4x4` fixed,
`Door` fixed, `Lockbox` fixed (with a real, separate "never a real prefab" gap
found and flagged, not fixed), `PlayerCurrency`/`PlayerBank`/`Coin`/`PlayerIdentity`'s
rename-cost gap all fixed together (they were interdependent — syncing Currency
alone without fixing its remaining direct callers would have been actively
harmful, per this doc's own original warning), `Furnace`/`Campfire` partially
fixed (the `isServer` guard — the single highest-severity finding in this whole
doc — landed; the larger screen-driven-Command-routing half did not, see below).
**`PlayerReading`/`PlayerWriting` (build-order item 6) fixed in two more
follow-up passes the same night, `v0.3.212-dev`/`v0.3.213-dev`** — see
`CHANGELOG.md` for full detail; both closed a real permanent-unlock-lost-
on-disconnect risk, plus two more instances of the unsynced-`HashSet` gap
found directly in their own call paths (`PlayerCrafting.bookGrantedRecipes`,
`PlayerMagic.bookGrantedWishes`).

## Two failure classes, not one

Everything found across both nights collapses into exactly two distinct bug
shapes. Knowing which one applies tells you what the fix looks like.

**Class A — "runs entirely client-local, the server never finds out."**
A `MonoBehaviour` (not `NetworkBehaviour`) implementing `IInteractable`/
`ISecondaryInteractable` mutates its own state and spawns loot directly from
`Complete()`/`CompleteSecondary()`, called straight off `PlayerInteraction`'s
local raycast dispatch. On a host this is invisible (server and client are the
same process). On a real remote client, the mutation and any `Instantiate()`'d
loot only ever exist on that one machine — `NetworkSpawnHelper.SpawnIfNetworked`
checks `NetworkServer.active`, which is false there, so nothing actually spawns
over the network. Fixed 5 times tonight: `ChoppableTree`, `ResourceNode`,
`BerryBush`, `HerbBush`, and (a narrower single-call-site case) `WaterSource`.

**Class B — "runs server-side correctly, but the result never syncs back to the
owning client."** A `NetworkBehaviour` component's real state lives in a plain
field, not a `[SyncVar]`/`SyncList`. The server's own copy updates correctly
(often via a `[Command]` elsewhere), but a genuine remote client's own copy —
what their own screen reads from — never hears about it. Fixed 6 times tonight:
`PlayerInventory.syncedSlots` (never read back client-side), `PlayerIdentity
.playerName`, `PlayerSkills.levels`, `PlayerMagic.knownLineages`,
`PlayerEquipment.syncedSlots` (same unread-broadcast gap, harder since a slot
holds a live object not just a count), `PlayerFame.fame` (harder than the
others — none of its ~9 call sites were Command-routed at all, so a naive
`[SyncVar]` would have reintroduced Class A in a new form).

A third, smaller pattern showed up once (`WaterSource.CompleteSecondary`
calling `.Equipped.Fill(...)` directly instead of the already-correct
`RequestFill()`): a proper Command-routed entry point exists elsewhere in the
codebase, but one specific call site was never migrated to use it. Worth
checking for during any future audit pass — it's cheap to fix once found, but
invisible to a pure grep-for-`[Command]` sweep since the correct mechanism
*does* exist, just isn't used everywhere it should be.

## The proven fix recipe (Class A)

Applied identically 5 times tonight, each independently verified:

1. Convert the `MonoBehaviour` to `NetworkBehaviour`.
2. Any visible state (broken/lit/available/on-cooldown) becomes a
   `[SyncVar]` (with a `hook` if it drives a renderer/collider toggle, plain
   otherwise). Any real-time timer (`respawnAt`, built on `Time.time`) stays a
   plain server-only field — `Time.time` is per-process, never meaningful to
   broadcast.
3. `Update()` gets an `if (!isServer) return;` guard around whatever drives
   that timer, so only the server ever decides when state changes — added
   *after* any purely-cosmetic per-client logic that's fine to keep running
   everywhere (e.g. `ResourceNode`'s disguise-material swap, which reflects
   the *local* player's own equipped Mining Face Shield, not shared state).
4. `Complete()`/`CompleteSecondary()` gets a dual-path dispatch: if the object
   has a `NetworkIdentity` and `NetworkClient.active`, route through a new
   `[Command(requiresAuthority = false)]` (using Mirror's `NetworkConnectionToClient
   sender = null` parameter to identify the calling player, since these are
   server-owned scene objects with no client authority the way a Player-owned
   component has) that resolves the caller and re-invokes the real logic,
   renamed `ServerComplete(GameObject player)`. Otherwise call `ServerComplete`
   directly (keeps solo/offline testing with no network session working
   unchanged).
5. Every `Destroy(gameObject)` call site becomes network-aware: `NetworkServer
   .Destroy(gameObject)` when `NetworkServer.active`, plain `Destroy()`
   otherwise.
6. The prefab (and, separately, every already-placed scene instance) needs a
   real `NetworkIdentity`. **Critical lesson learned the hard way tonight**:
   adding one via a batch script and trusting Mirror's automatic scene-object
   `sceneId` assignment is not reliable — the very first attempt (`ChoppableTree`
   /`Tree.prefab`) produced a real regression when each real Editor session
   (Ben's, traskmi's) independently self-healed a mismatched `sceneId` with
   its own random value. The fix used for every subsequent case (`ResourceNode`,
   `BerryBush`, `HerbBush`): force every scene instance's `sceneId` to `0`,
   invoke `NetworkIdentity`'s real (private, called via reflection)
   `OnValidate()` so exactly one canonical value gets generated, record it as
   a real prefab-instance override (`PrefabUtility
   .RecordPrefabInstancePropertyModifications`), and independently re-verify
   in a *second* Unity process (open the saved scene fresh, confirm every
   instance has a non-zero, unique `sceneId`) before trusting it.

This whole recipe is mechanical enough that it's worth writing as a genuine
reusable throwaway-script template next time it's needed, rather than
hand-writing it fresh — the `FixResourceNodeNetworkIdentity.cs`/
`VerifyResourceNodeFix.cs` shape from tonight (both deleted after running, per
this project's own convention) is close to that template already.

## The proven fix recipe (Class B)

Two flavors, depending on whether the field's mutation is already
Command-routed somewhere:

- **Already Command-routed elsewhere** (`PlayerSkills.levels` via `GainExperience`,
  increasingly called from inside other Commands): add a `SyncList` mirroring
  the field by stable string ID (`ItemDatabase`/`SkillDatabase.IdFor`/`.Find`,
  since Mirror can't sync a raw `ScriptableObject` reference), server-`Update()`-
  polls a signature to detect real changes and refresh the list, client
  subscribes via `syncedX.Callback` and reconciles into its own local copy.
  **For anything shaped like a running total that can also be mutated by a
  still-unconverted local-only path** (`PlayerInventory`'s own case, once
  `Pickup` prefabs without `NetworkIdentity` are still legal), the
  reconciliation must be a signed *delta* between old and new synced totals,
  never a destructive clear-and-rebuild — a real regression found and fixed
  tonight (`Inventory.ApplyStackableDelta`) after a clear-and-rebuild
  reconciliation silently deleted a Skill Book a client had picked up through
  the still-local-only path.
- **Not yet Command-routed anywhere** (`PlayerFame`'s `Grant()`, called
  directly by ~9 different UI/gameplay call sites): don't touch the call
  sites. Make the *mutating method itself* dispatch through a Command when
  `!isServer`, and apply directly when it already is. Every existing caller
  keeps working completely unchanged; the routing decision lives in exactly
  one place.

## Audit findings — full survey, 2026-08-28

195 scripts in `Assets/Scripts/`. 46 already `NetworkBehaviour`, 93 plain
`MonoBehaviour` (the rest are interfaces/enums/`ScriptableObject`s/static
utilities with no base-class relevance here). Every `NetworkBehaviour` script
was checked for `[Command]`/`[SyncVar]` presence; every `IInteractable`-family
`MonoBehaviour` was checked for the Class A shape.

### Fixed across both nights (2026-08-27/28) — no further action needed here

`PlayerInventory`, `PlayerIdentity`, `PlayerSkills`, `PlayerMagic`,
`PlayerEquipment`, `PlayerFame`, `PlayerVitals` (one call site),
`ChoppableTree`, `ResourceNode`, `BerryBush`, `HerbBush`, `WaterSource`,
`SkinnableCreature`, `StorageBox` (+ one pre-existing scene instance's missing
`NetworkIdentity`), `VillageFlag`, `Pickup` (despawn consistency), `PlayerTeam`
(built with persistence from the start this session).

### Confirmed Class A, not yet fixed — same recipe as above applies directly

Checked via direct grep for `[Command]` presence (zero, in every case) and
reading each interaction method's actual body:

- **`Furnace.cs`** — real nuance, not a direct match to the recipe above: the
  primary interaction (`Complete()`) just opens `FurnaceScreen`, it doesn't
  mutate anything itself. The *real* mutations (`ToggleQueue`, `SetAutoRun`,
  fuel/materials/output box wiring) all happen from `FurnaceScreen`'s own
  button clicks, calling `Furnace`'s public methods directly — meaning the fix
  shape here is "route `FurnaceScreen`'s button handlers through Commands
  targeting the `Furnace`'s own `NetworkIdentity`," not a single `Complete()`
  dispatch. **Worse, and higher priority than the interaction gap**:
  `Furnace.Update()` has no `isServer` guard at all (confirmed directly) —
  the actual real-time smelt-progress simulation currently runs
  independently, uncoordinated, on *every* machine that has the object
  loaded, not just "doesn't sync," a real risk of outright inconsistent
  results (e.g. two machines disagreeing about whether a smelt completed).
- **`Campfire.cs`** — identical shape to `Furnace.cs` in every respect
  (screen-driven mutation, no `isServer` guard on its own `Update()`
  simulation loop). Same fix shape, same priority.
- **`GardenPlot.cs`/`GardenPlot4x4.cs`** — closer to the standard recipe:
  `Complete()` directly calls `TryPlant`/`Harvest`, real mutations, zero
  Commands. Growth itself is time-based (`elapsed >= GrowDurationSeconds`) —
  needs the same `Time.time`-is-per-process consideration as every fixed
  Class A case.
- **`Door.cs`** — `CompleteSecondary()` toggles `isOpen` directly, no sync at
  all. A real remote client's own view of a door's open/closed state would
  never match anyone else's. Also directly relevant to the NPC-navmesh gap
  `CLAUDE.md` already documents (`Door.OpenForNPC`) — worth doing both at
  once rather than revisiting this file twice.
- **`BankBox.cs`/`Lockbox.cs`** — same un-networked shape `StorageBox` had
  before its own fix. Not yet checked in as much depth as the others above
  (lower play-frequency than Furnace/Campfire/GardenPlot), but the `Commands=0`
  signal is the same.
- **`VendorStall.cs`/`VillageVendor.cs`** — commerce (buy/sell), zero Commands.
  Real money changes hands here, on top of `PlayerCurrency` itself not being
  networked at all (see below) — this one is blocked on that broader gap as
  much as it needs its own Class A treatment.
- **`Coin.cs`** — a currency pickup; ties directly into the `PlayerCurrency`
  gap below, not fixable in isolation.

### Confirmed Class B, not yet fixed

- **`PlayerCurrency.cs`** — not networked at all. Already flagged in
  `BUGS_AND_ENHANCEMENTS.md` with the specific warning that a naive `[SyncVar]`
  conversion would make things *worse* (a remote client's own correct local
  balance would get overwritten by the server's permanently-stale copy),
  since — unlike `PlayerFame` — all 16 of its call sites (Vendor buy/sell,
  Bank, wages, rename cost, Coin pickups, Lockbox, ...) mutate directly with
  no Command anywhere. Needs the `PlayerFame`-style "dispatch through a
  Command inside the mutating method itself" treatment applied to `Add`/
  `Spend`/`RestoreBalance`, but touches far more call sites indirectly (every
  system above that reads/writes currency), so budget real testing time, not
  just the mechanical conversion.
- **`PlayerBank.cs`** — `Deposit`/`Withdraw`/`Exchange`, zero Commands,
  presumably called directly from `BankScreen`'s UI. Same shape as
  `PlayerCurrency`, same real-money stakes.
- **`PlayerReading.cs`** — ✅ **fixed, `v0.3.212-dev`**. `TryRead` now
  routes through `RequestRead`/`CmdRead`; also found and fixed the
  identical unsynced-`HashSet` gap in both `PlayerCrafting
  .bookGrantedRecipes` and `PlayerMagic.bookGrantedWishes`, directly in
  `TryRead`'s own call path.
- **`PlayerWriting.cs`** — ✅ **fixed, `v0.3.213-dev`**, the mirror-image
  action (writing a book, not reading one). Worse gap than reading's own:
  writing also spawns a brand-new physical object that never actually
  network-spawned for a real remote client on top of the same
  permanent-unlock risk. Same stable-string-ID Command pattern reused.
- **`PlayerMagic.TryWish`** — already flagged in the existing
  `BUGS_AND_ENHANCEMENTS.md` entry as the single confirmed instance of a much
  bigger, still-unscoped finding (see below). Not re-detailed here.

### Lower priority / probably fine as-is

- **Equip-carrier classes** (`PlayerBelt`, `PlayerBoot`, `PlayerJeans`,
  `PlayerShirt`, `PlayerSunglasses`, `PlayerMiningFaceShield`,
  `PlayerNavComputer`, `PlayerHealthMonitor`, `PlayerTool`) — zero `[Command]`
  each, but every one of them is only ever invoked *from inside*
  `PlayerInventory.CmdEquipInstance`/`CmdUnequipInstance`, which already runs
  server-side. Same shape `PlayerVitals` had before tonight (server-side by
  construction, no Command needed on the class itself) — genuinely fine, not
  a gap. Worth a final confirmation pass before fully trusting this (grep
  alone can't prove there's no OTHER, un-Command-routed call site the way
  `WaterSource` turned out to be one for `PlayerCanteen`), but not urgent.
- **NPC scripts** (`NPCCrafting`, `NPCFlee`, `NPCGathering`, `NPCGuarding`,
  `NPCHiring`, `NPCSeekFlag`, `NPCTraining`, `NPCVitals`, `NPCWander`,
  `PreyWander`) — zero Commands each, but NPCs are already established
  server-side-only per this project's multiplayer conversion history; a
  player's *interaction* with an NPC (hiring, training, ...) goes through
  `NPCHiringScreen`/`NPCJobScreen`/`NPCCraftingScreen`, which already show
  real Command counts (5, 3, 2 respectively). Genuinely low risk.
- **`NetworkStorageBoxSpike.cs`/`NetworkSpikeMovement.cs`** — read the names:
  these look like leftover prototype/spike scripts from the original Mirror
  bootstrap (`MULTIPLAYER_PLANNING.md` mentions a deliberately-isolated
  `NetworkSpike.unity` scene from the very start of the conversion). Worth a
  quick confirmation they're genuinely unused dead code before deleting, not
  a fix candidate.
- **Screen/UI classes** (`BankScreen`, `BuildScreen`, `CampfireScreen`,
  `CraftingScreen`, `FurnaceScreen`, `GameMenuScreen`, `InventoryScreen`,
  `MagicScreen`, `MapScreen`, `SkillsScreen`, `TeamScreen`, ...) — plain
  `MonoBehaviour` by design (local-only `OnGUI` draw, gated on
  `isLocalPlayer`/`netIdentity`). Not a gap in themselves; several of them
  are exactly where the *real* fix for `Furnace`/`Campfire` above needs to
  route its new Commands from.

## The bigger, still-unscoped finding (already logged, restated here for the plan)

Surfaced while fixing `PlayerMagic` two nights ago, restated here because
this audit's findings make it look even larger than first thought: **most
direct (non-Command) gameplay actions for a genuine remote client don't
reach the server at all.** `PlayerMagic.TryWish` was the first confirmed
instance (Will spent, skill trained, wish success/failure — all invisible to
the server, lost entirely on disconnect since `SaveManager` only ever
captures the server's own copy). This audit's survey found the identical
shape independently in `PlayerBank`, `PlayerReading`, `PlayerWriting`,
`Furnace`, `Campfire`, `GardenPlot`, `Door`, `VendorStall` — `PlayerBank`,
`PlayerReading`, `PlayerWriting`, `Furnace`'s worst half, `GardenPlot`, and
`Door` are now fixed (see the build-order section above); `TryWish` itself,
`Furnace`/`Campfire`'s remaining screen-driven half, and `VendorStall` are
still open. This is the
same "unestimated" scope `MULTIPLAYER_PLANNING.md` already flags for the full
48-script `PlayerXXX.cs` conversion — not something to attempt in one sitting,
but this audit at least turns "unestimated" into a concrete, prioritized list
instead of a vague warning.

## Recommended build order

Roughly by (real-money/permanent-loss risk) × (how often it's actually used
in normal play), not strictly the order found. Status as of `v0.3.210-dev`:

1. **`Furnace`/`Campfire`** — ⚠️ **partially fixed**. The `Update()`
   `isServer` guard landed (the single most severe finding in this doc —
   uncoordinated per-machine simulation, not just "doesn't sync"). Still
   open: `FurnaceScreen`/`CampfireScreen`'s own button actions
   (`ToggleQueue`, `SetAutoRun`, fuel/materials/output box assignment)
   still mutate state directly, client-local, no Command; full state
   (lit, fuel timer, queue, active recipe, linked boxes) isn't synced
   back to observers. This remains the biggest real gap left on this
   list — screen-driven mutation across many buttons, not one
   `Complete()` dispatch, genuinely more work than every other item here.
2. **`GardenPlot`/`GardenPlot4x4`** — ✅ **fixed**, `v0.3.210-dev`.
3. **`PlayerCurrency`** — ✅ **fixed**, `v0.3.210-dev`, along with every
   remaining direct caller that made it safe to turn on
   (`PlayerIdentity`'s rename cost, `Coin`). `VendorStall`'s own use of
   `PlayerBank.SpendDirect`/`DepositDirect` remains open (VendorStall
   itself still has zero Commands) — not made worse by this fix, just
   not fixed by it either.
4. **`PlayerBank`** — ✅ **fixed**, `v0.3.210-dev`.
5. **`Door`/`BankBox`/`Lockbox`** — ✅ **`Door` and `Lockbox` fixed**,
   `v0.3.210-dev`. `BankBox` needed no fix at all — it holds zero state
   of its own, `Complete()` just opens `BankScreen` (the real state is
   `PlayerBank`, already covered by item 4). Real complication found on
   `Lockbox`: it (and `Coin`) has never been a real prefab — both are
   built at runtime via `CreatePrimitive`/`new GameObject` +
   `AddComponent`, so there's no registered asset for Mirror to tell a
   fresh client how to visually reconstruct one. State-sync is fixed;
   genuine cross-client visibility of a newly-purchased Lockbox or a
   newly-dropped Coin still needs that prefab work, flagged not attempted.
6. **`PlayerReading`** — ✅ **fixed, `v0.3.212-dev`**, along with the two
   unsynced `bookGrantedRecipes`/`bookGrantedWishes` `HashSet`s directly
   in its path. **`PlayerWriting`** — ✅ **fixed, `v0.3.213-dev`**, the
   mirror-image action, same Command pattern reused.
7. **The big unscoped finding** — still open. Also newly confirmed as
   the actual blocker behind `VendorStall`/`VillageVendor` (zero Commands
   each) and `Furnace`/`Campfire`'s remaining screen-driven half.

## Follow-up audit, 2026-08-31 — Pattern A closed, Pattern B scoped

Prompted by a live 2-player session (2026-08-30) that found two real instances
of a new failure shape not covered above: **Pattern A**, a `[Command]`
method invoked from code already running server-side (Mirror's ownership
check fails wherever the call executes, not just when a real client calls
it) — `HostileCreature`/`PreyCreature.DropLoot` and `PlayerLoot.Receive`'s
hand-eviction path were both calling Command-wrapped methods from inside
server-side logic; both fixed same night via new plain server-safe sibling
methods (`PlayerDropping.ServerSpawnPickup`/`ServerDropFrom`). Before
building any shared infrastructure to prevent recurrences, a full static
audit was run against every `[Command]` method's call sites (Pattern A) and
every direct `Inventory`/equipment mutation call site across all 42 files
that touch one (Pattern B) — see
`feedback_audit_before_shared_infrastructure` in Claude's memory for why
audit-first was the right call here.

**Pattern A: fully closed.** No further instances found — every other
`[Command]` method's call sites resolve only to its own file's client-side
wrapper or a Screen's button handler. `PlayerFame.Grant`/`PlayerBank`'s
methods are correctly self-guarded (`if (!isServer) CmdX(...)`), so their
NPC-AI/server-side callers are safe by construction.

**Pattern B: five real buckets, not 46 uncoordinated instances.**
1. ✅ **Fixed, `v0.3.219-dev`.** `PlayerCrafting.CancelCraft()`'s refund
   (`PlayerCrafting.cs:399`, called from `CraftingScreen.cs:315`) never
   reached the server — unlike `StartCraft`, which is properly
   Command-routed. Now `RequestCancelCraft`/`CmdCancelCraft`, same shape.
2. **Partially fixed, `v0.3.225-dev`.** `InventoryTransfer.Move`/
   `MoveAsManyAsFit` — the shared drag/move utility behind essentially all
   inventory drag-and-drop — was called unrouted from
   `InventoryScreen.cs:826,1155,1172`, `FurnaceScreen.cs:357`,
   `CampfireScreen.cs:385`, `NPCHiringScreen.cs:345,353`. Escalated from
   "deferred to MVP5" to "fix now" after live testing showed it actively
   desyncing state, not just staying invisible: a Stone Knife dragged into
   a StorageBox stayed put locally (once the equipment-sync snap-back
   regression above was fixed) but the host never saw it at all.
   `StorageBox` now formally implements `IInventoryHolder`;
   `PlayerInventory.RequestMove`/`CmdMoveItem` gained an optional
   `containerNetId` per side (0 = use the existing string key, non-zero
   resolves an arbitrary world object by `NetworkIdentity`, same pattern
   `PlayerDropping`/`PlayerBuilding` already use); `InventoryScreen`'s new
   `ContainerNetIdFor` finds a nearby box via `StorageBox.Active`. **Still
   unrouted**: `FurnaceScreen`/`CampfireScreen`/`NPCHiringScreen`'s own
   `InventoryTransfer` calls — this fix covers exactly the StorageBox case
   that broke live tonight, not the whole item. Still a strong anchor data
   point for item 7's "most direct gameplay actions never reach the
   server" finding.
3. ✅ **Fixed, `v0.3.219-dev`.** StorageBox re-placement: pulling a box
   from inventory to place it (`InventoryScreen.cs:531`) and restoring it
   on a cancelled placement (`PlayerBuilding.cs:285`) both mutated
   locally, no Command. Now `PlayerBuilding.RequestArmExistingPiece`/
   `CmdArmExistingPiece` and `CmdRestoreExistingPiece` — the Command also
   sets the server's own `armedPiece`/`existingInstanceToPlace` fields,
   closing part of item 7's "armedPiece not synced" gap for this one path.
4. ✅ **Mostly fixed, `v0.3.220-dev`.** Real scope turned out smaller than
   first estimated once the carrier code was actually read: Equip and
   Unequip were *already* networked (a 2026-08-23 partial rollout,
   `PlayerInventory.RequestEquipInstance`/`RequestUnequipInstance`) for
   every case except equipping from a non-main-inventory source (a nested
   Backpack's cargo — still open, narrow). The two real gaps — world
   pickup (`Complete()`, all 10 carrier types) and `PlayerDropping
   .DropFrom`'s equipment branch — are now fixed via
   `PlayerInventory.RequestPickUpEquipment`/`CmdPickUpEquipment` and
   `PlayerDropping.CmdDropEquipment`/`ServerDropEquipmentFrom`. Caught and
   fixed before it shipped: routing pickup through a Command meant
   `PlayerLoot.ReceiveEquipment`'s own eviction branch started running
   server-side-only too, which would have reintroduced the exact
   Command-from-server-context bug in a third spot — fixed the same way
   as the other two instances (route through `ServerDropFrom`, not
   `DropFrom`). **Equip-from-nested-container also fixed, `v0.3.221-dev`**:
   `RequestEquipInstance`/`CmdEquipInstance` now take a source container
   key (reusing `ResolveContainerByKey`/`ContainerKeyFor`, the same scheme
   Eating/Medicine/Reading/Writing already use) instead of assuming main
   inventory — item 4 is now fully closed. `NavigationComputer`/
   `PersonalHealthMonitor`/`Sunglasses` remain off the pickup dispatch
   since none has a live world-pickup prefab in the project at all; a
   nearby StorageBox as an equip source still falls back to the local
   unrouted path, same already-accepted limitation Eating/Medicine/
   Reading share.
5. ✅ **Fixed, `v0.3.219-dev`.** `VendorStall`/`VendorStallScreen` (zero
   Commands) and `VillageVendor.Update()` (no `isServer` guard) — both
   already logged above. `VendorStallScreen` converted to
   `NetworkBehaviour` with real `RequestBuyFromStall`/`CmdBuyFromStall`
   and `RequestSellToStall`/`CmdSellToStall` Commands (a `TargetRpc`
   reports the real result back); `VillageVendor` converted to
   `NetworkBehaviour` and `Update()` guarded. Known follow-up left open:
   `VendorStallScreen`'s "Next restock in Ns" display now reads a
   server-only field, so a genuine remote client sees a stuck "0s" —
   needs one `[SyncVar]`, small, not done this pass.

**Unclear, flagged not guessed at**: `Furnace`/`Campfire`'s actual
`AddItem`/`RemoveItem` calls are all correctly inside the `isServer`-guarded
`Update()` (safe) — the doc's existing "screen-driven half" note above refers
to *other* state (queue/ignite toggles), not inventory mutation, so it's a
separate concern from this pattern. One `PlayerEquipment.cs:175-208` swap
helper's full caller set wasn't fully traced; likely just item 2 above, not
an independent path.

**Confirmed safe, no action needed**: `PlayerDropping`, `Pickup`,
`PlayerWriting`, `PlayerReading`, `PlayerMedicine`, `PlayerEating`,
`PlayerBackpack` (contents SyncList-fixed; its own equip/unequip calls are
Command-wrapped), `GardenPlot`/`GardenPlot4x4`, `ResourceNode`,
`AdminSpawnScreen` (still `#if UNITY_EDITOR`-gated), `PlayerRangedCombat`,
`NPCTraining`/`NPCCrafting`/`NPCGathering`/`NPCJob` (server/NPC-AI-only),
`InventorySaveUtility`, `PlayerCarriedItems`, `IngredientMatching`.

## Two more failure shapes found live, 2026-08-31 — the first real standalone-client test

Neither of these is Pattern A or B — both are new shapes, found the moment a
genuine second connection (not host-alone, not batch-mode) first exercised this
code:

**Pattern C — an unregistered Mirror spawnable prefab.** Any prefab with a
`NetworkIdentity` that's dynamically `Instantiate()`d at runtime must be in
`NetworkManager`'s "Registered Spawnable Prefabs" list. If it isn't, the server
spawns it fine (no error) and it's fully visible on host (client/server share the
same object there) — but a genuine remote client has nothing to construct it from
when the spawn message arrives, and silently drops it. **Completely invisible to
every check this project has used so far** (batch-mode compile, YAML grep, host-
alone Play testing) — the only way to find one is a real second connection actually
triggering that specific spawn. Found: 10 real gaps (`BerryBush`, `Boulder`,
`GoldOreChunk`, `HerbBush`, `MediumRockChunk`, `NetworkSpikePlayer`,
`PlatinumOreChunk`, `SandDigSite`, `SilverOreChunk`, `Tree`), fixed `v0.3.222-dev`.
**How to apply going forward**: any new prefab given a `NetworkIdentity` needs to be
added to this list at the same time — nothing currently enforces or checks this
automatically.

**Pattern D — a sync mechanism that explicitly excludes equipment-backed slots.**
`PlayerInventory`'s main-inventory sync and `Backpack`'s nested-inventory sync both
independently skip any slot holding a live `IEquippable` object
(`if (slot.equipment != null) continue`) when building their `SyncList` — a
documented, deliberate boundary when each was first built (full nested-equipment
capture was explicitly scoped out, matching `SAVE_LOAD_PLANNING.md`'s own v1
boundary). The gap: an equippable item that ends up sitting loose in a plain
inventory slot (not worn, not in a hand) — e.g. `PlayerCrafting.AddCraftedOutput`
putting a crafted Tool straight into the main inventory — never syncs to a remote
client at all, with zero errors anywhere (the server's own state is correct, it's
purely a "never told the client" gap). Fixed `v0.3.222-dev` via a new
`syncedEquipmentSlots` reconciled by live object identity (`NetworkIdentity.netId`),
mirrored onto both `PlayerInventory` and `Backpack`.

**Follow-up, same night**: checked whether `StorageBox`'s own contents had the
identical gap — found worse. `StorageBox.cs` had no sync mechanism at all, not
even the plain-item sync `PlayerInventory`/`Backpack` already had. Fixed
`v0.3.223-dev` with the same dual-`SyncList` shape `Backpack` uses.

**Real regression found live immediately after, `v0.3.224-dev`**: all three
equipment-slot reconciliations above used full-enforcement comparison ("does
local state match the current broadcast, checked every frame") rather than the
plain-item sync's proven additive-delta philosophy ("only act on what changed
since the last broadcast") — so dragging a Stone Knife into a StorageBox
snapped it right back, since moving into a container isn't Command-routed yet
and the full-enforcement check fought that local move every tick. Rewritten to
the same delta shape the plain-item sync already used successfully (confirmed
live: a Rock moved into/out of the same box, cross-machine, throughout this
whole investigation). Worth remembering: this compiled clean and looked correct
by inspection — only a real live retest caught it.

**Checked, different shape**: `NPCCargo.cs` (an NPC's carried-but-not-yet-
deposited items) has no sync either, but not because of a missed `SyncList` —
it's a plain `MonoBehaviour` on an NPC GameObject that isn't a networked object
at all. Confirmed directly: the actually-spawned prefabs
(`NPCFactoryWorkerFemale.prefab`/`NPCFactoryWorkerMale.prefab`) carry zero
`NetworkIdentity`. This is the front door of `MULTIPLAYER_PLANNING.md`'s own
"NPCs move server-side" phase, explicitly listed there as not started —
converting NPCs into real networked objects (spawning, server-authoritative
movement, observer visibility for every job type) is a substantial, already-
scoped, deliberately-separate piece of work, not a same-shape quick fix like
the three above. Deliberately left untouched tonight rather than scope-creeping
into it.

## Cross-references

- `MULTIPLAYER_PLANNING.md` — the original phased conversion plan; Phase 3
  was marked "fully complete" as of 2026-08-23, which both live-test nights
  since have shown was true only for player-authoritative *movement/input*
  gating, not for the much larger surface this audit covers.
- `BUGS_AND_ENHANCEMENTS.md`'s "Real live two-machine session" entry — the
  night-by-night blow-by-blow this doc's "Fixed across both nights" section
  summarizes.
- `CHANGELOG.md`'s `v0.3.205-dev` through `v0.3.209-dev` entries — full
  technical detail on every individual fix referenced above.
