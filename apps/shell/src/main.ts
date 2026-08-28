import { app, BrowserWindow, clipboard, dialog, ipcMain, Menu, nativeImage, Tray } from 'electron';
import path from 'node:path';
import { EngineSupervisor } from './engine-supervisor';
import { getPortableDataDir } from './portable';
import { createPopupWindow } from './popup';
import {
  isClipboardMonitoringEnabled,
  registerGlobalHotkey,
  setClipboardMonitoringEnabled,
  unregisterGlobalHotkey,
} from './quick-lookup';

const supervisor = new EngineSupervisor();
let mainWindow: BrowserWindow | null = null;
let tray: Tray | null = null;

// Portable mode (spec §6) also redirects Electron's own internal storage (cache, cookies,
// etc.) alongside the engine's — see engine-process.ts for the engine's side of this. Must
// happen before app.whenReady() for Electron to actually honor the new path.
const portableDataDir = getPortableDataDir();
if (portableDataDir) {
  app.setPath('userData', path.join(portableDataDir, 'electron'));
}

// Single-instance lock: a second launch focuses the existing window instead of spawning a
// second engine process and a second window.
const gotLock = app.requestSingleInstanceLock();
if (!gotLock) {
  app.quit();
} else {
  app.on('second-instance', () => {
    if (mainWindow) {
      if (mainWindow.isMinimized()) {
        mainWindow.restore();
      }
      mainWindow.show();
      mainWindow.focus();
    }
  });

  app.whenReady().then(bootstrap);
}

async function bootstrap(): Promise<void> {
  supervisor.onStatusChange((status) => {
    mainWindow?.webContents.send('engine:status', status);
  });

  await supervisor.start();
  createWindow();
  createTray();

  // Global hotkey to summon the popup from anywhere in the OS (spec §6). A failed
  // registration (accelerator already taken, or Electron's globalShortcut being a known
  // no-op under Wayland — spec §15's documented gap) just means the hotkey silently doesn't
  // fire; the tray's "Look up clipboard" item and clipboard monitoring stay available either
  // way as a fallback path into the same popup.
  if (!registerGlobalHotkey()) {
    console.error('[hotkey] failed to register the default global hotkey.');
  }

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
}

function createWindow(): void {
  mainWindow = new BrowserWindow({
    width: 1000,
    height: 700,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  });

  const devServerUrl = process.env.VITE_DEV_SERVER_URL;
  if (devServerUrl) {
    void mainWindow.loadURL(devServerUrl);
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  } else {
    // Copied in by scripts/copy-renderer.mjs, which both `npm run start` and the
    // electron-builder packaging step run before this ever loads.
    void mainWindow.loadFile(path.resolve(__dirname, '..', 'renderer-dist', 'index.html'));
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

function createTray(): void {
  const icon = nativeImage.createFromPath(path.join(__dirname, '..', 'assets', 'tray-icon.png'));
  tray = new Tray(icon);
  tray.setToolTip('Lughat');
  refreshTrayMenu();
  tray.on('click', () => {
    mainWindow?.show();
    mainWindow?.focus();
  });
}

function refreshTrayMenu(): void {
  tray?.setContextMenu(
    Menu.buildFromTemplate([
      {
        label: 'Show Lughat',
        click: () => {
          mainWindow?.show();
          mainWindow?.focus();
        },
      },
      { type: 'separator' },
      { label: 'Look up clipboard', click: () => createPopupWindow(clipboard.readText()) },
      {
        label: 'Monitor clipboard',
        type: 'checkbox',
        checked: isClipboardMonitoringEnabled(),
        click: (item) => {
          setClipboardMonitoringEnabled(item.checked);
          refreshTrayMenu();
        },
      },
      { type: 'separator' },
      { label: 'Quit', click: () => app.quit() },
    ]),
  );
}

ipcMain.handle('engine:info', () => supervisor.getInfo());

ipcMain.handle('dictionary:pick-file', async () => {
  if (!mainWindow) {
    return null;
  }

  const result = await dialog.showOpenDialog(mainWindow, {
    title: 'Import dictionary',
    properties: ['openFile'],
    filters: [
      { name: 'Dictionary files', extensions: ['ifo', 'mdx', 'csv', 'tsv', 'txt'] },
      { name: 'All files', extensions: ['*'] },
    ],
  });

  return result.canceled || result.filePaths.length === 0 ? null : result.filePaths[0];
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('will-quit', () => {
  unregisterGlobalHotkey();
  setClipboardMonitoringEnabled(false);
});

let quitting = false;
app.on('before-quit', async (event) => {
  if (quitting) {
    return;
  }

  event.preventDefault();
  quitting = true;
  await supervisor.shutdown();
  app.quit();
});
