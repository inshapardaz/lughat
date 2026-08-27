import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';
import ur from './locales/ur.json';

// Adding a language is dropping in a new /locales/<lang> bundle here — no component
// changes — per spec §10's resource-bundle architecture.
export const SUPPORTED_LANGUAGES = ['en', 'ur'] as const;
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

const RTL_LANGUAGES = new Set<SupportedLanguage>(['ur']);

export function directionFor(language: string): 'rtl' | 'ltr' {
  return RTL_LANGUAGES.has(language as SupportedLanguage) ? 'rtl' : 'ltr';
}

void i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    ur: { translation: ur },
  },
  lng: 'en',
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
});

export default i18n;
