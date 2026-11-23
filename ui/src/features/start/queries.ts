'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import { apiClient } from '@/lib/api/client';
import type { Locale } from '@/lib/i18n/config';

export { useProjects } from '@/lib/projects/queries';

/** Plan 0009: the start page asks the same two questions the form does — where am I, and can I see? */
export { useHosting } from '@/lib/hosting/queries';

export function useStartRuns() {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['start', 'runs', locale],
    queryFn: () => apiClient.get<unknown[]>('runs', locale),
  });
}
