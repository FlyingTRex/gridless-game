# MVP 2B Planning

Decided 2026-08-21, the same night MVP2 closed out in full. Pulled the
`VendorStall`/Village Vendor slice out of `MVP3_PLANNING.md`'s Commerce
track and into its own curated, closeable tier — same reasoning that
originally split Commerce out of a bundled MVP3 in the first place
(`MVP3_PLANNING.md`'s own "why not one bundled MVP3" section): Commerce
has no real dependency on Multiplayer for this slice specifically, the
Multiplayer live-test needs a standalone two-process build this session
hasn't set up yet, and `VendorStall` + Village Vendor is fully
single-player, low-risk, and immediately testable in the same
Editor-Play-mode loop already dialed in tonight. Rather than let this
work sit as an unlabeled sub-item inside MVP3's own list, it gets its own
tier — MVP2B, between MVP2 (closed) and the rest of MVP3.

Full design lives in `COMMERCE_PLANNING.md` — this doc is the curated
build-order checklist, same "live planning surface" role `MVP2_PLANNING.md`
already has for its own tier.

## The list

Locked in across a full "propose → be mean → fix" design pass before any
code — see `COMMERCE_PLANNING.md` sections 4-5 for the complete spec each
item below references.

1. [x] **`StorageBox` ownership gate — built 2026-08-21, compile-verified,
   not yet live-tested.** `isPlayerOwned` (bool, defaults `true` — zero
   regression for every existing/future player-placed box). Found while
   implementing: the real access path isn't `IInteractable` at all — a
   box's *contents* are surfaced by `InventoryScreen`'s proximity
   auto-detection via `StorageBox.FindNearby`, a static scan with no
   owner check, also shared by `PlayerCrafting` for drawing recipe
   materials from a nearby box. Gated at that single choke point instead
   of in `InventoryScreen` alone, so both callers are protected — a
   non-owned box can't be raided for crafting materials either. Also
   gated the pickup path (`Complete()`) and updated `Prompt` to show
   "(not yours)" — otherwise an emptied (sold-out) vendor box could just
   be picked up and stolen outright. Live test still needed: place a
   non-player-owned box, confirm direct interaction and pickup are both
   blocked, confirm `PlayerCrafting` can't draw materials from it.
2. [x] **`ItemValueCalculator` — built 2026-08-21, smoke-tested against
   real game data.** Shared, reusable (not scoped narrowly to this tier —
   the Traveling Trader needs the same thing later). Needed a real
   reverse lookup that didn't exist anywhere in the project (given an
   item, which recipe produces it) — built `RecipeDatabase.cs` following
   the exact existing `ItemDatabase`/`SkillDatabase` convention
   (`Assets/Resources/RecipeDatabase.asset`, populated by extending
   `DatabaseRepopulator` — same "run before any commit that adds a new
   recipe" habit as the other three databases; repopulated once, found
   68 `CraftingRecipe`s + 5 `SmeltableItem`s). New `ItemDefinition
   .baseValue` field (root value for raw/gathered materials with no
   recipe — see `CLAUDE.md`'s new project-convention entry for seeding
   this on future items). Recursive ingredient-cost sum for craftable
   items, tier scaling applied exactly once at the top (the compounding
   formula bug caught during design, see `COMMERCE_PLANNING.md`) — a
   cycle guard was also added defensively (crafting data isn't expected
   to cycle, but an unguarded recursive walk would stack-overflow
   silently if it ever did). **Smoke-tested against real data**: Iron
   Ore (seeded 5) → Iron Ingot (10 Ore × 5 = 50) → Iron Arrowhead (1
   Ingot × 50 = 50) — clean, linear, no exponential blowup. Still open,
   not yet resolved: an explicit sellable-items filter before anything
   rolls from the full item database (item 5 below needs this).
3. [x] **`VendorStall` core — built 2026-08-21, functionally tested (5/5
   atomicity checks passing against real running code, not just
   compiled).** Stock (`StorageBox` reference, assigned only through a new
   `AssignStock()` that also forces `SetPlayerOwned(false)` — the one
   real enforcement point for item 1's whole reason for existing, not
   left to whoever wires the Inspector reference to remember). Till
   (`int[]` by `CoinType`, matching `Lockbox`'s shape — all 5 types
   tracked even though V1 pricing is implicitly Copper-only, so it's
   ready the moment multi-currency pricing exists). Price list
   (`VendorPriceEntry[]`, gained an `OnValidate` warning for
   `buyPrice >= sellPrice`, the exploit-prevention rule from the design
   pass). `SellToVisitor`/`BuyFromVisitor` are both fully atomic — every
   precondition (stock/till coverage, *and* the visitor's own inventory
   has room/has the goods) checked before anything moves, matching Ben's
   exact "fail cleanly, no charge, inventory untouched" requirement.
   **Verified with a real functional test** (5 scenarios: over-buy past
   stock, a normal successful sale, insufficient funds, till can't cover
   a payout, stock box genuinely full) — caught and fixed one real test-
   setup bug along the way (a leftover partial stack meant "the box has
   0 free slots" didn't mean "can't accept 1 more of an item already
   stacked there" — correct `Inventory` behavior, not a `VendorStall`
   bug, but worth having actually exercised rather than assumed).
4. [x] **`VendorStallScreen` — built 2026-08-21, compile-verified, not yet
   live-tested.** Transact mode only for this tier (configure/owner mode
   deferred, same reasoning `COMMERCE_PLANNING.md` already gives it).
   `VendorStall` gained `IInteractable` (`Prompt`/`Complete`, opens the
   screen) — same "world object owns the interact, a player-side screen
   component owns the UI" shape `Furnace`/`FurnaceScreen` already
   establish. Lists every price-list entry (item name, current stock,
   Buy/Sell buttons with per-unit price), shows the player's wallet and
   the stall's till side by side, brief toast-style confirmation/failure
   messages on each transaction. `VendorStallScreen` added to the Player
   GameObject in `TestScene.unity` (same place `FurnaceScreen`/
   `CampfireScreen` already live) — without this, `Complete()` would
   silently find nothing to open. Needs a real live test once a
   `VendorStall` instance actually exists in the world (item 5).
5. [x] **Village Vendor driver — built 2026-08-21, functionally tested
   (9/9 checks passing, real seeded content, not just compiled).**
   `VillageVendor.cs`, pre-placed, no owner, `ItemValueCalculator`-priced.
   Stock gated by the linked Village Flag's current tier (falls back to
   Crude-only if no Flag exists, the safer failure direction rather than
   defaulting to "everything available"). Two restock paths: a reactive
   per-slot re-roll the instant that slot sells out (rolls a *fresh* item
   in, not a replenish), plus an independent 30-real-minute full-refresh
   timer that clears and re-rolls the entire offering against the Flag's
   *current* tier. Till regenerates over real time (1 Copper/30s, cap
   500 — unverified against real prices, expect retuning). **Also
   built**: the sellable-items curation gap flagged back in item 2 —
   `ItemDefinition.sellableByVendor` (opt-in, defaults false) plus a
   real starter content seed (9 raw/gathered items — Stick, Fiber, Plank,
   MRE Ration, all 5 Ore tiers — each given a hand-seeded `baseValue` and
   flagged sellable; more items get flagged in as content grows, not a
   one-time exhaustive pass). A real `Village Vendor` instance created in
   `TestScene.unity`. **Real bug found and fixed via the functional
   test**: for a low `baseValue` (Fiber = 1), the ±20% margin rounded
   both `buyPrice`/`sellPrice` to the same integer (1 and 1) — silently
   breaking the "generated prices are spread-safe by construction"
   guarantee item 3 was built around, via a different path (generated,
   not hand-typed) than the `OnValidate` warning covers. Fixed with a
   hard floor: `sellPrice` is forced at least 1 above `buyPrice` if
   rounding would otherwise collapse the spread.
6. [x] **Persistence — built 2026-08-21, functionally tested (5/5 checks,
   a real capture -> destroy -> restore round trip, not just compiled).**
   The genuinely tricky part: a `VendorStall`'s stock box is created
   dynamically at runtime (`VillageVendor`'s own setup), never baked into
   the scene, and `RestoreWorldObjects<StorageBox>` only ever restores an
   *existing* object — it never creates one. New bespoke
   `SaveManager.RestoreVendorStalls` creates the stock box under its
   *saved* `SaveId` before `RestoreWorldObjects<StorageBox>` runs, same
   "find or recreate" shape `RestoreNpcs` already uses for a different
   case. **Real ordering hazard found and fixed before it could bite
   live**: `VillageVendor`'s own setup used to run in `Start()`, but
   `SaveManager.Load()` *also* runs during some object's `Start()` (its
   own) — Unity gives no ordering guarantee between different objects'
   `Start()` calls, so the vendor's fresh-roll setup could easily have
   run *before* the save was restored, silently clobbering real saved
   state with a random reroll. Fixed by deferring `VillageVendor`'s setup
   to the first `Update()` tick instead — Unity *does* guarantee every
   object's `Start()` completes before any object's `Update()` begins,
   so this ordering is reliable in a way `Start()`-vs-`Start()` isn't.
   Till and price list captured/restored directly (new `SaveManager`
   fields); stock *contents* ride on `StorageBox`'s own existing save
   path once the box exists under the right ID. **Verified with a real
   round-trip test**: captured a stall's real state, destroyed its stock
   box (simulating what a scene reload actually does to a runtime-only
   object), ran the restore, and confirmed the box was recreated under
   the identical `SaveId`, with till and price list both correct.

## Extension: Vendor Stall + Bank Box made real, earned structures (2026-08-22)

Grew out of a long conversational "walk through real usage" pass with
Ben after the original 6 items above — not a critique of what was
built, but a real design gap it surfaced: the Village Vendor was still a
manually-placed, always-free scene fixture, and the Bank (with its
already-existing `PlayerBank.Exchange` 10:1 currency ladder) was
*also* a free, always-present fixture from the very first moment of a
fresh game, with no earned-progression story at all. Ben's own framing
of the payoff: this now gives players in eventual multiplayer a real
reason to travel between settlements ("that other village might have
something mine doesn't"), and ties two already-built systems (Commerce,
skill-tier progression) together instead of leaving Commerce feeling
bolted-on. Full "propose → be mean → fix" pass before any code (see the
session transcript for the full back-and-forth) — real findings along
the way:
- An open-ended "buy anything sellable off-list" idea (below) was a real
  exploit path until gated by the same Flag-tier ceiling already
  governing what a vendor stocks for sale.
- A naive tool-value formula (just reusing `CraftTierScale.Modifier`,
  the same 25x spread used for capacity/price) couldn't hit "Crude ~5,
  Masterwork 1000+" at all — checked the actual recipe data and found
  every tool tier shares identical flat ingredients (1 Rock + 2 Stick
  regardless of tier), so tier scaling was the *only* lever available.
- The first version of that new curve was geometric (constant ratio per
  tier), not actually "plausible early, hard endgame" as intended — Ben
  caught this directly and asked for a genuinely back-loaded reshape.
- Skill book trading (an idea raised along the way) turned out to need
  real per-instance vendor stock tracking that doesn't exist — logged
  separately in `BUGS_AND_ENHANCEMENTS.md` rather than built half-right.
- A "vendor funded by a Bank account" and "gated structures need
  multiplayer ownership rules" were both explicitly multiplayer-era
  ideas, logged as follow-ups rather than built now.

**Built and functionally tested, 2026-08-22:**

1. **`ToolMarketValueModifier`** (`CraftTier.cs`) — a dedicated,
   deliberately back-loaded value curve for weapons/tools, cubed on the
   normalized `SkillRequirement` fraction so it automatically re-scales
   if the tier-difficulty ladder ever gets harder (no second edit
   needed). Opt-in via new `ItemDefinition.usesToolMarketCurve`, kept
   separate from the general `Modifier` used everywhere else in
   `ItemValueCalculator` (a Masterwork Herbal Tea shouldn't cost as much
   as a Masterwork Axe just because both are tier 4). Verified against
   real Axe recipe data: Crude 5.00 → Rudimentary 6.25 → Normal 24.45 →
   Fine 160.63 → Masterwork 1,250.00 — genuinely back-loaded (step
   ratios 1.25x → 3.9x → 6.5x → 7.8x), not the geometric curve tried and
   rejected first.
2. **Sellability curation** — all 8 existing seed items and all 26
   weapon/tool assets (5 tiers each of Axe/Bow/Hammer/Knife/Pickaxe, plus
   Rudimentary Shovel) flagged `sellableByVendor`; seeds also flagged
   `isSeed` (a new opt-in marker, no global seed registry existed to
   derive this from automatically) with hand-seeded `baseValue`; Rock
   (the flat ingredient every tool recipe shares) seeded at `baseValue =
   3`. 43 total sellable items now (9 original + 8 seeds + 26 tools).
3. **`VendorStall` core rewrite** — supply/demand pricing (Ben's own
   idea): a bounded ±30% stock-based adjustment (`StockAdjustmentFactor`)
   applied to both `SellToVisitor` and `BuyFromVisitor`, computed once
   per transaction off pre-transaction stock level, not per-unit across
   a multi-item purchase. **Off-list selling**: `BuyFromVisitor` now
   accepts any `sellableByVendor` item not currently in the price list,
   priced live via `ItemValueCalculator`, gated by a new
   `VendorStall.MaxOffListBuyTier` so a low-tier vendor still can't buy
   something well outside what it could plausibly afford.
4. **`VillageVendor` stocking rewrite** — 8 total slots split into 6
   general + 2 dedicated seed slots, each category **distinct** (no
   duplicate items within a category, replacing the old duplicate-with-
   replacement logic), each item stocked at a **random quantity from 1
   up to its own `maxStack`**. Wires `MaxOffListBuyTier` from its own
   `CurrentMaxTier()` (the Flag-tier gate), kept current every `Update()`
   tick.
5. **Fame grants** — `PlayerFame.GrantVendorStall()` /
   `GrantBankBox()`, +10 each (between Hire's repeatable +1 and City
   Statue's founding-a-City +50).
6. **Real earned placement, not free fixtures** — new
   `BuildPiece.requiresVillageFlagAndHiredNpc` gate (≥1 Village Flag +
   ≥1 currently-hired NPC, a much lower bar than City Statue's
   Masterwork+10), enforced in `PlayerBuilding.LockReason`. A **one-per-
   Flag cap** (`FindNearestFlag` + generic `NearestFlagAlreadyHasStructure
   <T>`, checked at the real placement position in `Confirm` since the
   pre-aim `LockReason` check can't know that yet) applies to both new
   structures. `VendorStallPiece` (6 Plank/2 Cloth/2 Rope) and
   `BankBoxPiece` (4 Plank/4 Iron Ingot/2 Rope) are real `BuildPiece`
   assets + prefabs now, registered in `BuildPieceDatabase`. `BankBox`
   gained `[RequireComponent(typeof(SaveId))]` (it never needed one
   before — no per-instance state worth saving, `PlayerBank`'s balances
   are the real global ledger — but a player-*built* structure needs to
   persist as a placed piece like everything else). The previously
   always-free, pre-placed `Village Vendor` and `Bank Box` scene
   fixtures were **removed from `TestScene.unity`** — a fresh game now
   starts with neither, Copper-only, until the gate is met.
7. **Vendor Stall visual** — the Tripo3D-generated model (colorful
   red/cream striped awning, wood-grain counter, rope-lashed frame,
   small goods on the counter) imported and wired onto
   `VendorStallPiece.prefab`'s new `Visual` child. Measured/scaled per
   `CLAUDE.md`'s mandatory model-placement checklist (2.2m target height
   next to the 1.8m player reference, ground-offset applied after
   scaling) and the interaction `BoxCollider` resized to match the real
   footprint. **Verified by actually rendering the wired prefab and
   looking at the pixels** (not just a clean batch log) — hit the
   documented `-nographics` pitfall on the first attempt (a flat gray
   image, no real graphics device to render with), re-ran without it,
   confirmed the model reads clearly and sits correctly grounded.
   `BankBoxPiece` got a simple placeholder cube visual (reusing the
   existing `BankBox.mat`) rather than a new model generation — not
   asked for, same "simple placeholder first" precedent as the original
   Anvil/Boulder reuse.

Every code change compile-verified via full-project batch mode; items
1-6 also functionally tested against real running code (not just
compiled) via throwaway batch-mode test scripts, same discipline as the
original 6 MVP2B items — 8/8 checks passing on the value-curve
verification, 8/8 on the stocking/off-list-selling rewrite, 9/9 on the
placement-gate logic. **Deliberately not live-tested or placed in the
scene by this session** — Ben's own plan: build a Vendor Stall live
in-game (the real gate/ingredient/one-per-Flag path), save, exit,
reload, and confirm it all holds — a real end-to-end validation no
batch script can substitute for.

## Status: all 6 original items, plus the full extension (real earned
placement, multi-denomination currency, Bank Box), built and **live-tested
for real** as of 2026-08-22 — not just functionally tested against batch
scripts. Ben placed a real Vendor Stall in-game (real Flag+NPC gate, real
ingredient cost), opened a real stocked `VendorStallScreen`, bought/sold for
real (confirming the tool-value curve end-to-end — a Masterwork Pickaxe at
2100c against a Crude Axe at 6c), and ran a genuine save→exit→reload cycle
that surfaced and led to fixing a real latent persistence bug (see
`CHANGELOG.md`'s v0.3.156-dev entry). A second save→reload after the fix
confirmed identical stock/till, not a fresh reroll. Bank Box itself still
hasn't been placed/opened live even once — its placeholder cube visual is
known to look poor as a baked icon — and the Pay-from-Bank fallback has no
UI of its own yet (purely automatic/silent); both worth a look before
considering this tier fully closed.

## Explicitly not this tier

- **Traveling Trader, Player Stall drivers** — still MVP3 Commerce scope,
  built on top of the same `VendorStall` core once this tier proves it
  out. Not blocked by anything here, just sequenced after.
- **Team Vendor / Guild Vendor** — post-multiplayer, MVP4 scope, per
  `MVP3_PLANNING.md`/`TEAMS_AND_GUILDS_PLANNING.md`.
- **Minting (Ore→Furnace→Press→Coin)** — still deferred per
  `COMMERCE_PLANNING.md` section 3; the Village Vendor's real-time till
  regen is the substitute faucet for this tier, not a Press.

## Cross-references

- `COMMERCE_PLANNING.md` — full design for every item in the list above,
  including the "be mean" pass that found the ownership-gate
  prerequisite, the atomic-transaction rules, and the tier-scaling
  formula bug (found and fixed before any code was written).
- `MVP3_PLANNING.md` — where this slice was originally scoped before
  being pulled out into its own tier; still the source of truth for the
  rest of Commerce (Traveling Trader, Player Stall) and all of the
  Multiplayer track.
- `CLAUDE.md`'s project-conventions checklist — gained a new standing
  entry (2026-08-21) reminding future sessions to seed a `baseValue` on
  any new raw/gathered `ItemDefinition`, once item 2 above exists.

Planning only as of this entry — nothing built yet.
