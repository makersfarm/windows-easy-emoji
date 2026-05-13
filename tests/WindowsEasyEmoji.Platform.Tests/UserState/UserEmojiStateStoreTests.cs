using WindowsEasyEmoji.Core.User;
using WindowsEasyEmoji.Platform.UserState;

namespace WindowsEasyEmoji.Platform.Tests.UserState;

public sealed class UserEmojiStateStoreTests
{
    [Fact]
    public void Load_returns_empty_state_when_file_does_not_exist()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "user-state.json");
        var store = new UserEmojiStateStore(path);

        var state = store.Load();

        Assert.Empty(state);
    }

    [Fact]
    public void Save_persists_state_for_next_load()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "user-state.json");
        var store = new UserEmojiStateStore(path);
        var saved = new Dictionary<string, UserEmojiState>
        {
            ["red_heart"] = new(
                EmojiId: "red_heart",
                LastUsedAt: new DateTimeOffset(2026, 5, 13, 18, 0, 0, TimeSpan.Zero),
                UseCount: 3,
                Favorite: true,
                CustomAliases: ["내하트"])
        };

        store.Save(saved);
        var loaded = store.Load();

        Assert.True(loaded.ContainsKey("red_heart"));
        Assert.Equal(saved["red_heart"].EmojiId, loaded["red_heart"].EmojiId);
        Assert.Equal(saved["red_heart"].LastUsedAt, loaded["red_heart"].LastUsedAt);
        Assert.Equal(saved["red_heart"].UseCount, loaded["red_heart"].UseCount);
        Assert.Equal(saved["red_heart"].Favorite, loaded["red_heart"].Favorite);
        Assert.Equal(saved["red_heart"].CustomAliases, loaded["red_heart"].CustomAliases);
    }

    [Fact]
    public void Load_returns_empty_state_when_json_is_invalid()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "user-state.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "{not-json");
        var store = new UserEmojiStateStore(path);

        var state = store.Load();

        Assert.Empty(state);
    }

    [Fact]
    public void CreateDefault_uses_environment_override_when_set()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "user-state.json");
        Environment.SetEnvironmentVariable("WINDOWS_EASY_EMOJI_USER_STATE_PATH", path);

        try
        {
            var store = UserEmojiStateStore.CreateDefault();

            Assert.Equal(path, store.StatePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINDOWS_EASY_EMOJI_USER_STATE_PATH", null);
        }
    }
}
