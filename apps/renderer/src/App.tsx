import { useEffect } from 'react';
import { Alert, Center, Loader, Stack, Text, useDirection } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { AppShellLayout } from './components/AppShellLayout';
import { PopupView } from './components/PopupView';
import { directionFor } from './i18n';
import { useAppStore } from './store';
import { useEngineInfo } from './useEngineInfo';

export default function App() {
  const { t } = useTranslation();
  const { engineInfo, connected, error } = useEngineInfo();
  const setEngineInfo = useAppStore((s) => s.setEngineInfo);
  const setEngineConnected = useAppStore((s) => s.setEngineConnected);
  const loadPersistedSettings = useAppStore((s) => s.loadPersistedSettings);
  const connectEvents = useAppStore((s) => s.connectEvents);
  const language = useAppStore((s) => s.language);
  const { setDirection } = useDirection();

  // Mirrors the whole Mantine shell for RTL languages — this is the one place direction
  // actually gets applied; Mantine's own context is the source of truth, not the DOM
  // attribute directly (see the comment on store.ts's setLanguage).
  useEffect(() => {
    setDirection(directionFor(language));
    document.documentElement.lang = language;
  }, [language, setDirection]);

  useEffect(() => {
    if (engineInfo) {
      setEngineInfo(engineInfo);
    }
  }, [engineInfo, setEngineInfo]);

  useEffect(() => {
    setEngineConnected(connected);
  }, [connected, setEngineConnected]);

  useEffect(() => {
    if (engineInfo && connected) {
      void loadPersistedSettings();
      connectEvents();
    }
    // Only re-run when the engine (re)connects, not on every store re-render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [engineInfo, connected]);

  if (error) {
    return (
      <Center h="100vh">
        <Alert color="red" title={t('search.connectionFailed')}>
          {error}
        </Alert>
      </Center>
    );
  }

  if (!engineInfo) {
    return (
      <Center h="100vh">
        <Stack align="center" gap="xs">
          <Loader size="sm" />
          <Text c="dimmed" size="sm">
            {t('search.connecting')}
          </Text>
        </Stack>
      </Center>
    );
  }

  return window.location.hash.startsWith('#/popup') ? <PopupView /> : <AppShellLayout />;
}
