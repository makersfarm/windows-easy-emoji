# Windows Easy Emoji

Korean-first emoji search overlay for Windows.

Windows' built-in emoji panel (`Win + .`) is convenient, but Korean search is weak. Windows Easy Emoji aims to replace that flow with a lightweight tray app that opens a fast searchable overlay, supports Korean aliases and initial consonant search, and pastes the selected emoji into the current app.

## Current Status

This repository is in early MVP development.

Implemented so far:

- .NET 8 / WPF solution structure
- Core emoji data schema
- Korean text normalization and initial consonant matching
- Rule-based local emoji search and ranking
- JSON emoji data loader and external Korean description seed data
- WPF search overlay skeleton
- Windows tray app host
- Clipboard paste into the previously active window
- Low-level `Win + .` keyboard hook
- Fallback `Ctrl + Alt + Space` hotkey
- JSON settings persistence
- Runtime tray toggles for shortcut and paste settings
- Optional original clipboard restore after paste
- Single-instance app guard

Not implemented yet:

- Full CLDR/alias data import
- Installer or Microsoft Store packaging

## Settings

The app creates a local settings file at:

```text
%APPDATA%\WindowsEasyEmoji\settings.json
```

The app also stores local usage state at:

```text
%APPDATA%\WindowsEasyEmoji\user-state.json
```

Current MVP settings:

- `ReplaceWinPeriod`: enables the `Win + .` replacement hook.
- `RegisterFallbackHotkey`: enables the fallback hotkey.
- `FallbackHotkey`: defaults to `Ctrl+Alt+Space`.
- `AutoPaste`: pastes the selected emoji into the previously active app. If disabled, selection copies only.
- `RestoreClipboardAfterPaste`: restores the previous text clipboard after paste. It defaults to `false` because some apps read the clipboard asynchronously.

User state tracks recent usage, use count, favorites, and custom aliases. Empty search results are ranked with this local state so frequently used emoji appear first.

## Development

This repo pins .NET through `global.json`.

```powershell
dotnet restore WindowsEasyEmoji.sln
dotnet test WindowsEasyEmoji.sln
dotnet build WindowsEasyEmoji.sln -c Release
```

If `dotnet` is not installed locally, install the .NET 8 SDK from Microsoft.

Regenerate bundled emoji data from staged external sources:

```powershell
dotnet run --project tools/WindowsEasyEmoji.DataBuilder/WindowsEasyEmoji.DataBuilder.csproj
```

## Project Structure

```text
src/WindowsEasyEmoji.Core      Search, schema, ranking, data loading
src/WindowsEasyEmoji.Platform  Windows API integration boundary
src/WindowsEasyEmoji.App       WPF tray app and search overlay
tools/WindowsEasyEmoji.DataBuilder
tests/WindowsEasyEmoji.Core.Tests
docs/                          Planning and product/design decisions
```

## License

MIT
