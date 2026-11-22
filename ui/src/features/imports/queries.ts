'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import type { Locale } from '@/lib/i18n/config';
import { importsApi } from './api';

export function useImportLogs(take = 20) {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['imports', locale, take],
    queryFn: () => importsApi.logs(locale, take),
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
