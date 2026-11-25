'use client';

import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react';
import { useTranslations } from 'next-intl';
import type { DataTableColumn } from './data-table';

export type SortDirection = 'asc' | 'desc' | null;

export function sortDirection(orderBy: string, field: string): SortDirection {
  const [name, ...rest] = orderBy.trim().split(/\s+/);

  if (name !== field) {
    return null;
  }

  return rest[0]?.toLowerCase() === 'desc' ? 'desc' : 'asc';
}

export function ColumnHeader<TRow>({
  column,
  orderBy,
  onOrderChange,
}: {
  column: DataTableColumn<TRow>;
  orderBy: string;
  onOrderChange: (orderBy: string) => void;
}) {
  const common = useTranslations('common');

  if (!column.sortable) {
    return <span>{column.header}</span>;
  }

  const direction = sortDirection(orderBy, column.id);
  const next = direction === null ? column.id : direction === 'asc' ? `${column.id} desc` : '';
  const label =
    direction === null
      ? common('table.sortAsc')
      : direction === 'asc'
        ? common('table.sortDesc')
        : common('table.sortNone');
  const Icon = direction === null ? ArrowUpDown : direction === 'asc' ? ArrowUp : ArrowDown;

  return (
    <button
      type="button"
      onClick={() => onOrderChange(next)}
      aria-label={label}
      className="inline-flex items-center gap-1 rounded-sm text-start hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      {column.header}
      <Icon className="size-3" aria-hidden />
    </button>
  );
}
