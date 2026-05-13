using WindowsEasyEmoji.App.Tray;
using WindowsEasyEmoji.Platform.Clipboard;
using WindowsEasyEmoji.Platform.Diagnostics;
using WindowsEasyEmoji.Platform.Keyboard;
using WindowsEasyEmoji.Platform.Settings;
using WindowsEasyEmoji.Platform.UserState;
using WindowsEasyEmoji.Platform.Windows;

namespace WindowsEasyEmoji.App;

public partial class App : System.Windows.Application
{
    private Mutex? singleInstanceMutex;
    private SettingsStore? settingsStore;
    private UserEmojiStateStore? userEmojiStateStore;
    private AppSettings settings = AppSettings.Default;
    private MainWindow? overlayWindow;
    private SettingsWindow? settingsWindow;
    private TrayAppHost? trayAppHost;
    private KeyboardHookService? keyboardHookService;
    private HotkeyService? hotkeyService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        DiagnosticLog.Write("app.startup begin");
        singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: "Local\\WindowsEasyEmoji.App",
            createdNew: out var createdNew);
        if (!createdNew)
        {
            DiagnosticLog.Write("app.startup duplicate-instance");
            singleInstanceMutex.Dispose();
            singleInstanceMutex = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);

        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

        settingsStore = SettingsStore.CreateDefault();
        settings = settingsStore.Load();
        settingsStore.Save(settings);
        userEmojiStateStore = UserEmojiStateStore.CreateDefault();
        var userEmojiStateById = userEmojiStateStore.Load();
        DiagnosticLog.Write(
            $"app.settings replaceWinPeriod={settings.ReplaceWinPeriod} autoPaste={settings.AutoPaste} fallback={settings.RegisterFallbackHotkey} restore={settings.RestoreClipboardAfterPaste} fallbackHotkey={settings.FallbackHotkey}");
        DiagnosticLog.Write($"app.user-state loaded count={userEmojiStateById.Count} path={userEmojiStateStore.StatePath}");

        var foregroundWindowService = new ForegroundWindowService();
        var pasteCoordinator = new PasteCoordinator(foregroundWindowService, new ClipboardPasteService());

        overlayWindow = new MainWindow(
            pasteCoordinator,
            CreatePasteOptions(settings),
            userEmojiStateById,
            userEmojiStateStore);
        trayAppHost = new TrayAppHost(
            this,
            overlayWindow,
            foregroundWindowService,
            settings,
            settingsStore.SettingsPath);
        trayAppHost.SettingsChangeRequested += (_, nextSettings) => ApplySettings(nextSettings);
        trayAppHost.SettingsWindowRequested += (_, _) => ShowSettingsWindow();
        trayAppHost.Start();

        ConfigureShortcuts(settings);
        DiagnosticLog.Write("app.startup complete");
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        DiagnosticLog.Write("app.exit begin");
        DisposeShortcuts();
        trayAppHost?.Dispose();
        singleInstanceMutex?.ReleaseMutex();
        singleInstanceMutex?.Dispose();
        base.OnExit(e);
        DiagnosticLog.Write("app.exit complete");
    }

    private void ApplySettings(AppSettings nextSettings)
    {
        settings = AppSettingsValidator.Normalize(nextSettings);
        DiagnosticLog.Write(
            $"app.apply-settings replaceWinPeriod={settings.ReplaceWinPeriod} autoPaste={settings.AutoPaste} fallback={settings.RegisterFallbackHotkey} restore={settings.RestoreClipboardAfterPaste} fallbackHotkey={settings.FallbackHotkey}");
        settingsStore?.Save(settings);
        overlayWindow?.UpdatePasteOptions(CreatePasteOptions(settings));
        trayAppHost?.UpdateSettings(settings);
        ConfigureShortcuts(settings);
    }

    private void ShowSettingsWindow()
    {
        if (settingsStore is null)
        {
            return;
        }

        if (settingsWindow is { IsVisible: true })
        {
            settingsWindow.Activate();
            return;
        }

        settingsWindow = new SettingsWindow(settings, settingsStore.SettingsPath);
        var result = settingsWindow.ShowDialog();
        if (result == true)
        {
            ApplySettings(settingsWindow.Settings);
        }

        settingsWindow = null;
    }

    private void ConfigureShortcuts(AppSettings settings)
    {
        DisposeShortcuts();
        DiagnosticLog.Write("app.configure-shortcuts begin");

        if (settings.ReplaceWinPeriod)
        {
            var service = new KeyboardHookService();
            service.WinPeriodPressed += ShowOverlayFromShortcut;

            try
            {
                service.Start();
                keyboardHookService = service;
                DiagnosticLog.Write("app.configure-shortcuts keyboard-hook-enabled");
            }
            catch (InvalidOperationException)
            {
                DiagnosticLog.Write("app.configure-shortcuts keyboard-hook-failed");
                service.WinPeriodPressed -= ShowOverlayFromShortcut;
                service.Dispose();
            }
        }

        if (settings.RegisterFallbackHotkey)
        {
            var service = new HotkeyService();
            service.HotkeyPressed += ShowOverlayFromShortcut;

            try
            {
                service.RegisterFallbackHotkey(HotkeyGesture.Parse(settings.FallbackHotkey));
                if (!service.IsRegistered &&
                    !settings.FallbackHotkey.Equals(AppSettings.Default.FallbackHotkey, StringComparison.OrdinalIgnoreCase))
                {
                    service.RegisterFallbackHotkey();
                }
            }
            catch (ArgumentException)
            {
                DiagnosticLog.Write("app.configure-shortcuts fallback-parse-failed");
                service.RegisterFallbackHotkey();
            }

            if (service.IsRegistered)
            {
                hotkeyService = service;
                DiagnosticLog.Write("app.configure-shortcuts fallback-enabled");
            }
            else
            {
                DiagnosticLog.Write("app.configure-shortcuts fallback-failed");
                service.HotkeyPressed -= ShowOverlayFromShortcut;
                service.Dispose();
            }
        }
        DiagnosticLog.Write("app.configure-shortcuts complete");
    }

    private void DisposeShortcuts()
    {
        if (keyboardHookService is not null)
        {
            keyboardHookService.WinPeriodPressed -= ShowOverlayFromShortcut;
            keyboardHookService.Dispose();
            keyboardHookService = null;
        }

        if (hotkeyService is not null)
        {
            hotkeyService.HotkeyPressed -= ShowOverlayFromShortcut;
            hotkeyService.Dispose();
            hotkeyService = null;
        }
    }

    private void ShowOverlayFromShortcut(object? sender, EventArgs e)
    {
        DiagnosticLog.Write($"app.shortcut-dispatch sender={sender?.GetType().Name ?? "unknown"}");
        Dispatcher.BeginInvoke(() => trayAppHost?.ShowOverlay());
    }

    private static PasteOptions CreatePasteOptions(AppSettings settings)
    {
        return new PasteOptions(
            AutoPaste: settings.AutoPaste,
            RestoreOriginalClipboard: settings.RestoreClipboardAfterPaste);
    }
}
