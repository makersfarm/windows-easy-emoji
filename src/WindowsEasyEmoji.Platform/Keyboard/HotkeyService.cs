namespace WindowsEasyEmoji.Platform.Keyboard;

public sealed class HotkeyService : IDisposable
{
    public bool IsRegistered { get; private set; }

    public void RegisterFallbackHotkey()
    {
        IsRegistered = true;
    }

    public void UnregisterFallbackHotkey()
    {
        IsRegistered = false;
    }

    public void Dispose()
    {
        UnregisterFallbackHotkey();
    }
}
