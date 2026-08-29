import { clipboard, globalShortcut } from 'electron';
import { isLookupWorthy } from './lookup-worthy';
import { createPopupWindow } from './popup';

export const DEFAULT_HOTKEY = 'CommandOrControl+Shift+L';
const CLIPBOARD_POLL_MS = 700;

let clipboardTimer: ReturnType<typeof setInterval> | null = null;
let lastClipboardText = '';

/**
 * Registers the global hotkey that summons the popup with whatever's on the clipboard
 * (spec §6). Returns whether registration actually succeeded — it can fail if another app
 * already holds the accelerator, or (spec §15's documented gap) isn't supported at all under
 * Wayland, where Electron's globalShortcut is a known no-op.
 */
export function registerGlobalHotkey(accelerator: string = DEFAULT_HOTKEY): boolean {
  globalShortcut.unregisterAll();
  return globalShortcut.register(accelerator, () => {
    const text = clipboard.readText().trim();
    createPopupWindow(isLookupWorthy(text) ? text : '');
  });
}

export function unregisterGlobalHotkey(): void {
  globalShortcut.unregisterAll();
}

/** Opt-in (spec §6) — off until explicitly enabled via {@link setClipboardMonitoringEnabled}. */
export function setClipboardMonitoringEnabled(enabled: boolean): void {
  if (enabled) {
    if (clipboardTimer) {
      return;
    }
    lastClipboardText = clipboard.readText();
    clipboardTimer = setInterval(() => {
      const text = clipboard.readText().trim();
      if (text && text !== lastClipboardText && isLookupWorthy(text)) {
        lastClipboardText = text;
        createPopupWindow(text);
      }
    }, CLIPBOARD_POLL_MS);
  } else if (clipboardTimer) {
    clearInterval(clipboardTimer);
    clipboardTimer = null;
  }
}

export function isClipboardMonitoringEnabled(): boolean {
  return clipboardTimer !== null;
}
