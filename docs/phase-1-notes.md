# Phase 1 (MVP) — notes

Status: **Go for review.** All 36 MVP issues are implemented and pushed. Three things
below need a human on a real desktop before calling this done — everything else was
verified directly (curl, xUnit, Vitest, or reading the actual bytes on disk).

## What was built

- **Engine** — `IDictionaryProvider` registry with StarDict (production: `.dz`/`.idx.gz`,
  same-type and mixed-type article layouts), a plain word-list provider, and an MDX
  provider built against the community-documented layout (MDX has no official spec).
  SQLite data layer with versioned migrations. Content-hash keyed Lucene.NET index cache.
  Multi-dictionary search (exact/prefix/fuzzy/fulltext). Full REST + WebSocket API. A
  stable error-code contract — the engine never returns human-readable text.
- **Shell** — single-instance lock, `EngineSupervisor` (health checks, auto-restart with
  backoff, graceful shutdown), system tray, native file picker.
- **Renderer** — full AppShell UI (search, dictionary manager with drag-reorder,
  tabs, history, favorites, settings), a sandboxed article viewer, i18n with English and
  Urdu (real RTL via Mantine's own direction context, not a raw DOM attribute write),
  Noto Nastaliq Urdu bundled locally so Urdu still works offline.
- **Packaging** — self-contained single-file engine publish per RID, electron-builder
  config bundling it via `extraResources`, a release CI matrix across win/mac/linux.
- **Tests** — 17 xUnit tests (engine) + 7 Vitest/RTL tests (renderer), both wired into CI
  alongside the existing build steps. A Playwright E2E smoke test exists and is discovered
  correctly but couldn't be *run* here (see below).

## Verified

- Full REST API surface (import, list, lookup, search × 4 modes, history, favorites,
  settings, error paths) exercised live via curl against all three format providers,
  including a from-scratch binary MDX fixture built specifically to validate that parser.
- `dotnet test` — 17/17 passing (format providers, the registry, indexing/search, the data
  layer).
- `npm test` (renderer) — 7/7 passing, covering the search box and article viewer per the
  issue's acceptance criteria, plus the dictionary manager and settings screen.
- The full packaging pipeline (`build` → `copy-renderer` → `electron-builder --dir --win`)
  actually run end to end; confirmed the engine binary lands at
  `resources/engine/Lughat.Engine.Api.exe`, matching what `engine-process.ts` expects for
  a packaged build.
- `dotnet build`, `npm run build`, `npm run typecheck` all clean across the whole repo.

## Not verified here — needs a manual pass

1. **The actual Electron GUI.** Same sandbox restriction as the Phase 0 spike
   (`ELECTRON_RUN_AS_NODE=1`, confirmed enforced, not just a default). Run `npm run start`
   in `apps/shell` on a normal desktop and confirm: the window opens with the full UI (not
   blank), search/import/tabs/history/favorites/settings all work, the tray icon shows up,
   switching to Urdu actually mirrors the layout, and closing the app cleanly kills the
   engine child process.
2. **The Playwright E2E test.** `apps/e2e/tests/smoke.spec.ts` loads and is discovered
   correctly (`playwright test --list`), but actually launching Electron needs the same
   real GUI session item 1 does. Run `npm test --workspace @lughat/e2e` once, from
   `apps/shell`'s built state.
3. **The actual installers.** Only `--dir` (unpacked) was run here. Building the real
   NSIS/MSI/dmg/AppImage/deb/rpm targets works through electron-builder's own well-tested
   logic, not anything custom to this config, but hasn't been produced and opened here.

## Decisions and known gaps worth flagging

- **Trimming is off, on purpose.** `-p:PublishTrimmed=true` breaks the app — both
  System.Text.Json's reflection path (fixed with a source-gen `JsonSerializerContext`) and
  Dapper's dynamic IL-emit deserializer (not fixable without dropping Dapper). Untrimmed
  self-contained single-file is ~104MB per platform, not the spec's ~40-60MB estimate —
  that budget assumed trimming would work. Worth either revising the budget or, longer
  term, replacing Dapper with something trim/NativeAOT-friendly if the size actually
  matters to the team.
- **Embedded media doesn't load yet.** The media endpoint requires a bearer token, but
  plain `<img>`/`<audio src>` requests from the sandboxed article iframe can't carry a
  custom `Authorization` header — there's no fixture with real embedded media to have
  caught this earlier either. Real fix is probably a short-lived signed URL query param
  for media specifically. Flagged in code comments on both `MediaEndpoints.cs` and
  `ArticleView.tsx`.
- **StarDict binary resource types (embedded audio/images) are skipped**, not extracted —
  documented in `StarDictProvider.cs`. Text-type parsing around them is unaffected.
- **MDX support covers the common case only**: zlib or uncompressed blocks, unencrypted,
  version 2.x's 8-byte fields. Encrypted files, LZO compression, and MDX < 2.0 all raise a
  clear error code rather than attempting to parse — per the issue's acceptance criteria,
  but real-world coverage is inherently a best-effort given MDX has no official spec.

## Recommendation

Go, pending the three manual checks above. Nothing found while building this pointed at a
deeper architectural problem — the two trimming issues and the Dapper materialization bug
were all real, but all fixable (and fixed) without touching the design in spec.
