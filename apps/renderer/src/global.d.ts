export interface EngineInfo {
  baseUrl: string;
  token: string;
}

declare global {
  interface Window {
    lexroot: {
      getEngineInfo(): Promise<EngineInfo>;
    };
  }
}
