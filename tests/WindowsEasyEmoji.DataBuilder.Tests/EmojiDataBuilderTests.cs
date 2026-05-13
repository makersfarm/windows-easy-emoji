using WindowsEasyEmoji.DataBuilder;

namespace WindowsEasyEmoji.DataBuilder.Tests;

public sealed class EmojiDataBuilderTests
{
    [Fact]
    public void Build_merges_unicode_order_cldr_korean_terms_and_external_tags()
    {
        var records = EmojiDataBuilder.Build(DataSourcePaths.FromRepositoryRoot(FindRepositoryRoot()));

        Assert.True(records.Count >= 3900);

        var redHeart = records.Single(record => record.Emoji == "❤️");
        Assert.Equal("red_heart", redHeart.Id);
        Assert.Equal("Smileys & Emotion", redHeart.Category);
        Assert.Equal("빨간색 하트", redHeart.Name.Ko);
        Assert.Contains("하트", redHeart.Keywords["ko"]);
        Assert.Contains("사랑", redHeart.Keywords["ko"]);

        var fire = records.Single(record => record.Emoji == "🔥");
        Assert.Equal("fire", fire.Id);
        Assert.Contains("불", fire.Keywords["ko"]);

        var korea = records.Single(record => record.Emoji == "🇰🇷");
        Assert.Equal("flag_south_korea", korea.Id);
        Assert.Contains("대한민국", korea.Name.Ko);

        var medal = records.Single(record => record.Emoji == "🥇");
        Assert.Contains("victory", medal.Keywords["en"]);
    }

    [Fact]
    public void Build_marks_skin_tone_variants_hidden_and_generates_chosung_aliases()
    {
        var records = EmojiDataBuilder.Build(DataSourcePaths.FromRepositoryRoot(FindRepositoryRoot()));

        var thumbsUp = records.Single(record => record.Emoji == "👍");
        var mediumSkinThumbsUp = records.Single(record => record.Emoji == "👍🏽");
        Assert.True(thumbsUp.Flags.SupportsSkinTone);
        Assert.True(mediumSkinThumbsUp.Flags.HiddenByDefault);
        Assert.True(mediumSkinThumbsUp.Flags.IsVariant);

        var clappingHands = records.Single(record => record.Emoji == "👏");
        Assert.Contains("박수", clappingHands.Keywords["ko"]);
        Assert.Contains("ㅂㅅ", clappingHands.Chosung);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WindowsEasyEmoji.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }
}
