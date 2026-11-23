'use client';

import { Languages } from 'lucide-react';
import { useLocale, useTranslations } from 'next-intl';
// Next's own hooks on purpose: next-intl's usePathname drops the locale and its router adds the current
// one back, so swapping through them produced /en/fa/ (a 404).
import { usePathname, useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { LOCALE_STORAGE_KEY, LOCALES, type Locale } from '@/lib/i18n/config';
import { swapLocale } from '@/lib/i18n/locale-path';

export function LocaleSwitcher() {
  const t = useTranslations('common');
  const locale = useLocale() as Locale;
  const pathname = usePathname();
  const router = useRouter();

  const change = (next: Locale) => {
    if (next === locale) {
      return;
    }

    try {
      window.localStorage.setItem(LOCALE_STORAGE_KEY, next);
    } catch {
      // A blocked storage must not block the switch.
    }

    router.replace(swapLocale(pathname, window.location.search, next));
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label={t('locale.label')}>
          <Languages className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {LOCALES.map((item) => (
          <DropdownMenuItem
            key={item}
            onSelect={() => change(item)}
            data-active={item === locale}
            className="data-[active=true]:bg-accent"
          >
            {item === 'fa' ? 'فارسی' : 'English'}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
