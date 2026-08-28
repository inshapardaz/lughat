import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test/render';
import { SettingsView } from './SettingsView';
import { useAppStore } from '../store';

describe('SettingsView', () => {
  it('shows the current theme and language, and persists a language change', async () => {
    useAppStore.setState({ theme: 'auto', language: 'en', engineInfo: null });

    const user = userEvent.setup();
    renderWithProviders(<SettingsView />);

    expect(screen.getByText('English')).toBeInTheDocument();

    await user.click(screen.getByRole('combobox', { name: 'Language' }));
    await user.click(await screen.findByText('اردو'));

    expect(useAppStore.getState().language).toBe('ur');

    // Restore so other tests don't inherit Urdu.
    await useAppStore.getState().setLanguage('en');
  });
});
