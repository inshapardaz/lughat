import { useEffect, useState } from 'react';
import type { EngineInfo } from './global';

export function useEngineInfo() {
  const [engineInfo, setEngineInfo] = useState<EngineInfo | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!window.lexroot) {
      setError('window.lexroot is not available — this renderer needs to run inside the Electron shell.');
      return;
    }

    window.lexroot
      .getEngineInfo()
      .then((info) => {
        if (!info) {
          setError('The engine has not finished starting yet.');
          return;
        }
        setEngineInfo(info);
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to reach the engine.'));
  }, []);

  return { engineInfo, error };
}
