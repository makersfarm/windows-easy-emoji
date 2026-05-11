namespace WindowsEasyEmoji.Core.Emoji;

public sealed record EmojiFlags(
    bool SupportsSkinTone,
    bool IsVariant,
    bool HiddenByDefault);
