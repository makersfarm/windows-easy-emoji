using WindowsEasyEmoji.Platform.Windows;
using WindowsEasyEmoji.Platform.Diagnostics;

namespace WindowsEasyEmoji.Platform.Clipboard;

public sealed class PasteCoordinator
{
    private readonly IForegroundWindowService foregroundWindowService;
    private readonly IClipboardPasteService clipboardPasteService;

    public PasteCoordinator(
        IForegroundWindowService foregroundWindowService,
        IClipboardPasteService clipboardPasteService)
    {
        this.foregroundWindowService = foregroundWindowService;
        this.clipboardPasteService = clipboardPasteService;
    }

    public PasteResult PasteToTarget(IntPtr targetWindowHandle, string text)
    {
        return PasteToTarget(targetWindowHandle, text, PasteOptions.Default);
    }

    public PasteResult PasteToTarget(IntPtr targetWindowHandle, string text, PasteOptions options)
    {
        DiagnosticLog.Write(
            $"paste.start target={DiagnosticLog.Handle(targetWindowHandle)} textLength={text.Length} autoPaste={options.AutoPaste} restore={options.RestoreOriginalClipboard}");

        if (!options.AutoPaste)
        {
            clipboardPasteService.CopyText(text);
            DiagnosticLog.Write("paste.copy-only");
            return new PasteResult(Pasted: false, TargetActivated: false);
        }

        var targetActivated = false;
        if (targetWindowHandle != IntPtr.Zero)
        {
            targetActivated = foregroundWindowService.TryActivateWindow(targetWindowHandle);
        }

        var pasted = clipboardPasteService.PasteText(text);
        if (pasted && options.RestoreOriginalClipboard)
        {
            clipboardPasteService.RestoreOriginalClipboard();
        }

        if (!pasted)
        {
            clipboardPasteService.CopyText(text);
            DiagnosticLog.Write("paste.fallback-copy");
        }

        DiagnosticLog.Write($"paste.complete pasted={pasted} targetActivated={targetActivated}");
        return new PasteResult(pasted, targetActivated);
    }
}
