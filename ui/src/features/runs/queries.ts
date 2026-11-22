'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import type { Locale } from '@/lib/i18n/config';
import { runsApi } from './api';

export { useProjects } from '@/lib/projects/queries';

export function useRuns(project: string | null, status: string | null, repo: string | null) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['runs', locale, project, status, repo],
    queryFn: () => runsApi.list(locale, { project, status, repo }),
  });
}

export function useRun(project: string | null, runKey: string | null) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['run', locale, project, runKey],
    queryFn: () => runsApi.detail(locale, project!, runKey!),
    enabled: Boolean(project && runKey),
  });
}
