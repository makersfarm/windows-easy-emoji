# emoji_korean

External raw seed data for Korean emoji descriptions.

- Source repository: https://github.com/xxxjjhhh/emoji_korean
- Raw data file: `emoji_korean.json`
- Package metadata: `package.json`
- Source license metadata: `MIT` in `package.json`
- Imported on: 2026-05-11

## Usage

This data is not our canonical emoji schema. It is kept as a raw external source
for later merging into the Windows Easy Emoji catalog.

Current observed limitations:

- Records only contain `emoji`, `hexcode`, and `description`.
- No category, CLDR version, English name, aliases, shortcode, skin tone metadata,
  or ranking fields.
- No regional flag or ZWJ sequence coverage in the current source file.
- Some `hexcode` values are duplicated across different emoji, so the actual
  `emoji` string should be treated as the safer identity.
