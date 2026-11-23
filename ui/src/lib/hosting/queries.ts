'use client';

import { useQuery } from '@tanstack/react-query';
import { useLocale } from 'next-intl';
import { apiClient } from '@/lib/api/client';
import type { Locale } from '@/lib/i18n/config';

/** `GET api/system/hosting` (plan 0009 §6.2): where the dashboard runs and which folders it can see. */
export type Hosting = {
  mode: 'container' | 'direct';
  roots: string[];
  mapped: boolean;
  tokenSource: 'settings' | 'generated';
};

/** One query for every feature that asks (the project form and the start page). */
export function useHosting() {
  const locale = useLocale() as Locale;

  return useQuery({
    queryKey: ['hosting', locale],
    queryFn: () => apiClient.get<Hosting>('system/hosting', locale),
  });
}
