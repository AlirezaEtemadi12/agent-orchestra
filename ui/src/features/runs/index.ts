import { ListChecks } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { runsEn } from './i18n/en';
import { runsFa } from './i18n/fa';
import { runsManifest } from './runs.manifest';

export const runsMessages = { fa: runsFa, en: runsEn } as const;

export { RunsList } from './components/runs-list';
export { RunDetail } from './components/run-detail';

export const runsFeature = defineFeature({
  key: 'runs',
  route: '/',
  menu: 'overview',
  icon: ListChecks,
  manifest: runsManifest,
  messages: runsMessages,
});
