# NPC Guarding Planning

**Status: built, v0.3.106-dev (2026-08-16)** — see `CHANGELOG.md`'s
v0.3.106-dev entry for the built shape. Closes out MVP2 item 2 in full.
Left in present/future tense below rather than rewritten past-tense, same
convention `NPC_JOB_GENERALIZATION_PLANNING.md` uses for its own built
sections. Built as decision-locked — every open question in section 7
stayed a first-pass number/judgment call rather than getting resolved
further at build time. Verified via batch-mode compile + direct YAML
grep only so far — not yet live-tested in Play mode.

Planning doc for the "Guarding" NPC job (2026-08-16) — the one job family
flagged as unstarted since `NPC_JOB_GENERALIZATION_PLANNING.md` (2026-08-13)
and `BUGS_AND_ENHANCEMENTS.md`. Designed conversationally with Ben, decision-
locked where noted.

## 1. Why this is a bigger build than the other job families

Every other job family (Mining/Woodcutting/Foraging/Metalworking) reused
existing infrastructure — a target to interact with, a skill to train, tools
to equip. Guarding needs three things that don't exist anywhere in the
project yet:

1. **A real NPC health/death system.** Hired NPCs have never been
   `IDamageable` — `FAME_PLANNING.md`'s "kill an NPC" Fame input has been
   explicitly blocked on this exact gap since 2026-08-14. A Guard that can
   fight a Wolf needs to be able to take damage and actually die.
2. **NPC-initiated combat.** Every existing attack in this project is
   player-driven (`PlayerCombat`/`PlayerRangedCombat`) or a hostile
   creature attacking the player (`HostileCreature`). Nothing today has an
   NPC dealing damage to something else.
3. **Patrol movement**, not gather-and-deposit or walk-to-a-surface —
   Ben's own framing: circle around the village at the Village Flag's own
   visible range, "would simulate patrolling around the village."

## 2. NPC health & death — `NPCVitals.cs`

**Decided (Ben): real health, can actually die.** New component,
`IDamageable`, mirroring `SkinnableCreature`'s `TakeDamage`/`Die` shape but
without the skin/loot/respawn half — a Guard is a person, not a resource
node.

- `maxHealth`/`health`, `TakeDamage(amount)` reduces it, `<= 0` triggers
  `Die()`.
- **Death is permanent, not a respawn** (unlike `SkinnableCreature`'s
  `respawnDelay`) — `Die()` clears the job (`NPCJob.ClearJob()`, same
  tool-loss-for-good convention `NPCHiring.Fire()` already uses) and
  destroys the GameObject. No corpse, no loot, no skinning — this isn't a
  creature kill.
- **No Fame consequence for now.** `FAME_PLANNING.md`'s "-10 for killing a
  humanoid NPC" was written for the *player* killing an NPC directly, not
  a Guard dying to a Wolf — out of scope here, not touched.
- **Regen**: not decided in detail — leaning toward a slow out-of-combat
  regen (same spirit as `PlayerVitals`), a first-pass number to tune, not
  a hard design point.
- Only `NPCGuarding`-assigned NPCs need this in practice, but the
  component lives generically (any future NPC job that can take damage
  reuses it) rather than being Guard-specific.

## 3. Weapon equipping — two job definitions, not one

**Decided (Ben): melee and ranged, both supported in v1.** The existing
`ToolRequirement` shape (`NPCJobDefinition.toolRequirements`) requires
*every* listed requirement to be filled before `NPCJob.IsReady` — a melee
Guard doesn't need an Arrow slot, and a static per-job requirement list
can't conditionally drop one based on which weapon ends up equipped.

**Resolution: two `NPCJobDefinition` assets, one shared family.**
- **`GuardMeleeJob`** — `toolRequirements = [Weapon: all isMeleeWeapon
  items (Knife tiers, any future melee weapon)]`.
- **`GuardRangedJob`** — `toolRequirements = [Weapon: all isRangedWeapon
  items (Bow tiers), Arrow: all isArrow items]`.
- Both `family = Guarding` (new `SkillDefinition`), both `kind =
  JobKind.Guarding` (new enum value, section 5). They show as two tiles
  under one "Guarding" family tab in `NPCJobScreen` — zero UI changes
  needed, same family-tabs-then-job-tiles shape every other family
  already uses.
- `NPCGuarding.cs` (section 4) branches its actual attack behavior at
  runtime by checking which weapon is actually equipped
  (`item.isMeleeWeapon`/`isRangedWeapon`), same pattern
  `PlayerCombat.ResolveAttack`/`PlayerRangedCombat.GetEquippedBowAndArrow`
  already use — not by which job definition was assigned.

## 4. Combat behavior — `NPCGuarding.cs`

New sibling component to `NPCGathering`/`NPCCrafting`, same
`RequireComponent`-always-present-on-the-prefab, bail-early-if-wrong-kind
convention. Reuses `HostileCreature`'s own `Idle`/`Chasing`/`Attacking`
state shape, just retargeted at threats instead of the player, and adds a
patrol state on top.

- **Patrol anchor: the nearest Village Flag.** Radius = that Flag's own
  `CraftTierScale.VillageFlagRevealRadius(tier)` (35m Crude up to 75m
  Masterwork) — Ben's own framing, direct reuse of the table built for
  the Player Map earlier the same session. The Guard continuously moves
  along this circle rather than standing still.
- **No Village Flag placed**: falls back to standing at its current spot
  (no patrol path to walk), same "nothing to do, don't crash" fallback
  `NPCCrafting`/`NPCTraining` already use when their own prerequisite
  isn't met.
- **Detection ring** travels with the Guard as it patrols (not fixed to
  the Flag) — any `HostileCreature` within range breaks patrol and
  switches to Chasing, mirroring `HostileCreature`'s own
  detection/give-up/attack-range fields but scanning for
  `FindObjectsByType<HostileCreature>()` instead of the player.
- **Attacking**: melee closes to attack range and hits on a cooldown,
  damage = `punchDamage`-equivalent + `CraftTierScale.WeaponDamageBonus`
  (mirrors `PlayerCombat.ResolveAttack` exactly, just without a player
  swing input driving it). Ranged fires on a fixed cooldown (no manual
  draw-and-hold — an NPC doesn't need the player's variable-draw
  mechanic), damage = base roll + `ArrowDamageBonus`/`BowDamageBonus`
  (mirrors `PlayerRangedCombat.Fire`'s damage math, simplified timing).
  Both consume the target's health via the same `IDamageable.TakeDamage`
  every other attack in this project already uses — `HostileCreature`
  needs no changes at all to be a valid Guard target.
- **After the threat dies or flees out of `giveUpRadius`**: returns to
  patrolling the Flag's ring.
- **Trains the new Guarding skill** on a successful hit, same per-action
  training shape Archery/Melee already use.

## 5. `NPCJobDefinition.JobKind` gains `Guarding`

Third enum value alongside `Gathering`/`Crafting` (`NPC_JOB_
GENERALIZATION_PLANNING.md` section 7 / `CHANGELOG.md` v0.3.101-dev).
`NPCGathering`/`NPCCrafting` already early-out on a mismatched kind — this
is one more sibling in the same pattern, no changes needed to the other
two.

## 6. "Negative Fame players" — logged as a multiplayer-only hook, not built

**Decided (Ben): this targeting rule is about other players once
multiplayer exists, not the single local player today.** Explicitly *not*
building "Guard attacks the player when their own Fame goes negative" —
`NPCFlee.cs` already covers the negative-Fame-player reaction for ordinary
NPCs (fleeing), and inverting that for Guards specifically would be a real,
testable single-player mechanic, but it's not what was asked for here.
Left as a design note for whenever `MULTIPLAYER_PLANNING.md`'s player-vs-
player Fame visibility becomes real — `NPCGuarding`'s target scan is
written to consider "hostiles" generically (`HostileCreature` today), so
adding a second hostile-target type later doesn't need new plumbing, just
a second `FindObjectsByType` scan added to the same detection check.

## 7. Explicitly out of scope for this pass

- Exact `NPCVitals` starting health / regen rate — first-pass numbers,
  not designed in detail.
- Whether multiple Guards patrolling the same Flag coordinate at all (do
  their patrol circles overlap/collide?) — not designed, likely fine to
  ignore for a first pass.
- A Guard's equipped weapon/tools on death — lost for good, same
  convention `NPCHiring.Fire()` already uses, not returned to any
  inventory.
- Negative-Fame player targeting (section 6) — logged, not built.
- Any Fame consequence for a Guard's death.

## Cross-references

- `HostileCreature.cs` — the AI state shape (`Idle`/`Chasing`/
  `Attacking`) this reuses, retargeted.
- `SkinnableCreature.cs` — the `IDamageable`/`TakeDamage`/`Die` shape
  `NPCVitals.cs` mirrors (without the skin/loot/respawn half).
- `PlayerCombat.cs`/`PlayerRangedCombat.cs` — the damage-formula/weapon-
  detection patterns `NPCGuarding`'s own attack resolution reuses.
- `CraftTierScale.VillageFlagRevealRadius` — the exact table the patrol
  radius reuses, built earlier the same session for the Player Map.
- `NPC_JOB_GENERALIZATION_PLANNING.md` section 7 / `NPC_TRAINING_
  PLANNING.md` / `VILLAGE_FLAG_PLANNING.md` — the three prior job-family
  builds this follows the same sibling-component/family-tab conventions
  from.
- `FAME_PLANNING.md` — the still-blocked "kill an NPC" Fame input this
  doc's new `IDamageable` NPCs would technically unblock, but doesn't
  build.

Built, v0.3.106-dev.
