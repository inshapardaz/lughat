import { ActionIcon, Badge, Group, ScrollArea, Stack, Tabs, Text } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { useAppStore } from '../store';
import { ArticleView } from './ArticleView';

export function TabsBar() {
  const { t } = useTranslation();
  const tabs = useAppStore((s) => s.tabs);
  const activeTabId = useAppStore((s) => s.activeTabId);
  const setActiveTab = useAppStore((s) => s.setActiveTab);
  const closeTab = useAppStore((s) => s.closeTab);
  const navigateInTab = useAppStore((s) => s.navigateInTab);
  const goBack = useAppStore((s) => s.goBack);
  const goForward = useAppStore((s) => s.goForward);
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
        {tabs.map((tab) => {
          const current = tab.entries[tab.activeIndex];
          return (
            <Tabs.Tab key={tab.id} value={tab.id}>
              <Group gap={6} wrap="nowrap">
                <Text dir="auto">{current.term}</Text>
                <ActionIcon
                  size="xs"
                  variant="subtle"
                  component="span"
                  aria-label={t('tabs.close')}
                  onClick={(event) => {
                    event.stopPropagation();
                    closeTab(tab.id);
                  }}
                >
                  ×
                </ActionIcon>
              </Group>
            </Tabs.Tab>
          );
        })}
      </Tabs.List>

      {tabs.map((tab) => {
        const current = tab.entries[tab.activeIndex];
        const canGoBack = tab.activeIndex > 0;
        const canGoForward = tab.activeIndex < tab.entries.length - 1;

        return (
          <Tabs.Panel key={tab.id} value={tab.id} pt="md">
            <Stack gap="md">
              {(canGoBack || canGoForward) && (
                <Group gap={4}>
                  <ActionIcon variant="subtle" disabled={!canGoBack} aria-label={t('tabs.back')} onClick={() => goBack(tab.id)}>
                    ←
                  </ActionIcon>
                  <ActionIcon
                    variant="subtle"
                    disabled={!canGoForward}
                    aria-label={t('tabs.forward')}
                    onClick={() => goForward(tab.id)}
                  >
                    →
                  </ActionIcon>
                </Group>
              )}

              <ScrollArea.Autosize mah={480}>
                <Stack gap="lg">
                  {current.hits.length === 0 && (
                    <Stack gap="xs">
                      <Text c="dimmed" size="sm">
                        {t('search.noResults')}
                      </Text>
                      {current.suggestions && current.suggestions.length > 0 && (
                        <Group gap={6}>
                          <Text size="sm">{t('search.didYouMean')}</Text>
                          {current.suggestions.map((suggestion) => (
                            <Badge
                              key={suggestion}
                              component="button"
                              variant="light"
                              style={{ cursor: 'pointer' }}
                              onClick={() => navigateInTab(tab.id, suggestion)}
                            >
                              {suggestion}
                            </Badge>
                          ))}
                        </Group>
                      )}
                    </Stack>
                  )}
                  {current.hits.map((hit, index) => (
                    <ArticleView
                      key={`${hit.dictionaryId}-${index}`}
                      hit={hit}
                      engine={engineInfo}
                      onNavigate={(term) => navigateInTab(tab.id, term)}
                    />
                  ))}
                </Stack>
              </ScrollArea.Autosize>
            </Stack>
          </Tabs.Panel>
        );
      })}
    </Tabs>
  );
}
