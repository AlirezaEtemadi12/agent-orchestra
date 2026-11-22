import { Coins } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { costManifest } from './cost.manifest';
import { costEn } from './i18n/en';
import { costFa } from './i18n/fa';

export const costMessages = { fa: costFa, en: costEn } as const;

export { CostPage } from './components/cost-page';

export const costFeature = defineFeature({
  key: 'cost',
  route: '/cost/',
  menu: 'overview',
  icon: Coins,
  manifest: costManifest,
  messages: costMessages,
});
