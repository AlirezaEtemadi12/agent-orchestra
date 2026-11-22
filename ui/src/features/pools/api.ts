import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { Pool } from './types';

export const poolsApi = {
  list: (locale: Locale) => apiClient.get<Pool[]>('pools', locale),
};
