# muan/unicode-emoji-json Import

- Source: https://github.com/muan/unicode-emoji-json
- Package: `unicode-emoji-json`
- Imported package version: `0.7.0`
- Retrieved at: 2026-05-11
- Local manifest: `manifest.json`

## Files

- `data-by-emoji.json`: 1,914 RGI base emoji records keyed by emoji.
- `data-by-group.json`: 9 Unicode group buckets with ordered emoji records.
- `data-ordered-emoji.json`: 1,914 emoji in canonical display order.
- `data-emoji-components.json`: 9 emoji component characters.
- `README.md`, `LICENSE`, `package.json`, and `index.d.ts`: raw upstream files.

## License Notes

- The npm package metadata declares MIT.
- The upstream README also references the Unicode License Agreement for Unicode
  source data: https://www.unicode.org/license.html

## Integration Policy

Use this source as the canonical RGI backbone for ordering, Unicode group,
English name, slug, emoji version, Unicode version, and skin-tone support.

Do not use it as Korean search data by itself. Korean labels, aliases, and
search keywords should be merged from Korean-specific sources or our curated
records.

Skin tone variations are represented as support metadata on the base emoji, not
as separate entries. If we later expose skin tone variants in the UI, generate
those variants from `skin_tone_support` and `data-emoji-components.json`.
