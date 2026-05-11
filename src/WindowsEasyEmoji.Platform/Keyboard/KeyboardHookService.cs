using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowsEasyEmoji.Platform.Keyboard;

public sealed class KeyboardHookService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int KeyDownMask = unchecked((short)0x8000);

    private readonly LowLevelKeyboardProc hookProc;
    private readonly KeyboardShortcutDetector detector;
    private IntPtr hookHandle;

    public KeyboardHookService()
    {
        hookProc = HookCallback;
        detector = new KeyboardShortcutDetector(IsKeyDown);
    }

    public bool IsRunning { get; private set; }

    public event EventHandler? WinPeriodPressed;

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = module is null ? IntPtr.Zero : GetModuleHandle(module.ModuleName);
        hookHandle = SetWindowsHookEx(WhKeyboardLl, hookProc, moduleHandle, 0);
        if (hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to install keyboard hook. Win32 error: {Marshal.GetLastWin32Error()}");
        }

        IsRunning = true;
    }

    public void Stop()
    {
        if (hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hookHandle);
            hookHandle = IntPtr.Zero;
        }

        IsRunning = false;
    }

    public void Dispose()
    {
        Stop();
    }

    internal void RaiseWinPeriodPressedForTesting()
    {
        WinPeriodPressed?.Invoke(this, EventArgs.Empty);
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var keyboardData = Marshal.PtrToStructure<KeyboardData>(lParam);
            var keyEvent = new LowLevelKeyboardEvent(wParam.ToInt32(), (int)keyboardData.VirtualKey);
            if (detector.ShouldHandleWinPeriod(keyEvent))
            {
                WinPeriodPressed?.Invoke(this, EventArgs.Empty);
                return new IntPtr(1);
            }
        }

        return CallNextHookEx(hookHandle, code, wParam, lParam);
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return (GetKeyState(virtualKey) & KeyDownMask) != 0;
    }

    private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardData
    {
        public uint VirtualKey;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hMod, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hookHandle, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
