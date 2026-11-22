'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import type { Locale } from '@/lib/i18n/config';
import { libraryApi } from './api';
import type { LibraryFilter } from './types';

export function useLibrary(filter: LibraryFilter) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: [
      'library',
      locale,
      filter.project ?? null,
      filter.category,
      filter.status ?? null,
      filter.sort ?? null,
      filter.dir ?? null,
    ],
    queryFn: () => libraryApi.list(locale, filter),
  });
}

export function useLibraryItem(project: string | null, category: string | null, relPath: string | null) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['library-item', locale, project, category, relPath],
    queryFn: () => libraryApi.item(locale, project!, category!, relPath!),
    enabled: Boolean(project && category && relPath),
  });
}

export function useLibraryText(project: string | null, category: string | null, relPath: string | null) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['library-text', locale, project, category, relPath],
    queryFn: () => libraryApi.text(locale, project!, category!, relPath!),
    enabled: Boolean(project && category && relPath) && category !== 'Preview',
  });
}

export function usePlanPrompt(project: string | null, relPath: string | null, enabled: boolean) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['library-prompt', locale, project, relPath],
    queryFn: () => libraryApi.prompt(locale, project!, relPath!),
    enabled: enabled && Boolean(project && relPath),
  });
}
