# Working On

What's actively in progress right now, one line per active session. Check this
before starting new feature work — if something here overlaps what you're about
to build, coordinate before duplicating effort (see the Waterskin/Canteen
collision in `CHANGELOG.md`, 2026-08-02, for what happens when this doesn't get
checked).

Add a line when you start a non-trivial feature; remove it once merged to
`origin/main`. Stale entries are worse than none — if you're not sure whether an
entry is still active, ask before trusting it.

Note: "merged to origin/main" means the code is in — it doesn't require a live
Play-mode pass first. Manual test status for a shipped feature belongs in
`TEST_FEATURE_PLAN.md`, not here; don't keep an entry alive just to track that a
live test is still pending.

Format: `- YYYY-MM-DD — who — one-sentence description`

- 2026-08-16 — Ben+Claude — Village Flag's 5-tier recipe ladder (Stick+Cloth, Sewing-trained, deterministic by Stick tier) built and registered in `TestScene.unity`, per `VILLAGE_FLAG_PLANNING.md` section 2 — placeholder prefabs only, real Blender models still open. Merged to `origin/main` (v0.3.97-dev, folded into a parallel-session merge commit `a03dbed4`) — not yet live-tested in Play mode. Spawn loop and City Statue gate still unbuilt; leaving this entry active for whoever picks that up next.