# Translation Guide

This mod now supports SMAPI i18n files.

## How to add a translation

1. Copy `default.json`.
2. Rename the copy to your locale code, for example:
   - `fr.json`
   - `de.json`
   - `ja.json`
   - `pt-BR.json`
3. Translate only the values on the right side.
4. Keep all keys exactly the same.
5. Keep token placeholders intact, for example:
   - `{{headline}}`
   - `{{date}}`
   - `{{title}}`
   - `{{code}}`

## Notes

- Missing keys automatically fall back to English from `default.json`.
- JSON must stay valid (no trailing commas, double quotes only).
- AI prompt language is locale-aware: the active SMAPI locale is forwarded into Player2 prompt rules.
- This means adding files like `es.json` will also instruct AI-generated player-facing text to answer in Spanish, while command/JSON keys stay in English.
