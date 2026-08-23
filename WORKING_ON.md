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

Nothing in progress right now — cleared 2026-08-22 when the v0.3.161-dev commit
merged Multiplayer Phase 3 sub-phase 1 (Bootstrap): real Player prefab,
auto-host-on-load, NetworkIdentity/NetworkTransformReliable, all live-
confirmed. See `CHANGELOG.md`'s v0.3.161-dev entry and
`MULTIPLAYER_PLANNING.md` section 3 item 3 for the merged summary.
