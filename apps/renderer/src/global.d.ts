export interface EngineInfo {
  baseUrl: string;
  token: string;
}

export interface EngineStatus {
  info: EngineInfo | null;
  connected: boolean;
}

declare global {
  interface Window {
    lughat: {
      getEngineInfo(): Promise<EngineInfo>;
      pickDictionaryFile(): Promise<string | null>;
      onEngineStatus(callback: (status: EngineStatus) => void): () => void;
    };
  }
}
