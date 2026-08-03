# Gridless

First-person survival-crafting game (codename "The Flying T-Rex"), co-designed by
two people (traskmi + Ben) each working with their own Claude Code session against
this shared repo. Unity 6.3 LTS, dedicated-server multiplayer planned.

**Read `CHANGELOG.md` first** when picking up this repo cold — it's a why-focused,
skimmable history of what's been built and non-obvious gotchas hit along the way
(rendering pipeline traps, Editor onboarding fixes, tuning decisions). `git log` has
the full detail; the changelog is the fast path to context. Update it when you land a
meaningful change.

**Check `WORKING_ON.md` before starting a new feature.** Both collaborators' Claude
sessions work against this repo independently, and nothing else catches two sessions
solving the same problem in parallel — a version-bump collision or a fileID
collision self-heals at merge time, but building a whole duplicate system (see the
Waterskin/Canteen entry in `CHANGELOG.md`, 2026-08-02) wastes real design effort.
Add a line before starting non-trivial work; remove it once merged to `origin/main`.

## Design docs (`docs/`)

Read these directly rather than trusting a summary — they're actively evolving:
- `design-brief.md` — the systems/technical design brief (world scope, multiplayer
  architecture, Phase 1/2/3 MVP roadmap, magic, factions). The current build-order
  reference: work through the Phase 1 list before Phase 2.
- `game-overview.md` — narrative/setting pitch.
- `reconciliation-questions.md` — record of decisions made reconciling the above two
  when they first diverged.
- `skill-path-space.md` — endgame skill-tree spec.

## Project conventions

- **Unity version is pinned** — check `ProjectSettings/ProjectVersion.txt` before
  assuming a version; don't silently bump it.
- **New Input System only** (`activeInputHandler: 1`), no `.inputactions` asset yet —
  scripts read `Keyboard.current`/`Mouse.current` directly.
- **Single scene today**: `Assets/Scenes/TestScene.unity`, registered as scene 0 in
  `EditorBuildSettings` and auto-opened by `Assets/Editor/SceneAutoOpen.cs` when the
  Editor has none loaded (fixes a real empty-world bug on fresh clones — see
  changelog).
- **Scene/asset edits from a headless session**: these sessions can't drive the Unity
  Editor GUI. The established pattern is: write a throwaway `Assets/Editor/*.cs`
  script with a static method, run it via
  `Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod <Class.Method> -logFile <path>`,
  check the log for `CS####` compile errors, verify the result by grepping the saved
  `.unity`/`.asset` YAML for the expected fields, then **delete the throwaway
  script** (keep `Assets/Editor/` free of one-off setup code — `SceneAutoOpen.cs` is
  the one permanent exception).
- **Unity locks the project while its Editor is open** — batch mode will fail with
  "another Unity instance is running." Ask before assuming it's safe to run.
- Materials created at runtime (`new Material(Shader.Find(...))`) embed fine directly
  into a **scene** file but not reliably into a **prefab** — save as a real `.mat`
  asset via `AssetDatabase.CreateAsset` first, then reference it, or the prefab
  renders pink.
- **Bump the version number on every commit that touches gameplay code or scenes**
  (`Assets/Scripts/**`, `Assets/Scenes/**`, `Assets/Prefabs/**`). Two places must
  match, updated together in the same commit:
  - `GameVersion` in `Assets/Scripts/FirstPersonController.cs` (shown on-screen in
    the bottom-left debug panel)
  - the **"Current version"** line near the top of `CHANGELOG.md`

  Format: `MAJOR.MINOR.PATCH-dev` — increment PATCH for a normal commit; bump MINOR
  by hand for a completed Phase 1 milestone; MAJOR stays `0` until there's a real
  release. Doc-only commits (design docs, changelog, this file) don't need a bump.
  Both collaborators' Claude sessions should follow this — the in-game number and the
  changelog are meant to be cross-checkable at a glance, without digging into git.
