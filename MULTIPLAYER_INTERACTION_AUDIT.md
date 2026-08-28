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
- **`PlayerReading.cs`/`PlayerWriting.cs`** — `TryRead`/`TryWriteRecipeBook`/
  `TryWriteWishBook`, zero Commands. These grant *permanent* recipe/wish
  unlocks — a remote client's own successful read/write not reaching the
  server would be a real, disappointing loss (spent the book, kept none of
  the benefit), not just a display glitch.
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
captures the server's own copy). This audit's survey of `PlayerBank`,
`PlayerReading`, `PlayerWriting`, `Furnace`, `Campfire`, `GardenPlot`, `Door`,
`VendorStall` all independently reproduce the identical shape. This is the
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
6. **`PlayerReading`/`PlayerWriting`** — still open, not attempted this
   round.
7. **The big unscoped finding** — still open. Also newly confirmed as
   the actual blocker behind `VendorStall`/`VillageVendor` (zero Commands
   each) and `Furnace`/`Campfire`'s remaining screen-driven half.

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
