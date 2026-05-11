using System.Runtime.InteropServices;
using WindowsEasyEmoji.Platform.Diagnostics;

namespace WindowsEasyEmoji.Platform.Windows;

public sealed class ForegroundWindowService : IForegroundWindowService
{
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
            return false;
        }

        var result = SetForegroundWindow(windowHandle);
        DiagnosticLog.Write($"foreground.activate target={DiagnosticLog.Handle(windowHandle)} result={result}");
        return result;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
