using System.Diagnostics;
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
    private readonly string settingsPath;
    private AppSettings settings;
    private NotifyIcon? notifyIcon;

    public TrayAppHost(
        WpfApplication application,
        MainWindow overlayWindow,
        IForegroundWindowService foregroundWindowService,
        AppSettings settings,
        string settingsPath)
    {
        this.application = application;
        this.overlayWindow = overlayWindow;
        this.foregroundWindowService = foregroundWindowService;
        this.settings = settings;
        this.settingsPath = settingsPath;
    }

    public event EventHandler<AppSettings>? SettingsChangeRequested;

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

    public void UpdateSettings(AppSettings settings)
    {
        this.settings = settings;
        RefreshMenu();
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
        menu.Items.Add(CreateToggleItem(
            "Win + . 대체",
            settings.ReplaceWinPeriod,
            value => settings with { ReplaceWinPeriod = value }));
        menu.Items.Add(CreateToggleItem(
            $"Fallback hotkey 사용 ({settings.FallbackHotkey})",
            settings.RegisterFallbackHotkey,
            value => settings with { RegisterFallbackHotkey = value }));
        menu.Items.Add(CreateToggleItem(
            "자동 붙여넣기",
            settings.AutoPaste,
            value => settings with { AutoPaste = value }));
        menu.Items.Add(CreateToggleItem(
            "클립보드 원본 복원",
            settings.RestoreClipboardAfterPaste,
            value => settings with { RestoreClipboardAfterPaste = value }));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("설정 파일 열기", null, (_, _) => OpenSettingsFile());
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

    private ToolStripMenuItem CreateToggleItem(
        string text,
        bool isChecked,
        Func<bool, AppSettings> update)
    {
        var item = new ToolStripMenuItem(text)
        {
            Checked = isChecked,
            CheckOnClick = false
        };

        item.Click += (_, _) => SettingsChangeRequested?.Invoke(this, update(!isChecked));
        return item;
    }

    private void RefreshMenu()
    {
        if (notifyIcon is null)
        {
            return;
        }

        var oldMenu = notifyIcon.ContextMenuStrip;
        notifyIcon.ContextMenuStrip = BuildMenu();
        oldMenu?.Dispose();
    }

    private void OpenSettingsFile()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "notepad.exe",
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(settingsPath);
        Process.Start(startInfo);
    }
}
