using System.Runtime.InteropServices;
using WindowsEasyEmoji.Platform.Diagnostics;

namespace WindowsEasyEmoji.Platform.Clipboard;

public sealed class ClipboardPasteService : IClipboardPasteService
{
    private string? originalText;

    public bool PasteText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            DiagnosticLog.Write("clipboard.paste skipped-empty-text");
            return false;
        }

        DiagnosticLog.Write($"clipboard.paste start textLength={text.Length}");
        originalText = TryGetClipboardText();
        System.Windows.Clipboard.SetText(text);
        DiagnosticLog.Write($"clipboard.paste set-text originalTextPresent={originalText is not null}");
        var result = SendCtrlV();
        DiagnosticLog.Write($"clipboard.paste send-ctrl-v result={result}");
        return result;
    }

    public void CopyText(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            DiagnosticLog.Write($"clipboard.copy textLength={text.Length}");
            System.Windows.Clipboard.SetText(text);
        }
    }

    public void RestoreOriginalClipboard()
    {
        if (originalText is null)
        {
            DiagnosticLog.Write("clipboard.restore skipped-no-original");
            return;
        }

        DiagnosticLog.Write("clipboard.restore start");
        Thread.Sleep(650);
        System.Windows.Clipboard.SetText(originalText);
        originalText = null;
        DiagnosticLog.Write("clipboard.restore complete");
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

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
        {
            DiagnosticLog.Write($"clipboard.send-input sent={sent} expected={inputs.Length} error={Marshal.GetLastWin32Error()}");
        }

        return sent == inputs.Length;
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
        public MouseInputData Mouse;

        [FieldOffset(0)]
        public KeyboardInputData Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
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
