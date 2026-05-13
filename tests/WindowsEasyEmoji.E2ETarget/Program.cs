using System.Text;
using System.Windows.Forms;

var options = TargetOptions.Parse(args);

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.Run(new TargetForm(options));

internal sealed class TargetForm : Form
{
    private readonly TargetOptions options;
    private readonly TextBox textBox = new()
    {
        AcceptsReturn = true,
        Dock = DockStyle.Fill,
        Font = new System.Drawing.Font("Segoe UI Emoji", 24),
        Multiline = true,
        Name = "E2ETextBox"
    };

    public TargetForm(TargetOptions options)
    {
        this.options = options;
        Text = options.Title;
        Width = options.Width ?? 640;
        Height = options.Height ?? 320;
        if (options.Left is not null && options.Top is not null)
        {
            StartPosition = FormStartPosition.Manual;
            Left = options.Left.Value;
            Top = options.Top.Value;
        }
        Controls.Add(textBox);

        Shown += (_, _) =>
        {
            TopMost = true;
            Activate();
            textBox.Focus();
            TopMost = false;
            WriteFile(options.ReadyFile, Handle.ToInt64().ToString());
            Log("ready");
        };

        Activated += (_, _) => textBox.Focus();
        textBox.TextChanged += (_, _) =>
        {
            WriteFile(options.TextFile, textBox.Text);
            Log($"text-changed:{textBox.Text}");
        };
    }

    private void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(options.LogFile))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(options.LogFile)!);
        File.AppendAllText(
            options.LogFile,
            $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}",
            Encoding.UTF8);
    }

    private static void WriteFile(string path, string value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, value, Encoding.UTF8);
    }
}

internal sealed record TargetOptions(
    string Title,
    string ReadyFile,
    string TextFile,
    string LogFile,
    int? Left,
    int? Top,
    int? Width,
    int? Height)
{
    public static TargetOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length - 1; index += 2)
        {
            values[args[index]] = args[index + 1];
        }

        return new TargetOptions(
            Read(values, "--title"),
            Read(values, "--ready-file"),
            Read(values, "--text-file"),
            values.GetValueOrDefault("--log-file", string.Empty),
            ReadOptionalInt(values, "--left"),
            ReadOptionalInt(values, "--top"),
            ReadOptionalInt(values, "--width"),
            ReadOptionalInt(values, "--height"));
    }

    private static string Read(Dictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required argument {key}.");
    }

    private static int? ReadOptionalInt(Dictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) && int.TryParse(value, out var parsedValue)
            ? parsedValue
            : null;
    }
}
