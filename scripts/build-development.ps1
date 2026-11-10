# scripts/build-development.ps1
# Automated Full-Stack Packaging Script for BillMitra (Day-1 Executable Delivery)
[CmdletBinding()]
param (
    [string]$Configuration = "Release",
    [string]$SemanticVersion = "0.1.0-alpha.mvp0"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
$RepoRoot = Split-Path -Parent $ScriptRoot
$ArtifactsDir = Join-Path $RepoRoot "artifacts\development"
$BackendPublishDir = Join-Path $ArtifactsDir "Backend"
$FrontendPublishDir = Join-Path $ArtifactsDir "Frontend"
$WwwRootDir = Join-Path $BackendPublishDir "wwwroot"
$AngularProjectDir = Join-Path $RepoRoot "src\Frontend\billmitra-ui"
$DesktopProjectDir = Join-Path $RepoRoot "src\Desktop\BillMitra.Desktop"
$ApiProjectDir = Join-Path $RepoRoot "src\Backend\BillMitra.Api"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " BillMitra – Automated Packaging Pipeline (MVP-0)" -ForegroundColor Cyan
Write-Host " Version: $SemanticVersion | Target: $ArtifactsDir" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Clean output artifacts directory
Write-Host "`n[1/6] Cleaning artifacts directory..." -ForegroundColor Yellow
if (Test-Path $ArtifactsDir) {
    Remove-Item -Path $ArtifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null
New-Item -ItemType Directory -Path $BackendPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $FrontendPublishDir -Force | Out-Null

# 2. Build Angular Frontend in Production Mode
Write-Host "`n[2/6] Building Angular 22 Frontend (PrimeNG + Standalone)..." -ForegroundColor Yellow
Push-Location $AngularProjectDir
try {
    npm run build -- --configuration production
    if ($LASTEXITCODE -ne 0) { throw "Angular build failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

# Copy Frontend Build Output
$AngularDist = Join-Path $AngularProjectDir "dist\billmitra-ui\browser"
if (-not (Test-Path $AngularDist)) {
    $AngularDist = Join-Path $AngularProjectDir "dist\billmitra-ui"
}
Write-Host "Copying compiled Angular SPA from $AngularDist..." -ForegroundColor Gray
Copy-Item -Path "$AngularDist\*" -Destination $FrontendPublishDir -Recurse -Force

# 3. Publish Backend ASP.NET Core API (.NET 10)
Write-Host "`n[3/6] Publishing Backend API (.NET 10 Kestrel)..." -ForegroundColor Yellow
dotnet publish $ApiProjectDir -c $Configuration -r win-x64 --self-contained false -o $BackendPublishDir
if ($LASTEXITCODE -ne 0) { throw "Backend publish failed with exit code $LASTEXITCODE" }

# Copy Angular assets into Backend wwwroot so Kestrel can serve them directly
New-Item -ItemType Directory -Path $WwwRootDir -Force | Out-Null
Copy-Item -Path "$FrontendPublishDir\*" -Destination $WwwRootDir -Recurse -Force

# 4. Publish Desktop Shell (WPF + WebView2)
Write-Host "`n[4/6] Publishing Desktop Shell (WPF + WebView2)..." -ForegroundColor Yellow
dotnet publish $DesktopProjectDir -c $Configuration -r win-x64 --self-contained false -o $ArtifactsDir
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed with exit code $LASTEXITCODE" }

# Rename desktop shell to BillMitra.exe if needed
$DesktopExe = Join-Path $ArtifactsDir "BillMitra.Desktop.exe"
$TargetExe = Join-Path $ArtifactsDir "BillMitra.exe"
if (Test-Path $DesktopExe) {
    Copy-Item -Path $DesktopExe -Destination $TargetExe -Force
}

# 5. Generate version.json
Write-Host "`n[5/6] Generating version metadata..." -ForegroundColor Yellow
$CommitHash = "initial-commit"
try {
    $GitHash = git rev-parse --short HEAD 2>$null
    if ($GitHash) { $CommitHash = $GitHash }
} catch {}

$VersionMetadata = @{
    AppName = "BillMitra"
    Version = $SemanticVersion
    GitCommit = $CommitHash
    BuildDateUtc = [DateTime]::UtcNow.ToString("o")
    TargetOs = "Windows 10/11 x64"
    SchemaVersion = "1.0.0"
    Milestone = "MVP-0 Foundation"
} | ConvertTo-Json -Depth 4

Set-Content -Path (Join-Path $ArtifactsDir "version.json") -Value $VersionMetadata

# 6. Generate README-TESTING.md
Write-Host "`n[6/6] Generating README-TESTING.md..." -ForegroundColor Yellow
$ReadmeContent = @"
# BillMitra – Development Testing Package (MVP-0)

**Package Version:** $SemanticVersion  
**Build Date:** $([DateTime]::UtcNow.ToString("yyyy-MM-dd HH:mm:ss")) UTC  
**Target Environment:** Windows 10/11 x64  

---

## What This Package Delivers
* **Native Desktop Executable:** ``BillMitra.exe`` (WPF + WebView2 container).
* **Supervised Backend API:** Hosted under ``Backend/`` (ASP.NET Core .NET 10 Kestrel).
* **Integrated Frontend UI:** Hosted under ``Frontend/`` (Angular 22 + PrimeNG + Centralized Directives).
* **Automatic Child Process Supervision:** Launching ``BillMitra.exe`` starts the backend API on ``http://127.0.0.1:5000``, verifies health, and renders the application inside the native window.

---

## Prerequisites
1. **.NET 10 Runtime (Windows x64)** – installed on host.
2. **Microsoft Edge WebView2 Runtime** – standard on Windows 10/11.
3. **PostgreSQL 16+** (Optional for UI test, required for DB operations):
   - Default Connection: ``Host=localhost;Port=5432;Database=billmitra_dev;Username=postgres;Password=postgres;``
   - Connection strings can be customized in ``Backend/appsettings.json``.

---

## How to Test This Build
1. Navigate to:
   ``artifacts\development\``
2. Double-click:
   ``BillMitra.exe``
3. **Expected Behavior:**
   - The desktop splash window appears and checks backend health.
   - The child API process is started automatically on ``http://127.0.0.1:5000``.
   - The commercial Angular dashboard loads inside the window with uniform header, collapsible sidebar, KPI cards, and diagnostics table.
   - Live health probe reports API status.
   - Exiting the window cleanly terminates the background API process.

---

## Verification Endpoints
* **Swagger Documentation:** ``http://127.0.0.1:5000/swagger``
* **System Health Endpoint:** ``http://127.0.0.1:5000/api/v1/system/health``
* **Simple Health Check:** ``http://127.0.0.1:5000/health``
"@

Set-Content -Path (Join-Path $ArtifactsDir "README-TESTING.md") -Value $ReadmeContent

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " BUILD SUCCESSFUL! Development package generated at:" -ForegroundColor Green
Write-Host " $ArtifactsDir\BillMitra.exe" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
