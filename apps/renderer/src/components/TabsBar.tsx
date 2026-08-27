import { ActionIcon, Group, ScrollArea, Stack, Tabs, Text } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store';
import { ArticleView } from './ArticleView';

export function TabsBar() {
  const { t } = useTranslation();
  const tabs = useAppStore((s) => s.tabs);
  const activeTabId = useAppStore((s) => s.activeTabId);
  const setActiveTab = useAppStore((s) => s.setActiveTab);
  const closeTab = useAppStore((s) => s.closeTab);
  const engineInfo = useAppStore((s) => s.engineInfo);

  if (tabs.length === 0 || !engineInfo) {
    return (
      <Text c="dimmed" size="sm">
        {t('tabs.newSearch')}
      </Text>
    );
  }

  return (
    <Tabs value={activeTabId} onChange={(value) => value && setActiveTab(value)}>
      <Tabs.List>
        {tabs.map((tab) => (
          <Tabs.Tab key={tab.id} value={tab.id}>
            <Group gap={6} wrap="nowrap">
              <Text dir="auto">{tab.term}</Text>
              <ActionIcon
                size="xs"
                variant="subtle"
                component="span"
                onClick={(event) => {
                  event.stopPropagation();
                  closeTab(tab.id);
                }}
              >
                ×
              </ActionIcon>
            </Group>
          </Tabs.Tab>
        ))}
      </Tabs.List>

      {tabs.map((tab) => (
        <Tabs.Panel key={tab.id} value={tab.id} pt="md">
          <ScrollArea.Autosize mah={480}>
            <Stack gap="lg">
              {tab.hits.length === 0 && (
                <Text c="dimmed" size="sm">
                  {t('search.noResults')}
                </Text>
              )}
              {tab.hits.map((hit, index) => (
                <ArticleView key={`${hit.dictionaryId}-${index}`} hit={hit} engine={engineInfo} />
              ))}
            </Stack>
          </ScrollArea.Autosize>
        </Tabs.Panel>
      ))}
    </Tabs>
  );
}
