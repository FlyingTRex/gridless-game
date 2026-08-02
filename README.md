# Gridless

A survival, world-building, and economic management game set on an artificially constructed replica of Earth.

## Repo Structure

Unity project (Editor: 6000.3.0f1 LTS, Universal Render Pipeline):

- `docs/` — Game design documents, story bible, system specs
- `Assets/` — All game content, opened directly by the Unity Editor
  - `Scripts/` — Game code
  - `Scenes/` — Unity scenes
  - `Prefabs/` — Reusable prefab objects
  - `Art/` — Models, materials, textures
  - `Audio/` — Music and sound effects
  - `Data/` — Config, balance data, item definitions (e.g. ScriptableObjects)
- `Packages/` — Unity Package Manager manifest
- `ProjectSettings/` — Unity project configuration (populated further on first
  Editor open)

Binary assets (art, audio, models) are tracked via Git LFS — see `.gitattributes`.
Everything Unity generates locally (`Library/`, `Temp/`, `Obj/`, `Build/`,
`UserSettings/`, `.vs/`, `*.sln`, `*.csproj`) is gitignored and will regenerate
automatically the first time the project is opened in the Editor.

## Getting Started

1. Install [Unity Hub](https://unity.com/download) and Unity Editor `6000.3.0f1`
   (or let Hub prompt you to install it when opening the project).
2. In Unity Hub, choose **Add project from disk** and select this repo's root
   folder (the one containing `Assets/`).
3. Open the project — Unity will resolve packages and generate the remaining
   `ProjectSettings`/`Library` files on first load.

See [`docs/game-overview.md`](docs/game-overview.md) for the narrative/setting pitch,
and [`docs/design-brief.md`](docs/design-brief.md) for the systems/technical design
brief (world scope, multiplayer architecture, city growth, settlement warfare,
phased MVP roadmap). The two were reconciled in
[`docs/reconciliation-questions.md`](docs/reconciliation-questions.md) — read that
first if the other two seem to disagree on something.

## Contributing

1. Clone the repo: `git clone <repo-url>`
2. Create a branch for your changes: `git checkout -b your-feature-name`
3. Commit your changes: `git commit -m "Describe your change"`
4. Push and open a pull request: `git push origin your-feature-name`
