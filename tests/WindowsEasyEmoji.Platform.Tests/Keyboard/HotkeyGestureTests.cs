using WindowsEasyEmoji.Platform.Keyboard;

namespace WindowsEasyEmoji.Platform.Tests.Keyboard;

public sealed class HotkeyGestureTests
{
    [Theory]
    [InlineData("Ctrl+Win+Space", HotkeyModifiers.Control | HotkeyModifiers.Win, 0x20)]
    [InlineData("ctrl + alt + e", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x45)]
    [InlineData("Shift+F1", HotkeyModifiers.Shift, 0x70)]
    public void Parse_supports_configured_fallback_hotkey_strings(
        string text,
        HotkeyModifiers expectedModifiers,
        int expectedVirtualKey)
    {
        var gesture = HotkeyGesture.Parse(text);

        Assert.Equal(expectedModifiers, gesture.Modifiers);
        Assert.Equal(expectedVirtualKey, gesture.VirtualKey);
    }

    [Fact]
    public void Parse_rejects_unknown_key_names()
    {
        var exception = Assert.Throws<ArgumentException>(() => HotkeyGesture.Parse("Ctrl+Nope"));

        Assert.Contains("Unsupported hotkey key", exception.Message);
    }
}
