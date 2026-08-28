import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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

  it('renders the article inside a sandboxed iframe without allow-same-origin', () => {
    renderWithProviders(<ArticleView hit={hit} engine={engine} />);

    const iframe = screen.getByTitle('apple') as HTMLIFrameElement;
    expect(iframe.tagName).toBe('IFRAME');
    // allow-scripts (for cross-reference link interception) but never allow-same-origin —
    // that combination is what keeps the iframe's origin opaque, so a malicious article
    // still can't read cookies/localStorage or reach the parent DOM.
    expect(iframe.getAttribute('sandbox')).toBe('allow-scripts');
    expect(iframe.getAttribute('sandbox')).not.toContain('allow-same-origin');
    expect(iframe.srcdoc).toContain('a round fruit');
    expect(iframe.srcdoc).toContain('dict-1');
  });

  it('calls onNavigate when the iframe posts a navigate message', () => {
    const onNavigate = vi.fn();
    renderWithProviders(<ArticleView hit={hit} engine={engine} onNavigate={onNavigate} />);

    fireEvent(window, new MessageEvent('message', { data: { type: 'lughat:navigate', term: 'fruit' } }));

    expect(onNavigate).toHaveBeenCalledWith('fruit');
  });

  it('ignores unrelated postMessage events', () => {
    const onNavigate = vi.fn();
    renderWithProviders(<ArticleView hit={hit} engine={engine} onNavigate={onNavigate} />);

    fireEvent(window, new MessageEvent('message', { data: { type: 'something-else' } }));

    expect(onNavigate).not.toHaveBeenCalled();
  });

  describe('pronunciation', () => {
    const speak = vi.fn();
    const cancel = vi.fn();

    beforeEach(() => {
      Object.defineProperty(window, 'speechSynthesis', {
        configurable: true,
        value: { speak, cancel },
      });
      // jsdom doesn't implement the Web Speech API at all.
      // @ts-expect-error -- minimal test stub, not the real DOM type
      window.SpeechSynthesisUtterance = class {
        text: string;
        constructor(text: string) {
          this.text = text;
        }
      };
    });

    afterEach(() => {
      speak.mockClear();
      cancel.mockClear();
      delete (window as { speechSynthesis?: unknown }).speechSynthesis;
      delete (window as { SpeechSynthesisUtterance?: unknown }).SpeechSynthesisUtterance;
    });

    it('speaks the headword via OS text-to-speech when the pronounce button is clicked', async () => {
      const user = userEvent.setup();
      renderWithProviders(<ArticleView hit={hit} engine={engine} />);

      await user.click(screen.getByRole('button', { name: 'Pronounce' }));

      expect(cancel).toHaveBeenCalled();
      expect(speak).toHaveBeenCalledTimes(1);
      const utterance = speak.mock.calls[0][0] as SpeechSynthesisUtterance;
      expect(utterance.text).toBe('apple');
    });
  });
});
