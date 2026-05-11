using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;
using WindowsEasyEmoji.Platform.Diagnostics;
using WindowsEasyEmoji.Platform.Settings;
using WindowsEasyEmoji.Platform.Windows;
using WpfApplication = System.Windows.Application;

namespace WindowsEasyEmoji.App.Tray;

public sealed class TrayAppHost : IDisposable
{
    private const int ShowWindowShow = 5;

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
    public event EventHandler? SettingsWindowRequested;

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
        DiagnosticLog.Write("tray.start complete");
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
        menu.Items.Add("설정...", null, (_, _) => SettingsWindowRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
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
        DiagnosticLog.Write($"tray.show-overlay begin visible={overlayWindow.IsVisible}");
        var targetWindowHandle = foregroundWindowService.GetForegroundWindowHandle();
        overlayWindow.RememberTargetWindow(targetWindowHandle);
        DiagnosticLog.Write($"tray.show-overlay target={DiagnosticLog.Handle(targetWindowHandle)}");

        if (!overlayWindow.IsVisible)
        {
            overlayWindow.Show();
            DiagnosticLog.Write("tray.show-overlay window-shown");
        }

        var activated = ActivateOverlayWindow();
        DiagnosticLog.Write($"tray.show-overlay activate-result={activated}");
        overlayWindow.FocusSearchBox();
        DiagnosticLog.Write("tray.show-overlay complete");
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

    private bool ActivateOverlayWindow()
    {
        var wpfActivated = overlayWindow.Activate();
        var handle = new WindowInteropHelper(overlayWindow).Handle;
        var forcedActivated = TryForceForegroundWindow(handle);
        return wpfActivated || forcedActivated;
    }

    private static bool TryForceForegroundWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            DiagnosticLog.Write("tray.activate-overlay skipped-zero-handle");
            return false;
        }

        var currentThreadId = GetCurrentThreadId();
        var foregroundWindow = GetForegroundWindow();
        var foregroundThreadId = foregroundWindow == IntPtr.Zero
            ? 0
            : GetWindowThreadProcessId(foregroundWindow, out _);
        var attached = false;

        try
        {
            if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
            {
                attached = AttachThreadInput(currentThreadId, foregroundThreadId, attach: true);
            }

            ShowWindow(windowHandle, ShowWindowShow);
            BringWindowToTop(windowHandle);
            var setForegroundResult = SetForegroundWindow(windowHandle);
            SetActiveWindow(windowHandle);
            SetFocus(windowHandle);
            var foregroundAfter = GetForegroundWindow();
            var activated = setForegroundResult || foregroundAfter == windowHandle;

            DiagnosticLog.Write(
                $"tray.activate-overlay handle={DiagnosticLog.Handle(windowHandle)} foregroundBefore={DiagnosticLog.Handle(foregroundWindow)} foregroundAfter={DiagnosticLog.Handle(foregroundAfter)} attached={attached} setForeground={setForegroundResult} activated={activated}");
            return activated;
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, attach: false);
            }
        }
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);
}
