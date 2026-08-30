import { create } from 'zustand';
import {
  api,
  type DictionaryRecord,
  type FavoriteEntry,
  type GroupRecord,
  type HistoryEntry,
  type SearchHit,
  type SearchMode,
} from './api';
import type { EngineInfo } from './global';
import i18n from './i18n';
import { notifyError } from './notifyError';

export interface TabEntry {
  term: string;
  hits: SearchHit[];
  /** "Did you mean" candidates — populated when a lookup finds no exact match. */
  suggestions?: string[];
}

export interface Tab {
  id: string;
  /** Visited terms in this tab, in order — the back/forward stack (spec §6's "Cross-linked
   *  article navigation with back/forward history"). The tab's own id stays stable across
   *  navigation; only which entry is active changes. */
  entries: TabEntry[];
  activeIndex: number;
}

export type View = 'search' | 'dictionaries' | 'history' | 'favorites' | 'settings';
export type ThemePreference = 'light' | 'dark' | 'auto';

interface AppState {
  engineInfo: EngineInfo | null;
  engineConnected: boolean;

  view: View;
  dictionaries: DictionaryRecord[];
  groups: GroupRecord[];
  tabs: Tab[];
  activeTabId: string | null;
  history: HistoryEntry[];
  favorites: FavoriteEntry[];
  theme: ThemePreference;
  language: string;

  setEngineInfo: (info: EngineInfo) => void;
  setEngineConnected: (connected: boolean) => void;
  setView: (view: View) => void;

  loadDictionaries: () => Promise<void>;
  importDictionaryPath: (path: string) => Promise<void>;
  removeDictionary: (id: string) => Promise<void>;
  setDictionaryEnabled: (id: string, enabled: boolean) => Promise<void>;
  reorderDictionary: (id: string, groupId: string | null, sortOrder: number) => Promise<void>;

  runSearch: (query: string, mode: SearchMode) => Promise<void>;
  openLookupTab: (term: string) => Promise<void>;
  navigateInTab: (tabId: string, term: string) => Promise<void>;
  goBack: (tabId: string) => void;
  goForward: (tabId: string) => void;
  closeTab: (id: string) => void;
  setActiveTab: (id: string) => void;

  loadHistory: () => Promise<void>;
  loadFavorites: () => Promise<void>;
  addFavorite: (term: string, dictionaryId: string) => Promise<void>;
  removeFavorite: (id: string) => Promise<void>;

  setTheme: (theme: ThemePreference) => Promise<void>;
  setLanguage: (language: string) => Promise<void>;
  loadPersistedSettings: () => Promise<void>;
  connectEvents: () => void;
}

async function lookupWithSuggestions(engine: EngineInfo, term: string): Promise<TabEntry> {
  const hits = await api.lookup(engine, term);
  if (hits.length > 0) {
    return { term, hits };
  }

  // No exact match — offer fuzzy candidates as "did you mean" suggestions, per #49.
  const fuzzyHits = await api.search(engine, term, 'fuzzy');
  const suggestions = [...new Set(fuzzyHits.map((h) => h.headword))].filter(
    (s) => s.toLowerCase() !== term.toLowerCase(),
  );
  return { term, hits: [], suggestions: suggestions.length > 0 ? suggestions : undefined };
}

export const useAppStore = create<AppState>((set, get) => ({
  engineInfo: null,
  engineConnected: false,
  view: 'search',
  dictionaries: [],
  groups: [],
  tabs: [],
  activeTabId: null,
  history: [],
  favorites: [],
  theme: 'auto',
  language: 'en',

  setEngineInfo: (info) => set({ engineInfo: info }),
  setEngineConnected: (connected) => set({ engineConnected: connected }),
  setView: (view) => set({ view }),

  loadDictionaries: async () => {
    const engine = get().engineInfo;
    if (!engine) return;
    const { dictionaries, groups } = await api.listDictionaries(engine);
    set({ dictionaries, groups });
  },

  importDictionaryPath: async (path) => {
    const engine = get().engineInfo;
    if (!engine) return;
    try {
      await api.importDictionary(engine, path);
      await get().loadDictionaries();
    } catch (error) {
      notifyError(error, 'dictionaries.import');
    }
  },

  removeDictionary: async (id) => {
    const engine = get().engineInfo;
    if (!engine) return;
    try {
      await api.removeDictionary(engine, id);
      await get().loadDictionaries();
    } catch (error) {
      notifyError(error, 'dictionaries.remove');
    }
  },

  setDictionaryEnabled: async (id, enabled) => {
    const engine = get().engineInfo;
    if (!engine) return;
    try {
      await api.setDictionaryEnabled(engine, id, enabled);
      await get().loadDictionaries();
    } catch (error) {
      notifyError(error, 'dictionaries.title');
    }
  },

  reorderDictionary: async (id, groupId, sortOrder) => {
    const engine = get().engineInfo;
    if (!engine) return;
    try {
      await api.reorderDictionary(engine, id, groupId, sortOrder);
      await get().loadDictionaries();
    } catch (error) {
      notifyError(error, 'dictionaries.title');
    }
  },

  runSearch: async (query, mode) => {
    const engine = get().engineInfo;
    if (!engine || query.trim().length === 0) return;
    try {
      const hits = await api.search(engine, query, mode);
      const id = `search:${query}`;
      set((state) => ({
        tabs: upsertTab(state.tabs, { id, entries: [{ term: query, hits }], activeIndex: 0 }),
        activeTabId: id,
      }));
    } catch (error) {
      notifyError(error, 'nav.search');
    }
  },

  openLookupTab: async (term) => {
    const engine = get().engineInfo;
    if (!engine || term.trim().length === 0) return;
    try {
      const entry = await lookupWithSuggestions(engine, term);
      const id = `lookup:${term}`;
      set((state) => ({
        tabs: upsertTab(state.tabs, { id, entries: [entry], activeIndex: 0 }),
        activeTabId: id,
      }));
      await get().loadHistory();
    } catch (error) {
      notifyError(error, 'nav.search');
    }
  },

  navigateInTab: async (tabId, term) => {
    const engine = get().engineInfo;
    if (!engine || term.trim().length === 0) return;
    try {
      const entry = await lookupWithSuggestions(engine, term);

      set((state) => ({
        tabs: state.tabs.map((tab) => {
          if (tab.id !== tabId) return tab;
          // Standard back/forward semantics: navigating from a point you'd gone back to
          // discards whatever forward history existed past it.
          const entries = [...tab.entries.slice(0, tab.activeIndex + 1), entry];
          return { ...tab, entries, activeIndex: entries.length - 1 };
        }),
      }));
      await get().loadHistory();
    } catch (error) {
      notifyError(error, 'nav.search');
    }
  },

  goBack: (tabId) => {
    set((state) => ({
      tabs: state.tabs.map((tab) =>
        tab.id === tabId && tab.activeIndex > 0 ? { ...tab, activeIndex: tab.activeIndex - 1 } : tab,
      ),
    }));
  },

  goForward: (tabId) => {
    set((state) => ({
      tabs: state.tabs.map((tab) =>
        tab.id === tabId && tab.activeIndex < tab.entries.length - 1
          ? { ...tab, activeIndex: tab.activeIndex + 1 }
          : tab,
      ),
    }));
  },

  closeTab: (id) => {
    set((state) => {
      const tabs = state.tabs.filter((t) => t.id !== id);
      const activeTabId = state.activeTabId === id ? (tabs.at(-1)?.id ?? null) : state.activeTabId;
      return { tabs, activeTabId };
    });
  },

  setActiveTab: (id) => set({ activeTabId: id }),

  loadHistory: async () => {
    const engine = get().engineInfo;
    if (!engine) return;
    set({ history: await api.history(engine) });
  },

  loadFavorites: async () => {
    const engine = get().engineInfo;
    if (!engine) return;
    set({ favorites: await api.favorites(engine) });
  },

  addFavorite: async (term, dictionaryId) => {
    const engine = get().engineInfo;
    if (!engine) return;
    try {
      await api.addFavorite(engine, term, dictionaryId);
      await get().loadFavorites();
    } catch (error) {
      notifyError(error, 'favorites.add');
    }
  },

  removeFavorite: async (id) => {
    const engine = get().engineInfo;
    if (!engine) return;
    try {
      await api.removeFavorite(engine, id);
      await get().loadFavorites();
    } catch (error) {
      notifyError(error, 'favorites.remove');
    }
  },

  setTheme: async (theme) => {
    set({ theme });
    const engine = get().engineInfo;
    if (engine) {
      try {
        await api.setSetting(engine, 'appearance.theme', theme);
      } catch (error) {
        notifyError(error, 'settings.title');
      }
    }
  },

  // Direction is applied by a Mantine useDirection() consumer in the component tree (see
  // AppRoot), not here — Mantine's RTL mirroring only takes effect through its own context,
  // so writing document.documentElement.dir directly here would desync from what Mantine
  // components think their direction is.
  setLanguage: async (language) => {
    set({ language });
    await i18n.changeLanguage(language);
    const engine = get().engineInfo;
    if (engine) {
      try {
        await api.setSetting(engine, 'ui.language', language);
      } catch (error) {
        notifyError(error, 'settings.title');
      }
    }
  },

  loadPersistedSettings: async () => {
    const engine = get().engineInfo;
    if (!engine) return;

    const [theme, language] = await Promise.all([
      api.getSetting(engine, 'appearance.theme'),
      api.getSetting(engine, 'ui.language'),
    ]);

    if (typeof theme === 'string') {
      set({ theme: theme as ThemePreference });
    }

    if (typeof language === 'string') {
      set({ language });
      await i18n.changeLanguage(language);
    }
  },

  connectEvents: () => {
    const engine = get().engineInfo;
    if (!engine) return;

    // Best-effort live refresh on indexing progress/completion — a dropped socket just
    // means the dictionary manager falls back to whatever it last loaded, so no reconnect
    // logic is needed here the way it is for the engine process itself.
    const socket = new WebSocket(`${engine.baseUrl.replace(/^http/, 'ws')}/ws`);
    socket.addEventListener('message', () => {
      void get().loadDictionaries();
    });
  },
}));

function upsertTab(tabs: Tab[], tab: Tab): Tab[] {
  const existingIndex = tabs.findIndex((t) => t.id === tab.id);
  return existingIndex >= 0 ? tabs.map((t, i) => (i === existingIndex ? tab : t)) : [...tabs, tab];
}
