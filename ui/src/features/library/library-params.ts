import type { LibraryCategory } from './types';

export const LIBRARY_TABS = [
  { tab: 'plans', category: 'Plan' },
  { tab: 'previews', category: 'Preview' },
  { tab: 'ideas', category: 'Idea' },
  { tab: 'research', category: 'Research' },
  { tab: 'evidence', category: 'Evidence' },
] as const;

export type LibraryTab = (typeof LIBRARY_TABS)[number];

export type LibraryQuery = Record<string, string>;

/** `?tab=` picks the category; anything unknown falls back to plans. */
export function tabFrom(value: string | null): LibraryTab {
  return LIBRARY_TABS.find((tab) => tab.tab === value) ?? LIBRARY_TABS[0];
}

export function tabFor(category: string): LibraryTab {
  return LIBRARY_TABS.find((tab) => tab.category.toLowerCase() === category.toLowerCase()) ?? LIBRARY_TABS[0];
}

/** Status is a plan and an idea field; the other tabs hide the filter (plan 0002 §6.6). */
export function statusColumnVisible(category: LibraryCategory): boolean {
  return category === 'Plan' || category === 'Idea';
}

export function tabQuery(tab: LibraryTab, project: string | null): LibraryQuery {
  return {
    tab: tab.tab,
    ...(project ? { project } : {}),
  };
}

/** The detail URL always names the category, so an item's page is complete on its own. */
export function detailQuery(category: LibraryCategory, project: string, relPath: string): LibraryQuery {
  return { project, category, path: relPath };
}

export function detailHref(category: LibraryCategory, project: string, relPath: string) {
  return { pathname: '/library/view/', query: detailQuery(category, project, relPath) };
}

export function libraryHref(query: LibraryQuery) {
  return { pathname: '/library/', query };
}
