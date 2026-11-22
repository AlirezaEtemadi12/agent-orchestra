import { beforeEach, describe, expect, it } from 'vitest';
import { LOCALE_STORAGE_KEY } from '@/lib/i18n/config';
import { swapLocale } from '@/lib/i18n/locale-path';

describe('locale switcher', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('swaps the locale segment and keeps the query string', () => {
    expect(swapLocale('/fa/cost/', '?project=a', 'en')).toBe('/en/cost/?project=a');
    expect(swapLocale('/en/', '', 'fa')).toBe('/fa/');
  });

  it('keeps the path when the locale segment is missing', () => {
    expect(swapLocale('/cost/', '?month=2026-11', 'fa')).toBe('/fa/cost/?month=2026-11');
  });

  it('stores the chosen locale under the shared key', () => {
    window.localStorage.setItem(LOCALE_STORAGE_KEY, 'fa');

    expect(window.localStorage.getItem('orchestra.locale')).toBe('fa');
  });
});
