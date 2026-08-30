# Adding a UI language

Spec §10 / issue #65. `en.json` is the reference bundle — every other bundle is validated
against its key set, not the other way around.

1. Copy `en.json` to `<code>.json` using the language's ISO 639-1 code (e.g. `ar.json`).
   Translate every string; keep the JSON structure (nesting, key names) identical.
2. Add one entry to the `LANGUAGES` map in [`../index.ts`](../index.ts):
   ```ts
   ar: { label: 'العربية', direction: 'rtl', resource: ar },
   ```
   (plus the `import ar from './locales/ar.json'` line at the top). `direction` is `'rtl'` or
   `'ltr'` — nothing else in the app needs to know which languages are RTL; `directionFor()`
   and the language picker in Settings both read this map.
3. Run `npm run validate-locales --workspace @lughat/renderer` — it fails the build if the new
   bundle is missing (or has extra) keys compared to `en.json`.
4. Optional: if the language benefits from a bundled webfont (the way `ur.json` bundles Noto
   Nastaliq Urdu — see `../../styles/fonts.css`), add an `@font-face` and an
   `html[lang='<code>']` rule there. Not required — the system sans-serif stack in
   `fonts.css`'s default rule is legible for most scripts.

No other file changes are required — no component reads a hardcoded language list. The
`ar.json` bundle in this directory exists specifically to prove that: it was added following
exactly these four steps.
