namespace WindowsEasyEmoji.Platform.Keyboard;

public sealed class KeyboardShortcutDetector
{
    private readonly Func<int, bool> isKeyDown;
    private bool isLeftWindowsKeyDown;
    private bool isRightWindowsKeyDown;

    public KeyboardShortcutDetector(Func<int, bool> isKeyDown)
    {
        this.isKeyDown = isKeyDown;
    }

    public bool ShouldHandleWinPeriod(LowLevelKeyboardEvent keyEvent)
    {
        UpdateWindowsKeyState(keyEvent);

        var isKeyDownMessage = keyEvent.Message is KeyboardMessages.KeyDown or KeyboardMessages.SystemKeyDown;
        var isPeriod = keyEvent.VirtualKey == VirtualKeys.OemPeriod;
        var isWindowsDown =
            isLeftWindowsKeyDown ||
            isRightWindowsKeyDown ||
            isKeyDown(VirtualKeys.LeftWin) ||
            isKeyDown(VirtualKeys.RightWin);

        return isKeyDownMessage && isPeriod && isWindowsDown;
    }

    private void UpdateWindowsKeyState(LowLevelKeyboardEvent keyEvent)
    {
        if (keyEvent.VirtualKey is not (VirtualKeys.LeftWin or VirtualKeys.RightWin))
        {
            return;
        }

        if (keyEvent.Message is KeyboardMessages.KeyDown or KeyboardMessages.SystemKeyDown)
        {
            SetWindowsKeyState(keyEvent.VirtualKey, isDown: true);
            return;
        }

        if (keyEvent.Message is KeyboardMessages.KeyUp or KeyboardMessages.SystemKeyUp)
        {
            SetWindowsKeyState(keyEvent.VirtualKey, isDown: false);
        }
    }

    private void SetWindowsKeyState(int virtualKey, bool isDown)
    {
        if (virtualKey == VirtualKeys.LeftWin)
        {
            isLeftWindowsKeyDown = isDown;
        }
        else
        {
            isRightWindowsKeyDown = isDown;
        }
    }
}
