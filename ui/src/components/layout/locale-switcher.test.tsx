import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LOCALE_STORAGE_KEY } from '@/lib/i18n/config';
import { swapLocale } from '@/lib/i18n/locale-path';

const replace = vi.fn();
let browserPath = '/en/';

vi.mock('next/navigation', () => ({
  usePathname: () => browserPath,
  useRouter: () => ({ replace }),
}));

vi.mock('next-intl', () => ({
  useLocale: () => 'en',
  useTranslations: () => (key: string) => key,
}));

const { LocaleSwitcher } = await import('./locale-switcher');

describe('locale switcher', () => {
  beforeEach(() => {
    window.localStorage.clear();
    replace.mockClear();
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

  it.each([
    ['/en/', '/fa/'],
    ['/en/cost/', '/fa/cost/'],
  ])('goes from %s to %s, never to a doubled locale', async (from, to) => {
    browserPath = from;
    const user = userEvent.setup();

    render(<LocaleSwitcher />);
    await user.click(screen.getByRole('button', { name: 'locale.label' }));
    await user.click(await screen.findByText('فارسی'));

    expect(replace).toHaveBeenCalledWith(to);
    expect(window.localStorage.getItem(LOCALE_STORAGE_KEY)).toBe('fa');
  });
});
