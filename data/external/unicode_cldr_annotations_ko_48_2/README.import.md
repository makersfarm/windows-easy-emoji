# Unicode CLDR Korean Annotations 48.2 Import

- Source: https://github.com/unicode-org/cldr-json
- Package: `cldr-annotations-full` and `cldr-annotations-derived-full`
- Package version: `48.2.0`
- Retrieved at: 2026-05-11
- Local manifest: `manifest.json`

## Files

- `annotations-ko.json`: direct Korean annotations and TTS names.
- `annotations-derived-ko.json`: derived Korean annotations for variants and
  sequences.
- `cldr-annotations-full.package.json`: upstream package metadata.
- `cldr-annotations-derived-full.package.json`: upstream derived package
  metadata.
- `LICENSE`: Unicode License v3.

## Integration Policy

Use this as the primary Korean keyword and TTS source.

Merge by exact emoji sequence after canonical identity is established from
Unicode official data or `muan/unicode-emoji-json`. Preserve our hand-curated
aliases as higher-priority search hints, then add CLDR Korean terms as stable
baseline keywords.
