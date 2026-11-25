'use client';

import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { useLocale, useTranslations } from 'next-intl';
import { Button } from '@/components/ui/button';
import { useAppFormatter } from '@/lib/i18n/formatter';

const SIZES = [10, 20, 50, 100] as const;

export function Pagination({
  page,
  pageSize,
  count,
  onPageChange,
  onPageSizeChange,
}: {
  page: number;
  pageSize: number;
  count: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}) {
  const common = useTranslations('common');
  const locale = useLocale();
  const format = useAppFormatter();

  const pageCount = Math.max(1, Math.ceil(count / pageSize));
  const from = count === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, count);
  const rtl = locale === 'fa';

  const First = rtl ? ChevronsRight : ChevronsLeft;
  const Previous = rtl ? ChevronRight : ChevronLeft;
  const Next = rtl ? ChevronLeft : ChevronRight;
  const Last = rtl ? ChevronsLeft : ChevronsRight;

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
      <p className="text-muted-foreground">
        {common('table.range', {
          from: format.number(from),
          to: format.number(to),
          total: format.number(count),
        })}
      </p>

      <div className="flex flex-wrap items-center gap-1">
        <label className="flex items-center gap-2 text-muted-foreground">
          <span className="sr-only">{common('table.pageSize')}</span>
          <select
            className="h-8 rounded-md border bg-background px-2"
            value={pageSize}
            onChange={(event) => onPageSizeChange(Number(event.target.value))}
          >
            {SIZES.map((size) => (
              <option key={size} value={size}>
                {format.number(size)}
              </option>
            ))}
          </select>
        </label>

        <Button
          type="button"
          size="icon-sm"
          variant="outline"
          aria-label={common('table.first')}
          disabled={page <= 1}
          onClick={() => onPageChange(1)}
        >
          <First aria-hidden />
        </Button>
        <Button
          type="button"
          size="icon-sm"
          variant="outline"
          aria-label={common('table.previous')}
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          <Previous aria-hidden />
        </Button>
        <Button
          type="button"
          size="icon-sm"
          variant="outline"
          aria-label={common('table.next')}
          disabled={page >= pageCount}
          onClick={() => onPageChange(page + 1)}
        >
          <Next aria-hidden />
        </Button>
        <Button
          type="button"
          size="icon-sm"
          variant="outline"
          aria-label={common('table.last')}
          disabled={page >= pageCount}
          onClick={() => onPageChange(pageCount)}
        >
          <Last aria-hidden />
        </Button>
      </div>
    </div>
  );
}
