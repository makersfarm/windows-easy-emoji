using WindowsEasyEmoji.Platform.Clipboard;
using WindowsEasyEmoji.Platform.Windows;

namespace WindowsEasyEmoji.Platform.Tests.Clipboard;

public sealed class PasteCoordinatorTests
{
    [Fact]
    public void PasteToTarget_activates_target_window_before_pasting_text()
    {
        var events = new List<string>();
        var foreground = new RecordingForegroundWindowService(events);
        var clipboard = new RecordingClipboardPasteService(events, pasteResult: true);
        var coordinator = new PasteCoordinator(foreground, clipboard);
        var target = new IntPtr(1234);

        var result = coordinator.PasteToTarget(target, "❤️");

        Assert.True(result.Pasted);
        Assert.True(result.TargetActivated);
        Assert.Equal(["activate:1234", "paste:❤️"], events);
    }

    [Fact]
    public void PasteToTarget_skips_activation_for_empty_target_window()
    {
        var events = new List<string>();
        var foreground = new RecordingForegroundWindowService(events);
        var clipboard = new RecordingClipboardPasteService(events, pasteResult: true);
        var coordinator = new PasteCoordinator(foreground, clipboard);

        var result = coordinator.PasteToTarget(IntPtr.Zero, "😂");

        Assert.True(result.Pasted);
        Assert.False(result.TargetActivated);
        Assert.Equal(["paste:😂"], events);
    }

    [Fact]
    public void PasteToTarget_copies_text_when_send_input_paste_fails()
    {
        var events = new List<string>();
        var foreground = new RecordingForegroundWindowService(events);
        var clipboard = new RecordingClipboardPasteService(events, pasteResult: false);
        var coordinator = new PasteCoordinator(foreground, clipboard);

        var result = coordinator.PasteToTarget(new IntPtr(99), "🔥");

        Assert.False(result.Pasted);
        Assert.True(result.TargetActivated);
        Assert.Equal(["activate:99", "paste:🔥", "copy:🔥"], events);
    }

    [Fact]
    public void PasteToTarget_copies_text_without_pasting_when_target_activation_fails()
    {
        var events = new List<string>();
        var foreground = new RecordingForegroundWindowService(events, activationResult: false);
        var clipboard = new RecordingClipboardPasteService(events, pasteResult: true);
        var coordinator = new PasteCoordinator(foreground, clipboard);

        var result = coordinator.PasteToTarget(new IntPtr(55), "⭐");

        Assert.False(result.Pasted);
        Assert.False(result.TargetActivated);
        Assert.Equal(["activate:55", "copy:⭐"], events);
    }

    [Fact]
    public void PasteToTarget_requests_original_clipboard_restore_when_enabled_and_paste_succeeds()
    {
        var events = new List<string>();
        var foreground = new RecordingForegroundWindowService(events);
        var clipboard = new RecordingClipboardPasteService(events, pasteResult: true);
        var coordinator = new PasteCoordinator(foreground, clipboard);

        var result = coordinator.PasteToTarget(
            new IntPtr(77),
            "👍",
            new PasteOptions(RestoreOriginalClipboard: true));

        Assert.True(result.Pasted);
        Assert.Equal(["activate:77", "paste:👍", "restore"], events);
    }

    [Fact]
    public void PasteToTarget_copies_text_without_activating_target_when_auto_paste_is_disabled()
    {
        var events = new List<string>();
        var foreground = new RecordingForegroundWindowService(events);
        var clipboard = new RecordingClipboardPasteService(events, pasteResult: true);
        var coordinator = new PasteCoordinator(foreground, clipboard);

        var result = coordinator.PasteToTarget(
            new IntPtr(88),
            "✅",
            new PasteOptions(AutoPaste: false));

        Assert.False(result.Pasted);
        Assert.False(result.TargetActivated);
        Assert.Equal(["copy:✅"], events);
    }

    private sealed class RecordingForegroundWindowService : IForegroundWindowService
    {
        private readonly List<string> events;

        private readonly bool activationResult;

        public RecordingForegroundWindowService(List<string> events, bool activationResult = true)
        {
            this.events = events;
            this.activationResult = activationResult;
        }

        public IntPtr GetForegroundWindowHandle()
        {
            return new IntPtr(42);
        }

        public bool TryActivateWindow(IntPtr windowHandle)
        {
            events.Add($"activate:{windowHandle}");
            return activationResult;
        }
    }

    private sealed class RecordingClipboardPasteService : IClipboardPasteService
    {
        private readonly List<string> events;
        private readonly bool pasteResult;

        public RecordingClipboardPasteService(List<string> events, bool pasteResult)
        {
            this.events = events;
            this.pasteResult = pasteResult;
        }

        public bool PasteText(string text)
        {
            events.Add($"paste:{text}");
            return pasteResult;
        }

        public void CopyText(string text)
        {
            events.Add($"copy:{text}");
        }

        public void RestoreOriginalClipboard()
        {
            events.Add("restore");
        }
    }
}
