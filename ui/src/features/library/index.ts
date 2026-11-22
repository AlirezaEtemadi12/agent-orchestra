import { LibraryBig } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { libraryEn } from './i18n/en';
import { libraryFa } from './i18n/fa';
import { libraryManifest } from './library.manifest';

export const libraryMessages = { fa: libraryFa, en: libraryEn } as const;

export { LibraryPage } from './components/library-page';
export { LibraryView } from './components/library-view';

export const libraryFeature = defineFeature({
  key: 'library',
  route: '/library/',
  menu: 'work',
  icon: LibraryBig,
  manifest: libraryManifest,
  messages: libraryMessages,
});
