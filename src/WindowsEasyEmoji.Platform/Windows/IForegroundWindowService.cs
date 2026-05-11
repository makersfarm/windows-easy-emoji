namespace WindowsEasyEmoji.Platform.Windows;

public interface IForegroundWindowService
{
    IntPtr GetForegroundWindowHandle();

    bool TryActivateWindow(IntPtr windowHandle);
}
