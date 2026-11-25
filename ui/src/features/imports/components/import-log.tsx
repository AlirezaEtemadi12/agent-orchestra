'use client';

import { useTranslations } from 'next-intl';
import { DataTable, Toolbar, type DataTableColumn } from '@/components/app/data-table';
import { DateTimeText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { KpiCard } from '@/components/app/kpi-card';
import { PageHeader } from '@/components/app/page-header';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useTableQuery } from '@/lib/gridify/use-table-query';
import { useAppFormatter } from '@/lib/i18n/formatter';
import { useImportLastImport, useImportLogTable } from '../queries';
import type { ImportLogDto } from '../types';
import { ReimportButton } from './reimport-button';

export function ImportLog() {
  const t = useTranslations('imports');
  const common = useTranslations('common');
  const format = useAppFormatter();

  const tableQuery = useTableQuery({ defaultOrderBy: 'startedAt desc', defaultPageSize: 20 });
  const table = useImportLogTable({
    page: tableQuery.state.page,
    pageSize: tableQuery.state.pageSize,
    orderBy: tableQuery.state.orderBy,
    search: tableQuery.state.q,
  });
  const lastQuery = useImportLastImport();
  const last = lastQuery.data?.data[0];

  const columns: readonly DataTableColumn<ImportLogDto>[] = [
    {
      id: 'finishedAt',
      header: t('columns.finishedAt'),
      sortable: true,
      cell: (log) => <DateTimeText value={log.finishedAt} />,
    },
    {
      id: 'filesRead',
      header: t('files'),
      sortable: true,
      cell: (log) => (
        <span className="tabular-nums" data-ltr>
          {format.number(log.filesRead)}
        </span>
      ),
    },
    {
      id: 'blocksRead',
      header: t('blocks'),
      sortable: true,
      cell: (log) => (
        <span className="tabular-nums" data-ltr>
          {format.number(log.blocksRead)}
        </span>
      ),
    },
    {
      id: 'runsBuilt',
      header: t('runs'),
      sortable: true,
      cell: (log) => (
        <span className="tabular-nums" data-ltr>
          {format.number(log.runsBuilt)}
        </span>
      ),
    },
    {
      id: 'libraryItems',
      header: t('libraryItems'),
      sortable: true,
      cell: (log) => (
        <span className="tabular-nums" data-ltr>
          {format.number(log.libraryItems)}
        </span>
      ),
    },
    {
      id: 'errorCount',
      header: t('errors'),
      sortable: true,
      cell: (log) => (
        <span className="tabular-nums" data-ltr>
          {format.number(log.errorCount)}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} actions={<ReimportButton />} />

      {last ? (
        <div className="space-y-2">
          <p className="text-sm font-medium">{t('lastImport')}</p>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            <KpiCard label={t('files')} value={format.number(last.filesRead)} />
            <KpiCard label={t('blocks')} value={format.number(last.blocksRead)} />
            <KpiCard label={t('runs')} value={format.number(last.runsBuilt)} />
            <KpiCard label={t('libraryItems')} value={format.number(last.libraryItems)} />
            <KpiCard label={t('errors')} value={format.number(last.errorCount)} />
          </div>
          <p className="text-xs text-muted-foreground">
            <DateTimeText value={last.finishedAt} /> · <span data-ltr>{last.projectId}</span>
          </p>
        </div>
      ) : null}

      {last && last.errors.length > 0 ? (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('columns.file')}</TableHead>
              <TableHead>{t('columns.location')}</TableHead>
              <TableHead>{t('columns.message')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {last.errors.map((error, index) => (
              <TableRow key={`${error.file}.${index}`}>
                <TableCell data-ltr className="text-xs">
                  {error.file}
                </TableCell>
                <TableCell data-ltr className="text-xs">
                  {error.location ?? '—'}
                </TableCell>
                <TableCell className="whitespace-normal text-xs">{error.message}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : null}

      <Toolbar search={tableQuery.state.q} onSearch={tableQuery.setSearch} />

      <DataTable
        columns={columns}
        rows={table.data?.data ?? []}
        count={table.data?.count ?? 0}
        page={tableQuery.state.page}
        pageSize={tableQuery.state.pageSize}
        orderBy={tableQuery.state.orderBy}
        onPageChange={tableQuery.setPage}
        onPageSizeChange={tableQuery.setPageSize}
        onOrderChange={tableQuery.setOrderBy}
        onRetry={() => void table.refetch()}
        loading={table.isPending}
        error={table.error}
        empty={<EmptyState title={common('states.empty')} />}
      />
    </div>
  );
}
