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
  (`Gathering`→`Crafting`→`Guarding`) — the precedent the new `Vending`
  kind reuses directly (see the "NPC-staffed vendors" section after
  section 6). Not needed for v1's core `VendorStall` mechanic either way.

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

## 5. The drivers

Grew from three to five, 2026-08-19 — `TEAMS_AND_GUILDS_PLANNING.md`
designed **Team Vendor** and **Guild Vendor** as two more thin drivers
around this exact same `VendorStall` core, during the multiplayer Teams &
Guilds design session, without a new mechanic or a new screen. Folded back
into this table so the driver list stays a single source of truth instead
of drifting across two docs.

| | Player Stall | Traveling Trader | Village Vendor | Team Vendor | Guild Vendor |
|---|---|---|---|---|---|
| **Price list source** | Owner sets it by hand via `VendorStallScreen`'s configure mode | Rolled once at spawn: base value × Fame-band multiplier (`FAME_PLANNING.md` table), `CraftTier` gated by band (Renowned-only unlocks better tiers) | Hand-authored per instance — just content, same as any other prespawned world object | Set by team members, same as Player Stall | Set per `GuildTypeDefinition` (specialty content), member-priced vs. non-member |
| **Till funding** | Owner deposits manually; auto-draws Lockbox→Bank when low (section 6) | Seeded once at spawn to fill capacity, not replenished — sells out and needs the next visit | Regenerates slowly over real time (section 3's faucet) | No till at all — sale proceeds split evenly across current team members immediately, never pooled | A cut of every sale flows into the Guild Bank (a pure sink, spendable only on guild-wide Perks — see `TEAMS_AND_GUILDS_PLANNING.md`) |
| **Stock source** | Owner stocks the linked `StorageBox` by hand | Procedural loot roll, capacity capped at 16 slots (Masterwork Backpack) | Hand-authored, optionally role-restricted later (Innkeeper/Armory, same shape as `restrictToSkillBooks`) | Team members stock it by hand, same as Player Stall | Guild-specialty content, possibly exclusive items not sold anywhere else |
| **Placement** | Player-built (new Build tab piece, wraps a `StorageBox` + `VendorStall`) | Spawned/despawned by the existing `VillageFlagSpawner`/`NPCSeekFlag` system | Pre-placed in `TestScene.unity`, same convention as the scattered Wolves/hireable NPCs | Built within Team territory (a Village Flag-anchored zone) | Built within Guild territory (a Guild Sign/Marker-anchored 15m zone) |
| **Build risk** | Needs new Lockbox-assignment plumbing (section 6) | Needs a stock-roll table + Fame-band price formula | Lowest risk — no owner, no Fame math, no new plumbing beyond `VendorStall` itself | Needs the even-split payout logic (new — no other driver splits a sale across multiple recipients) plus Team's own multiplayer prerequisites | Needs the Guild Bank sink plus Guild's own multiplayer prerequisites; the "member vs. non-member price" distinction is also new (every existing driver charges one price to any visitor) |

**Recommended build order, unchanged from the earlier discussion and
reinforced by this table: `VendorStall` + Village Vendor first** (proves
the whole mechanic in single-player, today, with the fewest moving
parts) **→ Traveling Trader → Player Stall.** Team Vendor and Guild
Vendor are both explicitly post-multiplayer — neither is buildable before
`MULTIPLAYER_PLANNING.md`'s own prerequisites land, so they don't affect
the near-term build order above at all, just the eventual full shape of
the driver list.

### Traveling Trader, fleshed out (2026-08-19)

The driver's basic shape was already locked (spawns via the Village Flag
beacon, rolls a Fame-band-priced stock once), but several real mechanics
were still unspecified. Worked out conversationally, decision-locked here:

- **Not built on `NPCHiring` at all.** A Trader isn't hireable — it's a
  different kind of spawned entity entirely, conceptually closer to
  `NPCSeekFlag`'s walk-to-Flag-then-idle behavior than to a Factory Worker.
  Likely its own lean prefab: reuses `NPCSeekFlag`'s movement, minus all the
  hiring machinery, plus a `VendorStall` that becomes interactable once it
  arrives.
- **Its own separate, parallel spawn timer** — not a shared roll against
  the existing hireable-NPC spawn pool. Traders and hireable NPCs are
  conceptually unrelated (one you trade with, one you recruit), and coupling
  their spawn rates together would mean a busy period for one silently
  starves the other. A second, independent `VillageFlagSpawner`-shaped timer
  keeps them fully decoupled. Actual interval numbers not tuned yet — likely
  reuses the same formula *shape* (Fame-band + Flag-tier multiplier) as the
  existing spawner, just as its own independent instance, not necessarily
  the same baseline minutes.
- **Real visit-then-leave-then-return cycle**, not a one-time arrival.
  Reinforces what "Traveling" actually means: a Trader arrives, is
  interactable for its visit, then leaves (walks off/despawns) after a
  **fixed visit-duration timer** — deliberately not gated on stock/till
  being exhausted, simplest trigger, no need to watch transaction state to
  decide when to leave. The parallel spawn timer above eventually produces
  a fresh visit later, with a newly-rolled stock and a freshly-snapshotted
  Fame-band price (so pricing reflects Fame *at that visit*, not stale from
  the last one).
- **Visuals**: reuses the existing Male/Female Factory Worker (Kevin
  Iglesias dummy) rig, same as every other spawned NPC — consistent with
  this project's "reuse the rig, don't generate a new character model per
  role" convention. A small recognizable differentiator (a pack/cart prop,
  a distinct outfit color) is cheap flavor worth considering later, not new
  character-generation work.
- **Map marker**: gets its own distinct marker (shape and/or color),
  separate from hireable-NPC markers and the newer Team/Guild-mate markers
  (`TEAMS_AND_GUILDS_PLANNING.md`) — "there's a Trader nearby" is exactly
  the kind of at-a-glance info the Map's existing marker language already
  exists to convey, same reuse of `DrawNpcMarkers`' live-scan pattern every
  other marker category already uses.

Not built — this section is a real, decision-locked spec for whenever the
Traveling Trader driver is actually picked up (still second in the build
order, after `VendorStall` + Village Vendor).

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

### NPC-staffed vendors — the `Vending` job, fleshed out (2026-08-19)

Was logged in section 7 as identified-but-not-designed; worked out for
real this session, and it turns out to directly resolve the funding-chain
gap just above rather than needing its own separate solution.

- **The core "be mean" question first**: `VendorStall` already works fully
  passive, interactable by any visitor with nobody staffing it (see
  section 4) — so what does an NPC actually add beyond flavor? Decided:
  **automation, not access.** Staffing is an upgrade, never a gate — an
  unstaffed stall keeps working exactly as already designed. This matters
  because it means `Vending` never becomes a hard prerequisite for the
  core buy/sell loop `VendorStall` was already built to handle standalone.
- **New `NPCVending.cs`**, same sibling-component shape as `NPCGathering`/
  `NPCCrafting`/`NPCTraining`/`NPCGuarding` — lives permanently on the
  hireable NPC prefab, bails early if the assigned job's `JobKind` isn't
  `Vending`. A new `NPCJobDefinition.JobKind` value, third or fourth
  extension of that enum (after `Gathering`→`Crafting`→`Guarding`), same
  low-risk precedent each prior extension already established.
- **Two real automation duties**, both reusing existing NPC-job patterns
  rather than inventing new ones:
  - **Auto-restock** — walks between a linked backstock `StorageBox` and
    the stall's own stock box, same two-box walk `NPCCrafting` already
    does for materials/output.
  - **Auto-bank the till** — walks to the nearest Lockbox/Bank (same
    "nearest qualifying surface" scan `NPCGathering`/`NPCCrafting` already
    use for harvest targets/Anvil-Furnace surfaces) and deposits excess
    coin once the till's full. **This is the actual solution to this
    section's own flagged gap above** — rather than building abstract
    Lockbox-assignment/Bank-locality plumbing from scratch, an NPC that
    physically walks the coin over sidesteps needing that infrastructure
    to exist at all. The open "does it drain live/silently or only near
    the owner" question above is moot under this approach too — the NPC's
    walk *is* the transfer, there's no silent background drain to worry
    about griefing.
  - Wages paid through the existing `NPCHiring` pay-cycle, unchanged — no
    new economy plumbing needed for this part.
- **Scope: Player Stall only, for now.** Village Vendor has no owner
  concept at all (the reason it's the lowest-risk driver), so there's no
  one to assign a hire or pay wages — doesn't apply. Traveling Trader
  already *is* an NPC — staffing it would mean staffing a staff member,
  doesn't apply. Team Vendor/Guild Vendor are natural later extensions
  once those drivers exist (a Team or Guild assigns the hire instead of an
  individual player), same `Vending` job kind, no new mechanism needed —
  but explicitly post-multiplayer, same as those drivers themselves.

Not built — decision-locked design, same status as the rest of this doc.

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
- ~~**NPC-staffed vendors**~~ — **moved, no longer out of scope.** Fully
  designed now, see the "NPC-staffed vendors — the `Vending` job" section
  right after section 6. Kept this line only so the doc's history reads
  clearly (this used to be the one-sentence stub that section replaced).
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

## 8. Revisited: Metal Press / Ore→Furnace→Press→Coin minting (2026-08-17)

Ben proposed building a Metal Press to mint Copper Coins from Copper
Ingots as "the start of commerce," prompted by real friction testing the
Village Flag/City Statue loop with 0 starting NPCs now in the world.
Evaluated critically against this doc and `FAME_PLANNING.md` before
building anything:

- **This exact idea was already considered and explicitly deferred**,
  in writing, the day before (section 3 above: "don't wait on minting...
  Minting stays a real future improvement, not a blocker"; section 7
  lists it as out of scope). Not rejected as a bad idea — deliberately
  sequenced *after* a market exists to spend the minted currency on,
  since a faster faucet is pointless when the only current sink is NPC
  hire fees.
- **A Press is a faucet, not commerce** — it doesn't touch `VendorStall`,
  the price-list schema, or anything else this doc actually means by
  "commerce." Zero `VendorStall` code exists regardless of whether a
  Press ships.
- **The numbers don't support "we'll run out of money"**: 10 hires costs
  100 Copper total; starting reserves (20 Copper wallet + 25 Gold banked,
  worth ~2,500 Copper after exchange) already cover that 25x over. The
  actual bottleneck for reaching 10 hires is the Village Flag's real
  30-minute spawn interval now that no NPCs are pre-placed — a Press
  does nothing about that.
- **It's a bigger build than "one more recipe"**: Coins aren't
  `ItemDefinition`s — `PlayerCurrency` is a raw int-balance wallet, and
  `Coin.cs` exists purely to call `PlayerCurrency.Add()` on pickup. A
  Press recipe can't reuse `SmeltableItem`'s `outputItem` shape as-is; it
  needs its own recipe type whose "output" writes to the wallet directly,
  plus a new `BuildPiece`/model/screen — Furnace-scale work.
- **Even the design brief's own version overshoots what's minable
  today**: it specs the full 5-tier ladder (Copper→Platinum). All 5
  Ingots exist, but Silver/Gold/Platinum ore nodes aren't placed
  anywhere in `TestScene.unity` (zero scene references) — a full Press
  would mint coin types nobody can currently mine through real play.

**A real, useful piece of framework surfaced during this discussion,
independent of the Press**: `FAME_PLANNING.md`'s Traveling Trader
pricing formula ("base value × Fame-band multiplier") already assumes
every item has a base value — nothing defines that anywhere today. This
is a genuine currently-missing prerequisite, cheap to add
(`ItemDefinition.baseValue` + a fill-in pass), and unlike the Press it's
actually on the critical path to the Trader this section's "in theory"
framing is picturing. Recommended next real framework step, when
picked back up: `ItemDefinition.baseValue` + the `VendorStall` core
component + Village Vendor as the first driver — matches section 5's
already-decided build order, needs no minting or Press first.

**Not decided/built** — this section captures the discussion and the
recommendation, not a commitment either way. The Press stays logged as
real future work (section 7), just not promoted ahead of `VendorStall`.

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
- `TEAMS_AND_GUILDS_PLANNING.md` — where Team Vendor and Guild Vendor
  (section 5) were actually designed, and where the Guild Bank sink and
  Team's even-split payout logic are specified in full. Also confirms
  Team Vendor/Guild Vendor together are the real, previously-missing
  prerequisite for the "business-reach Fame" input referenced above.

Planning only — nothing built yet.
