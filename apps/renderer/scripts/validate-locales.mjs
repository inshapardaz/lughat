// Validates every /src/i18n/locales/<lang>.json bundle against en.json, the reference bundle
// (spec §10, issue #65). A bundle that's missing a key falls back to the English string at
// runtime (i18next's fallbackLng) rather than crashing, which makes a gap easy to ship by
// accident — this script is what actually catches it, in CI or locally.
import { readdirSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const localesDir = fileURLToPath(new URL('../src/i18n/locales', import.meta.url));

function flattenKeys(obj, prefix = '') {
  const keys = [];
  for (const [key, value] of Object.entries(obj)) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      keys.push(...flattenKeys(value, fullKey));
    } else {
      keys.push(fullKey);
    }
  }
  return keys;
}

function loadBundle(filename) {
  return JSON.parse(readFileSync(path.join(localesDir, filename), 'utf-8'));
}

const referenceFile = 'en.json';
const reference = new Set(flattenKeys(loadBundle(referenceFile)));

const bundleFiles = readdirSync(localesDir).filter((f) => f.endsWith('.json') && f !== referenceFile);

let ok = true;
for (const file of bundleFiles) {
  const keys = new Set(flattenKeys(loadBundle(file)));
  const missing = [...reference].filter((k) => !keys.has(k));
  const extra = [...keys].filter((k) => !reference.has(k));

  if (missing.length > 0 || extra.length > 0) {
    ok = false;
    console.error(`✗ ${file}`);
    for (const key of missing) console.error(`    missing: ${key}`);
    for (const key of extra) console.error(`    extra:   ${key}`);
  } else {
    console.log(`✓ ${file} (${keys.size} keys, matches ${referenceFile})`);
  }
}

if (!ok) {
  console.error('\nOne or more locale bundles are out of sync with en.json.');
  process.exit(1);
}

console.log(`\nAll ${bundleFiles.length} locale bundles match ${referenceFile}.`);
