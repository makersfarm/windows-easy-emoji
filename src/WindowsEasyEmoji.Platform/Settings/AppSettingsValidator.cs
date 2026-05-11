using WindowsEasyEmoji.Platform.Keyboard;

namespace WindowsEasyEmoji.Platform.Settings;

public static class AppSettingsValidator
{
    public static AppSettings Normalize(AppSettings settings)
    {
        var normalizedHotkey = string.Join(
            '+',
            settings.FallbackHotkey
                .Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        return settings with { FallbackHotkey = normalizedHotkey };
    }

    public static bool TryValidate(AppSettings settings, out string errorMessage)
    {
        var normalized = Normalize(settings);
        if (string.IsNullOrWhiteSpace(normalized.FallbackHotkey))
        {
            errorMessage = "Fallback hotkey를 입력하세요.";
            return false;
        }

        try
        {
            HotkeyGesture.Parse(normalized.FallbackHotkey);
        }
        catch (ArgumentException)
        {
            errorMessage = "지원하지 않는 fallback hotkey입니다.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
