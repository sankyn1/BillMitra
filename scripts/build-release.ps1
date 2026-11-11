# BillMitra Production Release & Installer Packaging Script
param(
    [string]$Configuration = "Release",
    [string]$SemanticVersion = "1.0.0"
)

$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
$DevelopmentDir = Join-Path $RootDir "artifacts\development"
$ReleaseDir = Join-Path $RootDir "artifacts\release"
$FrontendDir = Join-Path $RootDir "src\Frontend\billmitra-ui"
$BackendApiDir = Join-Path $RootDir "src\Backend\BillMitra.Api"
$DesktopDir = Join-Path $RootDir "src\Desktop\BillMitra.Desktop"
$InstallerScript = Join-Path $RootDir "scripts\installer.iss"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " BillMitra Production Release Packager (v$SemanticVersion)" -ForegroundColor Cyan
Write-Host " Target Release Directory: $ReleaseDir" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Clean & Prepare Artifacts Directories
Write-Host "`n[1/6] Preparing artifact directories..." -ForegroundColor Yellow
if (Test-Path $ReleaseDir) {
    Remove-Item -Path $ReleaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

# 2. Build Development Package First
Write-Host "`n[2/6] Compiling full distribution via build-development.ps1..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot "build-development.ps1") -Configuration $Configuration -SemanticVersion $SemanticVersion

# 3. Create Standalone Release ZIP Archive
Write-Host "`n[3/6] Packaging portable zip distribution..." -ForegroundColor Yellow
$ZipPath = Join-Path $ReleaseDir "BillMitra-v$SemanticVersion-win-x64.zip"
Compress-Archive -Path "$DevelopmentDir\*" -DestinationPath $ZipPath -Force
Write-Host "Created portable archive: $ZipPath" -ForegroundColor Green

# 4. Check for Inno Setup Compiler (ISCC)
Write-Host "`n[4/6] Checking for Inno Setup Compiler (ISCC)..." -ForegroundColor Yellow
$IsccPath = $null
$CandidatePaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
)

foreach ($path in $CandidatePaths) {
    if (Test-Path $path) {
        $IsccPath = $path
        break
    }
}

if (-not $IsccPath) {
    $cmd = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($cmd) {
        $IsccPath = $cmd.Source
    }
}

if ($IsccPath) {
    Write-Host "Found Inno Setup at: $IsccPath" -ForegroundColor Green
    Write-Host "Compiling setup executable using Inno Setup..." -ForegroundColor Yellow
    & $IsccPath $InstallerScript
    Write-Host "Setup installer created under: $ReleaseDir" -ForegroundColor Green
} else {
    Write-Host "ISCC.exe was not detected in default paths. Skipping .exe installer compilation." -ForegroundColor DarkGray
    Write-Host "Note: installer.iss is preconfigured and ready to run with Inno Setup 6." -ForegroundColor DarkGray
}

# 5. Output Manifest & Checksums
Write-Host "`n[5/6] Generating SHA256 checksums..." -ForegroundColor Yellow
$Checksums = @()
Get-ChildItem -Path $ReleaseDir -File | ForEach-Object {
    $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash
    $Checksums += [PSCustomObject]@{
        FileName = $_.Name
        SizeBytes = $_.Length
        SHA256 = $hash
    }
}
$Checksums | Out-File (Join-Path $ReleaseDir "SHA256SUMS.txt") -Encoding utf8
$Checksums | Format-Table -AutoSize

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " RELEASE PACKAGING COMPLETED!" -ForegroundColor Cyan
Write-Host " Location: $ReleaseDir" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
