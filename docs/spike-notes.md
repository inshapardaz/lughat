# Phase 0 spike — notes

Status: **Go**, with two toolchain decisions to make before Phase 1 starts.

## What was built

- `apps/engine/Lexroot.Engine.Api` — ASP.NET Core Minimal API on Kestrel. Binds to
  `127.0.0.1` on an ephemeral port, prints `READY:<port>` on startup, rejects any request
  without the expected `Authorization: Bearer <token>` header. Indexes a tiny fixture
  StarDict dictionary into an in-memory Lucene.NET index and serves `/api/ping` and
  `/api/lookup?term=&mode=exact|prefix`.
- `apps/shell` — Electron main process. Generates the per-launch token, spawns the engine
  with it in the environment, reads the `READY:` line off stdout, opens a `BrowserWindow`
  with `contextIsolation`/`sandbox` on and `nodeIntegration` off, and exposes engine
  connection info to the renderer through a `contextBridge` preload (`window.lexroot`).
- `apps/renderer` — React + Mantine (Vite). A single search box that calls
  `getEngineInfo()` over the bridge, then hits `/api/lookup` directly against the engine's
  loopback URL with the bearer token.
- `.github/workflows/ci.yml` — typechecks the shell, builds the renderer, builds the
  engine solution on every push/PR.

## Verified

- Engine: built and run standalone via `dotnet run`; `curl`'d directly —
  unauthenticated `/api/ping` returns 401, authenticated returns 200, `/api/lookup`
  returns correct results for both an exact term (`cat`) and a prefix (`ap` → `apple`),
  and an empty array for a non-existent term.
- Shell: `tsc --noEmit` passes; the engine-spawn/handshake code path is exercised
  indirectly (same `readline`-on-stdout logic the engine test above proves is fed the
  right output).
- Renderer: `vite build` succeeds and produces a working static bundle.
- All three of the above run clean via the exact commands `ci.yml` uses.

## Not verified here — needs a manual pass

Launching the actual Electron window (`npm run start` from `apps/shell`) requires a real
GUI session. The sandbox this spike was built in forces `ELECTRON_RUN_AS_NODE=1`, which
makes `require('electron')` return a path string instead of the API — Electron refuses to
open a window under it, by design (it's what stops an agent from popping windows on
someone's desktop unasked). Before Phase 1 starts, run `npm run start` in `apps/shell`
on a normal desktop session once, and confirm:

- the window opens and shows the search UI (not a blank/white screen)
- typing "apple", "book", or "cat" returns the fixture article text
- closing the window also terminates the `dotnet` engine child process (check Task
  Manager / Activity Monitor — no orphaned `dotnet` process left running)

## Toolchain findings that affect Phase 1

1. **Electron's latest major (44) needs Node ≥ 22.12.** Its installer uses Node's newer
   synchronous `require(esm)` support, which this dev machine's Node 20.17 doesn't have —
   installing it fails outright (`ERR_REQUIRE_ESM`), it's not a runtime-only issue. The
   spike pins `electron` to `^37.10.3`, the newest major that still installs cleanly on
   Node 20. **Decision needed:** either standardize the team/CI on Node 22+ and move to
   Electron's actual latest, or deliberately stay on the Electron 37 line for now. The
   spec (§3) says "Electron (latest stable)" — that's aspirational until the Node version
   is settled.
2. **`npm audit` flags `extract-zip` (via Electron's own install tooling) as high
   severity** — a symlink path-traversal issue. It only runs once, at `npm install` time,
   unzipping Electron's binary from Electron's own official GitHub releases — not
   reachable at runtime or from untrusted input. Left as-is for the spike; worth
   re-checking once the Node/Electron version question above is settled, since the fix
   likely arrives bundled with a newer Electron anyway.
3. **Vite 8's default bundler (Rolldown) failed to load its native binary** on this
   machine (`Cannot find module '@rolldown/binding-win32-x64-msvc'`) — also traces back to
   the same Node-version gap. The renderer is pinned to the stable Vite 6 line
   (`^6.3.5`) instead, which builds cleanly.

None of these block Phase 1 — they just mean the Node version for contributors/CI should
be pinned deliberately rather than left to "whatever's installed," and that pin should be
decided before the packaging work in Phase 1 (self-contained builds, electron-builder)
starts.

## StarDict spike scope

The spike reader (`Formats/StarDictReader.cs`) only handles uncompressed `.idx`/`.dict`
files with `sametypesequence=m` (plain text, one type for every entry) — enough to prove
the parse → index → lookup pipeline. The Phase 1 "StarDict provider (production quality)"
issue is where `.dict.dz` decompression and mixed per-entry type markers get added.

## Recommendation for Phase 1

Go. The handshake, auth, indexing, and UI-to-engine wiring all work as designed in the
spec. Before starting Phase 1 issues:

- Settle the Node version question above (recommend: move to Node 22 LTS, then Electron's
  actual latest — avoids carrying a pinned-old-major decision into the real product).
- Do the manual Electron launch check above once, on a normal desktop.
