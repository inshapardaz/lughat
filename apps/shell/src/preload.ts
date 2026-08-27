import { contextBridge, ipcRenderer } from 'electron';
import type { EngineInfo } from './engine-process';
import type { EngineStatus } from './engine-supervisor';

// A narrow, typed bridge into the main process — engine connection info, a native file
// picker, and engine connectivity status. No filesystem, no Node globals — see spec §2's
// contextIsolation/nodeIntegration note.
contextBridge.exposeInMainWorld('lughat', {
  getEngineInfo: (): Promise<EngineInfo> => ipcRenderer.invoke('engine:info'),

  pickDictionaryFile: (): Promise<string | null> => ipcRenderer.invoke('dictionary:pick-file'),

  onEngineStatus: (callback: (status: EngineStatus) => void): (() => void) => {
    const listener = (_event: Electron.IpcRendererEvent, status: EngineStatus) => callback(status);
    ipcRenderer.on('engine:status', listener);
    return () => ipcRenderer.removeListener('engine:status', listener);
  },
});
