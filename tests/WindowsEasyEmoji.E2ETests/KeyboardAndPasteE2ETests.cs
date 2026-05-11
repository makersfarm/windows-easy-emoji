using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Xunit.Abstractions;

namespace WindowsEasyEmoji.E2ETests;

[Collection("Windows desktop E2E")]
public sealed class KeyboardAndPasteE2ETests
{
    private readonly ITestOutputHelper output;

    public KeyboardAndPasteE2ETests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [UiE2EFact]
    public async Task WinPeriodShortcut_shows_search_overlay()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();

        var overlayHandle = session.WaitForOverlayWindow();

        Assert.NotEqual(IntPtr.Zero, overlayHandle);
        Assert.Contains("send-win-period", File.ReadAllText(session.DriverLogPath, Encoding.UTF8));
    }

    [UiE2EFact]
    public async Task Enter_after_win_period_pastes_selected_emoji_into_previously_focused_window()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.SendEnter();

        var pastedText = session.WaitForTargetText("❤️");

        Assert.Equal("❤️", pastedText);
    }

    [UiE2EFact]
    public async Task Fallback_hotkey_shows_search_overlay()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendFallbackHotkey();

        var overlayHandle = session.WaitForOverlayWindow();

        Assert.NotEqual(IntPtr.Zero, overlayHandle);
        Assert.Contains("app.shortcut-dispatch sender=HotkeyService", File.ReadAllText(session.AppLogPath, Encoding.UTF8));
    }

    [UiE2EFact]
    public async Task Korean_alias_query_pastes_matching_emoji()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText("ㅋㅋ");
        session.SendEnter();

        var pastedText = session.WaitForTargetText("😂");

        Assert.Equal("😂", pastedText);
        Assert.Contains("emojiId=face_with_tears_of_joy", File.ReadAllText(session.AppLogPath, Encoding.UTF8));
    }

    [UiE2EFact]
    public async Task Chosung_query_pastes_matching_emoji()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText("ㄸㅂ");
        session.SendEnter();

        var pastedText = session.WaitForTargetText("👍");

        Assert.Equal("👍", pastedText);
        Assert.Contains("emojiId=thumbs_up", File.ReadAllText(session.AppLogPath, Encoding.UTF8));
    }

    [UiE2EFact]
    public async Task Escape_after_opening_overlay_hides_overlay_without_pasting()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        var overlayHandle = session.WaitForOverlayWindow();
        session.SendEscape();

        session.WaitForOverlayHidden(overlayHandle);
        session.AssertTargetTextRemainsEmpty(TimeSpan.FromMilliseconds(750));
    }

    [UiE2EFact]
    public async Task Copy_only_mode_copies_selected_emoji_without_pasting_target()
    {
        using var session = await E2ESession.StartAsync(
            output,
            E2ESettings.Default with { AutoPaste = false });

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.SendEnter();

        var clipboardText = session.WaitForClipboardText("❤️");

        Assert.Equal("❤️", clipboardText);
        session.AssertTargetTextRemainsEmpty(TimeSpan.FromMilliseconds(750));
        Assert.Contains("paste.copy-only", File.ReadAllText(session.AppLogPath, Encoding.UTF8));
    }

    [UiE2EFact]
    public async Task Restore_clipboard_after_paste_restores_original_clipboard()
    {
        const string originalClipboardText = "original clipboard text";
        E2ESession.SetClipboardText(originalClipboardText);
        using var session = await E2ESession.StartAsync(
            output,
            E2ESettings.Default with { RestoreClipboardAfterPaste = true });

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.SendEnter();

        var pastedText = session.WaitForTargetText("❤️");
        var clipboardText = session.WaitForClipboardText(originalClipboardText);

        Assert.Equal("❤️", pastedText);
        Assert.Equal(originalClipboardText, clipboardText);
        Assert.Contains("clipboard.restore complete", File.ReadAllText(session.AppLogPath, Encoding.UTF8));
    }

    private sealed record E2ESettings(
        bool ReplaceWinPeriod,
        bool AutoPaste,
        bool RegisterFallbackHotkey,
        bool RestoreClipboardAfterPaste,
        string FallbackHotkey)
    {
        public static E2ESettings Default { get; } = new(
            ReplaceWinPeriod: true,
            AutoPaste: true,
            RegisterFallbackHotkey: true,
            RestoreClipboardAfterPaste: false,
            FallbackHotkey: "Ctrl+Alt+Space");
    }

    private sealed class E2ESession : IDisposable
    {
        private const int SwRestore = 9;
        private const int VkEscape = 0x1B;
        private const int VkReturn = 0x0D;
        private const int VkControl = 0x11;
        private const int VkMenu = 0x12;
        private const int VkSpace = 0x20;
        private const int VkLeftWin = 0x5B;
        private const int VkOemPeriod = 0xBE;
        private const uint InputMouse = 0;
        private const uint InputKeyboard = 1;
        private const uint MouseEventFLeftDown = 0x0002;
        private const uint MouseEventFLeftUp = 0x0004;
        private const uint KeyEventFKeyUp = 0x0002;
        private const uint KeyEventFUnicode = 0x0004;
        private static readonly JsonSerializerOptions SettingsJsonOptions = new() { WriteIndented = true };

        private readonly ITestOutputHelper output;
        private readonly string tempDirectory;
        private readonly string targetTitle;
        private readonly string readyFile;
        private readonly string textFile;
        private readonly string targetLogPath;
        private readonly string appLogPath;
        private readonly Process appProcess;
        private readonly Process targetProcess;
        private bool disposed;

        private E2ESession(
            ITestOutputHelper output,
            string tempDirectory,
            string targetTitle,
            string readyFile,
            string textFile,
            string targetLogPath,
            string appLogPath,
            string driverLogPath,
            Process appProcess,
            Process targetProcess)
        {
            this.output = output;
            this.tempDirectory = tempDirectory;
            this.targetTitle = targetTitle;
            this.readyFile = readyFile;
            this.textFile = textFile;
            this.targetLogPath = targetLogPath;
            this.appLogPath = appLogPath;
            DriverLogPath = driverLogPath;
            this.appProcess = appProcess;
            this.targetProcess = targetProcess;
        }

        public string DriverLogPath { get; }
        public string AppLogPath => appLogPath;

        private IntPtr TargetWindowHandle
        {
            get
            {
                if (!File.Exists(readyFile))
                {
                    return IntPtr.Zero;
                }

                var handleValue = long.Parse(File.ReadAllText(readyFile, Encoding.UTF8));
                return new IntPtr(handleValue);
            }
        }

        public static async Task<E2ESession> StartAsync(ITestOutputHelper output, E2ESettings? settings = null)
        {
            AssertInteractiveDesktop();
            StopExistingAppProcesses();

            var root = FindRepositoryRoot();
            var artifactRoot = Environment.GetEnvironmentVariable("WINDOWS_EASY_EMOJI_E2E_ARTIFACT_DIR");
            if (string.IsNullOrWhiteSpace(artifactRoot))
            {
                artifactRoot = Path.Combine(Path.GetTempPath(), "WindowsEasyEmoji.E2E");
            }

            var tempDirectory = Path.Combine(artifactRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            var targetTitle = $"WindowsEasyEmoji E2E Target {Guid.NewGuid():N}";
            var readyFile = Path.Combine(tempDirectory, "target.ready");
            var textFile = Path.Combine(tempDirectory, "target.txt");
            var targetLogPath = Path.Combine(tempDirectory, "target.log");
            var driverLogPath = Path.Combine(tempDirectory, "driver.log");
            var appLogPath = Path.Combine(tempDirectory, "app.log");
            var settingsPath = Path.Combine(tempDirectory, "settings.json");
            WriteSettings(settingsPath, settings ?? E2ESettings.Default);

            var targetExe = Path.Combine(root, "tests", "WindowsEasyEmoji.E2ETarget", "bin", "Debug", "net8.0-windows", "WindowsEasyEmoji.E2ETarget.exe");
            var appExe = Path.Combine(root, "src", "WindowsEasyEmoji.App", "bin", "Debug", "net8.0-windows", "WindowsEasyEmoji.App.exe");

            var appProcess = StartProcess(
                appExe,
                [],
                new Dictionary<string, string>
                {
                    ["WINDOWS_EASY_EMOJI_E2E_LOG"] = appLogPath,
                    ["WINDOWS_EASY_EMOJI_SETTINGS_PATH"] = settingsPath
                });

            WaitUntil(
                () => File.Exists(appLogPath) && File.ReadAllText(appLogPath, Encoding.UTF8).Contains("keyboard-hook.start", StringComparison.Ordinal),
                TimeSpan.FromSeconds(10),
                "app keyboard hook did not start");

            var targetProcess = StartProcess(
                targetExe,
                [
                    "--title", targetTitle,
                    "--ready-file", readyFile,
                    "--text-file", textFile,
                    "--log-file", targetLogPath
                ],
                environment: null);

            targetProcess.WaitForInputIdle(5_000);
            WaitUntil(() => File.Exists(readyFile), TimeSpan.FromSeconds(10), "target window did not become ready");

            var session = new E2ESession(
                output,
                tempDirectory,
                targetTitle,
                readyFile,
                textFile,
                targetLogPath,
                appLogPath,
                driverLogPath,
                appProcess,
                targetProcess);

            session.Log($"root:{root}");
            session.Log($"target-exe:{targetExe}");
            session.Log($"app-exe:{appExe}");
            session.Log($"temp:{tempDirectory}");
            session.Log($"session-id:{Process.GetCurrentProcess().SessionId}");
            session.Log($"user-interactive:{Environment.UserInteractive}");
            await Task.Delay(500);
            return session;
        }

        public void FocusTargetWindow()
        {
            var handle = TargetWindowHandle;
            Assert.NotEqual(IntPtr.Zero, handle);

            ShowWindow(handle, SwRestore);
            var setForegroundResult = SetForegroundWindow(handle);
            Log($"focus-target set-foreground-result={setForegroundResult} before-click={DescribeWindow(GetForegroundWindow())}");
            ClickWindowCenter(handle);
            Log($"focus-target after-click={DescribeWindow(GetForegroundWindow())}");
            WaitUntil(() => GetForegroundWindow() == handle, TimeSpan.FromSeconds(5), "target window did not receive foreground focus");
            Log($"focused-target:{handle}");
        }

        public void SendWinPeriod()
        {
            Log("send-win-period");
            SendKeyboardInputs(
                KeyInput(VkLeftWin, 0),
                KeyInput(VkOemPeriod, 0),
                KeyInput(VkOemPeriod, KeyEventFKeyUp),
                KeyInput(VkLeftWin, KeyEventFKeyUp));
        }

        public void SendFallbackHotkey()
        {
            Log("send-fallback-hotkey");
            SendKeyboardInputs(
                KeyInput(VkControl, 0),
                KeyInput(VkMenu, 0),
                KeyInput(VkSpace, 0),
                KeyInput(VkSpace, KeyEventFKeyUp),
                KeyInput(VkMenu, KeyEventFKeyUp),
                KeyInput(VkControl, KeyEventFKeyUp));
        }

        public void SendEnter()
        {
            Log("send-enter");
            Log($"foreground-before-enter:{DescribeWindow(GetForegroundWindow())}");
            SendKeyboardInputs(
                KeyInput(VkReturn, 0),
                KeyInput(VkReturn, KeyEventFKeyUp));
        }

        public void SendEscape()
        {
            Log("send-escape");
            SendKeyboardInputs(
                KeyInput(VkEscape, 0),
                KeyInput(VkEscape, KeyEventFKeyUp));
        }

        public void TypeSearchText(string text)
        {
            Log($"type-search-text length={text.Length}");
            foreach (var character in text)
            {
                SendKeyboardInputs(
                    UnicodeInput(character, KeyEventFUnicode),
                    UnicodeInput(character, KeyEventFUnicode | KeyEventFKeyUp));
            }
        }

        public IntPtr WaitForOverlayWindow()
        {
            var handle = IntPtr.Zero;
            WaitUntil(
                () =>
                {
                    handle = FindVisibleWindow(appProcess.Id, "Windows Easy Emoji");
                    return handle != IntPtr.Zero;
                },
                TimeSpan.FromSeconds(10),
                "Windows Easy Emoji overlay did not appear");

            Log($"overlay-visible:{handle}");
            WaitUntil(
                () => GetForegroundWindow() == handle,
                TimeSpan.FromSeconds(5),
                "Windows Easy Emoji overlay did not receive foreground focus");
            Log($"foreground-after-overlay:{DescribeWindow(GetForegroundWindow())}");
            WaitForAppLogContains("main-window.focus-search-box", TimeSpan.FromSeconds(5));
            return handle;
        }

        public void WaitForOverlayHidden(IntPtr handle)
        {
            WaitUntil(
                () => !IsWindowVisible(handle),
                TimeSpan.FromSeconds(5),
                "Windows Easy Emoji overlay did not hide");
            Log($"overlay-hidden:{handle}");
        }

        public string WaitForTargetText(string expected)
        {
            string? actual = null;
            WaitUntil(
                () =>
                {
                    actual = File.Exists(textFile) ? File.ReadAllText(textFile, Encoding.UTF8) : string.Empty;
                    return actual == expected;
                },
                TimeSpan.FromSeconds(10),
                $"target text did not become '{expected}'");

            Log($"target-text:{actual}");
            return actual!;
        }

        public void AssertTargetTextRemainsEmpty(TimeSpan duration)
        {
            var deadline = Stopwatch.StartNew();
            while (deadline.Elapsed < duration)
            {
                var actual = ReadTargetText();
                if (!string.IsNullOrEmpty(actual))
                {
                    throw new InvalidOperationException($"Expected target text to remain empty, but it became '{actual}'.");
                }

                Thread.Sleep(100);
            }

            Log("target-text-remained-empty");
        }

        public string WaitForClipboardText(string expected)
        {
            string? actual = null;
            WaitUntil(
                () =>
                {
                    actual = GetClipboardText();
                    return actual == expected;
                },
                TimeSpan.FromSeconds(5),
                $"clipboard text did not become '{expected}'");

            Log($"clipboard-text:{actual}");
            return actual!;
        }

        public void WaitForAppLogContains(string expected, TimeSpan timeout)
        {
            WaitUntil(
                () => File.Exists(appLogPath) && File.ReadAllText(appLogPath, Encoding.UTF8).Contains(expected, StringComparison.Ordinal),
                timeout,
                $"app log did not contain '{expected}'");
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Kill(appProcess);
            Kill(targetProcess);

            foreach (var path in new[] { DriverLogPath, targetLogPath, appLogPath })
            {
                if (File.Exists(path))
                {
                    output.WriteLine($"{Path.GetFileName(path)}:");
                    output.WriteLine(File.ReadAllText(path, Encoding.UTF8));
                }
            }
        }

        private static Process StartProcess(string fileName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"Required E2E executable not found: {fileName}", fileName);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(fileName)!
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            if (environment is not null)
            {
                foreach (var (key, value) in environment)
                {
                    startInfo.Environment[key] = value;
                }
            }

            return Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {fileName}");
        }

        private static void WriteSettings(string settingsPath, E2ESettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, SettingsJsonOptions), Encoding.UTF8);
        }

        private static void StopExistingAppProcesses()
        {
            foreach (var process in Process.GetProcessesByName("WindowsEasyEmoji.App"))
            {
                Kill(process);
            }
        }

        private static void AssertInteractiveDesktop()
        {
            if (!Environment.UserInteractive || Process.GetCurrentProcess().SessionId == 0)
            {
                throw new InvalidOperationException(
                    "UI E2E tests require an unlocked interactive desktop session. Run the GitHub runner with run.cmd in the logged-in Cloud PC session, not as a Windows service.");
            }
        }

        public static void SetClipboardText(string text)
        {
            RunOnStaThread(() => System.Windows.Clipboard.SetText(text));
        }

        private static string GetClipboardText()
        {
            return RunOnStaThread(() => System.Windows.Clipboard.ContainsText()
                ? System.Windows.Clipboard.GetText()
                : string.Empty);
        }

        private static T RunOnStaThread<T>(Func<T> action)
        {
            T? result = default;
            Exception? exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception caught)
                {
                    exception = caught;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception is not null)
            {
                throw exception;
            }

            return result!;
        }

        private static void RunOnStaThread(Action action)
        {
            RunOnStaThread(() =>
            {
                action();
                return true;
            });
        }

        private string ReadTargetText()
        {
            return File.Exists(textFile) ? File.ReadAllText(textFile, Encoding.UTF8) : string.Empty;
        }

        private static void Kill(Process process)
        {
            try
            {
                if (process.HasExited)
                {
                    return;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
        }

        private void Log(string message)
        {
            var line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
            File.AppendAllText(DriverLogPath, line, Encoding.UTF8);
            output.WriteLine(message);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "WindowsEasyEmoji.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not find repository root.");
        }

        private static IntPtr FindVisibleWindow(int processId, string title)
        {
            var found = IntPtr.Zero;
            EnumWindows((handle, _) =>
            {
                GetWindowThreadProcessId(handle, out var windowProcessId);
                if (windowProcessId == processId && IsWindowVisible(handle) && GetWindowTitle(handle) == title)
                {
                    found = handle;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return found;
        }

        private static string DescribeWindow(IntPtr handle)
        {
            GetWindowThreadProcessId(handle, out var processId);
            return $"{handle}:pid={processId}:title={GetWindowTitle(handle)}";
        }

        private static string GetWindowTitle(IntPtr handle)
        {
            var length = GetWindowTextLength(handle);
            if (length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(length + 1);
            GetWindowText(handle, builder, builder.Capacity);
            return builder.ToString();
        }

        private static void WaitUntil(Func<bool> condition, TimeSpan timeout, string failureMessage)
        {
            var deadline = Stopwatch.StartNew();
            while (deadline.Elapsed < timeout)
            {
                if (condition())
                {
                    return;
                }

                Thread.Sleep(100);
            }

            throw new TimeoutException(failureMessage);
        }

        private static void SendKeyboardInputs(params Input[] inputs)
        {
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent != inputs.Length)
            {
                throw new InvalidOperationException($"SendInput sent {sent} of {inputs.Length} inputs. Error: {Marshal.GetLastWin32Error()}");
            }
        }

        private static Input KeyInput(ushort virtualKey, uint flags)
        {
            return new Input
            {
                Type = InputKeyboard,
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

        private static Input UnicodeInput(char character, uint flags)
        {
            return new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInputData
                    {
                        VirtualKey = 0,
                        ScanCode = character,
                        Flags = flags,
                        Time = 0,
                        ExtraInfo = UIntPtr.Zero
                    }
                }
            };
        }

        private static void ClickWindowCenter(IntPtr handle)
        {
            if (!GetWindowRect(handle, out var rect))
            {
                return;
            }

            var x = rect.Left + ((rect.Right - rect.Left) / 2);
            var y = rect.Top + ((rect.Bottom - rect.Top) / 2);
            SetCursorPos(x, y);
            SendKeyboardInputs(
                MouseInput(MouseEventFLeftDown),
                MouseInput(MouseEventFLeftUp));
        }

        private static Input MouseInput(uint flags)
        {
            return new Input
            {
                Type = InputMouse,
                Data = new InputUnion
                {
                    Mouse = new MouseInputData
                    {
                        Dx = 0,
                        Dy = 0,
                        MouseData = 0,
                        Flags = flags,
                        Time = 0,
                        ExtraInfo = UIntPtr.Zero
                    }
                }
            };
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public uint Type;
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
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}

[CollectionDefinition("Windows desktop E2E", DisableParallelization = true)]
public sealed class WindowsDesktopE2ECollection;

[AttributeUsage(AttributeTargets.Method)]
public sealed class UiE2EFactAttribute : FactAttribute
{
    private const string RunEnvironmentVariable = "WINDOWS_EASY_EMOJI_RUN_UI_E2E";

    public UiE2EFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RunEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            Skip = $"Set {RunEnvironmentVariable}=1 to run Windows desktop UI E2E tests.";
        }
    }
}
