import { useEffect, useState } from 'react';
import { Autocomplete, Loader, ScrollArea, Stack, Text } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { api, type SearchHit } from '../api';
import { useAppStore } from '../store';
import { ArticleView } from './ArticleView';

export function initialTermFromHash(hash: string): string {
  const query = hash.split('?')[1];
  if (!query) {
    return '';
  }
  return new URLSearchParams(query).get('term') ?? '';
}

/**
 * The frameless popup window's content (spec §6/§10) — a minimal search + article view, no
 * tabs/history/sidebar. Reached via a URL hash (#/popup?term=...) rather than being a
 * separate Vite app, since it needs the exact same engine connection and article rendering
 * App.tsx and ArticleView.tsx already have.
 */
export function PopupView() {
  const { t } = useTranslation();
  const engineInfo = useAppStore((s) => s.engineInfo);
  const [value, setValue] = useState(() => initialTermFromHash(window.location.hash));
  const [suggestions, setSuggestions] = useState<string[]>([]);
  const [hits, setHits] = useState<SearchHit[]>([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);

  async function lookup(term: string) {
    if (!engineInfo || term.trim().length === 0) {
      setHits([]);
      return;
    }
    setLoading(true);
    setSearched(true);
    try {
      setHits(await api.lookup(engineInfo, term));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const initial = initialTermFromHash(window.location.hash);
    if (initial && engineInfo) {
      void lookup(initial);
    }
    // Popup windows are single-use — created fresh per hotkey/clipboard trigger — so this
    // only needs to run once the engine connection is ready, not on every store update.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [engineInfo]);

  useEffect(() => {
    if (!engineInfo || value.trim().length === 0) {
      setSuggestions([]);
      return;
    }

    let cancelled = false;
    api
      .search(engineInfo, value, 'prefix')
      .then((results) => {
        if (!cancelled) {
          setSuggestions([...new Set(results.map((h) => h.headword))]);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setSuggestions([]);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [engineInfo, value]);

  return (
    <Stack gap="xs" p="xs" h="100vh">
      <Autocomplete
        value={value}
        onChange={setValue}
        data={suggestions}
        placeholder={t('search.placeholder')}
        onOptionSubmit={lookup}
        onKeyDown={(event) => {
          if (event.key === 'Enter') {
            void lookup(value);
          }
        }}
        dir="auto"
        autoFocus
      />
      {loading && <Loader size="xs" />}
      <ScrollArea style={{ flex: 1 }}>
        <Stack gap="md">
          {!loading && searched && hits.length === 0 && (
            <Text c="dimmed" size="sm">
              {t('search.noResults')}
            </Text>
          )}
          {engineInfo && hits.map((hit, index) => <ArticleView key={`${hit.dictionaryId}-${index}`} hit={hit} engine={engineInfo} />)}
        </Stack>
      </ScrollArea>
    </Stack>
  );
}
