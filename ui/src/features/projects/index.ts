import { Cog } from 'lucide-react';
import { defineFeature } from '@/lib/features/define-feature';
import { projectsEn } from './i18n/en';
import { projectsFa } from './i18n/fa';
import { projectsManifest } from './projects.manifest';

export const projectsMessages = { fa: projectsFa, en: projectsEn } as const;

export { ProjectsPage } from './components/projects-page';
export { ProjectForm } from './components/project-form';

export const projectsFeature = defineFeature({
  key: 'projects',
  route: '/settings/projects/',
  menu: 'settings',
  icon: Cog,
  manifest: projectsManifest,
  messages: projectsMessages,
});
