import { contextBridge, ipcRenderer } from 'electron';
import type { EngineInfo } from './engine-process';

// The only bridge into the main process: read-only engine connection info.
// No filesystem, no Node globals — see spec §2's contextIsolation/nodeIntegration note.
contextBridge.exposeInMainWorld('lughat', {
  getEngineInfo: (): Promise<EngineInfo> => ipcRenderer.invoke('engine:info'),
});
