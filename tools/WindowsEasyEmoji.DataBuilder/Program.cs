using WindowsEasyEmoji.DataBuilder;

var repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());
var outputPath = Path.Combine(repositoryRoot, "src", "WindowsEasyEmoji.App", "Data", "emoji.json");

for (var index = 0; index < args.Length; index++)
{
    if (args[index] == "--output" && index + 1 < args.Length)
    {
        outputPath = Path.GetFullPath(args[++index]);
    }
}

var records = EmojiDataBuilder.Build(DataSourcePaths.FromRepositoryRoot(repositoryRoot));
EmojiDataBuilder.WriteJson(records, outputPath);
Console.WriteLine($"Wrote {records.Count} emoji records to {outputPath}");

static string FindRepositoryRoot(string startDirectory)
{
    var directory = new DirectoryInfo(startDirectory);
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
