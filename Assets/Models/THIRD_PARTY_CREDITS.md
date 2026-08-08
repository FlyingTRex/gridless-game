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
