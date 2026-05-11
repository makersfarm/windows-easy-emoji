[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$sessionId = [Diagnostics.Process]::GetCurrentProcess().SessionId
$isInteractive = [Environment]::UserInteractive
$isAdministrator = Get-IsAdministrator

Write-Host "UI E2E session check"
Write-Host "user=$($identity.Name)"
Write-Host "sessionId=$sessionId"
Write-Host "userInteractive=$isInteractive"
Write-Host "administrator=$isAdministrator"

try {
    Write-Host "query user:"
    query user
}
catch {
    Write-Warning "query user failed: $($_.Exception.Message)"
}

if (-not $isInteractive) {
    throw "GitHub runner is not running in an interactive desktop session."
}

if ($sessionId -eq 0) {
    throw "GitHub runner is running in session 0. Start the runner with run.cmd in the logged-in Cloud PC desktop, not as a service."
}

if ($identity.Name -match "^(NT AUTHORITY\\SYSTEM|LocalSystem)$") {
    throw "GitHub runner is running as LocalSystem. UI E2E must run as the logged-in desktop user."
}

Write-Host "UI E2E session looks usable."
