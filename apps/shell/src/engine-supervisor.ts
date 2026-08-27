import type { ChildProcessWithoutNullStreams } from 'node:child_process';
import { EngineInfo, spawnEngine } from './engine-process';

const HEALTH_CHECK_INTERVAL_MS = 5_000;
const MAX_RESTART_BACKOFF_MS = 30_000;
const SHUTDOWN_GRACE_MS = 1_000;

export interface EngineStatus {
  info: EngineInfo | null;
  connected: boolean;
}

/**
 * Spawns the sidecar and keeps it alive: restarts it with exponential backoff if it exits
 * unexpectedly, and shuts it down gracefully (POST /api/shutdown, falling back to a hard
 * kill) on app quit. See spec §2 / the "Sidecar process supervision" issue.
 */
export class EngineSupervisor {
  private childProcess: ChildProcessWithoutNullStreams | null = null;
  private info: EngineInfo | null = null;
  private restartAttempt = 0;
  private healthCheckTimer: ReturnType<typeof setInterval> | null = null;
  private shuttingDown = false;
  private listener: ((status: EngineStatus) => void) | null = null;

  onStatusChange(listener: (status: EngineStatus) => void): void {
    this.listener = listener;
  }

  getInfo(): EngineInfo | null {
    return this.info;
  }

  async start(): Promise<EngineInfo> {
    const engine = await spawnEngine();
    this.childProcess = engine.process;
    this.info = engine.info;
    this.restartAttempt = 0;
    this.notify(true);

    this.childProcess.on('exit', () => this.handleUnexpectedExit());
    this.startHealthChecks();

    return this.info;
  }

  async shutdown(): Promise<void> {
    this.shuttingDown = true;
    if (this.healthCheckTimer) {
      clearInterval(this.healthCheckTimer);
    }

    if (!this.info || !this.childProcess) {
      return;
    }

    try {
      await fetch(`${this.info.baseUrl}/api/shutdown`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${this.info.token}` },
      });
    } catch {
      // The sidecar may already be gone, or refused the request — fall through to a kill.
    }

    await new Promise((resolve) => setTimeout(resolve, SHUTDOWN_GRACE_MS));
    if (this.childProcess && !this.childProcess.killed) {
      this.childProcess.kill();
    }
  }

  private notify(connected: boolean): void {
    this.listener?.({ info: this.info, connected });
  }

  private startHealthChecks(): void {
    this.healthCheckTimer = setInterval(() => {
      void this.pingOnce();
    }, HEALTH_CHECK_INTERVAL_MS);
  }

  private async pingOnce(): Promise<void> {
    if (!this.info) {
      return;
    }

    try {
      const response = await fetch(`${this.info.baseUrl}/api/ping`, {
        headers: { Authorization: `Bearer ${this.info.token}` },
      });
      if (!response.ok) {
        throw new Error(`ping returned ${response.status}`);
      }
    } catch {
      // A single failed ping might just be a slow response, not a dead process — the
      // child's own 'exit' event is what actually triggers a restart, below.
    }
  }

  private async handleUnexpectedExit(): Promise<void> {
    if (this.shuttingDown) {
      return;
    }

    if (this.healthCheckTimer) {
      clearInterval(this.healthCheckTimer);
    }
    this.notify(false);

    const backoff = Math.min(1000 * 2 ** this.restartAttempt, MAX_RESTART_BACKOFF_MS);
    this.restartAttempt += 1;
    await new Promise((resolve) => setTimeout(resolve, backoff));

    try {
      await this.start();
    } catch (err) {
      console.error('[engine-supervisor] restart attempt failed', err);
      void this.handleUnexpectedExit();
    }
  }
}
