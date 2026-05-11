using System.Runtime.InteropServices;

namespace WindowsEasyEmoji.Platform.Clipboard;

public sealed class ClipboardPasteService : IClipboardPasteService
{
    private string? originalText;

    public bool PasteText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        originalText = TryGetClipboardText();
        System.Windows.Clipboard.SetText(text);
        return SendCtrlV();
    }

    public void CopyText(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            System.Windows.Clipboard.SetText(text);
        }
    }

    public void RestoreOriginalClipboard()
    {
        if (originalText is null)
        {
            return;
        }

        Thread.Sleep(650);
        System.Windows.Clipboard.SetText(originalText);
        originalText = null;
    }

    private static string? TryGetClipboardText()
    {
        try
        {
            return System.Windows.Clipboard.ContainsText()
                ? System.Windows.Clipboard.GetText()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool SendCtrlV()
    {
        var inputs = new[]
        {
            KeyboardInput(VirtualKey.Control, 0),
            KeyboardInput(VirtualKey.V, 0),
            KeyboardInput(VirtualKey.V, KeyEventFlags.KeyUp),
            KeyboardInput(VirtualKey.Control, KeyEventFlags.KeyUp)
        };

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static Input KeyboardInput(ushort virtualKey, KeyEventFlags flags)
    {
        return new Input
        {
            Type = InputType.Keyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInputData
                {
                    VirtualKey = virtualKey,
                    ScanCode = 0,
                    Flags = flags,
                    Time = 0,
                    ExtraInfo = UIntPtr.Zero
                }
            }
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    private static class VirtualKey
    {
        public const ushort Control = 0x11;
        public const ushort V = 0x56;
    }

    private enum InputType : uint
    {
        Keyboard = 1
    }

    [Flags]
    private enum KeyEventFlags : uint
    {
        KeyUp = 0x0002
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public InputType Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInputData Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputData
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public KeyEventFlags Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }
}
