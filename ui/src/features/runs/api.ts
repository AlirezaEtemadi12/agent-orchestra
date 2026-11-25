import type { Paging } from 'gridify-client';
import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { TableQuery } from '@/lib/gridify/build-query';
import type { RunDetail, RunListItem, RunsSummary } from './types';

export const runsApi = {
  table: (locale: Locale, project: string | null, query: TableQuery) => {
    const params = new URLSearchParams();

    if (project) {
      params.set('project', project);
    }

    params.set('page', String(query.page));
    params.set('pageSize', String(query.pageSize));

    if (query.orderBy) {
      params.set('orderBy', query.orderBy);
    }

    if (query.filter) {
      params.set('filter', query.filter);
    }

    return apiClient.get<Paging<RunListItem>>(`runs/table?${params.toString()}`, locale);
  },
  summary: (locale: Locale, project: string | null, filter: string) => {
    const params = new URLSearchParams();

    if (project) {
      params.set('project', project);
    }

    if (filter) {
      params.set('filter', filter);
    }

    return apiClient.get<RunsSummary>(`runs/summary?${params.toString()}`, locale);
  },
  detail: (locale: Locale, project: string, runKey: string) =>
    apiClient.get<RunDetail>(
      `runs/${encodeURIComponent(project)}/${encodeURIComponent(runKey)}`,
      locale,
    ),
};
