using WindowsEasyEmoji.Platform.Keyboard;

namespace WindowsEasyEmoji.Platform.Tests.Keyboard;

public sealed class KeyboardShortcutDetectorTests
{
    [Fact]
    public void ShouldHandle_detects_win_period_keydown()
    {
        var detector = new KeyboardShortcutDetector(key => key == VirtualKeys.LeftWin);
        var keyEvent = new LowLevelKeyboardEvent(
            Message: KeyboardMessages.KeyDown,
            VirtualKey: VirtualKeys.OemPeriod);

        var handled = detector.ShouldHandleWinPeriod(keyEvent);

        Assert.True(handled);
    }

    [Fact]
    public void ShouldHandle_ignores_period_without_windows_key()
    {
        var detector = new KeyboardShortcutDetector(_ => false);
        var keyEvent = new LowLevelKeyboardEvent(
            Message: KeyboardMessages.KeyDown,
            VirtualKey: VirtualKeys.OemPeriod);

        var handled = detector.ShouldHandleWinPeriod(keyEvent);

        Assert.False(handled);
    }

    [Fact]
    public void ShouldHandle_ignores_non_period_keys()
    {
        var detector = new KeyboardShortcutDetector(key => key == VirtualKeys.RightWin);
        var keyEvent = new LowLevelKeyboardEvent(
            Message: KeyboardMessages.KeyDown,
            VirtualKey: 0x41);

        var handled = detector.ShouldHandleWinPeriod(keyEvent);

        Assert.False(handled);
    }

    [Fact]
    public void ShouldHandle_tracks_windows_key_state_from_prior_hook_events()
    {
        var detector = new KeyboardShortcutDetector(_ => false);

        detector.ShouldHandleWinPeriod(new LowLevelKeyboardEvent(
            Message: KeyboardMessages.KeyDown,
            VirtualKey: VirtualKeys.LeftWin));
        var handled = detector.ShouldHandleWinPeriod(new LowLevelKeyboardEvent(
            Message: KeyboardMessages.KeyDown,
            VirtualKey: VirtualKeys.OemPeriod));

        Assert.True(handled);
    }

    [Fact]
    public void ShouldHandle_releases_tracked_windows_key_state_on_key_up()
    {
        var detector = new KeyboardShortcutDetector(_ => false);

        detector.ShouldHandleWinPeriod(new LowLevelKeyboardEvent(
            Message: KeyboardMessages.KeyDown,
            VirtualKey: VirtualKeys.LeftWin));
        detector.ShouldHandleWinPeriod(new LowLevelKeyboardEvent(
            Message: KeyboardMessages.KeyUp,
            VirtualKey: VirtualKeys.LeftWin));
        var handled = detector.ShouldHandleWinPeriod(new LowLevelKeyboardEvent(
            Message: KeyboardMessages.KeyDown,
            VirtualKey: VirtualKeys.OemPeriod));

        Assert.False(handled);
    }
}
