import { Layers } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { profilesEn } from './i18n/en';
import { profilesFa } from './i18n/fa';
import { profilesManifest } from './profiles.manifest';

export const profilesMessages = { fa: profilesFa, en: profilesEn } as const;

export { ProfilesPage } from './components/profiles-page';
export { ProfileEditor } from './components/profile-editor';

export const profilesFeature = defineFeature({
  key: 'profiles',
  route: '/settings/profiles/',
  menu: 'settings',
  icon: Layers,
  manifest: profilesManifest,
  messages: profilesMessages,
});
