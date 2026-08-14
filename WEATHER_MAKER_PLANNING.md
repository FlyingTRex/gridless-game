# Weather Maker Integration Planning

MVP2 item 5 (Sky and weather). Weather Maker (Digital Ruby, v8.1.0) is
already imported at `Assets/WeatherMaker/`. **Built and live-tested
2026-08-13** — all 11 build-order steps done (see section 7), full
day/night cycle watched start to finish by Ben (day → dusk → sunset →
night → moon), more thoroughly live-confirmed than almost anything else
shipped this session.

## 1. Why this, why now

MVP2 item 5's original write-up: "the procedural sky texture already
exists but has a known unresolved bug (the `Mathf.SmoothStep` vs. GLSL
`smoothstep` mismatch documented in `CLAUDE.md`, affecting cloud
coverage). Weather (temperature swings, rain) would be new." Rather than
hand-fixing that bug and hand-building weather from scratch, Weather Maker
replaces the whole system with a mature, maintained one — sky, clouds,
day/night, precipitation, fog, wind, lightning all in one asset. Also
directly ties into two other MVP2 items already flagged as related:
Constitution resisting cold/heat (item 1) and warm food/tea countering
cold (item 9) — "these three could become one coherent survival mini-
system instead of three unrelated features."

## 2. What's already compatible (confirmed, not assumed)

- **Unity 6000.3.21f1** and **URP 17.3.0** both meet Weather Maker's
  stated minimum (`Unity 6000+`, `URP 17.3+`) — checked directly against
  `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json`, not
  taken on faith from the readme.
- **`MainCamera` tag is already set** on the scene's Main Camera —
  Weather Maker requires this and it's already correct.

## 3. What this replaces (real, existing system)

- **`Assets/Data/Sky.mat`** is the scene's actual assigned skybox
  (`RenderSettings.m_SkyboxMaterial` in `TestScene.unity`, guid-matched
  directly against the asset). Backed by **`Assets/Textures/SkyTexture.png`**
  — the procedurally generated texture with the known `Mathf.SmoothStep`
  cloud-coverage bug documented in `CLAUDE.md`. Weather Maker's sky
  sphere fully replaces both; once integrated, this pair becomes dead
  weight worth deleting rather than leaving as silent cruft (see section
  7).
- **Camera clear flags are currently `Skybox`** — Weather Maker's own
  "Add Weather Maker to Scene" command changes this to `SolidColor`/clear
  automatically (confirmed by reading
  `WeatherMakerEditorCommands.AddWeatherMakerPrefab` directly), which is
  the expected/required change, not a side effect to work around.
- **Fog is currently off** (`m_Fog: 0` in `TestScene.unity`) and
  **ambient mode is Skybox-based** (`m_AmbientMode: 0`) — Weather Maker
  wants ambient source set to Color/Gradient with custom reflections per
  its own setup instructions, another real change, not additive.
- **No day/night cycle exists anywhere in this codebase today** —
  confirmed via grep (no sky/day-night scripts turned up outside
  `Assets/WeatherMaker/` itself). MVP2 item 5's "weather" wishlist item
  and Weather Maker's own day/night cycle manager are the same ask;
  nothing here conflicts with existing code, there's just nothing to
  reconcile.

## 4. Real gap found: no `AudioListener` anywhere in the project

Confirmed via grep — `TestScene.unity` has zero `AudioListener`
components. This isn't Weather-Maker-specific technical debt (this
project has no audio system at all yet, per `CLAUDE.md`'s Audio tab
placeholder), but Weather Maker specifically **uses the presence of an
enabled `AudioListener` to identify the local player** — without one,
weather zones, ambient sound, and dampening zones won't function
correctly. This needs adding to the Player object regardless of whether
real audio content ever gets authored:
- An `AudioListener` component.
- A kinematic `Rigidbody` on the same object.
- A tiny trigger `SphereCollider` (radius 0.001) on the same object.

Per the readme, these three go on the *same* GameObject as each other —
likely the Main Camera child (where an `AudioListener` conventionally
lives), not the player root. Use Weather Maker's own reference,
`WeatherMaker/Prefab/WeatherMakerPlayer.prefab`, to confirm exact
placement before wiring this onto the real `FirstPersonController`
Player object — **don't use "Add Weather Maker Player to Scene"**, that
command instantiates a brand new generic player prefab, which would
create an unwanted duplicate player object in a project that already has
a real one.

## 5. Batch-mode-scriptable entry points (a real finding, not assumed)

Checked `WeatherMakerEditorCommands.cs` directly rather than assuming
this all needs the Editor GUI. Three menu commands are plain `public
static void` methods with `[MenuItem]` attributes — callable via
`-executeMethod` same as any other batch-mode script in this project:

- `WeatherMakerEditorCommands.AddWeatherMakerPrefab` — instantiates
  `WeatherMakerPrefab` (or the 2D variant) into the active scene, and
  sets `Camera.main.clearFlags = SolidColor` / `backgroundColor = clear`.
- `WeatherMakerEditorCommands.EnableURP` — **the consequential one**.
  Sets `GraphicsSettings.defaultRenderPipeline` and
  `QualitySettings.renderPipeline` to Weather Maker's own bundled
  `WeatherMakerURPProfile` asset, project-wide. This **replaces** this
  project's current URP Render Pipeline Asset outright rather than
  merging into it — any existing tuned settings on the current pipeline
  asset (render scale, shadow distance, renderer features, etc.) would
  be lost unless first diffed/ported into a duplicate of
  `WeatherMakerURPProfile`, or merged the other direction. **This needs
  Ben's explicit sign-off before running** — it's exactly the kind of
  hard-to-reverse, project-wide setting change this session's own
  guidelines flag for confirmation first, not something to run blind
  even though it's technically scriptable.
- `WeatherMakerEditorCommands.SetupPostProcessing` — **not usable**,
  gated behind `UNITY_POST_PROCESSING_STACK_V2`, the legacy Post
  Processing Stack v2 package. This project would use URP's built-in
  Volume/Post Processing (part of URP itself), not that older package —
  this command doesn't apply here at all, skip it.

Also worth noting: `EditorUtility.DisplayDialog` calls inside these
methods are harmless in batch mode (Unity auto-dismisses modal dialogs
under `-batchmode`, doesn't hang) — not a blocker, just noted so it isn't
mistaken for one if a batch run looks like it stalls.

## 6. Design questions — resolved (2026-08-13)

1. **Render pipeline asset: replace outright.** Run `EnableURP` as-is;
   re-tune any settings that look off afterward rather than pre-merging.
2. **Feature scope: sky + weather only.** Sky sphere, day/night cycle,
   clouds, and precipitation (rain/snow) — matches MVP2 item 5's
   original ask exactly. Meteor showers, aurora borealis, full
   lightning/thunder, the water shader, and snow/wetness overlays are
   all explicitly deferred, not enabled by default just because they
   exist in the package.
3. **Art direction: start from a mid-tier profile, tune from there.**
   Begin with the `Good` performance profile (not `Fantastic`/`4K`, not
   `Simple`/`Fastest`) and adjust cloud/sky settings toward something
   that reads well against this project's existing low-poly art, rather
   than assuming realism is right or wrong up front.
4. **Old sky system: delete once confirmed working.** `Sky.mat`/
   `SkyTexture.png` get deleted in the same pass, once Weather Maker's
   sky is visually confirmed rendering correctly — not left as unused
   cruft, and not deleted pre-emptively before the replacement is proven.
5. **Gameplay bridge: in scope for this pass, not deferred.** Build the
   bridge from Weather Maker's live weather state to
   `PlayerVitals.bodyTemperature` now — rain/snow cools the player the
   same way a lit Campfire already warms them via `WarmNear`. Without
   this, the whole integration is purely cosmetic and doesn't actually
   deliver on item 5's real design intent (the Constitution/warm-food
   tie-in). This is genuinely new code regardless of which visual
   profiles get picked — nothing bridges Weather Maker to `PlayerVitals`
   today.

## 7. Build order — ✅ all 11 steps done (2026-08-13)

1. ✅ Confirmed Editor closed before every batch-mode run, same standing
   rule as every other script in this project.
2. ✅ Ran `WeatherMakerEditorCommands.EnableURP` — `GraphicsSettings
   .defaultRenderPipeline` confirmed pointing at `WeatherMakerURPProfile`
   via direct YAML check, not just trusting the log.
3. ✅ Ran `WeatherMakerEditorCommands.AddWeatherMakerPrefab` via a small
   wrapper script (bare `-executeMethod` doesn't reliably open/save the
   right scene in batch mode — same lesson learned in the save/load and
   skill-books builds) — `WeatherMakerPrefab` confirmed in
   `TestScene.unity`, camera clear flags flipped to `SolidColor`/clear.
4. ✅ Player setup — `AudioListener` + kinematic `Rigidbody` + tiny
   trigger `SphereCollider` (radius 0.001) added directly to the Main
   Camera GameObject.
5. ✅ Lighting settings — skybox → `WeatherMakerSkyBoxMaterial`, sun →
   Weather Maker's `Sun` light (found via recursive child search, not
   guessed), ambient mode → Gradient (`AmbientMode.Trilight`),
   reflections → Custom. **Color space switched Gamma → Linear**, a
   genuinely project-wide change (confirmed with Ben first, separately
   from the render-pipeline sign-off) — URP is designed/tested for
   Linear, so this arguably fixes a pre-existing mismatch rather than
   introducing a new one.
6. ✅ **Scope to sky + weather only — turned out to need nothing.**
   Across a full live-watched day/night cycle, nothing extra (rain,
   lightning, water, meteor showers, aurora) ever appeared unprompted —
   the prefab's own defaults already matched the intended scope, no
   deactivation pass was actually necessary.
7. ✅ Performance profile duplicated (`Good` →
   `Assets/Data/WeatherMakerPerformanceProfile_Gridless.asset`) and
   assigned; Sky/Cloud/Day-Night profiles assigned directly from the
   vendor's own bundled set (`_Procedural`, `_LightMediumScattered`,
   `_Default`) as a reasonable starting point, not yet further tuned.
8. ✅ **Old sky system deleted** (`Assets/Data/Sky.mat`,
   `Assets/Textures/SkyTexture.png`) — confirmed zero remaining
   references first (`RenderSettings.skybox` had already moved to
   Weather Maker's material), then removed via `git rm`. Resolves the
   `Mathf.SmoothStep` gotcha in `CLAUDE.md` by retiring the system it
   was found in.
9. ✅ Batch-mode compile checks — 10+ rounds across this whole build (see
   section 8 for the 3 real compile-blocking issues found and fixed
   along the way), all clean by the end.
10. ✅ **Live visual pass — the real one.** Ben watched a complete
    day/night cycle live in the Editor: clear day sky with clouds → dusk
    (purple gradient) → a genuinely striking orange/red sunset → full
    night with a textured, cloud-occluded moon. No shader corruption, no
    HUD/UI regressions, Console clean. This is the actual proof step —
    everything before it was structural (compile + YAML) verification
    only.
11. ✅ **Gameplay bridge built**: new `PlayerWeatherEffects.cs`, wired
    onto the Player object. Reads
    `WeatherMakerPrecipitationManagerScript.Instance`'s live
    Rain/Sleet/Snow/Hail intensities every frame and calls
    `PlayerVitals.WarmNear` with a colder target scaled by whichever
    precipitation type is currently strongest — reuses `WarmNear`
    directly rather than adding a separate "cool" method, since its
    `MoveTowards`-based implementation is already symmetric. No shelter/
    indoor detection yet (a future refinement, not v1 scope). First-pass
    cold-target/rate numbers, same "tune by playtesting" status as every
    other balance value in this project.

## 8. Real problems hit and fixed live (not in the original plan)

- **Two missing built-in Unity modules** caused real compile errors the
  moment `EnableURP` tried to recompile: `com.unity.modules.wind`
  (`WeatherMakerWindScript.cs` uses `WindZone`) and
  `com.unity.modules.screencapture` (`WeatherMakerScreenshotScript.cs`
  uses `ScreenCapture`). Both added to `Packages/manifest.json` — simple,
  zero-risk additive fixes, same class as any other built-in module gap.
- **A real Mirror API version mismatch**: `WeatherMakerMirrorNetworkScript
  .cs` (guarded by `#if MIRROR`, which is genuinely defined in this
  project) called `NetworkConnection.connectionId`, which doesn't exist
  on the Mirror version actually installed here — Weather Maker's
  optional network-sync script targets an older Mirror API. Patched to
  `GetHashCode()` as an explicitly-commented stand-in, since this whole
  method is unreachable in the current single-player build anyway (out
  of scope per decision 2) — real multiplayer weather sync is deferred
  to whenever `MULTIPLAYER_PLANNING.md`'s actual conversion happens, not
  fixed properly here.
- **The shipped `WeatherMakerDayNightCycleProfile_Default.asset` had
  `Speed`/`NightSpeed` both at `0`** — completely frozen, not the
  script's own class default of `10`. Found live when Ben asked "how
  long should it take before the night cycle comes thru" and the honest
  answer, read directly from the asset via a throwaway batch script
  (grep can't parse Weather Maker's binary-serialized assets), was
  "never, at this setting." Duplicated to
  `Assets/Data/WeatherMakerDayNightCycleProfile_Gridless.asset` with
  `Speed = NightSpeed = 480` (a full 24h in-game day in ~3 real minutes,
  Ben's explicit call — "much faster, for testing") and reassigned the
  day/night cycle manager to it.

## Cross-references

- `MVP2_PLANNING.md` item 5 — this doc is that item's real implementation
  plan, same relationship `SAVE_LOAD_PLANNING.md`/`SKILL_BOOKS_PLANNING.md`
  have to items 6/7.
- `CLAUDE.md`'s `Mathf.SmoothStep` gotcha — the bug this integration
  makes moot by replacing the system it was found in, not by fixing it
  in place.
