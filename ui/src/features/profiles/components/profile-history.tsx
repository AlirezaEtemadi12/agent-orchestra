'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { DataTable, type DataTableColumn } from '@/components/app/data-table';
import { EmptyState } from '@/components/app/empty-state';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useTableQuery } from '@/lib/gridify/use-table-query';
import { useVersionDiff, useVersionsTable } from '../queries';
import type { ProfileVersionSummary } from '../types';

type ProfileHistoryProps = {
  id: string;
  currentVersion: number;
  busy: boolean;
  onRevert: (version: number) => void;
};

export function ProfileHistory({ id, currentVersion, busy, onRevert }: ProfileHistoryProps) {
  const t = useTranslations('profiles');
  const common = useTranslations('common');
  const [left, setLeft] = useState<number | null>(null);
  const [right, setRight] = useState<number | null>(null);
  const diff = left !== null && right !== null && left !== right ? `${left},${right}` : undefined;

  const tableQuery = useTableQuery({ defaultOrderBy: 'version desc', defaultPageSize: 20 });
  const versions = useVersionsTable(id, {
    page: tableQuery.state.page,
    pageSize: tableQuery.state.pageSize,
    orderBy: tableQuery.state.orderBy,
  });
  const changes = useVersionDiff(id, diff);

  const columns: readonly DataTableColumn<ProfileVersionSummary>[] = [
    {
      id: 'version',
      header: 'n',
      sortable: true,
      cell: (version) => <span data-ltr>{t('version', { n: version.version })}</span>,
    },
    {
      id: 'reason',
      header: t('reason'),
      sortable: true,
      cell: (version) => <span className="text-sm">{version.reason}</span>,
    },
    {
      id: 'ab',
      header: '',
      cell: (version) => (
        <div className="flex gap-1">
          <Button
            type="button"
            size="xs"
            variant={left === version.version ? 'secondary' : 'ghost'}
            onClick={() => setLeft(version.version)}
          >
            a
          </Button>
          <Button
            type="button"
            size="xs"
            variant={right === version.version ? 'secondary' : 'ghost'}
            onClick={() => setRight(version.version)}
          >
            b
          </Button>
        </div>
      ),
    },
    {
      id: 'revert',
      header: '',
      cell: (version) =>
        version.version === currentVersion ? null : (
          <Button
            type="button"
            size="xs"
            variant="ghost"
            disabled={busy}
            onClick={() => onRevert(version.version)}
          >
            {t('revert')}
          </Button>
        ),
    },
  ];

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('history')}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <DataTable
          columns={columns}
          rows={versions.data?.data ?? []}
          count={versions.data?.count ?? 0}
          page={tableQuery.state.page}
          pageSize={tableQuery.state.pageSize}
          orderBy={tableQuery.state.orderBy}
          onPageChange={tableQuery.setPage}
          onPageSizeChange={tableQuery.setPageSize}
          onOrderChange={tableQuery.setOrderBy}
          onRetry={() => void versions.refetch()}
          loading={versions.isPending}
          error={versions.error}
          empty={<EmptyState title={common('states.empty')} />}
        />

        {changes.data?.diff ? (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>path</TableHead>
                <TableHead>a</TableHead>
                <TableHead>b</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {changes.data.diff.map((change) => (
                <TableRow key={change.path}>
                  <TableCell data-ltr className="text-xs">
                    {change.path}
                  </TableCell>
                  <TableCell data-ltr className="text-xs text-muted-foreground">
                    {change.from ?? '—'}
                  </TableCell>
                  <TableCell data-ltr className="text-xs">
                    {change.to ?? '—'}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : null}
      </CardContent>
    </Card>
  );
}
