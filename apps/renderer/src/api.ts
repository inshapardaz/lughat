import type { EngineInfo } from './global';

export interface LookupResult {
  headword: string;
  article: string;
}

export async function lookup(
  engine: EngineInfo,
  term: string,
  mode: 'exact' | 'prefix',
): Promise<LookupResult[]> {
  const url = new URL('/api/lookup', engine.baseUrl);
  url.searchParams.set('term', term);
  url.searchParams.set('mode', mode);

  const response = await fetch(url, {
    headers: { Authorization: `Bearer ${engine.token}` },
  });

  if (!response.ok) {
    throw new Error(`Lookup failed with status ${response.status}`);
  }

  return response.json();
}
