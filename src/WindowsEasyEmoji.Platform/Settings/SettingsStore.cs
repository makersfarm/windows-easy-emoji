using System.IO;
using System.Text.Json;

namespace WindowsEasyEmoji.Platform.Settings;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string settingsPath;

    public SettingsStore(string settingsPath)
    {
        this.settingsPath = settingsPath;
    }

    public static SettingsStore CreateDefault()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsEasyEmoji");

        return new SettingsStore(Path.Combine(directory, "settings.json"));
    }

    public AppSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return AppSettings.Default;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? AppSettings.Default;
            return Migrate(settings);
        }
        catch (JsonException)
        {
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(settingsPath, json);
    }

    private static AppSettings Migrate(AppSettings settings)
    {
        if (settings.FallbackHotkey.Equals("Ctrl+Win+Space", StringComparison.OrdinalIgnoreCase))
        {
            return settings with { FallbackHotkey = AppSettings.Default.FallbackHotkey };
        }

        return settings;
    }
}
