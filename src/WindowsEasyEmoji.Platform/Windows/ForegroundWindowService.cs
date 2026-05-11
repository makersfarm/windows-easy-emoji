using System.Runtime.InteropServices;

namespace WindowsEasyEmoji.Platform.Windows;

public sealed class ForegroundWindowService : IForegroundWindowService
{
    private const int RestoreWindow = 9;
    private static readonly TimeSpan ForegroundWaitTimeout = TimeSpan.FromMilliseconds(250);

    public IntPtr GetForegroundWindowHandle()
    {
        return GetForegroundWindow();
    }

    public bool TryActivateWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        if (GetForegroundWindow() == windowHandle)
        {
            return true;
        }

        if (IsIconic(windowHandle))
        {
            ShowWindow(windowHandle, RestoreWindow);
        }

        var currentForegroundWindow = GetForegroundWindow();
        var currentThreadId = GetCurrentThreadId();
        var foregroundThreadId = GetWindowThreadProcessId(currentForegroundWindow, out _);
        var targetThreadId = GetWindowThreadProcessId(windowHandle, out _);

        AttachThreadInputIfNeeded(currentThreadId, foregroundThreadId, attach: true);
        AttachThreadInputIfNeeded(currentThreadId, targetThreadId, attach: true);

        try
        {
            BringWindowToTop(windowHandle);
            SetForegroundWindow(windowHandle);
        }
        finally
        {
            AttachThreadInputIfNeeded(currentThreadId, targetThreadId, attach: false);
            AttachThreadInputIfNeeded(currentThreadId, foregroundThreadId, attach: false);
        }

        return WaitUntilForeground(windowHandle);
    }

    private static void AttachThreadInputIfNeeded(uint sourceThreadId, uint targetThreadId, bool attach)
    {
        if (targetThreadId != 0 && targetThreadId != sourceThreadId)
        {
            AttachThreadInput(sourceThreadId, targetThreadId, attach);
        }
    }

    private static bool WaitUntilForeground(IntPtr windowHandle)
    {
        var deadline = DateTimeOffset.UtcNow + ForegroundWaitTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (GetForegroundWindow() == windowHandle)
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return GetForegroundWindow() == windowHandle;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
