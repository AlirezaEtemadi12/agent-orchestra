import type { Paging } from 'gridify-client';
import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { TableQuery } from '@/lib/gridify/build-query';
import type {
  ProfileDiff,
  ProfileSummary,
  ProfileVersion,
  ProfileVersionSummary,
} from './types';

function encode(value: string): string {
  return encodeURIComponent(value);
}

export const profilesApi = {
  list: (locale: Locale) => apiClient.get<ProfileSummary[]>('profiles', locale),
  get: (locale: Locale, id: string, version?: number) =>
    apiClient.get<ProfileVersion>(
      `profiles/${encode(id)}${version ? `?version=${version}` : ''}`,
      locale,
    ),
  save: (locale: Locale, id: string, json: string, reason: string) =>
    apiClient.put<{ id: string; version: number; reason: string; exportedTo: string | null }>(
      `profiles/${encode(id)}`,
      locale,
      { json, reason },
    ),
  revert: (locale: Locale, id: string, version: number) =>
    apiClient.post<{ id: string; version: number }>(`profiles/${encode(id)}/revert`, locale, {
      version,
    }),
  diff: (locale: Locale, id: string, diff: string) =>
    apiClient.get<ProfileDiff>(
      `profiles/${encode(id)}/versions?diff=${encodeURIComponent(diff)}`,
      locale,
    ),
  versionsTable: (locale: Locale, id: string, query: TableQuery) => {
    const params = new URLSearchParams();

    params.set('page', String(query.page));
    params.set('pageSize', String(query.pageSize));

    if (query.orderBy) {
      params.set('orderBy', query.orderBy);
    }

    if (query.filter) {
      params.set('filter', query.filter);
    }

    return apiClient.get<Paging<ProfileVersionSummary>>(
      `profiles/${encode(id)}/versions/table?${params.toString()}`,
      locale,
    );
  },
};
