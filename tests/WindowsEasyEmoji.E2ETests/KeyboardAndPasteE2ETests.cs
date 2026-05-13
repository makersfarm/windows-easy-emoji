using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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

        var pastedText = session.WaitForTargetText("😀");

        Assert.Equal("😀", pastedText);
        Assert.Contains("emojiId=grinning_face", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task Fallback_hotkey_shows_search_overlay()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendFallbackHotkey();

        var overlayHandle = session.WaitForOverlayWindow();

        Assert.NotEqual(IntPtr.Zero, overlayHandle);
        Assert.Contains("app.shortcut-dispatch sender=HotkeyService", session.ReadAppLog());
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
        Assert.Contains("emojiId=face_with_tears_of_joy", session.ReadAppLog());
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
        Assert.Contains("emojiId=thumbs_up", session.ReadAppLog());
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

        var clipboardText = session.WaitForClipboardText("😀");

        Assert.Equal("😀", clipboardText);
        session.AssertTargetTextRemainsEmpty(TimeSpan.FromMilliseconds(750));
        Assert.Contains("paste.copy-only", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task Copy_only_korean_query_copies_matching_emoji_without_pasting_target()
    {
        using var session = await E2ESession.StartAsync(
            output,
            E2ESettings.Default with { AutoPaste = false });

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText("불");
        session.SendEnter();

        var clipboardText = session.WaitForClipboardText("🔥");

        Assert.Equal("🔥", clipboardText);
        session.AssertTargetTextRemainsEmpty(TimeSpan.FromMilliseconds(750));
        Assert.Contains("emojiId=fire", session.ReadAppLog());
        Assert.Contains("paste.copy-only", session.ReadAppLog());
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

        var pastedText = session.WaitForTargetText("😀");
        var clipboardText = session.WaitForClipboardText(originalClipboardText);

        Assert.Equal("😀", pastedText);
        Assert.Equal(originalClipboardText, clipboardText);
        Assert.Contains("clipboard.restore complete", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task Session_uses_isolated_user_state_path()
    {
        using var session = await E2ESession.StartAsync(output);

        var appLog = session.ReadAppLog();

        Assert.Contains($"path={session.UserStatePath}", appLog);
        Assert.DoesNotContain("AppData\\Roaming\\WindowsEasyEmoji\\user-state.json", appLog, StringComparison.OrdinalIgnoreCase);
    }

    [UiE2EFact]
    public async Task Heart_query_pastes_red_heart()
    {
        await AssertQueryPastesEmojiAsync("하트", "❤️", "red_heart");
    }

    [UiE2EFact]
    public async Task Fire_query_pastes_fire()
    {
        await AssertQueryPastesEmojiAsync("불", "🔥", "fire");
    }

    [UiE2EFact]
    public async Task English_query_pastes_matching_emoji()
    {
        await AssertQueryPastesEmojiAsync("fire", "🔥", "fire");
    }

    [UiE2EFact]
    public async Task Korea_query_pastes_korean_flag()
    {
        await AssertQueryPastesEmojiAsync("한국", "🇰🇷", "flag_south_korea");
    }

    [UiE2EFact]
    public async Task Check_query_pastes_check_mark_button()
    {
        await AssertQueryPastesEmojiAsync("체크", "✅", "check_mark_button");
    }

    [UiE2EFact]
    public async Task Clap_query_pastes_clapping_hands()
    {
        await AssertQueryPastesEmojiAsync("박수", "👏", "clapping_hands");
    }

    [UiE2EFact]
    public async Task Celebration_shortcut_query_pastes_party_popper()
    {
        await AssertQueryPastesEmojiAsync("ㅊㅋ", "🎉", "party_popper");
    }

    [UiE2EFact]
    public async Task Thanks_shortcut_query_pastes_folded_hands()
    {
        await AssertQueryPastesEmojiAsync("ㄱㅅ", "🙏", "folded_hands");
    }

    [UiE2EFact]
    public async Task Crying_shortcut_query_pastes_loudly_crying_face()
    {
        await AssertQueryPastesEmojiAsync("ㅠㅠ", "😭", "loudly_crying_face");
    }

    [UiE2EFact]
    public async Task Custom_fallback_hotkey_shows_search_overlay()
    {
        using var session = await E2ESession.StartAsync(
            output,
            E2ESettings.Default with { FallbackHotkey = "Ctrl+Alt+E" });

        session.FocusTargetWindow();
        session.SendFallbackHotkey();

        var overlayHandle = session.WaitForOverlayWindow();

        Assert.NotEqual(IntPtr.Zero, overlayHandle);
        Assert.Contains("fallbackHotkey=Ctrl+Alt+E", session.ReadAppLog());
        Assert.Contains("app.shortcut-dispatch sender=HotkeyService", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task Fallback_hotkey_still_opens_overlay_when_win_period_replacement_is_disabled()
    {
        using var session = await E2ESession.StartAsync(
            output,
            E2ESettings.Default with { ReplaceWinPeriod = false });

        session.FocusTargetWindow();
        session.SendFallbackHotkey();

        var overlayHandle = session.WaitForOverlayWindow();

        Assert.NotEqual(IntPtr.Zero, overlayHandle);
        Assert.DoesNotContain("keyboard-hook.start", session.ReadAppLog());
        Assert.Contains("app.shortcut-dispatch sender=HotkeyService", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task Empty_search_uses_recent_user_state_for_default_selection()
    {
        using var session = await E2ESession.StartAsync(
            output,
            userState:
            [
                E2EUserEmojiState.Create("fire", useCount: 10, lastUsedAt: new DateTimeOffset(2026, 5, 13, 0, 0, 0, TimeSpan.Zero)),
                E2EUserEmojiState.Create("red_heart", useCount: 10, lastUsedAt: new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero))
            ]);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.SendEnter();

        var pastedText = session.WaitForTargetText("🔥");

        Assert.Equal("🔥", pastedText);
        Assert.Contains("emojiId=fire", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task Custom_alias_from_user_state_can_drive_search()
    {
        using var session = await E2ESession.StartAsync(
            output,
            userState:
            [
                E2EUserEmojiState.Create("fire", customAliases: ["내불"])
            ]);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText("내불");
        session.SendEnter();

        var pastedText = session.WaitForTargetText("🔥");

        Assert.Equal("🔥", pastedText);
        Assert.Contains("emojiId=fire", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task User_state_records_selection_after_paste()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText("하트");
        session.SendEnter();
        session.WaitForTargetText("❤️");

        var state = session.WaitForUserState("red_heart", expectedUseCount: 1);

        Assert.Equal("red_heart", state.EmojiId);
        Assert.NotNull(state.LastUsedAt);
        Assert.Contains("main-window.user-state saved emojiId=red_heart", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task User_state_increments_existing_use_count_after_paste()
    {
        using var session = await E2ESession.StartAsync(
            output,
            userState:
            [
                E2EUserEmojiState.Create("red_heart", useCount: 2, lastUsedAt: new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero))
            ]);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText("하트");
        session.SendEnter();
        session.WaitForTargetText("❤️");

        var state = session.WaitForUserState("red_heart", expectedUseCount: 3);

        Assert.Equal(3, state.UseCount);
        Assert.NotNull(state.LastUsedAt);
    }

    [UiE2EFact]
    public async Task Copy_only_mode_records_selection_in_user_state()
    {
        using var session = await E2ESession.StartAsync(
            output,
            E2ESettings.Default with { AutoPaste = false });

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText("불");
        session.SendEnter();
        session.WaitForClipboardText("🔥");

        var state = session.WaitForUserState("fire", expectedUseCount: 1);

        Assert.Equal("fire", state.EmojiId);
        session.AssertTargetTextRemainsEmpty(TimeSpan.FromMilliseconds(750));
    }

    [UiE2EFact]
    public async Task No_results_enter_does_not_paste_or_copy()
    {
        const string originalClipboardText = "clipboard before no-result e2e";
        E2ESession.SetClipboardText(originalClipboardText);
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText("뷁뷁뷁");
        session.SendEnter();

        session.AssertTargetTextRemainsEmpty(TimeSpan.FromMilliseconds(750));
        Assert.Equal(originalClipboardText, E2ESession.GetClipboardText());
        Assert.Contains("main-window.refresh queryLength=3 resultCount=0 selectedIndex=-1", session.ReadAppLog());
        Assert.Contains("noResults=True", session.ReadAppLog());
        Assert.DoesNotContain("main-window.paste-result begin", session.ReadAppLog());
    }

    [UiE2EFact]
    public async Task Escape_after_typing_query_hides_overlay_without_pasting()
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        var overlayHandle = session.WaitForOverlayWindow();
        session.TypeSearchText("불");
        session.SendEscape();

        session.WaitForOverlayHidden(overlayHandle);
        session.AssertTargetTextRemainsEmpty(TimeSpan.FromMilliseconds(750));
        Assert.DoesNotContain("main-window.paste-result begin", session.ReadAppLog());
    }

    private async Task AssertQueryPastesEmojiAsync(string query, string expectedEmoji, string expectedEmojiId)
    {
        using var session = await E2ESession.StartAsync(output);

        session.FocusTargetWindow();
        session.SendWinPeriod();
        session.WaitForOverlayWindow();
        session.TypeSearchText(query);
        session.SendEnter();

        var pastedText = session.WaitForTargetText(expectedEmoji);

        Assert.Equal(expectedEmoji, pastedText);
        Assert.Contains($"emojiId={expectedEmojiId}", session.ReadAppLog());
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

    private sealed record E2EUserEmojiState(
        string EmojiId,
        DateTimeOffset? LastUsedAt,
        int UseCount,
        bool Favorite,
        string[] CustomAliases)
    {
        public static E2EUserEmojiState Create(
            string emojiId,
            DateTimeOffset? lastUsedAt = null,
            int useCount = 0,
            bool favorite = false,
            string[]? customAliases = null)
        {
            return new E2EUserEmojiState(
                EmojiId: emojiId,
                LastUsedAt: lastUsedAt,
                UseCount: useCount,
                Favorite: favorite,
                CustomAliases: customAliases ?? []);
        }
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
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpShowWindow = 0x0040;
        private const uint InputMouse = 0;
        private const uint InputKeyboard = 1;
        private const uint MouseEventFLeftDown = 0x0002;
        private const uint MouseEventFLeftUp = 0x0004;
        private const uint KeyEventFKeyUp = 0x0002;
        private const uint KeyEventFUnicode = 0x0004;
        private static readonly IntPtr HwndTopMost = new(-1);
        private static readonly IntPtr HwndNoTopMost = new(-2);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly ITestOutputHelper output;
        private readonly string tempDirectory;
        private readonly string targetTitle;
        private readonly string readyFile;
        private readonly string textFile;
        private readonly string targetLogPath;
        private readonly string appLogPath;
        private readonly string userStatePath;
        private readonly E2ESettings settings;
        private readonly Process appProcess;
        private readonly Process targetProcess;
        private IntPtr overlayWindowHandle;
        private bool disposed;

        private E2ESession(
            ITestOutputHelper output,
            string tempDirectory,
            string targetTitle,
            string readyFile,
            string textFile,
            string targetLogPath,
            string appLogPath,
            string userStatePath,
            string driverLogPath,
            E2ESettings settings,
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
            this.userStatePath = userStatePath;
            this.settings = settings;
            DriverLogPath = driverLogPath;
            this.appProcess = appProcess;
            this.targetProcess = targetProcess;
        }

        public string DriverLogPath { get; }
        public string AppLogPath => appLogPath;
        public string UserStatePath => userStatePath;

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

        public static async Task<E2ESession> StartAsync(
            ITestOutputHelper output,
            E2ESettings? settings = null,
            IReadOnlyList<E2EUserEmojiState>? userState = null)
        {
            AssertInteractiveDesktop();

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
            var userStatePath = Path.Combine(tempDirectory, "user-state.json");
            LogStartup(output, driverLogPath, $"start-session temp:{tempDirectory}");
            LogStartup(output, driverLogPath, $"session-id:{Process.GetCurrentProcess().SessionId}");
            LogStartup(output, driverLogPath, $"user-interactive:{Environment.UserInteractive}");
            StopExistingAppProcesses(output, driverLogPath);

            var effectiveSettings = settings ?? E2ESettings.Default;
            WriteSettings(settingsPath, effectiveSettings);
            WriteUserState(userStatePath, userState ?? []);

            var configuration = GetCurrentBuildConfiguration();
            var targetExe = Path.Combine(root, "tests", "WindowsEasyEmoji.E2ETarget", "bin", configuration, "net8.0-windows", "WindowsEasyEmoji.E2ETarget.exe");
            var appExe = Environment.GetEnvironmentVariable("WINDOWS_EASY_EMOJI_E2E_APP_EXE");
            if (string.IsNullOrWhiteSpace(appExe))
            {
                appExe = Path.Combine(root, "src", "WindowsEasyEmoji.App", "bin", configuration, "net8.0-windows", "WindowsEasyEmoji.App.exe");
            }

            LogStartup(output, driverLogPath, $"root:{root}");
            LogStartup(output, driverLogPath, $"target-exe:{targetExe}");
            LogStartup(output, driverLogPath, $"app-exe:{appExe}");
            LogStartup(output, driverLogPath, $"app-exe-source:{(Environment.GetEnvironmentVariable("WINDOWS_EASY_EMOJI_E2E_APP_EXE") is null ? "build-output" : "installed-app")}");
            LogStartup(output, driverLogPath, $"user-state:{userStatePath}");

            Process? appProcess = null;
            Process? targetProcess = null;
            try
            {
                appProcess = StartProcess(
                    appExe,
                    [],
                    new Dictionary<string, string>
                    {
                        ["WINDOWS_EASY_EMOJI_E2E_LOG"] = appLogPath,
                        ["WINDOWS_EASY_EMOJI_SETTINGS_PATH"] = settingsPath,
                        ["WINDOWS_EASY_EMOJI_USER_STATE_PATH"] = userStatePath
                    });
                LogStartup(output, driverLogPath, $"started-app pid:{appProcess.Id}");

                WaitForAppStartup(output, driverLogPath, appLogPath, appProcess, TimeSpan.FromSeconds(30));

                targetProcess = StartProcess(
                    targetExe,
                    [
                        "--title", targetTitle,
                        "--ready-file", readyFile,
                        "--text-file", textFile,
                        "--log-file", targetLogPath
                    ],
                    environment: null);
                LogStartup(output, driverLogPath, $"started-target pid:{targetProcess.Id}");

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
                    userStatePath,
                    driverLogPath,
                    effectiveSettings,
                    appProcess,
                    targetProcess);

                await Task.Delay(500);
                return session;
            }
            catch
            {
                LogStartup(output, driverLogPath, "start-session failed; cleaning launched processes");
                if (targetProcess is not null)
                {
                    Kill(targetProcess);
                }

                if (appProcess is not null)
                {
                    Kill(appProcess);
                }

                CaptureDesktopScreenshot(Path.Combine(tempDirectory, "desktop-startup-failure.png"));
                throw;
            }
        }

        public void FocusTargetWindow()
        {
            var handle = TargetWindowHandle;
            Assert.NotEqual(IntPtr.Zero, handle);

            var forcedForegroundResult = TryForceForegroundWindow(handle);
            Log($"focus-target force-foreground-result={forcedForegroundResult} after-force={DescribeWindow(GetForegroundWindow())}");
            if (GetForegroundWindow() != handle)
            {
                var anchorResult = TrySeedForegroundAndFocusTarget(handle);
                Log($"focus-target anchor-foreground-result={anchorResult} after-anchor={DescribeWindow(GetForegroundWindow())}");
            }

            var setForegroundResult = SetForegroundWindow(handle);
            Log($"focus-target set-foreground-result={setForegroundResult} before-click={DescribeWindow(GetForegroundWindow())}");

            if (GetForegroundWindow() != handle)
            {
                var clickResult = TryClickWindowCenter(handle);
                Log($"focus-target click-result={clickResult} after-click={DescribeWindow(GetForegroundWindow())}");
            }

            WaitUntil(() => GetForegroundWindow() == handle, TimeSpan.FromSeconds(5), "target window did not receive foreground focus");
            SetWindowPos(handle, HwndNoTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
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
            Log($"send-fallback-hotkey:{settings.FallbackHotkey}");
            SendHotkey(settings.FallbackHotkey);
        }

        public void SendEnter()
        {
            EnsureOverlayForeground();
            Log("send-enter");
            Log($"foreground-before-enter:{DescribeWindow(GetForegroundWindow())}");
            SendKeyboardInputs(
                KeyInput(VkReturn, 0),
                KeyInput(VkReturn, KeyEventFKeyUp));
        }

        public void SendEscape()
        {
            EnsureOverlayForeground();
            Log("send-escape");
            SendKeyboardInputs(
                KeyInput(VkEscape, 0),
                KeyInput(VkEscape, KeyEventFKeyUp));
        }

        public void TypeSearchText(string text)
        {
            EnsureOverlayForeground();
            Log($"type-search-text length={text.Length}");
            foreach (var character in text)
            {
                SendKeyboardInputs(
                    UnicodeInput(character, KeyEventFUnicode),
                    UnicodeInput(character, KeyEventFUnicode | KeyEventFKeyUp));
            }

            WaitForAppLogContains($"main-window.refresh queryLength={text.Length}", TimeSpan.FromSeconds(5));
            EnsureOverlayForeground();
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
            overlayWindowHandle = handle;
            WaitUntil(
                () => GetForegroundWindow() == handle,
                TimeSpan.FromSeconds(5),
                "Windows Easy Emoji overlay did not receive foreground focus");
            Log($"foreground-after-overlay:{DescribeWindow(GetForegroundWindow())}");
            WaitForAppLogContains("main-window.focus-search-box", TimeSpan.FromSeconds(5));
            return handle;
        }

        private void EnsureOverlayForeground()
        {
            if (overlayWindowHandle == IntPtr.Zero || !IsWindowVisible(overlayWindowHandle))
            {
                return;
            }

            if (GetForegroundWindow() == overlayWindowHandle)
            {
                return;
            }

            var forcedForegroundResult = TryForceForegroundWindow(overlayWindowHandle);
            Log($"overlay force-foreground-result={forcedForegroundResult} foreground={DescribeWindow(GetForegroundWindow())}");
            WaitUntil(
                () => GetForegroundWindow() == overlayWindowHandle,
                TimeSpan.FromSeconds(2),
                "Windows Easy Emoji overlay lost foreground focus");
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
                    actual = ReadTargetText();
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
                    return TryGetClipboardText(out actual) && actual == expected;
                },
                TimeSpan.FromSeconds(5),
                $"clipboard text did not become '{expected}'");

            Log($"clipboard-text:{actual}");
            return actual!;
        }

        public void WaitForAppLogContains(string expected, TimeSpan timeout)
        {
            WaitUntil(
                () => ReadAppLog().Contains(expected, StringComparison.Ordinal),
                timeout,
                $"app log did not contain '{expected}'");
        }

        public string ReadAppLog()
        {
            return ReadTextFileShared(appLogPath);
        }

        public E2EUserEmojiState WaitForUserState(string emojiId, int expectedUseCount)
        {
            E2EUserEmojiState? actual = null;
            WaitUntil(
                () =>
                {
                    actual = ReadUserState().FirstOrDefault(state =>
                        string.Equals(state.EmojiId, emojiId, StringComparison.Ordinal) &&
                        state.UseCount == expectedUseCount);
                    return actual is not null;
                },
                TimeSpan.FromSeconds(5),
                $"user state for '{emojiId}' did not reach use count {expectedUseCount}");

            Log($"user-state:{emojiId}:use-count={actual!.UseCount}");
            return actual;
        }

        public IReadOnlyList<E2EUserEmojiState> ReadUserState()
        {
            var json = ReadTextFileShared(userStatePath);
            return string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<E2EUserEmojiState>>(json, JsonOptions) ?? [];
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            CaptureDesktopScreenshot(Path.Combine(tempDirectory, "desktop-final.png"));
            Kill(appProcess);
            Kill(targetProcess);

            foreach (var path in new[] { DriverLogPath, targetLogPath, appLogPath })
            {
                if (File.Exists(path))
                {
                    output.WriteLine($"{Path.GetFileName(path)}:");
                    output.WriteLine(ReadTextFileShared(path));
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
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, JsonOptions), Encoding.UTF8);
        }

        private static void WriteUserState(string userStatePath, IReadOnlyList<E2EUserEmojiState> userState)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(userStatePath)!);
            File.WriteAllText(userStatePath, JsonSerializer.Serialize(userState, JsonOptions), Encoding.UTF8);
        }

        private static string GetCurrentBuildConfiguration()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (string.Equals(directory.Name, "Debug", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(directory.Name, "Release", StringComparison.OrdinalIgnoreCase))
                {
                    return directory.Name;
                }

                directory = directory.Parent;
            }

#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }

        private static void StopExistingAppProcesses(ITestOutputHelper output, string driverLogPath)
        {
            var existingProcesses = Process.GetProcessesByName("WindowsEasyEmoji.App");
            if (existingProcesses.Length == 0)
            {
                LogStartup(output, driverLogPath, "stop-existing-app-processes:none");
                return;
            }

            foreach (var process in existingProcesses)
            {
                LogStartup(output, driverLogPath, $"stop-existing-app-process pid:{process.Id} session:{SafeSessionId(process)} exited:{SafeHasExited(process)}");
                Kill(process);
            }

            WaitUntil(
                () => Process.GetProcessesByName("WindowsEasyEmoji.App").All(process => SafeHasExited(process)),
                TimeSpan.FromSeconds(10),
                "existing WindowsEasyEmoji.App processes did not exit");
            LogStartup(output, driverLogPath, "stop-existing-app-processes:complete");
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
            WaitUntil(
                () => TrySetClipboardText(text),
                TimeSpan.FromSeconds(5),
                "clipboard text could not be set");
        }

        public static string GetClipboardText()
        {
            string? text = null;
            WaitUntil(
                () => TryGetClipboardText(out text),
                TimeSpan.FromSeconds(5),
                "clipboard text could not be read");

            return text!;
        }

        private static bool TrySetClipboardText(string text)
        {
            try
            {
                RunOnStaThread(() => System.Windows.Clipboard.SetText(text));
                return true;
            }
            catch (COMException)
            {
                return false;
            }
        }

        private static bool TryGetClipboardText(out string text)
        {
            try
            {
                text = RunOnStaThread(() => System.Windows.Clipboard.ContainsText()
                    ? System.Windows.Clipboard.GetText()
                    : string.Empty);
                return true;
            }
            catch (COMException)
            {
                text = string.Empty;
                return false;
            }
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
            return ReadTextFileShared(textFile);
        }

        private static string ReadTextFileShared(string path)
        {
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
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

        private static void WaitForAppStartup(
            ITestOutputHelper output,
            string driverLogPath,
            string appLogPath,
            Process appProcess,
            TimeSpan timeout)
        {
            var deadline = Stopwatch.StartNew();
            while (deadline.Elapsed < timeout)
            {
                var appLog = ReadTextFileShared(appLogPath);
                if (appLog.Contains("app.startup complete", StringComparison.Ordinal))
                {
                    LogStartup(output, driverLogPath, $"app-startup-complete elapsed:{deadline.Elapsed}");
                    return;
                }

                if (SafeHasExited(appProcess))
                {
                    throw new InvalidOperationException(
                        $"app exited before startup completed. pid:{SafeProcessId(appProcess)} exit-code:{SafeExitCode(appProcess)} app-log:{TrimForLog(appLog)}");
                }

                Thread.Sleep(100);
            }

            throw new TimeoutException(
                $"app did not complete startup. pid:{SafeProcessId(appProcess)} exited:{SafeHasExited(appProcess)} foreground:{DescribeWindow(GetForegroundWindow())} running-app-processes:{DescribeRunningAppProcesses()} app-log:{TrimForLog(ReadTextFileShared(appLogPath))}");
        }

        private static void LogStartup(ITestOutputHelper output, string driverLogPath, string message)
        {
            var line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
            File.AppendAllText(driverLogPath, line, Encoding.UTF8);
            output.WriteLine(message);
        }

        private static string DescribeRunningAppProcesses()
        {
            var processes = Process.GetProcessesByName("WindowsEasyEmoji.App");
            if (processes.Length == 0)
            {
                return "none";
            }

            return string.Join(
                ",",
                processes.Select(process => $"pid={SafeProcessId(process)}:session={SafeSessionId(process)}:exited={SafeHasExited(process)}"));
        }

        private static string TrimForLog(string value)
        {
            const int maxLength = 4000;
            if (value.Length <= maxLength)
            {
                return value.ReplaceLineEndings("\\n");
            }

            return value[^maxLength..].ReplaceLineEndings("\\n");
        }

        private static int SafeProcessId(Process process)
        {
            try
            {
                return process.Id;
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }

        private static int SafeSessionId(Process process)
        {
            try
            {
                return process.SessionId;
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return -1;
            }
        }

        private static bool SafeHasExited(Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        private static int? SafeExitCode(Process process)
        {
            try
            {
                return process.HasExited ? process.ExitCode : null;
            }
            catch (InvalidOperationException)
            {
                return null;
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

        private static void SendHotkey(string hotkey)
        {
            var parts = hotkey
                .Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                throw new ArgumentException("Fallback hotkey must include at least one key.", nameof(hotkey));
            }

            var modifierKeys = new List<ushort>();
            for (var index = 0; index < parts.Length - 1; index++)
            {
                modifierKeys.Add(ParseVirtualKey(parts[index]));
            }

            var key = ParseVirtualKey(parts[^1]);
            var inputs = new List<Input>();
            inputs.AddRange(modifierKeys.Select(modifier => KeyInput(modifier, 0)));
            inputs.Add(KeyInput(key, 0));
            inputs.Add(KeyInput(key, KeyEventFKeyUp));

            for (var index = modifierKeys.Count - 1; index >= 0; index--)
            {
                inputs.Add(KeyInput(modifierKeys[index], KeyEventFKeyUp));
            }

            SendKeyboardInputs(inputs.ToArray());
        }

        private static ushort ParseVirtualKey(string key)
        {
            return key.ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => VkControl,
                "ALT" => VkMenu,
                "WIN" or "WINDOWS" => VkLeftWin,
                "SPACE" => VkSpace,
                { Length: 1 } value when value[0] is >= 'A' and <= 'Z' => value[0],
                { Length: 1 } value when value[0] is >= '0' and <= '9' => value[0],
                _ => throw new NotSupportedException($"Unsupported E2E hotkey key: {key}")
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

        private static bool TryForceForegroundWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var currentThreadId = GetCurrentThreadId();
            var foregroundWindow = GetForegroundWindow();
            var foregroundThreadId = foregroundWindow == IntPtr.Zero
                ? 0
                : GetWindowThreadProcessId(foregroundWindow, out _);
            var attached = false;

            try
            {
                if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
                {
                    attached = AttachThreadInput(currentThreadId, foregroundThreadId, attach: true);
                }

                SetWindowPos(handle, HwndTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
                ShowWindow(handle, SwRestore);
                BringWindowToTop(handle);
                var setForegroundResult = SetForegroundWindow(handle);
                SetActiveWindow(handle);
                SetFocus(handle);
                return setForegroundResult || GetForegroundWindow() == handle;
            }
            finally
            {
                if (attached)
                {
                    AttachThreadInput(currentThreadId, foregroundThreadId, attach: false);
                }
            }
        }

        private static bool TrySeedForegroundAndFocusTarget(IntPtr targetHandle)
        {
            if (targetHandle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                return RunOnStaThread(() =>
                {
                    using var anchor = new System.Windows.Forms.Form
                    {
                        Text = "Windows Easy Emoji E2E Focus Anchor",
                        Width = 96,
                        Height = 64,
                        StartPosition = System.Windows.Forms.FormStartPosition.Manual,
                        Left = 0,
                        Top = 0,
                        TopMost = true,
                        ShowInTaskbar = false,
                        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
                    };

                    anchor.Show();
                    System.Windows.Forms.Application.DoEvents();

                    var anchorHandle = anchor.Handle;
                    SetWindowPos(anchorHandle, HwndTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
                    ShowWindow(anchorHandle, SwRestore);
                    BringWindowToTop(anchorHandle);
                    SetForegroundWindow(anchorHandle);
                    SetActiveWindow(anchorHandle);
                    SetFocus(anchorHandle);
                    System.Windows.Forms.Application.DoEvents();
                    Thread.Sleep(100);

                    SetWindowPos(targetHandle, HwndTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
                    ShowWindow(targetHandle, SwRestore);
                    BringWindowToTop(targetHandle);
                    var targetForegroundResult = SetForegroundWindow(targetHandle);
                    System.Windows.Forms.Application.DoEvents();
                    Thread.Sleep(100);
                    return targetForegroundResult || GetForegroundWindow() == targetHandle;
                });
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        private static void CaptureDesktopScreenshot(string path)
        {
            try
            {
                var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                bitmap.Save(path, ImageFormat.Png);
            }
            catch (Exception exception) when (exception is ExternalException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                File.WriteAllText(path + ".txt", exception.ToString(), Encoding.UTF8);
            }
        }

        private bool TryClickWindowCenter(IntPtr handle)
        {
            if (!GetWindowRect(handle, out var rect))
            {
                return false;
            }

            var x = rect.Left + ((rect.Right - rect.Left) / 2);
            var y = rect.Top + ((rect.Bottom - rect.Top) / 2);
            SetCursorPos(x, y);
            var inputs = new[]
            {
                MouseInput(MouseEventFLeftDown),
                MouseInput(MouseEventFLeftUp)
            };
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent == inputs.Length)
            {
                return true;
            }

            Log($"focus-target click-send-input-failed sent={sent} expected={inputs.Length} error={Marshal.GetLastWin32Error()}");
            return false;
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
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

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
