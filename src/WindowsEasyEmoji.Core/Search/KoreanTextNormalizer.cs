using System.Text;
using System.Text.RegularExpressions;

namespace WindowsEasyEmoji.Core.Search;

public static partial class KoreanTextNormalizer
{
    private static readonly char[] ChosungTable =
    [
        'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
        'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    ];

    public static string NormalizeQuery(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return WhitespaceRegex()
            .Replace(value.Trim().ToLowerInvariant(), " ");
    }

    public static string ToCompactKey(string value)
    {
        var normalized = NormalizeQuery(value);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (character is not (' ' or '-' or '_'))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    public static IEnumerable<string> ExtractTokens(string value)
    {
        var normalized = NormalizeQuery(value);
        if (normalized.Length == 0)
        {
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var candidate in ExpandToken(token))
            {
                if (candidate.Length > 0 && seen.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
    }

    public static string ToChosung(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(ToChosungCharacter(character));
        }

        return builder.ToString();
    }

    private static IEnumerable<string> ExpandToken(string token)
    {
        yield return token;

        if (token.Length > 2 && token.StartsWith(':') && token.EndsWith(':'))
        {
            yield return token[1..^1];
        }

        var compact = ToCompactKey(token);
        if (!string.Equals(compact, token, StringComparison.Ordinal))
        {
            yield return compact;
        }
    }

    private static char ToChosungCharacter(char character)
    {
        const int hangulBase = 0xAC00;
        const int hangulLast = 0xD7A3;
        const int jungseongCount = 21;
        const int jongseongCount = 28;

        if (character < hangulBase || character > hangulLast)
        {
            return character;
        }

        var syllableIndex = character - hangulBase;
        var chosungIndex = syllableIndex / (jungseongCount * jongseongCount);
        return ChosungTable[chosungIndex];
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
