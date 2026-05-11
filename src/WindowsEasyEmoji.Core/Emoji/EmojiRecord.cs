namespace WindowsEasyEmoji.Core.Emoji;

public sealed record EmojiRecord(
    string Id,
    string Emoji,
    string BaseEmoji,
    IReadOnlyList<string> Unicode,
    string Version,
    string Category,
    string Group,
    int Order,
    LocalizedText Name,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Keywords,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> KoAliases,
    IReadOnlyList<string> Chosung,
    string SearchText,
    EmojiVariant Variant,
    EmojiFlags Flags);
