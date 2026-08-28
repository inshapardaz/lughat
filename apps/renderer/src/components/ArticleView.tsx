import { useMemo } from 'react';
import { Stack, Text, Title } from '@mantine/core';
import type { SearchHit } from '../api';
import type { EngineInfo } from '../global';

interface ArticleViewProps {
  hit: SearchHit;
  engine: EngineInfo;
}

/**
 * Renders one article inside a fully sandboxed iframe (sandbox="" — no scripts, opaque
 * origin) so dictionary-authored HTML/CSS can never execute script or reach the app.
 * `dir="auto"` on both the visible headword and the iframe body keeps bilingual content
 * bidi-isolated from the UI's own direction — spec §10/§15.
 */
export function ArticleView({ hit, engine }: ArticleViewProps) {
  const srcDoc = useMemo(() => buildSrcDoc(hit, engine), [hit, engine]);

  return (
    <Stack gap={4}>
      <Title order={4} dir="auto">
        {hit.headword}
      </Title>
      <Text size="xs" c="dimmed">
        {hit.dictionaryName}
      </Text>
      <iframe
        title={hit.headword}
        srcDoc={srcDoc}
        sandbox=""
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
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src ${mediaBase} data:; media-src ${mediaBase}; style-src 'unsafe-inline';" />
<base href="${mediaBase}" />
<style>
  body { font-family: system-ui, sans-serif; font-size: 15px; line-height: 1.6; margin: 0; padding: 4px; unicode-bidi: isolate; }
</style>
</head>
<body dir="auto">${hit.articleHtml}</body>
</html>`;
}
