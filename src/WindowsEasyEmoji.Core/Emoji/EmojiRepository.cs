using System.Text.Json;

namespace WindowsEasyEmoji.Core.Emoji;

public static class EmojiRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyList<EmojiRecord> LoadFromJson(string json)
    {
        try
        {
            var records = JsonSerializer.Deserialize<List<EmojiRecord>>(json, JsonOptions);
            if (records is null)
            {
                throw new InvalidOperationException("Emoji data root must be a JSON array.");
            }

            return records;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to load emoji data.", exception);
        }
    }
}
