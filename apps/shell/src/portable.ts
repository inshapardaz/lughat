import { existsSync } from 'node:fs';
import path from 'node:path';
import { app } from 'electron';

const PORTABLE_MARKER_FILE = 'portable.txt';
const PORTABLE_DATA_DIR = 'data';

/**
 * Portable mode (spec §6): dropping an empty "portable.txt" next to the app's own
 * executable makes it use a "data" folder right alongside itself instead of the OS
 * per-user profile directory — the whole point being the app can run off a removable
 * drive with no installer and no footprint left on the host machine. Only meaningful for
 * a packaged build; there's no single "next to the executable" location in dev mode, where
 * this always returns null (the existing per-OS default, or LUGHAT_DATA_DIR if a developer
 * has set that directly, both still apply as before).
 */
export function getPortableDataDir(): string | null {
  if (!app.isPackaged) {
    return null;
  }

  const appDir = path.dirname(process.execPath);
  if (!existsSync(path.join(appDir, PORTABLE_MARKER_FILE))) {
    return null;
  }

  return path.join(appDir, PORTABLE_DATA_DIR);
}
