import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { ImportLogDto, ImportResultDto } from './types';

export const importsApi = {
  reimport: (locale: Locale) => apiClient.post<ImportResultDto[]>('import', locale),
  logs: (locale: Locale, take = 20) => apiClient.get<ImportLogDto[]>(`import/logs?take=${take}`, locale),
};
