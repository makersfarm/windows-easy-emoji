namespace WindowsEasyEmoji.Core.User;

public sealed record UserEmojiState(
    string EmojiId,
    DateTimeOffset? LastUsedAt,
    int UseCount,
    bool Favorite,
    IReadOnlyList<string> CustomAliases);
