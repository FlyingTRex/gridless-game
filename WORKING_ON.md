# Working On

What's actively in progress right now, one line per active session. Check this
before starting new feature work — if something here overlaps what you're about
to build, coordinate before duplicating effort (see the Waterskin/Canteen
collision in `CHANGELOG.md`, 2026-08-02, for what happens when this doesn't get
checked).

Add a line when you start a non-trivial feature; remove it once merged to
`origin/main`. Stale entries are worse than none — if you're not sure whether an
entry is still active, ask before trusting it.

Format: `- YYYY-MM-DD — who — one-sentence description`

- 2026-08-11 — Claude (traskmi's session) — Building a real Iron Ingot model
  (low-poly + metallic material, via headless Blender) to replace the
  placeholder Rock_Quaternius mesh currently used as Iron's world-pickup
  visual (`IronChunk.prefab`). Model exported to `Assets/Models/
  IronIngot.glb`; next step is wiring it into `IronChunk.prefab` and
  `Iron.asset` — touches shared prefab/asset files, flagging here first.
