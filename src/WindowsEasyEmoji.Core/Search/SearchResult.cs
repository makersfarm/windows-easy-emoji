using WindowsEasyEmoji.Core.Emoji;

namespace WindowsEasyEmoji.Core.Search;

public sealed record SearchResult(
    EmojiRecord Record,
    int Score,
    SearchMatchType MatchType);
