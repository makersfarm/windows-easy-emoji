namespace WindowsEasyEmoji.Core.Emoji;

public sealed record EmojiVariant(
    string Type,
    string? ParentId,
    string? SkinTone,
    bool Default);
