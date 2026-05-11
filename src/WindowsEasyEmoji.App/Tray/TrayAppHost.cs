using System.Windows.Forms;
using WindowsEasyEmoji.Platform.Settings;
using WindowsEasyEmoji.Platform.Windows;
using WpfApplication = System.Windows.Application;

namespace WindowsEasyEmoji.App.Tray;

public sealed class TrayAppHost : IDisposable
{
    private readonly WpfApplication application;
    private readonly MainWindow overlayWindow;
    private readonly IForegroundWindowService foregroundWindowService;
    private readonly AppSettings settings;
    private NotifyIcon? notifyIcon;

    public TrayAppHost(
        WpfApplication application,
        MainWindow overlayWindow,
        IForegroundWindowService foregroundWindowService,
        AppSettings settings)
    {
        this.application = application;
        this.overlayWindow = overlayWindow;
        this.foregroundWindowService = foregroundWindowService;
        this.settings = settings;
    }

    public void Start()
    {
        notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Windows Easy Emoji",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        notifyIcon.DoubleClick += (_, _) => ShowOverlay();
    }

    public void Dispose()
    {
        if (notifyIcon is null)
        {
            return;
        }

        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        notifyIcon = null;
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Emoji Search 열기", null, (_, _) => ShowOverlay());
        menu.Items.Add($"Win + . 대체: {FormatOnOff(settings.ReplaceWinPeriod)}");
        menu.Items.Add($"Fallback: {settings.FallbackHotkey}");
        menu.Items.Add($"자동 붙여넣기: {FormatOnOff(settings.AutoPaste)}");
        menu.Items.Add($"클립보드 복원: {FormatOnOff(settings.RestoreClipboardAfterPaste)}");
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("설정", null, (_, _) => ShowOverlay());
        menu.Items.Add("종료", null, (_, _) => application.Shutdown());
        return menu;
    }

    public void ShowOverlay()
    {
        var targetWindowHandle = foregroundWindowService.GetForegroundWindowHandle();
        overlayWindow.RememberTargetWindow(targetWindowHandle);

        if (!overlayWindow.IsVisible)
        {
            overlayWindow.Show();
        }

        overlayWindow.Activate();
        overlayWindow.FocusSearchBox();
    }

    private static string FormatOnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }
}
