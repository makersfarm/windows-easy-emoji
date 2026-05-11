namespace WindowsEasyEmoji.Platform.Keyboard;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

public sealed record HotkeyGesture(HotkeyModifiers Modifiers, int VirtualKey)
{
    public static HotkeyGesture Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Hotkey cannot be empty.", nameof(text));
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException("Hotkey cannot be empty.", nameof(text));
        }

        var modifiers = HotkeyModifiers.None;
        int? virtualKey = null;

        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "alt":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "shift":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "win":
                case "windows":
                    modifiers |= HotkeyModifiers.Win;
                    break;
                default:
                    virtualKey = ParseVirtualKey(part);
                    break;
            }
        }

        if (virtualKey is null)
        {
            throw new ArgumentException("Hotkey must include a non-modifier key.", nameof(text));
        }

        return new HotkeyGesture(modifiers, virtualKey.Value);
    }

    private static int ParseVirtualKey(string key)
    {
        if (key.Equals("space", StringComparison.OrdinalIgnoreCase))
        {
            return 0x20;
        }

        if (key.Length == 1)
        {
            var character = char.ToUpperInvariant(key[0]);
            if (character is >= 'A' and <= 'Z')
            {
                return character;
            }

            if (character is >= '0' and <= '9')
            {
                return character;
            }
        }

        if (key.Length is 2 or 3 &&
            key[0] is 'f' or 'F' &&
            int.TryParse(key[1..], out var functionKeyNumber) &&
            functionKeyNumber is >= 1 and <= 24)
        {
            return 0x6F + functionKeyNumber;
        }

        throw new ArgumentException($"Unsupported hotkey key '{key}'.", nameof(key));
    }
}
