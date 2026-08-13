<#
.SYNOPSIS
    Retexture an existing 3D model via the 3D AI Studio API and download
    the resulting textured .glb.

.DESCRIPTION
    Talks to 3D AI Studio's REST API (https://api.3daistudio.com) headlessly
    from a script, same shape as Tools/Tripo3D/Generate-Model.ps1. Requires
    an API key in Tools/TextureAPI/.env (copy .env.example and fill it in)
    - see that file for where to get one.

    Uses this platform's Tripo-backed texturing endpoint specifically
    (POST /v1/3d-models/tripo/texture-model/) rather than its Tencent
    Hunyuan alternative - confirmed via the docs (2026-08-12) to be
    genuinely Tripo's own texturing tech, just exposed through this
    platform's API instead of openapi.tripo3d.ai directly. Standard
    quality costs 20 credits, Detailed costs 40 (+10 for a style image).

    This still retextures an EXISTING model, same as the Tencent
    alternative - it needs a real, publicly reachable URL (model_url), not
    a local file path (the docs also mention an inline `model` file upload
    field as an alternative, not used by this script). If you need to
    texture a locally-built model, you'll need to host it somewhere
    reachable first - this script doesn't do that upload step.

    Response field names: submit confirmed as
    { "task_id": "...", "created_at": "..." } directly (not nested under
    .data). Status polling uses the same generic
    /v1/generation-request/{task_id}/status/ endpoint as the Tencent path,
    returning status/progress/a results array of download URLs - exact key
    names inside `results[]` weren't shown, so this script tries a few
    likely candidates and always dumps the raw JSON to
    Output/last-response-debug.json and Output/last-poll-debug.json so a
    mismatch is diagnosable without burning another generation, same
    convention Generate-Model.ps1 already uses.

.PARAMETER ModelUrl
    Publicly reachable URL to the source 3D model to retexture.

.PARAMETER Prompt
    Text description of the desired texture (e.g. "wood grain texture with
    natural finish").

.PARAMETER OutputName
    Base filename (no extension) for the downloaded result, saved under
    Tools/TextureAPI/Output/. Defaults to a sanitized version of the prompt.

.PARAMETER TimeoutSeconds
    How long to poll before giving up.

.EXAMPLE
    ./Generate-Texture.ps1 -ModelUrl "https://example.com/campfire.glb" -Prompt "charred wood grain, ring of grey rocks"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModelUrl,

    [Parameter(Mandatory = $true)]
    [string]$Prompt,

    [string]$OutputName,

    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$envFile = Join-Path $scriptDir ".env"
$outputDir = Join-Path $scriptDir "Output"
$baseUrl = "https://api.3daistudio.com"

if (-not (Test-Path $envFile)) {
    Write-Error "No .env file found at $envFile`nCopy .env.example to .env and fill in TEXTURE_API_KEY first."
    exit 1
}

$apiKey = $null
foreach ($line in Get-Content $envFile) {
    $trimmed = $line.Trim()
    if ($trimmed -eq "" -or $trimmed.StartsWith("#")) { continue }
    if ($trimmed -match "^TEXTURE_API_KEY\s*=\s*(.+)$") {
        $apiKey = $matches[1].Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Error "TEXTURE_API_KEY is empty in $envFile - fill in your real key."
    exit 1
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

if ([string]::IsNullOrWhiteSpace($OutputName)) {
    $OutputName = ($Prompt.ToLower() -replace "[^a-z0-9]+", "-").Trim("-")
    if ($OutputName.Length -gt 50) { $OutputName = $OutputName.Substring(0, 50).Trim("-") }
    if ([string]::IsNullOrWhiteSpace($OutputName)) { $OutputName = "texture" }
}

$headers = @{
    "Authorization" = "Bearer $apiKey"
    "Content-Type"  = "application/json"
}

Write-Host "Submitting Tripo texture-model task for prompt: `"$Prompt`""
$body = @{ model_url = $ModelUrl; prompt = $Prompt } | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/v1/3d-models/tripo/texture-model/" -Method Post -Headers $headers -Body $body
}
catch {
    Write-Host "Failed to create texture-edit task: $($_.Exception.Message)"
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
        Write-Host "Response body: $($_.ErrorDetails.Message)"
    }
    elseif ($_.Exception.Response) {
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $bodyText = $reader.ReadToEnd()
            Write-Host "Response body: $bodyText"
        }
        catch {
            Write-Host "(could not read response body)"
        }
    }
    exit 1
}

$createDebugPath = Join-Path $outputDir "last-response-debug.json"
$createResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath $createDebugPath -Encoding utf8

# Docs only confirmed a top-level "task_id" field ({ "task_id": "abc-123" })
# - fall back to a nested .data.task_id (Tripo3D's own shape) in case this
# API actually wraps it the same way.
$taskId = $createResponse.task_id
if ([string]::IsNullOrWhiteSpace($taskId)) { $taskId = $createResponse.data.task_id }
if ([string]::IsNullOrWhiteSpace($taskId)) {
    Write-Error "No task_id found in response (see $createDebugPath for the real shape)."
    exit 1
}
Write-Host "Task created: $taskId"

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$status = $null
$pollResponse = $null

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 3

    try {
        $pollResponse = Invoke-RestMethod -Uri "$baseUrl/v1/generation-request/$taskId/status/" -Method Get -Headers $headers
    }
    catch {
        Write-Error "Failed to poll task status: $($_.Exception.Message)"
        exit 1
    }

    $status = $pollResponse.status
    if ([string]::IsNullOrWhiteSpace($status)) { $status = $pollResponse.data.status }

    if ($status -match "(?i)^(finished|success|completed)$" -or $status -match "(?i)^(failed|error|cancelled)$") {
        break
    }

    Write-Host "  status: $status"
}

$pollDebugPath = Join-Path $outputDir "last-poll-debug.json"
$pollResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath $pollDebugPath -Encoding utf8
Write-Host "Full poll response saved to $pollDebugPath"

if ($status -notmatch "(?i)^(finished|success|completed)$") {
    Write-Error "Generation did not succeed - final status: $status (see $pollDebugPath for details)"
    exit 1
}

# Docs confirmed a "results array with download URLs" but not the exact
# key inside each result item - try the likely candidates in order, dump
# the debug JSON either way so a mismatch is fixable without re-running
# the generation.
$downloadUrl = $pollResponse.results[0].url
if ([string]::IsNullOrWhiteSpace($downloadUrl)) { $downloadUrl = $pollResponse.results[0].download_url }
if ([string]::IsNullOrWhiteSpace($downloadUrl)) { $downloadUrl = $pollResponse.results[0].model_url }
if ([string]::IsNullOrWhiteSpace($downloadUrl)) { $downloadUrl = $pollResponse.output.download_url }
if ([string]::IsNullOrWhiteSpace($downloadUrl)) { $downloadUrl = $pollResponse.output.model_url }
if ([string]::IsNullOrWhiteSpace($downloadUrl)) { $downloadUrl = $pollResponse.data.output.download_url }

if ([string]::IsNullOrWhiteSpace($downloadUrl)) {
    Write-Error "Task succeeded but no download URL found at any expected path (see $pollDebugPath for the real response shape - update this script's field-name guesses once you see it)."
    exit 1
}

$outputPath = Join-Path $outputDir "$OutputName.glb"
Write-Host "Downloading result to $outputPath"
Invoke-WebRequest -Uri $downloadUrl -OutFile $outputPath

Write-Host "Done: $outputPath"
