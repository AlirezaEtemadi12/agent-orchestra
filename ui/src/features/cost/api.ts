import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { CostRow } from './types';

export type CostFilter = {
  project: string | null;
  from: string;
  to: string;
};

export type CostGroup = 'day' | 'model' | 'project' | 'month';

export function costPath(filter: CostFilter, groupBy: CostGroup, tz?: string): string {
  const params = new URLSearchParams({ from: filter.from, to: filter.to, groupBy });

  if (filter.project) {
    params.set('project', filter.project);
  }

  if (tz) {
    params.set('tz', tz);
  }

  return `usage/cost?${params.toString()}`;
}

export const costApi = {
  summary: (locale: Locale, path: string) => apiClient.get<CostRow[]>(path, locale),
};
