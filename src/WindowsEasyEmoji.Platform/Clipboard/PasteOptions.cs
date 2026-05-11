namespace WindowsEasyEmoji.Platform.Clipboard;

public sealed record PasteOptions(
    bool AutoPaste = true,
    bool RestoreOriginalClipboard = false)
{
    public static PasteOptions Default { get; } = new();
}
