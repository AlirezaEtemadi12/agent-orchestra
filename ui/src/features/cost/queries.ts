'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import type { Locale } from '@/lib/i18n/config';
import { costApi, costPath, type CostFilter, type CostGroup } from './api';

export function useCostSummary(filter: CostFilter, groupBy: CostGroup, tz?: string) {
  const locale = useLocale() as Locale;
  const path = costPath(filter, groupBy, tz);

  return useQuery({
    queryKey: ['cost', locale, path],
    queryFn: () => costApi.summary(locale, path),
  });
}
