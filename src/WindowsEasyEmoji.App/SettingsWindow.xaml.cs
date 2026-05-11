using System.Diagnostics;
using System.Windows;
using WindowsEasyEmoji.Platform.Settings;

namespace WindowsEasyEmoji.App;

public partial class SettingsWindow : Window
{
    private readonly string settingsPath;

    public SettingsWindow(AppSettings settings, string settingsPath)
    {
        InitializeComponent();
        this.settingsPath = settingsPath;
        Settings = settings;
        LoadSettings(settings);
    }

    public AppSettings Settings { get; private set; }

    private void LoadSettings(AppSettings settings)
    {
        ReplaceWinPeriodCheckBox.IsChecked = settings.ReplaceWinPeriod;
        FallbackHotkeyEnabledCheckBox.IsChecked = settings.RegisterFallbackHotkey;
        FallbackHotkeyTextBox.Text = settings.FallbackHotkey;
        AutoPasteCheckBox.IsChecked = settings.AutoPaste;
        RestoreClipboardCheckBox.IsChecked = settings.RestoreClipboardAfterPaste;
        RefreshControlStates();
        ErrorText.Text = string.Empty;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var nextSettings = AppSettingsValidator.Normalize(new AppSettings(
            ReplaceWinPeriod: ReplaceWinPeriodCheckBox.IsChecked == true,
            AutoPaste: AutoPasteCheckBox.IsChecked == true,
            RegisterFallbackHotkey: FallbackHotkeyEnabledCheckBox.IsChecked == true,
            RestoreClipboardAfterPaste: RestoreClipboardCheckBox.IsChecked == true,
            FallbackHotkey: FallbackHotkeyTextBox.Text));

        if (!AppSettingsValidator.TryValidate(nextSettings, out var errorMessage))
        {
            ErrorText.Text = errorMessage;
            FallbackHotkeyTextBox.Focus();
            FallbackHotkeyTextBox.SelectAll();
            return;
        }

        Settings = nextSettings;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        LoadSettings(AppSettings.Default);
    }

    private void OpenSettingsFileButton_Click(object sender, RoutedEventArgs e)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "notepad.exe",
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(settingsPath);
        Process.Start(startInfo);
    }

    private void FallbackHotkeyEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        RefreshControlStates();
    }

    private void AutoPasteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        RefreshControlStates();
    }

    private void RefreshControlStates()
    {
        FallbackHotkeyTextBox.IsEnabled = FallbackHotkeyEnabledCheckBox.IsChecked == true;
        RestoreClipboardCheckBox.IsEnabled = AutoPasteCheckBox.IsChecked == true;
    }
}
