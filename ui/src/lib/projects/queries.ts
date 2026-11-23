'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import { apiClient } from '@/lib/api/client';
import type { Locale } from '@/lib/i18n/config';

export type ProjectDto = {
  id: string;
  name: string;
  runCount: number;
  openRunCount: number;
  sessions: number;
  calls: number;
  requests: number;
  costUsd: number | null;
  lastImport: { finishedAt: string; errorCount: number } | null;
  origin: 'config' | 'dashboard' | null;
  rootPath: string | null;
  runsPath: string | null;
  repos: string[];
  /** Plan 0009 D11: the last event the dashboard received from this project, UTC. */
  lastEventAt?: string | null;
};

export function useProjects() {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['projects', locale],
    queryFn: () => apiClient.get<ProjectDto[]>('projects', locale),
  });
}
