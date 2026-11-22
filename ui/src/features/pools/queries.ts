'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import type { Locale } from '@/lib/i18n/config';
import { poolsApi } from './api';

/// D6: five seconds while the tab is visible, never in the background.
export function usePools() {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['pools', locale],
    queryFn: () => poolsApi.list(locale),
    refetchInterval: 5000,
    refetchIntervalInBackground: false,
  });
}
