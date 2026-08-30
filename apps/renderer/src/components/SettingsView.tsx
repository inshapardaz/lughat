import { SegmentedControl, Select, Stack, Text, useMantineColorScheme } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { labelFor, SUPPORTED_LANGUAGES } from '../i18n';
import { useAppStore } from '../store';

export function SettingsView() {
  const { t } = useTranslation();
  const theme = useAppStore((s) => s.theme);
  const setTheme = useAppStore((s) => s.setTheme);
  const language = useAppStore((s) => s.language);
  const setLanguage = useAppStore((s) => s.setLanguage);
  const { setColorScheme } = useMantineColorScheme();

  return (
    <Stack gap="lg" maw={420}>
      <Text fw={600}>{t('settings.title')}</Text>

      <Stack gap="xs">
        <Text size="sm" fw={500}>
          {t('settings.appearance')}
        </Text>
        <SegmentedControl
          aria-label={t('settings.theme')}
          value={theme}
          onChange={(value) => {
            const next = value as 'light' | 'dark' | 'auto';
            void setTheme(next);
            setColorScheme(next);
          }}
          data={[
            { label: t('settings.themeLight'), value: 'light' },
            { label: t('settings.themeDark'), value: 'dark' },
            { label: t('settings.themeAuto'), value: 'auto' },
          ]}
        />
      </Stack>

      <Select
        label={t('settings.language')}
        value={language}
        onChange={(value) => value && void setLanguage(value)}
        data={SUPPORTED_LANGUAGES.map((lang) => ({ value: lang, label: labelFor(lang) }))}
        allowDeselect={false}
      />
    </Stack>
  );
}
