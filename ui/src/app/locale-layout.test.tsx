import { describe, expect, it } from 'vitest';
import { localeHtmlProps } from '@/lib/i18n/config';

describe('the locale layout', () => {
  it('renders Persian right to left and English left to right', () => {
    expect(localeHtmlProps('fa')).toEqual({ lang: 'fa', dir: 'rtl' });
    expect(localeHtmlProps('en')).toEqual({ lang: 'en', dir: 'ltr' });
  });
});
