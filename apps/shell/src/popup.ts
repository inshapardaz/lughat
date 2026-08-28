import { BrowserWindow, screen } from 'electron';
import path from 'node:path';

/**
 * The frameless, always-on-top lookup popup (spec §6/§10) — summoned by the global hotkey
 * or clipboard monitoring, positioned near the cursor, and closed as soon as it loses focus
 * (clicking elsewhere dismisses it, matching how OS-level quick-lookup popups usually behave).
 * Shares the same renderer bundle as the main window; App.tsx switches to PopupView.tsx based
 * on the URL hash rather than this being a separate Vite app.
 */
export function createPopupWindow(term: string): BrowserWindow {
  const cursor = screen.getCursorScreenPoint();
  const display = screen.getDisplayNearestPoint(cursor);

  const width = 420;
  const height = 320;
  const x = Math.min(cursor.x + 12, display.workArea.x + display.workArea.width - width);
  const y = Math.min(cursor.y + 12, display.workArea.y + display.workArea.height - height);

  const popup = new BrowserWindow({
    width,
    height,
    x,
    y,
    frame: false,
    alwaysOnTop: true,
    resizable: false,
    skipTaskbar: true,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  });

  const hash = `/popup?term=${encodeURIComponent(term)}`;
  const devServerUrl = process.env.VITE_DEV_SERVER_URL;
  if (devServerUrl) {
    void popup.loadURL(`${devServerUrl}#${hash}`);
  } else {
    // Copied in by scripts/copy-renderer.mjs, same as the main window.
    void popup.loadFile(path.resolve(__dirname, '..', 'renderer-dist', 'index.html'), { hash });
  }

  popup.on('blur', () => popup.close());

  return popup;
}
