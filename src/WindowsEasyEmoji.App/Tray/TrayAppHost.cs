using System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace WindowsEasyEmoji.App.Tray;

public sealed class TrayAppHost : IDisposable
{
    private readonly WpfApplication application;
    private readonly MainWindow overlayWindow;
    private NotifyIcon? notifyIcon;

    public TrayAppHost(WpfApplication application, MainWindow overlayWindow)
    {
        this.application = application;
        this.overlayWindow = overlayWindow;
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
        menu.Items.Add("Win + . 대체: ON");
        menu.Items.Add("자동 붙여넣기: ON");
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("설정", null, (_, _) => ShowOverlay());
        menu.Items.Add("종료", null, (_, _) => application.Shutdown());
        return menu;
    }

    private void ShowOverlay()
    {
        if (!overlayWindow.IsVisible)
        {
            overlayWindow.Show();
        }

        overlayWindow.Activate();
        overlayWindow.FocusSearchBox();
    }
}
