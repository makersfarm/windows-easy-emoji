using WindowsEasyEmoji.Core.Emoji;

namespace WindowsEasyEmoji.Core.Tests.Emoji;

public sealed class EmojiRepositoryTests
{
    [Fact]
    public void LoadFromJson_loads_emoji_records()
    {
        const string json = """
        [
          {
            "id": "red_heart",
            "emoji": "❤️",
            "baseEmoji": "❤",
            "unicode": ["2764", "FE0F"],
            "version": "0.6",
            "category": "Smileys & Emotion",
            "group": "heart",
            "order": 120,
            "name": { "en": "red heart", "ko": "빨간 하트" },
            "keywords": {
              "en": ["heart", "love", "red"],
              "ko": ["하트", "사랑", "빨강", "마음"]
            },
            "aliases": [":heart:", "heart", "red_heart"],
            "koAliases": ["하트", "빨간하트", "사랑"],
            "chosung": ["ㅎㅌ", "ㅃㄱㅎㅌ", "ㅅㄹ"],
            "searchText": "red heart heart love 빨간 하트 하트 사랑 :heart: ㅎㅌ",
            "variant": {
              "type": "emoji_presentation",
              "parentId": null,
              "skinTone": null,
              "default": true
            },
            "flags": {
              "supportsSkinTone": false,
              "isVariant": false,
              "hiddenByDefault": false
            }
          }
        ]
        """;

        var records = EmojiRepository.LoadFromJson(json);

        var record = Assert.Single(records);
        Assert.Equal("red_heart", record.Id);
        Assert.Equal("❤️", record.Emoji);
        Assert.Equal("빨간 하트", record.Name.Ko);
        Assert.Contains("ㅎㅌ", record.Chosung);
    }

    [Fact]
    public void LoadFromJson_throws_clear_exception_for_invalid_json()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => EmojiRepository.LoadFromJson("{not-json"));

        Assert.Contains("Failed to load emoji data", exception.Message);
    }
}
