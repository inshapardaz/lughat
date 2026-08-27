import { useEffect } from 'react';
import { ActionIcon, Alert, Group, Stack, Text, UnstyledButton } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store';

export function FavoritesView() {
  const { t } = useTranslation();
  const favorites = useAppStore((s) => s.favorites);
  const loadFavorites = useAppStore((s) => s.loadFavorites);
  const removeFavorite = useAppStore((s) => s.removeFavorite);
  const openLookupTab = useAppStore((s) => s.openLookupTab);
  const setView = useAppStore((s) => s.setView);

  useEffect(() => {
    void loadFavorites();
  }, [loadFavorites]);

  if (favorites.length === 0) {
    return <Alert color="gray">{t('favorites.empty')}</Alert>;
  }

  return (
    <Stack gap="xs">
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
