# Teams & Guilds Planning

Planning only, nothing built — this is a design doc, not a build log. Grew out of
a `MULTIPLAYER_PLANNING.md` "be mean" re-audit (2026-08-19) that led to actually
starting Phase 0 (the `NetworkSpike.unity` infra spike), which in turn opened the
real question of what happens once a *second* real player exists: how do trusted
groups of players share resources, and how does the game's existing single-player
Guild concept (`PlayerGuilds`, currently a fixed dev-authored join/leave list with
no player-driven creation) become a real multiplayer system. Worked out
conversationally with Ben across one long session, confirmed via `AskUserQuestion`
at each real fork rather than assumed. **Scope target: Plan B** — the full
design-brief vision (public dedicated servers, "anyone can host or rent," real
strangers, not just a trusted friend group) — with IP-geolocated spawn explicitly
deferred, since the current 200×200 unit world is too small for it to add value yet.

Depends on `MULTIPLAYER_PLANNING.md`'s own prerequisites landing first —
specifically Phase 2 (player-authoritative gameplay conversion) and a real player
identity/naming system (still unbuilt; flagged there as a shared prerequisite for
several features, and flagged again below for the same reason). Nothing here can
be built before those exist.

## The core split: Team vs. Guild

Two genuinely orthogonal systems — a player's Team membership never depends on
their Guild membership, and vice versa:

- **Team** — small, trusted, co-op-focused. Pools *physical access*, never
  currency. The people you're actually building and playing with.
- **Guild** — large, formal, specialty-progression-focused. Pools *capital*
  toward collective upgrades, grants individual members alternate-path
  recipe/skill access. The trade affiliation(s) you've bought into, which may
  have nothing to do with who you play with session to session.

Both reuse the same underlying `BuildPiece`/`PlacedPiece` shape Village Flag
already proved out, and both settled on the same 3-tier role ladder
(**Owner / Officer / Member**) independently before the parallel was even
pointed out — a good sign it's the right shape for "a small group of trusted
roles" generally in this project, not a coincidence to fight.

## Team

- **Cap: 6 players.**
- **Roles: Owner / Officer / Member**, mirroring Guild's ladder. Owner
  invites/kicks/promotes/demotes.
- **Resources shared: physical access only, never currency.** Buildings,
  crafting stations (Furnace, Campfire/stove, etc.), and resource bins
  (StorageBoxes) are accessible to any current team member. **No Team Bank** —
  money always stays individually owned. This resolves an early open question
  cleanly: since there's no shared pool to drain, there's no "a member drains
  the bank" griefing risk to design around at all.
- **Team Vendor**: if a team places a vendor (a `VendorStall` driver, per
  `COMMERCE_PLANNING.md`), sale proceeds **split evenly across current team
  members** — no per-member contribution tracking needed (unlike Guild, see
  below). Ben's own framing: natural specialization within a team (one member
  crafts tools, another cooks) self-balances without needing explicit
  accounting; the real cross-player balancing force is Fame drawing outside
  customers in, not internal bookkeeping.
- **Territory, via Village Flag**: a Village Flag's existing tier-scaled radius
  (`CraftTierScale.VillageFlagRevealRadius`, Crude 35m through Masterwork 75m —
  already built for the Player Map's fog reveal) is reused as-is for a second
  purpose: a buildable-territory radius. **Confirmed not the tier-scaling-
  mismatch trap `CLAUDE.md` already warns about** (a ratio tuned for one
  quantity misapplied to an unrelated one, e.g. the Encumbrance/weight
  incident) — map-reveal radius and building-territory radius are the *same*
  kind of quantity (a spatial radius around a Flag), so direct reuse is sound
  here, not a repeat of that mistake.
  - **Federated, not single-zone.** Any Village Flag placed by the team's
    **Owner or a current Officer** contributes its own territory radius to the
    team's combined territory — a regular Member's personal Flag doesn't count.
    This means a team's territory can be the union of several separate,
    non-contiguous zones scattered across the map (hence "a team could own
    multiple villages in theory," Ben's own framing).
  - **Live-computed, not cached.** Contribution is tied to *current* rank,
    recomputed fresh whenever team territory is checked — no separate
    "share this Flag" toggle to keep in sync. The moment someone's demoted or
    kicked, their Flag stops contributing automatically. This reuses the exact
    pattern `MapScreen`'s `DrawFlagMarkers`/`DrawNpcMarkers` already use (a
    fresh scan every frame, no stored/stale state), not a new mechanism.

### Team — still open, not resolved here

- **Team's own lifecycle/disband rules were never defined.** Guild got a
  thorough one (see below); Team didn't. Can a team disband? What happens to
  team-owned structures if it does, or if it just dwindles to one remaining
  member?
- **Kicked/leaving members' personal items stranded in shared storage.** If a
  member stores personal items in a team-accessible StorageBox (the whole
  point of shared access) and then leaves or is kicked, do they lose access to
  their own items sitting in there? Real loss vector, not addressed.
- **Team Officer's exact permission set** — assumed to mirror the "trusted
  enough to expand shared territory" spirit of Guild's Officer (trusted enough
  to spend the shared Bank), but not explicitly itemized beyond the
  territory-contribution behavior above.

## Guild

- **Cap: 64 members.**
- **Player-founded, not purely dev-authored.** A craftable **Guild Sign**
  (`BuildPiece`), placed once, opens a picker over dev-authored
  `GuildTypeDefinition` assets (Hunting, Mining, Stonemason, etc. — the
  specialty *content*: which recipes/wishes get granted, which Perks are in
  the catalog, the vendor price-list template). The founding player becomes
  Owner. **Multiple separate guild instances of the same type can coexist** —
  the dropdown picks a template/flavor, not a unique server-wide slot, so
  there's no race to be "the" Hunting Guild before someone else claims it.
  - **Naming**: auto-named `"{PlayerName}'s {GuildType} Guild"` at creation
    (e.g. "Ben's Stonemason Guild"), with an `IRenameable` override available
    afterward — same pattern Village Flag already has.
  - **Guild Marker** — a separate, presumably cheaper, repeatable `BuildPiece`
    an *existing* guild places to extend its presence into another village.
    Distinct from the founding Sign mechanically and visually (Ben's explicit
    call, over reusing one object for both).
- **Roles: Owner / Officer / Member.**
  - **Owner**: promote/demote, disband.
  - **Officer**: authorized to spend the Guild Bank on Perk purchases, to
    place a Guild Marker (expansion), and to toggle the guild's Map
    visibility (Public/Hidden, see below) — all read as the same tier of
    trust ("act on the guild's behalf/reach"), not separate permissions.
  - **Member**: spends their *own* currency at the Guild Vendor only — no
    access to shared funds. The only trust-gated action in the whole system is
    touching collective money; anything a member does with their own currency
    needs no special permission at all.
- **Guild Bank**: funded by member dues plus a cut of Guild Vendor sales.
  **Spendable only on guild-wide Perks, never personally withdrawn** — this
  constraint is what makes the lighter Officer-can-just-trigger-it governance
  model safe, since nobody personally gains more than anyone else from a Perk
  purchase. Also a genuine currency sink, filling a gap `COMMERCE_PLANNING.md`
  already flagged as missing from this game's economy ("everything's a
  faucet, nothing's a sink").
  - **Contribution tracked per-member**, not a blind pool (Ben's explicit
    call) — a free hook for future recognition mechanics (biggest-contributor
    badge, etc.) without needing new plumbing later.
- **Guild Vendor**: a 4th `VendorStall` driver (per `COMMERCE_PLANNING.md`'s
  existing thin-driver architecture — one component, price/stock population
  differs per driver), member-priced, possibly stocking gear/blueprints not
  purchasable anywhere else.
- **Recipe/wish grants**: same underlying mechanism as Skill Books
  (`PlayerCrafting.bookGrantedRecipes`/`PlayerMagic.knownLineages` — a
  standing exception to the normal skill-level gate), but **revocable on
  leaving the guild**, not permanent like a book. This is the actual
  differentiator that makes ongoing membership mean something — a permanent
  grant would just make joining a slower, worse version of reading a book.
- **Perk catalog**: likely tiered like everything else in this project (afford
  Tier 1 early, save toward Tier 2) — a guild-level meta-progression layer
  alongside each player's own individual progression. Specialty-specific
  content (a Mining Guild's perks differ from a Hunting Guild's), but every
  guild uses the same universal catalog *structure* — the same "universal
  ladder, per-domain content" principle `CraftTier` already established
  project-wide, now reused for a second system.
- **Territory**: Guild Sign and Guild Marker both grant a **15m buildable
  territory radius**, same distance-check pattern as
  `VillageFlagRevealRadius`/`NPCGuarding.PatrolRadius`/the deposit-leash — no
  new spatial system needed.
  - **Never retroactive** — only affects structures built *after* the
    Sign/Marker goes up, explicitly never reassigns anything already there.
    Stated as a hard design principle, not just a default: without it, placing
    a Sign near someone's existing house becomes a real griefing move.
  - **No two different guilds' zones can overlap.** Placing a new Sign or
    Marker is blocked if it would land within 30m of another guild's existing
    territory — same minimum-clearance check Tree/Boulder scatter placement
    already uses for spacing. A guild's *own* additional Markers placing near
    its *own* existing territory isn't a conflict, so this check only runs
    against *other* guilds' zones.
- **Lifecycle**:
  - **Owner-triggered disband** — deliberate, irreversible. All Bank funds,
    Perks, and property (Guild Vendor, any guild structures) lost. Worth a
    real confirm-before-you-commit UX step at build time, matching how this
    project already treats other high-blast-radius actions.
  - **Automatic disband after 30 real days of total leadership vacuum** — the
    clock only starts once there's genuinely no one left at Owner *or*
    Officer rank. An active Officer alone keeps the guild alive indefinitely
    even if the Owner disappears, which means "Owner can promote an Officer"
    does double duty as both permission delegation *and* the actual
    continuity safeguard.

### Guild — still open, not resolved here

- **Full role permission matrix incomplete.** Only specific examples are
  pinned down (Officer spends the Bank/places Markers, Member buys
  personally). Whether an Officer can invite or kick members outright isn't
  itemized.
- **No cap on the number of Officers a guild can have** — an Owner could in
  principle promote all 63 other members. Not decided whether this should be
  bounded.
- **Vendor-stocking permission** (Team and Guild both) — who's allowed to add
  items to a shared vendor's stock isn't specified.

## Territory ownership — the unifying rule

Both territory systems (Team's federated Village-Flag zones, Guild's Sign/
Marker zones) ended up needing the same answer to "who owns a structure built
inside a zone," and it's the same rule for both, decided together:

**A zone is a permission gate only — "you must be a member to build here."
The builder explicitly chooses the actual owner (their Team, their Guild, or
personal) at placement time.** Ownership is never automatically inherited from
whichever zone a structure happens to sit in.

This is what makes the two systems compose cleanly instead of needing special-
case conflict rules: a Team's Village-Flag zone and a Guild's Sign/Marker zone
can freely overlap on the same ground with zero ambiguity (a team of
guildmates building their clubhouse on land that's both their team's and their
guild's territory just picks which one owns it when they place it) — the
"block overlapping placement" rule from Guild's section above stays scoped to
**Guild-vs-Guild only**, since that's the one case where two *competing*
claims over the same land would otherwise be genuinely ambiguous.

## Player Trade

A general mechanic, **not gated behind Team or Guild membership** — any two
nearby players can trade directly (wallet-to-wallet currency, item-to-item),
replacing "drop it on the ground and hope" as the only inter-player transfer
that exists today. Deliberately scoped as a base multiplayer feature rather
than a Team perk: strangers on a public Plan B server need the anti-scam
protection this provides far more than trusted teammates do, who barely need
it at all.

- **Atomic two-sided trade window** — both players offer into visible slots,
  both must explicitly confirm, and **either side changing their offer after
  a confirm resets both confirmations.** That reset-on-change behavior isn't
  optional polish, it's the actual anti-scam mechanism (stops the classic
  "confirm, then swap the item for junk" con) — a solved problem in the
  genre, not something to reinvent.
- Opens the same way `PlayerRenaming`'s right-click flow already works on
  NPCs/StorageBoxes — **blocked on the still-unbuilt real player-identity/
  naming system**, a shared prerequisite with Team's invite-by-name and the
  Map's guild-mate markers below.

## Map presentation

- **Player markers get their own shape**, distinct from NPC markers — not
  relying on color alone, since NPC markers already use color to mean *job
  status* (green=working, orange=payment-due, yellow=idle, blue=not-hired).
  Reusing the same color palette for a different meaning (player affiliation)
  on the same map would create real ambiguity (is a green dot an idle NPC or
  a teammate?), so shape carries "what kind of marker is this" and color
  carries "what does it mean" within that type.
- **Color**: Team color and Guild color are distinct. **Team takes priority**
  when someone is both a teammate and a guildmate — no blend, no ambiguity.
- **Unaffiliated players** (strangers, neither teammate nor guildmate) get no
  marker at all by default (an assumption, not explicitly confirmed) — keeps
  the map about *your* people rather than everyone on the server.
- **A legend in a map corner** lists what every color/shape means — Team,
  Guild, and the existing NPC status colors, all in one place. Reuses
  `MapScreen`'s existing rendering conventions (same kind of addition as the
  compass/waypoint UI already living there), no new mechanism.
- All of this reuses `DrawFlagMarkers`/`DrawNpcMarkers`'s existing "fresh scan
  every `OnGUI` frame, no cached state" pattern — a Team/Guild-affiliation
  marker pass is the same shape, just a third category.

### Guild Sign/Marker world-object visibility (separate from player markers above)

This is about the Guild Sign/Marker *structures themselves* showing up on the
map — a different thing from the Team/Guild-affiliation player markers above.

- **Visible to all players by default, labeled by guild name** — same
  treatment `DrawFlagMarkers` already gives every Village Flag (shown
  unconditionally, not gated by fog reveal). Consistent with an existing,
  shipped precedent rather than a new rule, and it actively serves the
  design: a player deciding which guild to join needs to be able to find and
  compare guilds, and a Guild Vendor's real economic balance (like Team
  Vendor's) depends on outside customers being able to find it, not just
  members.
- **A guild can opt out — a per-guild Public/Hidden toggle**, settable by
  Officer+ (same trust tier as placing a Guild Marker or spending the Bank).
  **Cosmetic only, not a physical cloak** — Hidden means no map marker for
  non-members; the Sign/Vendor/territory still physically exist and are
  walkable-into if someone finds them on foot. The guild's own members always
  see it regardless of the toggle — it only affects whether outsiders can
  *find* it via the Map, not whether it exists.
- The competitive-secrecy angle (not wanting a rival to scout territory
  before a raid) only matters once PvP/Settlement Warfare is real, which is
  explicitly out of scope here — this toggle exists for ordinary privacy
  preference (not wanting map-advertised traffic, not necessarily accepting
  new members right now), not designed around a combat system that doesn't
  exist yet.

## Cross-cutting connections found while designing this

- **Team Vendor + Guild Vendor together are the literal missing prerequisite**
  for `FAME_PLANNING.md`'s already-designed-but-blocked "business-reach Fame"
  mechanic — that doc explicitly named "an entire vendor/commerce system that
  doesn't exist in any form" as its biggest blocker. Once either vendor type
  ships, that Fame hook stops being blocked.
- **Guild's recipe-grant mechanism is a variant of the existing Skill Book
  system**, not a new one — same underlying exception-list plumbing, different
  revocation behavior.
- **Player identity/naming is a shared prerequisite** for three separate
  pieces here: general Player Trade, Team invite-by-name, and Guild-mate/
  Team-mate Map markers. Still unbuilt (`MULTIPLAYER_PLANNING.md` flagged this
  originally) — worth building once, reused by all three, not solved
  per-feature.
- **Territory reuses three already-proven patterns wholesale**, rather than
  inventing new ones: `CraftTierScale.VillageFlagRevealRadius` for Team's
  radius values, Tree/Boulder scatter placement's minimum-clearance check for
  the no-overlap rule, and `MapScreen`'s live-scan convention for
  contribution/marker computation.

## Explicitly not addressed here

- **Economy numbers** — dues amounts, Perk costs, Guild Vendor sales-cut
  percentage, Team Vendor split mechanics. Pure structure has been designed;
  no actual values exist yet. These need the same critical "check against
  existing tuned curves" pass every other economy addition in this project
  has gotten (Iron Arrow, Constitution/Dexterity, etc.) before anything is
  locked in — not attempted here.
- **PvP/friendly-fire interaction** with Team or Guild membership — explicitly
  out of scope, belongs to whatever the eventual Settlement Warfare/PvP phase
  (`docs/design-brief.md`) looks like, not assumed or designed here.
- **Implementation complexity note, not a design gap**: Team and Guild are
  genuinely harder to build than a typical single-player-script conversion,
  because they're relationships *between* players (concurrent kicks,
  concurrent Bank spends, promote/demote races) rather than one player's own
  server-validated actions. Worth carrying forward as a real cost estimate,
  not folded into the general "convert the 48 `PlayerXXX.cs` scripts" phase
  `MULTIPLAYER_PLANNING.md` already describes.

## Cross-references

- `MULTIPLAYER_PLANNING.md` — the parent doc; this design depends on its
  Phase 2 (player-authoritative gameplay) and player-identity prerequisite
  landing first. Nothing here is buildable before those exist.
- `COMMERCE_PLANNING.md` — `VendorStall`'s driver architecture, reused
  directly for both Team Vendor and Guild Vendor as new drivers.
- `FAME_PLANNING.md` — business-reach Fame, unblocked once either vendor type
  ships.
- `SKILL_BOOKS_PLANNING.md` — the recipe/wish-grant mechanism Guild's version
  reuses, with the one deliberate difference (revocable, not permanent).
- `VILLAGE_FLAG_PLANNING.md` — the placement/rename/spawn pattern Guild Sign/
  Marker and Team's territory system both build on.
- `CLAUDE.md`'s tier-scaling gotcha — explicitly checked and confirmed *not*
  to apply to Team's territory-radius reuse (same quantity type both times).
