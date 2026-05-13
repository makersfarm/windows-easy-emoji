using WindowsEasyEmoji.Core.Emoji;
using WindowsEasyEmoji.Core.Search;

namespace WindowsEasyEmoji.Core.Tests.Search;

public sealed class SearchQualityGoldenTests
{
    [Theory]
    [InlineData("하트", "red_heart")]
    [InlineData("사랑", "red_heart")]
    [InlineData("ㅋㅋ", "face_with_tears_of_joy")]
    [InlineData("웃음", "face_with_tears_of_joy")]
    [InlineData("불", "fire")]
    [InlineData("한국", "flag_south_korea")]
    [InlineData("박수", "clapping_hands")]
    [InlineData("체크", "check_mark_button")]
    [InlineData("ㅎㅌ", "red_heart")]
    [InlineData("따봉", "thumbs_up")]
    public void Search_returns_expected_top_result_for_korean_golden_queries(string query, string expectedId)
    {
        var service = new EmojiSearchService(LoadAppEmojiData());

        var result = service.Search(query, limit: 1).Single();

        Assert.Equal(expectedId, result.Record.Id);
    }

    private static IReadOnlyList<EmojiRecord> LoadAppEmojiData()
    {
        var repositoryRoot = FindRepositoryRoot();
        var dataPath = Path.Combine(repositoryRoot, "src", "WindowsEasyEmoji.App", "Data", "emoji.json");
        return EmojiRepository.LoadFromJson(File.ReadAllText(dataPath));
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
