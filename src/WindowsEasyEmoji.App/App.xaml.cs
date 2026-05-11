using WindowsEasyEmoji.App.Tray;
using WindowsEasyEmoji.Platform.Clipboard;
using WindowsEasyEmoji.Platform.Windows;

namespace WindowsEasyEmoji.App;

public partial class App : System.Windows.Application
{
    private TrayAppHost? trayAppHost;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

        var foregroundWindowService = new ForegroundWindowService();
        var pasteCoordinator = new PasteCoordinator(foregroundWindowService, new ClipboardPasteService());
        var initialTargetWindowHandle = foregroundWindowService.GetForegroundWindowHandle();
        var overlayWindow = new MainWindow(pasteCoordinator);
        overlayWindow.RememberTargetWindow(initialTargetWindowHandle);
        trayAppHost = new TrayAppHost(this, overlayWindow, foregroundWindowService);
        trayAppHost.Start();

        overlayWindow.Show();
        overlayWindow.FocusSearchBox();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        trayAppHost?.Dispose();
        base.OnExit(e);
    }
}
