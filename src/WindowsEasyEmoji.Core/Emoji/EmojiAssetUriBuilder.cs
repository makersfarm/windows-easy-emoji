using System.Text;

namespace WindowsEasyEmoji.Core.Emoji;

public static class EmojiAssetUriBuilder
{
    private const string TwemojiBaseUri = "https://cdn.jsdelivr.net/gh/twitter/twemoji@14.0.2/assets/72x72/";

    public static string? GetTwemojiPngUri(string emoji)
    {
        var assetKey = GetTwemojiAssetKey(emoji);
        return assetKey.Length == 0 ? null : $"{TwemojiBaseUri}{assetKey}.png";
    }

    public static string GetTwemojiAssetKey(string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
        {
            return string.Empty;
        }

        var hasZeroWidthJoiner = emoji.EnumerateRunes().Any(rune => rune.Value == 0x200D);
        var builder = new StringBuilder();
        foreach (var rune in emoji.EnumerateRunes())
        {
            if (rune.Value == 0xFE0E || (rune.Value == 0xFE0F && !hasZeroWidthJoiner))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('-');
            }

            builder.Append(rune.Value.ToString("x"));
        }

        return builder.ToString();
    }
}
