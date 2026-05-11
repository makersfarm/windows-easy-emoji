using WindowsEasyEmoji.App.Tray;
using WindowsEasyEmoji.Platform.Clipboard;
using WindowsEasyEmoji.Platform.Keyboard;
using WindowsEasyEmoji.Platform.Settings;
using WindowsEasyEmoji.Platform.Windows;

namespace WindowsEasyEmoji.App;

public partial class App : System.Windows.Application
{
    private TrayAppHost? trayAppHost;
    private KeyboardHookService? keyboardHookService;
    private HotkeyService? hotkeyService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

        var settingsStore = SettingsStore.CreateDefault();
        var settings = settingsStore.Load();
        settingsStore.Save(settings);

        var foregroundWindowService = new ForegroundWindowService();
        var pasteCoordinator = new PasteCoordinator(foregroundWindowService, new ClipboardPasteService());
        var pasteOptions = new PasteOptions(
            AutoPaste: settings.AutoPaste,
            RestoreOriginalClipboard: settings.RestoreClipboardAfterPaste);

        var overlayWindow = new MainWindow(pasteCoordinator, pasteOptions);
        trayAppHost = new TrayAppHost(this, overlayWindow, foregroundWindowService, settings);
        trayAppHost.Start();

        ConfigureShortcuts(settings);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        keyboardHookService?.Dispose();
        hotkeyService?.Dispose();
        trayAppHost?.Dispose();
        base.OnExit(e);
    }

    private void ConfigureShortcuts(AppSettings settings)
    {
        if (settings.ReplaceWinPeriod)
        {
            keyboardHookService = new KeyboardHookService();
            keyboardHookService.WinPeriodPressed += ShowOverlayFromShortcut;

            try
            {
                keyboardHookService.Start();
            }
            catch (InvalidOperationException)
            {
                keyboardHookService.Dispose();
                keyboardHookService = null;
            }
        }

        if (settings.RegisterFallbackHotkey)
        {
            hotkeyService = new HotkeyService();
            hotkeyService.HotkeyPressed += ShowOverlayFromShortcut;

            try
            {
                hotkeyService.RegisterFallbackHotkey(HotkeyGesture.Parse(settings.FallbackHotkey));
                if (!hotkeyService.IsRegistered &&
                    !settings.FallbackHotkey.Equals(AppSettings.Default.FallbackHotkey, StringComparison.OrdinalIgnoreCase))
                {
                    hotkeyService.RegisterFallbackHotkey();
                }
            }
            catch (ArgumentException)
            {
                hotkeyService.RegisterFallbackHotkey();
            }
        }
    }

    private void ShowOverlayFromShortcut(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() => trayAppHost?.ShowOverlay());
    }
}
