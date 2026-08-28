import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test/render';
import { ArticleView } from './ArticleView';
import type { SearchHit } from '../api';
import type { EngineInfo } from '../global';

const engine: EngineInfo = { baseUrl: 'http://127.0.0.1:12345', token: 'test-token' };

const hit: SearchHit = {
  dictionaryId: 'dict-1',
  dictionaryName: 'Spike Dict',
  headword: 'apple',
  articleHtml: '<b>apple</b> — a round fruit.',
  score: 1,
};

describe('ArticleView', () => {
  it('renders the headword and dictionary name', () => {
    renderWithProviders(<ArticleView hit={hit} engine={engine} />);

    expect(screen.getByText('apple')).toBeInTheDocument();
    expect(screen.getByText('Spike Dict')).toBeInTheDocument();
  });

  it('renders the article inside a fully sandboxed iframe', () => {
    renderWithProviders(<ArticleView hit={hit} engine={engine} />);

    const iframe = screen.getByTitle('apple') as HTMLIFrameElement;
    expect(iframe.tagName).toBe('IFRAME');
    expect(iframe.getAttribute('sandbox')).toBe('');
    expect(iframe.srcdoc).toContain('a round fruit');
    expect(iframe.srcdoc).toContain('dict-1');
  });
});
