namespace WindowsEasyEmoji.Platform.Clipboard;

public sealed record PasteResult(
    bool Pasted,
    bool TargetActivated);
