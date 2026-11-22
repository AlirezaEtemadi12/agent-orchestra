'use client';

import { useTranslations } from 'next-intl';
import { DateTimeText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { KpiCard } from '@/components/app/kpi-card';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useImportLogs } from '../queries';
import { ReimportButton } from './reimport-button';

export function ImportLog() {
  const t = useTranslations('imports');
  const common = useTranslations('common');
  const logs = useImportLogs(20);
  const last = logs.data?.[0];

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} actions={<ReimportButton />} />

      {logs.isPending ? <TableSkeleton /> : null}

      {logs.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void logs.refetch()}
        />
      ) : null}

      {!logs.isPending && !logs.isError && (!logs.data || logs.data.length === 0) ? (
        <EmptyState title={t('none')} />
      ) : null}

      {last ? (
        <div className="space-y-2">
          <p className="text-sm font-medium">{t('lastImport')}</p>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            <KpiCard label={t('files')} value={last.filesRead} />
            <KpiCard label={t('blocks')} value={last.blocksRead} />
            <KpiCard label={t('runs')} value={last.runsBuilt} />
            <KpiCard label={t('libraryItems')} value={last.libraryItems} />
            <KpiCard label={t('errors')} value={last.errorCount} />
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

      {logs.data && logs.data.length > 1 ? (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('lastImport')}</TableHead>
              <TableHead>{t('files')}</TableHead>
              <TableHead>{t('blocks')}</TableHead>
              <TableHead>{t('runs')}</TableHead>
              <TableHead>{t('libraryItems')}</TableHead>
              <TableHead>{t('errors')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {logs.data.map((log) => (
              <TableRow key={log.id}>
                <TableCell>
                  <DateTimeText value={log.finishedAt} />
                </TableCell>
                <TableCell data-ltr>{log.filesRead}</TableCell>
                <TableCell data-ltr>{log.blocksRead}</TableCell>
                <TableCell data-ltr>{log.runsBuilt}</TableCell>
                <TableCell data-ltr>{log.libraryItems}</TableCell>
                <TableCell data-ltr>{log.errorCount}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : null}
    </div>
  );
}
