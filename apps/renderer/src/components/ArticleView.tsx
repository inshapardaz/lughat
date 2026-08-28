import { useEffect, useMemo } from 'react';
import { ActionIcon, Group, Stack, Text, Title, Tooltip } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import type { SearchHit } from '../api';
import type { EngineInfo } from '../global';

interface ArticleViewProps {
  hit: SearchHit;
  engine: EngineInfo;
  /** Called when the reader clicks a cross-reference link inside the article. */
  onNavigate?: (term: string) => void;
}

const NAVIGATE_MESSAGE = 'lughat:navigate';

/**
 * Renders one article inside a sandboxed iframe. `allow-scripts` (but not
 * `allow-same-origin`) lets a small inline script intercept cross-reference link clicks and
 * postMessage the target term back to the parent — the iframe still can't read cookies,
 * localStorage, or the parent DOM, since its origin stays opaque without allow-same-origin.
 * `dir="auto"` on both the visible headword and the iframe body keeps bilingual content
 * bidi-isolated from the UI's own direction — spec §10/§15.
 */
export function ArticleView({ hit, engine, onNavigate }: ArticleViewProps) {
  const { t } = useTranslation();
  const srcDoc = useMemo(() => buildSrcDoc(hit, engine), [hit, engine]);
  const canSpeak = typeof window !== 'undefined' && 'speechSynthesis' in window;

  useEffect(() => {
    if (!onNavigate) {
      return;
    }

    const listener = (event: MessageEvent) => {
      if (event.data?.type === NAVIGATE_MESSAGE && typeof event.data.term === 'string') {
        onNavigate(event.data.term);
      }
    };

    window.addEventListener('message', listener);
    return () => window.removeEventListener('message', listener);
  }, [onNavigate]);

  const pronounce = () => {
    if (!canSpeak) {
      return;
    }
    // OS text-to-speech fallback (spec §6) — embedded dictionary audio, when an article's
    // own HTML includes an <audio> tag, plays natively via the iframe with no extra code.
    window.speechSynthesis.cancel();
    window.speechSynthesis.speak(new SpeechSynthesisUtterance(hit.headword));
  };

  return (
    <Stack gap={4}>
      <Group gap="xs">
        <Title order={4} dir="auto">
          {hit.headword}
        </Title>
        {canSpeak && (
          <Tooltip label={t('article.pronounce')}>
            <ActionIcon size="sm" variant="subtle" aria-label={t('article.pronounce')} onClick={pronounce}>
              🔊
            </ActionIcon>
          </Tooltip>
        )}
      </Group>
      <Text size="xs" c="dimmed">
        {hit.dictionaryName}
      </Text>
      <iframe
        title={hit.headword}
        srcDoc={srcDoc}
        sandbox="allow-scripts"
        style={{ width: '100%', minHeight: 140, border: 'none' }}
      />
    </Stack>
  );
}

function buildSrcDoc(hit: SearchHit, engine: EngineInfo): string {
  // Media referenced by relative path resolves against the dictionary's media endpoint —
  // see the "known gap" note on MediaEndpoints.cs: bearer-token auth doesn't reach plain
  // <img>/<audio> src requests, so embedded media loading is a documented follow-up, not
  // something this view can fix on its own.
  const mediaBase = `${engine.baseUrl}/api/media/${hit.dictionaryId}/`;

  return `<!doctype html>
<html>
<head>
<meta charset="utf-8" />
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src ${mediaBase} data:; media-src ${mediaBase}; style-src 'unsafe-inline'; script-src 'unsafe-inline';" />
<base href="${mediaBase}" />
<style>
  body { font-family: system-ui, sans-serif; font-size: 15px; line-height: 1.6; margin: 0; padding: 4px; unicode-bidi: isolate; }
</style>
</head>
<body dir="auto">${hit.articleHtml}
<script>
  document.addEventListener('click', function (event) {
    var link = event.target.closest('a');
    if (!link) return;
    var href = link.getAttribute('href') || '';
    if (/^(https?:|data:|mailto:)/i.test(href)) return; // real external links behave normally
    event.preventDefault();
    var term = (link.textContent || href.replace(/^[a-z][a-z0-9+.-]*:\\/\\//i, '')).trim();
    if (term) window.parent.postMessage({ type: '${NAVIGATE_MESSAGE}', term: term }, '*');
  });
</script>
</body>
</html>`;
}
