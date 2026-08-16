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

- 2026-08-16 — Ben+Claude — NPC Guarding built (`NPCVitals.cs`, `NPCGuarding.cs`, `GuardMeleeJob`/`GuardRangedJob`, `HostileCreature.RedirectAggro`, v0.3.106-dev, per `GUARDING_PLANNING.md`) — closes out MVP2 item 2 in full. Standalone-build logging also added (`[Skill]`/`[Fame]`/`[VillageFlag]`/`[NPCVitals]`/`[HostileCreature]` Debug.Log lines) so a long Compiled Game session can be reviewed via `Player.log` afterward. Then Rabbit built (`PreyWander.cs` — first real Prey Creature wander/flee AI, `Rabbit.prefab`, URP material fix, v0.3.107-dev, item 8) — Pig still open, Ben picked the LowPoly Pigs Pack but hasn't purchased it yet. Then Chicken Meat built (v0.3.108-dev) — new Blender-modeled drumstick, `PreyCreature.cs` gained a third loot slot, Chicken's scene instance wired. Found and fixed a real new gotcha along the way: glTFast's `AddRemap` material-extraction fix (documented in CLAUDE.md, used previously for Berry Seed) silently didn't apply here despite the `.meta`/`GetExternalObjectMap()` both looking correct — worked around by assigning materials directly on the wrapper prefab instead; logged a `BUGS_AND_ENHANCEMENTS.md` follow-up to check whether Berry Seed's original fix has the same silent problem. Then Pig built (v0.3.109-dev) from Ben's `Assets/Animal pack deluxe v2/` addition — closes out item 8's full 4-animal roster (Chicken/Pig/Deer/Rabbit all live). Same URP material fix as Rabbit, fresh `PigAnimator.controller` (the pack's own shipped controller has no Speed parameter), real `PreyWander` AI from the start, Raw Meat ×2-3 loot, 2 instances placed. No rescale needed — this pack ships at realistic meter scale already. Then traskmi did the first real combat live-test (hunted a Chicken with Bow+Arrow, then a Knife — confirms both frameworks actually work) and caught a real bug: the flying arrow visual faced backwards (fletching-first). Root-caused and fixed (v0.3.110-dev) — `FlyingArrow.Launch()`'s `LookRotation` was fighting the nested Arrow model's own baked equip-context rotation; fixed with a corrective 180° yaw, confirmed via a diagnostic render with color-coded direction markers. New CLAUDE.md gotcha documenting the pattern for future `LookRotation`-driven visuals. All throwaway diagnostic scripts deleted, compile verified clean throughout. Not yet committed — verified via compile + direct instantiated-material/pixel inspection + rendered visual checks, not yet live-tested in Play mode. Removing this entry once pushed to `origin/main`.
