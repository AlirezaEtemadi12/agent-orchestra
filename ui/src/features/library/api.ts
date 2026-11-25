import type { Paging } from 'gridify-client';
import type { Locale } from '@/lib/i18n/config';
import { apiClient } from '@/lib/api/client';
import type { TableQuery } from '@/lib/gridify/build-query';
import { env } from '@/lib/env';
import type { LibraryItem, LibraryItemDetail, LibraryTableQuery } from './types';

function encode(value: string): string {
  return encodeURIComponent(value);
}

function tablePath(filter: LibraryTableQuery, built: TableQuery): string {
  const params = new URLSearchParams({ category: filter.category });

  if (filter.project) {
    params.set('project', filter.project);
  }

  params.set('page', String(built.page));
  params.set('pageSize', String(built.pageSize));

  if (built.orderBy) {
    params.set('orderBy', built.orderBy);
  }

  if (built.filter) {
    params.set('filter', built.filter);
  }

  return `library/table?${params.toString()}`;
}

function itemPath(project: string, category: string, relPath: string): string {
  return `library/${encode(project)}/${category.toLowerCase()}/item?path=${encode(relPath)}`;
}

export const libraryApi = {
  table: (locale: Locale, filter: LibraryTableQuery, built: TableQuery) =>
    apiClient.get<Paging<LibraryItem>>(tablePath(filter, built), locale),
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
