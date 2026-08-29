import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // `tsc` compiles src/**/*.test.ts into dist/ too (dist/ isn't scoped to non-test files —
    // see tsconfig.json) — without this, Vitest's default glob picks up both the source and
    // the stale compiled CommonJS copies, and the latter can't even be loaded by Vitest.
    include: ['src/**/*.test.ts'],
  },
});
