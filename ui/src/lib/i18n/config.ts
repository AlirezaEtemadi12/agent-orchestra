export const LOCALES = ['fa', 'en'] as const;

export type Locale = (typeof LOCALES)[number];

export const LOCALE_DIRECTION: Record<Locale, 'rtl' | 'ltr'> = {
  fa: 'rtl',
  en: 'ltr',
};

/** Latin digits in Persian, so ids, costs and token counts read the same in both languages. */
export const FORMATTING_LOCALE: Record<Locale, string> = {
  fa: 'fa-u-nu-latn',
  en: 'en',
};

export const LOCALE_STORAGE_KEY = 'orchestra.locale';

export function localeHtmlProps(locale: Locale): { lang: Locale; dir: 'rtl' | 'ltr' } {
  return { lang: locale, dir: LOCALE_DIRECTION[locale] };
}
