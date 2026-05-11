namespace WindowsEasyEmoji.Platform.Clipboard;

public interface IClipboardPasteService
{
    bool PasteText(string text);

    void CopyText(string text);
}
