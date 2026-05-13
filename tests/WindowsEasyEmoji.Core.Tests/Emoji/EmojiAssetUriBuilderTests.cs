using WindowsEasyEmoji.Core.Emoji;

namespace WindowsEasyEmoji.Core.Tests.Emoji;

public sealed class EmojiAssetUriBuilderTests
{
    [Theory]
    [InlineData("😀", "1f600.png")]
    [InlineData("❤️", "2764.png")]
    [InlineData("👍🏽", "1f44d-1f3fd.png")]
    [InlineData("🇰🇷", "1f1f0-1f1f7.png")]
    [InlineData("🏳️‍🌈", "1f3f3-fe0f-200d-1f308.png")]
    [InlineData("❤️‍🔥", "2764-fe0f-200d-1f525.png")]
    public void GetTwemojiPngUri_returns_codepoint_asset_uri(string emoji, string expectedFileName)
    {
        var uri = EmojiAssetUriBuilder.GetTwemojiPngUri(emoji);

        Assert.NotNull(uri);
        Assert.EndsWith($"/{expectedFileName}", uri);
    }
}
