import { useLocale } from 'next-intl';
import { useMemo } from 'react';
import { FORMATTING_LOCALE, type Locale } from './config';

type DateInput = Date | string | number;

const DASH = '—';

export function formatDate(locale: Locale, value: DateInput): string {
  // Persian shows the Jalali calendar as numbers (1405/08/28); English keeps the medium style.
  const options: Intl.DateTimeFormatOptions =
    locale === 'fa'
      ? { year: 'numeric', month: '2-digit', day: '2-digit' }
      : { dateStyle: 'medium' };

  return new Intl.DateTimeFormat(FORMATTING_LOCALE[locale], options).format(new Date(value));
}

export function formatDateTime(locale: Locale, value: DateInput): string {
  return new Intl.DateTimeFormat(FORMATTING_LOCALE[locale], {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

export function formatNumber(locale: Locale, value: number): string {
  return new Intl.NumberFormat(FORMATTING_LOCALE[locale]).format(value);
}

export function formatMoney(locale: Locale, usd: number | null): string {
  if (usd === null) {
    return DASH;
  }

  const amount = new Intl.NumberFormat(FORMATTING_LOCALE[locale], {
    minimumFractionDigits: 3,
    maximumFractionDigits: 3,
  }).format(usd);

  return `$${amount}`;
}

export function formatTokens(locale: Locale, value: number): string {
  const trimmed = (number: number): string =>
    new Intl.NumberFormat(FORMATTING_LOCALE[locale], { maximumFractionDigits: 1 }).format(number);

  if (Math.abs(value) >= 1_000_000) {
    return `${trimmed(value / 1_000_000)}m`;
  }

  if (Math.abs(value) >= 1_000) {
    return `${trimmed(value / 1_000)}k`;
  }

  return trimmed(value);
}

export function formatDuration(seconds: number): string {
  const total = Math.max(0, Math.round(seconds));
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const rest = total % 60;
  const two = (part: number): string => part.toString().padStart(2, '0');

  return hours > 0 ? `${hours}:${two(minutes)}:${two(rest)}` : `${minutes}:${two(rest)}`;
}

export function formatPercent(locale: Locale, value: number): string {
  return new Intl.NumberFormat(FORMATTING_LOCALE[locale], {
    style: 'percent',
    maximumFractionDigits: 0,
  }).format(value);
}

export function useAppFormatter() {
  const locale = useLocale() as Locale;

  return useMemo(
    () => ({
      date: (value: DateInput) => formatDate(locale, value),
      dateTime: (value: DateInput) => formatDateTime(locale, value),
      number: (value: number) => formatNumber(locale, value),
      money: (usd: number | null) => formatMoney(locale, usd),
      tokens: (value: number) => formatTokens(locale, value),
      duration: (seconds: number) => formatDuration(seconds),
      percent: (value: number) => formatPercent(locale, value),
    }),
    [locale],
  );
}
