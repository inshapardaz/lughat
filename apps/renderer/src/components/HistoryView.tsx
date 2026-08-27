import { useEffect } from 'react';
import { Alert, Stack, Text, UnstyledButton } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store';

export function HistoryView() {
  const { t } = useTranslation();
  const history = useAppStore((s) => s.history);
  const loadHistory = useAppStore((s) => s.loadHistory);
  const openLookupTab = useAppStore((s) => s.openLookupTab);
  const setView = useAppStore((s) => s.setView);

  useEffect(() => {
    void loadHistory();
  }, [loadHistory]);

  if (history.length === 0) {
    return <Alert color="gray">{t('history.empty')}</Alert>;
  }

  return (
    <Stack gap="xs">
      {history.map((entry) => (
        <UnstyledButton
          key={entry.id}
          onClick={() => {
            setView('search');
            void openLookupTab(entry.term);
          }}
        >
          <Text dir="auto">{entry.term}</Text>
        </UnstyledButton>
      ))}
    </Stack>
  );
}
