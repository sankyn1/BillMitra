# BillMitra Development Executable & Packaging Script
# Produces runnable Day-1 Windows executable package under artifacts/development/

param(
    [string]$Configuration = "Release",
    [string]$SemanticVersion = "0.9.0-beta.1"
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
Write-Host "`n[0/6] Stopping existing development instances and preparing artifacts directory..." -ForegroundColor Yellow
Stop-Process -Name "BillMitra", "BillMitra.Api" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800

if (Test-Path $ArtifactsDir) {
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
    Milestone = "MVP-4 Payments, Receipts, Multi-Invoice Allocation & Ledgers"
} | ConvertTo-Json -Depth 4

Set-Content -Path (Join-Path $ArtifactsDir "version.json") -Value $VersionMetadata

# 6. Generate README-TESTING.md
Write-Host "`n[6/6] Generating README-TESTING.md..." -ForegroundColor Yellow
$ReadmePath = Join-Path $ArtifactsDir "README-TESTING.md"

$ReadmeContent = @'
# BillMitra - Development Testing Package (MVP-4)

- Target Environment: Windows 10/11 x64
- Milestone: MVP-4 (Payments, Multi-Invoice Allocation, Ledgers & Aging)

## What This Package Delivers
- Native Desktop Executable: BillMitra.exe (WPF + WebView2 container).
- First-Run Setup Wizard: Complete company onboarding, Indian GST validation, PAN extraction, and 36 GST state codes.
- Item Master & Party Master: Products, services, HSN/SAC codes, tax rates, customers, and suppliers.
- Central GST Calculation & Sales Documents (MVP-3): Intra-state (CGST+SGST), Inter-state (IGST), RCM, discounts, Section 170 round-off, 16-character Rule 46 numbering, QuestPDF generation (A4, A5, 80mm thermal receipt).
- Payments, Receipts & Multi-Invoice Settle (MVP-4):
  - Inward Customer Receipts & Outward Supplier Payments.
  - Multi-invoice FIFO auto-allocation & partial payment tracking.
  - Advance balance (unallocated amount) tracking.
  - Customer & Supplier transaction ledgers with chronological running balance.
  - Overdue aging bucket analysis (0-30, 31-60, 61-90, 90+ days).
  - Transactional payment reversal with audit reason logging and automated invoice balance recovery.
- Centralized Professional Dialog System: No browser alert/confirm calls; polished modal cards and auto-dismissing toasts.
- Uniform UI Design System: Consistent dropdown styling with SVG chevrons, tokenized colors, focus rings, and matched heights.
- Automatic Process Supervision: Launching BillMitra.exe starts the backend API on http://127.0.0.1:5000, checks health, and renders inside the native window.

## How to Test This Build
1. Navigate to artifacts\development\
2. Double-click BillMitra.exe
3. Navigate to "Payments & Receipts" in the sidebar.
4. Click "+ Record Customer Receipt" to record a payment, select a customer, view their open invoices, and allocate payments or leave as advance.
5. Open "Party Ledgers & Aging" to inspect chronological running balance statements and overdue aging buckets.
'@

Set-Content -Path $ReadmePath -Value $ReadmeContent -Encoding UTF8

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " BUILD SUCCESSFUL! Development package generated at:" -ForegroundColor Green
Write-Host " $ArtifactsDir\BillMitra.exe" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
