'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import type { Locale } from '@/lib/i18n/config';
import { projectsApi } from './api';
import type { ProjectBody, SetupRequest } from './types';

/** Plan 0009 §6.6: the browser opens on the roots and walks down from there. */
export function useFolders(path: string | null, enabled = true) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['projects', 'folders', locale, path],
    queryFn: () => projectsApi.folders(locale, path),
    enabled,
  });
}

export function useStoreState() {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['projects', 'store', locale],
    queryFn: () => projectsApi.store(locale),
  });
}

export function useEffectiveConfig(id: string | null) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['projects', id, 'effective', locale],
    queryFn: () => projectsApi.effective(locale, id as string),
    enabled: Boolean(id),
  });
}

export function useCheckFolder() {
  const locale = useLocale() as Locale;

  return useMutation({
    mutationFn: (rootPath: string) => projectsApi.check(locale, rootPath),
  });
}

/** Plan 0012 §6.5: the plan is asked again on every tick and every command edit. */
export function useSetupPlan() {
  const locale = useLocale() as Locale;

  return useMutation({
    mutationFn: (body: SetupRequest) => projectsApi.setupPlan(locale, body),
  });
}

export function useSetupApply() {
  const locale = useLocale() as Locale;
  const client = useQueryClient();

  return useMutation({
    mutationFn: (body: SetupRequest) => projectsApi.setupApply(locale, body),
    onSuccess: () => client.invalidateQueries({ queryKey: ['projects'] }),
  });
}

export function useCreateProject() {
  const locale = useLocale() as Locale;
  const client = useQueryClient();

  return useMutation({
    mutationFn: (body: ProjectBody) => projectsApi.create(locale, body),
    onSuccess: () => client.invalidateQueries({ queryKey: ['projects'] }),
  });
}

export function useUpdateProject() {
  const locale = useLocale() as Locale;
  const client = useQueryClient();

  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: ProjectBody }) => projectsApi.update(locale, id, body),
    onSuccess: () => client.invalidateQueries({ queryKey: ['projects'] }),
  });
}

export function useRemoveProject() {
  const locale = useLocale() as Locale;
  const client = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => projectsApi.remove(locale, id),
    onSuccess: () => client.invalidateQueries(),
  });
}
