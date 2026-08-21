# Custom Avatar & Creature Modeling — Planning

Scoped 2026-08-20, prompted by a conversational exploration of whether Blender
could replace Gridless's current placeholder character assets with custom-built
ones, for both the human Player/NPC model and the game's animal creatures.

## Current state (audited, not assumed)

- **Humans** run on a third-party asset pack: `Assets/Kevin Iglesias/Human
  Character Dummy/` (`HumanCharacterDummy_M.fbx` / `_F.fbx`) plus a matching
  `Human Animations` pack, used for both the Player and NPCs (`NPCFactoryWorker*`
  prefabs). `CLAUDE.md` already documents one real gotcha with this pack's
  materials (`HumanDummy*.mat` shipped on a legacy Built-in-only shader, fixed
  2026-08-14 — see that file's own entry).
- **Animals** are three real creature prefabs today, confirmed via a direct
  search of `Assets/Prefabs/` for `HostileCreature`/`PreyCreature` usage:
  **Wolf** (`HostileCreature`), **Rabbit** and **Pig** (`PreyCreature`). Both
  base classes derive from a shared `SkinnableCreature`. There is **no live
  Chicken creature prefab** — `ChickenMeatPickup`/`EggPickup` exist as items,
  implying a Chicken is planned or was at least a data source, but no
  `HostileCreature`/`PreyCreature` instance for it exists yet.
- Creature species today are distinguished by prefab + data, not by C#
  subclass — `HostileCreature.cs`/`PreyCreature.cs` are the only two classes,
  each reused across every species of its kind.

## Decisions locked in (Ben's calls, 2026-08-20)

- **Human replacement is mesh-only.** The existing Kevin Iglesias rig and
  animation clips stay — the new body must match that rig's exact bone
  names/hierarchy and bind pose so the current Animator Controller and every
  existing clip keep working unmodified. This is deliberately the smaller,
  lower-risk half of this project: no new rig, no new animations, no
  Animator/retargeting work.
- **Animals are in scope as a full roster**, not a single pilot-and-stop —
  Wolf, Rabbit, and Pig at minimum (Chicken is an open question, see below).
  Unlike the human case, animals get a **full custom pipeline each**: new
  mesh, new quadruped rig, new weight paint, new animations. There is no
  "one base + swap clothes" shortcut for animals the way there is for humans
  — each species is structurally its own character build.

## Why this isn't one undifferentiated task

The human and animal halves of this project have very different risk/effort
shapes and should be planned (and built) as separate tracks:

| | Human | Animals (each species) |
|---|---|---|
| Rig | Reuse existing (must match exactly) | New quadruped rig per species |
| Animations | Reuse existing clips | New animation set per species |
| Repeat cost after first one | N/A (one body, clothing is the repeat unit) | Full pipeline repeats per species |
| Primary risk | Matching bind pose/bone structure closely enough that existing clips don't break | Building the quadruped rig/weight-paint/animation pipeline correctly the first time |

## Human pipeline (mesh only)

1. Inspect `HumanCharacterDummy_M.fbx`/`_F.fbx` directly in Blender to record
   the exact bone names, hierarchy, and bind (rest) pose — this is the
   contract the new mesh must satisfy, not something to eyeball.
2. Model/sculpt the new body at the same real-world scale Gridless already
   uses (`CLAUDE.md`'s player-scale rule: `CharacterController` height 1.8,
   1 world unit = 1 meter) and roughly the same proportions as the existing
   dummy, so equipped gear/clothing sizing doesn't need to be redone.
3. Weight-paint the new mesh to the **existing** skeleton (import the rig
   from the current FBX rather than building a new one) — this is what makes
   "mesh only" true; a mismatched bind pose or bone naming breaks retargeting
   even if the mesh itself looks correct.
4. Before calling it done, play back the existing animation clips (idle,
   walk, run, melee, etc. — whatever the current Human Animations pack
   drives) against the new mesh and check for visible deformation breakage
   at the joints, not just a static T-pose comparison.
5. Swap the mesh reference on Player/NPC prefabs. Re-verify the
   `HumanDummy` material gotcha doesn't recur (new mesh needs its own
   correctly-URP material, not a copy of a broken legacy one).
6. Follow up with new clothing built to fit this body, same fitted-garment
   workflow already discussed conversationally (model against the body,
   weight to the same rig, export FBX with matching bone names).

## Animal pipeline (full custom, per species)

For each species (Wolf first, as the pipeline pilot even though the full
roster is in scope):

1. Model/sculpt the creature mesh at real-world scale relative to the
   1.8m-player reference (`CLAUDE.md`'s existing model-scaling rule already
   applies here, not just to imported single-object props).
2. Build a quadruped skeleton — structurally different bone chain from the
   human biped rig (spine/leg/tail topology), no existing asset to match
   against since these are being built from scratch.
3. Weight-paint the mesh to the new rig.
4. Author the animation set each species actually needs based on its
   `HostileCreature`/`PreyCreature` behavior today — at minimum idle, move
   (walk/run), and whatever `HostileCreature`'s `Idle`/`Chasing`/`Attacking`
   states and `PreyCreature`'s flee behavior require. Check each script
   directly for the actual states driven before finalizing the animation
   list — don't guess a generic animal set.
5. Export, wire into the existing prefab (`Wolf.prefab`/`Rabbit.prefab`/
   `Pig.prefab`), verify `SkinnableCreature`'s skin/loot/death visuals still
   work against the new mesh.
6. Repeat for the next species — expect each repeat to be faster once the
   quadruped pipeline is proven on the first one, but budget it as a real
   repeat cost, not a copy-paste.

## Open questions (not yet decided)

- **Is Chicken in scope now, or deferred until a live Chicken creature is
  actually built?** No `HostileCreature`/`PreyCreature` Chicken exists today
  — building a custom Chicken model ahead of the creature itself existing
  would be building art before there's a script/prefab to attach it to.
- **Equipment for animals** — the original conversation's "equipment that
  fits the avatar" was framed around human gear. Whether animals need any
  wearable equipment (tack/collars on a future tameable creature, etc.) is
  undesigned; nothing in the current creature scripts implies animals wear
  anything today.
- **Build order across the two tracks** — human (lower risk, matches
  existing rig) vs. Wolf-as-animal-pilot (higher risk, proves a pipeline
  with no existing asset to fall back on) haven't been sequenced against
  each other yet.

## Not yet built

Planning only — nothing modeled, rigged, or committed yet.
