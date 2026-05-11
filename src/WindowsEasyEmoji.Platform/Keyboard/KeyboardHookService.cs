namespace WindowsEasyEmoji.Platform.Keyboard;

public sealed class KeyboardHookService : IDisposable
{
    public bool IsRunning { get; private set; }

    public event EventHandler? WinPeriodPressed;

    public void Start()
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Dispose()
    {
        Stop();
    }

    internal void RaiseWinPeriodPressedForTesting()
    {
        WinPeriodPressed?.Invoke(this, EventArgs.Empty);
    }
}
