using WindowsEasyEmoji.Platform.Settings;

namespace WindowsEasyEmoji.Platform.Tests.Settings;

public sealed class SettingsStoreTests
{
    [Fact]
    public void Load_returns_default_settings_when_file_does_not_exist()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json");
        var store = new SettingsStore(path);

        var settings = store.Load();

        Assert.True(settings.ReplaceWinPeriod);
        Assert.True(settings.AutoPaste);
        Assert.True(settings.RegisterFallbackHotkey);
        Assert.False(settings.RestoreClipboardAfterPaste);
        Assert.Equal("Ctrl+Alt+Space", settings.FallbackHotkey);
    }

    [Fact]
    public void Save_persists_settings_for_next_load()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        var store = new SettingsStore(path);
        var saved = new AppSettings(
            ReplaceWinPeriod: false,
            AutoPaste: false,
            RegisterFallbackHotkey: true,
            RestoreClipboardAfterPaste: true,
            FallbackHotkey: "Ctrl+Alt+E");

        store.Save(saved);
        var loaded = store.Load();

        Assert.Equal(saved, loaded);
    }

    [Fact]
    public void Load_migrates_reserved_old_default_fallback_hotkey()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        var store = new SettingsStore(path);
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, """
            {
              "ReplaceWinPeriod": true,
              "AutoPaste": true,
              "RegisterFallbackHotkey": true,
              "RestoreClipboardAfterPaste": false,
              "FallbackHotkey": "Ctrl+Win+Space"
            }
            """);

        var settings = store.Load();

        Assert.Equal("Ctrl+Alt+Space", settings.FallbackHotkey);
    }

    [Fact]
    public void CreateDefault_uses_environment_override_when_set()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json");
        Environment.SetEnvironmentVariable("WINDOWS_EASY_EMOJI_SETTINGS_PATH", path);

        try
        {
            var store = SettingsStore.CreateDefault();

            Assert.Equal(path, store.SettingsPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINDOWS_EASY_EMOJI_SETTINGS_PATH", null);
        }
    }
}
