# Commerce Planning

**Status: planning only, nothing built yet (2026-08-16).** Design brief's
Phase 3 "Commerce system" scope, pulled forward into real design because
Ben wants to close `MVP2_PLANNING.md` item 8's ranged-combat work with a
real reason for the player to want currency — and because three separate
vendor ideas kept threatening to turn into three separate vendor systems.
Designed conversationally with Ben; decision-locked where noted, open
where noted.

## 1. Why this needed a plan before any code

Ben's ask, stated directly: *"can we author a single vendor system that
can span multiple ideas, so we don't have to invent new vending systems
for each new idea."* Three vendor concepts were on the table — a
player-built stall, a Traveling NPC, and prespawned village vendors
(Innkeeper/Armory-style) — and each one individually maps cleanly onto
existing infrastructure, but naively building them as three separate
features would mean three separate price lists, three separate stock/
till mechanics, and three separate screens drifting apart over time. This
doc exists to lock in one shared mechanic first.

## 2. What already exists (audited before designing anything new)

More is already built than "no commerce system" suggests:

- **A real currency system**: `PlayerCurrency` (wallet, 5-tier `CoinType`
  — Copper→Iron→Silver→Gold→Platinum, base-10, capped at 250/type),
  `PlayerBank` (single global account, uncapped, 3% fee, 10:1 exchange
  between adjacent tiers), `Lockbox` (personal storage, `CraftTier`-scaled
  capacity, 2,500/type baseline). `Coin.cs`/`PlayerCoinDrop.cs` handle
  physical world coins.
- **Fame-based vendor pricing is already designed**, not just an idea —
  `FAME_PLANNING.md` has a confirmed 5-band table (Infamous ≤-500 through
  Renowned ≥500) with symmetric pricing multipliers (+50% down to -20%)
  and a quality gate at the top band, explicitly scoped for "NPC traders/
  vendors including food." This plan reuses that table verbatim for the
  Traveling Trader rather than inventing new pricing logic.
- **The Traveling Trader's spawn/visit mechanism is already built**, just
  not wired to commerce — `VillageFlagSpawner.cs`/`NPCSeekFlag.cs` (the
  Village Flag beacon system, v0.3.103-dev) was explicitly designed with
  this reuse in mind.
- **A real precedent for role-restricted storage**: `StorageBox
  .restrictToSkillBooks` (built for the NPC Training Bookshelf, computed
  live off an `ItemDatabase` scan rather than hand-maintained) is the
  shape a future Innkeeper/Armory item filter would reuse.
- **A concrete capacity anchor**: Masterwork Backpack is 16 slots
  (confirmed in the prefab, not just design-doc text) — the number the
  Traveling Trader's inventory cap should use.
- **`NPCJobDefinition.JobKind`** has been cleanly extended twice already
  (`Gathering`→`Crafting`→`Guarding`) — precedent if a vendor ever needs
  to be NPC-staffed rather than a passive fixture (not needed for v1, see
  section 7).

## 3. The gap that shapes everything else: no currency faucet exists

`PlayerCurrency`'s own header comment says it plainly: *"No earn/spend
mechanic beyond Coin pickups exists yet."* Every character starts with a
fixed purse (20 Copper/5 Silver/1 Gold wallet + 25 Gold banked) and
nothing in the game currently mints new currency — the design brief's own
Ore→Furnace→Ingot→Press→Coin minting pipeline is explicitly flagged as
unbuilt ("the Coin/currency system... is still a standalone economy
layer, not fed by this ore pipeline").

**Decided (Ben, via the Village Vendor idea): don't wait on minting.** A
prespawned vendor's till regenerating slowly over real time (same
real-time-calibrated convention as Dexterity/Constitution training or the
Village Flag spawn interval) is a legitimate substitute faucet — players
earn real currency by selling to it, without the Ore/Press pipeline
needing to exist first. Minting stays a real future improvement, not a
blocker for this plan.

## 4. The shared core: `VendorStall`

One non-abstract component holds the entire transaction mechanic. Every
vendor idea in this doc is a thin *driver* around the same object — none
of them get their own price list, till, or screen.

- **Stock** — a reference to an existing `StorageBox` (reuse, not a new
  container type — same "assign an existing box" pattern NPC deposit
  targeting already uses via `PlayerNPCDeposit`).
- **Till** — `int[]` balances by `CoinType`, same shape as `Lockbox`'s own
  balance array.
- **Price list** — an array of `{ ItemDefinition, buyPrice, sellPrice,
  canBuy, canSell }` entries. `buyPrice` is what the stall pays a visitor
  selling *into* it; `sellPrice` is what a visitor pays to buy *out* of
  it. Absence from the list means not tradeable at that stall at all.
- **`SellToVisitor(item, qty)`** — visitor pays `sellPrice × qty` from
  their wallet, stock decreases, till grows. Fails if stock can't cover
  `qty` (the "out of stock" rule).
- **`BuyFromVisitor(item, qty)`** — visitor is paid `buyPrice × qty` from
  the till, stock grows (subject to the linked `StorageBox`'s own
  capacity/leftover handling, same convention `Inventory.AddItem` already
  uses), item leaves the visitor's inventory. Fails if the till can't
  cover the payout — the direct mirror of "out of stock," just applied to
  coins instead of items. This is the answer to the original "if a stall
  can buy unlimited goods, where does its currency come from" question —
  it can't, once the till runs dry.
- **`VendorStallScreen`** — one screen, two modes: **transact** (any
  visitor — browse the price list, buy/sell) and **configure** (owner
  only, once there's a permission concept to check). With a single local
  player today that distinction is moot, but the seam is built in from
  the start since multiplayer is the actual reason a "visitor" concept
  matters at all.
- **Persistence** — stock (via the linked `StorageBox`'s own save data),
  till, and price list all need to survive save/load, same `ISaveable`
  treatment `StorageBox`/`Lockbox` already get. Scope of the shared piece,
  not something each driver re-solves.

## 5. The three drivers

| | Player Stall | Traveling Trader | Village Vendor |
|---|---|---|---|
| **Price list source** | Owner sets it by hand via `VendorStallScreen`'s configure mode | Rolled once at spawn: base value × Fame-band multiplier (`FAME_PLANNING.md` table), `CraftTier` gated by band (Renowned-only unlocks better tiers) | Hand-authored per instance — just content, same as any other prespawned world object |
| **Till funding** | Owner deposits manually; auto-draws Lockbox→Bank when low (section 6) | Seeded once at spawn to fill capacity, not replenished — sells out and needs the next visit | Regenerates slowly over real time (section 3's faucet) |
| **Stock source** | Owner stocks the linked `StorageBox` by hand | Procedural loot roll, capacity capped at 16 slots (Masterwork Backpack) | Hand-authored, optionally role-restricted later (Innkeeper/Armory, same shape as `restrictToSkillBooks`) |
| **Placement** | Player-built (new Build tab piece, wraps a `StorageBox` + `VendorStall`) | Spawned/despawned by the existing `VillageFlagSpawner`/`NPCSeekFlag` system | Pre-placed in `TestScene.unity`, same convention as the scattered Wolves/hireable NPCs |
| **Build risk** | Needs new Lockbox-assignment plumbing (section 6) | Needs a stock-roll table + Fame-band price formula | Lowest risk — no owner, no Fame math, no new plumbing beyond `VendorStall` itself |

**Recommended build order, unchanged from the earlier discussion and
reinforced by this table: `VendorStall` + Village Vendor first** (proves
the whole mechanic in single-player, today, with the fewest moving
parts) **→ Traveling Trader → Player Stall.**

## 6. Player Stall's funding chain — real gaps, not yet solved

Ben's proposal: draw from the player's Lockbox first, then Bank if one
exists in town. Two real prerequisites this surfaces, neither free:

- **`Lockbox` has no assignment/targeting concept at all today** — no
  `Active` registry, nothing like `StorageBox`'s nearby-lookup. A stall
  needs a way to say "draw from *that* Lockbox specifically." New
  plumbing, consistent with the existing assigned-target pattern, but
  real work.
- **Bank has no locality concept at all today.** `BankBox`'s own code
  comment: *"the bank itself is global (PlayerBank), so any branch opens
  the same account... there's no per-branch ledger."* "A bank in the
  town" doesn't exist as a checkable condition yet — that's new logic,
  not a lookup against something already there.
- **Open, not decided**: does the stall draw the Bank down live/silently,
  or only while the owner is nearby or online? Moot with one player
  today; a real griefing/drain concern once multiplayer exists. Flagging
  now so it isn't forgotten later, not resolving it here.

## 7. Explicitly out of scope for this pass

- **Player-built Bank keeping half the transaction fee.** Kept
  deliberately separate from `VendorStall` — it isn't a vendor at all,
  it's Bank becoming a per-instance, ownable entity, which is a real
  redesign of `PlayerBank`/`BankBox`'s current single-global-ledger
  architecture (see section 6's Bank-locality point — the same
  architectural gap blocks both ideas). It also only pays off once a
  second real player is transacting at your specific branch, so it's
  priced for post-multiplayer, not now. Logged here so it isn't lost, not
  designed further.
- **NPC-staffed vendors** (an actual hired Innkeeper standing at the
  stall, a `Vending` `JobKind`) — the Village Vendor driver above is a
  passive fixture, not a hireable job, for v1. `JobKind` extends cleanly
  later if this becomes wanted (same precedent as `Crafting`/`Guarding`
  before it), not needed to ship the shared mechanic.
- **Role-restricted stock** (Innkeeper only accepts food/drink, Armory
  only weapons/armor) — the mechanism (`restrictToSkillBooks`'s pattern)
  is identified but not built; Village Vendor v1 can ship with an
  unrestricted hand-authored price list first.
- **Minting** (Ore→Furnace→Press→Coin) — still a real future
  improvement to the currency supply, explicitly not a blocker per
  section 3.
- **Multi-currency pricing** — prices are a single integer, implicitly
  Copper. No design work done on whether a price could ever span
  denominations.
- **The volatile gem market** and **city-scale central banking** —
  both still Phase 3 design-brief ideas with no relationship to this
  plan yet.

## Cross-references

- `PlayerCurrency.cs` / `PlayerBank.cs` / `Lockbox.cs` / `Coin.cs` — the
  existing currency/storage layer every driver reads or writes.
- `FAME_PLANNING.md`'s "Fame bands, and the Traveling Trader" section —
  the exact pricing table the Traveling Trader driver reuses, and the
  still-open "business-reach Fame" input this plan doesn't resolve (a
  player *running* a Trader/Inn business as a Fame input is still a
  separate, unbuilt idea from anything in this doc).
- `VILLAGE_FLAG_PLANNING.md` — the spawn/seek beacon system the Traveling
  Trader driver reuses directly.
- `StorageBox.cs` (`restrictToSkillBooks`) — the role-restriction pattern
  a future Innkeeper/Armory item filter would reuse.
- `NPC_TRAINING_PLANNING.md` — where `restrictToSkillBooks` was built,
  same "compute the allowlist live off `ItemDatabase`, don't hand-
  maintain it" discipline `EFFICIENCY_AUDIT.md` already warns about.
- `BUGS_AND_ENHANCEMENTS.md`'s four blocked Fame entries (Kill NPC,
  Player death, Guild creation, business-reach + Traveling Trader) — the
  last of those is the one this plan actually starts to unblock.

Planning only — nothing built yet.
