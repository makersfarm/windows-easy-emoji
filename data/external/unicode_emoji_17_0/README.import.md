# Unicode Emoji 17.0 Import

- Source: https://unicode.org/Public/emoji/latest/
- UCD emoji source: https://www.unicode.org/Public/17.0.0/ucd/emoji/
- Retrieved at: 2026-05-11
- Local manifest: `manifest.json`

## Files

- `emoji-test.txt`: keyboard/display test data in CLDR order.
- `emoji-sequences.txt`: valid emoji sequences.
- `emoji-zwj-sequences.txt`: valid ZWJ sequences.
- `emoji-data.txt`: emoji character properties.
- `emoji-variation-sequences.txt`: text-style and emoji-style variation
  sequences.

## Integration Policy

Use this as the authoritative Unicode 17.0 sequence and property source.

For the app search index, use `emoji-test.txt` as the canonical fully-qualified
emoji inventory and display order. Use the UCD emoji files for property checks,
variation selector handling, and validation.
