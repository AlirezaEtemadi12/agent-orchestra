'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import { buildGridifyQuery } from '@/lib/gridify/build-query';
import type { Locale } from '@/lib/i18n/config';
import { importsApi } from './api';
import type { ImportLogQuery } from './types';

const SEARCHABLE_FIELDS = ['projectId'] as const;

function gridify(query: ImportLogQuery) {
  return buildGridifyQuery({
    page: query.page,
    pageSize: query.pageSize,
    orderBy: query.orderBy,
    search: query.search,
    searchableFields: SEARCHABLE_FIELDS,
  });
}

export function useImportLogTable(query: ImportLogQuery) {
  const locale = useLocale() as Locale;
  const built = gridify(query);

  return useQuery({
    queryKey: ['imports', 'table', locale, built],
    queryFn: () => importsApi.table(locale, built),
  });
}

/** The KPI/errors block always shows the latest import, on any table page. */
export function useImportLastImport() {
  const locale = useLocale() as Locale;
  const built = buildGridifyQuery({ page: 1, pageSize: 1, orderBy: 'startedAt desc' });

  return useQuery({
    queryKey: ['imports', 'last', locale],
    queryFn: () => importsApi.table(locale, built),
  });
}

export function useReimport() {
  const locale = useLocale() as Locale;
  const client = useQueryClient();

  return useMutation({
    mutationFn: () => importsApi.reimport(locale),
    onSuccess: () => client.invalidateQueries(),
  });
}
