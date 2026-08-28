import { afterEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test/render';
import { SearchBar } from './SearchBar';
import { useAppStore } from '../store';
import { api } from '../api';
import type { EngineInfo } from '../global';

vi.mock('../api', () => ({
  api: {
    search: vi.fn(),
  },
}));

const engine: EngineInfo = { baseUrl: 'http://127.0.0.1:12345', token: 'test-token' };

describe('SearchBar', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('fetches prefix suggestions as the user types and opens a lookup tab on submit', async () => {
    const openLookupTab = vi.fn();
    useAppStore.setState({ engineInfo: engine, openLookupTab, setView: vi.fn() });
    vi.mocked(api.search).mockResolvedValue([
      { dictionaryId: 'd1', dictionaryName: 'Dict', headword: 'apple', articleHtml: 'x', score: 1 },
    ]);

    const user = userEvent.setup();
    renderWithProviders(<SearchBar />);

    await user.type(screen.getByRole('combobox'), 'app');

    await waitFor(() => expect(api.search).toHaveBeenCalledWith(engine, 'app', 'prefix'));

    await user.keyboard('{Enter}');
    expect(openLookupTab).toHaveBeenCalledWith('app');
  });

  it('does not query the engine for an empty search box', () => {
    useAppStore.setState({ engineInfo: engine, openLookupTab: vi.fn(), setView: vi.fn() });

    renderWithProviders(<SearchBar />);

    expect(api.search).not.toHaveBeenCalled();
  });
});
