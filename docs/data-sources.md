# Data Sources

## Current Sources

### Internal MVP records

- Location: `src/WindowsEasyEmoji.App/Data/emoji.json`
- Purpose: high-quality hand-curated records for the first MVP searches.
- Status: canonical records inside the app bundle.

### emoji_korean

- Source: https://github.com/xxxjjhhh/emoji_korean
- Local raw copy: `data/external/emoji_korean/emoji_korean.json`
- License metadata: MIT in `data/external/emoji_korean/package.json`
- Imported version: `1.1.0`
- Current record count in source: 1,312

Use this source as a Korean description seed, not as canonical data.

Observed limitations:

- Only `emoji`, `hexcode`, and `description` are provided.
- It has no category, CLDR version, English name, alias, shortcode, skin tone,
  popularity, or variant metadata.
- It does not cover regional flags or ZWJ sequences in the current source file.
- Some `hexcode` values are duplicated for different emoji, so our generated
  app records use the actual emoji code points as identity.

### badrex/LLM-generated-emoji-descriptions

- Source: https://huggingface.co/datasets/badrex/LLM-generated-emoji-descriptions
- Linked project repository: https://github.com/badrex/emojeez
- Local raw copy: `data/external/badrex_llm_generated_emoji_descriptions/`
- Hugging Face license metadata: `cc`
- Linked project repository license: MIT
- Imported row count: 5,034

Use this source as external staging data for English tags, semantic descriptions,
and broad emoji coverage.

Observed strengths:

- Includes `tags` and `LLM description`, which are useful for English search
  enrichment and future semantic ranking.
- Covers multi-codepoint emoji, ZWJ sequences, and regional flags better than
  the current Korean description source.
- Has unique `character` and `unicode` values in the imported snapshot.

Observed limitations:

- It is English-only.
- `LLM description` is generated content and should be treated as lower trust
  than CLDR metadata.
- The Hugging Face dataset card uses the broad `cc` license tag, so exact
  distribution treatment should be reviewed before bundling this data into a
  public release.

### muan/unicode-emoji-json

- Source: https://github.com/muan/unicode-emoji-json
- Local raw copy: `data/external/muan_unicode_emoji_json/`
- Package license metadata: MIT
- Upstream data license reference: Unicode License Agreement
- Imported package version: `0.7.0`
- Current base emoji count in source: 1,914

Use this source as the canonical RGI backbone for ordering, Unicode groups,
English names, slugs, emoji versions, Unicode versions, and skin-tone support.

Observed strengths:

- Provides RGI-only emoji data, which is a better production base than random
  scraped lists.
- Includes Unicode group data and canonical display order.
- Consolidates skin tone variations into base emoji records with
  `skin_tone_support`, which keeps the base search index compact.
- Covers flags and ZWJ sequences that the current Korean description seed does
  not cover.

Observed limitations:

- It is English-only and does not solve Korean search directly.
- It does not include search aliases, Korean keywords, popularity, or local
  ranking metadata.
- Because the data originates from Unicode, the Unicode License Agreement needs
  to be respected in addition to the package MIT license.

### Unicode Emoji 17.0 official data

- Source: https://unicode.org/Public/emoji/latest/
- UCD emoji source: https://www.unicode.org/Public/17.0.0/ucd/emoji/
- Local raw copy: `data/external/unicode_emoji_17_0/`
- License metadata: Unicode Terms of Use / Unicode License
- Imported version: `17.0`
- Fully-qualified emoji count in `emoji-test.txt`: 3,944

Use this source as the authoritative sequence, qualification, property, ZWJ,
and variation selector reference.

Observed strengths:

- It is the upstream standard source.
- `emoji-test.txt` is already in CLDR keyboard order.
- Includes fully-qualified, minimally-qualified, unqualified, and component
  statuses.
- Provides separate property and variation selector files for validation.

Observed limitations:

- It is not a ready app search schema.
- Korean search terms are not included here; they come from CLDR annotations.

### Unicode CLDR Korean annotations

- Source: https://github.com/unicode-org/cldr-json
- Local raw copy: `data/external/unicode_cldr_annotations_ko_48_2/`
- Package license metadata: Unicode-3.0
- Imported package version: `48.2.0`
- Direct Korean annotation entries: 1,966
- Derived Korean annotation entries: 2,376

Use this source as the primary Korean keyword and TTS-name source.

Observed strengths:

- This directly solves our Korean search problem better than English-only emoji
  lists.
- Provides Korean `default` keyword arrays and `tts` names.
- Derived annotations cover variants, flags, and skin-tone sequences.
- It comes from the same Unicode/CLDR ecosystem as the canonical emoji order.

Observed limitations:

- CLDR Korean terms are stable baseline labels, not necessarily the best
  colloquial Korean search terms.
- Some emoji keys are base forms without `FE0F`, so merge logic must normalize
  variation selectors.

### datasets/emojis

- Source: https://github.com/datasets/emojis
- Local raw copy: `data/external/datasets_emojis/`
- License metadata: PDDL for the data package; Unicode terms apply to source
  data.
- Imported CSV row count: 5,225

Use this source only as a convenience CSV for comparison and ad-hoc analysis.

Observed strengths:

- Simple CSV columns: `Group`, `Subgroup`, `CodePoint`, `Status`,
  `Representation`, `Name`, `Section`.
- Status counts match the imported Unicode 17.0 official data.

Observed limitations:

- It is transformed data, not the authoritative upstream file.
- It does not add Korean search metadata beyond what CLDR already provides.

## Reviewed But Not Staged

### EmojiAll

- Source: https://www.emojiall.com/en/list
- Korean list: https://www.emojiall.com/ko/list

Use as a manual product/search reference only.

Reason not staged:

- No official bulk JSON/CSV/API was found.
- Site pages contain useful names, codepoints, popularity, sentiment, topics,
  and multilingual text, but scraping that into an open-source app would create
  unclear licensing and maintenance risk.

### Emojipedia

- Source: https://emojipedia.org/
- API note: https://emojipedia.org/emojipedia-api

Use as a release-tracking and UX reference only.

Reason not staged:

- New API applications are closed and access is case-by-case.
- Emojipedia descriptions and definitions are copyrighted by Emojipedia.
- Good for checking version pages and naming references, not for bundling data.

### Fantantonio/Emoji-List-Unicode

- Source: https://github.com/Fantantonio/Emoji-List-Unicode

Reason not staged:

- The repository targets Unicode Emoji 13.1 and is stale for our current
  Unicode 17.0 baseline.
- No clear repository license metadata was found.
- The data is generated from Unicode pages, so official Unicode data is a better
  source.

### arp242/uni

- Source: https://github.com/arp242/uni

Reason not staged:

- This is a Unicode lookup CLI/tool, not a redistributable emoji search dataset.
- It can be useful later as a QA/reference tool, but not as our app data source.

### walinejs/emojis

- Source: https://github.com/walinejs/emojis

Reason not staged:

- This is a custom emoji preset/image distribution pattern for Waline comments.
- It is GPL-3.0 and includes third-party/custom image packs, which is not aligned
  with our Unicode text emoji search index.

### Hugging Face osbm/emoji

- Source: https://huggingface.co/datasets/osbm/emoji

Reason not staged:

- The useful visible data is image-heavy and includes base64 image columns.
- License metadata was not clear from the dataset API response.
- It does not add better Korean text search metadata than CLDR.

### Hugging Face rocca/emojis

- Source: https://huggingface.co/datasets/rocca/emojis

Reason not staged:

- It is a large platform emoji image collection, not a text search metadata
  source.
- Platform image licensing is heterogeneous and risky for redistribution.
- The dataset is from early 2022, so it is not our current Unicode baseline.

### KoalaAI/Emoji-Suggester

- Source: https://huggingface.co/KoalaAI/Emoji-Suggester

Reason not staged:

- This is a 1.49 GB English text-classification model, not raw emoji metadata.
- OpenRAIL model licensing and runtime size do not fit an aggressive lightweight
  Windows MVP.

### Mange/emoji-data

- Source: https://github.com/Mange/emoji-data

Reason not staged:

- It is a useful CLDR/Unicode build pipeline, but the code is GPL-3.0.
- Generated output is documented separately from repo code license, but we can
  get the same primary data directly from Unicode and CLDR packages.

## Merge Policy

- Preserve existing curated records when the same emoji already exists.
- Prefer official Unicode 17.0 as the authoritative sequence/property source.
- Use `muan/unicode-emoji-json` as a convenient RGI base identity, group, and
  display-order backbone when it matches the official baseline.
- Layer Korean search terms from Unicode CLDR Korean annotations first, then
  supplement with `emoji_korean` and our own curated aliases.
- Use `badrex/LLM-generated-emoji-descriptions` only for lower-trust English
  semantic enrichment.
- Import external-only emoji as low-priority `external_seed` records.
- Set source-specific staging categories and groups until records are promoted
  into canonical categories.
- Keep source raw files under `data/external/...` so future merge scripts can be
  regenerated and audited.
- Promote external records to canonical records only after adding category,
  aliases, keywords, and quality review.
