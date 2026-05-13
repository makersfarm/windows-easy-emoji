namespace WindowsEasyEmoji.DataBuilder;

public sealed record DataSourcePaths(
    string UnicodeEmojiTestPath,
    string CldrKoreanAnnotationsPath,
    string CldrKoreanDerivedAnnotationsPath,
    string EmojiKoreanPath,
    string BadrexRowsJsonlPath,
    string MuanDataByEmojiPath,
    string? CuratedEmojiPath)
{
    public static DataSourcePaths FromRepositoryRoot(string repositoryRoot)
    {
        var externalRoot = Path.Combine(repositoryRoot, "data", "external");
        return new DataSourcePaths(
            UnicodeEmojiTestPath: Path.Combine(externalRoot, "unicode_emoji_17_0", "emoji-test.txt"),
            CldrKoreanAnnotationsPath: Path.Combine(externalRoot, "unicode_cldr_annotations_ko_48_2", "annotations-ko.json"),
            CldrKoreanDerivedAnnotationsPath: Path.Combine(externalRoot, "unicode_cldr_annotations_ko_48_2", "annotations-derived-ko.json"),
            EmojiKoreanPath: Path.Combine(externalRoot, "emoji_korean", "emoji_korean.json"),
            BadrexRowsJsonlPath: Path.Combine(externalRoot, "badrex_llm_generated_emoji_descriptions", "rows.jsonl"),
            MuanDataByEmojiPath: Path.Combine(externalRoot, "muan_unicode_emoji_json", "data-by-emoji.json"),
            CuratedEmojiPath: Path.Combine(repositoryRoot, "src", "WindowsEasyEmoji.App", "Data", "emoji.json"));
    }
}
