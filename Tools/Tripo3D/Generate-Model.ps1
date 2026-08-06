<#
.SYNOPSIS
    Generate a 3D model from a text prompt via the Tripo3D API and download
    the resulting .glb.

.DESCRIPTION
    Talks to Tripo3D's REST API directly (https://openapi.tripo3d.ai) rather
    than the browser-based DCC Bridge, so it can run headlessly from a
    script. Requires an API key in Tools/Tripo3D/.env (copy .env.example
    and fill it in) - see that file for where to get one.

    Model URLs from Tripo3D expire 5 minutes after the task succeeds, so
    this script downloads immediately rather than just printing the URL.

.PARAMETER Prompt
    Text description of the model to generate.

.PARAMETER OutputName
    Base filename (no extension) for the downloaded .glb, saved under
    Tools/Tripo3D/Output/. Defaults to a sanitized version of the prompt.

.PARAMETER Model
    Tripo3D model version to use. Check https://developers.tripo3d.ai for
    the current recommended value if generation starts failing with a
    model-version error - this default will go stale over time.

.PARAMETER TimeoutSeconds
    How long to poll before giving up. Generation is typically 10-120s, but
    observed in practice sitting at 99% for a while before flipping to
    success - default is padded well past the typical range for that.

.EXAMPLE
    ./Generate-Model.ps1 -Prompt "a mossy stone pickaxe, low-poly"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Prompt,

    [string]$OutputName,

    [string]$Model = "v3.1-20260211",

    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$envFile = Join-Path $scriptDir ".env"
$outputDir = Join-Path $scriptDir "Output"
$baseUrl = "https://openapi.tripo3d.ai/v3"

if (-not (Test-Path $envFile)) {
    Write-Error "No .env file found at $envFile`nCopy .env.example to .env and fill in TRIPO3D_API_KEY first."
    exit 1
}

$apiKey = $null
foreach ($line in Get-Content $envFile) {
    $trimmed = $line.Trim()
    if ($trimmed -eq "" -or $trimmed.StartsWith("#")) { continue }
    if ($trimmed -match "^TRIPO3D_API_KEY\s*=\s*(.+)$") {
        $apiKey = $matches[1].Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Error "TRIPO3D_API_KEY is empty in $envFile - fill in your real key."
    exit 1
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

if ([string]::IsNullOrWhiteSpace($OutputName)) {
    $OutputName = ($Prompt.ToLower() -replace "[^a-z0-9]+", "-").Trim("-")
    if ($OutputName.Length -gt 50) { $OutputName = $OutputName.Substring(0, 50).Trim("-") }
    if ([string]::IsNullOrWhiteSpace($OutputName)) { $OutputName = "model" }
}

$headers = @{
    "Authorization" = "Bearer $apiKey"
    "Content-Type"  = "application/json"
}

Write-Host "Submitting generation task for prompt: `"$Prompt`""
$body = @{ prompt = $Prompt; model = $Model } | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/generation/text-to-model" -Method Post -Headers $headers -Body $body
}
catch {
    Write-Host "Failed to create generation task: $($_.Exception.Message)"
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

$taskId = $createResponse.data.task_id
if ([string]::IsNullOrWhiteSpace($taskId)) {
    Write-Error "No task_id in response: $($createResponse | ConvertTo-Json -Depth 5)"
    exit 1
}
Write-Host "Task created: $taskId"

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$status = "queued"
$taskData = $null

while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2

    try {
        $pollResponse = Invoke-RestMethod -Uri "$baseUrl/tasks/$taskId" -Method Get -Headers $headers
    }
    catch {
        Write-Error "Failed to poll task status: $($_.Exception.Message)"
        exit 1
    }

    $taskData = $pollResponse.data
    $status = $taskData.status

    if ($status -eq "success" -or $status -eq "failed" -or $status -eq "cancelled" -or $status -eq "banned") {
        break
    }

    Write-Host "  status: $status (progress: $($taskData.progress))"
}

# Always dump the last raw response - the docs' exact field names have
# already proven unreliable once; this makes the real schema inspectable
# without burning another generation to see it.
$debugPath = Join-Path $outputDir "last-response-debug.json"
$pollResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath $debugPath -Encoding utf8
Write-Host "Full response saved to $debugPath"

if ($status -ne "success") {
    Write-Error "Generation did not succeed - final status: $status (see $debugPath for details)"
    exit 1
}

$modelUrl = $taskData.output.model_url
if ([string]::IsNullOrWhiteSpace($modelUrl)) {
    Write-Error "Task succeeded but no output.model_url present at the expected path (see $debugPath for the real response shape)"
    exit 1
}

# Model URLs expire 5 minutes after success - download right away.
$outputPath = Join-Path $outputDir "$OutputName.glb"
Write-Host "Downloading model to $outputPath"
Invoke-WebRequest -Uri $modelUrl -OutFile $outputPath

$previewUrl = $taskData.output.rendered_image_url
if (-not [string]::IsNullOrWhiteSpace($previewUrl)) {
    $previewPath = Join-Path $outputDir "$OutputName.png"
    Invoke-WebRequest -Uri $previewUrl -OutFile $previewPath
    Write-Host "Preview image saved to $previewPath"
}

Write-Host "Done: $outputPath"
