[CmdletBinding()]
param(
    [string]$RunnerRoot = "C:\actions-runner"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not [Environment]::UserInteractive -or [Diagnostics.Process]::GetCurrentProcess().SessionId -eq 0) {
    throw "Start this script from the logged-in Cloud PC desktop session, not from a service or session 0."
}

$runCmd = Join-Path $RunnerRoot "run.cmd"
if (-not (Test-Path $runCmd)) {
    throw "GitHub Actions runner not found at $runCmd. Run scripts\setup-cloudpc-ui-e2e-runner.ps1 first."
}

Set-Location $RunnerRoot
Write-Host "Starting GitHub Actions runner in interactive session $([Diagnostics.Process]::GetCurrentProcess().SessionId)."
Write-Host "Keep this window open while UI E2E jobs run."
& $runCmd
