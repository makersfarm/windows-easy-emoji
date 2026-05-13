using System.IO;
using System.Text.Json;
using WindowsEasyEmoji.Core.User;

namespace WindowsEasyEmoji.Platform.UserState;

public sealed class UserEmojiStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string statePath;

    public UserEmojiStateStore(string statePath)
    {
        this.statePath = statePath;
    }

    public string StatePath => statePath;

    public static UserEmojiStateStore CreateDefault()
    {
        var overridePath = Environment.GetEnvironmentVariable("WINDOWS_EASY_EMOJI_USER_STATE_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return new UserEmojiStateStore(overridePath);
        }

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsEasyEmoji");

        return new UserEmojiStateStore(Path.Combine(directory, "user-state.json"));
    }

    public Dictionary<string, UserEmojiState> Load()
    {
        if (!File.Exists(statePath))
        {
            return new Dictionary<string, UserEmojiState>(StringComparer.Ordinal);
        }

        try
        {
            var json = File.ReadAllText(statePath);
            var states = JsonSerializer.Deserialize<List<UserEmojiState>>(json, JsonOptions) ?? [];
            return states
                .Where(state => !string.IsNullOrWhiteSpace(state.EmojiId))
                .GroupBy(state => state.EmojiId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, UserEmojiState>(StringComparer.Ordinal);
        }
    }

    public void Save(IReadOnlyDictionary<string, UserEmojiState> statesByEmojiId)
    {
        var directory = Path.GetDirectoryName(statePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var states = statesByEmojiId.Values
            .OrderByDescending(state => state.LastUsedAt)
            .ThenBy(state => state.EmojiId, StringComparer.Ordinal)
            .ToArray();
        File.WriteAllText(statePath, JsonSerializer.Serialize(states, JsonOptions));
    }
}
