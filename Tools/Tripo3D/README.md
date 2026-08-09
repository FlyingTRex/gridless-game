# Tripo3D API tooling

Scripted access to Tripo3D's text-to-3D generation API — separate from
their DCC Bridge plugin (that's an interactive, browser-to-Editor
workflow you drive by hand; this is the headless/scriptable path). Models
land in `Output/` for manual review — nothing here gets imported into
`Assets/` automatically, that's always a deliberate follow-up step (see
"Current status" below for what's actually been brought into the project
so far).

We're still evaluating whether this pipeline is worth using regularly —
nothing's committed to yet beyond the one test model currently sitting in
`TestScene.unity`.

## Setup

1. Get an API key from your account dashboard at
   [platform.tripo3d.ai](https://platform.tripo3d.ai).
2. Copy `.env.example` to `.env` in this folder and paste the key in.
   `.env` is gitignored — it will never get committed.

## Usage

```powershell
./Generate-Model.ps1 -Prompt "a mossy stone pickaxe, low-poly"
```

Downloads to `Output/<sanitized-prompt>.glb` (plus a `.png` preview if
Tripo3D returns one). Pass `-OutputName` to pick the filename yourself
instead. Generation typically takes 10–120 seconds (observed in practice sitting at
99% for a while before flipping to `success`); the script polls every 2
seconds up to `-TimeoutSeconds` (default 300).

Model download URLs expire 5 minutes after the task succeeds, so the
script downloads immediately — don't expect to reuse a printed URL later.

## Known rough edge

`-Model` defaults to a specific Tripo3D model-version string pulled from
their docs at setup time (2026-08-06) — if generation starts failing with
a model-version error, check [developers.tripo3d.ai](https://developers.tripo3d.ai)
for the current recommended value and pass `-Model` explicitly (or update
the default in the script).

## Output is not reviewed automatically

Everything in `Output/` is raw, unreviewed AI-generated content. Nothing
here gets imported into `Assets/` automatically — that's a deliberate
manual step once a model's actually been looked at.

## Two separate credit balances — don't confuse them

Tripo3D has **two independent balances that don't share credit**:

- The **API balance** (what `Generate-Model.ps1` spends, and what
  `GET /v3/account/balance` reports — see below). This is the one tied to
  the key in `.env`.
- The **Tripo Studio web tool balance** (spent when generating/converting
  models by hand at [studio.tripo3d.ai](https://studio.tripo3d.ai)).
  Possibly sourced from free/trial credit rather than a purchase — worth
  checking on your own account before assuming either way.

Check the API balance from the command line:

```bash
API_KEY=$(grep '^TRIPO3D_API_KEY=' .env | cut -d= -f2-)
curl -s -X GET https://openapi.tripo3d.ai/v3/account/balance -H "Authorization: Bearer $API_KEY"
```

**Cost data points so far** (not a price guarantee, just what we actually
saw): a single API `text-to-model` call for a simple prop (the berry bush
below) cost **20 credits** and produced a finished `.glb` directly. The
equivalent by hand through Tripo Studio's web UI was a two-step process —
a batch of 4 concept images (**40 credits**) via what the UI labeled
"Nano Banana" (likely Google's image-gen model), then a separate,
not-yet-done conversion of one chosen image into an actual 3D mesh
(**55 credits** quoted in the UI). If those two balances were the same
pool, the API route would be far cheaper per finished model — but since
they're separate (see above), that comparison isn't actually valid across
balances, only within one.

## Technical gotchas hit so far

- **PowerShell 5.1 chokes on non-ASCII characters in `.ps1` files.**
  Files get written as UTF-8 without a BOM, and Windows PowerShell 5.1
  doesn't reliably detect that — an em-dash or smart quote in a comment
  produced a "missing terminator" parse error that had nothing to do with
  the actual line it pointed at. Keep `.ps1` files plain ASCII (`-`
  instead of `—`, straight quotes only).
- **`.glb` import needs a package Unity doesn't ship with.** Added
  `com.unity.cloud.gltfast` (`6.2.0`) to `Packages/manifest.json` — this
  project had never needed a model importer before (everything else is
  procedural), so this was a first.
- **`output.model_url` can appear before `status` flips to `"success"`.**
  One generation sat at `"status": "running", "progress": 99` for a while
  with a fully-populated, working `output.model_url` already present.
  `Generate-Model.ps1` still waits for the official `"success"` status
  before downloading (the safer default), but if a run looks stuck near
  100%, the model may already be fetchable directly from that URL — see
  the script's polling loop for how to hit `GET /v3/tasks/{task_id}`
  directly.
- **Generation can take longer than Tripo3D's own "10-120 seconds"
  guidance.** The script's default timeout is padded to account for this
  (see `Generate-Model.ps1`'s current default).
- **Imported models are one fused mesh + one material, not editable
  parts.** Checked directly on the berry bush: a single mesh (753,364
  vertices, one submesh) and a single material for the whole object —
  no separate "berries" vs. "leaves" to recolor or reshape independently
  in Unity. Also worth noting: 753K vertices is a lot for something
  prompted as "low-poly" — that descriptor isn't a hard guarantee.
  Practical implication: if you want e.g. blue berries instead of red,
  the realistic path is regenerating with an explicit prompt ("...with
  blue berries..."), not editing the existing model — real part-level
  edits would need actual 3D modeling software (Blender etc.), which
  isn't part of this pipeline.
- **Tripo Studio (the web UI) gates model export behind a subscription
  — the API doesn't.** Converting the tree concept image to a 3D model
  cost 55 web-tool credits and produced a real, viewable result, but
  actually downloading the `.glb` requires subscribing — the credits
  already spent don't include export rights. This is a real point in the
  API route's favor: every API-generated model so far (the berry bush)
  came with a directly downloadable URL, no subscription needed, just
  the per-call credit cost. Practical takeaway: prefer the API for
  anything you actually need the file for; treat the web UI as
  concept-image/preview-only unless a subscription is worth it to you.
  Confirmed pricing for the "Pro" tier (seen directly in the Tripo Studio
  upgrade screen, 2026-08-06): **3,000 credits/month, ~200 models
  (~$0.07/model), $13.93/month billed annually ($167.16/year, shown with
  a 30%-off badge — unclear if that's a standing or temporary discount)**.
  Credits are shared across both prompting/generation and export, not a
  separate export-only allowance. Also includes 10 concurrent tasks, high
  queue priority, batch generation, 7-day history, more free retries, and
  unlimited storage.
  **Commercial-use licensing — resolved, but worth a final direct check.**
  That "Private Models · Commercial Use" line initially raised the
  question of whether the pay-as-you-go/API tier (what's actually been
  used so far) grants commercial rights at all. Per Tripo3D's own blog
  content: their **free tier** (300 credits/month, no payment) is
  CC BY 4.0/attribution-required and explicitly **not for commercial
  use**; the **API** (pay-as-you-go, what `Generate-Model.ps1` uses)
  **includes commercial use for all generated assets**, license tied to
  the API account; **Pro/paid subscription** tiers also grant full
  commercial rights, no attribution required. So the route already
  validated here (API + real purchased credits) should already be fine
  to ship in the actual game — the Pro subscription isn't required for
  this specific concern. This is from search results summarizing their
  blog, not a direct read of the actual Terms of Service — worth a final
  direct check there before fully relying on it for a real release.
- **Lesson learned (2026-08-09) — the API has a genuine two-step
  text-to-image-then-image-to-model path, confirmed via
  [docs.tripo3d.ai](https://docs.tripo3d.ai/), not yet used by
  `Generate-Model.ps1`.** Came up chasing the "can we reuse/reshape part
  of an existing model" question (see the design-brief's Foundation/Wall
  reuse ideation) — the real answer turned out to be two related but
  separate tools, neither built yet:
  - **`text_to_image`** — a real, separate request type (5 credits,
    same task_id/poll shape as `text_to_model`) that generates a 2D
    concept image from a prompt, explicitly documented as "ideal inputs
    for downstream 3D modeling." Much cheaper than a full model
    generation (20 credits) — worth generating a few cheap variations
    to check composition/style before committing to the pricier 3D
    conversion, same idea Tripo Studio's web UI already does by hand
    (see the Tree/Backpack entries below), just confirmed scriptable
    too now.
  - **`image_to_model`** — converts an existing image into a 3D model;
    the image goes in via a file-upload token, not a raw URL, so this
    needs an upload step first. Not yet tried.
  - **Separately: Blender has a full Python API (`bpy`)**, scriptable
    headless (`blender --background --python script.py`), which could
    do genuine part-level mesh editing (separate the twig platform from
    its posts, reshape a piece into a different one) that this pipeline
    fundamentally can't do today (see "one fused mesh" above). **Not
    installed on this machine as of 2026-08-09** — Ben's installing it;
    revisit the "extract/reshape model parts" idea once it's available,
    including whether Tripo3D's meshes actually have clean-enough seams
    to separate this way at all (unconfirmed either way).
  - **Update, same day, once installed** (`C:\Program Files\Blender
    Foundation\Blender 5.2\blender.exe`, 5.2.0 LTS, Python 3.13.13):
    tested both the "separate existing parts" question and, separately,
    whether Blender can build a model *from scratch* (no Tripo3D input
    at all).
    - **Part-separation groundwork**: imported `Assets/Models/TwigFoundation.glb`
      (793,896 verts, 1 Unity mesh object) and ran a `bmesh` flood-fill
      connectivity pass. It's actually **2,118 separate disconnected
      geometric islands** internally — individual twig/branch/rope
      pieces positioned together, fused into one mesh only at export/
      import time, not one continuous connected surface. So "separate
      the posts from the platform" is a *spatial classification*
      problem (group islands by position) rather than needing a clean
      cut along a seam — more promising than either a flat yes or no,
      but not yet proven to produce a clean, usable split when actually
      attempted. Left on hold at Ben's direction in favor of the
      from-scratch test below; revisit if/when part-reuse comes back up.
    - **From-scratch modeling: confirmed, works well.** Built the 5
      Trimmed Stick craft-tier models (Crude through Masterwork) as a
      real test case — a procedurally-varied *family* of related models
      (angularity/smoothness/carving scaled by tier) that Tripo3D
      couldn't produce coherently across 5 independent generations
      anyway. `bpy.ops.mesh.primitive_cone_add` turned out to only have
      two vertex rings (base + tip, no length-wise resolution) — ended
      up building the shaft directly via `bmesh` (rings bridged with
      quads) for real control. See `CHANGELOG.md` v0.1.173-dev for the
      full build and the bugs hit along the way. Bottom line: Blender is
      a genuine third option alongside Tripo3D generation and manual
      editing — good fit for anything proceduralizable (tiered
      variants, primitive-based props), not a replacement for organic/
      complex shapes Tripo3D is better suited to.

- **Texturing a model we built ourselves (not a Tripo3D generation) —
  confirmed working, same day.** `texture_model` needs an
  `original_model_task_id` — i.e. the model must already exist as a
  Tripo3D task. The path in: `import_model` (uploads an external file,
  registers it as a task, free/0 credits) → `texture_model` on that
  task ID with a text prompt (20 credits at detailed quality — same
  price as a full from-scratch generation). New script:
  `Tools/Tripo3D/Texture-Model.ps1`.
  - This uses a **different API surface** than `Generate-Model.ps1`.
    That script talks to the path-based `v3` REST API
    (`openapi.tripo3d.ai/v3/generation/...`), which has no documented
    texture-an-existing-mesh endpoint. `import_model`/`texture_model`
    only exist on the older task-based `v2` API
    (`api.tripo3d.ai/v2/openapi`, `POST /task` with a `"type"` field) —
    confirmed against Tripo3D's own official Python SDK source
    (`github.com/VAST-AI-Research/tripo-python-sdk`), since
    `platform.tripo3d.ai/docs` is a JS-rendered SPA a simple fetch can't
    read. Same API key/Bearer auth works on both.
  - **File upload is a two-tier system, and the obvious endpoint is a
    trap.** `POST /upload` looks like the generic upload endpoint but
    is image-only — a real `.glb` gets a clean `"This image file type
    is not supported"` rejection. The actual path for model files is
    STS-credentialed S3 upload: `POST /upload/sts/token` returns
    temporary AWS credentials (access key/secret/session token) good
    for one object, which then needs a real SigV4-signed S3 PUT.
    Installed the `AWS.Tools.S3` PowerShell module
    (`Install-Module -Name AWS.Tools.S3 -Scope CurrentUser -Force`,
    plus the NuGet provider it depends on) rather than hand-rolling AWS
    signing — `Write-S3Object` with the STS credentials handles it in
    one call. The STS response's `s3_host` is a real AWS host
    (`s3.us-west-2.amazonaws.com` observed) with a real region, not a
    custom S3-compatible endpoint — get the region wrong and S3 replies
    with exactly which region it wanted.
  - **Response field names for `texture_model`'s output are `output.model`
    and `output.rendered_image`** (not `_url`-suffixed like `v3`'s
    `output.model_url` — different API surface, different schema).
  - **Cost finding:** texturing an existing model costs the same as
    generating one from scratch (both 20 credits at default settings,
    confirmed from two real task logs). Building geometry in Blender
    first isn't a cost optimization — the payoff is controlled,
    consistent geometry (e.g. a coherent 5-tier family) combined with
    Tripo3D's real texture quality, for the same price either path
    would've cost alone.
  - Check current balance any time via `GET /user/balance` on the `v2`
    host — returns `{"balance": N, "frozen": N}`.
  - Tested on `TrimmedStickMasterwork.glb` (see CHANGELOG v0.1.174-dev)
    — real wood grain, warm tones, genuinely better than the flat-color
    material the other 4 tiers still use. Not yet applied beyond that
    one tier.

## Current status (2026-08-06, latest entry 2026-08-09)

- **Twig Foundation (2026-08-09) — hit the "stuck at 99%" timeout
  pattern, actually succeeded server-side, recovered by polling
  directly.** Prompt: `"a crude foundation platform made of bundled
  sticks and branches lashed together with rope, flat square panel,
  primitive twig construction, isolated on a plain background, no
  person, no model, low-poly game asset"`. `Generate-Model.ps1` gave up
  after sitting at `progress: 99` past its own timeout — same failure
  mode documented below for the Grass Belt/Crude Stone Knife — but a
  direct `GET /v3/tasks/{id}` a few minutes later showed
  `"status": "success"`. Downloaded the freshly-signed `model_url`/
  `rendered_image_url` by hand via `curl` rather than re-generating (no
  credits wasted). Result: a genuine lashed-twig-and-rope platform on
  short legs, exactly matching the prompt. Imported as
  `Assets/Models/TwigFoundation.glb` and swapped into
  `Foundation.prefab`'s `Slab` child only — the root `BoxCollider` and
  all 4 `BuildSocket`s stayed untouched, so gameplay
  footprint/snapping/upgrades needed zero changes, only the visual.
- **Anvil (2026-08-09) — clean on the first attempt, imported but not
  wired to gameplay yet.** Prompt: `"a blacksmith's anvil, solid iron
  block on a sturdy wooden stump base, worn dark metal with a pointed
  horn, isolated on a plain background, no person, no model, low-poly
  game asset"`. No 500s, no timeout — reads clearly as a classic anvil
  on a wooden stump. Imported as `Assets/Models/Anvil.glb` (1 renderer,
  1 mesh, 724,999 vertices — same "low-poly" wording doesn't guarantee
  a low vertex count pattern seen on every generation so far; bounds
  0.62 x 0.76 x 1.00). Ben's call: import only, no scene placement, no
  prefab, no recipe — there's no Forging/Metalworking mechanic built
  yet (the design-brief's "hammer + anvil + wood fuel + steel → sword"
  is still aspirational vision text, not a concrete system), so this is
  parked for whenever that system actually gets designed.
- **Berry bush** — generated via the API (`a small berry bush, low-poly
  game asset`), imported into `Assets/Models/GeneratedBerryBush.glb`,
  placed in `TestScene.unity` at `(2, 0, 2)` as "Generated Berry Bush
  (Tripo3D test)" for visual review. Reviewed at both close range and
  from a normal distance — reads clearly as a bush with distinguishable
  berries and leaves, sits correctly on the ground at a reasonable scale
  next to other world objects. **Kept** — no decision yet on whether it
  replaces the existing procedural Berry Bush or just sits alongside it
  for further comparison.
- **Tree** — a prompt was written specifically to target the procedural
  tree's known problems (see `BUGS_AND_ENHANCEMENTS.md`'s tree entry —
  pole-like trunk, floating "grape cluster" foliage, washed-out bark):

  ```
  A small game-ready tree, tapered trunk widening at the base, three to
  four branches spreading outward and upward at varied angles, one full
  rounded canopy of overlapping leaves (not separate leaf clusters), rich
  brown bark, saturated green foliage, low-poly stylized game asset,
  centered on a plain background
  ```

  Run by hand through Tripo Studio (not the API) — produced 4 concept
  images, all a clear visual improvement over the procedural tree's
  documented shape problems (real trunk taper, connected branches, one
  cohesive canopy). One was converted to an actual 3D model (55 web-tool
  credits) and looks just as strong in the 3D preview as the concept
  images promised — but **couldn't be exported/downloaded without a
  Tripo Studio subscription** (see the gotcha above). Not yet brought
  into Unity as a result — next step is most likely just regenerating
  the same prompt through the API script instead, which doesn't have
  this restriction.
- **Meshy AI ([meshy.ai](https://www.meshy.ai)) — considered as an
  alternative to Tripo3D, evaluation inconclusive.** Their marketing/docs
  advertise a free tier (100 credits/month, no card required) and a
  useful-looking generation panel with an explicit **poly count slider**
  (a real advantage over Tripo3D — Tripo's "low-poly" prompt wording
  produced a 753K-vertex mesh anyway, see the berry bush gotcha above;
  Meshy lets you set a hard target instead of hoping the prompt is
  respected). In practice, hands-on testing didn't match the docs: the
  account's Quick Generate panel defaulted to image-to-3D only (no
  visible text-prompt input), the site became unresponsive mid-session,
  and after a refresh it prompted to subscribe rather than allowing a
  free text-to-3D generation at all. Same lesson as Tripo Studio's export
  wall: **what a service's docs/marketing claim about a free tier and
  what actually happens in a live account can diverge** — verify hands-on
  before relying on either. Not pursued further for now; Meshy's own API
  is also confirmed pay-before-you-go (a separate purchased credit pool,
  same two-balance pattern as Tripo3D) if it's worth revisiting later via
  that route instead of the web app.
- **Backpack — first real gameplay integration, not just comparison
  (v0.1.74-dev).** Prompted for a "photorealistic small crude leather
  backpack" and generated the *same prompt text* through both paths to
  compare them directly:
  - **Tripo Studio web UI**, single concept image first (10 credits) to
    check composition before committing to a 3D model — first attempt
    put a person wearing the backpack (not asked for), fixed by adding
    explicit negative constraints ("no person, no model, no human,"
    "product photography... alone," "isolated on a plain white
    background"). Resulting image and 3D model: no metal hardware,
    rope-laced closure, deliberately crude look.
  - **API** (`generation/text-to-model`, same prompt text): produced a
    visibly different result — has a metal buckle and snap studs, reads
    more polished/less "crude" than the web UI version.

  **Same prompt, two different pipelines, two different results** — a
  concrete answer to "is the API a viable substitute for the web UI,"
  and the answer is no, not for identical output; treat them as
  different tools that happen to share a prompt box. Used the **API**
  version (`Assets/Models/CrudeLeatherBackpack.glb`) to replace the
  existing 5-scaled-cube placeholder backpack model in both
  `Assets/Prefabs/Backpack.prefab` and the standalone "Backpack"
  GameObject in `TestScene.unity`. Commercial use is covered under the
  pay-as-you-go API account per Tripo3D's licensing (see above) — no
  attribution requirement, unlike the CC-BY third-party models tracked
  in `Assets/Models/THIRD_PARTY_CREDITS.md`.
- **Open question, mostly resolved for one-off asset swaps, still open
  for regular production use:** the Backpack swap shows the API path
  can go from prompt to a real in-game asset in one sitting, with usable
  commercial licensing and no export paywall (unlike Tripo Studio).
  Still nothing wired into any build/import automation — every step so
  far has been a deliberate, manual one-at-a-time call — and it's still
  unproven whether prompt-to-result consistency is good enough to lean
  on for a large batch of assets rather than one-off hero pieces.
- **Crude Stone Knife (v0.1.115-dev) — 4 failed attempts, then 2 real
  ones.** The first 4 `generation/text-to-model` calls (2026-08-07, for
  the prompt `"a crude knapped stone knife blade, no handle, low-poly"`
  and a trivial `"a rock"` test) all failed instantly with a generic
  `500`/"Unknown error on server side" — confirmed not a client-side
  bug (request body matched Tripo3D's own current docs exactly) and
  concluded to be a systemic outage on their end; no fix possible from
  our side, request IDs logged for potential support contact. Retried
  hours later with a photorealistic prompt — the API actually accepted
  the task this time (no more 500s) but hit Tripo3D's own **20-minute
  server-side processing timeout** (`error_code: 2018`) right at the
  finish line; balance wasn't charged for it, and the 2D concept
  preview it did produce along the way looked far more ornate/engraved
  than "crude" called for. Retried once more with a simplified prompt
  (`"a photorealistic crude knapped flint knife blade, no handle,
  plain and unadorned, sharp chipped edge, rough grey stone texture"`)
  — succeeded cleanly this time, genuinely faster (99% within ~2
  minutes vs. the earlier attempt's 24% after 5), and read as
  convincingly crude/knapped rather than a fantasy artifact. **Known
  limitation, accepted as-is:** despite "no handle" in every attempt's
  prompt, the model always comes back with a full handle/crossguard —
  Tripo3D appears to default "knife" toward a hilted shape regardless
  of that instruction. Imported as `Assets/Models/CrudeStoneKnife.glb`
  (43MB — by far the largest model in the project; same "actual output
  is much higher-poly than prompted" pattern as the berry bush) and
  swapped in for the old placeholder Capsule primitive in
  `Assets/Prefabs/RockKnifePickup.prefab` (the Crude Knife's world
  pickup, referenced by `CrudeKnife.asset`), sized to match the old
  placeholder's footprint (`0.08 x 0.05 x 0.35`).
- **Stone hammer (v0.1.143-dev) — clean on the first attempt.** Prompt:
  `"a crude stone hammer with a wooden handle, primitive tool, rough
  grey stone head bound to the handle with cord, isolated on a plain
  background, no person, no model, low-poly game asset"`. No 500s, no
  timeout, no unwanted extra geometry — reads exactly as intended, a
  stone head lashed to a wood handle. Used to fill the Hammer CraftTier
  ladder (same 5-tiers-share-one-model pattern as Pickaxe/Axe before
  it, those two sourced from hand-downloaded Poly Pizza models
  instead). Worth noting as a small pattern so far: prompts explicitly
  describing the *binding/construction detail* ("bound... with cord,"
  "with a screw cap," "with shoulder straps") seem to correlate with
  cleaner results than a bare object name — not proven, just an
  observation across enough generations now to mention.
- **Grass backpack (v0.1.133-dev) — clean, strong result, same
  server-timeout pattern, and a slow-download gotcha.** Prompt: `"a
  small woven grass backpack, plant fiber cordage bag with shoulder
  straps, isolated on a plain background, no person, no model,
  low-poly game asset"`. Hit the same client-side "did not succeed"
  timeout the Grass Belt/Knife hit — task actually succeeded a bit
  later, caught by polling `GET /v3/tasks/{id}` directly. Unlike the
  Grass Belt (came back as a closed ring instead of an open strap),
  this one nailed a proper backpack silhouette on the first attempt —
  woven basket body, leather straps, buckle closure. **New gotcha**:
  the 42MB download itself got killed twice by the shell tool's own
  timeout mid-transfer (large file, slow connection) — recovered with
  `curl -C -` (resume) against a freshly-repolled URL. Confirmed a
  `GET /v3/tasks/{id}` call returns a **new** signed `model_url` each
  time, even long after the task succeeded — don't assume the first
  URL you got is the only one available if it's slow or the transfer
  gets interrupted.
- **Grass belt (v0.1.122-dev) — hit the 20-minute server-side timeout,
  then quietly succeeded anyway.** Prompt: `"a green woven grass belt,
  plant fiber cordage wrapped in a coil, isolated on a plain
  background, no person, no model, low-poly game asset"`. The client
  script's own poll loop gave up after sitting at `progress: 99` past
  its timeout (same failure mode as the Crude Stone Knife's first real
  attempt), but polling `GET /v3/tasks/{task_id}` directly a few
  minutes later showed `"status": "success"` — the task had actually
  finished server-side, the client just wasn't watching anymore.
  Downloaded the `model_url` immediately (5-minute expiry after
  success). Came back as a closed woven ring/wreath rather than an open
  strap with overlapping ends — accepted as-is (Ben's call) rather than
  spending another 20 credits chasing an exact strap shape. Imported as
  `Assets/Models/GrassBelt.glb`, replacing the flat grey Cube
  placeholder on `Assets/Prefabs/CrudeFiberBelt.prefab`. **Practical
  takeaway:** a client-side "did not succeed" error from
  `Generate-Model.ps1` isn't necessarily a real failure — worth a
  direct `GET /v3/tasks/{id}` check before assuming credits were wasted
  and retrying from scratch.
- **Rope coil (v0.1.116-dev) — clean on the first attempt.** Prompt:
  `"a photorealistic small coil of rope, hemp fiber texture, tightly
  wound, isolated on a plain background"`. No 500s, no timeout, no
  unwanted extra geometry like the knife's handle — just a tidy
  bundled coil, matching the prompt directly. 20 credits. Imported as
  `Assets/Models/RopeCoil.glb`. `Rope.asset` never had a
  `worldPickupPrefab` at all (no old placeholder to replace), so this
  needed a brand-new `Assets/Prefabs/RopeCoilPickup.prefab` built from
  scratch rather than a swap — same `Pickup`/`Rigidbody`/`BoxCollider`
  shape as `StickPickup.prefab`, model uniformly scaled to a 0.28
  max-dimension target (no old footprint to match, so this was picked
  to match the size range of other small hand-carried pickups).
