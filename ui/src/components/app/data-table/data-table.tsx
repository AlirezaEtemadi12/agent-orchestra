'use client';

import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
  type SortingState,
} from '@tanstack/react-table';
import { useTranslations } from 'next-intl';
import { useMemo, type ReactNode } from 'react';
import { ErrorState } from '@/components/app/error-state';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ApiError } from '@/lib/api/api-error';
import { ColumnHeader, sortDirection } from './column-header';
import { Pagination } from './pagination';

export type DataTableColumn<TRow> = {
  /** The column's id; when `sortable`, it is the mapped Gridify key. */
  id: string;
  /** Already translated. */
  header: string;
  sortable?: boolean;
  cell: (row: TRow) => ReactNode;
  className?: string;
  headClassName?: string;
};

export type DataTableProps<TRow> = {
  columns: readonly DataTableColumn<TRow>[];
  rows: readonly TRow[];
  count: number;
  page: number;
  pageSize: number;
  orderBy: string;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  onOrderChange: (orderBy: string) => void;
  onRetry: () => void;
  loading: boolean;
  error: unknown;
  empty: ReactNode;
};

function sortingState(orderBy: string): SortingState {
  const [field, ...rest] = orderBy.trim().split(/\s+/);

  return field ? [{ id: field, desc: rest[0]?.toLowerCase() === 'desc' }] : [];
}

export function DataTable<TRow>({
  columns,
  rows,
  count,
  page,
  pageSize,
  orderBy,
  onPageChange,
  onPageSizeChange,
  onOrderChange,
  onRetry,
  loading,
  error,
  empty,
}: DataTableProps<TRow>) {
  const common = useTranslations('common');

  const tableColumns = useMemo<ColumnDef<TRow>[]>(
    () =>
      columns.map((column) => ({
        id: column.id,
        header: column.header,
        accessorFn: (row: TRow) => row,
        enableSorting: column.sortable ?? false,
        cell: ({ row }) => column.cell(row.original),
        meta: column,
      })),
    [columns],
  );

  // TanStack Table is the table engine the plan asks for; the compiler cannot memoize its API.
  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: [...rows],
    columns: tableColumns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    manualSorting: true,
    pageCount: Math.max(1, Math.ceil(count / pageSize)),
    state: {
      pagination: { pageIndex: page - 1, pageSize },
      sorting: sortingState(orderBy),
    },
  });

  if (error) {
    return (
      <ErrorState
        title={error instanceof ApiError ? error.message : common('errors.network')}
        retryLabel={common('actions.retry')}
        onRetry={onRetry}
      />
    );
  }

  if (loading) {
    return <TableSkeleton rows={Math.min(pageSize, 10)} />;
  }

  if (count === 0) {
    return <>{empty}</>;
  }

  return (
    <div className="space-y-3">
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((group) => (
            <TableRow key={group.id}>
              {group.headers.map((header) => {
                const column = header.column.columnDef.meta as DataTableColumn<TRow>;
                const direction = sortDirection(orderBy, column.id);

                return (
                  <TableHead
                    key={header.id}
                    className={column.headClassName}
                    aria-sort={
                      direction === 'asc'
                        ? 'ascending'
                        : direction === 'desc'
                          ? 'descending'
                          : column.sortable
                            ? 'none'
                            : undefined
                    }
                  >
                    <ColumnHeader
                      column={column}
                      orderBy={orderBy}
                      onOrderChange={onOrderChange}
                    />
                  </TableHead>
                );
              })}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {table.getRowModel().rows.map((row) => (
            <TableRow key={row.id}>
              {row.getVisibleCells().map((cell) => {
                const column = cell.column.columnDef.meta as DataTableColumn<TRow>;

                return (
                  <TableCell key={cell.id} className={column.className}>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </TableCell>
                );
              })}
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Pagination
        page={page}
        pageSize={pageSize}
        count={count}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
      />
    </div>
  );
}
