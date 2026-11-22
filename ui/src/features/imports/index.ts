import { FileClock } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { importsEn } from './i18n/en';
import { importsFa } from './i18n/fa';
import { importsManifest } from './imports.manifest';

export const importsMessages = { fa: importsFa, en: importsEn } as const;

export { ImportLog } from './components/import-log';
export { ReimportButton } from './components/reimport-button';

export const importsFeature = defineFeature({
  key: 'imports',
  route: '/imports/',
  menu: 'system',
  icon: FileClock,
  manifest: importsManifest,
  messages: importsMessages,
});
