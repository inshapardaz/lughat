import { useState } from 'react';
import { AppShell, Alert, Loader, Stack, Text, TextInput, Title } from '@mantine/core';
import { useEngineInfo } from './useEngineInfo';
import type { LookupResult } from './api';
import { lookup } from './api';

export default function App() {
  const { engineInfo, error: engineError } = useEngineInfo();
  const [term, setTerm] = useState('');
  const [results, setResults] = useState<LookupResult[]>([]);
  const [searchError, setSearchError] = useState<string | null>(null);

  async function handleChange(value: string) {
    setTerm(value);
    setSearchError(null);

    if (!engineInfo || value.trim().length === 0) {
      setResults([]);
      return;
    }

    try {
      setResults(await lookup(engineInfo, value.trim(), 'prefix'));
    } catch (err) {
      setSearchError(err instanceof Error ? err.message : 'Lookup failed.');
    }
  }

  return (
    <AppShell header={{ height: 60 }} padding="md">
      <AppShell.Header p="md">
        <Title order={3}>Lughat — spike search</Title>
      </AppShell.Header>
      <AppShell.Main>
        <Stack maw={480} mx="auto" mt="xl" gap="md">
          {engineError && <Alert color="red" title="Engine connection failed">{engineError}</Alert>}
          {!engineInfo && !engineError && (
            <Stack align="center" gap="xs">
              <Loader size="sm" />
              <Text c="dimmed" size="sm">Connecting to the dictionary engine…</Text>
            </Stack>
          )}
          {engineInfo && (
            <>
              <TextInput
                label="Search"
                placeholder="Try “apple”, “book” or “cat”"
                value={term}
                onChange={(event) => handleChange(event.currentTarget.value)}
                autoFocus
              />
              {searchError && <Alert color="red">{searchError}</Alert>}
              <Stack gap="sm">
                {results.map((result) => (
                  <Stack key={result.headword} gap={2}>
                    <Text fw={600}>{result.headword}</Text>
                    <Text c="dimmed">{result.article}</Text>
                  </Stack>
                ))}
                {term.trim().length > 0 && results.length === 0 && !searchError && (
                  <Text c="dimmed" size="sm">No matches yet.</Text>
                )}
              </Stack>
            </>
          )}
        </Stack>
      </AppShell.Main>
    </AppShell>
  );
}
