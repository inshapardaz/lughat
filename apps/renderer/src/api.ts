import type { EngineInfo } from './global';

export interface DictionaryRecord {
  id: string;
  name: string;
  format: string;
  filePath: string;
  contentHash: string;
  enabled: boolean;
  groupId: string | null;
  sortOrder: number;
  indexedAt: string | null;
}

export interface GroupRecord {
  id: string;
  name: string;
  sortOrder: number;
}

export interface SearchHit {
  dictionaryId: string;
  dictionaryName: string;
  headword: string;
  articleHtml: string;
  score: number;
}

export interface HistoryEntry {
  id: string;
  term: string;
  dictionaryId: string;
  timestamp: string;
}

export interface FavoriteEntry {
  id: string;
  term: string;
  dictionaryId: string;
  tag: string | null;
  createdAt: string;
}

export type SearchMode = 'exact' | 'prefix' | 'fuzzy' | 'fulltext';

export class EngineApiError extends Error {
  constructor(
    public readonly code: string,
    message: string,
  ) {
    super(message);
  }
}

async function request<T>(engine: EngineInfo, path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${engine.baseUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${engine.token}`,
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new EngineApiError(body?.error ?? 'unknown', body?.detail ?? `Request to ${path} failed with ${response.status}.`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  listDictionaries: (engine: EngineInfo) =>
    request<{ dictionaries: DictionaryRecord[]; groups: GroupRecord[] }>(engine, '/api/dictionaries'),

  importDictionary: (engine: EngineInfo, path: string) =>
    request<DictionaryRecord>(engine, '/api/dictionaries', { method: 'POST', body: JSON.stringify({ path }) }),

  removeDictionary: (engine: EngineInfo, id: string) =>
    request<void>(engine, `/api/dictionaries/${id}`, { method: 'DELETE' }),

  setDictionaryEnabled: (engine: EngineInfo, id: string, enabled: boolean) =>
    request<void>(engine, `/api/dictionaries/${id}/enabled`, { method: 'PUT', body: JSON.stringify({ enabled }) }),

  reorderDictionary: (engine: EngineInfo, id: string, groupId: string | null, sortOrder: number) =>
    request<void>(engine, `/api/dictionaries/${id}/order`, {
      method: 'PUT',
      body: JSON.stringify({ groupId, sortOrder }),
    }),

  createGroup: (engine: EngineInfo, name: string) =>
    request<GroupRecord>(engine, '/api/groups', { method: 'POST', body: JSON.stringify({ name }) }),

  lookup: (engine: EngineInfo, term: string) => request<SearchHit[]>(engine, `/api/lookup?term=${encodeURIComponent(term)}`),

  search: (engine: EngineInfo, query: string, mode: SearchMode) =>
    request<SearchHit[]>(engine, `/api/search?query=${encodeURIComponent(query)}&mode=${mode}`),

  history: (engine: EngineInfo) => request<HistoryEntry[]>(engine, '/api/history'),

  favorites: (engine: EngineInfo) => request<FavoriteEntry[]>(engine, '/api/favorites'),

  addFavorite: (engine: EngineInfo, term: string, dictionaryId: string, tag: string | null = null) =>
    request<FavoriteEntry>(engine, '/api/favorites', { method: 'POST', body: JSON.stringify({ term, dictionaryId, tag }) }),

  removeFavorite: (engine: EngineInfo, id: string) => request<void>(engine, `/api/favorites/${id}`, { method: 'DELETE' }),

  getSetting: async (engine: EngineInfo, key: string): Promise<unknown | null> => {
    const response = await fetch(`${engine.baseUrl}/api/settings/${encodeURIComponent(key)}`, {
      headers: { Authorization: `Bearer ${engine.token}` },
    });
    if (response.status === 204) {
      return null;
    }
    if (!response.ok) {
      return null;
    }
    return response.json();
  },

  setSetting: (engine: EngineInfo, key: string, value: unknown) =>
    request<void>(engine, `/api/settings/${encodeURIComponent(key)}`, { method: 'PUT', body: JSON.stringify(value) }),

  mediaUrl: (engine: EngineInfo, dictionaryId: string, relativePath: string) =>
    `${engine.baseUrl}/api/media/${dictionaryId}/${relativePath}`,

  exportAnki: async (engine: EngineInfo, deckName: string, cards: { front: string; back: string }[]): Promise<Blob> => {
    const response = await fetch(`${engine.baseUrl}/api/anki-export`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${engine.token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ deckName, cards }),
    });
    if (!response.ok) {
      const body = await response.json().catch(() => null);
      throw new EngineApiError(body?.error ?? 'unknown', body?.detail ?? 'Anki export failed.');
    }
    return response.blob();
  },
};
