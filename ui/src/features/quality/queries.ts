'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import { apiClient } from '@/lib/api/client';
import type { Locale } from '@/lib/i18n/config';
import type { ModelQuality } from './types';

export function useQuality(project: string | null) {
  const locale = useLocale() as Locale;
  const path = project ? `usage/quality?project=${encodeURIComponent(project)}` : 'usage/quality';

  return useQuery({
    queryKey: ['quality', locale, project],
    queryFn: () => apiClient.get<ModelQuality>(path, locale),
  });
}
