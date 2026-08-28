import { ActionIcon, Popover, SimpleGrid, Tabs, Tooltip } from '@mantine/core';
import { useTranslation } from 'react-i18next';

// A deliberately small starter set per script — enough to type common dictionary headwords
// and IPA transcriptions without a full keyboard layout. Easy to extend per script later.
const CHARACTER_SETS: Record<string, string[]> = {
  ipa: ['ə', 'ʃ', 'ʒ', 'θ', 'ð', 'ŋ', 'ɪ', 'ʊ', 'æ', 'ɑ', 'ɔ', 'ʌ', 'ɛ', 'ˈ', 'ˌ', 'ː'],
  cyrillic: [
    'а', 'б', 'в', 'г', 'д', 'е', 'ж', 'з', 'и', 'й', 'к', 'л', 'м', 'н', 'о', 'п',
    'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ъ', 'ы', 'ь', 'э', 'ю', 'я',
  ],
  urdu: [
    'ا', 'ب', 'پ', 'ت', 'ٹ', 'ث', 'ج', 'چ', 'ح', 'خ', 'د', 'ڈ', 'ذ', 'ر', 'ڑ', 'ز',
    'ژ', 'س', 'ش', 'ص', 'ض', 'ط', 'ظ', 'ع', 'غ', 'ف', 'ق', 'ک', 'گ', 'ل', 'م', 'ن',
    'و', 'ہ', 'ھ', 'ء', 'ی', 'ے',
  ],
};

interface VirtualKeyboardProps {
  onInsert: (character: string) => void;
}

export function VirtualKeyboard({ onInsert }: VirtualKeyboardProps) {
  const { t } = useTranslation();
  const label = t('search.virtualKeyboard');

  return (
    <Popover position="bottom-start" withArrow shadow="md" transitionProps={{ duration: 0 }}>
      <Popover.Target>
        <Tooltip label={label}>
          <ActionIcon variant="subtle" aria-label={label}>
            ⌨
          </ActionIcon>
        </Tooltip>
      </Popover.Target>
      <Popover.Dropdown miw={260}>
        <Tabs defaultValue="ipa">
          <Tabs.List>
            <Tabs.Tab value="ipa">IPA</Tabs.Tab>
            <Tabs.Tab value="cyrillic">Кириллица</Tabs.Tab>
            <Tabs.Tab value="urdu">اردو</Tabs.Tab>
          </Tabs.List>
          {Object.entries(CHARACTER_SETS).map(([script, characters]) => (
            <Tabs.Panel key={script} value={script} pt="xs">
              <SimpleGrid cols={8} spacing={4}>
                {characters.map((character) => (
                  <ActionIcon key={character} variant="default" onClick={() => onInsert(character)}>
                    {character}
                  </ActionIcon>
                ))}
              </SimpleGrid>
            </Tabs.Panel>
          ))}
        </Tabs>
      </Popover.Dropdown>
    </Popover>
  );
}
