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
- JSON emoji data loader
- WPF search overlay skeleton
- Windows tray menu skeleton
- Clipboard paste service skeleton

Not implemented yet:

- Real `Win + .` low-level keyboard hook
- Fallback hotkey registration
- Settings persistence
- Full CLDR/alias data import
- Installer or Microsoft Store packaging

## Development

This repo pins .NET through `global.json`.

```powershell
dotnet restore WindowsEasyEmoji.sln
dotnet test WindowsEasyEmoji.sln
dotnet build WindowsEasyEmoji.sln -c Release
```

If `dotnet` is not installed locally, install the .NET 8 SDK from Microsoft.

## Project Structure

```text
src/WindowsEasyEmoji.Core      Search, schema, ranking, data loading
src/WindowsEasyEmoji.Platform  Windows API integration boundary
src/WindowsEasyEmoji.App       WPF tray app and search overlay
tests/WindowsEasyEmoji.Core.Tests
docs/                          Planning and product/design decisions
```

## License

MIT
