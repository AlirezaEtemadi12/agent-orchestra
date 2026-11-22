import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { RunDetail, RunListItem } from './types';

type RunsFilter = {
  project?: string | null;
  status?: string | null;
  repo?: string | null;
};

function query(filter: RunsFilter): string {
  const params = new URLSearchParams();

  if (filter.project) {
    params.set('project', filter.project);
  }

  if (filter.status) {
    params.set('status', filter.status);
  }

  if (filter.repo) {
    params.set('repo', filter.repo);
  }

  const text = params.toString();

  return text.length === 0 ? 'runs' : `runs?${text}`;
}

export const runsApi = {
  list: (locale: Locale, filter: RunsFilter) => apiClient.get<RunListItem[]>(query(filter), locale),
  detail: (locale: Locale, project: string, runKey: string) =>
    apiClient.get<RunDetail>(
      `runs/${encodeURIComponent(project)}/${encodeURIComponent(runKey)}`,
      locale,
    ),
};
