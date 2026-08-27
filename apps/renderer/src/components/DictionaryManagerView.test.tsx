import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test/render';
import { DictionaryManagerView } from './DictionaryManagerView';
import { useAppStore } from '../store';

describe('DictionaryManagerView', () => {
  it('shows an empty state when no dictionaries are imported', () => {
    useAppStore.setState({
      dictionaries: [],
      loadDictionaries: vi.fn(async () => {}),
    });

    renderWithProviders(<DictionaryManagerView />);

    expect(screen.getByText(/no dictionaries yet/i)).toBeInTheDocument();
  });

  it('lists imported dictionaries with their ready state', () => {
    useAppStore.setState({
      dictionaries: [
        {
          id: 'd1',
          name: 'Spike Dict',
          format: 'stardict',
          filePath: 'x.ifo',
          contentHash: 'abc',
          enabled: true,
          groupId: null,
          sortOrder: 0,
          indexedAt: '2026-01-01T00:00:00Z',
        },
      ],
      loadDictionaries: vi.fn(async () => {}),
    });

    renderWithProviders(<DictionaryManagerView />);

    expect(screen.getByText('Spike Dict')).toBeInTheDocument();
    expect(screen.getByText(/ready/i)).toBeInTheDocument();
  });
});
