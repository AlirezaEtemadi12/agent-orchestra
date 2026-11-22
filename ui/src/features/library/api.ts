import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import { env } from '@/lib/env';
import type { LibraryFilter, LibraryItem, LibraryItemDetail } from './types';

function encode(value: string): string {
  return encodeURIComponent(value);
}

function filterQuery(filter: LibraryFilter): string {
  const params = new URLSearchParams({ category: filter.category });

  if (filter.project) {
    params.set('project', filter.project);
  }

  if (filter.status) {
    params.set('status', filter.status);
  }

  if (filter.sort) {
    params.set('sort', filter.sort);
  }

  if (filter.dir) {
    params.set('dir', filter.dir);
  }

  return `library?${params.toString()}`;
}

function itemPath(project: string, category: string, relPath: string): string {
  return `library/${encode(project)}/${category.toLowerCase()}/item?path=${encode(relPath)}`;
}

export const libraryApi = {
  list: (locale: Locale, filter: LibraryFilter) => apiClient.get<LibraryItem[]>(filterQuery(filter), locale),
  item: (locale: Locale, project: string, category: string, relPath: string) =>
    apiClient.get<LibraryItemDetail>(itemPath(project, category, relPath), locale),
  prompt: (locale: Locale, project: string, relPath: string) =>
    apiClient.get<{ text: string }>(
      `library/${encode(project)}/plan/prompt?path=${encode(relPath)}`,
      locale,
    ),
  text: (locale: Locale, project: string, category: string, relPath: string) =>
    apiClient.text(`library/${encode(project)}/${category.toLowerCase()}/file?path=${encode(relPath)}`, locale),
};

function base(): string {
  return env.apiBaseUrl.replace(/\/+$/, '');
}

/** The file endpoint, for an image, a markdown body or the preview frame. */
export function fileUrl(project: string, category: string, relPath: string): string {
  return `${base()}/api/library/${encode(project)}/${category.toLowerCase()}/file?path=${encode(relPath)}`;
}

/** The server returns the preview's API path; the frame needs the full origin. */
export function absoluteApiUrl(relative: string): string {
  return `${base()}/api/${relative.replace(/^\/+/, '')}`;
}
