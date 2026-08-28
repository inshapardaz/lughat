import { useEffect, useState } from 'react';
import { Autocomplete } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { api } from '../api';
import { useAppStore } from '../store';

export function SearchBar() {
  const { t } = useTranslation();
  const engineInfo = useAppStore((s) => s.engineInfo);
  const openLookupTab = useAppStore((s) => s.openLookupTab);
  const setView = useAppStore((s) => s.setView);
  const [value, setValue] = useState('');
  const [suggestions, setSuggestions] = useState<string[]>([]);

  useEffect(() => {
    if (!engineInfo || value.trim().length === 0) {
      setSuggestions([]);
      return;
    }

    let cancelled = false;
    api
      .search(engineInfo, value, 'prefix')
      .then((hits) => {
        if (!cancelled) {
          setSuggestions([...new Set(hits.map((h) => h.headword))]);
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

  function submit(term: string) {
    const trimmed = term.trim();
    if (trimmed.length === 0) {
      return;
    }
    setValue(trimmed);
    setView('search');
    void openLookupTab(trimmed);
  }

  return (
    <Autocomplete
      value={value}
      onChange={setValue}
      data={suggestions}
      placeholder={t('search.placeholder')}
      onOptionSubmit={submit}
      onKeyDown={(event) => {
        if (event.key === 'Enter') {
          submit(value);
        }
      }}
      w={360}
      dir="auto"
    />
  );
}
