import { AppShell, Group, Loader, NavLink, Stack, Title } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store';
import { DictionaryManagerView } from './DictionaryManagerView';
import { FavoritesView } from './FavoritesView';
import { HistoryView } from './HistoryView';
import { SearchBar } from './SearchBar';
import { SettingsView } from './SettingsView';
import { TabsBar } from './TabsBar';

export function AppShellLayout() {
  const { t } = useTranslation();
  const view = useAppStore((s) => s.view);
  const setView = useAppStore((s) => s.setView);
  const engineConnected = useAppStore((s) => s.engineConnected);

  return (
    <AppShell header={{ height: 64 }} navbar={{ width: 200, breakpoint: 'sm' }} padding="md">
      <AppShell.Header p="md">
        <Group justify="space-between" wrap="nowrap">
          <Title order={3}>{t('app.title')}</Title>
          <SearchBar />
          {!engineConnected && <Loader size="xs" />}
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        <Stack gap={4}>
          <NavLink label={t('nav.search')} active={view === 'search'} onClick={() => setView('search')} />
          <NavLink
            label={t('nav.dictionaries')}
            active={view === 'dictionaries'}
            onClick={() => setView('dictionaries')}
          />
          <NavLink label={t('nav.history')} active={view === 'history'} onClick={() => setView('history')} />
          <NavLink label={t('nav.favorites')} active={view === 'favorites'} onClick={() => setView('favorites')} />
          <NavLink label={t('nav.settings')} active={view === 'settings'} onClick={() => setView('settings')} />
        </Stack>
      </AppShell.Navbar>

      <AppShell.Main>
        {view === 'search' && <TabsBar />}
        {view === 'dictionaries' && <DictionaryManagerView />}
        {view === 'history' && <HistoryView />}
        {view === 'favorites' && <FavoritesView />}
        {view === 'settings' && <SettingsView />}
      </AppShell.Main>
    </AppShell>
  );
}
