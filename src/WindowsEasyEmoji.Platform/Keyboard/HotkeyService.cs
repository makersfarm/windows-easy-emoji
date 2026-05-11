using System.Runtime.InteropServices;
using System.Windows.Interop;
using WindowsEasyEmoji.Platform.Diagnostics;

namespace WindowsEasyEmoji.Platform.Keyboard;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x454D4F4A;
    private const int WmHotkey = 0x0312;

    private HwndSource? source;
    public bool IsRegistered { get; private set; }

    public event EventHandler? HotkeyPressed;

    public void RegisterFallbackHotkey()
    {
        RegisterFallbackHotkey(HotkeyGesture.Parse("Ctrl+Alt+Space"));
    }

    public void RegisterFallbackHotkey(HotkeyGesture gesture)
    {
        if (IsRegistered)
        {
            return;
        }

        source = new HwndSource(new HwndSourceParameters("WindowsEasyEmojiHotkeySink")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        });

        source.AddHook(WndProc);
        IsRegistered = RegisterHotKey(source.Handle, HotkeyId, (uint)gesture.Modifiers, (uint)gesture.VirtualKey);
        DiagnosticLog.Write($"hotkey.register hwnd={DiagnosticLog.Handle(source.Handle)} modifiers={gesture.Modifiers} vk={gesture.VirtualKey} result={IsRegistered}");
        if (!IsRegistered)
        {
            source.RemoveHook(WndProc);
            source.Dispose();
            source = null;
        }
    }

    public void UnregisterFallbackHotkey()
    {
        if (source is not null)
        {
            UnregisterHotKey(source.Handle, HotkeyId);
            source.RemoveHook(WndProc);
            source.Dispose();
            source = null;
        }

        IsRegistered = false;
    }

    public void Dispose()
    {
        UnregisterFallbackHotkey();
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            DiagnosticLog.Write("hotkey.pressed");
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
