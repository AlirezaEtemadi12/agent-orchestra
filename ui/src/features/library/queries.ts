'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import { buildGridifyQuery } from '@/lib/gridify/build-query';
import type { Locale } from '@/lib/i18n/config';
import { libraryApi } from './api';
import type { LibraryTableQuery } from './types';

const SEARCHABLE_FIELDS = ['title', 'slug', 'number'] as const;

function gridify(query: LibraryTableQuery) {
  return buildGridifyQuery({
    page: query.page,
    pageSize: query.pageSize,
    orderBy: query.orderBy,
    search: query.search,
    searchableFields: SEARCHABLE_FIELDS,
    filters: query.status ? [{ field: 'status', value: query.status }] : [],
  });
}

export function useLibraryTable(query: LibraryTableQuery) {
  const locale = useLocale() as Locale;
  const built = gridify(query);

  return useQuery({
    queryKey: ['library', 'table', locale, query.project, query.category, built],
    queryFn: () => libraryApi.table(locale, query, built),
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
