import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { _electron as electron, expect, test } from '@playwright/test';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const shellDir = path.resolve(__dirname, '..', '..', 'shell');
const fixtureIfoPath = path
  .resolve(__dirname, '..', '..', 'engine', 'Lughat.Engine.Api', 'Fixtures', 'spike-dict', 'spike-dict.ifo')
  .replace(/\\/g, '/');

/**
 * Launches the real app (dev-mode: renderer must already be built + copied — see
 * apps/shell's "start" script — and the engine DLL must already be built), imports the
 * spike fixture dictionary directly through the engine API (Playwright can't drive the
 * native OS file-picker dialog the UI's "Import dictionary" button opens), then verifies
 * a search actually surfaces that dictionary's article — an end-to-end check of the MVP
 * feature list in spec §6, matching this issue's acceptance criteria.
 */
test('imports a dictionary and looks up a word end to end', async () => {
  const app = await electron.launch({ args: [shellDir] });
  const window = await app.firstWindow();
  await window.waitForLoadState('domcontentloaded');

  const searchBox = window.getByPlaceholder(/search a word/i);
  await searchBox.waitFor({ timeout: 20_000 });

  await window.evaluate(async (ifoPath) => {
    const engine = await window.lughat.getEngineInfo();
    const response = await fetch(`${engine.baseUrl}/api/dictionaries`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${engine.token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ path: ifoPath }),
    });
    if (!response.ok) {
      throw new Error(`Import failed: ${response.status} ${await response.text()}`);
    }
  }, fixtureIfoPath);

  // Indexing the tiny fixture is near-instant, but give it a moment before searching.
  await window.waitForTimeout(1000);

  await searchBox.fill('apple');
  await searchBox.press('Enter');

  await expect(window.getByText('A round fruit with red or green skin and crisp flesh.')).toBeVisible({
    timeout: 10_000,
  });

  await app.close();
});
