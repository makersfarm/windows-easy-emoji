using WindowsEasyEmoji.Core.Emoji;
using WindowsEasyEmoji.Core.Search;
using WindowsEasyEmoji.Core.User;

namespace WindowsEasyEmoji.Core.Tests.Search;

public sealed class EmojiSearchServiceTests
{
    [Fact]
    public void Search_ranks_exact_korean_alias_above_english_keyword_match()
    {
        var service = new EmojiSearchService(
        [
            Emoji("red_heart", "❤️", koAliases: ["하트"], koKeywords: ["사랑"], enKeywords: ["heart"]),
            Emoji("heart_eyes", "😍", koAliases: ["반함"], koKeywords: ["얼굴"], enKeywords: ["heart"])
        ]);

        var results = service.Search("하트").ToArray();

        Assert.Equal("red_heart", results[0].Record.Id);
        Assert.Equal(SearchMatchType.KoreanAliasExact, results[0].MatchType);
    }

    [Fact]
    public void Search_matches_chosung_query()
    {
        var service = new EmojiSearchService(
        [
            Emoji("red_heart", "❤️", koAliases: ["빨간 하트"], chosung: ["ㅃㄱ ㅎㅌ"]),
            Emoji("fire", "🔥", koAliases: ["불"], chosung: ["ㅂ"])
        ]);

        var results = service.Search("ㅃㄱㅎㅌ").ToArray();

        Assert.Equal("red_heart", results[0].Record.Id);
        Assert.Equal(SearchMatchType.KoreanAliasExact, results[0].MatchType);
    }

    [Fact]
    public void Search_ranks_korean_alias_chosung_above_keyword_chosung()
    {
        var service = new EmojiSearchService(
        [
            Emoji("smiling_face_with_hearts", "🥰", koKeywords: ["하트"], chosung: ["ㅎㅌ"], order: 1),
            Emoji("red_heart", "❤️", koAliases: ["하트"], chosung: ["ㅎㅌ"], order: 2)
        ]);

        var results = service.Search("ㅎㅌ").ToArray();

        Assert.Equal("red_heart", results[0].Record.Id);
        Assert.Equal(SearchMatchType.KoreanAliasExact, results[0].MatchType);
    }

    [Fact]
    public void Search_applies_favorite_recent_and_use_count_boosts()
    {
        var service = new EmojiSearchService(
        [
            Emoji("white_heart", "🤍", koAliases: ["하트"], order: 1),
            Emoji("red_heart", "❤️", koAliases: ["하트"], order: 2)
        ],
        new Dictionary<string, UserEmojiState>
        {
            ["red_heart"] = new(
                EmojiId: "red_heart",
                LastUsedAt: DateTimeOffset.UtcNow,
                UseCount: 20,
                Favorite: true,
                CustomAliases: [])
        });

        var results = service.Search("하트").ToArray();

        Assert.Equal("red_heart", results[0].Record.Id);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void Empty_search_orders_recent_usage_before_default_order()
    {
        var service = new EmojiSearchService(
        [
            Emoji("red_heart", "❤️", order: 1),
            Emoji("fire", "🔥", order: 2)
        ],
        new Dictionary<string, UserEmojiState>
        {
            ["red_heart"] = new(
                EmojiId: "red_heart",
                LastUsedAt: new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero),
                UseCount: 1,
                Favorite: false,
                CustomAliases: []),
            ["fire"] = new(
                EmojiId: "fire",
                LastUsedAt: new DateTimeOffset(2026, 5, 13, 0, 0, 0, TimeSpan.Zero),
                UseCount: 1,
                Favorite: false,
                CustomAliases: [])
        });

        var results = service.Search("").ToArray();

        Assert.Equal("fire", results[0].Record.Id);
    }

    [Fact]
    public void Search_matches_custom_aliases_from_user_state()
    {
        var service = new EmojiSearchService(
        [
            Emoji("red_heart", "❤️", koAliases: ["하트"]),
            Emoji("fire", "🔥", koAliases: ["불"])
        ],
        new Dictionary<string, UserEmojiState>
        {
            ["fire"] = new(
                EmojiId: "fire",
                LastUsedAt: null,
                UseCount: 0,
                Favorite: false,
                CustomAliases: ["내최애"])
        });

        var results = service.Search("내최애").ToArray();

        Assert.Equal("fire", results[0].Record.Id);
        Assert.Equal(SearchMatchType.KoreanAliasExact, results[0].MatchType);
    }

    [Fact]
    public void Search_penalizes_hidden_variants()
    {
        var service = new EmojiSearchService(
        [
            Emoji("thumbs_up", "👍", koAliases: ["따봉"], hiddenByDefault: false, order: 1),
            Emoji("thumbs_up_medium_skin", "👍🏽", koAliases: ["따봉"], hiddenByDefault: true, order: 2)
        ]);

        var results = service.Search("따봉").ToArray();

        Assert.Equal("thumbs_up", results[0].Record.Id);
        Assert.True(results[0].Score > results[1].Score);
    }

    private static EmojiRecord Emoji(
        string id,
        string emoji,
        IReadOnlyList<string>? koAliases = null,
        IReadOnlyList<string>? koKeywords = null,
        IReadOnlyList<string>? enKeywords = null,
        IReadOnlyList<string>? aliases = null,
        IReadOnlyList<string>? chosung = null,
        bool hiddenByDefault = false,
        int order = 100)
    {
        var koName = koAliases?.FirstOrDefault() ?? id;
        var enName = id.Replace('_', ' ');

        return new EmojiRecord(
            Id: id,
            Emoji: emoji,
            BaseEmoji: emoji,
            Unicode: [],
            Version: "1.0",
            Category: "Smileys & Emotion",
            Group: "test",
            Order: order,
            Name: new LocalizedText(enName, koName),
            Keywords: new Dictionary<string, IReadOnlyList<string>>
            {
                ["en"] = enKeywords ?? [],
                ["ko"] = koKeywords ?? []
            },
            Aliases: aliases ?? [],
            KoAliases: koAliases ?? [],
            Chosung: chosung ?? [],
            SearchText: string.Empty,
            Variant: new EmojiVariant("emoji_presentation", null, null, !hiddenByDefault),
            Flags: new EmojiFlags(false, hiddenByDefault, hiddenByDefault));
    }
}
