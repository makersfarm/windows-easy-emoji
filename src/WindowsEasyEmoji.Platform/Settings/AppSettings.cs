namespace WindowsEasyEmoji.Platform.Settings;

public sealed record AppSettings(
    bool ReplaceWinPeriod,
    bool AutoPaste,
    bool RegisterFallbackHotkey,
    bool RestoreClipboardAfterPaste,
    string FallbackHotkey)
{
    public static AppSettings Default { get; } = new(
        ReplaceWinPeriod: true,
        AutoPaste: true,
        RegisterFallbackHotkey: true,
        RestoreClipboardAfterPaste: false,
        FallbackHotkey: "Ctrl+Alt+Space");
}
