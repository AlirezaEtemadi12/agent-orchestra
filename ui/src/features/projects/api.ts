import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type {
  EffectiveConfigDto,
  FolderCheck,
  FolderList,
  ProjectBody,
  ProjectStoreState,
  SavedProject,
  SetupPlan,
  SetupRequest,
  SetupResult,
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
  // Plan 0012 §6.3.2: what the setup screen would do, and then doing it.
  setupPlan: (locale: Locale, body: SetupRequest) =>
    apiClient.post<SetupPlan>('projects/setup/plan', locale, body),
  setupApply: (locale: Locale, body: SetupRequest) =>
    apiClient.post<SetupResult>('projects/setup/apply', locale, body),
  // Plan 0009: where the dashboard runs, the folders it may list, and what to paste.
  folders: (locale: Locale, path: string | null) =>
    apiClient.get<FolderList>(`system/folders${path ? `?path=${encode(path)}` : ''}`, locale),
};
