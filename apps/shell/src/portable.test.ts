import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import path from 'node:path';

const existsSyncMock = vi.fn();
vi.mock('node:fs', () => ({ existsSync: (...args: unknown[]) => existsSyncMock(...args) }));

const appMock = { isPackaged: true };
vi.mock('electron', () => ({ app: appMock }));

describe('getPortableDataDir', () => {
  const originalExecPath = process.execPath;

  beforeEach(() => {
    existsSyncMock.mockReset();
    appMock.isPackaged = true;
    Object.defineProperty(process, 'execPath', { value: 'C:/Apps/Lughat/Lughat.exe', configurable: true });
  });

  afterEach(() => {
    Object.defineProperty(process, 'execPath', { value: originalExecPath, configurable: true });
  });

  it('returns null when the app is not packaged (no single "next to the exe" location in dev mode)', async () => {
    appMock.isPackaged = false;
    const { getPortableDataDir } = await import('./portable');

    expect(getPortableDataDir()).toBeNull();
    expect(existsSyncMock).not.toHaveBeenCalled();
  });

  it('returns null when no portable.txt marker sits next to the executable', async () => {
    existsSyncMock.mockReturnValue(false);
    const { getPortableDataDir } = await import('./portable');

    expect(getPortableDataDir()).toBeNull();
  });

  it('returns a "data" folder next to the executable when the marker is present', async () => {
    existsSyncMock.mockReturnValue(true);
    const { getPortableDataDir } = await import('./portable');

    expect(getPortableDataDir()).toBe(path.join('C:/Apps/Lughat', 'data'));
    expect(existsSyncMock).toHaveBeenCalledWith(path.join('C:/Apps/Lughat', 'portable.txt'));
  });
});
