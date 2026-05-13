using System.Runtime.InteropServices;
using WindowsEasyEmoji.Platform.Diagnostics;

namespace WindowsEasyEmoji.Platform.Windows;

public sealed class TextInputAnchorService : ITextInputAnchorService
{
    public ScreenRectangle GetAnchorRectangle(IntPtr targetWindowHandle)
    {
        if (TryGetCaretRectangle(targetWindowHandle, out var caretRectangle))
        {
            DiagnosticLog.Write($"text-anchor source=caret target={DiagnosticLog.Handle(targetWindowHandle)} rect={Format(caretRectangle)}");
            return caretRectangle;
        }

        if (targetWindowHandle != IntPtr.Zero && GetWindowRect(targetWindowHandle, out var windowRectangle))
        {
            var rectangle = ToScreenRectangle(windowRectangle);
            DiagnosticLog.Write($"text-anchor source=window target={DiagnosticLog.Handle(targetWindowHandle)} rect={Format(rectangle)}");
            return rectangle;
        }

        if (GetCursorPos(out var cursorPoint))
        {
            var rectangle = new ScreenRectangle(cursorPoint.X, cursorPoint.Y, cursorPoint.X + 1, cursorPoint.Y + 1);
            DiagnosticLog.Write($"text-anchor source=cursor target={DiagnosticLog.Handle(targetWindowHandle)} rect={Format(rectangle)}");
            return rectangle;
        }

        DiagnosticLog.Write($"text-anchor source=origin target={DiagnosticLog.Handle(targetWindowHandle)}");
        return new ScreenRectangle(0, 0, 1, 1);
    }

    private static bool TryGetCaretRectangle(IntPtr targetWindowHandle, out ScreenRectangle rectangle)
    {
        rectangle = default;
        if (targetWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        var threadId = GetWindowThreadProcessId(targetWindowHandle, out _);
        if (threadId == 0)
        {
            return false;
        }

        var info = new GuiThreadInfo
        {
            Size = Marshal.SizeOf<GuiThreadInfo>()
        };
        if (!GetGUIThreadInfo(threadId, ref info) || info.HwndCaret == IntPtr.Zero)
        {
            return false;
        }

        var topLeft = new NativePoint(info.CaretRectangle.Left, info.CaretRectangle.Top);
        var bottomRight = new NativePoint(
            Math.Max(info.CaretRectangle.Right, info.CaretRectangle.Left + 1),
            Math.Max(info.CaretRectangle.Bottom, info.CaretRectangle.Top + 1));
        if (!ClientToScreen(info.HwndCaret, ref topLeft) || !ClientToScreen(info.HwndCaret, ref bottomRight))
        {
            return false;
        }

        rectangle = new ScreenRectangle(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        return true;
    }

    private static ScreenRectangle ToScreenRectangle(NativeRectangle rectangle)
    {
        return new ScreenRectangle(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
    }

    private static string Format(ScreenRectangle rectangle)
    {
        return $"{rectangle.Left:0},{rectangle.Top:0},{rectangle.Right:0},{rectangle.Bottom:0}";
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo guiThreadInfo);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRectangle rectangle);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size;
        public int Flags;
        public IntPtr HwndActive;
        public IntPtr HwndFocus;
        public IntPtr HwndCapture;
        public IntPtr HwndMenuOwner;
        public IntPtr HwndMoveSize;
        public IntPtr HwndCaret;
        public NativeRectangle CaretRectangle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public NativePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
