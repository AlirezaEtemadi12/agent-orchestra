'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import type { Locale } from '@/lib/i18n/config';
import { boardApi } from './api';

/// D6: five seconds while the tab is visible, never in the background.
export function useBoard(project: string | null = null) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['board', locale, project],
    queryFn: () => boardApi.get(locale, project),
    refetchInterval: 5000,
    refetchIntervalInBackground: false,
  });
}
