using System.Runtime.InteropServices;

namespace WindowsEasyEmoji.Platform.Windows;

public sealed class ForegroundWindowService : IForegroundWindowService
{
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

        return SetForegroundWindow(windowHandle);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
