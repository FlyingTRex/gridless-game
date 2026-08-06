# Third-Party Model Credits

Running ledger of every non-AI-generated, non-procedural model brought
into `Assets/Models/` and the exact attribution text its license
requires. Check this before shipping anything — every entry here needs
to actually appear in `GameMenuScreen`'s Credits tab
(`Assets/Scripts/GameMenuScreen.cs`, `DrawCreditsTab()`) before release;
as of 2026-08-06 that tab still only lists "Tekim" / "the T-Rex" and
does NOT yet include any of the below.

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
  Tripo3D-generated one. Not yet decided whether this is the one that
  actually ships — attribution only needs to land in the Credits tab if
  it does.

## Tree branch by Poly by Google

- File: `Assets/Models/TreeBranch_PolyByGoogle.glb`
- Source: [Poly Pizza](https://poly.pizza)
- License: CC-BY
- **Required attribution text (exact, from the download popup):**
  `Tree branch by Poly by Google [CC-BY] via Poly Pizza`
- Status (2026-08-06): replacing the Stick item's visual (both
  `Assets/Prefabs/StickPickup.prefab` and the two pre-placed world
  pickups in `TestScene.unity`) — this one **is** actively being used,
  not just a comparison object. Attribution needs to land in the Credits
  tab before release.
