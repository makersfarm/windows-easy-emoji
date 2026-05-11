using System.Diagnostics;
using System.IO;

namespace WindowsEasyEmoji.Platform.Diagnostics;

public static class DiagnosticLog
{
    private const string LogPathEnvironmentVariable = "WINDOWS_EASY_EMOJI_E2E_LOG";
    private static readonly object Gate = new();

    public static void Write(string message)
    {
        var path = Environment.GetEnvironmentVariable(LogPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        lock (Gate)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                writer.WriteLine($"{DateTimeOffset.Now:O} pid={Environment.ProcessId} tid={Environment.CurrentManagedThreadId} {message}");
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public static string Handle(IntPtr handle)
    {
        return $"0x{handle.ToInt64():X}";
    }
}
