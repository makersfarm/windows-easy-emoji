using WindowsEasyEmoji.Core.Emoji;
using WindowsEasyEmoji.Core.User;

namespace WindowsEasyEmoji.Core.Search;

public sealed class EmojiSearchService
{
    private const int CustomAliasExactScore = 1500;
    private const int CuratedAliasExactScore = 1300;
    private const int KoreanAliasExactScore = 1000;
    private const int AliasExactScore = 900;
    private const int KoreanKeywordExactScore = 800;
    private const int ChosungExactScore = 760;
    private const int EnglishExactScore = 700;
    private const int PrefixScore = 500;
    private const int ContainsScore = 300;
    private const int FuzzyScore = 100;

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> CuratedQueryBoosts =
        CreateCuratedQueryBoosts();

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
                .ThenByDescending(result => GetLastUsedAt(result.Record.Id))
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
        userStateByEmojiId.TryGetValue(record.Id, out var userState);
        var (matchType, baseScore) = GetBaseMatch(record, userState, normalizedQuery, compactQuery);
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
        UserEmojiState? userState,
        string normalizedQuery,
        string compactQuery)
    {
        if (ContainsExact(userState?.CustomAliases ?? [], normalizedQuery, compactQuery))
        {
            return (SearchMatchType.KoreanAliasExact, CustomAliasExactScore);
        }

        if (ContainsCuratedQueryBoost(record.Id, normalizedQuery, compactQuery))
        {
            return (SearchMatchType.CuratedAliasExact, CuratedAliasExactScore);
        }

        if (ContainsExact(record.KoAliases, normalizedQuery, compactQuery))
        {
            return (SearchMatchType.KoreanAliasExact, KoreanAliasExactScore);
        }

        if (ContainsChosungExact(record.KoAliases, compactQuery))
        {
            return (SearchMatchType.KoreanAliasExact, KoreanAliasExactScore);
        }

        if (ContainsExact(record.Aliases, normalizedQuery, compactQuery))
        {
            return (SearchMatchType.AliasExact, AliasExactScore);
        }

        if (ContainsExact(GetKeywordList(record, "ko").Append(record.Name.Ko), normalizedQuery, compactQuery))
        {
            return (SearchMatchType.KoreanKeywordExact, KoreanKeywordExactScore);
        }

        if (ContainsExact(record.Chosung, normalizedQuery, compactQuery))
        {
            return (SearchMatchType.ChosungExact, ChosungExactScore);
        }

        if (ContainsExact(GetKeywordList(record, "en").Append(record.Name.En), normalizedQuery, compactQuery))
        {
            return (SearchMatchType.EnglishExact, EnglishExactScore);
        }

        var searchableValues = GetSearchableValues(record).ToArray();
        if (searchableValues.Any(value => IsPrefix(value, normalizedQuery, compactQuery)))
        {
            return (SearchMatchType.Prefix, PrefixScore);
        }

        if (searchableValues.Any(value => IsContains(value, normalizedQuery, compactQuery)))
        {
            return (SearchMatchType.Contains, ContainsScore);
        }

        if (searchableValues.Any(value => IsSubsequence(compactQuery, KoreanTextNormalizer.ToCompactKey(value))))
        {
            return (SearchMatchType.Fuzzy, FuzzyScore);
        }

        return (SearchMatchType.None, 0);
    }

    private DateTimeOffset? GetLastUsedAt(string emojiId)
    {
        return userStateByEmojiId.TryGetValue(emojiId, out var state)
            ? state.LastUsedAt
            : null;
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

    private static bool ContainsCuratedQueryBoost(string emojiId, string normalizedQuery, string compactQuery)
    {
        if (CuratedQueryBoosts.TryGetValue(normalizedQuery, out var normalizedMatches) && normalizedMatches.Contains(emojiId))
        {
            return true;
        }

        return !string.Equals(normalizedQuery, compactQuery, StringComparison.Ordinal) &&
               CuratedQueryBoosts.TryGetValue(compactQuery, out var compactMatches) &&
               compactMatches.Contains(emojiId);
    }

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> CreateCuratedQueryBoosts()
    {
        var boosts = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        Add("face_with_tears_of_joy", "ㅋㅋ", "ㅋㅋㅋ", "ㅎㅎ", "ㅎㅎㅎ", "웃음", "웃겨", "웃기다", "개웃김", "lol");
        Add("crying_face", "슬픔", "슬퍼", "울음", "눈물");
        Add("loudly_crying_face", "ㅠㅠ", "ㅜㅜ", "엉엉", "대성통곡");
        Add("red_heart", "하트", "ㅎㅌ", "사랑", "빨간하트", "내사랑");
        Add("fire", "불", "핫", "화재", "불타", "쩐다");
        Add("flag_south_korea", "한국", "대한민국", "태극기", "korea", "kor");
        Add("thumbs_up", "따봉", "좋아요", "굿", "굳", "최고", "ㅇㅋ", "오케이", "찬성");
        Add("thumbs_down", "싫어요", "별로", "최악", "반대");
        Add("folded_hands", "감사", "ㄱㅅ", "고마워", "제발", "부탁", "기도", "합장");
        Add("party_popper", "축하", "ㅊㅋ", "추카", "축하해", "축하드립니다", "파티", "폭죽");
        Add("birthday_cake", "생일", "생일축하", "생축", "ㅅㅊ", "케이크");
        Add("check_mark_button", "체크", "확인", "완료", "완료됨", "체크표시");

        return boosts.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlySet<string>)pair.Value,
            StringComparer.Ordinal);

        void Add(string emojiId, params string[] queries)
        {
            foreach (var query in queries)
            {
                var normalizedQuery = KoreanTextNormalizer.NormalizeQuery(query);
                AddQuery(normalizedQuery, emojiId);
            }
        }

        void AddQuery(string query, string emojiId)
        {
            if (query.Length == 0)
            {
                return;
            }

            if (!boosts.TryGetValue(query, out var emojiIds))
            {
                emojiIds = new HashSet<string>(StringComparer.Ordinal);
                boosts[query] = emojiIds;
            }

            emojiIds.Add(emojiId);
        }
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

    private static bool ContainsChosungExact(IEnumerable<string> values, string compactQuery)
    {
        return values.Any(value =>
            KoreanTextNormalizer.ToCompactKey(KoreanTextNormalizer.ToChosung(value)) == compactQuery);
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
