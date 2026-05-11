using WindowsEasyEmoji.Core.Emoji;
using WindowsEasyEmoji.Core.User;

namespace WindowsEasyEmoji.Core.Search;

public sealed class EmojiSearchService
{
    private readonly IReadOnlyList<EmojiRecord> records;
    private readonly IReadOnlyDictionary<string, UserEmojiState> userStateByEmojiId;

    public EmojiSearchService(
        IReadOnlyList<EmojiRecord> records,
        IReadOnlyDictionary<string, UserEmojiState>? userStateByEmojiId = null)
    {
        this.records = records;
        this.userStateByEmojiId = userStateByEmojiId ?? new Dictionary<string, UserEmojiState>();
    }

    public IReadOnlyList<SearchResult> Search(string query, int limit = 20)
    {
        var normalizedQuery = KoreanTextNormalizer.NormalizeQuery(query);
        var compactQuery = KoreanTextNormalizer.ToCompactKey(normalizedQuery);

        if (normalizedQuery.Length == 0)
        {
            return records
                .Select(record => ToResult(record, SearchMatchType.None, 0))
                .OrderByDescending(result => result.Score)
                .ThenBy(result => result.Record.Order)
                .ThenBy(result => result.Record.Id, StringComparer.Ordinal)
                .Take(limit)
                .ToArray();
        }

        return records
            .Select(record => Score(record, normalizedQuery, compactQuery))
            .Where(result => result.MatchType is not SearchMatchType.None)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Record.Order)
            .ThenBy(result => result.Record.Id, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();
    }

    private SearchResult Score(EmojiRecord record, string normalizedQuery, string compactQuery)
    {
        var (matchType, baseScore) = GetBaseMatch(record, normalizedQuery, compactQuery);
        if (matchType is SearchMatchType.None)
        {
            return new SearchResult(record, 0, SearchMatchType.None);
        }

        return ToResult(record, matchType, baseScore);
    }

    private SearchResult ToResult(EmojiRecord record, SearchMatchType matchType, int baseScore)
    {
        var score = baseScore;
        if (userStateByEmojiId.TryGetValue(record.Id, out var state))
        {
            if (state.Favorite)
            {
                score += 80;
            }

            if (state.LastUsedAt is not null)
            {
                score += 60;
            }

            score += Math.Min(50, Math.Max(0, state.UseCount));
        }

        if (record.Flags.HiddenByDefault)
        {
            score -= 300;
        }

        return new SearchResult(record, score, matchType);
    }

    private static (SearchMatchType MatchType, int Score) GetBaseMatch(
        EmojiRecord record,
        string normalizedQuery,
        string compactQuery)
    {
        if (ContainsExact(record.KoAliases, normalizedQuery, compactQuery))
        {
            return (SearchMatchType.KoreanAliasExact, 1000);
        }

        if (ContainsExact(record.Aliases, normalizedQuery, compactQuery))
        {
            return (SearchMatchType.AliasExact, 900);
        }

        if (ContainsExact(GetKeywordList(record, "ko").Append(record.Name.Ko), normalizedQuery, compactQuery))
        {
            return (SearchMatchType.KoreanKeywordExact, 800);
        }

        if (ContainsExact(record.Chosung, normalizedQuery, compactQuery))
        {
            return (SearchMatchType.ChosungExact, 760);
        }

        if (ContainsExact(GetKeywordList(record, "en").Append(record.Name.En), normalizedQuery, compactQuery))
        {
            return (SearchMatchType.EnglishExact, 700);
        }

        var searchableValues = GetSearchableValues(record).ToArray();
        if (searchableValues.Any(value => IsPrefix(value, normalizedQuery, compactQuery)))
        {
            return (SearchMatchType.Prefix, 500);
        }

        if (searchableValues.Any(value => IsContains(value, normalizedQuery, compactQuery)))
        {
            return (SearchMatchType.Contains, 300);
        }

        if (searchableValues.Any(value => IsSubsequence(compactQuery, KoreanTextNormalizer.ToCompactKey(value))))
        {
            return (SearchMatchType.Fuzzy, 100);
        }

        return (SearchMatchType.None, 0);
    }

    private static IEnumerable<string> GetSearchableValues(EmojiRecord record)
    {
        yield return record.Name.Ko;
        yield return record.Name.En;
        yield return record.SearchText;

        foreach (var value in record.KoAliases)
        {
            yield return value;
        }

        foreach (var value in record.Aliases)
        {
            yield return value;
        }

        foreach (var value in record.Chosung)
        {
            yield return value;
        }

        foreach (var value in GetKeywordList(record, "ko"))
        {
            yield return value;
        }

        foreach (var value in GetKeywordList(record, "en"))
        {
            yield return value;
        }
    }

    private static IReadOnlyList<string> GetKeywordList(EmojiRecord record, string locale)
    {
        return record.Keywords.TryGetValue(locale, out var values) ? values : [];
    }

    private static bool ContainsExact(IEnumerable<string> values, string normalizedQuery, string compactQuery)
    {
        return values.Any(value =>
        {
            var normalizedValue = KoreanTextNormalizer.NormalizeQuery(value);
            return normalizedValue == normalizedQuery ||
                   KoreanTextNormalizer.ToCompactKey(normalizedValue) == compactQuery;
        });
    }

    private static bool IsPrefix(string value, string normalizedQuery, string compactQuery)
    {
        var normalizedValue = KoreanTextNormalizer.NormalizeQuery(value);
        var compactValue = KoreanTextNormalizer.ToCompactKey(normalizedValue);
        return normalizedValue.StartsWith(normalizedQuery, StringComparison.Ordinal) ||
               compactValue.StartsWith(compactQuery, StringComparison.Ordinal);
    }

    private static bool IsContains(string value, string normalizedQuery, string compactQuery)
    {
        var normalizedValue = KoreanTextNormalizer.NormalizeQuery(value);
        var compactValue = KoreanTextNormalizer.ToCompactKey(normalizedValue);
        return normalizedValue.Contains(normalizedQuery, StringComparison.Ordinal) ||
               compactValue.Contains(compactQuery, StringComparison.Ordinal);
    }

    private static bool IsSubsequence(string query, string value)
    {
        if (query.Length == 0)
        {
            return false;
        }

        var queryIndex = 0;
        foreach (var character in value)
        {
            if (query[queryIndex] == character)
            {
                queryIndex++;
                if (queryIndex == query.Length)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
