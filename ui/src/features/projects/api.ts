import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type {
  Connect,
  EffectiveConfigDto,
  FolderCheck,
  FolderList,
  ProjectBody,
  ProjectStoreState,
  SavedProject,
} from './types';

function encode(value: string): string {
  return encodeURIComponent(value);
}

export const projectsApi = {
  store: (locale: Locale) => apiClient.get<ProjectStoreState>('projects/store', locale),
  create: (locale: Locale, body: ProjectBody) => apiClient.post<SavedProject>('projects', locale, body),
  update: (locale: Locale, id: string, body: ProjectBody) =>
    apiClient.put<SavedProject>(`projects/${encode(id)}`, locale, body),
  remove: (locale: Locale, id: string) => apiClient.delete(`projects/${encode(id)}`, locale),
  check: (locale: Locale, rootPath: string) =>
    apiClient.post<FolderCheck>('projects/check', locale, { rootPath }),
  effective: (locale: Locale, id: string) =>
    apiClient.get<EffectiveConfigDto>(`projects/${encode(id)}/effective`, locale),
  prompt: (locale: Locale, body: { name: string; rootPath: string; repos: string[]; check: FolderCheck }) =>
    apiClient.post<{ text: string }>('projects/scaffold-prompt', locale, body),
  // Plan 0009: where the dashboard runs, the folders it may list, and what to paste.
  folders: (locale: Locale, path: string | null) =>
    apiClient.get<FolderList>(`system/folders${path ? `?path=${encode(path)}` : ''}`, locale),
  connect: (locale: Locale, id: string) => apiClient.get<Connect>(`projects/${encode(id)}/connect`, locale),
};
