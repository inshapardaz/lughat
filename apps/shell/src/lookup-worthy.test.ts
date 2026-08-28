import { describe, expect, it } from 'vitest';
import { isLookupWorthy } from './lookup-worthy';

describe('isLookupWorthy', () => {
  it('accepts a short word or phrase', () => {
    expect(isLookupWorthy('apple')).toBe(true);
    expect(isLookupWorthy('a round fruit')).toBe(true);
  });

  it('rejects an empty string', () => {
    expect(isLookupWorthy('')).toBe(false);
  });

  it('rejects text longer than the lookup-worthy limit', () => {
    expect(isLookupWorthy('a'.repeat(80))).toBe(true);
    expect(isLookupWorthy('a'.repeat(81))).toBe(false);
  });

  it('rejects multi-line text — someone copied a paragraph, not a word', () => {
    expect(isLookupWorthy('line one\nline two')).toBe(false);
  });
});
