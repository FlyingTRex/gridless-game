# Hunter NPC — Planning

Planning only, 2026-08-20 — not built. Prompted by Ben asking whether a
"gather" NPC could be extended to skin animals, which surfaced a bigger real
gap: no NPC in this project can kill anything, and no NPC can skin a corpse
either (`SkinnableCreature` implements neither `INPCHarvestable` nor
`INPCSearchable` — confirmed by direct grep). A Hunter is a new fourth
`NPCJobDefinition.JobKind` (alongside Gathering/Crafting/Guarding), with its
own sibling component, `NPCHunting.cs` — not an extension of `NPCGathering`
or `NPCGuarding`, since it genuinely needs pieces of both (a combat loop to
kill prey, a harvest loop to skin corpses) and neither existing component's
`kind`-gate convention is meant to carry two roles at once.

## Be-mean pass — real risks found and how each got resolved

**Risk 1 — a Hunter that also fights Wolves risks permanent NPC death for a
resource-gathering role, not a defense role.** `HostileCreature` fights
back; `NPCVitals`/`IDamageable` death is explicitly permanent
(`NPCVitals.Die()`'s own design). Losing a Hunter to a Wolf while it was
only ever meant to harvest Rabbits would be a bad trade.
**Resolved**: Hunter's own kill AI only ever targets `PreyCreature` — never
initiates combat against `HostileCreature`.

**Risk 2 — "prey only" seemed to mean Hunter could never get a Wolf pelt at
all, which felt like a real capability gap** (Ben's question: "if the
hunter sees a dead wolf, would it still harvest it?").
**Resolved, and it costs nothing extra**: the corpse-scavenging half of
this design scans for *any* `SkinnableCreature` with `IsDead == true` in
range — it doesn't care about subclass. A dead Wolf, however it died
(player kill, a Guard's kill, anything), is automatically eligible. Hunter
kills prey itself, and opportunistically scavenges whatever corpses it
finds — including Wolves — without ever having picked a fight with one.

**Risk 3 — does a scavenging Hunter risk getting attacked by a *live* Wolf
while walking near one to reach a corpse?** Checked `HostileCreature`
directly: since the Guarding build (2026-08-16), a Wolf only tracks the
player or whoever's `RedirectAggro`'d it after landing a hit — it does not
proactively attack an arbitrary nearby NPC. A Hunter that never attacks a
Wolf is never attacked by one either. **Confirmed safe — no `NPCVitals`/
`IDamageable` needed on the Hunter for this scenario; it has no exposure to
combat at all by construction, not by luck.**

**Risk 4 — local wildlife population collapse.** An efficient Hunter
actively seeking out every `PreyCreature` in its leash radius draws down
the same finite, slowly-respawning pool
(`SkinnableCreature.respawnDelay`, 180s default per creature) the player's
own hunting also relies on.
**Reframed by Ben, correctly**: this isn't really Hunter-vs-player
competition — `NPCHunting` inherits the exact same `DepositContainer`
mechanism `NPCGathering` already has (walk to the player-assigned
StorageBox once full, deposit, resume). The Meat/Leather/Pelt a Hunter
harvests doesn't vanish into an NPC-only sink, it lands in a box the
player owns — net new supply, not consumed supply. So a Hunter is a
straightforward output boost to the player's Cooking/Medical Raw Meat
pipeline and leather/pelt stock, the same way a Woodworking or Mining NPC
already boosts Plank/Ore supply rather than competing for it.
**What's still a real, smaller-scoped risk**: purely the *local wildlife
density* near one settlement running thin faster than it regrows if
multiple Hunters (or a Hunter plus the player) both draw from the same
small radius — an ecological pacing question, not a player-supply
question. Worth watching in live testing; if it's a real problem the fix
is the same one already proposed for "NPCs work too fast" elsewhere in
`BUGS_AND_ENHANCEMENTS.md` — lengthen the node's own respawn delay rather
than throttling the NPC's action speed directly.

**Risk 5 — inventing a new "Hunting" skill would deepen a gap
`ENDGAME_PLANNING.md` already flagged** (Combat has no unifying skill
across Melee/Archery/Guarding). **Resolved**: Hunter trains **Archery**,
the same skill `PlayerRangedCombat` and Guard (Ranged) already train — one
more real use of an existing skill, not a fifth combat-adjacent skill
fragmenting the discipline further.

**Risk 6 — true animated gear-swapping (holster bow, draw knife) would be
new visual-attach machinery for a purely cosmetic payoff.**
`NPCEquipmentVisual` currently attaches each `ToolRequirement` to a fixed
bone for the job's entire duration — no per-state attach concept exists.
**Resolved**: equip Bow + Arrow + Knife simultaneously, exactly the pattern
`GuardRangedJob.asset` already ships (two simultaneous `ToolRequirement`
entries, Weapon at bone 18, Arrow at bone 17) — a Hunter just adds a third.
No new visual system needed.

**Risk 7 (not fully resolved, flagged for build time)** — `PreyCreature`
currently has **no flee/wander-away behavior at all** ("this is NOT yet
the full Prey Creature archetype... that behavior still doesn't exist," per
its own header comment) — prey are stationary today. That makes a Hunter's
kill loop trivial to build now (walk into range, shoot, no chase needed),
but it's an implicit dependency: if flee behavior ever ships, `NPCHunting`
would need a real Chasing state added on top, the same shape `NPCGuarding`
already has. Not a reason to delay this build — just don't assume today's
simplicity is permanent.

## Design

### New `JobKind.Hunting` + `NPCHunting.cs`

Sibling to `NPCGathering`/`NPCCrafting`/`NPCGuarding` on the same prefab,
same "always present, bail early if `job.AssignedJob.kind != Hunting`"
convention. Two internal states, not the three `NPCGuarding` needs (no
Chasing today, per Risk 7):

- **Hunting** — scan for the nearest live `PreyCreature` within
  `searchRadius`/leash-from-deposit (same `WithinLeash` pattern
  `NPCGathering` already has). Walk into `rangedAttackRange`, fire — reuse
  `FlyingArrow.cs` the same way `NPCGuarding`'s ranged-Guard attack already
  does. No damage-back risk (prey doesn't fight).
- **Scavenging** — scan for the nearest dead `SkinnableCreature`
  (`IsDead == true`, any subclass) within range. Walk up, trigger skin,
  loot lands in `NPCCargo`.

Falls back to whichever pool has a target; if both are empty, behaves like
an idle Gathering NPC with nothing left in range (same "walk to deposit
container, wait" fallback).

### Deposit — same `NPCCargo`/`DepositContainer` mechanism as every other job

`NPCHunting` reuses `NPCJob.DepositContainer`/`NPCCargo` exactly as-is —
once cargo is full (or nothing's left in range), walk to the player-
assigned StorageBox and deposit, same as Mining/Woodworking/Forage already
do. This is what makes Risk 4 below a net positive rather than a
competition: harvested Meat/Leather/Pelt lands in the player's own box, a
real supply boost to Cooking/Medical's Raw Meat pipeline and to whatever
Leather/Pelt-consuming recipes exist, not consumption that competes with
the player for the same resource.

### Corpse-skinning needs a new interface, not a reuse of the existing two

`SkinnableCreature.DropLoot` yields **two distinct item types at once**
(Pelt + Meat, or LootA/B/C) — doesn't fit `INPCHarvestable`'s
single-item-plus-count contract. And unlike `INPCSearchable`
(BerryBush/HerbBush), there's no reason to scatter loot as loose Pickups
first and collect it on a second pass — the corpse doesn't move, so a
direct-to-cargo yield (skip the scatter, same precedent
`ChoppableTree.TryHarvestForNPC` already established for its own single-
item case) is strictly simpler.

Proposed new interface, **`INPCSkinnable`**:
```csharp
public interface INPCSkinnable
{
    bool IsAvailable { get; }   // isDead && not already claimed by another NPC
    ItemDefinition[] RequiredTools { get; }
    float SkillGain { get; }
    Transform transform { get; }

    // Direct-to-cargo yield, no scatter -- caller (NPCHunting) already
    // verified RequiredTools and encumbrance before calling this, same
    // convention as TryHarvestForNPC.
    bool TrySkinForNPC(List<(ItemDefinition item, int count)> results);
}
```
`SkinnableCreature` implements this once in the shared base — subclasses'
existing `DropLoot(PlayerDropping)` stays untouched for the player path;
`TrySkinForNPC` is a new parallel method that runs the same
chance/count rolls but appends to the passed list instead of calling
`dropping.SpawnPickup`. `Complete()`'s tool-check/skill-gain lines get
extracted into a small shared helper both the player path and this new
NPC path call, rather than duplicated.

### Tool loadout

New `HunterJob.asset`, three simultaneous `ToolRequirement`s — Weapon (bow,
reuse Guard Ranged's acceptable-items list and bone 18), Arrow (reuse Guard
Ranged's list and bone 17), Knife (new — bone/offset TBD at build time,
likely hip-attached given the model). `family = Archery`. Arrow supply:
inherits Guard (Ranged)'s existing "permanent equipped loadout, no stock
consumption" simplification (`CLAUDE.md`'s own documented v1 call) rather
than inventing per-shot consumption just for this job.

### `NPCGathering.FindTarget`'s pool-gating pattern applies here too

Not literally reused (Hunter isn't Gathering-kind), but the same
lesson from `searchesBushes`/`collectLoosePickups`/`harvestsToollessRock`
applies: **only Hunter should ever scan the live-prey and dead-corpse
pools.** Don't let Guard or Forage NPCs accidentally start targeting a
`PreyCreature` or a corpse just because the pool exists — gate both scans
inside `NPCHunting.cs` itself, which only runs at all when
`kind == Hunting`, so no extra boolean flags are even needed here (unlike
Gathering's shared single component serving multiple job flavors).

## Build order (proposed, not committed)

1. `INPCSkinnable` interface + `SkinnableCreature.TrySkinForNPC` +
   extracted tool/skill-check helper. Verify against the player's own skin
   flow still working unchanged (regression risk: this touches
   `Complete()`, a live player-facing action).
2. `JobKind.Hunting` enum value + `NPCHunting.cs` (Hunting state only —
   walk into range, fire, reuse `FlyingArrow`).
3. Scavenging state (dead-corpse pool scan + `TrySkinForNPC` call +
   `NPCEncumbrance.CanPickUp` gate, mirroring `ConsiderHarvestable`'s
   shape).
4. `HunterJob.asset` (Bow + Arrow + Knife, family = Archery) +
   `NPCJobScreen` wiring (should need zero new UI code — it's already
   data-driven off `NPCJobDefinition`).
5. Live-test: confirm prey kill works, confirm scavenging picks up both
   self-killed and pre-existing corpses (including a player- or
   Guard-killed Wolf), confirm no combat exposure ever triggers against
   the Hunter near a live Wolf.

No code has been written for any of this yet.
