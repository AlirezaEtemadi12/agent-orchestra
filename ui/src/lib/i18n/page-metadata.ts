import type { Metadata } from 'next';
import { messages } from '@/i18n/messages';
import type { FeatureKey } from '@/lib/features/define-feature';
import type { Locale } from './config';

export function pageMetadata(locale: Locale, namespace: 'common' | FeatureKey): Metadata {
  const catalogue = messages[locale][namespace] as { title?: string };

  return { title: catalogue.title };
}
