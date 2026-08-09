<#
.SYNOPSIS
    Apply an AI-generated texture to an existing local 3D model file via
    Tripo3D's texture_model task.

.DESCRIPTION
    Uses a different API surface than Generate-Model.ps1: that script talks
    to the path-based v3 REST API (openapi.tripo3d.ai/v3/generation/...),
    which has no documented endpoint for texturing a model that didn't
    originate from a Tripo3D generation. This script instead uses the
    task-based v2 API (api.tripo3d.ai/v2/openapi), confirmed against
    Tripo3D's own official Python SDK source
    (github.com/VAST-AI-Research/tripo-python-sdk) since the interactive
    docs site is a JS-rendered SPA that isn't readable via a simple fetch.
    Same API key works for both surfaces.

    Three-step pipeline, same Bearer-token auth as Generate-Model.ps1:
      1. POST /upload (multipart/form-data) - uploads the local model file,
         returns an image_token. (Named "image_token" even for a model file
         in Tripo3D's own SDK - a generic upload endpoint, not image-only.)
         Done via curl.exe rather than Invoke-RestMethod - Windows
         PowerShell 5.1 has no clean native multipart/form-data support.
      2. POST /task {type: import_model} - registers the uploaded file as a
         real Tripo3D task, giving it a task_id texture_model can reference
         as original_model_task_id. Polled via GET /task/{id} same as
         Generate-Model.ps1's v3 polling, just singular "/task" not "/tasks".
      3. POST /task {type: texture_model} - generates new PBR textures from
         a text prompt for that imported model. Polled the same way.

    First real run against this endpoint shape - the exact response field
    names (image_token vs file_token, output.model_url vs something else)
    are inferred from the SDK's documented request shape, not a confirmed
    live response. Every step dumps its raw JSON to Output/ for inspection
    if a guessed field name turns out wrong.

.PARAMETER InputModel
    Path to the local .glb/.obj/.fbx/.stl to texture.

.PARAMETER Prompt
    Text description of the desired texture/material look.

.PARAMETER OutputName
    Base filename (no extension) for the downloaded result, saved under
    Tools/Tripo3D/Output/. Defaults to "<InputModel base name>-textured".

.PARAMETER TextureQuality
    "standard" or "detailed" (detailed costs more credits).

.EXAMPLE
    ./Texture-Model.ps1 -InputModel "..\..\Assets\Models\TrimmedStickMasterwork.glb" -Prompt "polished walnut wood grain"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputModel,

    [Parameter(Mandatory = $true)]
    [string]$Prompt,

    [string]$OutputName,

    [ValidateSet("standard", "detailed")]
    [string]$TextureQuality = "detailed",

    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$envFile = Join-Path $scriptDir ".env"
$outputDir = Join-Path $scriptDir "Output"
$baseUrl = "https://api.tripo3d.ai/v2/openapi"

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

if (-not (Test-Path $InputModel)) {
    Write-Error "Input model not found: $InputModel"
    exit 1
}
$InputModel = (Resolve-Path $InputModel).Path

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}
if ([string]::IsNullOrWhiteSpace($OutputName)) {
    $OutputName = [System.IO.Path]::GetFileNameWithoutExtension($InputModel) + "-textured"
}

$headers = @{ "Authorization" = "Bearer $apiKey" }
$jsonHeaders = @{ "Authorization" = "Bearer $apiKey"; "Content-Type" = "application/json" }

function Wait-TripoTask {
    param([string]$TaskId, [string]$Label)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $status = "queued"
    $taskData = $null

    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2

        try {
            $pollResponse = Invoke-RestMethod -Uri "$baseUrl/task/$TaskId" -Method Get -Headers $headers
        }
        catch {
            Write-Error "Failed to poll $Label task: $($_.Exception.Message)"
            exit 1
        }

        $taskData = $pollResponse.data
        $status = $taskData.status

        if ($status -eq "success" -or $status -eq "failed" -or $status -eq "cancelled" -or $status -eq "banned") {
            break
        }
        Write-Host "  [$Label] status: $status (progress: $($taskData.progress))"
    }

    $debugPath = Join-Path $outputDir "last-$Label-response-debug.json"
    $taskData | ConvertTo-Json -Depth 10 | Out-File -FilePath $debugPath -Encoding utf8
    Write-Host "[$Label] full response saved to $debugPath"

    if ($status -ne "success") {
        Write-Error "$Label did not succeed - final status: $status (see $debugPath for details)"
        exit 1
    }
    return $taskData
}

# Step 1: upload the local model file via Tripo3D's STS-credentialed S3
# upload (the legacy /upload endpoint turned out to be image-only - a real
# .glb was rejected with "This image file type is not supported"). Needs
# the AWS.Tools.S3 module (Install-Module -Name AWS.Tools.S3 -Scope
# CurrentUser) for real SigV4-signed PUT support against the temporary
# credentials /upload/sts/token hands back.
if (-not (Get-Module -ListAvailable -Name AWS.Tools.S3)) {
    Write-Error "AWS.Tools.S3 module not installed. Run: Install-Module -Name AWS.Tools.S3 -Scope CurrentUser -Force"
    exit 1
}
Import-Module AWS.Tools.S3 -ErrorAction Stop

$ext = [System.IO.Path]::GetExtension($InputModel).TrimStart(".").ToLower()
Write-Host "Requesting STS upload token for .$ext..."
$stsBody = @{ format = $ext } | ConvertTo-Json
try {
    $stsResponse = Invoke-RestMethod -Uri "$baseUrl/upload/sts/token" -Method Post -Headers $jsonHeaders -Body $stsBody
}
catch {
    Write-Host "Failed to get STS token: $($_.Exception.Message)"
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) { Write-Host "Response body: $($_.ErrorDetails.Message)" }
    exit 1
}
$sts = $stsResponse.data
$stsDebugPath = Join-Path $outputDir "last-sts-response-debug.json"
$stsResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath $stsDebugPath -Encoding utf8

$s3Region = "us-east-1"
if ($sts.s3_host -match "^s3\.([a-z0-9-]+)\.amazonaws\.com$") {
    $s3Region = $matches[1]
}
Write-Host "Uploading $InputModel to s3://$($sts.resource_bucket)/$($sts.resource_uri) (region $s3Region)..."
Write-S3Object -BucketName $sts.resource_bucket -Key $sts.resource_uri -File $InputModel `
    -AccessKey $sts.sts_ak -SecretKey $sts.sts_sk -SessionToken $sts.session_token `
    -Region $s3Region
Write-Host "Uploaded."

# Step 2: register the uploaded file as a real Tripo3D model task.
$importBody = @{
    type = "import_model"
    file = @{ object = @{ bucket = $sts.resource_bucket; key = $sts.resource_uri } }
} | ConvertTo-Json -Depth 5

try {
    $importCreate = Invoke-RestMethod -Uri "$baseUrl/task" -Method Post -Headers $jsonHeaders -Body $importBody
}
catch {
    Write-Host "Failed to create import_model task: $($_.Exception.Message)"
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) { Write-Host "Response body: $($_.ErrorDetails.Message)" }
    exit 1
}

$importTaskId = $importCreate.data.task_id
if ([string]::IsNullOrWhiteSpace($importTaskId)) {
    Write-Error "No task_id in import_model response: $($importCreate | ConvertTo-Json -Depth 5)"
    exit 1
}
Write-Host "Import task created: $importTaskId"
Wait-TripoTask -TaskId $importTaskId -Label "import" | Out-Null
Write-Host "Import succeeded."

# Step 3: texture the imported model from a text prompt.
$textureBody = @{
    type                    = "texture_model"
    original_model_task_id  = $importTaskId
    texture_prompt          = @{ text = $Prompt }
    texture_quality         = $TextureQuality
    pbr                     = $true
} | ConvertTo-Json -Depth 5

try {
    $textureCreate = Invoke-RestMethod -Uri "$baseUrl/task" -Method Post -Headers $jsonHeaders -Body $textureBody
}
catch {
    Write-Host "Failed to create texture_model task: $($_.Exception.Message)"
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) { Write-Host "Response body: $($_.ErrorDetails.Message)" }
    exit 1
}

$textureTaskId = $textureCreate.data.task_id
if ([string]::IsNullOrWhiteSpace($textureTaskId)) {
    Write-Error "No task_id in texture_model response: $($textureCreate | ConvertTo-Json -Depth 5)"
    exit 1
}
Write-Host "Texture task created: $textureTaskId"
$textureResult = Wait-TripoTask -TaskId $textureTaskId -Label "texture"
Write-Host "Texture succeeded."

$modelUrl = $textureResult.output.model
if ([string]::IsNullOrWhiteSpace($modelUrl)) {
    Write-Error "Task succeeded but no output.model present - see the texture debug JSON in $outputDir for the real response shape."
    exit 1
}

$outputPath = Join-Path $outputDir "$OutputName.glb"
Write-Host "Downloading textured model to $outputPath"
Invoke-WebRequest -Uri $modelUrl -OutFile $outputPath

$previewUrl = $textureResult.output.rendered_image
if (-not [string]::IsNullOrWhiteSpace($previewUrl)) {
    $previewPath = Join-Path $outputDir "$OutputName-preview.webp"
    Invoke-WebRequest -Uri $previewUrl -OutFile $previewPath
    Write-Host "Preview image saved to $previewPath"
}

Write-Host "Done: $outputPath"
