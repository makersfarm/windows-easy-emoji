namespace WindowsEasyEmoji.Core.User;

public static class UserEmojiStateUpdater
{
    public static UserEmojiState ToggleFavorite(UserEmojiState? current, string emojiId)
    {
        if (current is null)
        {
            return new UserEmojiState(
                EmojiId: emojiId,
                LastUsedAt: null,
                UseCount: 0,
                Favorite: true,
                CustomAliases: []);
        }

        return current with
        {
            Favorite = !current.Favorite
        };
    }

    public static UserEmojiState RecordUse(
        UserEmojiState? current,
        string emojiId,
        DateTimeOffset usedAt)
    {
        if (current is null)
        {
            return new UserEmojiState(
                EmojiId: emojiId,
                LastUsedAt: usedAt,
                UseCount: 1,
                Favorite: false,
                CustomAliases: []);
        }

        return current with
        {
            LastUsedAt = usedAt,
            UseCount = Math.Max(0, current.UseCount) + 1
        };
    }
}
