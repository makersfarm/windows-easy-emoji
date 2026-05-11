# Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first working codebase skeleton for Windows Easy Emoji: .NET solution, core emoji search engine, tests, and WPF/platform integration stubs.

**Architecture:** Keep behavior in `WindowsEasyEmoji.Core` so it is testable without WPF. Keep Windows API integration in `WindowsEasyEmoji.Platform`. Keep WPF startup, tray host, overlay window, and settings window in `WindowsEasyEmoji.App`.

**Tech Stack:** .NET 8, WPF, xUnit, System.Text.Json, Windows Forms NotifyIcon interop, Win32 P/Invoke stubs.

---

### Task 1: Workspace SDK and Solution

**Files:**
- Create: `.gitignore`
- Create: `global.json`
- Create: `WindowsEasyEmoji.sln`
- Create: `src/WindowsEasyEmoji.Core/WindowsEasyEmoji.Core.csproj`
- Create: `src/WindowsEasyEmoji.Platform/WindowsEasyEmoji.Platform.csproj`
- Create: `src/WindowsEasyEmoji.App/WindowsEasyEmoji.App.csproj`
- Create: `tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj`

- [ ] **Step 1: Install local SDK if `dotnet` is unavailable**

Run:

```powershell
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  New-Item -ItemType Directory -Force .dotnet | Out-Null
  Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile .dotnet/dotnet-install.ps1
  & .dotnet/dotnet-install.ps1 -Channel 8.0 -InstallDir .dotnet
  $env:PATH = "$PWD\\.dotnet;$env:PATH"
}
dotnet --info
```

Expected: `dotnet --info` prints SDK information.

- [ ] **Step 2: Scaffold the solution**

Run:

```powershell
dotnet new sln -n WindowsEasyEmoji
dotnet new classlib -n WindowsEasyEmoji.Core -o src/WindowsEasyEmoji.Core
dotnet new classlib -n WindowsEasyEmoji.Platform -o src/WindowsEasyEmoji.Platform
dotnet new wpf -n WindowsEasyEmoji.App -o src/WindowsEasyEmoji.App
dotnet new xunit -n WindowsEasyEmoji.Core.Tests -o tests/WindowsEasyEmoji.Core.Tests
dotnet sln add src/WindowsEasyEmoji.Core/WindowsEasyEmoji.Core.csproj
dotnet sln add src/WindowsEasyEmoji.Platform/WindowsEasyEmoji.Platform.csproj
dotnet sln add src/WindowsEasyEmoji.App/WindowsEasyEmoji.App.csproj
dotnet sln add tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj
dotnet add src/WindowsEasyEmoji.Platform/WindowsEasyEmoji.Platform.csproj reference src/WindowsEasyEmoji.Core/WindowsEasyEmoji.Core.csproj
dotnet add src/WindowsEasyEmoji.App/WindowsEasyEmoji.App.csproj reference src/WindowsEasyEmoji.Core/WindowsEasyEmoji.Core.csproj
dotnet add src/WindowsEasyEmoji.App/WindowsEasyEmoji.App.csproj reference src/WindowsEasyEmoji.Platform/WindowsEasyEmoji.Platform.csproj
dotnet add tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj reference src/WindowsEasyEmoji.Core/WindowsEasyEmoji.Core.csproj
```

Expected: solution and projects are created.

- [ ] **Step 3: Add baseline ignore and SDK pin**

Create `.gitignore`:

```gitignore
bin/
obj/
.vs/
.dotnet/
TestResults/
*.user
*.suo
```

Create `global.json`:

```json
{
  "sdk": {
    "version": "8.0.000",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 4: Baseline build**

Run:

```powershell
dotnet build WindowsEasyEmoji.sln
```

Expected: build succeeds before custom code.

### Task 2: Core Schema and Normalization

**Files:**
- Create: `src/WindowsEasyEmoji.Core/Emoji/EmojiRecord.cs`
- Create: `src/WindowsEasyEmoji.Core/Emoji/EmojiVariant.cs`
- Create: `src/WindowsEasyEmoji.Core/Emoji/EmojiFlags.cs`
- Create: `src/WindowsEasyEmoji.Core/Emoji/LocalizedText.cs`
- Create: `src/WindowsEasyEmoji.Core/Search/KoreanTextNormalizer.cs`
- Test: `tests/WindowsEasyEmoji.Core.Tests/Search/KoreanTextNormalizerTests.cs`

- [ ] **Step 1: Write failing normalization tests**

Create `KoreanTextNormalizerTests.cs` with tests for lowercasing, separator-insensitive keys, shortcode tokens, and Hangul initial consonants.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet test tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj --filter KoreanTextNormalizerTests
```

Expected: tests fail because `KoreanTextNormalizer` does not exist.

- [ ] **Step 3: Implement schema records and normalizer**

Implement immutable record types for emoji data and a static `KoreanTextNormalizer` with `NormalizeQuery`, `ToCompactKey`, `ExtractTokens`, and `ToChosung`.

- [ ] **Step 4: Run tests and verify GREEN**

Run:

```powershell
dotnet test tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj --filter KoreanTextNormalizerTests
```

Expected: tests pass.

### Task 3: Search Ranking

**Files:**
- Create: `src/WindowsEasyEmoji.Core/Search/SearchMatchType.cs`
- Create: `src/WindowsEasyEmoji.Core/Search/SearchResult.cs`
- Create: `src/WindowsEasyEmoji.Core/Search/EmojiSearchService.cs`
- Create: `src/WindowsEasyEmoji.Core/User/UserEmojiState.cs`
- Test: `tests/WindowsEasyEmoji.Core.Tests/Search/EmojiSearchServiceTests.cs`

- [ ] **Step 1: Write failing search tests**

Create tests proving:
- Korean alias exact match ranks above English match.
- Chosung query `ㅎㅌ` finds heart emoji.
- Recent/favorite boosts reorder otherwise similar results.
- Hidden variants are penalized.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet test tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj --filter EmojiSearchServiceTests
```

Expected: tests fail because `EmojiSearchService` does not exist.

- [ ] **Step 3: Implement minimal search service**

Implement in-memory scoring from plan4: exact Korean alias, alias, Korean keyword, chosung, English keyword, prefix, contains, favorite, recent, use count, and hidden variant penalty.

- [ ] **Step 4: Run tests and verify GREEN**

Run:

```powershell
dotnet test tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj --filter EmojiSearchServiceTests
```

Expected: tests pass.

### Task 4: Repository and Sample Data

**Files:**
- Create: `src/WindowsEasyEmoji.Core/Emoji/EmojiRepository.cs`
- Create: `src/WindowsEasyEmoji.App/Data/emoji.json`
- Test: `tests/WindowsEasyEmoji.Core.Tests/Emoji/EmojiRepositoryTests.cs`

- [ ] **Step 1: Write failing repository tests**

Create tests proving JSON data can be loaded into `EmojiRecord` and invalid JSON produces a clear exception.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet test tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj --filter EmojiRepositoryTests
```

Expected: tests fail because `EmojiRepository` does not exist.

- [ ] **Step 3: Implement repository and seed sample data**

Implement `EmojiRepository.LoadFromJson(string json)` and add a small `emoji.json` seed with heart/laugh/thumbs/fire/cry examples.

- [ ] **Step 4: Run tests and verify GREEN**

Run:

```powershell
dotnet test tests/WindowsEasyEmoji.Core.Tests/WindowsEasyEmoji.Core.Tests.csproj --filter EmojiRepositoryTests
```

Expected: tests pass.

### Task 5: WPF and Platform Skeleton

**Files:**
- Create: `src/WindowsEasyEmoji.Platform/Keyboard/KeyboardHookService.cs`
- Create: `src/WindowsEasyEmoji.Platform/Keyboard/HotkeyService.cs`
- Create: `src/WindowsEasyEmoji.Platform/Clipboard/ClipboardPasteService.cs`
- Create: `src/WindowsEasyEmoji.App/Tray/TrayAppHost.cs`
- Modify: `src/WindowsEasyEmoji.App/App.xaml`
- Modify: `src/WindowsEasyEmoji.App/App.xaml.cs`
- Modify: `src/WindowsEasyEmoji.App/MainWindow.xaml`
- Modify: `src/WindowsEasyEmoji.App/MainWindow.xaml.cs`

- [ ] **Step 1: Add platform skeleton**

Create public classes with explicit method names but no unsafe behavior enabled by default: `Start`, `Stop`, `ShowOverlay`, `PasteText`.

- [ ] **Step 2: Wire WPF startup**

Start the app hidden in tray mode, expose an overlay window, and keep shutdown explicit.

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build WindowsEasyEmoji.sln
```

Expected: build succeeds.

### Task 6: Final Verification

**Files:**
- No new files.

- [ ] **Step 1: Run all tests**

Run:

```powershell
dotnet test WindowsEasyEmoji.sln
```

Expected: all tests pass.

- [ ] **Step 2: Build release**

Run:

```powershell
dotnet build WindowsEasyEmoji.sln -c Release
```

Expected: release build succeeds.
