using WindowsEasyEmoji.Platform.Windows;

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
        if (!options.AutoPaste)
        {
            clipboardPasteService.CopyText(text);
            return new PasteResult(Pasted: false, TargetActivated: false);
        }

        var targetActivated = false;
        if (targetWindowHandle != IntPtr.Zero)
        {
            targetActivated = foregroundWindowService.TryActivateWindow(targetWindowHandle);
            if (!targetActivated)
            {
                clipboardPasteService.CopyText(text);
                return new PasteResult(Pasted: false, TargetActivated: false);
            }
        }

        var pasted = clipboardPasteService.PasteText(text);
        if (pasted && options.RestoreOriginalClipboard)
        {
            clipboardPasteService.RestoreOriginalClipboard();
        }

        if (!pasted)
        {
            clipboardPasteService.CopyText(text);
        }

        return new PasteResult(pasted, targetActivated);
    }
}
