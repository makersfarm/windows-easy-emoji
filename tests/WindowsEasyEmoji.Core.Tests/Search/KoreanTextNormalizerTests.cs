using WindowsEasyEmoji.Core.Search;

namespace WindowsEasyEmoji.Core.Tests.Search;

public sealed class KoreanTextNormalizerTests
{
    [Theory]
    [InlineData("  Heart  ", "heart")]
    [InlineData("  빨간   하트  ", "빨간 하트")]
    [InlineData(":Heart:", ":heart:")]
    public void NormalizeQuery_trims_lowercases_and_collapses_spaces(string input, string expected)
    {
        var actual = KoreanTextNormalizer.NormalizeQuery(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("red-heart", "redheart")]
    [InlineData("red_heart", "redheart")]
    [InlineData("빨간 하트", "빨간하트")]
    public void ToCompactKey_removes_separators_used_by_shortcodes_and_names(string input, string expected)
    {
        var actual = KoreanTextNormalizer.ToCompactKey(input);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExtractTokens_keeps_colon_shortcode_and_plain_alias_forms()
    {
        var tokens = KoreanTextNormalizer.ExtractTokens(":heart: 빨간_하트 red-heart").ToArray();

        Assert.Contains(":heart:", tokens);
        Assert.Contains("heart", tokens);
        Assert.Contains("빨간_하트", tokens);
        Assert.Contains("빨간하트", tokens);
        Assert.Contains("red-heart", tokens);
        Assert.Contains("redheart", tokens);
    }

    [Theory]
    [InlineData("하트", "ㅎㅌ")]
    [InlineData("빨간 하트", "ㅃㄱ ㅎㅌ")]
    [InlineData("사랑해", "ㅅㄹㅎ")]
    public void ToChosung_returns_initial_consonants_for_hangul_syllables(string input, string expected)
    {
        var actual = KoreanTextNormalizer.ToChosung(input);

        Assert.Equal(expected, actual);
    }
}
