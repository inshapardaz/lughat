import { describe, expect, it, vi } from 'vitest';
import { buildAnkiCardsFromFavorites } from './anki';
import { api } from './api';
import type { EngineInfo } from './global';
import type { FavoriteEntry, SearchHit } from './api';

vi.mock('./api', () => ({
  api: { lookup: vi.fn() },
}));

const engine: EngineInfo = { baseUrl: 'http://127.0.0.1:12345', token: 'test-token' };

function favorite(term: string, dictionaryId: string): FavoriteEntry {
  return { id: `${term}-${dictionaryId}`, term, dictionaryId, tag: null, createdAt: '2026-01-01T00:00:00Z' };
}

function hit(term: string, dictionaryId: string): SearchHit {
  return { dictionaryId, dictionaryName: 'Dict', headword: term, articleHtml: `<p>${term}</p>`, score: 1 };
}

describe('buildAnkiCardsFromFavorites', () => {
  it('picks the hit from the favorite\'s own dictionary when a term has matches in several', async () => {
    vi.mocked(api.lookup).mockResolvedValue([hit('apple', 'dict-a'), hit('apple', 'dict-b')]);

    const cards = await buildAnkiCardsFromFavorites(engine, [favorite('apple', 'dict-b')]);

    expect(cards).toEqual([{ front: 'apple', back: '<p>apple</p>' }]);
  });

  it('falls back to the first hit if the favorited dictionary no longer has one', async () => {
    vi.mocked(api.lookup).mockResolvedValue([hit('apple', 'dict-a')]);

    const cards = await buildAnkiCardsFromFavorites(engine, [favorite('apple', 'dict-deleted')]);

    expect(cards).toEqual([{ front: 'apple', back: '<p>apple</p>' }]);
  });

  it('skips a favorite whose term no longer resolves to anything', async () => {
    vi.mocked(api.lookup).mockResolvedValue([]);

    const cards = await buildAnkiCardsFromFavorites(engine, [favorite('gone', 'dict-a')]);

    expect(cards).toEqual([]);
  });
});
