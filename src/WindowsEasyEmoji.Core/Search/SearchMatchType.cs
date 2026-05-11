namespace WindowsEasyEmoji.Core.Search;

public enum SearchMatchType
{
    None = 0,
    Fuzzy = 1,
    Contains = 2,
    Prefix = 3,
    EnglishExact = 4,
    ChosungExact = 5,
    KoreanKeywordExact = 6,
    AliasExact = 7,
    KoreanAliasExact = 8
}
