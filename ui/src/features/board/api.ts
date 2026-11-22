import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { BoardResponse } from './types';

export const boardApi = {
  get: (locale: Locale, project?: string | null) =>
    apiClient.get<BoardResponse>(
      project ? `runs/board?project=${encodeURIComponent(project)}` : 'runs/board',
      locale,
    ),
};
