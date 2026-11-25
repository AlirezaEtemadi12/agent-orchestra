'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import { buildGridifyQuery } from '@/lib/gridify/build-query';
import type { Locale } from '@/lib/i18n/config';
import { profilesApi } from './api';
import type { ProfileVersionsTableQuery } from './types';

export function useProfiles() {
  const locale = useLocale() as Locale;

  return useQuery({ queryKey: ['profiles'], queryFn: () => profilesApi.list(locale) });
}

export function useProfile(id: string | null) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['profiles', id, 'current'],
    queryFn: () => profilesApi.get(locale, id as string),
    enabled: Boolean(id),
  });
}

export function useVersionsTable(id: string | null, query: ProfileVersionsTableQuery) {
  const locale = useLocale() as Locale;
  const built = buildGridifyQuery({
    page: query.page,
    pageSize: query.pageSize,
    orderBy: query.orderBy,
  });

  return useQuery({
    queryKey: ['profiles', id, 'versions', 'table', built],
    queryFn: () => profilesApi.versionsTable(locale, id as string, built),
    enabled: Boolean(id),
  });
}

export function useVersionDiff(id: string | null, diff?: string) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['profiles', id, 'versions', 'diff', diff ?? null],
    queryFn: () => profilesApi.diff(locale, id as string, diff as string),
    enabled: Boolean(id && diff),
  });
}

export function useSaveProfile(id: string) {
  const locale = useLocale() as Locale;
  const client = useQueryClient();

  return useMutation({
    mutationFn: ({ json, reason }: { json: string; reason: string }) =>
      profilesApi.save(locale, id, json, reason),
    onSuccess: () => client.invalidateQueries({ queryKey: ['profiles'] }),
  });
}

export function useRevertProfile(id: string) {
  const locale = useLocale() as Locale;
  const client = useQueryClient();

  return useMutation({
    mutationFn: (version: number) => profilesApi.revert(locale, id, version),
    onSuccess: () => client.invalidateQueries({ queryKey: ['profiles'] }),
  });
}
