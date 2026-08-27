import { app, BrowserWindow, dialog, ipcMain, Menu, nativeImage, Tray } from 'electron';
import path from 'node:path';
import { EngineSupervisor } from './engine-supervisor';

const supervisor = new EngineSupervisor();
let mainWindow: BrowserWindow | null = null;
let tray: Tray | null = null;

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
    void mainWindow.loadFile(path.resolve(__dirname, '..', '..', 'renderer', 'dist', 'index.html'));
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

function createTray(): void {
  const icon = nativeImage.createFromPath(path.join(__dirname, '..', 'assets', 'tray-icon.png'));
  tray = new Tray(icon);
  tray.setToolTip('Lughat');
  tray.setContextMenu(
    Menu.buildFromTemplate([
      {
        label: 'Show Lughat',
        click: () => {
          mainWindow?.show();
          mainWindow?.focus();
        },
      },
      { type: 'separator' },
      { label: 'Quit', click: () => app.quit() },
    ]),
  );
  tray.on('click', () => {
    mainWindow?.show();
    mainWindow?.focus();
  });
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
