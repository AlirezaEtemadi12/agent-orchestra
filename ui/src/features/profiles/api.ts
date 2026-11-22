import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { ProfileSummary, ProfileVersion, ProfileVersions } from './types';

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
  versions: (locale: Locale, id: string, diff?: string) =>
    apiClient.get<ProfileVersions>(
      `profiles/${encode(id)}/versions${diff ? `?diff=${encodeURIComponent(diff)}` : ''}`,
      locale,
    ),
};
