import { afterEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test/render';
import { PopupView, initialTermFromHash } from './PopupView';
import { useAppStore } from '../store';
import { api } from '../api';
import type { EngineInfo } from '../global';

vi.mock('../api', () => ({
  api: { lookup: vi.fn(), search: vi.fn() },
}));

const engine: EngineInfo = { baseUrl: 'http://127.0.0.1:12345', token: 'test-token' };

describe('initialTermFromHash', () => {
  it('reads the term query param out of the popup hash', () => {
    expect(initialTermFromHash('#/popup?term=apple')).toBe('apple');
  });

  it('URL-decodes the term', () => {
    expect(initialTermFromHash('#/popup?term=hello%20world')).toBe('hello world');
  });

  it('returns an empty string when there is no query', () => {
    expect(initialTermFromHash('#/popup')).toBe('');
  });
});

describe('PopupView', () => {
  afterEach(() => {
    vi.clearAllMocks();
    window.location.hash = '';
  });

  it('looks up the term from the hash on mount and renders the result', async () => {
    window.location.hash = '#/popup?term=apple';
    useAppStore.setState({ engineInfo: engine });
    vi.mocked(api.lookup).mockResolvedValue([
      { dictionaryId: 'd1', dictionaryName: 'Dict', headword: 'apple', articleHtml: 'A round fruit.', score: 1 },
    ]);
    vi.mocked(api.search).mockResolvedValue([]);

    renderWithProviders(<PopupView />);

    await waitFor(() => expect(api.lookup).toHaveBeenCalledWith(engine, 'apple'));
    expect(await screen.findByText('apple')).toBeInTheDocument();
  });
});
