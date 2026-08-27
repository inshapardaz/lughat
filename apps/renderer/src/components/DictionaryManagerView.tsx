import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { ActionIcon, Alert, Button, Group, Paper, Stack, Switch, Text } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { closestCenter, DndContext, type DragEndEvent } from '@dnd-kit/core';
import { arrayMove, SortableContext, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { useAppStore } from '../store';

function SortableRow({ id, children }: { id: string; children: ReactNode }) {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id });
  const style = { transform: CSS.Transform.toString(transform), transition };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      {children}
    </div>
  );
}

export function DictionaryManagerView() {
  const { t } = useTranslation();
  const dictionaries = useAppStore((s) => s.dictionaries);
  const loadDictionaries = useAppStore((s) => s.loadDictionaries);
  const importDictionaryPath = useAppStore((s) => s.importDictionaryPath);
  const removeDictionary = useAppStore((s) => s.removeDictionary);
  const setDictionaryEnabled = useAppStore((s) => s.setDictionaryEnabled);
  const reorderDictionary = useAppStore((s) => s.reorderDictionary);

  useEffect(() => {
    void loadDictionaries();
  }, [loadDictionaries]);

  async function handleImport() {
    const path = await window.lughat.pickDictionaryFile();
    if (path) {
      await importDictionaryPath(path);
    }
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) {
      return;
    }

    const oldIndex = dictionaries.findIndex((d) => d.id === active.id);
    const newIndex = dictionaries.findIndex((d) => d.id === over.id);
    const reordered = arrayMove(dictionaries, oldIndex, newIndex);
    reordered.forEach((dict, index) => void reorderDictionary(dict.id, dict.groupId, index));
  }

  return (
    <Stack>
      <Group justify="space-between">
        <Text fw={600}>{t('dictionaries.title')}</Text>
        <Button size="xs" onClick={() => void handleImport()}>
          {t('dictionaries.import')}
        </Button>
      </Group>

      {dictionaries.length === 0 && <Alert color="gray">{t('dictionaries.empty')}</Alert>}

      <DndContext collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <SortableContext items={dictionaries.map((d) => d.id)} strategy={verticalListSortingStrategy}>
          <Stack gap="xs">
            {dictionaries.map((dict) => (
              <SortableRow key={dict.id} id={dict.id}>
                <Paper withBorder p="sm">
                  <Group justify="space-between" wrap="nowrap">
                    <Stack gap={0}>
                      <Text fw={500}>{dict.name}</Text>
                      <Text size="xs" c="dimmed">
                        {dict.format} · {dict.indexedAt ? t('dictionaries.ready') : t('dictionaries.indexing')}
                      </Text>
                    </Stack>
                    <Group gap="xs" wrap="nowrap">
                      <Switch
                        checked={dict.enabled}
                        label={dict.enabled ? t('dictionaries.enabled') : t('dictionaries.disabled')}
                        onChange={(event) => void setDictionaryEnabled(dict.id, event.currentTarget.checked)}
                      />
                      <ActionIcon color="red" variant="subtle" onClick={() => void removeDictionary(dict.id)}>
                        ✕
                      </ActionIcon>
                    </Group>
                  </Group>
                </Paper>
              </SortableRow>
            ))}
          </Stack>
        </SortableContext>
      </DndContext>
    </Stack>
  );
}
