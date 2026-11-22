import { Layers } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { poolsEn } from './i18n/en';
import { poolsFa } from './i18n/fa';
import { poolsManifest } from './pools.manifest';

export const poolsMessages = { fa: poolsFa, en: poolsEn } as const;

export { PoolCards, Pools } from './components/pools';

export const poolsFeature = defineFeature({
  key: 'pools',
  route: '/pools/',
  menu: 'work',
  icon: Layers,
  manifest: poolsManifest,
  messages: poolsMessages,
});
