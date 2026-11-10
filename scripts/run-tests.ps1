# scripts/run-tests.ps1
# Automated Test Runner for BillMitra
[CmdletBinding()]
param (
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
$RepoRoot = Split-Path -Parent $ScriptRoot
$SolutionPath = Join-Path $RepoRoot "BillMitra.slnx"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " BillMitra – Automated Test Suite Execution" -ForegroundColor Cyan
Write-Host " Target Solution: $SolutionPath" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

dotnet test $SolutionPath -c $Configuration --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) {
    Write-Host "`nTest execution failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`nAll test suites passed successfully!" -ForegroundColor Green
