'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import { buildGridifyQuery } from '@/lib/gridify/build-query';
import type { Locale } from '@/lib/i18n/config';
import { runsApi } from './api';
import type { RunsQuery } from './types';

const SEARCHABLE_FIELDS = ['taskId', 'planNumber', 'planTitle', 'task'] as const;

function gridify(query: RunsQuery) {
  return buildGridifyQuery({
    page: query.page,
    pageSize: query.pageSize,
    orderBy: query.orderBy,
    search: query.search,
    searchableFields: SEARCHABLE_FIELDS,
    filters: [
      ...(query.status ? [{ field: 'status', value: query.status }] : []),
      ...(query.repo ? [{ field: 'repo', value: query.repo }] : []),
    ],
  });
}

export { useProjects } from '@/lib/projects/queries';

export function useRunsTable(query: RunsQuery) {
  const locale = useLocale() as Locale;
  const built = gridify(query);

  return useQuery({
    queryKey: ['runs', 'table', locale, query.project, built],
    queryFn: () => runsApi.table(locale, query.project, built),
  });
}

export function useRunsSummary(query: RunsQuery) {
  const locale = useLocale() as Locale;
  const built = gridify(query);

  return useQuery({
    queryKey: ['runs', 'summary', locale, query.project, built.filter],
    queryFn: () => runsApi.summary(locale, query.project, built.filter),
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
