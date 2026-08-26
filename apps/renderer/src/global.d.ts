export interface EngineInfo {
  baseUrl: string;
  token: string;
}

declare global {
  interface Window {
    lughat: {
      getEngineInfo(): Promise<EngineInfo>;
    };
  }
}
