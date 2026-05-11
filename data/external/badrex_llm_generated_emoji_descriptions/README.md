# badrex/LLM-generated-emoji-descriptions

External raw seed data for English emoji descriptions and tags.

- Source dataset: https://huggingface.co/datasets/badrex/LLM-generated-emoji-descriptions
- Linked project repository: https://github.com/badrex/emojeez
- Hugging Face dataset license metadata: `cc`
- Linked project repository license: MIT, copied as `LICENSE.emojeez`
- Imported on: 2026-05-11

## Local Files

- `README.source.md`: Hugging Face dataset card snapshot.
- `train-00000-of-00001.parquet`: original source data file.
- `rows.jsonl`: API-exported JSONL snapshot for easier local inspection.
- `rows.preview.json`: first 100 rows for quick review.
- `manifest.json`: import metadata.

## Observed Shape

- Rows: 5,034
- Columns: `character`, `unicode`, `short description`, `tags`, `LLM description`
- Unique `character`: 5,034
- Unique `unicode`: 5,034
- Multi-codepoint unicode rows: 3,648
- ZWJ unicode rows: 2,501
- Regional flag rows: 258
- Average tags per row: about 6.64

## Usage

This source is not our canonical emoji schema. Use it as external staging data
for future merge work.

Recommended use:

- English alias/tag enrichment.
- Semantic description source for future ranking or embedding experiments.
- Coverage source for flags, ZWJ sequences, and multi-codepoint emoji.

Do not promote directly into the app bundle until the exact license treatment is
decided. The Hugging Face card uses the broad `cc` license tag, while the linked
project repository is MIT licensed.
