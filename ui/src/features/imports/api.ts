import type { Paging } from 'gridify-client';
import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { TableQuery } from '@/lib/gridify/build-query';
import type { ImportLogDto, ImportResultDto } from './types';

export const importsApi = {
  reimport: (locale: Locale) => apiClient.post<ImportResultDto[]>('import', locale),
  table: (locale: Locale, query: TableQuery) => {
    const params = new URLSearchParams();

    params.set('page', String(query.page));
    params.set('pageSize', String(query.pageSize));

    if (query.orderBy) {
      params.set('orderBy', query.orderBy);
    }

    if (query.filter) {
      params.set('filter', query.filter);
    }

    return apiClient.get<Paging<ImportLogDto>>(`import/logs/table?${params.toString()}`, locale);
  },
};
