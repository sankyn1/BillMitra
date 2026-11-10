# BillMitra Development Executable & Packaging Script
# Produces runnable Day-1 Windows executable package under artifacts/development/

param(
    [string]$Configuration = "Release",
    [string]$SemanticVersion = "0.2.0-beta.1"
)

$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
$ArtifactsDir = Join-Path $RootDir "artifacts\development"
$FrontendDir = Join-Path $RootDir "src\Frontend\billmitra-ui"
$BackendApiDir = Join-Path $RootDir "src\Backend\BillMitra.Api"
$DesktopDir = Join-Path $RootDir "src\Desktop\BillMitra.Desktop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " BillMitra - Development Package Builder ($SemanticVersion)" -ForegroundColor Cyan
Write-Host " Target Directory: $ArtifactsDir" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 0. Clean & Prepare Artifacts Directory
if (Test-Path $ArtifactsDir) {
    Write-Host "`n[0/6] Cleaning existing artifacts directory..." -ForegroundColor Yellow
    Remove-Item -Path $ArtifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $ArtifactsDir "Backend") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $ArtifactsDir "Frontend") -Force | Out-Null

# 1. Build Angular Frontend
Write-Host "`n[1/6] Compiling Angular 22 Frontend (Standalone + Signals)..." -ForegroundColor Yellow
Push-Location $FrontendDir
try {
    npm run build
}
finally {
    Pop-Location
}

$UiDistDir = Join-Path $FrontendDir "dist\billmitra-ui\browser"
if (-not (Test-Path $UiDistDir)) {
    $UiDistDir = Join-Path $FrontendDir "dist\billmitra-ui"
}

Copy-Item -Path "$UiDistDir\*" -Destination (Join-Path $ArtifactsDir "Frontend") -Recurse -Force
Write-Host "Frontend bundle copied to artifacts/development/Frontend." -ForegroundColor Green

# 2. Publish Backend API
Write-Host "`n[2/6] Publishing ASP.NET Core .NET 10 Backend API..." -ForegroundColor Yellow
$BackendPublishDir = Join-Path $ArtifactsDir "Backend"
dotnet publish $BackendApiDir -c $Configuration -r win-x64 --self-contained false -o $BackendPublishDir

# Copy wwwroot static files into Backend publish directory
$BackendWwwroot = Join-Path $BackendPublishDir "wwwroot"
if (Test-Path $BackendWwwroot) {
    Remove-Item -Path $BackendWwwroot -Recurse -Force
}
Copy-Item -Path (Join-Path $ArtifactsDir "Frontend") -Destination $BackendWwwroot -Recurse -Force
Write-Host "Backend API published with embedded frontend static files." -ForegroundColor Green

# 3. Publish Desktop Shell (WPF + WebView2)
Write-Host "`n[3/6] Building Desktop Host Shell (WPF + WebView2)..." -ForegroundColor Yellow
dotnet publish $DesktopDir -c $Configuration -r win-x64 --self-contained false -o $ArtifactsDir

# 4. Verify Runnable Executable
$ExePath = Join-Path $ArtifactsDir "BillMitra.exe"
if (-not (Test-Path $ExePath)) {
    throw "Fatal: BillMitra.exe was not created in $ArtifactsDir"
}
Write-Host "Verified primary executable: $ExePath" -ForegroundColor Green

# 5. Generate version.json
Write-Host "`n[5/6] Generating version.json manifest..." -ForegroundColor Yellow
$CommitHash = "unknown"
try {
    $CommitHash = (git rev-parse --short HEAD).Trim()
} catch {}

$VersionMetadata = @{
    AppName = "BillMitra"
    Version = $SemanticVersion
    GitCommit = $CommitHash
    BuildDateUtc = [DateTime]::UtcNow.ToString("o")
    TargetOs = "Windows 10/11 x64"
    SchemaVersion = "1.0.0"
    Milestone = "MVP-1 Setup & Persistence"
} | ConvertTo-Json -Depth 4

Set-Content -Path (Join-Path $ArtifactsDir "version.json") -Value $VersionMetadata

# 6. Generate README-TESTING.md
Write-Host "`n[6/6] Generating README-TESTING.md..." -ForegroundColor Yellow
$ReadmePath = Join-Path $ArtifactsDir "README-TESTING.md"

$ReadmeContent = @'
# BillMitra - Development Testing Package (MVP-1)

- Target Environment: Windows 10/11 x64

## What This Package Delivers
- Native Desktop Executable: BillMitra.exe (WPF + WebView2 container).
- First-Run Setup Wizard: Complete company onboarding, Indian GST validation, PAN extraction, and 36 GST state codes.
- Local Database Persistence: Dapper runtime repositories storing company profile, accounting financial years, and admin credentials in local SQLEXPRESS / SQLite fallback.
- Supervised Backend API: Hosted under Backend/ (ASP.NET Core .NET 10 Kestrel).
- Integrated Frontend UI: Hosted under Frontend/ (Angular 22 + PrimeNG + Centralized Directives).
- Automatic Child Process Supervision: Launching BillMitra.exe starts the backend API on http://127.0.0.1:5000, verifies health, and renders the application inside the native window.

## Prerequisites
1. .NET 10 Runtime (Windows x64)
2. Microsoft Edge WebView2 Runtime
3. Local SQL Server (SQLEXPRESS) or PostgreSQL (Automated fallback to SQLite is active if neither is running)

## How to Test This Build
1. Navigate to artifacts\development\
2. Double-click BillMitra.exe
3. If uninitialized, the 4-step BillMitra Business Setup Wizard will be displayed.
4. Fill in business details (e.g. Legal Name, State, GSTIN, and Admin password).
5. Click 'Complete Setup & Open BillMitra' to initialize your database and unlock the live Executive Control Center.
'@

Set-Content -Path $ReadmePath -Value $ReadmeContent -Encoding UTF8

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " BUILD SUCCESSFUL! Development package generated at:" -ForegroundColor Green
Write-Host " $ArtifactsDir\BillMitra.exe" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
