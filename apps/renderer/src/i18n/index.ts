import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';
import ur from './locales/ur.json';
import ar from './locales/ar.json';

// Adding a language is dropping in a new /locales/<lang>.json bundle plus one row in
// LANGUAGES below — no component changes — per spec §10's resource-bundle architecture.
// See locales/README.md for the full contributor workflow and the validation script that
// checks a new bundle's keys against en.json (the reference) before it ships.
export const LANGUAGES = {
  en: { label: 'English', direction: 'ltr', resource: en },
  ur: { label: 'اردو', direction: 'rtl', resource: ur },
  ar: { label: 'العربية', direction: 'rtl', resource: ar },
} as const satisfies Record<string, { label: string; direction: 'ltr' | 'rtl'; resource: unknown }>;

export const SUPPORTED_LANGUAGES = Object.keys(LANGUAGES) as (keyof typeof LANGUAGES)[];
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

export function directionFor(language: string): 'rtl' | 'ltr' {
  return LANGUAGES[language as SupportedLanguage]?.direction ?? 'ltr';
}

export function labelFor(language: string): string {
  return LANGUAGES[language as SupportedLanguage]?.label ?? language;
}

void i18n.use(initReactI18next).init({
  resources: Object.fromEntries(
    SUPPORTED_LANGUAGES.map((lang) => [lang, { translation: LANGUAGES[lang].resource }]),
  ),
  lng: 'en',
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
});

export default i18n;
