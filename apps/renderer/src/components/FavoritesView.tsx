import { useEffect, useState } from 'react';
import { ActionIcon, Alert, Button, Group, Stack, Text, UnstyledButton } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store';
import { api } from '../api';
import { buildAnkiCardsFromFavorites, downloadBlob } from '../anki';

export function FavoritesView() {
  const { t } = useTranslation();
  const favorites = useAppStore((s) => s.favorites);
  const loadFavorites = useAppStore((s) => s.loadFavorites);
  const removeFavorite = useAppStore((s) => s.removeFavorite);
  const openLookupTab = useAppStore((s) => s.openLookupTab);
  const setView = useAppStore((s) => s.setView);
  const engineInfo = useAppStore((s) => s.engineInfo);
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    void loadFavorites();
  }, [loadFavorites]);

  async function exportToAnki() {
    if (!engineInfo || favorites.length === 0) {
      return;
    }
    setExporting(true);
    try {
      const cards = await buildAnkiCardsFromFavorites(engineInfo, favorites);
      const blob = await api.exportAnki(engineInfo, 'Lughat Favorites', cards);
      downloadBlob(blob, 'lughat-favorites.apkg');
    } finally {
      setExporting(false);
    }
  }

  if (favorites.length === 0) {
    return <Alert color="gray">{t('favorites.empty')}</Alert>;
  }

  return (
    <Stack gap="xs">
      <Group justify="flex-end">
        <Button size="xs" variant="light" loading={exporting} onClick={() => void exportToAnki()}>
          {t('anki.export')}
        </Button>
      </Group>
      {favorites.map((fav) => (
        <Group key={fav.id} justify="space-between">
          <UnstyledButton
            onClick={() => {
              setView('search');
              void openLookupTab(fav.term);
            }}
          >
            <Text dir="auto">{fav.term}</Text>
          </UnstyledButton>
          <ActionIcon color="red" variant="subtle" onClick={() => void removeFavorite(fav.id)}>
            ✕
          </ActionIcon>
        </Group>
      ))}
    </Stack>
  );
}
