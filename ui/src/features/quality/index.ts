import { Gauge } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { qualityEn } from './i18n/en';
import { qualityFa } from './i18n/fa';
import { qualityManifest } from './quality.manifest';

export const qualityMessages = { fa: qualityFa, en: qualityEn } as const;

export { QualityPage } from './components/quality-page';

export const qualityFeature = defineFeature({
  key: 'quality',
  route: '/quality/',
  menu: 'overview',
  icon: Gauge,
  manifest: qualityManifest,
  messages: qualityMessages,
});
