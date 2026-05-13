using WindowsEasyEmoji.Core.User;

namespace WindowsEasyEmoji.Core.Tests.User;

public sealed class UserEmojiStateUpdaterTests
{
    [Fact]
    public void RecordUse_creates_state_for_first_use()
    {
        var usedAt = new DateTimeOffset(2026, 5, 13, 17, 30, 0, TimeSpan.Zero);

        var state = UserEmojiStateUpdater.RecordUse(null, "red_heart", usedAt);

        Assert.Equal("red_heart", state.EmojiId);
        Assert.Equal(usedAt, state.LastUsedAt);
        Assert.Equal(1, state.UseCount);
        Assert.False(state.Favorite);
        Assert.Empty(state.CustomAliases);
    }

    [Fact]
    public void RecordUse_preserves_user_fields_and_increments_usage()
    {
        var usedAt = new DateTimeOffset(2026, 5, 13, 17, 31, 0, TimeSpan.Zero);
        var current = new UserEmojiState(
            EmojiId: "red_heart",
            LastUsedAt: new DateTimeOffset(2026, 5, 12, 17, 31, 0, TimeSpan.Zero),
            UseCount: 4,
            Favorite: true,
            CustomAliases: ["내하트"]);

        var state = UserEmojiStateUpdater.RecordUse(current, "red_heart", usedAt);

        Assert.Equal(usedAt, state.LastUsedAt);
        Assert.Equal(5, state.UseCount);
        Assert.True(state.Favorite);
        Assert.Equal(["내하트"], state.CustomAliases);
    }
}
