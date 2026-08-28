# Phase 2 (v1) — notes

Status: **Go for review on 11 of 12 issues.** #48 (scan-on-hover) is deliberately not
implemented — see below, not a shortcut taken quietly.

## What was built

- **Formats**: DSL (ABBYY Lingvo) and XDXF providers, following the same pattern as
  Phase 1's StarDict/MDX/word-list providers.
- **Search/renderer**: fuzzy "did you mean" suggestions when a lookup has no exact match;
  per-tab back/forward navigation with cross-reference link interception inside the
  sandboxed article iframe; OS text-to-speech pronunciation; a virtual keyboard
  (IPA/Cyrillic/Urdu) for the search box.
- **Anki export**: a real `.apkg` builder (SQLite collection + zip), verified by actually
  opening the generated file, not just "didn't throw."
- **Portable mode**: a marker file next to the app's executable redirects both the engine's
  data and Electron's own storage into a folder alongside it.
- **Shell-native**: a frameless popup lookup window, a global hotkey, and opt-in clipboard
  monitoring, sharing the popup window and reachable today via the system tray (no dedicated
  Hotkeys/Advanced settings tab yet).
- **Release packaging**: code-signing/notarization plumbing wired to read from repo secrets
  that don't exist yet — turning it on later is adding secrets, not changing code.

## Verified

- Engine: 25/25 xUnit tests, including a new one that opens the generated Anki `.apkg`,
  extracts its SQLite database, and checks the actual notes/cards rows.
- Renderer: 23/23 Vitest tests, covering suggestions, back/forward navigation, cross-link
  postMessage interception, TTS, the virtual keyboard, and the popup view's hash-based
  routing.
- Shell: a new, minimal Vitest setup (apps/shell had none before this phase) — 7/7 tests
  for the pure logic that's actually testable without a real Electron GUI: what counts as
  "lookup-worthy" clipboard text, and portable-mode detection (via a mocked `electron`
  import).
- `dotnet build`/`test`, `npm run typecheck`, and all three `npm test` workspaces clean
  across the whole repo as of this write-up.

## What's *not* verified — the same ceiling as every prior phase

The popup window actually appearing positioned correctly, the global hotkey actually firing
system-wide, clipboard monitoring actually triggering off a real copy — all of this needs a
real GUI session. Confirmed (again) that `ELECTRON_RUN_AS_NODE=1` is enforced in this
sandbox and Electron refuses to open a window under it. Same manual desktop pass already
outstanding from Phases 0/1 (`npm run start` in `apps/shell`) should specifically also
exercise: the default hotkey, the tray's clipboard toggle and manual lookup, and dropping a
`portable.txt` next to a packaged build to confirm data actually lands in `./data`.

## #48 — scan-on-hover, deliberately not implemented

Every other Phase 2 shell feature is something Electron itself exposes an API for
(`globalShortcut`, `clipboard`) or that's pure application logic. "Word under the cursor in
*any* application" is neither — it needs OS accessibility APIs (Windows UI Automation,
macOS's Accessibility API, Linux AT-SPI), which means either a native Node addon per
platform or shelling out to platform-specific tooling, and either way, permission prompts
(macOS Accessibility access in particular) and behavior I have no way to exercise or verify
in this environment — not "hard to verify," genuinely no path to verify at all here, on any
platform, including Windows.

Shipping something that merely *looks* like hover-scan without being able to confirm it
does the one thing that matters (correctly read the word under the cursor over an arbitrary
other application's window) seemed like a worse outcome than being explicit that it needs
dedicated native-module work and a real desktop to build and test it — this issue is still
open, not silently dropped.

## Recommendation

Go, pending the manual desktop pass above, same shape as Phases 0 and 1's recommendation.
#48 needs a scoping decision from the team: pick up native-module work for real hover-scan,
or descope it in favor of the hotkey + clipboard paths already shipped, which cover the
same "look up whatever I just read" use case without needing OS accessibility hooks at all.
