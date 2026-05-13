using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WindowsEasyEmoji.Core.Emoji;
using WindowsEasyEmoji.Core.Search;

namespace WindowsEasyEmoji.DataBuilder;

public static partial class EmojiDataBuilder
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IReadOnlyList<EmojiRecord> Build(DataSourcePaths paths)
    {
        var unicodeRows = ReadUnicodeRows(paths.UnicodeEmojiTestPath);
        var directAnnotations = ReadAnnotations(paths.CldrKoreanAnnotationsPath, "annotations");
        var derivedAnnotations = ReadAnnotations(paths.CldrKoreanDerivedAnnotationsPath, "annotationsDerived");
        var emojiKorean = ReadEmojiKorean(paths.EmojiKoreanPath);
        var badrex = ReadBadrex(paths.BadrexRowsJsonlPath);
        var muan = ReadMuan(paths.MuanDataByEmojiPath);
        var curated = ReadCurated(paths.CuratedEmojiPath);

        var records = new List<EmojiRecord>(unicodeRows.Count);
        var usedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in unicodeRows)
        {
            var key = NormalizeEmojiKey(row.Emoji);
            directAnnotations.TryGetValue(row.Emoji, out var direct);
            directAnnotations.TryGetValue(key, out var normalizedDirect);
            derivedAnnotations.TryGetValue(row.Emoji, out var derived);
            derivedAnnotations.TryGetValue(key, out var normalizedDerived);
            emojiKorean.TryGetValue(key, out var koreanSeed);
            badrex.TryGetValue(row.Emoji, out var semanticSeed);
            muan.TryGetValue(row.Emoji, out var muanRecord);
            muan.TryGetValue(key, out var normalizedMuanRecord);
            curated.TryGetValue(row.Emoji, out var curatedRecord);
            curated.TryGetValue(key, out var normalizedCuratedRecord);

            var sourceCurated = curatedRecord ?? normalizedCuratedRecord;
            var sourceMuan = muanRecord ?? normalizedMuanRecord;
            var annotation = direct ?? normalizedDirect ?? derived ?? normalizedDerived;
            var id = MakeUniqueId(sourceCurated?.Id ?? sourceMuan?.Slug ?? Slugify(row.Name), usedIds);
            var englishName = sourceMuan?.Name ?? row.Name.ToLowerInvariant();
            var koreanName = annotation?.Tts.FirstOrDefault() ?? koreanSeed ?? sourceCurated?.Name.Ko ?? englishName;
            var isSkinToneVariant = row.Codepoints.Any(IsSkinToneModifier);
            var supportsSkinTone = sourceMuan?.SkinToneSupport == true && !isSkinToneVariant;

            var koKeywords = MergeTerms(
                annotation?.DefaultTerms ?? [],
                annotation?.Tts ?? [],
                SplitKoreanSeed(koreanSeed),
                sourceCurated?.Keywords.TryGetValue("ko", out var curatedKoKeywords) == true ? curatedKoKeywords : []);
            var enKeywords = MergeTerms(
                SplitEnglishName(englishName),
                semanticSeed?.Tags ?? [],
                SplitEnglishName(semanticSeed?.ShortDescription),
                sourceCurated?.Keywords.TryGetValue("en", out var curatedEnKeywords) == true ? curatedEnKeywords : []);
            var aliases = MergeTerms(
                [sourceMuan is null ? $":{id}:" : $":{sourceMuan.Slug}:", sourceMuan?.Slug ?? id],
                sourceCurated?.Aliases ?? []);
            var koAliases = MergeTerms(
                annotation?.Tts ?? [],
                koreanSeed is null ? [] : [koreanSeed],
                sourceCurated?.KoAliases ?? []);
            var chosung = BuildChosung(koKeywords.Concat(koAliases).Append(koreanName));
            var searchText = string.Join(' ', MergeTerms(
                [englishName, koreanName, row.Emoji],
                enKeywords,
                koKeywords,
                aliases,
                koAliases,
                chosung));

            records.Add(new EmojiRecord(
                Id: id,
                Emoji: row.Emoji,
                BaseEmoji: key,
                Unicode: row.Codepoints.Select(codepoint => codepoint.ToString("X")).ToArray(),
                Version: sourceMuan?.EmojiVersion ?? row.EmojiVersion,
                Category: row.Group,
                Group: row.Subgroup,
                Order: row.Order,
                Name: new LocalizedText(englishName, koreanName),
                Keywords: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["en"] = enKeywords,
                    ["ko"] = koKeywords
                },
                Aliases: aliases,
                KoAliases: koAliases,
                Chosung: chosung,
                SearchText: searchText,
                Variant: new EmojiVariant(
                    isSkinToneVariant ? "skin_tone" : "emoji_presentation",
                    isSkinToneVariant ? Slugify(row.NameWithoutSkinTone) : null,
                    isSkinToneVariant ? row.SkinToneName : null,
                    !isSkinToneVariant),
                Flags: new EmojiFlags(supportsSkinTone, isSkinToneVariant, isSkinToneVariant)));
        }

        return records;
    }

    public static void WriteJson(IEnumerable<EmojiRecord> records, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, JsonSerializer.Serialize(records, WriteOptions) + Environment.NewLine);
    }

    private static IReadOnlyList<UnicodeEmojiRow> ReadUnicodeRows(string path)
    {
        var rows = new List<UnicodeEmojiRow>();
        var group = string.Empty;
        var subgroup = string.Empty;
        var order = 0;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("# group:", StringComparison.Ordinal))
            {
                group = line["# group:".Length..].Trim();
                continue;
            }

            if (line.StartsWith("# subgroup:", StringComparison.Ordinal))
            {
                subgroup = line["# subgroup:".Length..].Trim();
                continue;
            }

            if (line.Length == 0 || line.StartsWith('#') || !line.Contains(';'))
            {
                continue;
            }

            var parts = line.Split('#', 2);
            var left = parts[0].Split(';', 2);
            var status = left[1].Trim();
            if (!status.StartsWith("fully-qualified", StringComparison.Ordinal))
            {
                continue;
            }

            var codepoints = left[0]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => Convert.ToInt32(value, 16))
                .ToArray();
            var commentParts = parts[1].Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            var emoji = string.Concat(codepoints.Select(char.ConvertFromUtf32));
            var emojiVersion = commentParts.Length > 1 && commentParts[1].StartsWith('E') ? commentParts[1][1..] : string.Empty;
            var name = commentParts.Length > 2 ? commentParts[2] : emoji;

            rows.Add(new UnicodeEmojiRow(
                Emoji: emoji,
                Codepoints: codepoints,
                EmojiVersion: emojiVersion,
                Group: group,
                Subgroup: subgroup,
                Name: name,
                Order: order++));
        }

        return rows;
    }

    private static IReadOnlyDictionary<string, KoreanAnnotation> ReadAnnotations(string path, string rootName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement.GetProperty(rootName).GetProperty("annotations");
        var annotations = new Dictionary<string, KoreanAnnotation>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            var defaultTerms = ReadStringArray(property.Value, "default");
            var tts = ReadStringArray(property.Value, "tts");
            annotations[property.Name] = new KoreanAnnotation(defaultTerms, tts);
        }

        return annotations;
    }

    private static IReadOnlyDictionary<string, string> ReadEmojiKorean(string path)
    {
        var rows = JsonSerializer.Deserialize<List<EmojiKoreanRow>>(File.ReadAllText(path), ReadOptions) ?? [];
        return rows
            .GroupBy(row => NormalizeEmojiKey(row.Emoji), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Description, StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, BadrexRow> ReadBadrex(string path)
    {
        var rows = new Dictionary<string, BadrexRow>(StringComparer.Ordinal);
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var row = JsonSerializer.Deserialize<BadrexRow>(line, ReadOptions);
            if (row is not null)
            {
                rows[row.Character] = row;
            }
        }

        return rows;
    }

    private static IReadOnlyDictionary<string, MuanRow> ReadMuan(string path)
    {
        var rows = JsonSerializer.Deserialize<Dictionary<string, MuanRow>>(File.ReadAllText(path), ReadOptions) ?? [];
        return rows;
    }

    private static IReadOnlyDictionary<string, EmojiRecord> ReadCurated(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return new Dictionary<string, EmojiRecord>(StringComparer.Ordinal);
        }

        return EmojiRepository.LoadFromJson(File.ReadAllText(path))
            .Where(record => !record.Id.StartsWith("emoji_korean_", StringComparison.Ordinal) &&
                             !record.Variant.Type.Equals("external_seed", StringComparison.OrdinalIgnoreCase))
            .GroupBy(record => NormalizeEmojiKey(record.Emoji), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray()
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static IReadOnlyList<string> MergeTerms(params IEnumerable<string>[] termGroups)
    {
        var terms = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var term in termGroups.SelectMany(group => group))
        {
            var normalized = KoreanTextNormalizer.NormalizeQuery(term);
            if (normalized.Length > 0 && seen.Add(normalized))
            {
                terms.Add(normalized);
            }
        }

        return terms;
    }

    private static IReadOnlyList<string> BuildChosung(IEnumerable<string> values)
    {
        return MergeTerms(values.Select(KoreanTextNormalizer.ToChosung));
    }

    private static IEnumerable<string> SplitKoreanSeed(string? value)
    {
        return value is null ? [] : KoreanTextNormalizer.ExtractTokens(value).Append(value);
    }

    private static IEnumerable<string> SplitEnglishName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return EnglishTokenRegex()
            .Matches(value.ToLowerInvariant())
            .Select(match => match.Value)
            .Append(value.ToLowerInvariant());
    }

    private static string NormalizeEmojiKey(string emoji)
    {
        return emoji.Replace("\uFE0F", string.Empty, StringComparison.Ordinal);
    }

    private static string Slugify(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.ToLowerInvariant())
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }
        }

        return builder.ToString().Trim('_');
    }

    private static string MakeUniqueId(string preferredId, HashSet<string> usedIds)
    {
        var id = Slugify(preferredId);
        if (usedIds.Add(id))
        {
            return id;
        }

        var index = 2;
        while (!usedIds.Add($"{id}_{index}"))
        {
            index++;
        }

        return $"{id}_{index}";
    }

    private static bool IsSkinToneModifier(int codepoint)
    {
        return codepoint is >= 0x1F3FB and <= 0x1F3FF;
    }

    [GeneratedRegex("[a-z0-9]+")]
    private static partial Regex EnglishTokenRegex();

    private sealed partial record UnicodeEmojiRow(
        string Emoji,
        IReadOnlyList<int> Codepoints,
        string EmojiVersion,
        string Group,
        string Subgroup,
        string Name,
        int Order)
    {
        public string NameWithoutSkinTone => SkinTonePhraseRegex().Replace(Name, string.Empty).Trim();
        public string? SkinToneName => SkinTonePhraseRegex().Match(Name) is { Success: true } match ? match.Value : null;

        [GeneratedRegex(":\\s*(light|medium-light|medium|medium-dark|dark) skin tone", RegexOptions.IgnoreCase)]
        private static partial Regex SkinTonePhraseRegex();
    }

    private sealed record KoreanAnnotation(IReadOnlyList<string> DefaultTerms, IReadOnlyList<string> Tts);

    private sealed record EmojiKoreanRow(string Emoji, string Hexcode, string Description);

    private sealed record BadrexRow(
        string Character,
        string Unicode,
        string ShortDescription,
        IReadOnlyList<string> Tags,
        string LlmDescription);

    private sealed record MuanRow(
        string Name,
        string Slug,
        string Group,
        [property: JsonPropertyName("emoji_version")]
        string EmojiVersion,
        [property: JsonPropertyName("unicode_version")]
        string UnicodeVersion,
        [property: JsonPropertyName("skin_tone_support")]
        bool SkinToneSupport);
}
