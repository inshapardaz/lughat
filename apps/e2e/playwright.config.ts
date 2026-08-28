import { defineConfig } from '@playwright/test';

// No `use.browserName` — these tests launch the Electron app itself via `_electron`,
// not a browser. See docs/spike-notes.md / README for why this can't run in most
// sandboxed CI-like environments: Electron needs a real GUI session.
export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  retries: 0,
  reporter: 'list',
});
