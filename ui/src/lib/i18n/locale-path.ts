import { LOCALES, type Locale } from './config';

/** `/fa/cost/` + `?project=a` + `en` → `/en/cost/?project=a`. The query string is never dropped. */
export function swapLocale(pathname: string, search: string, locale: Locale): string {
  const segments = pathname.split('/');
  const next = [...segments];

  if (LOCALES.includes(segments[1] as Locale)) {
    next[1] = locale;
  } else {
    next.splice(1, 0, locale);
  }

  return `${next.join('/')}${search}`;
}
