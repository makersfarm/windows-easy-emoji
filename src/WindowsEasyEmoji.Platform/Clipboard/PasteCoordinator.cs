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
        var targetActivated = false;
        if (targetWindowHandle != IntPtr.Zero)
        {
            targetActivated = foregroundWindowService.TryActivateWindow(targetWindowHandle);
        }

        var pasted = clipboardPasteService.PasteText(text);
        if (!pasted)
        {
            clipboardPasteService.CopyText(text);
        }

        return new PasteResult(pasted, targetActivated);
    }
}
