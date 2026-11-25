'use client';

import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { useCallback } from 'react';

export type TableState = {
  page: number;
  pageSize: number;
  orderBy: string;
  q: string;
};

export type TableQueryOptions = {
  defaultOrderBy: string;
  defaultPageSize: number;
};

const PAGE_SIZES = [10, 20, 50, 100] as const;

export type TablePageSize = (typeof PAGE_SIZES)[number];

function pageFrom(value: string | null): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= 1 ? parsed : 1;
}

function pageSizeFrom(value: string | null, fallback: number): TablePageSize {
  if (value === null) {
    return PAGE_SIZES.includes(fallback as TablePageSize) ? (fallback as TablePageSize) : 20;
  }

  const parsed = Number(value);
  return PAGE_SIZES.includes(parsed as TablePageSize) ? (parsed as TablePageSize) : 20;
}

/**
 * Plan 0010 D9: the page, its size, the order and the search live in the URL. The hook writes only
 * its four keys and preserves every other parameter; `state.orderBy` is always the effective order.
 */
export function useTableQuery({ defaultOrderBy, defaultPageSize }: TableQueryOptions) {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const state: TableState = {
    page: pageFrom(searchParams.get('page')),
    pageSize: pageSizeFrom(searchParams.get('pageSize'), defaultPageSize),
    orderBy: searchParams.get('orderBy') ?? defaultOrderBy,
    q: searchParams.get('q') ?? '',
  };

  const params = Object.fromEntries(searchParams.entries());

  const write = useCallback(
    (next: URLSearchParams, mode: 'push' | 'replace') => {
      const query = next.toString();
      const url = query ? `${pathname}?${query}` : pathname;

      if (mode === 'replace') {
        router.replace(url, { scroll: false });
      } else {
        router.push(url, { scroll: false });
      }
    },
    [pathname, router],
  );

  const change = useCallback(
    (update: (next: URLSearchParams) => void, mode: 'push' | 'replace') => {
      const next = new URLSearchParams(searchParams.toString());
      update(next);

      if (next.get('page') === '1') {
        next.delete('page');
      }

      if (next.get('pageSize') === String(defaultPageSize)) {
        next.delete('pageSize');
      }

      if (!next.get('orderBy') || next.get('orderBy') === defaultOrderBy) {
        next.delete('orderBy');
      }

      if (!next.get('q')) {
        next.delete('q');
      }

      write(next, mode);
    },
    [searchParams, defaultOrderBy, defaultPageSize, write],
  );

  const setPage = useCallback(
    (page: number) => {
      change((next) => next.set('page', String(Math.max(1, page))), 'push');
    },
    [change],
  );

  const setPageSize = useCallback(
    (pageSize: number) => {
      change((next) => {
        next.set('pageSize', String(pageSize));
        next.delete('page');
      }, 'push');
    },
    [change],
  );

  const setOrderBy = useCallback(
    (orderBy: string) => {
      change((next) => {
        if (orderBy) {
          next.set('orderBy', orderBy);
        } else {
          next.delete('orderBy');
        }

        next.delete('page');
      }, 'push');
    },
    [change],
  );

  const setSearch = useCallback(
    (q: string) => {
      const text = q.trim();

      change((next) => {
        if (text) {
          next.set('q', text);
        } else {
          next.delete('q');
        }

        next.delete('page');
      }, 'replace');
    },
    [change],
  );

  return { state, params, setPage, setPageSize, setOrderBy, setSearch };
}
