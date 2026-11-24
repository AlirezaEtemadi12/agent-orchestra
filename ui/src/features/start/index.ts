import { Rocket } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { startEn } from './i18n/en';
import { startFa } from './i18n/fa';
import { startManifest } from './start.manifest';

export const startMessages = { fa: startFa, en: startEn } as const;

export { StartPage } from './components/start-page';

export const startFeature = defineFeature({
  key: 'start',
  route: '/start/',
  menu: 'overview',
  icon: Rocket,
  manifest: startManifest,
  messages: startMessages,
});
