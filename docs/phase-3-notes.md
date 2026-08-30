# Phase 3 (v2) — notes

Status: **Go for review on 5 of 8 issues** (#58, #59, #60, #61, #65). #62, #63, #64 are
deliberately not implemented this pass — see below, not shortcuts taken quietly. This mirrors
the #48 call in Phase 2: ship what can actually be verified, document what can't rather than
guess at it.

## What was built

- **#61 WordNet provider**: reads the real WNDB on-disk layout (`index.sense` marker +
  `index.{noun,verb,adj,adv}` + `data.{noun,verb,adj,adv}`, same directory), flattening
  WordNet's synset/lemma relational model to one article per lemma. Synonyms (other words in
  the same synset) and the hypernym ("broader term") are rendered as the same kind of
  clickable cross-reference link DSL/XDXF articles already use.
- **#60 Stemming/lemmatization**: a from-scratch classic Porter stemmer (`EnglishPorterStemmer`)
  behind an `IStemmer` interface and a `StemmerRegistry` keyed by language code. Indexing adds
  stemmed shadow fields (`headwordStemmed`/`articleStemmed`) via a `PerFieldAnalyzerWrapper`,
  analyzed with whichever stemmer the dictionary's (new) `Language` column names — defaults to
  `"en"`. Fulltext search ORs a stemmed reading of the query into the existing query, so a new
  language just means registering a new `IStemmer`; `SearchService`'s method signatures don't
  change.
- **#58 Kaikki.org import pipeline**: `KaikkiProvider` reads a Kaikki/Wiktextract `.jsonl`
  dump (one JSON object per line: `word`, `pos`, `senses[].glosses[]`), merging multiple
  part-of-speech lines for the same word into one article — same aggregation pattern as
  DSL/XDXF. Only the fields this app surfaces today are read; a dump carries much more
  (etymology, audio, translations) a future pass can pull in without reshaping this provider.
- **#59 Bundled starter dictionary**: `StarterDictionary/starter-en.jsonl`, ~55 common English
  words hand-authored in Kaikki's JSONL shape (not a real Kaikki download — see "known gap"
  below), imported automatically on first launch when the dictionary list is empty. Verified
  by actually running the engine against a scratch `LUGHAT_DATA_DIR` and confirming via curl
  that the starter dictionary imports, indexes, and is both exact- and fulltext-searchable.
- **#65 Additional UI language bundle workflow**: `i18n/index.ts` now drives language metadata
  (label, RTL/LTR, resource bundle) from one `LANGUAGES` map instead of a hardcoded `en`/`ur`
  list plus a `lang === 'ur' ? … : …` ternary in `SettingsView`. Added `scripts/validate-locales.mjs`
  (new `npm run validate-locales`) that fails if any bundle's keys drift from `en.json`'s. Added
  a third language, **Arabic (`ar.json`)**, by following exactly the four steps written up in
  `src/i18n/locales/README.md` — proving the architecture scales with zero component changes
  beyond the one `LANGUAGES` map entry.

## Verified

- Engine: 49/49 xUnit tests (was 39 before this phase — added WordNet, Kaikki, Porter
  stemmer, stemmed-search, and stemmer-registry coverage).
- Ran the engine directly (`dotnet run`, `LUGHAT_DATA_DIR` pointed at a scratch folder,
  `LUGHAT_ENGINE_TOKEN` set manually) and hit it with curl: confirmed the starter dictionary
  auto-imports on a fresh (empty) database, `GET /api/lookup?term=apple` returns the exact
  match, and `GET /api/search?query=running&mode=fulltext` returns the `run` entry — the
  stemming path working end-to-end, not just unit-tested in isolation.
- Renderer: 23/23 Vitest tests, `tsc -b --noEmit` clean, `npm run validate-locales` passes for
  both `ur.json` and the new `ar.json` against `en.json`.
- `dotnet ef migrations add AddDictionaryLanguage` generated and applied cleanly (adds
  `Dictionaries.Language`, default `"en"`, non-breaking for existing rows).

## What's *not* verified

Same ceiling as every prior phase for anything GUI-dependent: Arabic's RTL rendering, the
language picker showing the new option, and the starter dictionary appearing in a real
Electron window all still need the manual desktop pass already outstanding since Phase 0
(`npm run start` in `apps/shell`) — this sandbox still can't launch Electron's GUI
(`ELECTRON_RUN_AS_NODE=1` enforced).

## Known gap: the bundled starter dictionary isn't a real Kaikki download

`starter-en.jsonl` is 55 hand-authored entries in the right JSONL shape, not a slice of an
actual Kaikki.org dump — there's no network access in this sandbox to fetch one, and shipping
a large real dump would also blow well past what's reasonable to bundle in the installer
without a separate download-on-demand step. The **pipeline** (`KaikkiProvider`) is real and
tested against Kaikki's documented format; pointing it at an actual dump file works exactly
the same way. Swapping in a real, larger starter dictionary later is a content change (drop a
different `.jsonl` in `StarterDictionary/`), not a code change.

## #62, #63, #64 — deliberately not implemented

**#62 (Babylon BGL provider).** Babylon's `.bgl` format is a proprietary binary layout with no
authoritative public spec I have reliable, verifiable knowledge of — real-world `.bgl` files
are predominantly per-block gzip-compressed, and the block/length-encoding details I could
reconstruct from memory aren't something I can validate against an actual `.bgl` file in this
sandbox (no network access, no bundled sample). Implementing a binary parser I can't test
against real input and presenting it as "BGL support" would look done without being trustworthy
— it would very likely silently fail on real files while passing only against a fixture I
invented to match my own guesses. Same reasoning as #48 in Phase 2: don't ship something that
looks complete but has no real verification path. Needs either a real sample `.bgl` file to
validate against, or a decision to build on an existing open-source BGL reader instead of a
from-scratch implementation.

**#63 (Opt-in plugin system for online dictionary sources).** This is explicitly the app's
first non-offline feature — it needs a concrete reference source to build against (which
online dictionary API, what auth/rate-limit/ToS constraints, what the response shape looks
like), plus real UX and security decisions (network egress from a previously offline-only app,
how credentials/API keys are stored if the reference source needs one). Guessing at all of
that overnight risks locking in a shape the team didn't actually want — this is a "the team
should pick the reference source" scoping conversation, not an implementation gap.

**#64 (Optional user-owned settings/favorites sync).** "User-owned folder (WebDAV/Dropbox/
etc.)" spans real protocol and security tradeoffs: WebDAV needs a client and credential
storage, Dropbox/etc. needs OAuth and API quota handling, a plain local folder (the one
sync-agnostic common denominator) is trivial to build but may not be what "sync" means to the
team. Convergence semantics (last-write-wins vs. merge, conflict handling) are also a real
design decision, not just plumbing. Picking one silently and shipping it risks being the wrong
one people then have to migrate away from.

## Recommendation

Go on #58/#59/#60/#61/#65, pending the same manual desktop pass every phase has needed.
#62/#63/#64 need scoping input from the team before the next pass: a real `.bgl` sample (or a
decision to adopt an existing reader) for #62, a chosen reference online source for #63, and a
chosen sync mechanism for #64.
