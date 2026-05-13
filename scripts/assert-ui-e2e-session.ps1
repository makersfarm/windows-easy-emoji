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
    $queryUserExitCode = $LASTEXITCODE
    if ($queryUserExitCode -ne 0) {
        Write-Warning "query user exited with code $queryUserExitCode. Continuing because the managed session checks passed independently."
    }
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

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class UiE2ENative
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

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
        public KEYBDINPUT keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort virtualKey;
        public ushort scanCode;
        public uint flags;
        public uint time;
        public IntPtr extraInfo;
    }
}
"@

$foregroundWindow = [UiE2ENative]::GetForegroundWindow()
Write-Host ("foregroundWindow=0x{0:X}" -f $foregroundWindow.ToInt64())

if ($foregroundWindow -eq [IntPtr]::Zero) {
    throw "No foreground window is available. Open the Cloud PC desktop and keep it unlocked/visible before running UI E2E."
}

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
Write-Host "sendInputSmokeSent=$sent"
Write-Host "sendInputSmokeError=$sendInputError"

if ($sent -ne $inputs.Length) {
    throw "SendInput smoke test failed. Error $sendInputError. Reconnect to the Cloud PC desktop, unlock it, keep the RDP/Windows App window visible, and run the runner with run.cmd instead of as a service."
}

Write-Host "UI E2E session looks usable."
$global:LASTEXITCODE = 0
