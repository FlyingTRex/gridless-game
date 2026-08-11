# Third-Party Model Credits

Running ledger of every non-AI-generated, non-procedural model brought
into `Assets/Models/` and the exact attribution text its license
requires. Check this before shipping anything — every entry here needs
to actually appear in `GameMenuScreen`'s Credits tab
(`Assets/Scripts/GameMenuScreen.cs`, `DrawCreditsTab()`) before release.
**As of 2026-08-07, the actively-shipping entries below (Tree branch,
Stone, Big Tree) are in the Credits tab** — Ben caught the initial gap
by actually checking the in-game Credits screen during a playtest.

Distinct from `Tools/Tripo3D/README.md`, which tracks AI-generated
models and their own (different) licensing situation.

## Big Tree by 3Donimus

- File: `Assets/Models/BigTree_3Donimus.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: CC-BY
- **Required attribution text (exact, from the download popup):**
  `Big Tree by 3Donimus [CC-BY] via Poly Pizza`
- Status (2026-08-06): imported and placed in `TestScene.unity` for
  visual comparison against the procedural tree and the (pending)
  Tripo3D-generated one.
- **Update (2026-08-07, v0.1.91-dev):** made choppable — added
  `ChoppableTree` (same component the procedural Tree uses) plus a
  `CapsuleCollider`, per Ben's request. No longer comparison-only; now
  an actively-used gameplay object. **In the Credits tab as of
  v0.1.91-dev.**

## Tree branch by Poly by Google

- File: `Assets/Models/TreeBranch_PolyByGoogle.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: CC-BY
- **Required attribution text (exact, from the download popup):**
  `Tree branch by Poly by Google [CC-BY] via Poly Pizza`
- Status (2026-08-06): replacing the Stick item's visual (both
  `Assets/Prefabs/StickPickup.prefab` and the two pre-placed world
  pickups in `TestScene.unity`) — this one **is** actively being used,
  not just a comparison object. **In the Credits tab as of 2026-08-07.**

## Stone by Poly by Google

- File: `Assets/Models/Stone_PolyByGoogle.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: CC-BY
- **Required attribution text (exact, from the download popup):**
  `Stone by Poly by Google [CC-BY] via Poly Pizza`
- Status (2026-08-07): replacing the Rock Node's visual (the main
  punchable resource, previously a plain built-in Sphere primitive), and
  also `Assets/Prefabs/RockChunk.prefab` (the Small Rock pieces that
  scatter when Rock Node breaks — non-uniformly scaled smaller/differently
  so it doesn't read as a shrunk clone of the parent). Actively being
  used, not a comparison object. **In the Credits tab as of 2026-08-07.**

## Rock by Quaternius

- File: `Assets/Models/Rock_Quaternius.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: **Public domain** — no attribution required, but Ben can
  optionally credit the creator per the download popup:
  `Rock by Quaternius`
- Status (2026-08-07): replacing the Boulder's visual — previously a
  hand-tuned procedural shape (displaced-mesh body + 8 clustered
  pebbles, `CHANGELOG.md` v0.1.62-dev), not a crude placeholder like
  Rock Node's old sphere was. Actively being used. Public domain means
  this one doesn't strictly need a Credits tab entry, but worth listing
  here anyway for sourcing/tracking consistency with everything else in
  this ledger.

## Grass Wispy by Quaternius

- File: `Assets/Models/GrassWispy_Quaternius.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: **Public domain** — no attribution required, but Ben asked
  for the full credits treatment anyway per the download popup:
  `Grass Wispy by Quaternius`
- Status (2026-08-08, v0.1.146-dev): replacing `Fiber.asset`'s missing
  `worldPickupPrefab` — the item existed since early in the session
  (Trimmed Stick yields Fiber) but had no visual/icon at all until now.
  Actively being used. **In the Credits tab as of v0.1.146-dev.**

## Low Poly Axe by suerozcelik

- File: `Assets/Models/Axe_suerozcelik.fbx`
- Source: [Poly Pizza](https://poly.pizza)
- License: CC-BY — attribution required.
- **Required attribution text (exact, from the download popup):**
  `Low Poly Axe by suerozcelik [CC-BY] via Poly Pizza`
- Status (2026-08-08, v0.1.142-dev): first real model for the Axe
  CraftTier ladder — same visual reused across all 5 tiers (Crude/
  Rudimentary/Normal/Fine/Masterwork), which previously had real
  recipes but zero model/icon/`worldPickupPrefab` at all. First `.fbx`
  import this session (every prior model was `.glb`) — Unity's native
  FBX importer handled it directly, no separate texture files needed
  (materials/colors came through intact). Actively being used. **In
  the Credits tab as of v0.1.142-dev.**

## Pickaxe by CreativeTrio

- File: `Assets/Models/Pickaxe_CreativeTrio.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: **Public domain** — no attribution required, but Ben asked
  for the full credits treatment anyway per the download popup:
  `Pickaxe by CreativeTrio`
- Status (2026-08-08, v0.1.141-dev): first real model for the Pickaxe
  CraftTier ladder — same visual reused across all 5 tiers (Crude/
  Rudimentary/Normal/Fine/Masterwork), which previously had real
  recipes but zero model/icon/`worldPickupPrefab` at all. Actively
  being used. **In the Credits tab as of v0.1.141-dev.**

## Strawberries by Jarlan Perez

- File: `Assets/Models/Strawberries_JarlanPerez.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: CC-BY — attribution required.
- **Required attribution text (exact, from the download popup):**
  `Strawberries by Jarlan Perez [CC-BY] via Poly Pizza`
- Status (2026-08-08, v0.1.139-dev): replacing `BerryPickup.prefab`'s
  placeholder Sphere (the world/held visual for the `Berry` item).
  Also replaced the pre-placed "Berry Bush" scene object in
  `TestScene.unity`, which turned out to be a standalone copy rather
  than a real `PrefabInstance` (same bug class as Canteen/Backpack
  earlier this session) — the model swap wouldn't have reached it
  otherwise. Actively being used. **In the Credits tab as of
  v0.1.139-dev.**

## Wolf by Quaternius

- File: `Assets/Models/Wolf_Quaternius.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: **Public domain** — no attribution required, but Ben asked
  for the full credits treatment anyway per the download popup:
  `Wolf by Quaternius`
- Status (2026-08-10, v0.1.190-dev): first Basic Combat target —
  `HostileCreature` (idle/detect/chase/attack/death), skinnable with a
  Knife once dead for Wolf Pelt + Raw Meat, first real user of the new
  `Bare-handed` weapon-usage skill (`SkillCategory.Combat`, previously
  design-only). Actively being used. **In the Credits tab as of
  v0.1.190-dev.**

## beef steak by Dario Demi (D911C)

- File: `Assets/Models/BeefSteak_DarioDemi.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: CC-BY — attribution required.
- **Required attribution text (exact, from the download popup):**
  `beef steak by Dario Demi (D911C) [CC-BY] via Poly Pizza`
- Status (2026-08-10, v0.1.190-dev): `RawMeat.asset`'s `worldPickupPrefab`
  — Raw Meat drops from skinning a killed Wolf (`HostileCreature`).
  Actively being used. **In the Credits tab as of v0.1.190-dev.**

## Wood Planks by Quaternius

- File: `Assets/Models/WoodPlanks_Quaternius.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: **Public domain** — no attribution required, but Ben asked
  for the full credits treatment anyway per the download popup:
  `Wood Planks by Quaternius`
- Status (2026-08-08, v0.1.137-dev): replacing `PlankChunk.prefab`'s
  placeholder Cube (the chunk `Log.prefab` actually drops when
  chopped, and now also `Plank.asset`'s `worldPickupPrefab`, wired for
  the first time). Actively being used. **In the Credits tab as of
  v0.1.137-dev** — unlike Rock by Quaternius above, added to the live
  tab despite being public domain, per Ben's explicit request this
  time.

## SD Macross Factory Worker by Tipatat Chennavasin

- File: `Assets/Models/NPCFactoryWorker.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: CC-BY — attribution required.
- **Required attribution text (exact, from the download popup):**
  `SD Macross Factory Worker by Tipatat Chennavasin [CC-BY] via Poly Pizza`
- Status (2026-08-10): first step toward Hireable autonomous NPCs (Phase
  1's last unbuilt item) — Ben's explicit request. Raw import was a
  static (no armature/animations) 6-mesh chibi/SD figure roughly 0.71m
  tall; rejoined into one mesh, uniformly scaled to a 1.4m target
  height, and re-origined to feet-at-ground in Blender before export so
  no additional Unity-side scale was needed (avoiding the world/local
  collider-scale bug hit on `RawMeatPickup`/`Wolf` earlier this
  session — collider numbers here were hand-computed directly in local
  space instead). Placed once in `TestScene.unity` with a new
  `NPCWander` component (idle wander within a radius of its spawn
  point, same flat-ground `Vector3.MoveTowards` approach as
  `HostileCreature`, no NavMesh) — deliberately no interaction, dialogue,
  or AI yet, since nothing about NPCs is designed beyond the name.
  **In the Credits tab as of this entry.**
