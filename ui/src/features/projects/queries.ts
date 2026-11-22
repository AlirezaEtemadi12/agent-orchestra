'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import type { Locale } from '@/lib/i18n/config';
import { projectsApi } from './api';
import type { FolderCheck, ProjectBody } from './types';

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

export function useScaffoldPrompt() {
  const locale = useLocale() as Locale;

  return useMutation({
    mutationFn: (body: { name: string; rootPath: string; repos: string[]; check: FolderCheck }) =>
      projectsApi.prompt(locale, body),
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
