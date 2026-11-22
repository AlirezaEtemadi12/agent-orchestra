import { Gauge } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { boardManifest } from './board.manifest';
import { boardEn } from './i18n/en';
import { boardFa } from './i18n/fa';

export const boardMessages = { fa: boardFa, en: boardEn } as const;

export { Board, BoardCardView, BoardLanes } from './components/board';

export const boardFeature = defineFeature({
  key: 'board',
  route: '/board/',
  menu: 'work',
  icon: Gauge,
  manifest: boardManifest,
  messages: boardMessages,
});
