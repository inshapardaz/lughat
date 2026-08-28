import { api, type FavoriteEntry } from './api';
import type { EngineInfo } from './global';

export interface AnkiCardDraft {
  front: string;
  back: string;
}

/**
 * Resolves each favorite's article via the engine (Playwright/the file-picker button can't
 * carry cached article HTML across a browser-native download, so this re-fetches) and picks
 * the hit from the favorite's own dictionary specifically, falling back to whichever hit
 * comes back first if that dictionary was since removed.
 */
export async function buildAnkiCardsFromFavorites(
  engine: EngineInfo,
  favorites: FavoriteEntry[],
): Promise<AnkiCardDraft[]> {
  const cards: AnkiCardDraft[] = [];

  for (const favorite of favorites) {
    const hits = await api.lookup(engine, favorite.term);
    const hit = hits.find((h) => h.dictionaryId === favorite.dictionaryId) ?? hits[0];
    if (hit) {
      cards.push({ front: favorite.term, back: hit.articleHtml });
    }
  }

  return cards;
}

/** Triggers a browser-native "Save File" for a Blob — no Electron IPC needed for this. */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}
