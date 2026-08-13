# Texture API tooling (3D AI Studio)

Scripted access to [3D AI Studio](https://www.3daistudio.com)'s texture-
generation API — a second, separate texture/model API alongside
`Tools/Tripo3D/`, added 2026-08-12 (Ben's key). **Turns out to genuinely be
Tripo's own texturing tech under the hood** — this platform exposes it as
a distinct endpoint (`/v1/3d-models/tripo/texture-model/`) alongside a
Tencent Hunyuan alternative (`/v1/3d-models/tencent/texture-edit/`, not
used by this script), rather than being an unrelated third vendor — Ben's
hunch when he shared the texturing doc link turned out right. Not yet used
on any real project asset — set up and ready to try, nothing generated
with it yet.

## Setup

1. Get an API key from your account dashboard at
   [3daistudio.com](https://www.3daistudio.com/Platform/API).
2. Copy `.env.example` to `.env` in this folder and paste the key in.
   `.env` is gitignored — it will never get committed.

## Usage

```powershell
./Generate-Texture.ps1 -ModelUrl "https://example.com/some-model.glb" -Prompt "charred wood grain, ring of grey rocks"
```

Downloads to `Output/<sanitized-prompt>.glb`. Pass `-OutputName` to pick
the filename yourself instead.

## Important: this retextures an EXISTING model, not text-to-texture

Same as Tripo3D's own `texture_model`, this endpoint needs a real,
**publicly reachable URL** to an existing model file (`model_url`) — it
can't generate a texture from a prompt alone, and this script doesn't
accept a local file path. The docs also mention an inline `model` file
upload field as an alternative to `model_url`, not used by this script.
If the model you want textured was built locally (Blender, a Tripo3D
download, etc.), you'll need to host it somewhere reachable first, or add
support for the inline-upload field. The docs didn't detail that upload
field's exact shape as of 2026-08-12 (unlike Tripo3D's own `import_model` +
STS-credentialed S3 upload for the equivalent case on the direct API — see
`Tools/Tripo3D/README.md`'s "Texturing a model we built ourselves"
section) — this is an open gap, not solved here yet.

## API reference (from docs.3daistudio.com, 2026-08-12)

- Base URL: `https://api.3daistudio.com`
- Auth: `Authorization: Bearer YOUR_API_KEY` header
- Rate limit: 3 requests/minute by default (429 if exceeded); custom
  limits available via the dashboard
- **Tripo texturing endpoint** (what this script uses):
  `POST /v1/3d-models/tripo/texture-model/` — body
  `{ "model_url": "...", "prompt": "..." }` (`model` file upload or
  `image_url` are alternatives to `model_url`/`prompt` respectively; also
  supports `style_image_url`/`style_image`, `texture_quality`
  ("standard"/"detailed"), `pbr`, `texture`, `texture_alignment`,
  `texture_seed`, `compress`, `bake`).
- Tencent Hunyuan alternative (not used by this script):
  `POST /v1/3d-models/tencent/texture-edit/` — body
  `{ "file_url": "...", "prompt": "..." }`.
- Submit response confirmed: `{ "task_id": "...", "created_at": "..." }`.
- Status polling (same endpoint for both backends):
  `GET /v1/generation-request/{task_id}/status/` — returns
  status/progress/a `results` array of download URLs; exact key inside
  each result item wasn't shown in the docs.
- Credit cost: Tripo texturing is 20 (standard) or 40 (detailed) credits,
  +10 for a style image; Tencent Hunyuan texturing is 80 credits.
- Credits are valid 365 days from purchase; check balance via the credit
  balance endpoint (not yet used by this script)

## Known gap: exact `results[]` item field name is unconfirmed

Everything else about the request/response shape is confirmed directly
from the docs (see above), but the exact key holding each result's
download URL inside the `results[]` array wasn't shown in any page
fetched while setting this up. `Generate-Texture.ps1` tries a few likely
candidates (`results[0].url`, `.download_url`, `.model_url`, then a couple
older fallback shapes) and always dumps the raw response to
`Output/last-response-debug.json` and `Output/last-poll-debug.json` so a
mismatch is fixable by inspecting the real shape, same convention
`Tools/Tripo3D/Generate-Model.ps1` already uses for the same reason (that
project's own docs turned out unreliable on exact field names too). **Not
yet verified against a real API call** — expect the first real run to
need a small field-name fix once the actual response shape is visible in
the debug JSON.
