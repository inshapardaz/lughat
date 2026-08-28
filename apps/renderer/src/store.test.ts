import { afterEach, describe, expect, it, vi } from 'vitest';
import { useAppStore } from './store';
import { api } from './api';
import type { EngineInfo } from './global';
import type { SearchHit } from './api';

vi.mock('./api', () => ({
  api: {
    lookup: vi.fn(),
    search: vi.fn(),
    history: vi.fn().mockResolvedValue([]),
  },
}));

vi.mock('./i18n', () => ({ default: { changeLanguage: vi.fn() } }));

const engine: EngineInfo = { baseUrl: 'http://127.0.0.1:12345', token: 'test-token' };

function hit(headword: string): SearchHit {
  return { dictionaryId: 'd1', dictionaryName: 'Dict', headword, articleHtml: `<p>${headword}</p>`, score: 1 };
}

describe('store: lookup tabs, suggestions, and back/forward navigation', () => {
  afterEach(() => {
    vi.clearAllMocks();
    useAppStore.setState({ tabs: [], activeTabId: null, engineInfo: null });
  });

  it('openLookupTab falls back to fuzzy search for "did you mean" suggestions when there is no exact match', async () => {
    useAppStore.setState({ engineInfo: engine });
    vi.mocked(api.lookup).mockResolvedValue([]);
    vi.mocked(api.search).mockResolvedValue([hit('apple'), hit('apply')]);

    await useAppStore.getState().openLookupTab('appel');

    const tab = useAppStore.getState().tabs[0];
    expect(api.search).toHaveBeenCalledWith(engine, 'appel', 'fuzzy');
    expect(tab.entries[0].hits).toEqual([]);
    expect(tab.entries[0].suggestions).toEqual(['apple', 'apply']);
  });

  it('does not offer a suggestion identical to the searched term', async () => {
    useAppStore.setState({ engineInfo: engine });
    vi.mocked(api.lookup).mockResolvedValue([]);
    vi.mocked(api.search).mockResolvedValue([hit('Apple')]);

    await useAppStore.getState().openLookupTab('apple');

    expect(useAppStore.getState().tabs[0].entries[0].suggestions).toBeUndefined();
  });

  it('navigateInTab pushes a new entry and back/forward move between them', async () => {
    useAppStore.setState({ engineInfo: engine });
    vi.mocked(api.lookup).mockResolvedValueOnce([hit('apple')]);
    await useAppStore.getState().openLookupTab('apple');
    const tabId = useAppStore.getState().tabs[0].id;

    vi.mocked(api.lookup).mockResolvedValueOnce([hit('fruit')]);
    await useAppStore.getState().navigateInTab(tabId, 'fruit');

    let tab = useAppStore.getState().tabs[0];
    expect(tab.entries.map((e) => e.term)).toEqual(['apple', 'fruit']);
    expect(tab.activeIndex).toBe(1);

    useAppStore.getState().goBack(tabId);
    tab = useAppStore.getState().tabs[0];
    expect(tab.activeIndex).toBe(0);
    expect(tab.entries[tab.activeIndex].term).toBe('apple');

    useAppStore.getState().goForward(tabId);
    tab = useAppStore.getState().tabs[0];
    expect(tab.activeIndex).toBe(1);
    expect(tab.entries[tab.activeIndex].term).toBe('fruit');
  });

  it('navigating after going back discards the stale forward history', async () => {
    useAppStore.setState({ engineInfo: engine });
    vi.mocked(api.lookup).mockResolvedValueOnce([hit('apple')]);
    await useAppStore.getState().openLookupTab('apple');
    const tabId = useAppStore.getState().tabs[0].id;

    vi.mocked(api.lookup).mockResolvedValueOnce([hit('fruit')]);
    await useAppStore.getState().navigateInTab(tabId, 'fruit');
    useAppStore.getState().goBack(tabId);

    vi.mocked(api.lookup).mockResolvedValueOnce([hit('tree')]);
    await useAppStore.getState().navigateInTab(tabId, 'tree');

    const tab = useAppStore.getState().tabs[0];
    expect(tab.entries.map((e) => e.term)).toEqual(['apple', 'tree']);
    expect(tab.activeIndex).toBe(1);
  });
});
