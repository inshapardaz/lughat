import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test/render';
import { FavoritesView } from './FavoritesView';
import { useAppStore } from '../store';
import { api } from '../api';
import { buildAnkiCardsFromFavorites, downloadBlob } from '../anki';
import type { EngineInfo } from '../global';

vi.mock('../api', () => ({
  api: { exportAnki: vi.fn() },
}));

vi.mock('../anki', () => ({
  buildAnkiCardsFromFavorites: vi.fn(),
  downloadBlob: vi.fn(),
}));

const engine: EngineInfo = { baseUrl: 'http://127.0.0.1:12345', token: 'test-token' };

describe('FavoritesView export', () => {
  it('exports favorites to an Anki package and triggers a download', async () => {
    useAppStore.setState({
      engineInfo: engine,
      favorites: [{ id: 'f1', term: 'apple', dictionaryId: 'd1', tag: null, createdAt: '2026-01-01T00:00:00Z' }],
      loadFavorites: vi.fn(),
      removeFavorite: vi.fn(),
      openLookupTab: vi.fn(),
      setView: vi.fn(),
    });

    const cards = [{ front: 'apple', back: '<p>apple</p>' }];
    vi.mocked(buildAnkiCardsFromFavorites).mockResolvedValue(cards);
    const blob = new Blob(['fake apkg']);
    vi.mocked(api.exportAnki).mockResolvedValue(blob);

    const user = userEvent.setup();
    renderWithProviders(<FavoritesView />);

    await user.click(screen.getByRole('button', { name: 'Export to Anki' }));

    expect(buildAnkiCardsFromFavorites).toHaveBeenCalledWith(engine, useAppStore.getState().favorites);
    expect(api.exportAnki).toHaveBeenCalledWith(engine, 'Lughat Favorites', cards);
    expect(downloadBlob).toHaveBeenCalledWith(blob, 'lughat-favorites.apkg');
  });
});
