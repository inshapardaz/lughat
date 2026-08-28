import { ChildProcessWithoutNullStreams, spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import path from 'node:path';
import readline from 'node:readline';
import { app } from 'electron';

export interface EngineInfo {
  baseUrl: string;
  token: string;
}

const READY_PREFIX = 'READY:';
const HANDSHAKE_TIMEOUT_MS = 15_000;

/**
 * Spawns the .NET sidecar and performs the handshake described in spec §2:
 * the shell generates a per-launch bearer token and passes it via env var,
 * the sidecar binds to loopback on an ephemeral port and prints "READY:<port>".
 */
export function spawnEngine(): Promise<{ process: ChildProcessWithoutNullStreams; info: EngineInfo }> {
  return new Promise((resolve, reject) => {
    const token = randomBytes(16).toString('hex');
    const { command, args } = resolveEngineCommand();

    const child = spawn(command, args, {
      env: { ...process.env, LUGHAT_ENGINE_TOKEN: token },
    });

    const timeout = setTimeout(() => {
      child.kill();
      reject(new Error('Engine did not become ready within the handshake timeout.'));
    }, HANDSHAKE_TIMEOUT_MS);

    const rl = readline.createInterface({ input: child.stdout });
    rl.on('line', (line) => {
      if (line.startsWith(READY_PREFIX)) {
        clearTimeout(timeout);
        const port = line.slice(READY_PREFIX.length).trim();
        resolve({ process: child, info: { baseUrl: `http://127.0.0.1:${port}`, token } });
      }
    });

    child.stderr.on('data', (chunk) => {
      console.error(`[engine] ${chunk.toString().trim()}`);
    });

    child.on('error', (err) => {
      clearTimeout(timeout);
      reject(err);
    });

    child.on('exit', (code) => {
      console.error(`[engine] process exited with code ${code}`);
    });
  });
}

function resolveEngineCommand(): { command: string; args: string[] } {
  if (app.isPackaged) {
    // electron-builder copies the self-contained, untrimmed publish output (see
    // apps/engine/publish.sh) to extraResources under "engine" — it needs no `dotnet`
    // install on the target machine, so it's launched directly.
    const exeName = process.platform === 'win32' ? 'Lughat.Engine.Api.exe' : 'Lughat.Engine.Api';
    return { command: path.join(process.resourcesPath, 'engine', exeName), args: [] };
  }

  // Dev-mode: launch the already-built engine DLL via the `dotnet` host.
  const enginePath = path.resolve(
    __dirname,
    '..',
    '..',
    'engine',
    'Lughat.Engine.Api',
    'bin',
    'Debug',
    'net9.0',
    'Lughat.Engine.Api.dll',
  );
  return { command: 'dotnet', args: [enginePath] };
}
