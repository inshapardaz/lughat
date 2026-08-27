import type { ReactElement } from 'react';
import { render } from '@testing-library/react';
import { DirectionProvider, MantineProvider } from '@mantine/core';
import { I18nextProvider } from 'react-i18next';
import i18n from '../i18n';

export function renderWithProviders(ui: ReactElement) {
  return render(
    <DirectionProvider>
      <MantineProvider>
        <I18nextProvider i18n={i18n}>{ui}</I18nextProvider>
      </MantineProvider>
    </DirectionProvider>,
  );
}
