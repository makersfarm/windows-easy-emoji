using System.Runtime.InteropServices;
using WindowsEasyEmoji.Platform.Diagnostics;

namespace WindowsEasyEmoji.Platform.Windows;

public sealed class ForegroundWindowService : IForegroundWindowService
{
    private const int RestoreWindow = 9;
    private static readonly TimeSpan ForegroundWaitTimeout = TimeSpan.FromMilliseconds(250);

    public IntPtr GetForegroundWindowHandle()
    {
        var handle = GetForegroundWindow();
        DiagnosticLog.Write($"foreground.get handle={DiagnosticLog.Handle(handle)}");
        return handle;
    }

    public bool TryActivateWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            DiagnosticLog.Write("foreground.activate target=0x0 result=False reason=zero-handle");
            return false;
        }

        if (GetForegroundWindow() == windowHandle)
        {
            DiagnosticLog.Write($"foreground.activate target={DiagnosticLog.Handle(windowHandle)} result=True reason=already-foreground");
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
            var setForegroundResult = SetForegroundWindow(windowHandle);
            DiagnosticLog.Write($"foreground.activate set-foreground target={DiagnosticLog.Handle(windowHandle)} result={setForegroundResult}");
        }
        finally
        {
            AttachThreadInputIfNeeded(currentThreadId, targetThreadId, attach: false);
            AttachThreadInputIfNeeded(currentThreadId, foregroundThreadId, attach: false);
        }

        var result = WaitUntilForeground(windowHandle);
        DiagnosticLog.Write($"foreground.activate target={DiagnosticLog.Handle(windowHandle)} result={result}");
        return result;
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
