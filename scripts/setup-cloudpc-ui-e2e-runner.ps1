[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunnerToken,

    [string]$RepoUrl = "https://github.com/makersfarm",
    [string]$RunnerRoot = "C:\actions-runner",
    [string]$RunnerName = "$env:COMPUTERNAME-ui-e2e",
    [string]$Labels = "self-hosted,windows,ui-e2e,cloudpc"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message"
}

function Get-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-RunnerAsset {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/actions/runner/releases/latest" -Headers @{
        "User-Agent" = "windows-easy-emoji-runner-setup"
    }

    $asset = $release.assets |
        Where-Object { $_.name -like "actions-runner-win-x64-*.zip" } |
        Select-Object -First 1

    if ($null -eq $asset) {
        throw "Could not find a Windows x64 GitHub Actions runner asset in the latest release."
    }

    [pscustomobject]@{
        Version = $release.tag_name
        Name = $asset.name
        Url = $asset.browser_download_url
    }
}

Write-Step "Checking Cloud PC runner prerequisites"
$isAdministrator = Get-IsAdministrator
Write-Host "administrator=$isAdministrator"
Write-Host "sessionId=$([Diagnostics.Process]::GetCurrentProcess().SessionId)"
Write-Host "userInteractive=$([Environment]::UserInteractive)"

if (-not $isAdministrator) {
    Write-Warning "This PowerShell is not elevated. Initial setup can still work, but power/session settings may fail. If Administrators shows 'deny only', open PowerShell with Run as administrator or make this Cloud PC user a Local Administrator."
}

Write-Step "Reducing sleep and lock interference for UI E2E"
try {
    powercfg /change monitor-timeout-ac 0
    powercfg /change standby-timeout-ac 0
    powercfg /change hibernate-timeout-ac 0
}
catch {
    Write-Warning "powercfg failed: $($_.Exception.Message)"
}

try {
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name ScreenSaveActive -Value "0"
}
catch {
    Write-Warning "Could not disable the current user's screen saver: $($_.Exception.Message)"
}

Write-Step "Preparing runner directory"
New-Item -ItemType Directory -Force -Path $RunnerRoot | Out-Null

$runnerConfigPath = Join-Path $RunnerRoot ".runner"
if (Test-Path $runnerConfigPath) {
    Write-Host "Runner is already configured in $RunnerRoot."
    Write-Host "To reconfigure it, remove the runner in GitHub first or use GitHub's runner remove token with config.cmd remove."
    return
}

Push-Location $RunnerRoot
try {
    $runnerAsset = Get-RunnerAsset
    $zipPath = Join-Path $RunnerRoot $runnerAsset.Name

    Write-Step "Downloading GitHub Actions runner $($runnerAsset.Version)"
    Invoke-WebRequest -Uri $runnerAsset.Url -OutFile $zipPath

    Write-Step "Extracting runner"
    Expand-Archive -Path $zipPath -DestinationPath $RunnerRoot -Force
    Remove-Item -Path $zipPath -Force

    Write-Step "Configuring runner"
    & .\config.cmd `
        --unattended `
        --url $RepoUrl `
        --token $RunnerToken `
        --name $RunnerName `
        --labels $Labels `
        --work "_work" `
        --replace
}
finally {
    Pop-Location
}

Write-Step "Runner configured"
$startScript = Join-Path $PSScriptRoot "start-cloudpc-ui-e2e-runner.ps1"
Write-Host "Start it from the logged-in Cloud PC desktop with:"
Write-Host "  & `"$startScript`" -RunnerRoot `"$RunnerRoot`""
Write-Host ""
Write-Host "Do not install this runner as a Windows service for UI E2E. Service/session 0 cannot drive foreground windows, SendInput, tray UI, or clipboard paste reliably."
