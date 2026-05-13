[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-QueryUser {
    try {
        $output = query user 2>&1
        return [pscustomobject]@{
            Output = @($output)
            ExitCode = $LASTEXITCODE
        }
    }
    catch {
        return [pscustomobject]@{
            Output = @($_.Exception.Message)
            ExitCode = -1
        }
    }
}

function Get-QueryUserSessionState([string[]]$Output, [int]$SessionId) {
    foreach ($line in $Output) {
        $normalized = ($line -replace "^\s*>", "").Trim()
        if ($normalized -match "\s+$SessionId\s+(?<state>\S+)\s+") {
            return $Matches["state"]
        }
    }

    return $null
}

function Invoke-TsconToConsole([int]$SessionId) {
    $tsconPath = Join-Path $env:WINDIR "System32\tscon.exe"
    if (-not (Test-Path $tsconPath)) {
        Write-Warning "tscon.exe was not found at $tsconPath."
        return
    }

    Write-Host "tsconAttempt=sessionId=$SessionId dest=console"
    $output = & $tsconPath $SessionId /dest:console 2>&1
    $exitCode = $LASTEXITCODE
    foreach ($line in @($output)) {
        Write-Host "tscon: $line"
    }

    Write-Host "tsconExitCode=$exitCode"
    Start-Sleep -Seconds 3
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

$queryUserResult = Invoke-QueryUser
Write-Host "query user:"
foreach ($line in $queryUserResult.Output) {
    Write-Host $line
}

if ($queryUserResult.ExitCode -ne 0) {
    Write-Warning "query user exited with code $($queryUserResult.ExitCode). Continuing because the managed session checks passed independently."
}

$queryUserSessionState = Get-QueryUserSessionState $queryUserResult.Output $sessionId
if (-not [string]::IsNullOrWhiteSpace($queryUserSessionState)) {
    Write-Host "queryUserCurrentSessionState=$queryUserSessionState"
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

if ($queryUserSessionState -eq "Disc") {
    if ($isAdministrator) {
        Invoke-TsconToConsole $sessionId
    }
    else {
        Write-Warning "Current session is disconnected and runner is not administrator, so tscon recovery cannot be attempted."
    }
}

$artifactRoot = $env:WINDOWS_EASY_EMOJI_E2E_ARTIFACT_DIR
if ([string]::IsNullOrWhiteSpace($artifactRoot)) {
    $artifactRoot = Join-Path (Get-Location) "artifacts\ui-e2e"
}

$preflightDirectory = Join-Path $artifactRoot "preflight"
New-Item -ItemType Directory -Force -Path $preflightDirectory | Out-Null

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class UiE2ENative
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint inputCount, INPUT[] inputs, int inputSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mouse;

        [FieldOffset(0)]
        public KEYBDINPUT keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr extraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort virtualKey;
        public ushort scanCode;
        public uint flags;
        public uint time;
        public UIntPtr extraInfo;
    }
}
"@

function Convert-HandleToHex([IntPtr]$Handle) {
    return "0x{0:X}" -f $Handle.ToInt64()
}

function Save-DesktopScreenshot([string]$Path) {
    try {
        $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
        if ($bounds.Width -le 0 -or $bounds.Height -le 0) {
            return
        }

        $bitmap = [System.Drawing.Bitmap]::new($bounds.Width, $bounds.Height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $bounds.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()
    }
    catch {
        $_ | Out-String | Set-Content -Path "$Path.txt" -Encoding UTF8
    }
}

$initialForegroundWindow = [UiE2ENative]::GetForegroundWindow()
Write-Host ("initialForegroundWindow={0}" -f (Convert-HandleToHex $initialForegroundWindow))

$form = [System.Windows.Forms.Form]::new()
$form.Text = "Windows Easy Emoji UI E2E Preflight"
$form.Width = 520
$form.Height = 220
$form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
$form.TopMost = $true
$label = [System.Windows.Forms.Label]::new()
$label.Text = "Windows Easy Emoji UI E2E preflight"
$label.Dock = [System.Windows.Forms.DockStyle]::Fill
$label.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
$form.Controls.Add($label)

try {
    $form.Show()
    $form.Activate()
    [UiE2ENative]::ShowWindow($form.Handle, 5) | Out-Null
    [UiE2ENative]::BringWindowToTop($form.Handle) | Out-Null
    $setForegroundResult = [UiE2ENative]::SetForegroundWindow($form.Handle)
    [UiE2ENative]::SetActiveWindow($form.Handle) | Out-Null
    [UiE2ENative]::SetFocus($form.Handle) | Out-Null

    for ($index = 0; $index -lt 20; $index++) {
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 100
        if ([UiE2ENative]::GetForegroundWindow() -eq $form.Handle) {
            break
        }
    }

    $probeForegroundWindow = [UiE2ENative]::GetForegroundWindow()
    Save-DesktopScreenshot (Join-Path $preflightDirectory "desktop-preflight.png")

    $keyboardInputType = 1
    $keyUp = 0x0002
    $virtualKeyShift = 0x10
    $inputSize = [Runtime.InteropServices.Marshal]::SizeOf([type][UiE2ENative+INPUT])
    $inputs = [UiE2ENative+INPUT[]]::new(2)
    $inputs[0].type = $keyboardInputType
    $inputs[0].union.keyboard.virtualKey = $virtualKeyShift
    $inputs[1].type = $keyboardInputType
    $inputs[1].union.keyboard.virtualKey = $virtualKeyShift
    $inputs[1].union.keyboard.flags = $keyUp

    $sent = [UiE2ENative]::SendInput([uint32]$inputs.Length, $inputs, $inputSize)
    $sendInputError = [Runtime.InteropServices.Marshal]::GetLastWin32Error()

    $diagnostics = [ordered]@{
        user = $identity.Name
        sessionId = $sessionId
        userInteractive = $isInteractive
        administrator = $isAdministrator
        initialForegroundWindow = Convert-HandleToHex $initialForegroundWindow
        probeHandle = Convert-HandleToHex $form.Handle
        setForegroundResult = $setForegroundResult
        probeForegroundWindow = Convert-HandleToHex $probeForegroundWindow
        sendInputSmokeSent = $sent
        sendInputSmokeExpected = $inputs.Length
        sendInputSmokeError = $sendInputError
        virtualScreen = @{
            left = [System.Windows.Forms.SystemInformation]::VirtualScreen.Left
            top = [System.Windows.Forms.SystemInformation]::VirtualScreen.Top
            width = [System.Windows.Forms.SystemInformation]::VirtualScreen.Width
            height = [System.Windows.Forms.SystemInformation]::VirtualScreen.Height
        }
    }

    $diagnostics | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $preflightDirectory "preflight.json") -Encoding UTF8

    Write-Host ("probeHandle={0}" -f (Convert-HandleToHex $form.Handle))
    Write-Host "setForegroundResult=$setForegroundResult"
    Write-Host ("probeForegroundWindow={0}" -f (Convert-HandleToHex $probeForegroundWindow))
    Write-Host "sendInputSmokeSent=$sent"
    Write-Host "sendInputSmokeError=$sendInputError"

    if ($probeForegroundWindow -ne $form.Handle) {
        throw "The runner cannot foreground its own probe window. Open the Cloud PC desktop, unlock it, keep the RDP/Windows App window visible, and start the runner with run.cmd in that desktop session."
    }

    if ($sent -ne $inputs.Length) {
        throw "SendInput smoke test failed. Error $sendInputError. Reconnect to the Cloud PC desktop, unlock it, keep the RDP/Windows App window visible, and run the runner with run.cmd instead of as a service."
    }
}
finally {
    $form.Close()
    $form.Dispose()
}

Write-Host "UI E2E session looks usable."
$global:LASTEXITCODE = 0
