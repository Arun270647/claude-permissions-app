# GitHub Release Creation Script
# This script creates a GitHub release and uploads the Windows executable

$ErrorActionPreference = "Stop"

$repo = "Arun270647/claude-permissions-app"
$tag = "v1.0.0"
$name = "v1.0.0 - Initial Release"
$exePath = "publish\win-x64\ClaudePermissionAssistant.exe"
$releaseNotesPath = "RELEASE_NOTES_v1.0.0.md"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GitHub Release Creator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if executable exists
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Executable not found at $exePath" -ForegroundColor Red
    Write-Host "Run rebuild.bat first!" -ForegroundColor Red
    exit 1
}

$fileSize = (Get-Item $exePath).Length / 1MB
Write-Host "Found executable: $exePath ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor Green

# Check if release notes exist
if (-not (Test-Path $releaseNotesPath)) {
    Write-Host "ERROR: Release notes not found at $releaseNotesPath" -ForegroundColor Red
    exit 1
}

$releaseNotes = Get-Content $releaseNotesPath -Raw
Write-Host "Loaded release notes from $releaseNotesPath" -ForegroundColor Green
Write-Host ""

# Check for GitHub token
$token = $env:GITHUB_TOKEN
if (-not $token) {
    Write-Host "GitHub token not found in environment." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To create a release, you need a GitHub Personal Access Token." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Steps to create one:" -ForegroundColor White
    Write-Host "1. Go to: https://github.com/settings/tokens/new" -ForegroundColor White
    Write-Host "2. Name: 'Release Creator'" -ForegroundColor White
    Write-Host "3. Expiration: 7 days" -ForegroundColor White
    Write-Host "4. Scopes: Check 'repo' (Full control of private repositories)" -ForegroundColor White
    Write-Host "5. Click 'Generate token'" -ForegroundColor White
    Write-Host "6. Copy the token" -ForegroundColor White
    Write-Host ""

    $token = Read-Host "Paste your GitHub token here (input is hidden)" -AsSecureString
    $token = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($token))

    if (-not $token) {
        Write-Host "No token provided. Exiting." -ForegroundColor Red
        exit 1
    }
}

Write-Host "GitHub token found!" -ForegroundColor Green
Write-Host ""

# Create the release
Write-Host "Creating release $tag..." -ForegroundColor Cyan

$releaseBody = @{
    tag_name = $tag
    name = $name
    body = $releaseNotes
    draft = $false
    prerelease = $false
} | ConvertTo-Json -Depth 10

$headers = @{
    "Authorization" = "token $token"
    "Accept" = "application/vnd.github.v3+json"
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases" -Method Post -Headers $headers -Body $releaseBody
    Write-Host "✅ Release created successfully!" -ForegroundColor Green
    Write-Host "Release ID: $($response.id)" -ForegroundColor Gray
    Write-Host "Release URL: $($response.html_url)" -ForegroundColor Gray
    Write-Host ""

    $uploadUrl = $response.upload_url -replace '\{\?name,label\}', ''
    $assetName = "ClaudePermissionAssistant-Windows-v1.0.0.exe"

    # Upload the asset
    Write-Host "Uploading Windows executable..." -ForegroundColor Cyan
    Write-Host "This may take a minute (file is $([math]::Round($fileSize, 2)) MB)..." -ForegroundColor Gray

    $uploadHeaders = @{
        "Authorization" = "token $token"
        "Accept" = "application/vnd.github.v3+json"
        "Content-Type" = "application/octet-stream"
    }

    $fileBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $exePath))

    $uploadResponse = Invoke-RestMethod -Uri "$uploadUrl`?name=$assetName" -Method Post -Headers $uploadHeaders -Body $fileBytes

    Write-Host "✅ Executable uploaded successfully!" -ForegroundColor Green
    Write-Host "Download URL: $($uploadResponse.browser_download_url)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "RELEASE PUBLISHED!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "View at: $($response.html_url)" -ForegroundColor Cyan
    Write-Host ""

} catch {
    Write-Host "❌ Failed to create release" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red

    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "API Response: $responseBody" -ForegroundColor Red
    }

    exit 1
}
