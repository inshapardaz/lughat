import { app, BrowserWindow, ipcMain } from 'electron';
import type { ChildProcessWithoutNullStreams } from 'node:child_process';
import path from 'node:path';
import { EngineInfo, spawnEngine } from './engine-process';

let engineProcess: ChildProcessWithoutNullStreams | null = null;
let engineInfo: EngineInfo | null = null;

async function createWindow(): Promise<void> {
  const engine = await spawnEngine();
  engineProcess = engine.process;
  engineInfo = engine.info;

  const win = new BrowserWindow({
    width: 1000,
    height: 700,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  });

  // Vite dev server in development; the renderer's built output otherwise.
  const devServerUrl = process.env.VITE_DEV_SERVER_URL;
  if (devServerUrl) {
    await win.loadURL(devServerUrl);
  } else {
    await win.loadFile(path.resolve(__dirname, '..', '..', 'renderer', 'dist', 'index.html'));
  }
}

ipcMain.handle('engine:info', () => engineInfo);

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
  engineProcess?.kill();
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () => {
  engineProcess?.kill();
});
