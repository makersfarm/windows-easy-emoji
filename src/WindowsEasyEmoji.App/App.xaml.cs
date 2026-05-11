using WindowsEasyEmoji.App.Tray;

namespace WindowsEasyEmoji.App;

public partial class App : System.Windows.Application
{
    private TrayAppHost? trayAppHost;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

        var overlayWindow = new MainWindow();
        trayAppHost = new TrayAppHost(this, overlayWindow);
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
