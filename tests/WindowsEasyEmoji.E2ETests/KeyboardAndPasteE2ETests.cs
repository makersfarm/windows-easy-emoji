using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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

    private sealed class E2ESession : IDisposable
    {
        private const int SwRestore = 9;
        private const int VkReturn = 0x0D;
        private const int VkLeftWin = 0x5B;
        private const int VkOemPeriod = 0xBE;
        private const uint InputMouse = 0;
        private const uint InputKeyboard = 1;
        private const uint MouseEventFLeftDown = 0x0002;
        private const uint MouseEventFLeftUp = 0x0004;
        private const uint KeyEventFKeyUp = 0x0002;

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

        public static async Task<E2ESession> StartAsync(ITestOutputHelper output)
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

        public void SendEnter()
        {
            Log("send-enter");
            Log($"foreground-before-enter:{DescribeWindow(GetForegroundWindow())}");
            SendKeyboardInputs(
                KeyInput(VkReturn, 0),
                KeyInput(VkReturn, KeyEventFKeyUp));
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
            return handle;
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
