using WindowsEasyEmoji.App.Tray;
using WindowsEasyEmoji.Platform.Clipboard;
using WindowsEasyEmoji.Platform.Keyboard;
using WindowsEasyEmoji.Platform.Settings;
using WindowsEasyEmoji.Platform.Windows;

namespace WindowsEasyEmoji.App;

public partial class App : System.Windows.Application
{
    private Mutex? singleInstanceMutex;
    private SettingsStore? settingsStore;
    private AppSettings settings = AppSettings.Default;
    private MainWindow? overlayWindow;
    private SettingsWindow? settingsWindow;
    private TrayAppHost? trayAppHost;
    private KeyboardHookService? keyboardHookService;
    private HotkeyService? hotkeyService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: "Local\\WindowsEasyEmoji.App",
            createdNew: out var createdNew);
        if (!createdNew)
        {
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

        var foregroundWindowService = new ForegroundWindowService();
        var pasteCoordinator = new PasteCoordinator(foregroundWindowService, new ClipboardPasteService());

        overlayWindow = new MainWindow(pasteCoordinator, CreatePasteOptions(settings));
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
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        DisposeShortcuts();
        trayAppHost?.Dispose();
        singleInstanceMutex?.ReleaseMutex();
        singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void ApplySettings(AppSettings nextSettings)
    {
        settings = AppSettingsValidator.Normalize(nextSettings);
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

        if (settings.ReplaceWinPeriod)
        {
            var service = new KeyboardHookService();
            service.WinPeriodPressed += ShowOverlayFromShortcut;

            try
            {
                service.Start();
                keyboardHookService = service;
            }
            catch (InvalidOperationException)
            {
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
                service.RegisterFallbackHotkey();
            }

            if (service.IsRegistered)
            {
                hotkeyService = service;
            }
            else
            {
                service.HotkeyPressed -= ShowOverlayFromShortcut;
                service.Dispose();
            }
        }
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
        Dispatcher.BeginInvoke(() => trayAppHost?.ShowOverlay());
    }

    private static PasteOptions CreatePasteOptions(AppSettings settings)
    {
        return new PasteOptions(
            AutoPaste: settings.AutoPaste,
            RestoreOriginalClipboard: settings.RestoreClipboardAfterPaste);
    }
}
