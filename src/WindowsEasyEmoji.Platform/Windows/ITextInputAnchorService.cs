namespace WindowsEasyEmoji.Platform.Windows;

public interface ITextInputAnchorService
{
    ScreenRectangle GetAnchorRectangle(IntPtr targetWindowHandle);
}
