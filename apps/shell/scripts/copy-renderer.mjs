// electron-builder (and Electron's own file:// loading in main.ts) can't reach across the
// monorepo into apps/renderer/dist, so this copies the renderer's build output into the
// shell package right before packaging or running `npm start`.
import { cpSync, existsSync, rmSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const source = path.resolve(__dirname, '..', '..', 'renderer', 'dist');
const destination = path.resolve(__dirname, '..', 'renderer-dist');

if (!existsSync(source)) {
  console.error(`Renderer build not found at ${source} — run "npm run build --workspace @lughat/renderer" first.`);
  process.exit(1);
}

rmSync(destination, { recursive: true, force: true });
cpSync(source, destination, { recursive: true });
console.log(`Copied renderer build to ${destination}`);
